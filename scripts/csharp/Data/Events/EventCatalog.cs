using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards;

namespace ProjectSummoner.Data.Events;

/// <summary>
/// Central registry of all event definitions.
/// Provides type-safe event lookup and query methods.
/// </summary>
public static class EventCatalog
{
    // =========================================================================
    // EVENT DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, EventDefinition> _events = new()
    {
        // =====================================================================
        // ACT 1: THE INITIATE'S PATH
        // =====================================================================

        [EventId.FirstTrial] = new BattleEventDefinition
        {
            Id = EventId.FirstTrial,
            NameKey = "campaign.battle.first_trial.name",
            DescriptionKey = "campaign.battle.first_trial.description",
            Position = new Vector2(100, 300),
            Biome = BiomeId.SummerPlains,
            Difficulty = 1,
            IsTutorial = true,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 1)
            },
            EnemyHp = 30f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<string> { CardId.FireWisp, CardId.Puff, CardId.Pebbloom },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 30,
                CardXpReward = 15,
                SummonerXpReward = 20
            }
        },

        [EventId.SecondChallenge] = new BattleEventDefinition
        {
            Id = EventId.SecondChallenge,
            NameKey = "campaign.battle.second_challenge.name",
            DescriptionKey = "campaign.battle.second_challenge.description",
            Position = new Vector2(250, 300),
            Biome = BiomeId.SummerPlains,
            Difficulty = 2,
            IsTutorial = true,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.Puff, 2),
                new(CardId.Pebbloom, 1)
            },
            EnemyHp = 45f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<string> { CardId.Pebbloom, CardId.Puff, CardId.FireWisp },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 40,
                CardXpReward = 18,
                SummonerXpReward = 25
            }
        },

        [EventId.Caravan01] = new CaravanEventDefinition
        {
            Id = EventId.Caravan01,
            NameKey = "campaign.event.caravan_01.name",
            DescriptionKey = "campaign.event.caravan_01.description",
            Position = new Vector2(400, 300),
            ShopId = "caravan_tutorial"
        },

        [EventId.ThirdTrial] = new BattleEventDefinition
        {
            Id = EventId.ThirdTrial,
            NameKey = "campaign.battle.third_trial.name",
            DescriptionKey = "campaign.battle.third_trial.description",
            Position = new Vector2(550, 300),
            Biome = BiomeId.SummerPlains,
            Difficulty = 3,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.Puff, 2),
                new(CardId.Pebbloom, 2)
            },
            EnemyHp = 60f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<string> { CardId.FireWisp, CardId.ManaBolt, CardId.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 50,
                CardXpReward = 20,
                SummonerXpReward = 30
            }
        },

        [EventId.PathFork] = new ChoiceEventDefinition
        {
            Id = EventId.PathFork,
            NameKey = "campaign.choice.path_fork.name",
            DescriptionKey = "campaign.choice.path_fork.description",
            Position = new Vector2(700, 300),
            Options = new List<ChoiceOption>
            {
                new("elite", "campaign.path.elite.label", "campaign.path.elite.description"),
                new("standard", "campaign.path.standard.label", "campaign.path.standard.description")
            }
        },

        [EventId.EliteBattle01] = new EliteEventDefinition
        {
            Id = EventId.EliteBattle01,
            NameKey = "campaign.battle.elite_01.name",
            DescriptionKey = "campaign.battle.elite_01.description",
            Position = new Vector2(850, 200),
            Biome = BiomeId.SummerPlains,
            Difficulty = 5,
            RequiresDeck = true,
            LevelCap = 3,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 2),
                new(CardId.Puff, 2)
            },
            EnemyHp = 80f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<string> { CardId.FireWisp, CardId.ManaBolt, CardId.Pebbloom },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 80,
                CardXpReward = 30,
                SummonerXpReward = 45
            }
        },

        [EventId.StandardBattle01] = new BattleEventDefinition
        {
            Id = EventId.StandardBattle01,
            NameKey = "campaign.battle.standard_01.name",
            DescriptionKey = "campaign.battle.standard_01.description",
            Position = new Vector2(850, 400),
            Biome = BiomeId.SummerPlains,
            Difficulty = 3,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.Pebbloom, 3)
            },
            EnemyHp = 55f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<string> { CardId.Puff, CardId.Pebbloom, CardId.FireWisp },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 50,
                CardXpReward = 22,
                SummonerXpReward = 35
            }
        },

        [EventId.Act1Boss] = new BossEventDefinition
        {
            Id = EventId.Act1Boss,
            NameKey = "campaign.battle.act1_boss.name",
            DescriptionKey = "campaign.battle.act1_boss.description",
            Position = new Vector2(1000, 300),
            Biome = BiomeId.SummerPlains,
            Difficulty = 6,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 2),
                new(CardId.Pebbloom, 2),
                new(CardId.Puff, 2)
            },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Fixed,
                FixedCards = new List<FixedRewardEntry>
                {
                    new(CardId.ManaBolt, "rare", 1)
                },
                GoldReward = 100,
                CardXpReward = 40,
                SummonerXpReward = 60
            }
        },

        // =====================================================================
        // TEST ARENA
        // =====================================================================

        [EventId.ArenaEarthSprite] = new BattleEventDefinition
        {
            Id = EventId.ArenaEarthSprite,
            NameKey = "campaign.battle.arena_earth_sprite.name",
            DescriptionKey = "campaign.battle.arena_earth_sprite.description",
            Position = new Vector2(100, 100),
            Biome = BiomeId.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardId.Pebbloom, 4),
                new(CardId.Puff, 2)
            },
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 3),
                new(CardId.Puff, 2)
            },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None }
        },

        [EventId.ArenaPuff] = new BattleEventDefinition
        {
            Id = EventId.ArenaPuff,
            NameKey = "campaign.battle.arena_puff.name",
            DescriptionKey = "campaign.battle.arena_puff.description",
            Position = new Vector2(250, 100),
            Biome = BiomeId.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardId.Puff, 4),
                new(CardId.Pebbloom, 2)
            },
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 3),
                new(CardId.Pebbloom, 2)
            },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None }
        },

        [EventId.ArenaFireWisp] = new BattleEventDefinition
        {
            Id = EventId.ArenaFireWisp,
            NameKey = "campaign.battle.arena_fire_wisp.name",
            DescriptionKey = "campaign.battle.arena_fire_wisp.description",
            Position = new Vector2(400, 100),
            Biome = BiomeId.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 6)
            },
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.Pebbloom, 3),
                new(CardId.Puff, 2)
            },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None }
        },

        [EventId.ArenaCloudSwarm] = new BattleEventDefinition
        {
            Id = EventId.ArenaCloudSwarm,
            NameKey = "campaign.battle.arena_cloud_swarm.name",
            DescriptionKey = "campaign.battle.arena_cloud_swarm.description",
            Position = new Vector2(100, 250),
            Biome = BiomeId.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardId.Puff, 6)
            },
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 3),
                new(CardId.Pebbloom, 2)
            },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None }
        },

        [EventId.ArenaManaBolt] = new BattleEventDefinition
        {
            Id = EventId.ArenaManaBolt,
            NameKey = "campaign.battle.arena_mana_bolt.name",
            DescriptionKey = "campaign.battle.arena_mana_bolt.description",
            Position = new Vector2(250, 250),
            Biome = BiomeId.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardId.ManaBolt, 5),
                new(CardId.FireWisp, 3)
            },
            EnemyDeck = new List<DeckEntry>
            {
                new(CardId.Pebbloom, 3),
                new(CardId.FireWisp, 2)
            },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None }
        },

        [EventId.DebugArena] = new BattleEventDefinition
        {
            Id = EventId.DebugArena,
            NameKey = "debug.arena.name",
            DescriptionKey = "debug.arena.description",
            Position = new Vector2(400, 250),
            Biome = BiomeId.SummerPlains,
            Difficulty = 0,
            RequiresDeck = false,
            Repeatable = true,
            AiType = "passive",
            ScenePath = "res://scenes/battlefield/dev/debug_arena.tscn",
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardId.FireWisp, 5),
                new(CardId.Puff, 5),
                new(CardId.Pebbloom, 5),
                new(CardId.ManaBolt, 5),
                new(CardId.WaterFrog, 5)
            },
            EnemyDeck = new List<DeckEntry>(),
            EnemyHp = 999999f,
            Rewards = new BattleRewardConfig { Type = RewardType.None }
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get an event by ID.</summary>
    public static EventDefinition? GetEvent(string id)
    {
        return _events.GetValueOrDefault(id);
    }

    /// <summary>Get an event by ID with specific type.</summary>
    public static T? GetEvent<T>(string id) where T : EventDefinition
    {
        return _events.GetValueOrDefault(id) as T;
    }

    /// <summary>Check if an event exists.</summary>
    public static bool HasEvent(string id) => _events.ContainsKey(id);

    /// <summary>Get all event IDs.</summary>
    public static string[] GetAllEventIds() => _events.Keys.ToArray();

    /// <summary>Get all events.</summary>
    public static EventDefinition[] GetAllEvents() => _events.Values.ToArray();

    /// <summary>Total event count.</summary>
    public static int Count => _events.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get all events of a specific type.</summary>
    public static T[] GetEventsByType<T>() where T : EventDefinition
    {
        return _events.Values.OfType<T>().ToArray();
    }

    /// <summary>Get all battle events (Battle, Elite, Boss).</summary>
    public static BattleEventDefinition[] GetAllBattles()
    {
        return _events.Values
            .Where(e => e.Type.IsCombat())
            .Cast<BattleEventDefinition>()
            .ToArray();
    }

    /// <summary>Get battles by biome.</summary>
    public static BattleEventDefinition[] GetBattlesByBiome(string biomeId)
    {
        return _events.Values
            .OfType<BattleEventDefinition>()
            .Where(b => b.Biome == biomeId)
            .ToArray();
    }

    /// <summary>Get battles by difficulty range.</summary>
    public static BattleEventDefinition[] GetBattlesByDifficulty(int minDifficulty, int maxDifficulty)
    {
        return _events.Values
            .OfType<BattleEventDefinition>()
            .Where(b => b.Difficulty >= minDifficulty && b.Difficulty <= maxDifficulty)
            .ToArray();
    }

    /// <summary>Get all tutorial battles.</summary>
    public static BattleEventDefinition[] GetTutorialBattles()
    {
        return _events.Values
            .OfType<BattleEventDefinition>()
            .Where(b => b.IsTutorial)
            .ToArray();
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
    public static Godot.Collections.Dictionary GetEventAsDict(string id)
    {
        var evt = GetEvent(id);
        if (evt == null) return new Godot.Collections.Dictionary();
        return ToDictionary(evt);
    }

    /// <summary>Convert EventDefinition to Godot Dictionary.</summary>
    public static Godot.Collections.Dictionary ToDictionary(EventDefinition evt)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = evt.Id,
            ["type"] = evt.Type.ToStringId(),
            ["name_key"] = evt.NameKey,
            ["description_key"] = evt.DescriptionKey,
            ["position"] = evt.Position,
            ["repeatable"] = evt.Repeatable
        };

        // Add type-specific fields
        switch (evt)
        {
            case BattleEventDefinition battle:
                AddBattleFields(dict, battle);
                break;
            case ChoiceEventDefinition choice:
                AddChoiceFields(dict, choice);
                break;
            case CaravanEventDefinition caravan:
                dict["shop_id"] = caravan.ShopId;
                break;
        }

        return dict;
    }

    private static void AddBattleFields(Godot.Collections.Dictionary dict, BattleEventDefinition battle)
    {
        dict["biome_id"] = battle.Biome;
        dict["difficulty"] = battle.Difficulty;
        dict["is_tutorial"] = battle.IsTutorial;
        dict["requires_deck"] = battle.RequiresDeck;
        dict["enemy_hp"] = battle.EnemyHp;
        dict["ai_type"] = battle.AiType;

        // Enemy deck
        var enemyDeck = new Godot.Collections.Array();
        foreach (var entry in battle.EnemyDeck)
        {
            enemyDeck.Add(new Godot.Collections.Dictionary
            {
                ["catalog_id"] = entry.CardId,
                ["count"] = entry.Count
            });
        }
        dict["enemy_deck"] = enemyDeck;

        // Dev player deck
        if (battle.DevPlayerDeck != null)
        {
            var devDeck = new Godot.Collections.Array();
            foreach (var entry in battle.DevPlayerDeck)
            {
                devDeck.Add(new Godot.Collections.Dictionary
                {
                    ["catalog_id"] = entry.CardId,
                    ["count"] = entry.Count
                });
            }
            dict["dev_player_deck"] = devDeck;
        }

        // Scene path
        if (!string.IsNullOrEmpty(battle.ScenePath))
        {
            dict["scene_path"] = battle.ScenePath;
        }

        // Elite-specific
        if (battle is EliteEventDefinition elite && elite.LevelCap.HasValue)
        {
            dict["level_cap"] = elite.LevelCap.Value;
        }

        // Rewards
        AddRewardFields(dict, battle.Rewards);
    }

    private static void AddRewardFields(Godot.Collections.Dictionary dict, BattleRewardConfig rewards)
    {
        dict["reward_type"] = rewards.Type.ToStringId();
        dict["gold_reward"] = rewards.GoldReward;
        dict["card_xp_reward"] = rewards.CardXpReward;
        dict["summoner_xp_reward"] = rewards.SummonerXpReward;
        dict["player_selects"] = rewards.PlayerSelects;

        if (rewards.Type == RewardType.Fixed && rewards.FixedCards.Count > 0)
        {
            var fixedCards = new Godot.Collections.Array();
            foreach (var entry in rewards.FixedCards)
            {
                fixedCards.Add(new Godot.Collections.Dictionary
                {
                    ["catalog_id"] = entry.CardId,
                    ["rarity"] = entry.Rarity,
                    ["count"] = entry.Count
                });
            }
            dict["reward_cards"] = fixedCards;
        }

        if (rewards.Type == RewardType.Flexible && rewards.CardOptions.Count > 0)
        {
            var options = new Godot.Collections.Array();
            foreach (var cardId in rewards.CardOptions)
            {
                options.Add(cardId);
            }
            dict["reward_options"] = options;
        }
    }

    private static void AddChoiceFields(Godot.Collections.Dictionary dict, ChoiceEventDefinition choice)
    {
        var options = new Godot.Collections.Array();
        foreach (var opt in choice.Options)
        {
            options.Add(new Godot.Collections.Dictionary
            {
                ["id"] = opt.Id,
                ["label_key"] = opt.LabelKey,
                ["description_key"] = opt.DescriptionKey
            });
        }
        dict["options"] = options;
    }
}
