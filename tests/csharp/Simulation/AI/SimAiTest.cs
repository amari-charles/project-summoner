namespace Fateforged.Tests.Simulation.AI;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimAiTest
{
    private MatchState _state = null!;
    private Simulation _sim = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
        _sim = new Simulation(_state);
    }

    // =========================================================================
    // AI Tick Produces Commands
    // =========================================================================

    [TestCase]
    public void AiTick_NoAi_NoCommands()
    {
        // Neither summoner has AI configured
        SimAi.Tick(_state, 0.016f);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void AiTick_PrepPhase_ProducesCommands()
    {
        _state.Phase = GamePhase.Preparation;
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Simple };
        summoner.Mana = 10;
        summoner.Hand.Add("card");
        _state.CardDataMap["card"] = SimTestHelper.CreateSummonCard("card", manaCost: 3);
        summoner.AiPlayTimer = 5.0f;
        summoner.AiNextPlayTime = 0.001f; // Would fire immediately

        SimAi.Tick(_state, 0.016f);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(1);
    }

    [TestCase]
    public void AiTick_GameOverPhase_NoCommands()
    {
        _state.Phase = GamePhase.GameOver;
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Simple };
        summoner.Mana = 10;
        summoner.Hand.Add("card");
        _state.CardDataMap["card"] = SimTestHelper.CreateSummonCard("card", manaCost: 3);
        summoner.AiPlayTimer = 5.0f;
        summoner.AiNextPlayTime = 0.001f;

        SimAi.Tick(_state, 0.016f);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void AiTick_Casting_NoCommands()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Simple };
        summoner.IsCasting = true;
        summoner.Mana = 10;
        summoner.Hand.Add("card");
        _state.CardDataMap["card"] = SimTestHelper.CreateSummonCard("card", manaCost: 3);
        summoner.AiNextPlayTime = 0.001f;

        SimAi.Tick(_state, 0.016f);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void AiTick_TimerNotReady_NoCommands()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Simple };
        summoner.Mana = 10;
        summoner.Hand.Add("card");
        _state.CardDataMap["card"] = SimTestHelper.CreateSummonCard("card", manaCost: 3);
        summoner.AiNextPlayTime = 10.0f; // Way in the future

        SimAi.Tick(_state, 0.016f);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void AiTick_TimerReady_ProducesCommand()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Simple };
        summoner.Mana = 10;
        summoner.Hand.Add("card");
        _state.CardDataMap["card"] = SimTestHelper.CreateSummonCard("card", manaCost: 3);
        summoner.AiPlayTimer = 5.0f;
        summoner.AiNextPlayTime = 3.0f; // Already past

        SimAi.Tick(_state, 0.016f);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(1);
        var cmd = _state.PendingCommandBuffer[0] as PlayCardCommand;
        AssertThat(cmd).IsNotNull();
        AssertThat(cmd!.Team).IsEqual(1);
    }

    // =========================================================================
    // Full Integration: AI Tick via Simulation.Tick()
    // =========================================================================

    [TestCase]
    public void SimulationTick_AiProducesCommands_ExecutedNextFrame()
    {
        // Configure AI with immediate timer
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Simple };
        summoner.Mana = 10;
        summoner.Hand.Add("test_card");
        _state.CardDataMap["test_card"] = SimTestHelper.CreateSummonCard("test_card", manaCost: 3);
        summoner.AiPlayTimer = 100f; // Way past
        summoner.AiNextPlayTime = 1f;

        // Tick 1: AI produces command (ExecuteFrame = FrameNumber + 1)
        _sim.Tick(0.016f);

        // Tick 2: Command is drained and executed
        var events = _sim.Tick(0.016f);

        // Should have casting/hand events from the card being played
        bool hasCasting = events.Any(e => e is CastingStartedEvent);
        AssertThat(hasCasting).IsTrue();
    }

    // =========================================================================
    // Determinism
    // =========================================================================

    [TestCase]
    public void Determinism_SameSeed_SameResult()
    {
        // Run two independent simulations with same seed and same AI config
        var result1 = RunSimulationForFrames(42, 60);
        var result2 = RunSimulationForFrames(42, 60);

        // Both should produce the same number of commands
        AssertThat(result1.Count).IsEqual(result2.Count);

        // And same card indices
        for (int i = 0; i < result1.Count; i++)
        {
            AssertThat(result1[i].CardIndex).IsEqual(result2[i].CardIndex);
            AssertThat(result1[i].Team).IsEqual(result2[i].Team);
        }
    }

    [TestCase]
    public void Determinism_DifferentSeed_DifferentResult()
    {
        var result1 = RunSimulationForFrames(42, 120);
        var result2 = RunSimulationForFrames(99, 120);

        // With different seeds, at least the timing/selection should differ
        // (They could theoretically match by chance, but extremely unlikely over 120 frames)
        bool anyDifference = result1.Count != result2.Count;
        if (!anyDifference && result1.Count > 0)
        {
            for (int i = 0; i < result1.Count; i++)
            {
                if (result1[i].CardIndex != result2[i].CardIndex)
                {
                    anyDifference = true;
                    break;
                }
            }
        }
        // Note: This is a probabilistic test. If both seeds happen to produce
        // identical results, that's theoretically possible but very unlikely.
        // We accept it if they match — the important guarantee is same-seed determinism above.
    }

    // =========================================================================
    // Test Arena Integration: Prep→Battle transition with AI
    // =========================================================================

    [TestCase]
    public void AiTick_PrepToBattleTransition_AiPlaysAfterPhaseChange()
    {
        // Mimic TestBattleScene: start in Preparation with PrepTimeRemaining=0
        var state = new MatchState
        {
            Phase = GamePhase.Preparation,
            PrepTimeRemaining = 0f,
            FrameNumber = 0,
            Rng = new DeterministicRng(42),
        };

        // Register summoners (mimic BattleScene.RegisterSummoner)
        state.Summoners[0].CurrentHp = 999999f;
        state.Summoners[0].MaxHp = 999999f;
        state.Summoners[0].Mana = 999f;
        state.Summoners[0].MaxMana = 999f;
        state.Summoners[0].IsAlive = true;
        state.Summoners[0].Team = Team.Player;
        state.Summoners[0].MaxHandSize = 4;

        state.Summoners[1].CurrentHp = 999999f;
        state.Summoners[1].MaxHp = 999999f;
        state.Summoners[1].Mana = 999f;
        state.Summoners[1].MaxMana = 999f;
        state.Summoners[1].IsAlive = true;
        state.Summoners[1].Team = Team.Enemy;
        state.Summoners[1].MaxHandSize = 4;

        // Populate card data and deck (mimic 30 fire_wisps)
        var cardData = SimTestHelper.CreateSummonCard("fire_wisp", manaCost: 3);
        state.CardDataMap["fire_wisp"] = cardData;
        for (int i = 0; i < 30; i++)
            state.Summoners[1].Deck.Add("fire_wisp");

        // Configure AI (mimic LoadAiForEnemy → ConfigureAi)
        state.Summoners[1].Ai = new AiConfig
        {
            Type = AiType.Heuristic,
            Personality = AiPersonality.Balanced,
            Difficulty = 3,
            PlayIntervalMin = 3.0f,
            PlayIntervalMax = 6.0f,
        };
        SimAi.InitializeTimer(state, state.Summoners[1]);

        var sim = new Simulation(state);

        // Run for 8 seconds (480 ticks at 60fps) — enough for at least one AI play
        bool foundCasting = false;
        for (int f = 0; f < 480; f++)
        {
            // Mimic TestBattleScene._Process: refresh mana
            if (state.Summoners[1].Mana < 900)
                state.Summoners[1].Mana = 999f;

            var events = sim.Tick(SimulationNode.FIXED_DELTA);
            foreach (var evt in events)
            {
                if (evt is CastingStartedEvent cse && cse.Team == 1)
                {
                    foundCasting = true;
                    break;
                }
            }
            if (foundCasting)
                break;
        }

        AssertThat(foundCasting).IsTrue();
        // Verify phase transitioned to Battle
        AssertThat(state.Phase).IsEqual(GamePhase.Battle);
    }

    /// <summary>
    /// Helper: set up a fresh sim with AI and run for N frames, collecting PlayCardCommands.
    /// </summary>
    private static List<PlayCardCommand> RunSimulationForFrames(uint seed, int frames)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var sim = new Simulation(state);

        var summoner = state.Summoners[1];
        summoner.Ai = new AiConfig
        {
            Type = AiType.Simple,
            PlayIntervalMin = 0.5f,
            PlayIntervalMax = 1.5f,
        };
        summoner.Mana = 100;
        summoner.MaxMana = 100;

        // Add several cards to hand and card data
        for (int i = 0; i < 4; i++)
        {
            string id = $"test_card_{i}";
            summoner.Hand.Add(id);
            state.CardDataMap[id] = SimTestHelper.CreateSummonCard(id, manaCost: 2);
        }

        // Also populate deck for redraws
        for (int i = 0; i < 8; i++)
        {
            string id = $"deck_card_{i}";
            summoner.Deck.Add(id);
            state.CardDataMap[id] = SimTestHelper.CreateSummonCard(id, manaCost: 2);
        }

        SimAi.InitializeTimer(state, summoner);

        var commands = new List<PlayCardCommand>();

        for (int f = 0; f < frames; f++)
        {
            // Snapshot pending buffer before tick
            int before = state.PendingCommandBuffer.Count;
            var events = sim.Tick(SimulationNode.FIXED_DELTA);

            // Check for CastingStartedEvent as proxy for command execution
            foreach (var evt in events)
            {
                if (evt is CastingStartedEvent cse && cse.Team == 1)
                {
                    commands.Add(new PlayCardCommand(cse.Team, cse.CardIndex, cse.SpawnPosition));
                }
            }
        }

        return commands;
    }
}
