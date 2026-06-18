namespace Fateforged.Tests.Simulation;

using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class WinConditionTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    // =========================================================================
    // DestroySummoner
    // =========================================================================

    [TestCase]
    public void DestroySummoner_BothAlive_ReturnsNull()
    {
        var wc = new DestroySummonerWinCondition();

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNull();
    }

    [TestCase]
    public void DestroySummoner_Team0Dead_Team1Wins()
    {
        _state.Summoners[0].IsAlive = false;
        var wc = new DestroySummonerWinCondition();

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNotNull();
        AssertThat(result!.WinnerTeam).IsEqual(1);
    }

    [TestCase]
    public void DestroySummoner_Team1Dead_Team0Wins()
    {
        _state.Summoners[1].IsAlive = false;
        var wc = new DestroySummonerWinCondition();

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNotNull();
        AssertThat(result!.WinnerTeam).IsEqual(0);
    }

    [TestCase]
    public void DestroySummoner_Team1ZeroHp_Team0Wins()
    {
        _state.Summoners[1].CurrentHp = 0f;
        _state.Summoners[1].IsAlive = true;
        var wc = new DestroySummonerWinCondition();

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNotNull();
        AssertThat(result!.WinnerTeam).IsEqual(0);
    }

    // =========================================================================
    // SurviveTime
    // =========================================================================

    [TestCase]
    public void SurviveTime_BelowLimit_ReturnsNull()
    {
        _state.MatchTime = 30f;
        var wc = new SurviveTimeWinCondition(60f);

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNull();
    }

    [TestCase]
    public void SurviveTime_AtLimit_Team0Wins()
    {
        _state.MatchTime = 60f;
        var wc = new SurviveTimeWinCondition(60f);

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNotNull();
        AssertThat(result!.WinnerTeam).IsEqual(0);
    }

    // =========================================================================
    // TimedDestroy
    // =========================================================================

    [TestCase]
    public void TimedDestroy_Expired_Team1Wins()
    {
        _state.MatchTime = 120f;
        var wc = new TimedDestroyWinCondition(60f);

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNotNull();
        AssertThat(result!.WinnerTeam).IsEqual(1);
    }

    [TestCase]
    public void TimedDestroy_EnemyDestroyed_Team0Wins()
    {
        _state.MatchTime = 30f;
        _state.Summoners[1].IsAlive = false;
        var wc = new TimedDestroyWinCondition(60f);

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNotNull();
        AssertThat(result!.WinnerTeam).IsEqual(0);
    }

    // =========================================================================
    // KillCount
    // =========================================================================

    [TestCase]
    public void KillCount_BelowTarget_ReturnsNull()
    {
        _state.KillCount = 3;
        var wc = new KillCountWinCondition(10);

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNull();
    }

    [TestCase]
    public void KillCount_AtTarget_Team0Wins()
    {
        _state.KillCount = 10;
        var wc = new KillCountWinCondition(10);

        var result = wc.Evaluate(_state);

        AssertThat(result).IsNotNull();
        AssertThat(result!.WinnerTeam).IsEqual(0);
    }

    // =========================================================================
    // Factory
    // =========================================================================

    [TestCase]
    public void Factory_Default_CreatesDestroySummoner()
    {
        _state.WinCondition = WinConditionType.DestroySummoner;
        var wc = WinConditionFactory.Create(_state);

        AssertThat(wc).IsInstanceOf<DestroySummonerWinCondition>();
    }

    [TestCase]
    public void Factory_SurviveTime_CreatesCorrectType()
    {
        _state.WinCondition = WinConditionType.SurviveTime;
        _state.WinConditionTimeLimit = 120f;
        var wc = WinConditionFactory.Create(_state);

        AssertThat(wc).IsInstanceOf<SurviveTimeWinCondition>();
        AssertThat(((SurviveTimeWinCondition)wc).TimeLimit).IsEqual(120f);
    }

    [TestCase]
    public void Factory_KillCount_CreatesCorrectType()
    {
        _state.WinCondition = WinConditionType.KillCount;
        _state.WinConditionKillTarget = 25;
        var wc = WinConditionFactory.Create(_state);

        AssertThat(wc).IsInstanceOf<KillCountWinCondition>();
        AssertThat(((KillCountWinCondition)wc).KillTarget).IsEqual(25);
    }

    [TestCase]
    public void Factory_TimedDestroy_CreatesCorrectType()
    {
        _state.WinCondition = WinConditionType.TimedDestroy;
        _state.WinConditionTimeLimit = 90f;
        var wc = WinConditionFactory.Create(_state);

        AssertThat(wc).IsInstanceOf<TimedDestroyWinCondition>();
        AssertThat(((TimedDestroyWinCondition)wc).TimeLimit).IsEqual(90f);
    }

    // =========================================================================
    // Parse
    // =========================================================================

    [TestCase]
    public void Parse_DestroyBase_ReturnsDestroySummoner()
    {
        AssertThat(WinConditionFactory.Parse("destroy_base"))
            .IsEqual(WinConditionType.DestroySummoner);
    }

    [TestCase]
    public void Parse_DestroySummonerAliases_ReturnDestroySummoner()
    {
        AssertThat(WinConditionFactory.Parse("DESTROY_SUMMONER"))
            .IsEqual(WinConditionType.DestroySummoner);
        AssertThat(WinConditionFactory.Parse(" summoner_destroyed "))
            .IsEqual(WinConditionType.DestroySummoner);
    }

    [TestCase]
    public void Parse_SurviveTime_ReturnsSurviveTime()
    {
        AssertThat(WinConditionFactory.Parse("survive_time")).IsEqual(WinConditionType.SurviveTime);
    }

    [TestCase]
    public void Parse_Unknown_DefaultsToDestroySummoner()
    {
        AssertThat(WinConditionFactory.Parse("some_unknown_mode"))
            .IsEqual(WinConditionType.DestroySummoner);
    }
}
