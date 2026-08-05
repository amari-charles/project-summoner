using System.Collections.Generic;
using System.Collections.Immutable;
using Fateforged.Data.Rewards;
using Fateforged.Infrastructure.Persistence;
using Godot;

namespace Fateforged.Meta.Rewards;

public sealed class UniversalRewardRuntime
{
    public RewardRuntimeStatus Status { get; }
    public RewardResolver Resolver { get; }
    public RewardClaimService Claims { get; }
    public IRewardProfileStore ProfileStore { get; }
    public RewardGrantHandlerRegistry Handlers { get; }
    public RewardContentCatalog Catalog { get; }
    public ImmutableArray<string> Errors { get; }

    private UniversalRewardRuntime(
        IRewardProfileStore profileStore,
        RewardContentCatalog catalog,
        RewardRuntimeStatus status,
        ImmutableArray<string> errors = default
    )
    {
        ProfileStore = profileStore;
        Catalog = catalog;
        Status = status;
        Errors = errors.IsDefault ? [] : errors;
        Handlers = RewardGrantHandlerRegistry.CreateDefault();
        Resolver = new RewardResolver([
            new AuthoredRewardOptionSource(),
            new PoolRewardOptionSource(),
        ]);
        Claims = new RewardClaimService(profileStore, Handlers);
    }

    public static UniversalRewardRuntime Create(
        IRewardProfileStore profileStore,
        RewardContentCatalog? catalog = null
    ) => new(profileStore, catalog ?? new RewardContentCatalog(), RewardRuntimeStatus.Ready);

    public static UniversalRewardRuntime CreateUnavailable() =>
        new(
            UnavailableRewardProfileStore.Instance,
            new RewardContentCatalog(),
            RewardRuntimeStatus.Unavailable
        );

    public static UniversalRewardRuntime CreateInvalid(
        IRewardProfileStore profileStore,
        IEnumerable<string> errors
    ) => new(profileStore, new RewardContentCatalog(), RewardRuntimeStatus.Invalid, [.. errors]);

    public Godot.Collections.Dictionary ToStatusDictionary() =>
        new()
        {
            ["status"] = Status switch
            {
                RewardRuntimeStatus.Ready => "ready",
                RewardRuntimeStatus.Invalid => "invalid",
                _ => "unavailable",
            },
            ["can_resolve"] = Status == RewardRuntimeStatus.Ready,
            ["can_claim"] = Status == RewardRuntimeStatus.Ready,
            ["errors"] = new Godot.Collections.Array<string>(Errors),
        };
}

internal sealed class UnavailableRewardProfileStore : IRewardProfileStore
{
    public static UnavailableRewardProfileStore Instance { get; } = new();
    private readonly Fateforged.Domain.Profile.Rewards.RewardProfileState _state = new();

    private UnavailableRewardProfileStore() { }

    public Fateforged.Domain.Profile.Rewards.RewardProfileState GetRewardState() => _state;

    public bool TryGetOrCreateRewardSeed(
        Fateforged.Data.Summoners.SummonerId summonerId,
        out ulong seed,
        out string error
    )
    {
        seed = 0;
        error = "Reward profile store is unavailable.";
        return false;
    }

    public IReadOnlySet<string> GetOwnedRewardKeys(
        Fateforged.Data.Summoners.SummonerId summonerId
    ) => new HashSet<string>();

    public bool TryStoreResolvedOffer(
        Fateforged.Domain.Profile.Rewards.ResolvedRewardOfferSnapshot snapshot,
        Fateforged.Domain.Profile.Rewards.PendingRewardSelection? pending,
        out string error
    )
    {
        error = "Reward profile store is unavailable.";
        return false;
    }

    public IRewardGrantTransaction BeginRewardTransaction() => new UnavailableRewardTransaction();

    private sealed class UnavailableRewardTransaction : IRewardGrantTransaction
    {
        public bool IsAvailable => false;

        public bool TryStage(IRewardGrantMutation mutation, out string error)
        {
            error = "Reward profile store is unavailable.";
            return false;
        }

        public bool TryStageReceipt(
            Fateforged.Domain.Profile.Rewards.RewardClaimReceipt receipt,
            out string error
        )
        {
            error = "Reward profile store is unavailable.";
            return false;
        }

        public RewardTransactionCommitResult Commit() =>
            RewardTransactionCommitResult.Unavailable("Reward profile store is unavailable.");
    }
}
