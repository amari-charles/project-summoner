namespace Fateforged.Tests.Meta.Rewards;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class RewardClaimServiceTest
{
    [TestCase]
    public void URS_C11_C12_ValidBundleCommitsOnceAndRetryReturnsReceipt()
    {
        var store = StoreWithChoice(
            new ResourceRewardGrantDefinition
            {
                ResourceId = "gold",
                Amount = 40,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
            },
            new AcademyProgressFlagRewardGrantDefinition
            {
                FlagId = "lesson_complete",
                Amount = 1,
                Target = new RewardOwnershipTarget(
                    RewardOwnershipScope.SummonerCampaign,
                    "summoner_cole"
                ),
            }
        );
        var claims = new RewardClaimService(
            store,
            RewardGrantHandlerRegistry.CreateDefault()
        );
        var request = new RewardClaimRequest
        {
            ClaimId = new RewardClaimId("claim"),
            SelectedOptionIds = [new RewardOptionId("option")],
        };

        var first = claims.Claim(request);
        var retry = claims.Claim(request);

        AssertThat(first.Status).IsEqual(RewardRuntimeStatus.Ready);
        AssertThat(retry.Status).IsEqual(RewardRuntimeStatus.AlreadyClaimed);
        AssertThat(retry.Receipt).IsEqual(first.Receipt);
        AssertThat(store.Profile.Resources.Gold).IsEqual(40);
        AssertThat(
                store.Profile.CampaignProgressMap["summoner_cole"].Academy.RewardFlags[
                    "lesson_complete"
                ]
            )
            .IsEqual(1);
        AssertThat(store.CommitCount).IsEqual(1);
        AssertThat(store.State.PendingSelections).IsEmpty();
    }

    [TestCase]
    public void URS_C13_C14_InvalidGrantOrCommitFailureLeavesProfileAndReceiptUntouched()
    {
        var invalid = StoreWithChoice(
            new ResourceRewardGrantDefinition
            {
                ResourceId = "not_a_resource",
                Amount = 40,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
            }
        );
        var invalidResult = Service(invalid).Claim(Request());

        AssertThat(invalidResult.Status).IsEqual(RewardRuntimeStatus.Invalid);
        AssertThat(invalid.Profile.Resources.Gold).IsEqual(0);
        AssertThat(invalid.State.ClaimReceipts).IsEmpty();

        var failing = StoreWithChoice(
            new ResourceRewardGrantDefinition
            {
                ResourceId = "gold",
                Amount = 40,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
            }
        );
        failing.FailCommit = true;
        var failedCommit = Service(failing).Claim(Request());

        AssertThat(failedCommit.Status).IsEqual(RewardRuntimeStatus.Invalid);
        AssertThat(failing.Profile.Resources.Gold).IsEqual(0);
        AssertThat(failing.State.ClaimReceipts).IsEmpty();
        AssertThat(failing.State.PendingSelections).HasSize(1);
    }

    [TestCase]
    public void URS_C27_ConcurrentClaimsCommitOneReceiptAndOneGrant()
    {
        var store = StoreWithChoice(
            new ResourceRewardGrantDefinition
            {
                ResourceId = "gold",
                Amount = 40,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
            }
        );
        var claims = Service(store);
        RewardClaimResult? first = null;
        RewardClaimResult? second = null;

        Parallel.Invoke(
            () => first = claims.Claim(Request()),
            () => second = claims.Claim(Request())
        );

        AssertThat(store.Profile.Resources.Gold).IsEqual(40);
        AssertThat(store.CommitCount).IsEqual(1);
        AssertThat(first!.Receipt).IsEqual(second!.Receipt);
        AssertThat(
                new[] { first.Status, second.Status }.Count(status =>
                    status == RewardRuntimeStatus.Ready
                )
            )
            .IsEqual(1);
    }

    private static RewardClaimService Service(InMemoryRewardStore store) =>
        new(store, RewardGrantHandlerRegistry.CreateDefault());

    private static RewardClaimRequest Request() =>
        new()
        {
            ClaimId = new RewardClaimId("claim"),
            SelectedOptionIds = [new RewardOptionId("option")],
        };

    private static InMemoryRewardStore StoreWithChoice(
        params RewardGrantDefinition[] grants
    )
    {
        var store = new InMemoryRewardStore();
        var claimId = new RewardClaimId("claim");
        store.State.ResolvedOffers[claimId.Value] = new ResolvedRewardOfferSnapshot
        {
            ClaimId = claimId,
            OfferId = new RewardOfferId("offer"),
            Source = new RewardSourceContext
            {
                SourceType = "test",
                SourceId = "source",
            },
            SummonerId = new SummonerId("summoner_cole"),
            SelectionMode = RewardSelectionMode.PlayerChoice,
            ChooseCount = 1,
            Options =
            [
                new RewardOptionDefinition
                {
                    Id = new RewardOptionId("option"),
                    Grants = [.. grants],
                },
            ],
        };
        store.State.PendingSelections[claimId.Value] = new PendingRewardSelection
        {
            ClaimId = claimId,
            ChooseCount = 1,
        };
        return store;
    }

    private sealed class InMemoryRewardStore : IRewardProfileStore
    {
        public RewardProfileState State { get; } = new();
        public ProfileData Profile { get; private set; } = new();
        public bool FailCommit { get; set; }
        public int CommitCount { get; private set; }
        private readonly object _sync = new();

        public RewardProfileState GetRewardState()
        {
            lock (_sync)
            {
                return new RewardProfileState
                {
                    AcademySeedBySummoner = new(State.AcademySeedBySummoner),
                    ResolvedOffers = new(State.ResolvedOffers),
                    PendingSelections = new(State.PendingSelections),
                    ClaimReceipts = new(State.ClaimReceipts),
                };
            }
        }

        public bool TryGetOrCreateAcademySeed(
            SummonerId summonerId,
            out ulong seed,
            out string error
        )
        {
            seed = 1;
            error = "";
            return true;
        }

        public IReadOnlySet<string> GetOwnedRewardKeys(SummonerId summonerId) =>
            new HashSet<string>();

        public bool TryStoreResolvedOffer(
            ResolvedRewardOfferSnapshot snapshot,
            PendingRewardSelection? pending,
            out string error
        )
        {
            State.ResolvedOffers[snapshot.ClaimId.Value] = snapshot;
            if (pending != null)
                State.PendingSelections[snapshot.ClaimId.Value] = pending;
            error = "";
            return true;
        }

        public IRewardGrantTransaction BeginRewardTransaction() => new Transaction(this);

        private sealed class Transaction : IRewardGrantTransaction
        {
            private readonly InMemoryRewardStore _store;
            private readonly List<IRewardGrantMutation> _mutations = [];
            private RewardClaimReceipt? _receipt;

            public Transaction(InMemoryRewardStore store) => _store = store;
            public bool IsAvailable => true;

            public bool TryStage(IRewardGrantMutation mutation, out string error)
            {
                _mutations.Add(mutation);
                error = "";
                return true;
            }

            public bool TryStageReceipt(RewardClaimReceipt receipt, out string error)
            {
                _receipt = receipt;
                error = "";
                return true;
            }

            public RewardTransactionCommitResult Commit()
            {
                lock (_store._sync)
                {
                    if (_store.FailCommit)
                        return RewardTransactionCommitResult.Unavailable("simulated failure");
                    if (_store.State.ClaimReceipts.ContainsKey(_receipt!.ClaimId.Value))
                        return RewardTransactionCommitResult.Unavailable(
                            "claim already committed"
                        );

                    var candidate = Clone(_store.Profile);
                    foreach (var mutation in _mutations)
                    {
                        if (!mutation.TryApply(candidate, out var error))
                            return RewardTransactionCommitResult.Unavailable(error);
                    }
                    _store.Profile = candidate;
                    _store.State.ClaimReceipts[_receipt.ClaimId.Value] = _receipt;
                    _store.State.PendingSelections.Remove(_receipt.ClaimId.Value);
                    _store.CommitCount++;
                    return new RewardTransactionCommitResult(true, "");
                }
            }

            private static ProfileData Clone(ProfileData source) =>
                new()
                {
                    Resources = new Resources
                    {
                        Gold = source.Resources.Gold,
                        Gems = source.Resources.Gems,
                        Essence = source.Resources.Essence,
                        Fragments = source.Resources.Fragments,
                    },
                    CampaignProgressMap = source.CampaignProgressMap.ToDictionary(
                        pair => pair.Key,
                        pair => new CampaignProgress
                        {
                            Gold = pair.Value.Gold,
                            Academy = new AcademyProgress
                            {
                                RewardFlags = new Dictionary<string, int>(
                                    pair.Value.Academy.RewardFlags
                                ),
                            },
                        }
                    ),
                };
        }
    }
}
