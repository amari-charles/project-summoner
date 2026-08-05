using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;

namespace Fateforged.Domain.Profile.Rewards;

public readonly record struct RewardClaimId(string Value)
{
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
}

public sealed record RewardSourceContext
{
    public required string SourceType { get; init; }
    public required string SourceId { get; init; }
    public string OccurrenceId { get; init; } = "";
}

public sealed record ResolvedRewardOfferSnapshot
{
    public required RewardClaimId ClaimId { get; init; }
    public required RewardOfferId OfferId { get; init; }
    public required RewardSourceContext Source { get; init; }
    public required SummonerId SummonerId { get; init; }
    public int ResolutionVersion { get; init; } = 1;
    public RewardSelectionMode SelectionMode { get; init; }
    public int ChooseCount { get; init; } = 1;
    public ImmutableArray<RewardOptionDefinition> Options { get; init; } = [];
}

public sealed record PendingRewardSelection
{
    public required RewardClaimId ClaimId { get; init; }
    public int ChooseCount { get; init; }
    public ImmutableArray<RewardOptionId> SelectedOptionIds { get; init; } = [];
}

public sealed record RewardClaimReceipt
{
    public required RewardClaimId ClaimId { get; init; }
    public ImmutableArray<RewardOptionId> ClaimedOptionIds { get; init; } = [];
    public ImmutableArray<RewardGrantDefinition> AppliedGrants { get; init; } = [];
}

public sealed class RewardProfileState
{
    [JsonPropertyName("academy_seed_by_summoner")]
    public Dictionary<string, ulong> AcademySeedBySummoner { get; set; } = [];

    [JsonPropertyName("resolved_offers")]
    public Dictionary<string, ResolvedRewardOfferSnapshot> ResolvedOffers { get; set; } = [];

    [JsonPropertyName("pending_selections")]
    public Dictionary<string, PendingRewardSelection> PendingSelections { get; set; } = [];

    [JsonPropertyName("claim_receipts")]
    public Dictionary<string, RewardClaimReceipt> ClaimReceipts { get; set; } = [];
}
