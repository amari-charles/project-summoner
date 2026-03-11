using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Cards;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards.Handlers;
using Fateforged.Data.Traits;
using Fateforged.Stats;

namespace Fateforged.Meta.Cards;

/// <summary>
/// Card Service - Unified service for card ownership and progression.
///
/// String-accepting facade for GDScript; delegates to typed handlers internally.
/// </summary>
[GlobalClass]
public partial class CardService : Node
{
    public static CardService? Instance { get; private set; }

    private IProfileRepository? _profileRepo;
    private CardOwnershipHandler? _ownership;
    private CardProgressionHandler? _progression;

    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void CardsGrantedEventHandler(Godot.Collections.Array<string> instanceIds);

    [Signal]
    public delegate void CardRemovedEventHandler(string instanceId);

    [Signal]
    public delegate void CardXpChangedEventHandler(string cardInstanceId, int newXp, int newLevel);

    [Signal]
    public delegate void CardLeveledUpEventHandler(string cardInstanceId, int newLevel);

    [Signal]
    public delegate void TraitAppliedEventHandler(string cardInstanceId, string traitId);

    [Signal]
    public delegate void CollectionChangedEventHandler();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        Initialize();
    }

    private void Initialize()
    {
        GD.Print("CardService: Initializing...");

        _profileRepo = ProfileRepository.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("CardService: ProfileRepository.Instance not available");
            return;
        }

        _ownership = new CardOwnershipHandler(_profileRepo);
        _progression = new CardProgressionHandler(_profileRepo);

        _profileRepo.DataChanged += OnRepoDataChanged;

        GD.Print("CardService: Ready");
    }

    public override void _ExitTree()
    {
        if (_profileRepo != null)
        {
            _profileRepo.DataChanged -= OnRepoDataChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    /// <summary>Initialize for testing with mock dependencies.</summary>
    public void InitForTesting(IProfileRepository repo)
    {
        ArgumentNullException.ThrowIfNull(repo);
        _profileRepo = repo;
        _ownership = new CardOwnershipHandler(repo);
        _progression = new CardProgressionHandler(repo);
    }

    private void OnRepoDataChanged()
    {
        EmitSignal(SignalName.CollectionChanged);
    }

    // =========================================================================
    // OWNERSHIP - QUERIES
    // =========================================================================

    /// <summary>Get all card instances in the collection.</summary>
    public CardInstance[] ListCards()
    {
        return _ownership?.ListCards() ?? [];
    }

    /// <summary>Get a specific card instance by ID.</summary>
    public CardInstance? GetCard(string cardInstanceId)
    {
        return _ownership?.GetCard(CardInstanceId.FromString(cardInstanceId));
    }

    /// <summary>Get count of cards by catalog ID.</summary>
    public int GetCardCount(string catalogId)
    {
        return _ownership?.GetCardCount(CardId.FromString(catalogId)) ?? 0;
    }

    /// <summary>Check if player owns at least one of a card.</summary>
    public bool HasCard(string catalogId)
    {
        return _ownership?.HasCard(CardId.FromString(catalogId)) ?? false;
    }

    /// <summary>Get all instances of a specific catalog ID.</summary>
    public CardInstance[] GetCardsByCatalogId(string catalogId)
    {
        return _ownership?.GetCardsByCatalogId(CardId.FromString(catalogId)) ?? [];
    }

    /// <summary>Get all AccountWide cards.</summary>
    public CardInstance[] GetAccountWideCards()
    {
        return _ownership?.GetAccountWideCards() ?? [];
    }

    /// <summary>Get SummonerBound cards for a specific summoner.</summary>
    public CardInstance[] GetSummonerBoundCards(string summonerId)
    {
        return _ownership?.GetSummonerBoundCards(Data.Summoners.SummonerId.FromString(summonerId)) ?? [];
    }

    /// <summary>Get all cards owned by a summoner (AccountWide + SummonerBound).</summary>
    public CardInstance[] GetOwnedCards(string summonerId)
    {
        return _ownership?.GetOwnedCards(Data.Summoners.SummonerId.FromString(summonerId)) ?? [];
    }

    /// <summary>Get collection grouped by catalog ID.</summary>
    public Dictionary<string, CardInstance[]> GetCollectionGrouped()
    {
        return _ownership?.GetCollectionGrouped() ?? [];
    }

    /// <summary>Get collection summary for UI display.</summary>
    public CollectionSummaryEntry[] GetCollectionSummary()
    {
        return _ownership?.GetCollectionSummary() ?? [];
    }

    // =========================================================================
    // OWNERSHIP - OPERATIONS
    // =========================================================================

    /// <summary>Grant cards to the player's collection.</summary>
    public string[] GrantCards(IEnumerable<(string catalogId, string rarity)> cards)
    {
        var typedCards = cards.Select(c => (CardId.FromString(c.catalogId), c.rarity));
        var instanceIds = _ownership?.GrantCards(typedCards) ?? [];

        if (instanceIds.Length > 0)
        {
            var gdArray = new Godot.Collections.Array<string>();
            foreach (var id in instanceIds)
                gdArray.Add(id);
            EmitSignal(SignalName.CardsGranted, gdArray);
        }

        return instanceIds.Select(id => (string)id).ToArray();
    }

    /// <summary>Grant a single card.</summary>
    public string GrantCard(string catalogId, string rarity = "common")
    {
        var instanceId = _ownership?.GrantCard(CardId.FromString(catalogId), rarity) ?? CardInstanceId.None;
        return instanceId;
    }

    /// <summary>Remove a card instance from the collection.</summary>
    public bool RemoveCard(string cardInstanceId)
    {
        var success = _ownership?.RemoveCard(CardInstanceId.FromString(cardInstanceId)) ?? false;
        if (success)
        {
            EmitSignal(SignalName.CardRemoved, cardInstanceId);
        }
        return success;
    }

    /// <summary>Calculate dismantle value for a rarity.</summary>
    public static int GetDismantleValue(string rarity)
    {
        return CardOwnershipHandler.GetDismantleValue(rarity);
    }

    // =========================================================================
    // PROGRESSION - XP
    // =========================================================================

    /// <summary>Grant XP to a card.</summary>
    public int GrantXp(string cardInstanceId, int amount)
    {
        var typedId = CardInstanceId.FromString(cardInstanceId);
        var newXp = _progression?.GrantXp(typedId, amount) ?? 0;
        if (newXp > 0)
        {
            var card = _ownership?.GetCard(typedId);
            EmitSignal(SignalName.CardXpChanged, cardInstanceId, newXp, card?.Level ?? 1);
        }
        return newXp;
    }

    /// <summary>Grant XP to multiple cards.</summary>
    public Dictionary<string, int> GrantXpToCards(IEnumerable<string> cardInstanceIds, int amount)
    {
        var typedIds = cardInstanceIds.Select(CardInstanceId.FromString);
        var typedResults = _progression?.GrantXpToCards(typedIds, amount) ?? [];
        return typedResults.ToDictionary(kvp => (string)kvp.Key, kvp => kvp.Value);
    }

    /// <summary>Grant XP to multiple cards (GDScript-friendly).</summary>
    public Godot.Collections.Dictionary GrantXpToCardsArray(Godot.Collections.Array<string> cardInstanceIds, int amount)
    {
        var results = GrantXpToCards(cardInstanceIds, amount);
        var gdResult = new Godot.Collections.Dictionary();
        foreach (var kvp in results)
        {
            gdResult[kvp.Key] = kvp.Value;
        }
        return gdResult;
    }

    /// <summary>Get XP required for a level (base).</summary>
    public int GetXpForLevel(int level)
    {
        return _progression?.GetXpForLevel(level) ?? 0;
    }

    /// <summary>Get XP required for a level with rarity scaling.</summary>
    public int GetXpForLevelWithRarity(int level, string rarity)
    {
        return _progression?.GetXpForLevelWithRarity(level, rarity) ?? 0;
    }

    /// <summary>Get rarity multiplier.</summary>
    public float GetRarityMultiplier(string rarity)
    {
        return _progression?.GetRarityMultiplier(rarity) ?? 1.0f;
    }

    /// <summary>Get XP needed for next level.</summary>
    public int GetXpToNextLevel(string cardInstanceId)
    {
        return _progression?.GetXpToNextLevel(CardInstanceId.FromString(cardInstanceId)) ?? 0;
    }

    /// <summary>Get progress toward next level (0.0 - 1.0).</summary>
    public float GetLevelProgress(string cardInstanceId)
    {
        return _progression?.GetLevelProgress(CardInstanceId.FromString(cardInstanceId)) ?? 0f;
    }

    // =========================================================================
    // PROGRESSION - LEVEL-UP
    // =========================================================================

    /// <summary>Check if card can level up (has enough XP).</summary>
    public bool CanLevelUp(string cardInstanceId)
    {
        return _progression?.CanLevelUp(CardInstanceId.FromString(cardInstanceId)) ?? false;
    }

    /// <summary>Level up a card (XP-only, no gold cost). Trait spend is deferred.</summary>
    public bool LevelUpCard(string cardInstanceId)
    {
        var success = _progression?.LevelUpCard(CardInstanceId.FromString(cardInstanceId)) ?? false;
        if (success)
        {
            var card = GetCard(cardInstanceId);
            EmitSignal(SignalName.CardLeveledUp, cardInstanceId, card?.Level ?? 1);
        }
        return success;
    }

    // =========================================================================
    // PROGRESSION - UNIFIED TRAIT LEDGER (Pass 2 stubs)
    // =========================================================================

    public int GetCardUnspentTraitPoints(string cardInstanceId)
    {
        return _progression?.GetCardUnspentTraitPoints(CardInstanceId.FromString(cardInstanceId)) ?? 0;
    }

    public int GrantCardTraitPoints(string cardInstanceId, int amount, string source = "")
    {
        return _progression?.GrantCardTraitPoints(CardInstanceId.FromString(cardInstanceId), amount, source) ?? 0;
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> RollCardTraitOffers(string cardInstanceId, int count = 3)
    {
        var offers = _progression?.RollCardTraitOffers(CardInstanceId.FromString(cardInstanceId), count) ?? [];
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var offer in offers)
        {
            var traitDef = TraitCatalog.GetTrait(offer.TraitId.Value);
            var displayName = string.IsNullOrWhiteSpace(offer.DisplayName.LocalizationKey)
                ? offer.DisplayName.ResolveDisplayText()
                : ResolveLoc(offer.DisplayName.LocalizationKey);
            var description = string.IsNullOrWhiteSpace(offer.Description.LocalizationKey)
                ? offer.Description.ResolveDisplayText()
                : ResolveLoc(offer.Description.LocalizationKey);
            var summaryShort = TraitSummaryFormatter.BuildSummaryShort(traitDef);

            result.Add(new Godot.Collections.Dictionary
            {
                ["trait_id"] = offer.TraitId.Value,
                ["display_name"] = displayName,
                ["description"] = description,
                ["summary_short"] = summaryShort,
                ["weight"] = offer.Weight.Value
            });
        }
        return result;
    }

    public bool SpendCardTraitPoint(string cardInstanceId, string traitId)
    {
        var success = _progression?.SpendCardTraitPoint(CardInstanceId.FromString(cardInstanceId), traitId) ?? false;
        if (success)
            EmitSignal(SignalName.TraitApplied, cardInstanceId, traitId);
        return success;
    }

    // =========================================================================
    // PROGRESSION - TRAITS
    // =========================================================================

    /// <summary>Get all traits applied to a card.</summary>
    public Godot.Collections.Array<string> GetAppliedTraits(string cardInstanceId)
    {
        var traits = _progression?.GetAppliedTraits(CardInstanceId.FromString(cardInstanceId)) ?? [];
        var result = new Godot.Collections.Array<string>();
        foreach (var t in traits)
            result.Add(t);
        return result;
    }

    /// <summary>Get a trait as dictionary for GDScript (name, description, stat_mods).</summary>
    public Godot.Collections.Dictionary GetCardTraitDict(string traitId)
    {
        if (string.IsNullOrWhiteSpace(traitId))
            return [];

        var normalizedTraitId = traitId.Trim();
        var unifiedTrait = TraitCatalog.GetTrait(normalizedTraitId);
        if (unifiedTrait == null)
            return [];

        var statMods = new Godot.Collections.Dictionary();
        foreach (var mod in unifiedTrait.Modifiers)
        {
            if (mod.HasSummonerStat && mod.Stat.HasValue)
            {
                var statKey = mod.Stat.Value.ToSnakeCase();
                statMods[statKey] = mod.Value;
            }

            if (mod.StatMults == null || mod.StatMults.Count == 0)
                continue;

            foreach (var (stat, mult) in mod.StatMults)
                statMods[stat.ToSnakeCase()] = mult;
        }

        return new Godot.Collections.Dictionary
        {
            ["id"] = (string)unifiedTrait.Id,
            ["name"] = ResolveLoc(unifiedTrait.NameKey),
            ["description"] = ResolveLoc(unifiedTrait.DescriptionKey),
            ["summary_short"] = TraitSummaryFormatter.BuildSummaryShort(unifiedTrait),
            ["stat_mods"] = statMods
        };
    }

    /// <summary>Get stat modifiers from card's traits (for C# callers).</summary>
    public Dictionary<string, float> GetTraitStatModifiersTyped(string cardInstanceId)
    {
        return _progression?.GetTraitStatModifiers(CardInstanceId.FromString(cardInstanceId)) ?? [];
    }

    /// <summary>Get additive spawn-count bonus from card traits.</summary>
    public int GetTraitSpawnCountBonus(string cardInstanceId)
    {
        return _progression?.GetTraitSpawnCountBonus(CardInstanceId.FromString(cardInstanceId)) ?? 0;
    }

    /// <summary>Get stat modifiers from card's traits (for GDScript callers).</summary>
    public Godot.Collections.Dictionary GetTraitStatModifiers(string cardInstanceId)
    {
        var mods = _progression?.GetTraitStatModifiers(CardInstanceId.FromString(cardInstanceId)) ?? [];
        var result = new Godot.Collections.Dictionary();
        foreach (var (stat, mult) in mods)
            result[stat] = mult;
        return result;
    }

    // =========================================================================
    // PROGRESSION - INFO
    // =========================================================================

    /// <summary>Get card progression info.</summary>
    public Godot.Collections.Dictionary GetCardProgressionInfoDict(string cardInstanceId)
    {
        var info = _progression?.GetCardProgressionInfo(CardInstanceId.FromString(cardInstanceId));
        if (info == null)
            return [];

        var traitsArray = new Godot.Collections.Array<string>();
        foreach (var t in info.Traits)
            traitsArray.Add(t);

        return new Godot.Collections.Dictionary
        {
            ["card_instance_id"] = info.CardInstanceId,
            ["catalog_id"] = info.CatalogId,
            ["rarity"] = info.Rarity,
            ["rarity_multiplier"] = info.RarityMultiplier,
            ["level"] = info.Level,
            ["max_level"] = info.MaxLevel,
            ["xp"] = info.Xp,
            ["xp_for_next_level"] = info.XpForNextLevel,
            ["xp_progress"] = info.XpProgress,
            ["can_level_up"] = info.CanLevelUp,
            ["traits"] = traitsArray,
            ["is_max_level"] = info.IsMaxLevel,
            ["unspent_trait_points"] = info.UnspentTraitPoints
        };
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

    /// <summary>Get all cards that can level up.</summary>
    public CardInstance[] GetCardsReadyToLevelUp()
    {
        return _progression?.GetCardsReadyToLevelUp() ?? [];
    }

    // =========================================================================
    // GODOT INTEROP
    // =========================================================================

    /// <summary>List cards as array of dictionaries for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> ListCardsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in ListCards())
        {
            result.Add(DtoConverters.ToDict(card));
        }
        return result;
    }

    /// <summary>Get card as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetCardDict(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        return card != null ? DtoConverters.ToDict(card) : [];
    }

    /// <summary>
    /// Get effective card stats (base catalog stats with trait multipliers applied).
    /// </summary>
    public Godot.Collections.Dictionary GetEffectiveStatsDict(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return [];

        var cardDef = CardCatalog.GetCard(card.CatalogId);
        if (cardDef == null)
            return [];

        var effective = CardCatalog.ToDictionary(cardDef);
        var traitMods = GetTraitStatModifiersTyped(cardInstanceId);
        if (traitMods.Count == 0)
            return effective;

        foreach (var (statKey, multiplier) in traitMods)
        {
            if (multiplier <= 0f || !effective.ContainsKey(statKey))
                continue;

            var current = effective[statKey];
            if (current.VariantType != Variant.Type.Float && current.VariantType != Variant.Type.Int)
                continue;

            var baseValue = (float)current.AsDouble();
            effective[statKey] = baseValue * multiplier;
        }

        return effective;
    }

    /// <summary>Get owned cards for summoner as array for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetOwnedCardsDict(string summonerId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetOwnedCards(summonerId))
        {
            result.Add(DtoConverters.ToDict(card));
        }
        return result;
    }

    /// <summary>Grant cards from GDScript array.</summary>
    public Godot.Collections.Array<string> GrantCardsFromArray(Godot.Collections.Array<Godot.Collections.Dictionary> cardsArray)
    {
        var cards = new List<(string catalogId, string rarity)>();
        foreach (var dict in cardsArray)
        {
            if (dict.TryGetValue("catalog_id", out var catalogIdVar) &&
                dict.TryGetValue("rarity", out var rarityVar))
            {
                cards.Add((catalogIdVar.AsString(), rarityVar.AsString()));
            }
        }

        var instanceIds = GrantCards(cards);
        var result = new Godot.Collections.Array<string>();
        foreach (var id in instanceIds)
            result.Add(id);
        return result;
    }

    /// <summary>Get collection summary for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCollectionSummaryDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var entry in GetCollectionSummary())
        {
            var instancesArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var inst in entry.Instances)
                instancesArray.Add(DtoConverters.ToDict(inst));

            result.Add(new Godot.Collections.Dictionary
            {
                ["catalog_id"] = entry.CatalogId,
                ["count"] = entry.Count,
                ["rarity"] = entry.Rarity,
                ["instances"] = instancesArray
            });
        }
        return result;
    }
}
