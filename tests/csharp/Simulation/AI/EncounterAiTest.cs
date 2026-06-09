namespace Fateforged.Tests.Simulation.AI;

using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class EncounterAiTest
{
    [TestCase]
    public void DefaultTrainer_UsesLegalCardPlay()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Simulation(state);
        var summoner = state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Simple };
        summoner.Mana = 10f;
        summoner.Hand.Add("weak_unit");
        summoner.AiPlayTimer = 99f;
        summoner.AiNextPlayTime = 1f;
        state.CardDataMap["weak_unit"] = SimTestHelper.CreateSummonCard("weak_unit", manaCost: 2);
        state.EncounterAi = EncounterAiConfig.DefaultTrainer();

        sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(state.PendingCommandBuffer.Count).IsEqual(1);
        AssertThat(state.PendingCommandBuffer[0]).IsInstanceOf<PlayCardCommand>();
    }

    [TestCase]
    public void EncounterSpawnUnits_BypassesDeckOnlyWhenEncounterSourced()
    {
        var state = SimTestHelper.CreateBattleState();
        state.CardDataMap["training_target"] = SimTestHelper.CreateSummonCard(
            "training_target",
            unitDamage: 0f
        );
        var config = EncounterAiConfig.ScriptedEncounter();
        state.EncounterAi = config;

        var result = EncounterAi.ExecuteAction(
            state,
            config,
            new EncounterAction
            {
                Kind = EncounterActionKind.SpawnUnits,
                Source = EncounterActionSource.Encounter,
                CardId = "training_target",
                Team = 1,
                Position = new SimVector3(10f, 0f, 0f),
            }
        );

        AssertThat((int)result.Status).IsEqual((int)EncounterActionStatus.Executed);
        AssertThat(state.PendingCommandBuffer[0]).IsInstanceOf<SpawnUnitCommand>();
    }

    [TestCase]
    public void CapRule_PreventsRepeatedSpawnSpam()
    {
        var state = SimTestHelper.CreateBattleState();
        state.CardDataMap["training_target"] = SimTestHelper.CreateSummonCard(
            "training_target",
            unitDamage: 0f
        );
        SimTestHelper.CreateMeleeUnit(state, team: 1, damage: 0f);
        var config = EncounterAiConfig.ScriptedEncounter();
        config.Rules.Add(new EncounterRule { Kind = EncounterRuleKind.CapRule, MaxAlive = 1 });
        state.EncounterAi = config;

        var result = EncounterAi.ExecuteAction(
            state,
            config,
            new EncounterAction
            {
                Kind = EncounterActionKind.SpawnUnits,
                Source = EncounterActionSource.Encounter,
                CardId = "training_target",
                Team = 1,
            }
        );

        AssertThat((int)result.Status).IsEqual((int)EncounterActionStatus.Blocked);
        AssertThat(state.PendingCommandBuffer).IsEmpty();
    }

    [TestCase]
    public void PacingGate_BlocksMajorActionsWhenPlayerIsOverwhelmed()
    {
        var state = SimTestHelper.CreateBattleState();
        state.CardDataMap["weak_unit"] = SimTestHelper.CreateSummonCard("weak_unit");
        SimTestHelper.CreateMeleeUnit(state, team: 1, x: -18f, damage: 10f);
        SimTestHelper.CreateMeleeUnit(state, team: 1, x: -16f, damage: 10f);
        var config = EncounterAiConfig.ScriptedEncounter();
        config.LastPlayerDamageTime = state.MatchTime;
        state.EncounterAi = config;

        AssertThat((int)config.LastDangerState).IsEqual((int)EncounterDangerState.Calm);
        config.LastDangerState = EncounterAi.EvaluateDangerState(state, 1);
        var result = EncounterAi.ExecuteAction(
            state,
            config,
            new EncounterAction
            {
                Kind = EncounterActionKind.SpawnUnits,
                Source = EncounterActionSource.Encounter,
                CardId = "weak_unit",
                Team = 1,
            }
        );

        AssertThat((int)config.LastDangerState).IsEqual((int)EncounterDangerState.Overwhelmed);
        AssertThat((int)result.Status).IsEqual((int)EncounterActionStatus.Blocked);
    }

    [TestCase]
    public void PoolRules_SupportWeakThenStrongEscalation()
    {
        var state = SimTestHelper.CreateBattleState();
        state.CardDataMap["weak_unit"] = SimTestHelper.CreateSummonCard("weak_unit");
        state.CardDataMap["strong_unit"] = SimTestHelper.CreateSummonCard("strong_unit");
        var config = EncounterAiConfig.ScriptedEncounter();
        config.Rules.Add(
            new EncounterRule
            {
                Kind = EncounterRuleKind.PoolRule,
                StartTime = 0f,
                EndTime = 2f,
                CardPool = ["weak_unit"],
            }
        );
        config.Rules.Add(
            new EncounterRule
            {
                Kind = EncounterRuleKind.PoolRule,
                StartTime = 2f,
                EndTime = 4f,
                CardPool = ["strong_unit"],
            }
        );
        state.EncounterAi = config;

        state.MatchTime = 1f;
        EncounterAi.ExecuteAction(
            state,
            config,
            new EncounterAction
            {
                Kind = EncounterActionKind.SpawnUnits,
                Source = EncounterActionSource.Encounter,
                Team = 1,
            }
        );
        var weakCommand = (SpawnUnitCommand)state.PendingCommandBuffer[0];
        state.PendingCommandBuffer.Clear();

        state.MatchTime = 3f;
        EncounterAi.ExecuteAction(
            state,
            config,
            new EncounterAction
            {
                Kind = EncounterActionKind.SpawnUnits,
                Source = EncounterActionSource.Encounter,
                Team = 1,
            }
        );
        var strongCommand = (SpawnUnitCommand)state.PendingCommandBuffer[0];

        AssertThat(weakCommand.CatalogId.Value).IsEqual("weak_unit");
        AssertThat(strongCommand.CatalogId.Value).IsEqual("strong_unit");
    }

    [TestCase]
    public void FutureActions_FailSafelyAsUnsupported()
    {
        var state = SimTestHelper.CreateBattleState();
        var config = EncounterAiConfig.ScriptedEncounter();

        var result = EncounterAi.ExecuteAction(
            state,
            config,
            new EncounterAction
            {
                Kind = EncounterActionKind.SpawnHazard,
                Source = EncounterActionSource.Hazard,
            }
        );

        AssertThat((int)result.Status).IsEqual((int)EncounterActionStatus.Unsupported);
        AssertThat(state.PendingCommandBuffer).IsEmpty();
    }

    [TestCase]
    public void BehaviorRule_AppliesToConfiguredEncounterTeam()
    {
        var state = SimTestHelper.CreateBattleState();
        state.EncounterAi = new EncounterAiConfig
        {
            Team = 0,
            UseTrainerAi = false,
            Rules =
            [
                new EncounterRule
                {
                    Kind = EncounterRuleKind.BehaviorRule,
                    AiType = AiType.Heuristic,
                    Personality = AiPersonality.Aggressive,
                    PlayIntervalMin = 1f,
                    PlayIntervalMax = 2f,
                },
            ],
        };

        EncounterAi.Tick(state, Simulation.FixedDeltaSeconds);

        var ai = state.Summoners[0].Ai;
        AssertThat(ai).IsNotNull();
        AssertThat((int)ai!.Type).IsEqual((int)AiType.Heuristic);
        AssertThat((int)ai.Personality).IsEqual((int)AiPersonality.Aggressive);
        AssertThat(ai.PlayIntervalMin).IsEqual(1f);
        AssertThat(ai.PlayIntervalMax).IsEqual(2f);
        AssertThat(state.Summoners[1].Ai).IsNull();
    }
}
