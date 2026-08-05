using System.Collections.Generic;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Meta.Rewards;

namespace Fateforged.Infrastructure.Persistence;

public interface IRewardProfileStore : IRewardGrantTransactionFactory
{
    RewardProfileState GetRewardState();
    bool TryGetOrCreateRewardSeed(SummonerId summonerId, out ulong seed, out string error);
    IReadOnlySet<string> GetOwnedRewardKeys(SummonerId summonerId);
    bool TryStoreResolvedOffer(
        ResolvedRewardOfferSnapshot snapshot,
        PendingRewardSelection? pending,
        out string error
    );
}

public sealed class ProfileRewardGrantTransaction : IRewardGrantTransaction
{
    private readonly ProfileRepository _owner;
    private readonly int _expectedRevision;
    private readonly List<IRewardGrantMutation> _mutations = [];
    private RewardClaimReceipt? _receipt;
    private bool _completed;

    public bool IsAvailable => !_completed;

    internal ProfileRewardGrantTransaction(ProfileRepository owner, int expectedRevision)
    {
        _owner = owner;
        _expectedRevision = expectedRevision;
    }

    public bool TryStage(IRewardGrantMutation mutation, out string error)
    {
        if (_completed)
        {
            error = "Reward transaction is already complete.";
            return false;
        }

        _mutations.Add(mutation);
        error = "";
        return true;
    }

    public bool TryStageReceipt(RewardClaimReceipt receipt, out string error)
    {
        if (_completed)
        {
            error = "Reward transaction is already complete.";
            return false;
        }

        if (_receipt != null)
        {
            error = "Reward transaction already has a receipt.";
            return false;
        }

        _receipt = receipt;
        error = "";
        return true;
    }

    public RewardTransactionCommitResult Commit()
    {
        if (_completed)
            return RewardTransactionCommitResult.Unavailable(
                "Reward transaction is already complete."
            );

        _completed = true;
        if (_receipt == null)
            return RewardTransactionCommitResult.Unavailable(
                "Reward transaction cannot commit without a claim receipt."
            );

        return _owner.TryCommitRewardTransaction(_expectedRevision, _mutations, _receipt);
    }
}
