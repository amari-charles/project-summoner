namespace Fateforged.Tests.Meta.Progression;

using System.Reflection;
using Fateforged.Domain.Progression;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Rewards;
using Fateforged.Session;
using Fateforged.View;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class BattleProgressionLegacyRemovalTest
{
    [TestCase]
    public void BPA_C18_SupersededBattleProgressionApisAreAbsent()
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        AssertThat(typeof(BattleScene).GetMethod("GrantCardXp", flags)).IsNull();
        AssertThat(typeof(BattleScene).GetMethod("GrantSummonerXp", flags)).IsNull();
        AssertThat(typeof(CampaignService).GetMethod("ClaimPendingReward", flags)).IsNull();
        AssertThat(typeof(CampaignService).GetMethod("SetPendingReward", flags)).IsNull();
        AssertThat(typeof(RewardService).GetMethod("GetBattleRewardSpec", flags)).IsNull();
        AssertThat(typeof(BattleScene).Assembly.GetType("Fateforged.Meta.Rewards.BattleRewardSpec"))
            .IsNull();
        AssertThat(
                typeof(BattleScene).Assembly.GetType(
                    "Fateforged.Domain.Profile.Campaign.PendingRewardData"
                )
            )
            .IsNull();
        AssertThat(typeof(BattleSessionConfig).GetProperty("BattleAttemptId")).IsNotNull();
        AssertThat(new BattleSessionConfig().BattleAttemptId).IsEqual(BattleAttemptId.None);
    }
}
