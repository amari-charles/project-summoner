namespace Fateforged.Tests.Meta.Progression;

using System.Linq;
using System.Threading.Tasks;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Domain.Progression;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Progression;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class LocalProgressionAuthorityTest
{
    private static readonly SummonerId SummonerId = SummonerIds.Cole;
    private static readonly CampaignId CampaignId = new("campaign_act_1");
    private static readonly BattleId BattleId = new(EventIds.FirstTrial.Value);
    private static readonly CardInstanceId CardInstanceId = new("owned-fire-wisp");

    [TestCase]
    public void BPA_C01_StartPersistsRandomAttemptSeedAndCapturedRewards()
    {
        var (store, authority) = CreateAuthority();

        var result = Start(authority);

        AssertThat(result.Status).IsEqual(ProgressionAuthorityStatus.Ready);
        AssertThat(result.Attempt!.AttemptId.Value.Length).IsEqual(32);
        AssertThat(result.Attempt.DeckCardInstanceIds).ContainsExactly(CardInstanceId);
        AssertThat(result.Attempt.CardXpReward).IsGreater(0);
        AssertThat(store.Data.Rewards.RewardSeedBySummoner[SummonerId.Value]).IsGreater(0UL);
        AssertThat(store.Data.CampaignProgressMap[SummonerId.Value].ActiveBattleAttempt)
            .IsEqual(result.Attempt);
        AssertThat(store.CommitCount).IsEqual(1);
    }

    [TestCase]
    public void BPA_C03_C07_StartingAgainAbandonsStaleAttemptWithoutGrants()
    {
        var (store, authority) = CreateAuthority();
        var first = Start(authority).Attempt!;

        var second = Start(authority).Attempt!;

        var progress = store.Data.CampaignProgressMap[SummonerId.Value];
        AssertThat(second.AttemptId).IsNotEqual(first.AttemptId);
        AssertThat(progress.BattleAttemptCompletions[first.AttemptId.Value].Outcome)
            .IsEqual(BattleTerminalOutcome.Abandoned);
        AssertThat(store.Data.SummonerInstances.Single().Xp).IsEqual(0);
        AssertThat(store.Data.Collection.Single().Xp).IsEqual(0);
    }

    [TestCase(BattleTerminalOutcome.Defeat)]
    [TestCase(BattleTerminalOutcome.Abandoned)]
    public void BPA_C04_C05_C06_NonVictoryClosesAttemptWithoutXpOrRewards(
        BattleTerminalOutcome outcome = BattleTerminalOutcome.Defeat
    )
    {
        var (store, authority) = CreateAuthority();
        var attempt = Start(authority).Attempt!;

        var result = Complete(authority, attempt, outcome);

        AssertThat(result.Status).IsEqual(ProgressionAuthorityStatus.Ready);
        AssertThat(result.RewardOffers).IsEmpty();
        AssertThat(store.Data.SummonerInstances.Single().Xp).IsEqual(0);
        AssertThat(store.Data.Collection.Single().Xp).IsEqual(0);
        AssertThat(store.Data.CampaignProgressMap[SummonerId.Value].CompletedBattles).IsEmpty();
    }

    [TestCase]
    public void BPA_C02_C08_C10_VictoryAtomicallyGrantsXpAndPersistsFirstClearChoice()
    {
        var (store, authority) = CreateAuthority();
        var attempt = Start(authority).Attempt!;
        var commitsBefore = store.CommitCount;

        var result = Complete(authority, attempt, BattleTerminalOutcome.Victory);

        AssertThat(result.Status).IsEqual(ProgressionAuthorityStatus.Ready);
        AssertThat(store.CommitCount).IsEqual(commitsBefore + 1);
        AssertThat(store.Data.SummonerInstances.Single().Xp).IsEqual(attempt.SummonerXpReward);
        AssertThat(store.Data.Collection.Single().Xp).IsEqual(attempt.CardXpReward);
        AssertThat(result.RewardOffers).HasSize(1);
        AssertThat(result.RewardOffers[0].DisplayState).IsEqual(RewardOfferDisplayState.Pending);
        AssertThat(result.Completion!.PendingClaimIds).HasSize(1);
        AssertThat(store.Data.CampaignProgressMap[SummonerId.Value].CompletedBattles)
            .Contains(BattleId);
    }

    [TestCase]
    public void BPA_C11_C13_RetryAndConcurrentCompletionCannotDoubleGrant()
    {
        var (store, authority) = CreateAuthority();
        var attempt = Start(authority).Attempt!;
        ProgressionAuthorityResult? first = null;
        ProgressionAuthorityResult? second = null;

        Parallel.Invoke(
            () => first = Complete(authority, attempt, BattleTerminalOutcome.Victory),
            () => second = Complete(authority, attempt, BattleTerminalOutcome.Victory)
        );

        AssertThat(new[] { first!.Status, second!.Status })
            .ContainsExactlyInAnyOrder(
                ProgressionAuthorityStatus.Ready,
                ProgressionAuthorityStatus.AlreadyCompleted
            );
        AssertThat(store.Data.SummonerInstances.Single().Xp).IsEqual(attempt.SummonerXpReward);
        AssertThat(store.Data.Collection.Single().Xp).IsEqual(attempt.CardXpReward);
    }

    [TestCase]
    public void BPA_C09_C12_ClaimIsAttemptBoundAndIdempotent()
    {
        var (store, authority) = CreateAuthority();
        var attempt = Start(authority).Attempt!;
        var victory = Complete(authority, attempt, BattleTerminalOutcome.Victory);
        var offer = victory.RewardOffers.Single();
        var option = offer.Options.First();

        var claim = authority.ClaimBattleReward(
            new BattleRewardClaimRequest
            {
                AttemptId = attempt.AttemptId,
                ClaimId = offer.ClaimId!.Value,
                SelectedOptionIds = [option.Id],
            }
        );
        var retry = authority.ClaimBattleReward(
            new BattleRewardClaimRequest
            {
                AttemptId = attempt.AttemptId,
                ClaimId = offer.ClaimId.Value,
                SelectedOptionIds = [option.Id],
            }
        );

        AssertThat(claim.Status).IsEqual(ProgressionAuthorityStatus.Ready);
        AssertThat(retry.Status).IsEqual(ProgressionAuthorityStatus.AlreadyCompleted);
        AssertThat(store.Data.CampaignProgressMap[SummonerId.Value].Gold).IsEqual(30);
        AssertThat(store.Data.Collection.Count).IsEqual(2);
        AssertThat(store.Data.Rewards.PendingSelections).IsEmpty();
    }

    [TestCase]
    public void BPA_C10_ReplayGrantsNewAttemptXpButNoFirstClearReward()
    {
        var (store, authority) = CreateAuthority();
        var first = Start(authority).Attempt!;
        Complete(authority, first, BattleTerminalOutcome.Victory);
        var replay = Start(authority).Attempt!;

        var result = Complete(authority, replay, BattleTerminalOutcome.Victory);

        AssertThat(result.RewardOffers).IsEmpty();
        AssertThat(store.Data.SummonerInstances.Single().Xp)
            .IsEqual(first.SummonerXpReward + replay.SummonerXpReward);
        AssertThat(store.Data.Collection.Single().Xp)
            .IsEqual(first.CardXpReward + replay.CardXpReward);
    }

    [TestCase]
    public void BPA_C10_UnknownAndConflictingOutcomesAreRejected()
    {
        var (_, authority) = CreateAuthority();
        var unknown = authority.CompleteBattleAttempt(
            new CompleteBattleAttemptRequest
            {
                AttemptId = new BattleAttemptId("unknown"),
                Outcome = BattleTerminalOutcome.Victory,
            }
        );
        var attempt = Start(authority).Attempt!;
        Complete(authority, attempt, BattleTerminalOutcome.Defeat);
        var conflict = Complete(authority, attempt, BattleTerminalOutcome.Victory);

        AssertThat(unknown.Status).IsEqual(ProgressionAuthorityStatus.Invalid);
        AssertThat(conflict.Status).IsEqual(ProgressionAuthorityStatus.Invalid);
    }

    [TestCase]
    public void BPA_C11_StartPersistenceFailureLeavesNoAttempt()
    {
        var (store, authority) = CreateAuthority();
        store.FailNextCommit = true;

        var result = Start(authority);

        AssertThat(result.Status).IsEqual(ProgressionAuthorityStatus.Unavailable);
        AssertThat(store.Data.CampaignProgressMap).IsEmpty();
    }

    [TestCase]
    public void BPA_C12_CompletionPersistenceFailureLeavesStartedStateAndNoGrants()
    {
        var (store, authority) = CreateAuthority();
        var attempt = Start(authority).Attempt!;
        store.FailNextCommit = true;

        var result = Complete(authority, attempt, BattleTerminalOutcome.Victory);

        AssertThat(result.Status).IsEqual(ProgressionAuthorityStatus.Unavailable);
        AssertThat(store.Data.SummonerInstances.Single().Xp).IsEqual(0);
        AssertThat(store.Data.Collection.Single().Xp).IsEqual(0);
        AssertThat(store.Data.CampaignProgressMap[SummonerId.Value].ActiveBattleAttempt)
            .IsNotNull();
        AssertThat(store.Data.CampaignProgressMap[SummonerId.Value].CompletedBattles).IsEmpty();
    }

    [TestCase]
    public void BPA_C14_FirstClearAndXpAreSummonerScoped()
    {
        var (store, authority) = CreateAuthority();
        var otherSummoner = SummonerIds.Selene;
        var otherCard = new CardInstanceId("owned-puff");
        store.Data.UnlockedSummoners.Add(otherSummoner);
        store.Data.SummonerInstances.Add(new SummonerInstance { SummonerId = otherSummoner });
        store.Data.Collection.Add(
            new CardInstance
            {
                Id = otherCard,
                CatalogId = CardIds.Puff,
                ProfileId = store.Data.ProfileId,
            }
        );

        var first = Start(authority).Attempt!;
        Complete(authority, first, BattleTerminalOutcome.Victory);
        var second = authority
            .StartBattleAttempt(
                new StartBattleAttemptRequest
                {
                    SummonerId = otherSummoner,
                    CampaignId = CampaignId,
                    BattleId = BattleId,
                    DeckCardInstanceIds = [otherCard],
                }
            )
            .Attempt!;
        var secondResult = Complete(authority, second, BattleTerminalOutcome.Victory);

        AssertThat(secondResult.RewardOffers).HasSize(1);
        AssertThat(store.Data.CampaignProgressMap).HasSize(2);
        AssertThat(store.Data.SummonerInstances.Single(value => value.SummonerId == SummonerId).Xp)
            .IsEqual(first.SummonerXpReward);
        AssertThat(
                store.Data.SummonerInstances.Single(value => value.SummonerId == otherSummoner).Xp
            )
            .IsEqual(second.SummonerXpReward);
    }

    private static (
        InMemoryProgressionProfileStore store,
        LocalProgressionAuthority authority
    ) CreateAuthority()
    {
        var store = new InMemoryProgressionProfileStore
        {
            Data = new ProfileData
            {
                ProfileId = new ProfileId("profile"),
                UnlockedSummoners = [SummonerId],
                SummonerInstances = [new SummonerInstance { SummonerId = SummonerId }],
                Collection =
                [
                    new CardInstance
                    {
                        Id = CardInstanceId,
                        CatalogId = CardIds.FireWisp,
                        ProfileId = new ProfileId("profile"),
                    },
                ],
            },
        };
        return (store, new LocalProgressionAuthority(store, UniversalRewardRuntime.Create(store)));
    }

    private static ProgressionAuthorityResult Start(LocalProgressionAuthority authority) =>
        authority.StartBattleAttempt(
            new StartBattleAttemptRequest
            {
                SummonerId = SummonerId,
                CampaignId = CampaignId,
                BattleId = BattleId,
                DeckCardInstanceIds = [CardInstanceId],
            }
        );

    private static ProgressionAuthorityResult Complete(
        LocalProgressionAuthority authority,
        BattleAttempt attempt,
        BattleTerminalOutcome outcome
    ) =>
        authority.CompleteBattleAttempt(
            new CompleteBattleAttemptRequest { AttemptId = attempt.AttemptId, Outcome = outcome }
        );
}
