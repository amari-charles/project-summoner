using System.Collections.Immutable;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Domain.Progression;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Rewards;

namespace Fateforged.Meta.Progression;

public enum ProgressionAuthorityStatus
{
    Ready,
    AlreadyCompleted,
    Invalid,
    Unavailable,
}

public sealed record StartBattleAttemptRequest
{
    public required SummonerId SummonerId { get; init; }
    public required CampaignId CampaignId { get; init; }
    public required BattleId BattleId { get; init; }
    public ImmutableArray<CardInstanceId> DeckCardInstanceIds { get; init; } = [];
}

public sealed record CompleteBattleAttemptRequest
{
    public required BattleAttemptId AttemptId { get; init; }
    public required BattleTerminalOutcome Outcome { get; init; }
}

public sealed record BattleRewardClaimRequest
{
    public required BattleAttemptId AttemptId { get; init; }
    public required RewardClaimId ClaimId { get; init; }
    public ImmutableArray<RewardOptionId> SelectedOptionIds { get; init; } = [];
}

/// <summary>Provider-neutral normalized result returned by every authority operation.</summary>
public sealed record ProgressionAuthorityResult
{
    public required ProgressionAuthorityStatus Status { get; init; }
    public BattleAttempt? Attempt { get; init; }
    public BattleAttemptCompletion? Completion { get; init; }
    public ImmutableArray<RewardOfferViewModel> RewardOffers { get; init; } = [];
    public RewardClaimReceipt? ClaimReceipt { get; init; }
    public ImmutableArray<string> Errors { get; init; } = [];

    public bool IsSuccess =>
        Status is ProgressionAuthorityStatus.Ready or ProgressionAuthorityStatus.AlreadyCompleted;

    public static ProgressionAuthorityResult Unavailable(string error) =>
        new() { Status = ProgressionAuthorityStatus.Unavailable, Errors = [error] };

    public static ProgressionAuthorityResult Invalid(string error) =>
        new() { Status = ProgressionAuthorityStatus.Invalid, Errors = [error] };
}

/// <summary>
/// Coarse-grained player progression port. Implementations own persistence and
/// authority; callers never request arbitrary profile mutations.
/// </summary>
public interface IProgressionAuthority
{
    ProgressionAuthorityResult StartBattleAttempt(StartBattleAttemptRequest request);
    ProgressionAuthorityResult CompleteBattleAttempt(CompleteBattleAttemptRequest request);
    ProgressionAuthorityResult GetBattleRewards(BattleAttemptId attemptId);
    ProgressionAuthorityResult GetPendingBattleRewards(SummonerId summonerId);
    ProgressionAuthorityResult ClaimBattleReward(BattleRewardClaimRequest request);
}
