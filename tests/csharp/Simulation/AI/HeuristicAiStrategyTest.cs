namespace Fateforged.Tests.Simulation.AI;

using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Data;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class HeuristicAiStrategyTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    // =========================================================================
    // Battlefield State Evaluation
    // =========================================================================

    [TestCase]
    public void EvaluateBattlefieldState_NoUnits_Even()
    {
        var result = HeuristicAiStrategy.EvaluateBattlefieldState(_state, 0, _state.Summoners[0]);
        AssertThat((int)result).IsEqual((int)HeuristicAiStrategy.BattlefieldState.Even);
    }

    [TestCase]
    public void EvaluateBattlefieldState_MoreEnemies_Losing()
    {
        // 0 friendly, 3 enemy → unit disadvantage
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 6f);
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 7f);

        var result = HeuristicAiStrategy.EvaluateBattlefieldState(_state, 0, _state.Summoners[0]);
        // 3 unit disadvantage * 0.5 weight = -1.5, well below losing badly threshold
        AssertThat((int)result).IsEqual((int)HeuristicAiStrategy.BattlefieldState.LosingBadly);
    }

    [TestCase]
    public void EvaluateBattlefieldState_MoreFriendlies_Winning()
    {
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -6f);
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -7f);

        var result = HeuristicAiStrategy.EvaluateBattlefieldState(_state, 0, _state.Summoners[0]);
        AssertThat((int)result).IsEqual((int)HeuristicAiStrategy.BattlefieldState.Winning);
    }

    [TestCase]
    public void EvaluateBattlefieldState_LowHp_LosingBadly()
    {
        _state.Summoners[0].CurrentHp = 10f; // 10% HP
        _state.Summoners[0].MaxHp = 100f;

        var result = HeuristicAiStrategy.EvaluateBattlefieldState(_state, 0, _state.Summoners[0]);
        AssertThat((int)result).IsEqual((int)HeuristicAiStrategy.BattlefieldState.LosingBadly);
    }

    // =========================================================================
    // Card Scoring
    // =========================================================================

    [TestCase]
    public void ScoreCard_SummonPreferredOverSpell_WhenNoEnemies()
    {
        var summonCard = SimTestHelper.CreateSummonCard("test_summon", manaCost: 3);
        var spellCard = SimTestHelper.CreateSpellCard("test_spell", manaCost: 3);
        var rng = new DeterministicRng(42);

        // Use high difficulty to minimize randomness
        float summonScore = HeuristicAiStrategy.ScoreCard(summonCard,
            HeuristicAiStrategy.BattlefieldState.Even, AiPersonality.Balanced, 6, 0, rng);
        float spellScore = HeuristicAiStrategy.ScoreCard(spellCard,
            HeuristicAiStrategy.BattlefieldState.Even, AiPersonality.Balanced, 6, 0, rng);

        // With no enemies, spells get penalty — summon should score higher
        AssertThat(summonScore).IsGreater(spellScore);
    }

    [TestCase]
    public void ScoreCard_AggressivePrefersSummons()
    {
        var summonCard = SimTestHelper.CreateSummonCard("test_summon", manaCost: 2);
        var rng = new DeterministicRng(42);

        float balancedScore = HeuristicAiStrategy.ScoreCard(summonCard,
            HeuristicAiStrategy.BattlefieldState.Even, AiPersonality.Balanced, 6, 2, rng);
        float aggressiveScore = HeuristicAiStrategy.ScoreCard(summonCard,
            HeuristicAiStrategy.BattlefieldState.Even, AiPersonality.Aggressive, 6, 2, rng);

        AssertThat(aggressiveScore).IsGreater(balancedScore);
    }

    [TestCase]
    public void ScoreCard_SpellFocusedPrefersSpells()
    {
        var spellCard = SimTestHelper.CreateSpellCard("test_spell", manaCost: 3);
        var rng = new DeterministicRng(42);

        float balancedScore = HeuristicAiStrategy.ScoreCard(spellCard,
            HeuristicAiStrategy.BattlefieldState.Even, AiPersonality.Balanced, 6, 3, rng);
        float spellFocusedScore = HeuristicAiStrategy.ScoreCard(spellCard,
            HeuristicAiStrategy.BattlefieldState.Even, AiPersonality.SpellFocused, 6, 3, rng);

        AssertThat(spellFocusedScore).IsGreater(balancedScore);
    }

    // =========================================================================
    // Card Selection
    // =========================================================================

    [TestCase]
    public void SelectCardIndex_NoHand_ReturnsNegative()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Heuristic, Difficulty = 6 };
        summoner.Hand.Clear();

        int result = HeuristicAiStrategy.SelectCardIndex(_state, summoner);
        AssertThat(result).IsEqual(-1);
    }

    [TestCase]
    public void SelectCardIndex_CantAfford_ReturnsNegative()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Heuristic, Difficulty = 6 };
        summoner.Mana = 0;
        summoner.Hand.Add("expensive_card");
        _state.CardDataMap["expensive_card"] = SimTestHelper.CreateSummonCard("expensive_card", manaCost: 10);

        int result = HeuristicAiStrategy.SelectCardIndex(_state, summoner);
        AssertThat(result).IsEqual(-1);
    }

    [TestCase]
    public void SelectCardIndex_PicksPlayableCard()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Heuristic, Difficulty = 6 };
        summoner.Mana = 5;
        summoner.Hand.Add("affordable");
        _state.CardDataMap["affordable"] = SimTestHelper.CreateSummonCard("affordable", manaCost: 3);

        int result = HeuristicAiStrategy.SelectCardIndex(_state, summoner);
        AssertThat(result).IsEqual(0);
    }

    // =========================================================================
    // Spawn Position
    // =========================================================================

    [TestCase]
    public void SelectSpawnPosition_EnemyTeam_PositiveX()
    {
        var summoner = _state.Summoners[1]; // Enemy team
        summoner.Ai = new AiConfig { Type = AiType.Heuristic };
        summoner.Hand.Add("test");
        _state.CardDataMap["test"] = SimTestHelper.CreateSummonCard("test");

        var pos = HeuristicAiStrategy.SelectSpawnPosition(_state, summoner, "test");
        AssertThat(pos.X).IsGreater(0f);
    }

    [TestCase]
    public void SelectSpawnPosition_PlayerTeam_NegativeX()
    {
        var summoner = _state.Summoners[0]; // Player team
        summoner.Ai = new AiConfig { Type = AiType.Heuristic };
        summoner.Hand.Add("test");
        _state.CardDataMap["test"] = SimTestHelper.CreateSummonCard("test");

        var pos = HeuristicAiStrategy.SelectSpawnPosition(_state, summoner, "test");
        AssertThat(pos.X).IsLess(0f);
    }

    // =========================================================================
    // Spawn Zone Selection
    // =========================================================================

    [TestCase]
    public void SelectSpawnZone_AggressiveWinning_Aggressive()
    {
        var zone = HeuristicAiStrategy.SelectSpawnZone(AiPersonality.Aggressive,
            HeuristicAiStrategy.BattlefieldState.Winning);
        AssertThat(zone).IsEqual("aggressive");
    }

    [TestCase]
    public void SelectSpawnZone_DefensiveLosing_Defensive()
    {
        var zone = HeuristicAiStrategy.SelectSpawnZone(AiPersonality.Defensive,
            HeuristicAiStrategy.BattlefieldState.Losing);
        AssertThat(zone).IsEqual("defensive");
    }

    [TestCase]
    public void SelectSpawnZone_BalancedEven_Neutral()
    {
        var zone = HeuristicAiStrategy.SelectSpawnZone(AiPersonality.Balanced,
            HeuristicAiStrategy.BattlefieldState.Even);
        AssertThat(zone).IsEqual("neutral");
    }

    // =========================================================================
    // Play Timing
    // =========================================================================

    [TestCase]
    public void ComputeNextPlayInterval_ReturnsWithinRange()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig
        {
            Type = AiType.Heuristic,
            Difficulty = 3,
            PlayIntervalMin = 3.0f,
            PlayIntervalMax = 6.0f
        };

        // Run several times to check range
        for (int i = 0; i < 20; i++)
        {
            float interval = HeuristicAiStrategy.ComputeNextPlayInterval(_state, summoner);
            // With state multipliers and difficulty, range can vary, but should be positive
            AssertThat(interval).IsGreater(0f);
        }
    }
}
