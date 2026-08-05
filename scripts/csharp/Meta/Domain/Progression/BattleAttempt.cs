using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Meta.Campaign;

namespace Fateforged.Domain.Progression;

/// <summary>Unique occurrence identity created by the active progression authority.</summary>
public readonly record struct BattleAttemptId(string Value)
{
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    public override string ToString() => Value;

    public static BattleAttemptId FromString(string value) => new(value);

    public static readonly BattleAttemptId None = new("");
}

/// <summary>Durable lifecycle state for one campaign battle occurrence.</summary>
public enum BattleAttemptState
{
    Started,
    Victory,
    Defeat,
    Abandoned,
}

/// <summary>Terminal result accepted by the progression authority.</summary>
public enum BattleTerminalOutcome
{
    Victory,
    Defeat,
    Abandoned,
}

/// <summary>
/// Persisted promise for one launched battle. Reward amounts are authority-resolved
/// snapshots; remote implementations must not trust client-authored values.
/// </summary>
public sealed record BattleAttempt
{
    [JsonPropertyName("attempt_id")]
    public required BattleAttemptId AttemptId { get; init; }

    [JsonPropertyName("summoner_id")]
    public required SummonerId SummonerId { get; init; }

    [JsonPropertyName("campaign_id")]
    public required CampaignId CampaignId { get; init; }

    [JsonPropertyName("battle_id")]
    public required BattleId BattleId { get; init; }

    [JsonPropertyName("deck_card_instance_ids")]
    public List<CardInstanceId> DeckCardInstanceIds { get; init; } = [];

    [JsonPropertyName("card_xp_reward")]
    public int CardXpReward { get; init; }

    [JsonPropertyName("summoner_xp_reward")]
    public int SummonerXpReward { get; init; }

    /// <summary>First-clear offers resolved and frozen before battle launch.</summary>
    [JsonPropertyName("first_clear_reward_snapshots")]
    public List<ResolvedRewardOfferSnapshot> FirstClearRewardSnapshots { get; init; } = [];

    [JsonPropertyName("state")]
    public BattleAttemptState State { get; init; } = BattleAttemptState.Started;

    [JsonPropertyName("started_at")]
    public long StartedAtUnixSeconds { get; init; }
}

/// <summary>Minimal durable terminal receipt retained after the active attempt closes.</summary>
public sealed record BattleAttemptCompletion
{
    [JsonPropertyName("attempt_id")]
    public required BattleAttemptId AttemptId { get; init; }

    [JsonPropertyName("outcome")]
    public required BattleTerminalOutcome Outcome { get; init; }

    [JsonPropertyName("completed_at")]
    public long CompletedAtUnixSeconds { get; init; }

    [JsonPropertyName("claim_ids")]
    public List<RewardClaimId> ClaimIds { get; init; } = [];

    [JsonPropertyName("pending_claim_ids")]
    public List<RewardClaimId> PendingClaimIds { get; init; } = [];
}
