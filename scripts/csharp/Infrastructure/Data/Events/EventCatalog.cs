using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Shop;
using Godot;

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
        // ACT 1: THE INITIATE'S PATH
        // =====================================================================

        [EventIds.FirstTrial] = new BattleEventDefinition
        {
            Id = EventIds.FirstTrial,
            NameKey = "campaign.battle.first_trial.name",
            DescriptionKey = "campaign.battle.first_trial.description",
            Position = new Vector2(100, 320),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            IsTutorial = true,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 1) },
            EnemyHp = 20f,
            AiType = "passive",
            AiDifficulty = 0,
            AiPlayIntervalMin = 999f,
            AiPlayIntervalMax = 999f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.FireWisp, CardIds.Puff, CardIds.Pebbloom },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 30,
                CardXpReward = 15,
                SummonerXpReward = 20,
            },
        },

        [EventIds.SecondChallenge] = new BattleEventDefinition
        {
            Id = EventIds.SecondChallenge,
            NameKey = "campaign.battle.second_challenge.name",
            DescriptionKey = "campaign.battle.second_challenge.description",
            Position = new Vector2(230, 250),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 2,
            IsTutorial = true,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 1), new(CardIds.Puff, 1) },
            EnemyHp = 40f,
            AiType = "simple",
            AiDifficulty = 1,
            AiPlayIntervalMin = 7.0f,
            AiPlayIntervalMax = 10.0f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.Pebbloom, CardIds.Puff, CardIds.FireWisp },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 40,
                CardXpReward = 18,
                SummonerXpReward = 25,
            },
        },

        [EventIds.OpeningDoctrine] = new ChoiceEventDefinition
        {
            Id = EventIds.OpeningDoctrine,
            NameKey = "campaign.choice.opening_doctrine.name",
            DescriptionKey = "campaign.choice.opening_doctrine.description",
            Position = new Vector2(360, 300),
            Options = new List<ChoiceOption>
            {
                new(
                    ChoiceIds.Aggressive,
                    "campaign.path.aggressive.label",
                    "campaign.path.aggressive.description"
                ),
                new(
                    ChoiceIds.Prepared,
                    "campaign.path.prepared.label",
                    "campaign.path.prepared.description"
                ),
                new(
                    ChoiceIds.Insight,
                    "campaign.path.insight.label",
                    "campaign.path.insight.description"
                ),
            },
        },

        [EventIds.AggressivePush] = new BattleEventDefinition
        {
            Id = EventIds.AggressivePush,
            NameKey = "campaign.battle.aggressive_push.name",
            DescriptionKey = "campaign.battle.aggressive_push.description",
            Position = new Vector2(500, 180),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 3,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 2), new(CardIds.Puff, 1) },
            EnemyHp = 55f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.FireWisp, CardIds.ManaBolt, CardIds.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 45,
                CardXpReward = 20,
                SummonerXpReward = 28,
            },
        },

        [EventIds.ScoutSkirmish] = new BattleEventDefinition
        {
            Id = EventIds.ScoutSkirmish,
            NameKey = "campaign.battle.scout_skirmish.name",
            DescriptionKey = "campaign.battle.scout_skirmish.description",
            Position = new Vector2(530, 300),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 3,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.Puff, 2), new(CardIds.FireWisp, 1) },
            EnemyHp = 58f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.Puff, CardIds.Pebbloom, CardIds.ManaBolt },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 45,
                CardXpReward = 20,
                SummonerXpReward = 28,
            },
        },

        [EventIds.Caravan01] = new CaravanEventDefinition
        {
            Id = EventIds.Caravan01,
            NameKey = "campaign.event.caravan_01.name",
            DescriptionKey = "campaign.event.caravan_01.description",
            Position = new Vector2(560, 460),
            ShopId = ShopIds.CaravanTutorial,
        },

        [EventIds.StabilityLine] = new BattleEventDefinition
        {
            Id = EventIds.StabilityLine,
            NameKey = "campaign.battle.stability_line.name",
            DescriptionKey = "campaign.battle.stability_line.description",
            Position = new Vector2(720, 380),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 4,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 2), new(CardIds.Puff, 2) },
            EnemyHp = 65f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.Pebbloom, CardIds.Puff, CardIds.FireWisp },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 50,
                CardXpReward = 22,
                SummonerXpReward = 32,
            },
        },

        [EventIds.ThirdTrial] = new BattleEventDefinition
        {
            Id = EventIds.ThirdTrial,
            NameKey = "campaign.battle.third_trial.name",
            DescriptionKey = "campaign.battle.third_trial.description",
            Position = new Vector2(860, 255),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 5,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.Pebbloom, 2),
                new(CardIds.Puff, 2),
                new(CardIds.FireWisp, 1),
            },
            EnemyHp = 78f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.FireWisp,
                    CardIds.Pebbloom,
                    CardIds.ManaBolt,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 55,
                CardXpReward = 24,
                SummonerXpReward = 36,
            },
        },

        [EventIds.MidlineTrial] = new BattleEventDefinition
        {
            Id = EventIds.MidlineTrial,
            NameKey = "campaign.battle.midline_trial.name",
            DescriptionKey = "campaign.battle.midline_trial.description",
            Position = new Vector2(1000, 330),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 5,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 2),
                new(CardIds.Pebbloom, 2),
                new(CardIds.Puff, 1),
            },
            EnemyHp = 75f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.FireWisp,
                    CardIds.ManaBolt,
                    CardIds.Pebbloom,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 55,
                CardXpReward = 24,
                SummonerXpReward = 36,
            },
        },

        [EventIds.RouteChoice] = new ChoiceEventDefinition
        {
            Id = EventIds.RouteChoice,
            NameKey = "campaign.choice.route_choice.name",
            DescriptionKey = "campaign.choice.route_choice.description",
            Position = new Vector2(1150, 260),
            Options = new List<ChoiceOption>
            {
                new(
                    ChoiceIds.Ridge,
                    "campaign.path.ridge.label",
                    "campaign.path.ridge.description"
                ),
                new(
                    ChoiceIds.River,
                    "campaign.path.river.label",
                    "campaign.path.river.description"
                ),
                new(
                    ChoiceIds.Grove,
                    "campaign.path.grove.label",
                    "campaign.path.grove.description"
                ),
            },
        },

        [EventIds.RidgeAssault] = new BattleEventDefinition
        {
            Id = EventIds.RidgeAssault,
            NameKey = "campaign.battle.ridge_assault.name",
            DescriptionKey = "campaign.battle.ridge_assault.description",
            Position = new Vector2(1300, 120),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 7,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 3), new(CardIds.Puff, 2) },
            EnemyHp = 105f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.FireWisp, CardIds.ManaBolt, CardIds.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 70,
                CardXpReward = 28,
                SummonerXpReward = 46,
            },
        },

        [EventIds.RiverHold] = new BattleEventDefinition
        {
            Id = EventIds.RiverHold,
            NameKey = "campaign.battle.river_hold.name",
            DescriptionKey = "campaign.battle.river_hold.description",
            Position = new Vector2(1320, 420),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 5,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 3), new(CardIds.Puff, 2) },
            EnemyHp = 90f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.Pebbloom, CardIds.Puff, CardIds.FireWisp },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 60,
                CardXpReward = 24,
                SummonerXpReward = 38,
            },
        },

        [EventIds.GrovePatrol] = new BattleEventDefinition
        {
            Id = EventIds.GrovePatrol,
            NameKey = "campaign.battle.grove_patrol.name",
            DescriptionKey = "campaign.battle.grove_patrol.description",
            Position = new Vector2(1360, 280),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 6,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 3), new(CardIds.FireWisp, 2) },
            EnemyHp = 98f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.Pebbloom, CardIds.FireWisp, CardIds.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 65,
                CardXpReward = 26,
                SummonerXpReward = 42,
            },
        },

        [EventIds.Caravan02] = new CaravanEventDefinition
        {
            Id = EventIds.Caravan02,
            NameKey = "campaign.event.caravan_02.name",
            DescriptionKey = "campaign.event.caravan_02.description",
            Position = new Vector2(1500, 350),
            ShopId = ShopIds.CaravanTutorial,
        },

        [EventIds.Chokepoint] = new BattleEventDefinition
        {
            Id = EventIds.Chokepoint,
            NameKey = "campaign.battle.chokepoint.name",
            DescriptionKey = "campaign.battle.chokepoint.description",
            Position = new Vector2(1660, 220),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 7,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 3),
                new(CardIds.Pebbloom, 2),
                new(CardIds.Puff, 2),
            },
            EnemyHp = 110f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.ManaBolt,
                    CardIds.FireWisp,
                    CardIds.Pebbloom,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 70,
                CardXpReward = 30,
                SummonerXpReward = 48,
            },
        },

        [EventIds.Gatekeeper] = new BossEventDefinition
        {
            Id = EventIds.Gatekeeper,
            NameKey = "campaign.battle.gatekeeper.name",
            DescriptionKey = "campaign.battle.gatekeeper.description",
            Position = new Vector2(1810, 340),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 8,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 3),
                new(CardIds.Pebbloom, 3),
                new(CardIds.Puff, 2),
            },
            EnemyHp = 125f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Fixed,
                FixedCards = new List<FixedRewardEntry> { new(CardIds.Charge, "common", 1) },
                GoldReward = 80,
                CardXpReward = 35,
                SummonerXpReward = 55,
            },
        },

        [EventIds.PathFork] = new ChoiceEventDefinition
        {
            Id = EventIds.PathFork,
            NameKey = "campaign.choice.path_fork.name",
            DescriptionKey = "campaign.choice.path_fork.description",
            Position = new Vector2(1960, 260),
            Options = new List<ChoiceOption>
            {
                new(
                    ChoiceIds.Elite,
                    "campaign.path.elite.label",
                    "campaign.path.elite.description"
                ),
                new(
                    ChoiceIds.Standard,
                    "campaign.path.standard.label",
                    "campaign.path.standard.description"
                ),
                new(
                    ChoiceIds.Gambit,
                    "campaign.path.gambit.label",
                    "campaign.path.gambit.description"
                ),
            },
        },

        [EventIds.EliteBattle01] = new EliteEventDefinition
        {
            Id = EventIds.EliteBattle01,
            NameKey = "campaign.battle.elite_01.name",
            DescriptionKey = "campaign.battle.elite_01.description",
            Position = new Vector2(1890, 155),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 9,
            RequiresDeck = true,
            LevelCap = 4,
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 4), new(CardIds.Puff, 3) },
            EnemyHp = 140f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.ManaBolt,
                    CardIds.FireWisp,
                    CardIds.Pebbloom,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 95,
                CardXpReward = 38,
                SummonerXpReward = 60,
            },
        },

        [EventIds.EliteBattle02] = new EliteEventDefinition
        {
            Id = EventIds.EliteBattle02,
            NameKey = "campaign.battle.elite_02.name",
            DescriptionKey = "campaign.battle.elite_02.description",
            Position = new Vector2(2030, 235),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 10,
            RequiresDeck = true,
            LevelCap = 5,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 4),
                new(CardIds.Pebbloom, 3),
                new(CardIds.Puff, 3),
            },
            EnemyHp = 155f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.ManaBolt, CardIds.Pebbloom, CardIds.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 105,
                CardXpReward = 42,
                SummonerXpReward = 66,
            },
        },

        [EventIds.EliteBattle03] = new EliteEventDefinition
        {
            Id = EventIds.EliteBattle03,
            NameKey = "campaign.battle.elite_03.name",
            DescriptionKey = "campaign.battle.elite_03.description",
            Position = new Vector2(2430, 120),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 11,
            RequiresDeck = true,
            LevelCap = 6,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 5),
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 3),
            },
            EnemyHp = 170f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.ManaBolt, CardIds.FireWisp, CardIds.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 115,
                CardXpReward = 46,
                SummonerXpReward = 72,
            },
        },

        [EventIds.EliteBattle04] = new EliteEventDefinition
        {
            Id = EventIds.EliteBattle04,
            NameKey = "campaign.battle.elite_04.name",
            DescriptionKey = "campaign.battle.elite_04.description",
            Position = new Vector2(2590, 210),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 12,
            RequiresDeck = true,
            LevelCap = 7,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 6),
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 4),
            },
            EnemyHp = 185f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.ManaBolt,
                    CardIds.FireWisp,
                    CardIds.Pebbloom,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 125,
                CardXpReward = 50,
                SummonerXpReward = 80,
            },
        },

        [EventIds.StandardBattle01] = new BattleEventDefinition
        {
            Id = EventIds.StandardBattle01,
            NameKey = "campaign.battle.standard_01.name",
            DescriptionKey = "campaign.battle.standard_01.description",
            Position = new Vector2(2130, 440),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 8,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 4), new(CardIds.Puff, 2) },
            EnemyHp = 120f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.Puff, CardIds.Pebbloom, CardIds.FireWisp },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 75,
                CardXpReward = 32,
                SummonerXpReward = 52,
            },
        },

        [EventIds.Caravan03] = new CaravanEventDefinition
        {
            Id = EventIds.Caravan03,
            NameKey = "campaign.event.caravan_03.name",
            DescriptionKey = "campaign.event.caravan_03.description",
            Position = new Vector2(2280, 530),
            ShopId = ShopIds.CaravanTutorial,
        },

        [EventIds.StandardBattle02] = new BattleEventDefinition
        {
            Id = EventIds.StandardBattle02,
            NameKey = "campaign.battle.standard_02.name",
            DescriptionKey = "campaign.battle.standard_02.description",
            Position = new Vector2(2440, 470),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 9,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 3),
                new(CardIds.FireWisp, 2),
            },
            EnemyHp = 135f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.Pebbloom, CardIds.Puff, CardIds.ManaBolt },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 85,
                CardXpReward = 36,
                SummonerXpReward = 58,
            },
        },

        [EventIds.StandardBattle03] = new BattleEventDefinition
        {
            Id = EventIds.StandardBattle03,
            NameKey = "campaign.battle.standard_03.name",
            DescriptionKey = "campaign.battle.standard_03.description",
            Position = new Vector2(2600, 410),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 10,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 4),
                new(CardIds.FireWisp, 2),
            },
            EnemyHp = 150f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.Pebbloom,
                    CardIds.FireWisp,
                    CardIds.ManaBolt,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 95,
                CardXpReward = 40,
                SummonerXpReward = 64,
            },
        },

        [EventIds.StandardBattle04] = new BattleEventDefinition
        {
            Id = EventIds.StandardBattle04,
            NameKey = "campaign.battle.standard_04.name",
            DescriptionKey = "campaign.battle.standard_04.description",
            Position = new Vector2(2760, 470),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 11,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.Pebbloom, 5),
                new(CardIds.Puff, 4),
                new(CardIds.FireWisp, 3),
            },
            EnemyHp = 168f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.Pebbloom,
                    CardIds.FireWisp,
                    CardIds.ManaBolt,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 105,
                CardXpReward = 44,
                SummonerXpReward = 72,
            },
        },

        [EventIds.GambitBattle01] = new BattleEventDefinition
        {
            Id = EventIds.GambitBattle01,
            NameKey = "campaign.battle.gambit_01.name",
            DescriptionKey = "campaign.battle.gambit_01.description",
            Position = new Vector2(2140, 280),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 8,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 3),
                new(CardIds.Pebbloom, 3),
                new(CardIds.Puff, 2),
            },
            EnemyHp = 135f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.ManaBolt, CardIds.FireWisp, CardIds.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 80,
                CardXpReward = 34,
                SummonerXpReward = 54,
            },
        },

        [EventIds.GambitBattle02] = new BattleEventDefinition
        {
            Id = EventIds.GambitBattle02,
            NameKey = "campaign.battle.gambit_02.name",
            DescriptionKey = "campaign.battle.gambit_02.description",
            Position = new Vector2(2310, 330),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 12,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 4),
                new(CardIds.Pebbloom, 3),
                new(CardIds.Puff, 3),
            },
            EnemyHp = 175f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.Pebbloom,
                    CardIds.FireWisp,
                    CardIds.ManaBolt,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 120,
                CardXpReward = 46,
                SummonerXpReward = 72,
            },
        },

        [EventIds.GambitBattle03] = new BattleEventDefinition
        {
            Id = EventIds.GambitBattle03,
            NameKey = "campaign.battle.gambit_03.name",
            DescriptionKey = "campaign.battle.gambit_03.description",
            Position = new Vector2(2480, 280),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 10,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 4),
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 3),
            },
            EnemyHp = 160f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.ManaBolt, CardIds.Puff, CardIds.FireWisp },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 88,
                CardXpReward = 38,
                SummonerXpReward = 62,
            },
        },

        [EventIds.GambitBattle04] = new BattleEventDefinition
        {
            Id = EventIds.GambitBattle04,
            NameKey = "campaign.battle.gambit_04.name",
            DescriptionKey = "campaign.battle.gambit_04.description",
            Position = new Vector2(2650, 330),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 13,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 5),
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 4),
            },
            EnemyHp = 200f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.FireWisp,
                    CardIds.ManaBolt,
                    CardIds.Pebbloom,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 130,
                CardXpReward = 52,
                SummonerXpReward = 84,
            },
        },

        [EventIds.RejoinTrial] = new BattleEventDefinition
        {
            Id = EventIds.RejoinTrial,
            NameKey = "campaign.battle.rejoin_trial.name",
            DescriptionKey = "campaign.battle.rejoin_trial.description",
            Position = new Vector2(2920, 300),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 12,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 4),
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 4),
            },
            EnemyHp = 165f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.FireWisp,
                    CardIds.Pebbloom,
                    CardIds.ManaBolt,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 120,
                CardXpReward = 52,
                SummonerXpReward = 82,
            },
        },

        [EventIds.FinalAnte] = new BattleEventDefinition
        {
            Id = EventIds.FinalAnte,
            NameKey = "campaign.battle.final_ante.name",
            DescriptionKey = "campaign.battle.final_ante.description",
            Position = new Vector2(3080, 210),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 13,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 5),
                new(CardIds.Pebbloom, 4),
                new(CardIds.Puff, 4),
            },
            EnemyHp = 180f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId> { CardIds.ManaBolt, CardIds.FireWisp, CardIds.Puff },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 130,
                CardXpReward = 56,
                SummonerXpReward = 88,
            },
        },

        [EventIds.StormBreaker] = new BattleEventDefinition
        {
            Id = EventIds.StormBreaker,
            NameKey = "campaign.battle.storm_breaker.name",
            DescriptionKey = "campaign.battle.storm_breaker.description",
            Position = new Vector2(3240, 320),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 14,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 6),
                new(CardIds.Pebbloom, 5),
                new(CardIds.Puff, 5),
            },
            EnemyHp = 205f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Flexible,
                CardOptions = new List<CardId>
                {
                    CardIds.ManaBolt,
                    CardIds.Pebbloom,
                    CardIds.FireWisp,
                },
                PlayerSelects = true,
                ExcludeOwned = true,
                GoldReward = 140,
                CardXpReward = 60,
                SummonerXpReward = 95,
            },
        },

        [EventIds.Act1Boss] = new BossEventDefinition
        {
            Id = EventIds.Act1Boss,
            NameKey = "campaign.battle.act1_boss.name",
            DescriptionKey = "campaign.battle.act1_boss.description",
            Position = new Vector2(3420, 260),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 15,
            RequiresDeck = true,
            EnemyDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 7),
                new(CardIds.Pebbloom, 6),
                new(CardIds.Puff, 6),
            },
            EnemyHp = 240f,
            Rewards = new BattleRewardConfig
            {
                Type = RewardType.Fixed,
                FixedCards = new List<FixedRewardEntry> { new(CardIds.ManaBolt, "rare", 1) },
                GoldReward = 180,
                CardXpReward = 70,
                SummonerXpReward = 120,
            },
        },

        // =====================================================================
        // TEST ARENA
        // =====================================================================

        [EventIds.ArenaEarthSprite] = new BattleEventDefinition
        {
            Id = EventIds.ArenaEarthSprite,
            NameKey = "campaign.battle.arena_earth_sprite.name",
            DescriptionKey = "campaign.battle.arena_earth_sprite.description",
            Position = new Vector2(100, 100),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 4), new(CardIds.Puff, 2) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 3), new(CardIds.Puff, 2) },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None },
        },

        [EventIds.ArenaPuff] = new BattleEventDefinition
        {
            Id = EventIds.ArenaPuff,
            NameKey = "campaign.battle.arena_puff.name",
            DescriptionKey = "campaign.battle.arena_puff.description",
            Position = new Vector2(250, 100),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.Puff, 4), new(CardIds.Pebbloom, 2) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 3), new(CardIds.Pebbloom, 2) },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None },
        },

        [EventIds.ArenaFireWisp] = new BattleEventDefinition
        {
            Id = EventIds.ArenaFireWisp,
            NameKey = "campaign.battle.arena_fire_wisp.name",
            DescriptionKey = "campaign.battle.arena_fire_wisp.description",
            Position = new Vector2(400, 100),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.FireWisp, 6) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 3), new(CardIds.Puff, 2) },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None },
        },

        [EventIds.ArenaCloudSwarm] = new BattleEventDefinition
        {
            Id = EventIds.ArenaCloudSwarm,
            NameKey = "campaign.battle.arena_cloud_swarm.name",
            DescriptionKey = "campaign.battle.arena_cloud_swarm.description",
            Position = new Vector2(100, 250),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry> { new(CardIds.Puff, 6) },
            EnemyDeck = new List<DeckEntry> { new(CardIds.FireWisp, 3), new(CardIds.Pebbloom, 2) },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None },
        },

        [EventIds.ArenaManaBolt] = new BattleEventDefinition
        {
            Id = EventIds.ArenaManaBolt,
            NameKey = "campaign.battle.arena_mana_bolt.name",
            DescriptionKey = "campaign.battle.arena_mana_bolt.description",
            Position = new Vector2(250, 250),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardIds.ManaBolt, 5),
                new(CardIds.FireWisp, 3),
            },
            EnemyDeck = new List<DeckEntry> { new(CardIds.Pebbloom, 3), new(CardIds.FireWisp, 2) },
            EnemyHp = 100f,
            Rewards = new BattleRewardConfig { Type = RewardType.None },
        },

        [EventIds.ArenaWindEarthNewCards] = new BattleEventDefinition
        {
            Id = EventIds.ArenaWindEarthNewCards,
            NameKey = "campaign.battle.arena_wind_earth_new_cards.name",
            DescriptionKey = "campaign.battle.arena_wind_earth_new_cards.description",
            Position = new Vector2(550, 250),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 1,
            RequiresDeck = false,
            Repeatable = true,
            ScenePath = "res://scenes/battle/battlefield/dev/debug_arena.tscn",
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
            Rewards = new BattleRewardConfig { Type = RewardType.None },
        },

        [EventIds.DebugArena] = new BattleEventDefinition
        {
            Id = EventIds.DebugArena,
            NameKey = "debug.arena.name",
            DescriptionKey = "debug.arena.description",
            Position = new Vector2(400, 250),
            Biome = BiomeIds.SummerPlains,
            Difficulty = 0,
            RequiresDeck = false,
            Repeatable = true,
            AiType = "passive",
            ScenePath = "res://scenes/battle/battlefield/dev/debug_arena.tscn",
            DevPlayerDeck = new List<DeckEntry>
            {
                new(CardIds.FireWisp, 5),
                new(CardIds.Puff, 5),
                new(CardIds.Pebbloom, 5),
                new(CardIds.ManaBolt, 5),
                new(CardIds.WaterFrog, 5),
            },
            EnemyDeck = new List<DeckEntry>(),
            EnemyHp = 999999f,
            Rewards = new BattleRewardConfig { Type = RewardType.None },
        },
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

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
            ["position"] = evt.Position,
            ["repeatable"] = evt.Repeatable,
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
                dict["shop_id"] = (string)caravan.ShopId;
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
        dict["enemy_hp"] = battle.EnemyHp;
        dict["ai_type"] = battle.AiType;
        dict["ai_difficulty"] = battle.AiDifficulty;
        dict["ai_config"] = new Godot.Collections.Dictionary
        {
            ["play_interval_min"] = battle.AiPlayIntervalMin,
            ["play_interval_max"] = battle.AiPlayIntervalMax,
        };

        // Enemy deck
        var enemyDeck = new Godot.Collections.Array();
        foreach (var entry in battle.EnemyDeck)
        {
            enemyDeck.Add(
                new Godot.Collections.Dictionary
                {
                    ["catalog_id"] = (string)entry.CardId,
                    ["count"] = entry.Count,
                }
            );
        }
        dict["enemy_deck"] = enemyDeck;

        // Dev player deck
        if (battle.DevPlayerDeck != null)
        {
            var devDeck = new Godot.Collections.Array();
            foreach (var entry in battle.DevPlayerDeck)
            {
                devDeck.Add(
                    new Godot.Collections.Dictionary
                    {
                        ["catalog_id"] = (string)entry.CardId,
                        ["count"] = entry.Count,
                    }
                );
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

    private static void AddRewardFields(
        Godot.Collections.Dictionary dict,
        BattleRewardConfig rewards
    )
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
                fixedCards.Add(
                    new Godot.Collections.Dictionary
                    {
                        ["catalog_id"] = (string)entry.CardId,
                        ["rarity"] = entry.Rarity,
                        ["count"] = entry.Count,
                    }
                );
            }
            dict["reward_cards"] = fixedCards;
        }

        if (rewards.Type == RewardType.Flexible && rewards.CardOptions.Count > 0)
        {
            var options = new Godot.Collections.Array();
            foreach (var cardId in rewards.CardOptions)
            {
                options.Add((string)cardId);
            }
            dict["reward_options"] = options;
        }
    }

    private static void AddChoiceFields(
        Godot.Collections.Dictionary dict,
        ChoiceEventDefinition choice
    )
    {
        var options = new Godot.Collections.Array();
        foreach (var opt in choice.Options)
        {
            options.Add(
                new Godot.Collections.Dictionary
                {
                    ["id"] = (string)opt.Id,
                    ["label_key"] = opt.LabelKey,
                    ["description_key"] = opt.DescriptionKey,
                }
            );
        }
        dict["options"] = options;
    }
}
