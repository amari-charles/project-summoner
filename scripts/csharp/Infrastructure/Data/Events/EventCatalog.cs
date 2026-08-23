using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Rewards;
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
            CardXpReward = 15,
            SummonerXpReward = 20,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCardAndAddToSelectedDeck(
                EventIds.FirstTrial,
                30,
                true,
                CardIds.FireWisp,
                CardIds.Puff,
                CardIds.Pebbloom
            ),
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
            CardXpReward = 18,
            SummonerXpReward = 25,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCardAndAddToSelectedDeck(
                EventIds.SecondChallenge,
                40,
                true,
                CardIds.Pebbloom,
                CardIds.Puff,
                CardIds.FireWisp
            ),
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
            CardXpReward = 20,
            SummonerXpReward = 28,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.AggressivePush,
                45,
                true,
                CardIds.FireWisp,
                CardIds.ManaBolt,
                CardIds.Puff
            ),
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
            CardXpReward = 20,
            SummonerXpReward = 28,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.ScoutSkirmish,
                45,
                true,
                CardIds.Puff,
                CardIds.Pebbloom,
                CardIds.ManaBolt
            ),
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
            CardXpReward = 22,
            SummonerXpReward = 32,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.StabilityLine,
                50,
                true,
                CardIds.Pebbloom,
                CardIds.Puff,
                CardIds.FireWisp
            ),
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
            CardXpReward = 24,
            SummonerXpReward = 36,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.ThirdTrial,
                55,
                true,
                CardIds.FireWisp,
                CardIds.Pebbloom,
                CardIds.ManaBolt
            ),
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
            CardXpReward = 24,
            SummonerXpReward = 36,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.MidlineTrial,
                55,
                true,
                CardIds.FireWisp,
                CardIds.ManaBolt,
                CardIds.Pebbloom
            ),
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
            CardXpReward = 28,
            SummonerXpReward = 46,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.RidgeAssault,
                70,
                true,
                CardIds.FireWisp,
                CardIds.ManaBolt,
                CardIds.Puff
            ),
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
            CardXpReward = 24,
            SummonerXpReward = 38,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.RiverHold,
                60,
                true,
                CardIds.Pebbloom,
                CardIds.Puff,
                CardIds.FireWisp
            ),
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
            CardXpReward = 26,
            SummonerXpReward = 42,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.GrovePatrol,
                65,
                true,
                CardIds.Pebbloom,
                CardIds.FireWisp,
                CardIds.Puff
            ),
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
            CardXpReward = 30,
            SummonerXpReward = 48,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.Chokepoint,
                70,
                true,
                CardIds.ManaBolt,
                CardIds.FireWisp,
                CardIds.Pebbloom
            ),
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
            CardXpReward = 35,
            SummonerXpReward = 55,
            FirstClearRewardOffers = BattleRewardAuthoring.AutomaticCards(
                EventIds.Gatekeeper,
                80,
                new BattleRewardCard(CardIds.Charge, "common", 1)
            ),
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
            CardXpReward = 38,
            SummonerXpReward = 60,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.EliteBattle01,
                95,
                true,
                CardIds.ManaBolt,
                CardIds.FireWisp,
                CardIds.Pebbloom
            ),
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
            CardXpReward = 42,
            SummonerXpReward = 66,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.EliteBattle02,
                105,
                true,
                CardIds.ManaBolt,
                CardIds.Pebbloom,
                CardIds.Puff
            ),
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
            CardXpReward = 46,
            SummonerXpReward = 72,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.EliteBattle03,
                115,
                true,
                CardIds.ManaBolt,
                CardIds.FireWisp,
                CardIds.Puff
            ),
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
            CardXpReward = 50,
            SummonerXpReward = 80,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.EliteBattle04,
                125,
                true,
                CardIds.ManaBolt,
                CardIds.FireWisp,
                CardIds.Pebbloom
            ),
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
            CardXpReward = 32,
            SummonerXpReward = 52,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.StandardBattle01,
                75,
                true,
                CardIds.Puff,
                CardIds.Pebbloom,
                CardIds.FireWisp
            ),
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
            CardXpReward = 36,
            SummonerXpReward = 58,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.StandardBattle02,
                85,
                true,
                CardIds.Pebbloom,
                CardIds.Puff,
                CardIds.ManaBolt
            ),
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
            CardXpReward = 40,
            SummonerXpReward = 64,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.StandardBattle03,
                95,
                true,
                CardIds.Pebbloom,
                CardIds.FireWisp,
                CardIds.ManaBolt
            ),
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
            CardXpReward = 44,
            SummonerXpReward = 72,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.StandardBattle04,
                105,
                true,
                CardIds.Pebbloom,
                CardIds.FireWisp,
                CardIds.ManaBolt
            ),
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
            CardXpReward = 34,
            SummonerXpReward = 54,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.GambitBattle01,
                80,
                true,
                CardIds.ManaBolt,
                CardIds.FireWisp,
                CardIds.Puff
            ),
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
            CardXpReward = 46,
            SummonerXpReward = 72,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.GambitBattle02,
                120,
                true,
                CardIds.Pebbloom,
                CardIds.FireWisp,
                CardIds.ManaBolt
            ),
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
            CardXpReward = 38,
            SummonerXpReward = 62,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.GambitBattle03,
                88,
                true,
                CardIds.ManaBolt,
                CardIds.Puff,
                CardIds.FireWisp
            ),
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
            CardXpReward = 52,
            SummonerXpReward = 84,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.GambitBattle04,
                130,
                true,
                CardIds.FireWisp,
                CardIds.ManaBolt,
                CardIds.Pebbloom
            ),
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
            CardXpReward = 52,
            SummonerXpReward = 82,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.RejoinTrial,
                120,
                true,
                CardIds.FireWisp,
                CardIds.Pebbloom,
                CardIds.ManaBolt
            ),
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
            CardXpReward = 56,
            SummonerXpReward = 88,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.FinalAnte,
                130,
                true,
                CardIds.ManaBolt,
                CardIds.FireWisp,
                CardIds.Puff
            ),
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
            CardXpReward = 60,
            SummonerXpReward = 95,
            FirstClearRewardOffers = BattleRewardAuthoring.ChooseOneCard(
                EventIds.StormBreaker,
                140,
                true,
                CardIds.ManaBolt,
                CardIds.Pebbloom,
                CardIds.FireWisp
            ),
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
            CardXpReward = 70,
            SummonerXpReward = 120,
            FirstClearRewardOffers = BattleRewardAuthoring.AutomaticCards(
                EventIds.Act1Boss,
                180,
                new BattleRewardCard(CardIds.ManaBolt, "rare", 1)
            ),
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
            NameKey = "campaign.battle.arena_all_units.name",
            DescriptionKey = "campaign.battle.arena_all_units.description",
            Position = new Vector2(700, 100),
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
            NameKey = "campaign.battle.arena_all_cards.name",
            DescriptionKey = "campaign.battle.arena_all_cards.description",
            Position = new Vector2(850, 100),
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
            NameKey = "campaign.battle.arena_all_spells.name",
            DescriptionKey = "campaign.battle.arena_all_spells.description",
            Position = new Vector2(1000, 100),
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
            NameKey = "campaign.battle.arena_sprite_units.name",
            DescriptionKey = "campaign.battle.arena_sprite_units.description",
            Position = new Vector2(1150, 100),
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
            Position = new Vector2(400, 250),
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
        dict["enemy_side"] = new Godot.Collections.Dictionary
        {
            ["team"] = 1,
            ["source"] = "authored",
            ["summoner"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["id"] = "campaign_enemy",
                ["display_name"] = "Campaign Enemy",
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
