using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Encounters;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Progression;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Deck;
using Godot;
using Godot.Collections;
using DeckModel = Fateforged.Domain.Profile.Decks.Deck;
using GdArray = Godot.Collections.Array;

namespace Fateforged.Meta.Encounters;

public sealed class BattleEncounterHandler : IEncounterExecutionHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly Func<SummonerId> _getActiveSummoner;
    private Dictionary _lastCompletionSummary = [];

    public BattleEncounterHandler(
        IProfileRepository profileRepo,
        Func<SummonerId> getActiveSummoner
    )
    {
        _profileRepo = profileRepo;
        _getActiveSummoner = getActiveSummoner;
    }

    public EncounterExecutionKind Kind => EncounterExecutionKind.Battle;

    public Dictionary GetPreparationState(EncounterDefinition encounter)
    {
        var validation = Validate(encounter);
        return new Dictionary
        {
            ["label_key"] = encounter.NameKey,
            ["role"] = encounter.Role.ToString(),
            ["deck_mode"] = encounter.Loadout.Mode.ToString(),
            ["repeatable"] = encounter.Role == EncounterRole.Practice,
            ["can_start"] = validation.IsValid,
            ["loadout"] = ToLoadout(encounter),
            ["deck_validation"] = ToValidation(validation, encounter.Loadout.Rules),
            ["battle_config"] = ToBattleConfig(encounter.BattleConfig, ResolvePlayerDeck(encounter)),
            ["reward_previews"] = new GdArray(),
            ["selected_deck"] = GetActiveDeckSummary(),
        };
    }

    public Dictionary ResolveBattleConfig(EncounterDefinition encounter)
    {
        var validation = Validate(encounter);
        return validation.IsValid
            ? ToBattleConfig(encounter.BattleConfig, ResolvePlayerDeck(encounter))
            : [];
    }

    public bool UpdateLoadout(EncounterDefinition encounter, Array<Dictionary> slots)
    {
        if (encounter.Loadout.Mode != EncounterDeckMode.Flexible)
            return false;
        var summonerId = _getActiveSummoner();
        if (!summonerId.HasValue)
            return false;
        var selected = new List<CardInstanceId>();
        foreach (var slot in slots)
        {
            var instanceId = CardInstanceId.FromString(
                slot.GetValueOrDefault("card_instance_id", "").AsString()
            );
            if (!instanceId.HasValue || selected.Contains(instanceId))
                return false;
            var card = _profileRepo.GetCard(instanceId);
            if (
                card == null
                || (card.BoundToSummonerId.HasValue && card.BoundToSummonerId != summonerId)
            )
                return false;
            selected.Add(instanceId);
        }
        var progress = _profileRepo.GetSummonerProgress(summonerId);
        progress.Quests.EncounterLoadouts[encounter.Id] = new EncounterLoadoutState
        {
            SelectedCardInstanceIds = selected,
        };
        _profileRepo.UpdateSummonerProgress(summonerId, progress);
        return true;
    }

    public Dictionary FillLoadoutFromDeck(EncounterDefinition encounter, string sourceDeckId)
    {
        if (encounter.Loadout.Mode != EncounterDeckMode.Flexible)
            return Failure("flexible_loadout_required");
        var summonerId = _getActiveSummoner();
        var sourceDeck = _profileRepo.GetDeck(DeckId.FromString(sourceDeckId));
        if (!summonerId.HasValue || sourceDeck == null || sourceDeck.SummonerId != summonerId)
            return Failure("source_deck_not_found");

        var progress = _profileRepo.GetSummonerProgress(summonerId);
        var selected = GetSelectedIds(encounter, progress).ToList();
        var selectedSet = selected.ToHashSet();
        var suppliedCount = encounter.Loadout.SuppliedCards.Sum(entry => entry.Count);
        var maxDeckSize = EffectiveMaxDeckSize(encounter.Loadout.Rules);
        var openSlots = Math.Max(0, maxDeckSize - suppliedCount - selected.Count);
        var skipped = new Array<string>();
        var copied = 0;
        foreach (var instanceId in sourceDeck.CardInstanceIds)
        {
            if (selectedSet.Contains(instanceId))
                continue;
            var card = _profileRepo.GetCard(instanceId);
            if (
                card == null
                || (card.BoundToSummonerId.HasValue && card.BoundToSummonerId != summonerId)
                || !IsCardAllowed(card.CatalogId, encounter.Loadout.Rules)
                || openSlots <= 0
            )
            {
                skipped.Add(instanceId.Value);
                continue;
            }
            selected.Add(instanceId);
            selectedSet.Add(instanceId);
            copied++;
            openSlots--;
        }
        progress.Quests.EncounterLoadouts[encounter.Id] = new EncounterLoadoutState
        {
            SelectedCardInstanceIds = selected,
        };
        _profileRepo.UpdateSummonerProgress(summonerId, progress);
        return new Dictionary
        {
            ["success"] = true,
            ["source_deck_id"] = sourceDeck.Id.Value,
            ["copied_count"] = copied,
            ["skipped_card_instance_ids"] = skipped,
            ["selected_card_instance_ids"] = new Array<string>(selected.Select(id => id.Value)),
        };
    }

    public Dictionary SaveLoadoutToDeck(
        EncounterDefinition encounter,
        string targetDeckId,
        string newDeckName
    )
    {
        if (encounter.Loadout.Mode != EncounterDeckMode.Flexible)
            return Failure("flexible_loadout_required");
        var summonerId = _getActiveSummoner();
        if (!summonerId.HasValue)
            return Failure("active_summoner_required");
        var replacing = !string.IsNullOrWhiteSpace(targetDeckId);
        DeckModel? targetDeck = null;
        if (replacing)
        {
            targetDeck = _profileRepo.GetDeck(DeckId.FromString(targetDeckId));
            if (targetDeck == null || targetDeck.SummonerId != summonerId)
                return Failure("target_deck_not_found");
        }
        else if (string.IsNullOrWhiteSpace(newDeckName))
            return Failure("deck_name_required");

        var progress = _profileRepo.GetSummonerProgress(summonerId);
        var selected = GetSelectedIds(encounter, progress)
            .Where(id => _profileRepo.GetCard(id) != null)
            .ToList();
        var selectedSet = selected.ToHashSet();
        var remaining = _profileRepo
            .ListCards()
            .Where(card =>
                !selectedSet.Contains(card.Id)
                && (!card.BoundToSummonerId.HasValue || card.BoundToSummonerId == summonerId)
            )
            .ToList();
        var omitted = new Array<string>();
        foreach (var supplied in encounter.Loadout.SuppliedCards)
        {
            for (var i = 0; i < supplied.Count; i++)
            {
                var owned = remaining.FirstOrDefault(card => card.CatalogId == supplied.CardId);
                if (owned == null)
                {
                    omitted.Add(supplied.CardId.Value);
                    continue;
                }
                selected.Add(owned.Id);
                remaining.Remove(owned);
            }
        }
        if (selected.Count > DeckService.MaxDeckSize)
            return Failure("deck_too_large");
        var id = _profileRepo.UpsertDeck(
            new DeckModel
            {
                Id = targetDeck?.Id ?? DeckId.None,
                ProfileId = _profileRepo.GetCurrentProfileId(),
                SummonerId = summonerId,
                Name = targetDeck?.Name ?? newDeckName.Trim(),
                Slot = targetDeck?.Slot ?? 0,
                IsActive = targetDeck?.IsActive ?? false,
                CardInstanceIds = selected,
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            }
        );
        return !id.HasValue
            ? Failure("save_failed")
            : new Dictionary
            {
                ["success"] = true,
                ["deck_id"] = id.Value,
                ["created"] = !replacing,
                ["omitted_supplied_card_ids"] = omitted,
            };
    }

    public Dictionary Complete(EncounterDefinition encounter, EncounterOutcome outcome)
    {
        _lastCompletionSummary = new Dictionary
        {
            ["encounter_id"] = encounter.Id,
            ["outcome"] = outcome.ToString().ToSnakeCase(),
            ["granted_rewards"] = new Array<Dictionary>(),
        };
        return (Dictionary)_lastCompletionSummary.Duplicate(true);
    }

    public Dictionary GetCompletionSummary() =>
        (Dictionary)_lastCompletionSummary.Duplicate(true);

    public Dictionary ConsumeCompletionSummary()
    {
        var result = GetCompletionSummary();
        _lastCompletionSummary = [];
        return result;
    }

    private Dictionary ToLoadout(EncounterDefinition encounter)
    {
        var selectedIds =
            encounter.Loadout.Mode == EncounterDeckMode.Owned
                ? GetActiveDeckIds()
                : GetSelectedIds(
                    encounter,
                    _profileRepo.GetSummonerProgress(_getActiveSummoner())
                );
        var selectedCards = new Array<Dictionary>();
        foreach (var instanceId in selectedIds)
        {
            var card = _profileRepo.GetCard(instanceId);
            if (card != null)
                selectedCards.Add(
                    new Dictionary
                    {
                        ["card_instance_id"] = instanceId.Value,
                        ["card_id"] = card.CatalogId.Value,
                        ["locked"] = false,
                    }
                );
        }
        var availableCards = new Array<Dictionary>();
        var summonerId = _getActiveSummoner();
        foreach (
            var card in _profileRepo
                .ListCards()
                .Where(card =>
                    !card.BoundToSummonerId.HasValue || card.BoundToSummonerId == summonerId
                )
                .OrderBy(card => card.CatalogId.Value, StringComparer.Ordinal)
        )
            availableCards.Add(
                new Dictionary
                {
                    ["card_instance_id"] = card.Id.Value,
                    ["card_id"] = card.CatalogId.Value,
                    ["selected"] = selectedIds.Contains(card.Id),
                }
            );
        return new Dictionary
        {
            ["mode"] = encounter.Loadout.Mode.ToString(),
            ["supplied_cards"] = ToDeckEntries(encounter.Loadout.SuppliedCards),
            ["selected_cards"] = selectedCards,
            ["available_cards"] = availableCards,
            ["rules"] = ToRules(encounter.Loadout.Rules),
        };
    }

    private DeckValidation Validate(EncounterDefinition encounter)
    {
        var issues = new List<DeckIssue>();
        var deck = ResolvePlayerDeck(encounter);
        if (encounter.Loadout.Mode == EncounterDeckMode.Fixed && deck.Count == 0)
            issues.Add(Issue("fixed_deck_empty"));
        if (encounter.Loadout.Mode == EncounterDeckMode.Owned && deck.Count == 0)
            issues.Add(Issue("owned_deck_required"));
        ValidateRules(deck, encounter.Loadout.Rules, issues);
        return issues.Count == 0 ? DeckValidation.Valid() : new(false, "invalid", issues);
    }

    private List<DeckEntry> ResolvePlayerDeck(EncounterDefinition encounter)
    {
        if (encounter.Loadout.Mode == EncounterDeckMode.Owned)
            return ResolveActiveDeck();
        var result = CopyEntries(encounter.Loadout.SuppliedCards);
        if (encounter.Loadout.Mode == EncounterDeckMode.Flexible)
        {
            foreach (
                var id in GetSelectedIds(
                    encounter,
                    _profileRepo.GetSummonerProgress(_getActiveSummoner())
                )
            )
            {
                var card = _profileRepo.GetCard(id);
                if (card != null)
                    AppendEntry(result, card.CatalogId, 1);
            }
        }
        return result;
    }

    private List<DeckEntry> ResolveActiveDeck()
    {
        var deckId = _profileRepo.GetProfileMetadata()?.Meta.SelectedDeck ?? "";
        var deck = _profileRepo.GetDeck(DeckId.FromString(deckId));
        var result = new List<DeckEntry>();
        if (deck == null)
            return result;
        foreach (var id in deck.CardInstanceIds)
        {
            var card = _profileRepo.GetCard(id);
            if (card != null)
                AppendEntry(result, card.CatalogId, 1);
        }
        return result;
    }

    private IReadOnlyList<CardInstanceId> GetActiveDeckIds()
    {
        var deckId = _profileRepo.GetProfileMetadata()?.Meta.SelectedDeck ?? "";
        return _profileRepo.GetDeck(DeckId.FromString(deckId))?.CardInstanceIds ?? [];
    }

    private static IReadOnlyList<CardInstanceId> GetSelectedIds(
        EncounterDefinition encounter,
        SummonerProgress progress
    ) =>
        progress.Quests.EncounterLoadouts.TryGetValue(encounter.Id, out var state)
            ? state.SelectedCardInstanceIds
            : [];

    private void ValidateRules(
        IReadOnlyList<DeckEntry> deck,
        EncounterDeckRules rules,
        List<DeckIssue> issues
    )
    {
        var total = deck.Sum(entry => entry.Count);
        var max = EffectiveMaxDeckSize(rules);
        if (total > max)
            issues.Add(Issue("max_cards", ("current", total), ("count", max)));
        var summons = CountByType(deck, CardType.Summon);
        if (summons < rules.MinSummons)
            issues.Add(Issue("min_summons", ("current", summons), ("count", rules.MinSummons)));
        var spells = CountByType(deck, CardType.Spell);
        if (spells < rules.MinSpells)
            issues.Add(Issue("min_spells", ("current", spells), ("count", rules.MinSpells)));
        foreach (var entry in deck)
        {
            if (!IsCardAllowed(entry.CardId, rules))
                issues.Add(Issue("card_not_allowed", ("card_id", entry.CardId.Value)));
        }
        var cardIds = deck.Select(entry => entry.CardId).ToHashSet();
        foreach (var required in rules.RequiredOwnedCards)
        {
            if (!cardIds.Contains(required))
                issues.Add(Issue("required_card_missing", ("card_id", required.Value)));
        }
    }

    private static bool IsCardAllowed(CardId cardId, EncounterDeckRules rules)
    {
        var card = CardCatalog.GetCard(cardId);
        return card != null
            && !rules.BannedCards.Contains(cardId)
            && (rules.AllowedCardTypes.Count == 0 || rules.AllowedCardTypes.Contains(card.Type))
            && (
                rules.AllowedElements.Count == 0
                || card.ElementalAffinity == Element.Neutral
                || rules.AllowedElements.Contains(card.ElementalAffinity)
            );
    }

    private static int CountByType(IReadOnlyList<DeckEntry> deck, CardType type) =>
        deck.Sum(entry => CardCatalog.GetCard(entry.CardId)?.Type == type ? entry.Count : 0);

    private static int EffectiveMaxDeckSize(EncounterDeckRules rules) =>
        rules.MaxDeckSize > 0
            ? Math.Min(rules.MaxDeckSize, DeckService.MaxDeckSize)
            : DeckService.MaxDeckSize;

    private static Dictionary ToBattleConfig(
        EncounterBattleConfig? config,
        IReadOnlyList<DeckEntry> playerDeck
    )
    {
        if (config == null)
            return [];
        var result = new Dictionary
        {
            ["biome_id"] = config.Biome.Value,
            ["enemy_side"] = ToEnemySide(config),
        };
        if (playerDeck.Count > 0)
            result["player_side"] = new Dictionary
            {
                ["team"] = 0,
                ["source"] = "profile",
                ["summoner"] = new Dictionary { ["source"] = "profile" },
                ["deck"] = new Dictionary
                {
                    ["source"] = "authored",
                    ["cards"] = ToDeckEntries(playerDeck),
                },
                ["controller"] = new Dictionary { ["kind"] = "player" },
            };
        return result;
    }

    private static Dictionary ToEnemySide(EncounterBattleConfig config)
    {
        var controller = new Dictionary
        {
            ["kind"] = config.EncounterAi != null ? "encounter_ai" : "trainer_ai",
            ["ai_type"] = config.AiType,
            ["ai_difficulty"] = config.AiDifficulty,
            ["ai_config"] = new Dictionary
            {
                ["play_interval_min"] = config.AiPlayIntervalMin,
                ["play_interval_max"] = config.AiPlayIntervalMax,
            },
        };
        if (config.EncounterAi != null)
            controller["encounter_ai"] = ToEncounterAi(config.EncounterAi);
        return new Dictionary
        {
            ["team"] = 1,
            ["source"] = "authored",
            ["summoner"] = new Dictionary
            {
                ["source"] = "authored",
                ["id"] = "academy_opponent",
                ["display_name"] = "Academy Opponent",
                ["hp"] = config.EnemyHp,
                ["max_hp"] = config.EnemyHp,
                ["mana"] = 100f,
                ["max_mana"] = 100f,
                ["cast_speed"] = 1f,
                ["damage_bonus"] = 0f,
                ["damage_reduction"] = 0f,
                ["soul_strength"] = 0f,
            },
            ["deck"] = new Dictionary
            {
                ["source"] = "authored",
                ["deferred"] = config.EnemyDeck.Count == 0 && config.EncounterAi != null,
                ["cards"] = ToDeckEntries(config.EnemyDeck),
            },
            ["controller"] = controller,
        };
    }

    private static Dictionary ToEncounterAi(EncounterAiConfig config)
    {
        var result = new Dictionary
        {
            ["preset"] = config.Preset,
            ["team"] = config.Team,
            ["rules"] = ToRulesArray(config.Rules),
        };
        if (config.UseTrainerAi.HasValue)
            result["use_trainer_ai"] = config.UseTrainerAi.Value;
        return result;
    }

    private static GdArray ToRulesArray(IEnumerable<EncounterRule> rules)
    {
        var result = new GdArray();
        foreach (var rule in rules)
        {
            var dict = new Dictionary
            {
                ["id"] = rule.Id,
                ["kind"] = rule.Kind,
                ["enabled"] = rule.Enabled,
                ["start_time"] = rule.StartTime,
                ["rhythm"] = rule.Rhythm,
                ["placement"] = rule.Placement,
                ["source"] = rule.Source,
                ["actions"] = ToActionArray(rule.Actions),
            };
            if (rule.EndTime.HasValue)
                dict["end_time"] = rule.EndTime.Value;
            if (rule.IntervalSeconds.HasValue)
                dict["interval_seconds"] = rule.IntervalSeconds.Value;
            if (rule.MaxExecutions.HasValue)
                dict["max_executions"] = rule.MaxExecutions.Value;
            if (rule.MaxAlive.HasValue)
                dict["max_alive"] = rule.MaxAlive.Value;
            if (rule.CardPool.Count > 0)
                dict["card_pool"] = ToCardIds(rule.CardPool);
            AddAiFields(dict, rule.AiType, rule.AiPersonality, rule.AiPlayIntervalMin, rule.AiPlayIntervalMax);
            result.Add(dict);
        }
        return result;
    }

    private static GdArray ToActionArray(IEnumerable<EncounterAction> actions)
    {
        var result = new GdArray();
        foreach (var action in actions)
        {
            var dict = new Dictionary
            {
                ["kind"] = action.Kind,
                ["source"] = action.Source,
                ["team"] = action.Team,
                ["placement"] = action.Placement,
                ["activate_immediately"] = action.ActivateImmediately,
                ["allow_when_overwhelmed"] = action.AllowWhenOverwhelmed,
                ["ignore_caps"] = action.IgnoreCaps,
                ["rule_id"] = action.RuleId,
                ["enabled"] = action.Enabled,
            };
            if (action.CardId.HasValue)
                dict["card_id"] = action.CardId.Value;
            if (action.CardIds.Count > 0)
                dict["card_ids"] = ToCardIds(action.CardIds);
            if (action.Position.HasValue)
                dict["position"] = ToPosition(action.Position.Value);
            if (action.Positions.Count > 0)
                dict["positions"] = ToPositions(action.Positions);
            AddAiFields(dict, action.AiType, action.AiPersonality, action.AiPlayIntervalMin, action.AiPlayIntervalMax);
            result.Add(dict);
        }
        return result;
    }

    private static void AddAiFields(Dictionary dict, string? type, string? personality, float? min, float? max)
    {
        if (!string.IsNullOrWhiteSpace(type))
            dict["ai_type"] = type;
        if (!string.IsNullOrWhiteSpace(personality))
            dict["ai_personality"] = personality;
        if (min.HasValue || max.HasValue)
        {
            var config = new Dictionary();
            if (min.HasValue)
                config["play_interval_min"] = min.Value;
            if (max.HasValue)
                config["play_interval_max"] = max.Value;
            dict["ai_config"] = config;
        }
    }

    private static Dictionary ToRules(EncounterDeckRules rules) =>
        new()
        {
            ["has_rules"] = rules.HasRules,
            ["allowed_card_types"] = new Array<string>(rules.AllowedCardTypes.Select(v => v.ToString())),
            ["allowed_elements"] = new Array<string>(rules.AllowedElements.Select(v => v.ToString())),
            ["min_summons"] = rules.MinSummons,
            ["min_spells"] = rules.MinSpells,
            ["max_deck_size"] = rules.MaxDeckSize,
            ["required_owned_cards"] = ToCardIds(rules.RequiredOwnedCards),
            ["banned_cards"] = ToCardIds(rules.BannedCards),
        };

    private static Dictionary ToValidation(DeckValidation validation, EncounterDeckRules rules) =>
        new()
        {
            ["is_valid"] = validation.IsValid,
            ["status"] = validation.Status,
            ["issues"] = new GdArray(
                validation.Issues.Select(issue => Variant.From(new Dictionary
                {
                    ["code"] = issue.Code,
                    ["arguments"] = issue.Arguments,
                }))
            ),
            ["has_rules"] = rules.HasRules,
        };

    private Dictionary GetActiveDeckSummary()
    {
        var id = _profileRepo.GetProfileMetadata()?.Meta.SelectedDeck ?? "";
        var deck = _profileRepo.GetDeck(DeckId.FromString(id));
        return new Dictionary
        {
            ["id"] = id,
            ["name"] = deck?.Name ?? "",
            ["card_count"] = deck?.CardInstanceIds.Count ?? 0,
        };
    }

    private static GdArray ToDeckEntries(IEnumerable<DeckEntry> entries)
    {
        var result = new GdArray();
        foreach (var entry in entries)
            result.Add(new Dictionary { ["catalog_id"] = entry.CardId.Value, ["count"] = entry.Count });
        return result;
    }

    private static GdArray ToCardIds(IEnumerable<CardId> ids) =>
        new(ids.Select(id => Variant.From(id.Value)));

    private static Dictionary ToPosition(EncounterPosition value) =>
        new() { ["x"] = value.X, ["z"] = value.Z };

    private static GdArray ToPositions(IEnumerable<EncounterPosition> values) =>
        new(values.Select(value => Variant.From(ToPosition(value))));

    private static List<DeckEntry> CopyEntries(IEnumerable<DeckEntry> entries)
    {
        var result = new List<DeckEntry>();
        foreach (var entry in entries)
            AppendEntry(result, entry.CardId, entry.Count);
        return result;
    }

    private static void AppendEntry(List<DeckEntry> entries, CardId cardId, int count)
    {
        if (!cardId.HasValue || count <= 0)
            return;
        var index = entries.FindIndex(entry => entry.CardId == cardId);
        if (index < 0)
            entries.Add(new DeckEntry(cardId, count));
        else
            entries[index] = new DeckEntry(cardId, entries[index].Count + count);
    }

    private static Dictionary Failure(string error) =>
        new() { ["success"] = false, ["error"] = error };

    private sealed record DeckValidation(bool IsValid, string Status, IReadOnlyList<DeckIssue> Issues)
    {
        public static DeckValidation Valid() => new(true, "valid", []);
    }

    private sealed record DeckIssue(string Code, Dictionary Arguments);

    private static DeckIssue Issue(string code, params (string Key, Variant Value)[] values)
    {
        var arguments = new Dictionary();
        foreach (var (key, value) in values)
            arguments[key] = value;
        return new DeckIssue(code, arguments);
    }
}
