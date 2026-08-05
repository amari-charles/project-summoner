using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Fateforged.Cards;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;

namespace Fateforged.Data.Rewards;

public readonly record struct RewardOfferId(string Value)
{
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
}

public readonly record struct RewardOptionId(string Value)
{
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
}

public readonly record struct UniversalRewardPoolId(string Value)
{
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
}

public enum RewardSelectionMode
{
    Automatic,
    PlayerChoice,
}

public enum RewardPreviewPolicy
{
    Exact,
    CategoryUntilEarned,
}

public enum RewardOwnershipScope
{
    Account,
    SummonerCampaign,
    Summoner,
    CardInstance,
}

public enum RewardDuplicatePolicy
{
    Allow,
    ExcludeOwned,
    ExcludeExactDuplicates,
}

public readonly record struct RewardOwnershipTarget(
    RewardOwnershipScope Scope,
    string TargetId = ""
);

public sealed record RewardSelectionRule
{
    public RewardSelectionMode Mode { get; init; } = RewardSelectionMode.Automatic;
    public int ShowCount { get; init; } = 1;
    public int ChooseCount { get; init; } = 1;
}

public sealed record RewardEligibilityDefinition
{
    public RewardDuplicatePolicy DuplicatePolicy { get; init; } = RewardDuplicatePolicy.Allow;
}

public sealed record RewardOfferDefinition
{
    public required RewardOfferId Id { get; init; }
    public required RewardSelectionRule Selection { get; init; }
    public RewardPreviewPolicy PreviewPolicy { get; init; } = RewardPreviewPolicy.Exact;
    public RewardEligibilityDefinition Eligibility { get; init; } = new();
    public required RewardOptionSourceDefinition OptionSource { get; init; }
}

public sealed record RewardOptionDefinition
{
    public required RewardOptionId Id { get; init; }
    public string LabelKey { get; init; } = "";
    public string DescriptionKey { get; init; } = "";
    public ImmutableArray<RewardGrantDefinition> Grants { get; init; } = [];
}

public sealed record RewardPoolDefinition
{
    public required UniversalRewardPoolId Id { get; init; }
    public string CategoryKey { get; init; } = "";
    public ImmutableArray<RewardOptionDefinition> Options { get; init; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AuthoredRewardOptionSourceDefinition), "authored")]
[JsonDerivedType(typeof(PoolRewardOptionSourceDefinition), "pool")]
public abstract record RewardOptionSourceDefinition;

public sealed record AuthoredRewardOptionSourceDefinition : RewardOptionSourceDefinition
{
    public ImmutableArray<RewardOptionDefinition> Options { get; init; } = [];
}

public sealed record PoolRewardOptionSourceDefinition : RewardOptionSourceDefinition
{
    public required UniversalRewardPoolId PoolId { get; init; }
    public string PreviewCategoryKey { get; init; } = "";
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CardRewardGrantDefinition), "card")]
[JsonDerivedType(typeof(ResourceRewardGrantDefinition), "resource")]
[JsonDerivedType(typeof(ItemRewardGrantDefinition), "item")]
[JsonDerivedType(typeof(SummonerUnlockRewardGrantDefinition), "summoner_unlock")]
[JsonDerivedType(typeof(CosmeticRewardGrantDefinition), "cosmetic")]
[JsonDerivedType(typeof(EmoteRewardGrantDefinition), "emote")]
[JsonDerivedType(typeof(SummonerExperienceRewardGrantDefinition), "summoner_xp")]
[JsonDerivedType(typeof(CardExperienceRewardGrantDefinition), "card_xp")]
[JsonDerivedType(typeof(SummonerTraitRewardGrantDefinition), "summoner_trait")]
[JsonDerivedType(typeof(CardTraitRewardGrantDefinition), "card_trait")]
[JsonDerivedType(typeof(AcademyProgressFlagRewardGrantDefinition), "academy_progress_flag")]
public abstract record RewardGrantDefinition
{
    public required RewardOwnershipTarget Target { get; init; }
}

public sealed record CardRewardGrantDefinition : RewardGrantDefinition
{
    public required CardId CardId { get; init; }
    public string Rarity { get; init; } = "common";
    public int Count { get; init; } = 1;
}

public sealed record ResourceRewardGrantDefinition : RewardGrantDefinition
{
    public required string ResourceId { get; init; }
    public int Amount { get; init; }
}

public sealed record ItemRewardGrantDefinition : RewardGrantDefinition
{
    public required ItemId ItemId { get; init; }
    public int Count { get; init; } = 1;
}

public sealed record SummonerUnlockRewardGrantDefinition : RewardGrantDefinition
{
    public required SummonerId SummonerId { get; init; }
}

public sealed record CosmeticRewardGrantDefinition : RewardGrantDefinition
{
    public required string CosmeticId { get; init; }
}

public sealed record EmoteRewardGrantDefinition : RewardGrantDefinition
{
    public required string EmoteId { get; init; }
}

public sealed record SummonerExperienceRewardGrantDefinition : RewardGrantDefinition
{
    public int Amount { get; init; }
}

public sealed record CardExperienceRewardGrantDefinition : RewardGrantDefinition
{
    public int Amount { get; init; }
}

public sealed record SummonerTraitRewardGrantDefinition : RewardGrantDefinition
{
    public required TraitId TraitId { get; init; }
    public int Amount { get; init; } = 1;
}

public sealed record CardTraitRewardGrantDefinition : RewardGrantDefinition
{
    public required CardTraitId TraitId { get; init; }
    public int Amount { get; init; } = 1;
}

public sealed record AcademyProgressFlagRewardGrantDefinition : RewardGrantDefinition
{
    public required string FlagId { get; init; }
    public int Amount { get; init; } = 1;
}
