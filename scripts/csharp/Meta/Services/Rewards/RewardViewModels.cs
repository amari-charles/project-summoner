using System.Collections.Immutable;
using System.Linq;
using Fateforged.Data.Rewards;
using Fateforged.Domain.Profile.Rewards;

namespace Fateforged.Meta.Rewards;

public enum RewardOfferDisplayState
{
    Preview,
    Pending,
    Claimed,
}

public sealed record RewardGrantViewModel
{
    public required string Kind { get; init; }
    public required RewardOwnershipScope OwnershipScope { get; init; }
    public string TargetId { get; init; } = "";
    public string ContentId { get; init; } = "";
    public string Rarity { get; init; } = "";
    public int Amount { get; init; } = 1;
}

public sealed record RewardOptionViewModel
{
    public required RewardOptionId Id { get; init; }
    public required string LabelKey { get; init; }
    public string DescriptionKey { get; init; } = "";
    public ImmutableArray<RewardGrantViewModel> Grants { get; init; } = [];
    public bool IsSelected { get; init; }
}

public sealed record RewardOfferViewModel
{
    public required RewardOfferId Id { get; init; }
    public required RewardRuntimeStatus Status { get; init; }
    public required RewardPreviewPolicy PreviewPolicy { get; init; }
    public required RewardOfferDisplayState DisplayState { get; init; }
    public RewardSelectionMode SelectionMode { get; init; }
    public RewardClaimId? ClaimId { get; init; }
    public string CategoryKey { get; init; } = "";
    public int ChooseCount { get; init; }
    public ImmutableArray<RewardOptionViewModel> Options { get; init; } = [];
    public RewardClaimReceipt? Receipt { get; init; }
}

public sealed class RewardViewModelFactory
{
    public RewardOfferViewModel Create(
        RewardOfferDefinition offer,
        ResolvedRewardOfferSnapshot? snapshot,
        PendingRewardSelection? pending = null,
        RewardClaimReceipt? receipt = null
    ) =>
        new()
        {
            Id = offer.Id,
            Status =
                receipt != null
                    ? RewardRuntimeStatus.AlreadyClaimed
                    : RewardRuntimeStatus.Ready,
            PreviewPolicy = offer.PreviewPolicy,
            DisplayState =
                receipt != null
                    ? RewardOfferDisplayState.Claimed
                    : pending != null
                        ? RewardOfferDisplayState.Pending
                        : RewardOfferDisplayState.Preview,
            SelectionMode = snapshot?.SelectionMode ?? offer.Selection.Mode,
            ClaimId = snapshot?.ClaimId,
            CategoryKey =
                offer.OptionSource is PoolRewardOptionSourceDefinition pool
                    ? pool.PreviewCategoryKey
                    : "",
            ChooseCount = snapshot?.ChooseCount ?? offer.Selection.ChooseCount,
            Options =
                snapshot?.Options.Select(option => ToView(option, pending)).ToImmutableArray()
                ?? [],
            Receipt = receipt,
        };

    private static RewardOptionViewModel ToView(
        RewardOptionDefinition option,
        PendingRewardSelection? pending
    ) =>
        new()
        {
            Id = option.Id,
            LabelKey = option.LabelKey,
            DescriptionKey = option.DescriptionKey,
            IsSelected =
                pending?.SelectedOptionIds.Contains(option.Id) == true,
            Grants = option.Grants.Select(CreateGrant).ToImmutableArray(),
        };

    public static RewardGrantViewModel CreateGrant(RewardGrantDefinition grant)
    {
        var (kind, contentId, rarity, amount) = grant switch
        {
            CardRewardGrantDefinition value => ("card", (string)value.CardId, value.Rarity, value.Count),
            ResourceRewardGrantDefinition value => ("resource", value.ResourceId, "", value.Amount),
            ItemRewardGrantDefinition value => ("item", (string)value.ItemId, "", value.Count),
            SummonerUnlockRewardGrantDefinition value => ("summoner_unlock", (string)value.SummonerId, "", 1),
            CosmeticRewardGrantDefinition value => ("cosmetic", value.CosmeticId, "", 1),
            EmoteRewardGrantDefinition value => ("emote", value.EmoteId, "", 1),
            SummonerExperienceRewardGrantDefinition value => ("summoner_xp", "", "", value.Amount),
            CardExperienceRewardGrantDefinition value => ("card_xp", "", "", value.Amount),
            SummonerTraitRewardGrantDefinition value => ("summoner_trait", (string)value.TraitId, "", value.Amount),
            CardTraitRewardGrantDefinition value => ("card_trait", (string)value.TraitId, "", value.Amount),
            AcademyProgressFlagRewardGrantDefinition value => ("academy_progress_flag", value.FlagId, "", value.Amount),
            _ => ("unknown", "", "", 0),
        };
        return new RewardGrantViewModel
        {
            Kind = kind,
            OwnershipScope = grant.Target.Scope,
            TargetId = grant.Target.TargetId,
            ContentId = contentId,
            Rarity = rarity,
            Amount = amount,
        };
    }
}
