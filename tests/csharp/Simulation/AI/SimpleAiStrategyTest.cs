namespace Fateforged.Tests.Simulation.AI;

using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Data;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimpleAiStrategyTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void SelectCardIndex_EmptyHand_ReturnsNegative()
    {
        var summoner = _state.Summoners[1];
        summoner.Hand.Clear();

        int result = SimpleAiStrategy.SelectCardIndex(_state, summoner);
        AssertThat(result).IsEqual(-1);
    }

    [TestCase]
    public void SelectCardIndex_NoAffordableCards_ReturnsNegative()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 0;
        summoner.Hand.Add("expensive");
        _state.CardDataMap["expensive"] = SimTestHelper.CreateSummonCard("expensive", manaCost: 10);

        int result = SimpleAiStrategy.SelectCardIndex(_state, summoner);
        AssertThat(result).IsEqual(-1);
    }

    [TestCase]
    public void SelectCardIndex_SinglePlayable_ReturnsThatIndex()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 5;
        summoner.Hand.Add("cheap");
        _state.CardDataMap["cheap"] = SimTestHelper.CreateSummonCard("cheap", manaCost: 3);

        int result = SimpleAiStrategy.SelectCardIndex(_state, summoner);
        AssertThat(result).IsEqual(0);
    }

    [TestCase]
    public void SelectCardIndex_MultiplePlayable_ReturnsValidIndex()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 10;
        summoner.Hand.Add("card_a");
        summoner.Hand.Add("card_b");
        summoner.Hand.Add("card_c");
        _state.CardDataMap["card_a"] = SimTestHelper.CreateSummonCard("card_a", manaCost: 3);
        _state.CardDataMap["card_b"] = SimTestHelper.CreateSummonCard("card_b", manaCost: 4);
        _state.CardDataMap["card_c"] = SimTestHelper.CreateSummonCard("card_c", manaCost: 5);

        int result = SimpleAiStrategy.SelectCardIndex(_state, summoner);
        AssertThat(result).IsGreaterEqual(0);
        AssertThat(result).IsLess(3);
    }

    [TestCase]
    public void SelectSpawnPosition_EnemyTeam_PositiveX()
    {
        var summoner = _state.Summoners[1];

        var pos = SimpleAiStrategy.SelectSpawnPosition(_state, summoner, "test");
        AssertThat(pos.X).IsGreater(0f);
        AssertThat(pos.X).IsLess(BattlefieldBounds.HalfWidth);
    }

    [TestCase]
    public void SelectSpawnPosition_PlayerTeam_NegativeX()
    {
        var summoner = _state.Summoners[0];

        var pos = SimpleAiStrategy.SelectSpawnPosition(_state, summoner, "test");
        AssertThat(pos.X).IsLess(0f);
        AssertThat(pos.X).IsGreater(-BattlefieldBounds.HalfWidth);
    }

    [TestCase]
    public void SelectSpawnPosition_ZWithinBounds()
    {
        var summoner = _state.Summoners[1];

        for (int i = 0; i < 10; i++)
        {
            var pos = SimpleAiStrategy.SelectSpawnPosition(_state, summoner, "test");
            AssertThat(pos.Z).IsGreaterEqual(-BattlefieldBounds.HalfDepth);
            AssertThat(pos.Z).IsLessEqual(BattlefieldBounds.HalfDepth);
        }
    }
}
