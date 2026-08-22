using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Summoner;
using Godot;
using GDict = Godot.Collections.Dictionary;

namespace Fateforged.Meta.Services.Traits;

[GlobalClass]
public partial class TraitTreeService : Node
{
    private const string CardCoreRootPrefix = "__card_core_root__:";

    public static TraitTreeService? Instance { get; private set; }

    private IProfileRepository? _profileRepo;

    public override void _Ready()
    {
        Instance = this;
        _profileRepo = ProfileRepository.Instance;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public GDict GetSummonerTreeViewModel(string summonerId)
    {
        if (!TryBuildSummonerSnapshot(summonerId, out var snapshot))
            return new GDict();

        return BuildTreeViewModel("summoner", summonerId, snapshot.Context, null);
    }

    public GDict GetCardTreeViewModel(string cardInstanceId)
    {
        if (!TryBuildCardSnapshot(cardInstanceId, out var snapshot))
            return new GDict();

        var viewModel = BuildTreeViewModel(
            "card",
            cardInstanceId,
            snapshot.Context,
            CardCoreCatalog.GetCoreTraitIds(snapshot.Instance.CatalogId)
                .Select(id => id.Value)
                .ToHashSet(StringComparer.Ordinal)
        );
        AddCardCoreRoot(viewModel, snapshot.Instance.CatalogId);
        return viewModel;
    }

    public GDict GetTraitNodeDetail(string ownerType, string ownerId, string traitId)
    {
        if (
            string.IsNullOrWhiteSpace(ownerType)
            || string.IsNullOrWhiteSpace(ownerId)
            || string.IsNullOrWhiteSpace(traitId)
        )
            return new GDict();

        var vm = ownerType.Trim().ToLowerInvariant() switch
        {
            "summoner" => GetSummonerTreeViewModel(ownerId),
            "card" => GetCardTreeViewModel(ownerId),
            _ => new GDict(),
        };

        if (vm.Count == 0)
            return new GDict();

        var targetTraitId = traitId.Trim();
        foreach (var node in ReadDictArray(vm, "progression_nodes"))
        {
            if (ReadString(node, "id") != targetTraitId)
                continue;

            return BuildDetailFromNode(node, ReadInt(vm, "unspent_trait_points"));
        }

        foreach (var node in ReadDictArray(vm, "one_off_nodes"))
        {
            if (ReadString(node, "id") != targetTraitId)
                continue;

            return BuildDetailFromNode(node, ReadInt(vm, "unspent_trait_points"));
        }

        return new GDict();
    }

    public GDict TryUnlockTrait(string ownerType, string ownerId, string traitId)
    {
        if (
            string.IsNullOrWhiteSpace(ownerType)
            || string.IsNullOrWhiteSpace(ownerId)
            || string.IsNullOrWhiteSpace(traitId)
        )
        {
            return new GDict { ["success"] = false, ["reason"] = "Invalid unlock request" };
        }

        var normalizedOwnerType = ownerType.Trim().ToLowerInvariant();
        var normalizedOwnerId = ownerId.Trim();
        var normalizedTraitId = traitId.Trim();

        var detail = GetTraitNodeDetail(normalizedOwnerType, normalizedOwnerId, normalizedTraitId);
        if (detail.Count == 0)
        {
            return new GDict
            {
                ["success"] = false,
                ["reason"] = "Trait is not available for this owner",
            };
        }

        if (ReadBool(detail, "is_owned"))
        {
            return new GDict { ["success"] = false, ["reason"] = "Trait already unlocked" };
        }

        if (!ReadBool(detail, "unlock_button_visible"))
        {
            return new GDict { ["success"] = false, ["reason"] = "Trait cannot be unlocked here" };
        }

        if (!ReadBool(detail, "unlock_button_enabled"))
        {
            return new GDict
            {
                ["success"] = false,
                ["reason"] = ReadString(
                    detail,
                    "unlock_blocked_reason",
                    "Requirements were not met"
                ),
            };
        }

        var success = normalizedOwnerType switch
        {
            "summoner" => SummonerProgressionService.Instance?.SpendTraitPoint(
                normalizedOwnerId,
                normalizedTraitId
            ) ?? false,
            "card" => CardService.Instance?.SpendCardTraitPoint(
                normalizedOwnerId,
                normalizedTraitId
            ) ?? false,
            _ => false,
        };

        var reason = success ? "" : "Requirements were not met";
        var updatedTree = normalizedOwnerType switch
        {
            "summoner" => GetSummonerTreeViewModel(normalizedOwnerId),
            "card" => GetCardTreeViewModel(normalizedOwnerId),
            _ => new GDict(),
        };

        return new GDict
        {
            ["success"] = success,
            ["reason"] = reason,
            ["owner_type"] = normalizedOwnerType,
            ["owner_id"] = normalizedOwnerId,
            ["trait_id"] = normalizedTraitId,
            ["tree"] = updatedTree,
        };
    }

    private GDict BuildTreeViewModel(
        string ownerType,
        string ownerId,
        TraitTreeOwnerContext context,
        IReadOnlySet<string>? cardCoreTraitIds
    )
    {
        var progressionTraits = TraitCatalog
            .GetTraitsByAcquisitionMode(TraitAcquisitionMode.LevelUpOffer)
            .Where(trait => !trait.IsInnate)
            .Where(trait => cardCoreTraitIds == null || cardCoreTraitIds.Contains(trait.Id.Value))
            .Where(trait =>
                context.OwnedTraitIds.Contains(trait.Id.Value)
                || TraitTreeEvaluator.MatchesOwnerTags(trait, context)
            )
            .ToList();

        var progressionById = progressionTraits.ToDictionary(
            t => t.Id.Value,
            t => t,
            StringComparer.Ordinal
        );
        var depthById = TraitTreeEvaluator.ComputeDepthByTraitId(progressionTraits);

        var progressionNodes = new Godot.Collections.Array<GDict>();
        var maxDepth = 0;

        foreach (
            var trait in progressionTraits
                .OrderBy(
                    t => ResolveNameWithFallback(t.NameKey, t.Id.Value),
                    StringComparer.Ordinal
                )
                .ThenBy(t => t.Id.Value, StringComparer.Ordinal)
        )
        {
            var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(trait, context);
            // Permanent alternative branches remain in save state but disappear
            // from the normal Core view after the player commits elsewhere.
            if (evaluation.IsPermanentlyClosed)
                continue;

            var depth = depthById.GetValueOrDefault(trait.Id.Value, 0);
            maxDepth = Math.Max(maxDepth, depth);

            var lockedReason = BuildLockedReason(evaluation, progressionById);
            var unlockBlockedReason = evaluation.CanUnlockNow
                ? ""
                : BuildUnlockBlockedReason(evaluation, progressionById);

            progressionNodes.Add(
                new GDict
                {
                    ["id"] = trait.Id.Value,
                    ["name"] = ResolveNameWithFallback(trait.NameKey, trait.Id.Value),
                    ["description"] = ResolveDescription(trait.DescriptionKey),
                    ["name_key"] = trait.NameKey,
                    ["description_key"] = trait.DescriptionKey,
                    ["category"] = trait.Category.ToStringValue(),
                    ["acquisition_mode"] = trait.AcquisitionMode.ToStringValue(),
                    ["depth"] = depth,
                    ["prerequisites"] = ToStringArray(trait.Prerequisites),
                    ["state"] = evaluation.NodeState.ToStringValue(),
                    ["is_owned"] = evaluation.IsOwned,
                    ["is_unlockable"] = evaluation.IsEligibleWithoutPoints,
                    ["can_unlock"] = evaluation.CanUnlockNow,
                    ["is_closed"] = evaluation.IsPermanentlyClosed,
                    ["locked_reason"] = lockedReason,
                    ["unlock_blocked_reason"] = unlockBlockedReason,
                }
            );
        }

        var oneOffNodes = new Godot.Collections.Array<GDict>();
        var oneOffTraits = TraitCatalog
            .GetTraitsByAcquisitionMode(TraitAcquisitionMode.GrantedOnly)
            .Where(trait =>
                context.OwnedTraitIds.Contains(trait.Id.Value)
                || TraitTreeEvaluator.MatchesOwnerTags(trait, context)
            )
            .OrderBy(
                trait => ResolveNameWithFallback(trait.NameKey, trait.Id.Value),
                StringComparer.Ordinal
            )
            .ThenBy(trait => trait.Id.Value, StringComparer.Ordinal)
            .ToList();

        foreach (var trait in oneOffTraits)
        {
            var isOwned = context.OwnedTraitIds.Contains(trait.Id.Value);
            oneOffNodes.Add(
                new GDict
                {
                    ["id"] = trait.Id.Value,
                    ["name"] = ResolveNameWithFallback(trait.NameKey, trait.Id.Value),
                    ["description"] = ResolveDescription(trait.DescriptionKey),
                    ["name_key"] = trait.NameKey,
                    ["description_key"] = trait.DescriptionKey,
                    ["category"] = trait.Category.ToStringValue(),
                    ["acquisition_mode"] = trait.AcquisitionMode.ToStringValue(),
                    ["depth"] = 0,
                    ["prerequisites"] = ToStringArray(trait.Prerequisites),
                    ["state"] = isOwned
                        ? TraitTreeNodeState.Owned.ToStringValue()
                        : TraitTreeNodeState.Locked.ToStringValue(),
                    ["is_owned"] = isOwned,
                    ["is_unlockable"] = false,
                    ["can_unlock"] = false,
                    ["locked_reason"] = isOwned ? "" : "Granted from events or rewards",
                    ["unlock_blocked_reason"] = isOwned ? "" : "Granted from events or rewards",
                }
            );
        }

        var edges = new Godot.Collections.Array<GDict>();
        var progressionIdSet = progressionNodes
            .Select(node => ReadString(node, "id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var trait in progressionTraits)
        {
            if (!progressionIdSet.Contains(trait.Id.Value))
                continue;
            foreach (var prereqId in trait.Prerequisites)
            {
                if (string.IsNullOrWhiteSpace(prereqId) || !progressionIdSet.Contains(prereqId))
                    continue;

                edges.Add(new GDict { ["from"] = prereqId, ["to"] = trait.Id.Value });
            }
        }

        var hasAvailableUnlocks = progressionNodes.Any(node =>
            ReadBool(node, "is_unlockable") && !ReadBool(node, "is_owned")
        );
        var hasUnlockableNow = progressionNodes.Any(node => ReadBool(node, "can_unlock"));

        return new GDict
        {
            ["owner_type"] = ownerType,
            ["owner_id"] = ownerId,
            ["level"] = context.CurrentLevel,
            ["unspent_trait_points"] = context.UnspentTraitPoints,
            ["max_depth"] = maxDepth,
            ["has_available_unlocks"] = hasAvailableUnlocks,
            ["has_unlockable_now"] = hasUnlockableNow,
            ["progression_nodes"] = progressionNodes,
            ["one_off_nodes"] = oneOffNodes,
            ["edges"] = edges,
        };
    }

    private static GDict BuildDetailFromNode(GDict node, int unspentTraitPoints)
    {
        var state = ReadString(node, "state");
        var isOwned = ReadBool(node, "is_owned");
        var canUnlock = ReadBool(node, "can_unlock");
        var isUnlockable = ReadBool(node, "is_unlockable");
        var acquisitionMode = ReadString(node, "acquisition_mode");

        var unlockButtonVisible =
            !isOwned && acquisitionMode == TraitAcquisitionMode.LevelUpOffer.ToStringValue();
        var unlockButtonEnabled = unlockButtonVisible && canUnlock;

        return new GDict
        {
            ["id"] = ReadString(node, "id"),
            ["name"] = ReadString(node, "name"),
            ["description"] = ReadString(node, "description"),
            ["category"] = ReadString(node, "category"),
            ["state"] = state,
            ["is_owned"] = isOwned,
            ["is_unlockable"] = isUnlockable,
            ["can_unlock"] = canUnlock,
            ["locked_reason"] = ReadString(node, "locked_reason"),
            ["unlock_blocked_reason"] = unlockButtonEnabled
                ? ""
                : ReadString(node, "unlock_blocked_reason", ReadString(node, "locked_reason")),
            ["unlock_button_visible"] = unlockButtonVisible,
            ["unlock_button_enabled"] = unlockButtonEnabled,
            ["unlock_button_text"] = "Unlock (1)",
            ["unspent_trait_points"] = unspentTraitPoints,
        };
    }

    private static void AddCardCoreRoot(GDict viewModel, CardId cardCatalogId)
    {
        if (
            !viewModel.TryGetValue("progression_nodes", out var nodesValue)
            || nodesValue.VariantType != Variant.Type.Array
        )
            return;

        var cardDefinition = CardCatalog.GetCard(cardCatalogId);
        if (cardDefinition == null)
            return;

        var progressionNodes = nodesValue.AsGodotArray<GDict>();
        var rootId = CardCoreRootPrefix + cardCatalogId.Value;
        var rootNode = new GDict
        {
            ["id"] = rootId,
            ["name"] = cardDefinition.Name,
            ["description"] = cardDefinition.Description,
            ["category"] = "core",
            ["acquisition_mode"] = "inherent",
            ["depth"] = 0,
            ["prerequisites"] = new Godot.Collections.Array<string>(),
            ["state"] = TraitTreeNodeState.Owned.ToStringValue(),
            ["is_owned"] = true,
            ["is_unlockable"] = false,
            ["can_unlock"] = false,
            ["is_closed"] = false,
            ["is_core_root"] = true,
            ["locked_reason"] = "",
            ["unlock_blocked_reason"] = "",
        };

        var rootChildren = new List<string>();
        foreach (var node in progressionNodes)
        {
            var prerequisites = node.TryGetValue("prerequisites", out var prerequisitesValue)
                && prerequisitesValue.VariantType == Variant.Type.Array
                ? prerequisitesValue.AsGodotArray<string>()
                : new Godot.Collections.Array<string>();

            node["depth"] = ReadInt(node, "depth") + 1;
            if (prerequisites.Count != 0)
                continue;

            node["prerequisites"] = new Godot.Collections.Array<string> { rootId };
            rootChildren.Add(ReadString(node, "id"));
        }

        progressionNodes.Insert(0, rootNode);

        if (rootChildren.Count > 0)
        {
            viewModel["max_depth"] = ReadInt(viewModel, "max_depth") + 1;
            if (
                viewModel.TryGetValue("edges", out var edgesValue)
                && edgesValue.VariantType == Variant.Type.Array
            )
            {
                var edges = edgesValue.AsGodotArray<GDict>();
                foreach (var childId in rootChildren)
                    edges.Add(new GDict { ["from"] = rootId, ["to"] = childId });
            }
        }
    }

    private bool TryBuildSummonerSnapshot(string summonerId, out SummonerSnapshot snapshot)
    {
        snapshot = default;

        if (_profileRepo == null)
            return false;

        var typedSummonerId = SummonerId.FromString(summonerId);
        var summoner = _profileRepo.GetSummonerInstance(typedSummonerId);
        if (summoner == null)
            return false;

        var summonerDef = SummonerCatalog.GetSummoner(typedSummonerId);
        if (summonerDef == null)
            return false;

        var tags = new HashSet<string>(summonerDef.TraitEligibilityTags, StringComparer.Ordinal)
        {
            TraitTags.Summoner,
        };

        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summoner,
            EligibilityTags = tags,
            OwnedTraitIds = summoner.GetAllTraitIds().ToHashSet(StringComparer.Ordinal),
            CurrentLevel = summoner.Level,
            UnspentTraitPoints = summoner.UnspentTraitPoints,
        };

        snapshot = new SummonerSnapshot(summoner, summonerDef, context);
        return true;
    }

    private bool TryBuildCardSnapshot(string cardInstanceId, out CardSnapshot snapshot)
    {
        snapshot = default;

        if (_profileRepo == null)
            return false;

        var typedCardId = CardInstanceId.FromString(cardInstanceId);
        var card = _profileRepo.GetCard(typedCardId);
        if (card == null)
            return false;

        var cardDef = CardCatalog.GetCard(card.CatalogId);
        if (cardDef == null)
            return false;

        var ownerTypeTag = TraitTreeEvaluator.ResolveOwnerTypeTag(cardDef);
        if (string.IsNullOrEmpty(ownerTypeTag))
            return false;

        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = ownerTypeTag,
            EligibilityTags = TraitTreeEvaluator.BuildEffectiveCardTagSet(cardDef, ownerTypeTag),
            OwnedTraitIds = card
                .Traits.Select(traitId => traitId.Value)
                .ToHashSet(StringComparer.Ordinal),
            CurrentLevel = card.Level,
            UnspentTraitPoints = card.UnspentTraitPoints,
            CardCatalogId = card.CatalogId.Value,
            CardRarity = card.Rarity,
        };

        snapshot = new CardSnapshot(card, cardDef, context);
        return true;
    }

    private string BuildLockedReason(
        TraitUnlockEvaluation evaluation,
        IReadOnlyDictionary<string, TraitDefinition> progressionById
    )
    {
        if (evaluation.IsOwned || evaluation.NodeState == TraitTreeNodeState.Available)
            return "";

        if (evaluation.MissingPrerequisiteIds.Count == 0)
            return evaluation.LockedReason;

        var prerequisiteNames = evaluation
            .MissingPrerequisiteIds.Select(id =>
                progressionById.TryGetValue(id, out var prereq)
                    ? ResolveNameWithFallback(prereq.NameKey, id)
                    : id
            )
            .ToArray();

        return $"Requires: {string.Join(", ", prerequisiteNames)}";
    }

    private string BuildUnlockBlockedReason(
        TraitUnlockEvaluation evaluation,
        IReadOnlyDictionary<string, TraitDefinition> progressionById
    )
    {
        if (evaluation.CanUnlockNow)
            return "";

        if (evaluation.MissingPrerequisiteIds.Count > 0)
            return BuildLockedReason(evaluation, progressionById);

        return string.IsNullOrWhiteSpace(evaluation.UnlockBlockedReason)
            ? evaluation.LockedReason
            : evaluation.UnlockBlockedReason;
    }

    private string ResolveNameWithFallback(string key, string fallback)
    {
        var resolved = ResolveLoc(key);
        if (string.IsNullOrWhiteSpace(resolved) || resolved == key)
            return fallback;
        return resolved;
    }

    private string ResolveDescription(string key)
    {
        var resolved = ResolveLoc(key);
        return resolved == key ? "" : resolved;
    }

    private string ResolveLoc(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        var loc = GetTree()?.Root?.GetNodeOrNull<Node>("Loc");
        if (loc != null && loc.HasMethod("t"))
            return loc.Call("t", key).AsString();

        return key;
    }

    private static Godot.Collections.Array<string> ToStringArray(IEnumerable<string> values)
    {
        var result = new Godot.Collections.Array<string>();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static string ReadString(GDict dict, string key, string fallback = "")
    {
        return dict.TryGetValue(key, out var value) ? value.AsString() : fallback;
    }

    private static int ReadInt(GDict dict, string key, int fallback = 0)
    {
        if (!dict.TryGetValue(key, out var value))
            return fallback;

        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => (int)value.AsDouble(),
            _ => fallback,
        };
    }

    private static bool ReadBool(GDict dict, string key, bool fallback = false)
    {
        return dict.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Bool
            ? value.AsBool()
            : fallback;
    }

    private static IEnumerable<GDict> ReadDictArray(GDict dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value.VariantType != Variant.Type.Array)
            return [];

        var result = new List<GDict>();
        foreach (var entry in value.AsGodotArray())
        {
            if (entry.VariantType == Variant.Type.Dictionary)
                result.Add(entry.AsGodotDictionary());
        }

        return result;
    }

    private readonly record struct SummonerSnapshot(
        SummonerInstance Instance,
        SummonerDefinition Definition,
        TraitTreeOwnerContext Context
    );

    private readonly record struct CardSnapshot(
        CardInstance Instance,
        CardDefinition Definition,
        TraitTreeOwnerContext Context
    );
}
