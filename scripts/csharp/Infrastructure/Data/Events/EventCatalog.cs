using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Rewards;
using Fateforged.Domain.Progression;

namespace Fateforged.Data.Events;

/// <summary>
/// Central registry of all event definitions.
/// Provides type-safe event lookup and query methods.
/// </summary>
public static class EventCatalog
{
    // =========================================================================
    // EVENT DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<EventId, EventDefinition> _events = new()
    {
        // =====================================================================
        // TEST ARENA
        // =====================================================================

        [EventIds.ArenaEarthSprite] = new BattleEventDefinition
        {
            Id = EventIds.ArenaEarthSprite,
            NameKey = "debug.battle.arena_earth_sprite.name",
            DescriptionKey = "debug.battle.arena_earth_sprite.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = true,
            Repeatable = true,
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 4), new(CardIds.Puff, 2) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 3), new(CardIds.Puff, 2) },
            EnemyHp = 100f,
            CardXpReward = 15,
            SummonerXpReward = 20,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCardAndAddToSelectedDeck(
                EventIds.ArenaEarthSprite,
                30,
                true,
                CardIds.FireWisp,
                CardIds.Puff,
                CardIds.Pebbloom
            ),
        },

        [EventIds.ArenaPuff] = new BattleEventDefinition
        {
            Id = EventIds.ArenaPuff,
            NameKey = "debug.battle.arena_puff.name",
            DescriptionKey = "debug.battle.arena_puff.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.Puff, 4), new(CardIds.Pebbloom, 2) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 3), new(CardIds.Pebbloom, 2) },
            EnemyHp = 100f,
            FirstClearRewardOffers = BattleRewardAuthoring.AutomaticCards(
                EventIds.ArenaPuff,
                30,
                new BattleRewardCard(CardIds.Puff, "common", 1)
            ),
        },

        [EventIds.ArenaFireWisp] = new BattleEventDefinition
        {
            Id = EventIds.ArenaFireWisp,
            NameKey = "debug.battle.arena_fire_wisp.name",
            DescriptionKey = "debug.battle.arena_fire_wisp.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.FireWisp, 6) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 3), new(CardIds.Puff, 2) },
            EnemyHp = 100f,
        },

        [EventIds.ArenaCloudSwarm] = new BattleEventDefinition
        {
            Id = EventIds.ArenaCloudSwarm,
            NameKey = "debug.battle.arena_cloud_swarm.name",
            DescriptionKey = "debug.battle.arena_cloud_swarm.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.Puff, 6) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 3), new(CardIds.Pebbloom, 2) },
            EnemyHp = 100f,
        },

        [EventIds.ArenaManaBolt] = new BattleEventDefinition
        {
            Id = EventIds.ArenaManaBolt,
            NameKey = "debug.battle.arena_mana_bolt.name",
            DescriptionKey = "debug.battle.arena_mana_bolt.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardIds.ManaBolt, 5),
                new(CardIds.FireWisp, 3),
            },
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 3), new(CardIds.FireWisp, 2) },
            EnemyHp = 100f,
        },

        [EventIds.ArenaWindEarthNewCards] = new BattleEventDefinition
        {
            Id = EventIds.ArenaWindEarthNewCards,
            NameKey = "debug.battle.arena_wind_earth_new_cards.name",
            DescriptionKey = "debug.battle.arena_wind_earth_new_cards.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 4),
                new(CardIds.WindEvasionTank, 3),
                new(CardIds.WindPushbackUnit, 3),
                new(CardIds.WindCleaveUnit, 3),
                new(CardIds.EarthFlatDamageReductionTank, 3),
                new(CardIds.EarthBulletUnit, 3),
                new(CardIds.TailWind, 3),
                new(CardIds.Fortify, 3),
            },
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 4),
                new(CardIds.WindCleaveUnit, 2),
                new(CardIds.EarthBulletUnit, 2),
                new(CardIds.TailWind, 2),
                new(CardIds.Fortify, 2),
            },
            EnemyHp = 100f,
        },

        [EventIds.ArenaAllUnits] = new BattleEventDefinition
        {
            Id = EventIds.ArenaAllUnits,
            NameKey = "debug.battle.arena_all_units.name",
            DescriptionKey = "debug.battle.arena_all_units.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 0,
            RequiresDeck = false,
            Repeatable = true,
            AiType = "none",
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = BuildActiveCoreElementUnitDeck(),
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 3),
                new(CardIds.Pebbloom, 3),
                new(CardIds.Puff, 3),
                new(CardIds.WaterFrog, 3),
            },
            EnemyHp = 999999f,
        },

        [EventIds.ArenaAllCards] = new BattleEventDefinition
        {
            Id = EventIds.ArenaAllCards,
            NameKey = "debug.battle.arena_all_cards.name",
            DescriptionKey = "debug.battle.arena_all_cards.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 0,
            RequiresDeck = false,
            Repeatable = true,
            AiType = "none",
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = BuildActiveCoreElementCardDeck(),
            EnemyDeck = BuildActiveCoreElementUnitDeck(),
            EnemyHp = 999999f,
        },

        [EventIds.ArenaAllSpells] = new BattleEventDefinition
        {
            Id = EventIds.ArenaAllSpells,
            NameKey = "debug.battle.arena_all_spells.name",
            DescriptionKey = "debug.battle.arena_all_spells.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 0,
            RequiresDeck = false,
            Repeatable = true,
            AiType = "none",
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = BuildActiveCoreElementSpellTestDeck(),
            EnemyDeck = BuildRealArtSpellTargetDeck(),
            EnemyHp = 999999f,
        },

        [EventIds.ArenaSpriteUnits] = new BattleEventDefinition
        {
            Id = EventIds.ArenaSpriteUnits,
            NameKey = "debug.battle.arena_sprite_units.name",
            DescriptionKey = "debug.battle.arena_sprite_units.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 0,
            RequiresDeck = false,
            Repeatable = true,
            AiType = "none",
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = BuildRealArtDebugUnitDeck(),
            EnemyDeck = BuildRealArtDebugUnitDeck(),
            EnemyHp = 999999f,
        },

        [EventIds.DebugArena] = new BattleEventDefinition
        {
            Id = EventIds.DebugArena,
            NameKey = "debug.arena.name",
            DescriptionKey = "debug.arena.description",
            Biome = BiomeIds.SummerPlains,
            Difficulty = 0,
            RequiresDeck = false,
            Repeatable = true,
            AiType = "passive",
            RuntimeSurface = BattleRuntimeSurface.DebugArena,
            DevPlayerDeck = BuildActiveCoreElementCardDeck(),
            EnemyDeck = BuildActiveCoreElementUnitDeck(),
            EnemyHp = 999999f,
        },
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    private static List<DeckEntry> BuildActiveCoreElementUnitDeck()
    {
        return BuildActiveCoreElementDeck(CardType.Summon);
    }

    private static List<DeckEntry> BuildActiveCoreElementCardDeck()
    {
        return BuildActiveCoreElementDeck();
    }

    private static List<DeckEntry> BuildActiveCoreElementSpellTestDeck()
    {
        var deck = BuildRealArtSpellTargetDeck();
        deck.AddRange(BuildActiveCoreElementDeck(CardType.Spell));
        return deck;
    }

    private static List<DeckEntry> BuildRealArtSpellTargetDeck()
    {
        return new List<DeckEntry>
        {
            new(CardIds.FireWisp, 2),
            new(CardIds.FireWolf, 2),
            new(CardIds.WaterFrog, 2),
            new(CardIds.Pebbloom, 2),
            new(CardIds.EarthKomodoDragon, 2),
            new(CardIds.Puff, 2),
        };
    }

    private static List<DeckEntry> BuildRealArtDebugUnitDeck()
    {
        return new List<DeckEntry>
        {
            new(CardIds.FireWisp, 3),
            new(CardIds.FireWolf, 3),
            new(CardIds.WaterFrog, 3),
            new(CardIds.Pebbloom, 3),
            new(CardIds.EarthKomodoDragon, 3),
            new(CardIds.Puff, 3),
        };
    }

    private static List<DeckEntry> BuildActiveCoreElementDeck(CardType? cardType = null)
    {
        Element[] allowedElements = [Element.Fire, Element.Water, Element.Earth, Element.Wind];

        return CardCatalog
            .GetAllCards()
            .Where(card => allowedElements.Contains(card.ElementalAffinity))
            .Where(card => !cardType.HasValue || card.Type == cardType.Value)
            .Where(card => (card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) == 0)
            .OrderBy(card => GetCoreElementSortOrder(card.ElementalAffinity))
            .ThenBy(card => card.Type == CardType.Summon ? 0 : 1)
            .ThenBy(card => card.Name)
            .Select(card => new DeckEntry(card.Id))
            .ToList();
    }

    private static int GetCoreElementSortOrder(Element element)
    {
        return element switch
        {
            Element.Fire => 0,
            Element.Water => 1,
            Element.Earth => 2,
            Element.Wind => 3,
            _ => 99,
        };
    }

    /// <summary>Get an event by ID.</summary>
    public static EventDefinition? GetEvent(EventId id)
    {
        return _events.GetValueOrDefault(id);
    }

    /// <summary>Get an event by ID with specific type.</summary>
    public static T? GetEvent<T>(EventId id)
        where T : EventDefinition
    {
        return _events.GetValueOrDefault(id) as T;
    }

    /// <summary>Check if an event exists.</summary>
    public static bool HasEvent(EventId id) => _events.ContainsKey(id);

    /// <summary>Get all event IDs.</summary>
    public static EventId[] GetAllEventIds() => _events.Keys.ToArray();

    /// <summary>Get all events.</summary>
    public static EventDefinition[] GetAllEvents() => _events.Values.ToArray();

    /// <summary>Total event count.</summary>
    public static int Count => _events.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get all events of a specific type.</summary>
    public static T[] GetEventsByType<T>()
        where T : EventDefinition
    {
        return _events.Values.OfType<T>().ToArray();
    }

    /// <summary>Get all battle events (Battle, Elite, Boss).</summary>
    public static BattleEventDefinition[] GetAllBattles()
    {
        return _events.Values.Where(e => e.Type.IsCombat()).Cast<BattleEventDefinition>().ToArray();
    }

    /// <summary>Get battles by biome.</summary>
    public static BattleEventDefinition[] GetBattlesByBiome(string biomeId)
    {
        return _events
            .Values.OfType<BattleEventDefinition>()
            .Where(b => b.Biome == biomeId)
            .ToArray();
    }

    /// <summary>Get battles by difficulty range.</summary>
    public static BattleEventDefinition[] GetBattlesByDifficulty(
        int minDifficulty,
        int maxDifficulty
    )
    {
        return _events
            .Values.OfType<BattleEventDefinition>()
            .Where(b => b.Difficulty >= minDifficulty && b.Difficulty <= maxDifficulty)
            .ToArray();
    }

    /// <summary>Get all tutorial battles.</summary>
    public static BattleEventDefinition[] GetTutorialBattles()
    {
        return _events.Values.OfType<BattleEventDefinition>().Where(b => b.IsTutorial).ToArray();
    }

    /// <summary>Get all repeatable events.</summary>
    public static EventDefinition[] GetRepeatableEvents()
    {
        return _events.Values.Where(e => e.Repeatable).ToArray();
    }

    // =========================================================================
    // GDSCRIPT BRIDGE
    // =========================================================================

    /// <summary>Get event as Godot Dictionary for GDScript interop.</summary>
    public static Godot.Collections.Dictionary GetEventAsDict(EventId id)
    {
        var evt = GetEvent(id);
        if (evt == null)
            return new Godot.Collections.Dictionary();
        return ToDictionary(evt);
    }

    /// <summary>Convert EventDefinition to Godot Dictionary.</summary>
    public static Godot.Collections.Dictionary ToDictionary(EventDefinition evt)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = (string)evt.Id,
            ["type"] = evt.Type.ToStringId(),
            ["name_key"] = evt.NameKey,
            ["description_key"] = evt.DescriptionKey,
            ["repeatable"] = evt.Repeatable,
        };

        // Add type-specific fields
        switch (evt)
        {
            case BattleEventDefinition battle:
                AddBattleFields(dict, battle);
                break;
        }

        return dict;
    }

    private static void AddBattleFields(
        Godot.Collections.Dictionary dict,
        BattleEventDefinition battle
    )
    {
        dict["biome_id"] = (string)battle.Biome;
        dict["difficulty"] = battle.Difficulty;
        dict["is_tutorial"] = battle.IsTutorial;
        dict["requires_deck"] = battle.RequiresDeck;
        dict["enemy_side"] = new Godot.Collections.Dictionary
        {
            ["team"] = 1,
            ["source"] = "authored",
            ["summoner"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["id"] = "authored_enemy",
                ["display_name"] = "Authored Enemy",
                ["hp"] = battle.EnemyHp,
                ["max_hp"] = battle.EnemyHp,
                ["mana"] = 100f,
                ["max_mana"] = 100f,
                ["cast_speed"] = 1f,
                ["damage_bonus"] = 0f,
                ["damage_reduction"] = 0f,
                ["soul_strength"] = 0f,
            },
            ["deck"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["cards"] = ToDeckEntriesArray(battle.EnemyDeck),
            },
            ["controller"] = new Godot.Collections.Dictionary
            {
                ["kind"] = "trainer_ai",
                ["ai_type"] = battle.AiType,
                ["ai_difficulty"] = battle.AiDifficulty,
                ["ai_config"] = new Godot.Collections.Dictionary
                {
                    ["play_interval_min"] = battle.AiPlayIntervalMin,
                    ["play_interval_max"] = battle.AiPlayIntervalMax,
                },
            },
        };

        // Dev player deck
        if (battle.DevPlayerDeck != null)
        {
            dict["player_side"] = new Godot.Collections.Dictionary
            {
                ["team"] = 0,
                ["source"] = "profile",
                ["summoner"] = new Godot.Collections.Dictionary { ["source"] = "profile" },
                ["deck"] = new Godot.Collections.Dictionary
                {
                    ["source"] = "authored",
                    ["cards"] = ToDeckEntriesArray(battle.DevPlayerDeck),
                },
                ["controller"] = new Godot.Collections.Dictionary { ["kind"] = "player" },
            };
        }

        dict["runtime_surface"] = battle.RuntimeSurface.ToStringId();

        // Elite-specific
        if (battle is EliteEventDefinition elite && elite.LevelCap.HasValue)
        {
            dict["level_cap"] = elite.LevelCap.Value;
        }

        dict["card_xp_reward"] = battle.CardXpReward;
        dict["summoner_xp_reward"] = battle.SummonerXpReward;
        dict["first_clear_reward_offers"] = ToRewardOffersArray(battle.FirstClearRewardOffers);
    }

    private static Godot.Collections.Array ToDeckEntriesArray(IEnumerable<DeckEntry> entries)
    {
        var deck = new Godot.Collections.Array();
        foreach (var entry in entries)
        {
            deck.Add(
                new Godot.Collections.Dictionary
                {
                    ["catalog_id"] = (string)entry.CardId,
                    ["count"] = entry.Count,
                }
            );
        }
        return deck;
    }

    private static Godot.Collections.Array ToRewardOffersArray(
        IEnumerable<RewardOfferDefinition> offers
    )
    {
        var result = new Godot.Collections.Array();
        foreach (var offer in offers)
        {
            var options = new Godot.Collections.Array();
            if (offer.OptionSource is AuthoredRewardOptionSourceDefinition authored)
            {
                foreach (var option in authored.Options)
                {
                    var grants = new Godot.Collections.Array();
                    foreach (var grant in option.Grants)
                    {
                        var grantDict = new Godot.Collections.Dictionary
                        {
                            ["ownership_scope"] = grant.Target.Scope.ToString().ToLowerInvariant(),
                            ["target_id"] = grant.Target.TargetId,
                        };
                        switch (grant)
                        {
                            case CardRewardGrantDefinition card:
                                grantDict["kind"] = "card";
                                grantDict["content_id"] = card.CardId.Value;
                                grantDict["rarity"] = card.Rarity;
                                grantDict["amount"] = card.Count;
                                break;
                            case ResourceRewardGrantDefinition resource:
                                grantDict["kind"] = "resource";
                                grantDict["content_id"] = resource.ResourceId;
                                grantDict["amount"] = resource.Amount;
                                break;
                            default:
                                grantDict["kind"] = grant.GetType().Name;
                                grantDict["amount"] = 1;
                                break;
                        }
                        grants.Add(grantDict);
                    }
                    options.Add(
                        new Godot.Collections.Dictionary
                        {
                            ["id"] = option.Id.Value,
                            ["label_key"] = option.LabelKey,
                            ["description_key"] = option.DescriptionKey,
                            ["grants"] = grants,
                        }
                    );
                }
            }
            result.Add(
                new Godot.Collections.Dictionary
                {
                    ["id"] = offer.Id.Value,
                    ["selection_mode"] = offer.Selection.Mode.ToString().ToLowerInvariant(),
                    ["choose_count"] = offer.Selection.ChooseCount,
                    ["options"] = options,
                }
            );
        }
        return result;
    }

}
