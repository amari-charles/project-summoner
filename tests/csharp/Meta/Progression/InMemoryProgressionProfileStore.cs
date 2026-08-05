namespace Fateforged.Tests.Meta.Progression;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Rewards;

internal sealed class InMemoryProgressionProfileStore : IProgressionProfileStore
{
    private readonly object _gate = new();

    public ProfileData Data { get; set; } = new();
    public int CommitCount { get; private set; }
    public bool FailNextCommit { get; set; }
    public Action<ProfileData>? BeforeNextCommit { get; set; }

    public ProfileData GetProgressionSnapshot()
    {
        lock (_gate)
            return Clone(Data);
    }

    public RewardProfileState GetRewardState() => Data.Rewards;

    public bool TryCommitProgression(
        IReadOnlyList<IRewardGrantMutation> mutations,
        out string error
    )
    {
        lock (_gate)
        {
            BeforeNextCommit?.Invoke(Data);
            BeforeNextCommit = null;
            if (FailNextCommit)
            {
                FailNextCommit = false;
                error = "Injected persistence failure.";
                return false;
            }
            var candidate = Clone(Data);
            foreach (var mutation in mutations)
                if (!mutation.TryApply(candidate, out error))
                    return false;
            Data = candidate;
            CommitCount++;
            error = "";
            return true;
        }
    }

    public bool TryGetOrCreateRewardSeed(SummonerId summonerId, out ulong seed, out string error)
    {
        if (!Data.Rewards.RewardSeedBySummoner.TryGetValue(summonerId.Value, out seed))
            Data.Rewards.RewardSeedBySummoner[summonerId.Value] = seed = 1;
        error = "";
        return true;
    }

    public IReadOnlySet<string> GetOwnedRewardKeys(SummonerId summonerId) =>
        GetProgressionSnapshot().Collection.Select(card => $"card:{card.CatalogId}").ToHashSet();

    private static ProfileData Clone(ProfileData profile) =>
        JsonSerializer.Deserialize<ProfileData>(JsonSerializer.Serialize(profile))!;

    public bool TryStoreResolvedOffer(
        ResolvedRewardOfferSnapshot snapshot,
        PendingRewardSelection? pending,
        out string error
    )
    {
        Data.Rewards.ResolvedOffers[snapshot.ClaimId.Value] = snapshot;
        if (pending != null)
            Data.Rewards.PendingSelections[pending.ClaimId.Value] = pending;
        error = "";
        return true;
    }

    public IRewardGrantTransaction BeginRewardTransaction() => new UnavailableTransaction();

    private sealed class UnavailableTransaction : IRewardGrantTransaction
    {
        public bool IsAvailable => false;

        public bool TryStage(IRewardGrantMutation mutation, out string error)
        {
            error = "Not used by progression authority tests.";
            return false;
        }

        public bool TryStageReceipt(RewardClaimReceipt receipt, out string error)
        {
            error = "Not used by progression authority tests.";
            return false;
        }

        public RewardTransactionCommitResult Commit() =>
            RewardTransactionCommitResult.Unavailable("Not used by progression authority tests.");
    }
}
