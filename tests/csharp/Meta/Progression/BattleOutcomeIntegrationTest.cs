namespace Fateforged.Tests.Meta.Progression;

using Fateforged.Domain.Progression;
using Fateforged.Meta.Progression;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class BattleOutcomeIntegrationTest
{
    [TestCase]
    public void BPA_C02_C04_C05_C06_C14_C16_CoordinatorForwardsTypedTerminalOutcome()
    {
        var authority = new RecordingAuthority();
        var coordinator = new BattleOutcomeCoordinator(authority);
        var attemptId = new BattleAttemptId("attempt");

        var result = coordinator.Report(attemptId, BattleTerminalOutcome.Defeat);

        AssertThat(result.Status).IsEqual(ProgressionAuthorityStatus.Unavailable);
        AssertThat(authority.LastCompleteRequest).IsNotNull();
        AssertThat(authority.LastCompleteRequest!.AttemptId).IsEqual(attemptId);
        AssertThat(authority.LastCompleteRequest.Outcome).IsEqual(BattleTerminalOutcome.Defeat);
    }

    private sealed class RecordingAuthority : IProgressionAuthority
    {
        public CompleteBattleAttemptRequest? LastCompleteRequest { get; private set; }

        public ProgressionAuthorityResult StartBattleAttempt(StartBattleAttemptRequest request) =>
            ProgressionAuthorityResult.Unavailable("stub");

        public ProgressionAuthorityResult CompleteBattleAttempt(
            CompleteBattleAttemptRequest request
        )
        {
            LastCompleteRequest = request;
            return ProgressionAuthorityResult.Unavailable("stub");
        }

        public ProgressionAuthorityResult GetBattleRewards(BattleAttemptId attemptId) =>
            ProgressionAuthorityResult.Unavailable("stub");

        public ProgressionAuthorityResult GetPendingBattleRewards(
            Fateforged.Data.Summoners.SummonerId summonerId
        ) => ProgressionAuthorityResult.Unavailable("stub");

        public ProgressionAuthorityResult ClaimBattleReward(BattleRewardClaimRequest request) =>
            ProgressionAuthorityResult.Unavailable("stub");
    }
}
