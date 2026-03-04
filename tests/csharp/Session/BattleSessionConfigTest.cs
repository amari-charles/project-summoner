namespace Fateforged.Tests.Session;

using Fateforged.Session;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class BattleSessionConfigTest
{
    // =========================================================================
    // ForPractice() DEFAULTS
    // =========================================================================

    [TestCase]
    public void ForPractice_SetsCorrectMode()
    {
        var config = BattleSessionConfig.ForPractice();
        AssertThat(config.Mode).IsEqual(4); // PRACTICE
    }

    [TestCase]
    public void ForPractice_HasZeroXpRewards()
    {
        var config = BattleSessionConfig.ForPractice();
        AssertThat(config.CardXpReward).IsEqual(0);
        AssertThat(config.SummonerXpReward).IsEqual(0);
    }

    [TestCase]
    public void ForPractice_IsNotRankedMatch()
    {
        var config = BattleSessionConfig.ForPractice();
        AssertThat(config.IsRankedMatch).IsFalse();
    }

    [TestCase]
    public void ForPractice_HasEmptyOriginScene()
    {
        var config = BattleSessionConfig.ForPractice();
        AssertThat(config.OriginScene).IsEqual("");
    }

    // =========================================================================
    // FIELD DEFAULTS
    // =========================================================================

    [TestCase]
    public void NewConfig_HasEmptyRankedMatchInfo()
    {
        var config = new BattleSessionConfig();
        AssertThat(config.RankedMatchInfo.Count).IsEqual(0);
    }

    [TestCase]
    public void NewConfig_HasDefaultWinCondition()
    {
        var config = new BattleSessionConfig();
        AssertThat(config.WinCondition).IsEqual("destroy_base");
    }

    // =========================================================================
    // XP REWARD FIELD POPULATION
    // =========================================================================

    [TestCase]
    public void CardXpReward_CanBeSet()
    {
        var config = new BattleSessionConfig { CardXpReward = 25 };
        AssertThat(config.CardXpReward).IsEqual(25);
    }

    [TestCase]
    public void SummonerXpReward_CanBeSet()
    {
        var config = new BattleSessionConfig { SummonerXpReward = 50 };
        AssertThat(config.SummonerXpReward).IsEqual(50);
    }

    [TestCase]
    public void OriginScene_CanBeSet()
    {
        var config = new BattleSessionConfig { OriginScene = "res://scenes/meta/screens/campaign_map.tscn" };
        AssertThat(config.OriginScene).IsEqual("res://scenes/meta/screens/campaign_map.tscn");
    }

    [TestCase]
    public void IsRankedMatch_CanBeSet()
    {
        var config = new BattleSessionConfig { IsRankedMatch = true };
        AssertThat(config.IsRankedMatch).IsTrue();
    }

    [TestCase]
    public void IsMpClient_FalseByDefault()
    {
        var config = new BattleSessionConfig();
        AssertThat(config.IsMpClient).IsFalse();
    }

    [TestCase]
    public void IsMpClient_TrueWhenMultiplayerWithoutAuthority()
    {
        var config = new BattleSessionConfig { IsMultiplayer = true, HasAuthority = false };
        AssertThat(config.IsMpClient).IsTrue();
    }
}
