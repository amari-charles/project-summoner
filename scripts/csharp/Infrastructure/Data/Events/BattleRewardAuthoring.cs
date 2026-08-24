using System.Collections.Immutable;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Rewards;

namespace Fateforged.Data.Events;

/// <summary>Typed content-authoring helpers that produce universal reward offers.</summary>
public static class BattleRewardAuthoring
{
    public static ImmutableArray<RewardOfferDefinition> ChooseOneCard(
        EventId battleId,
        int gold,
        bool excludeOwned,
        params CardId[] cardIds
    ) => ChooseOneCard(battleId, gold, excludeOwned, false, cardIds);

    public static ImmutableArray<RewardOfferDefinition> ChooseOneCardAndAddToSelectedDeck(
        EventId battleId,
        int gold,
        bool excludeOwned,
        params CardId[] cardIds
    ) => ChooseOneCard(battleId, gold, excludeOwned, true, cardIds);

    private static ImmutableArray<RewardOfferDefinition> ChooseOneCard(
        EventId battleId,
        int gold,
        bool excludeOwned,
        bool addToSelectedDeck,
        params CardId[] cardIds
    )
    {
        var options = cardIds
            .Select(cardId => new RewardOptionDefinition
            {
                Id = new RewardOptionId(cardId.Value),
                LabelKey = cardId.Value,
                Grants = BuildCommonGrants(gold)
                    .Add(
                        new CardRewardGrantDefinition
                        {
                            Target = SummonerTarget(),
                            CardId = cardId,
                            Rarity =
                                CardCatalog.GetCard(cardId)?.Rarity.ToString().ToLowerInvariant()
                                ?? "common",
                            Placement = addToSelectedDeck
                                ? CardRewardPlacement.SelectedDeckIfAvailable
                                : CardRewardPlacement.CollectionOnly,
                        }
                    ),
            })
            .ToImmutableArray();
        return
        [
            new RewardOfferDefinition
            {
                Id = FirstClearOfferId(battleId),
                Selection = new RewardSelectionRule
                {
                    Mode = RewardSelectionMode.PlayerChoice,
                    ShowCount = options.Length,
                    ChooseCount = 1,
                },
                Eligibility = new RewardEligibilityDefinition
                {
                    DuplicatePolicy = excludeOwned
                        ? RewardDuplicatePolicy.ExcludeOwned
                        : RewardDuplicatePolicy.Allow,
                    FallbackToDuplicatesWhenInsufficient = excludeOwned,
                },
                OptionSource = new AuthoredRewardOptionSourceDefinition { Options = options },
            },
        ];
    }

    public static ImmutableArray<RewardOfferDefinition> AutomaticCards(
        EventId battleId,
        int gold,
        params BattleRewardCard[] cards
    )
    {
        var grants = BuildCommonGrants(gold).ToBuilder();
        foreach (var card in cards)
            grants.Add(
                new CardRewardGrantDefinition
                {
                    Target = SummonerTarget(),
                    CardId = card.CardId,
                    Rarity = card.Rarity,
                    Count = card.Count,
                }
            );
        return Automatic(battleId, grants.ToImmutable());
    }

    private static ImmutableArray<RewardOfferDefinition> Automatic(
        EventId battleId,
        ImmutableArray<RewardGrantDefinition> grants
    ) =>
        grants.Length == 0
            ? []
            :
            [
                new RewardOfferDefinition
                {
                    Id = FirstClearOfferId(battleId),
                    Selection = new RewardSelectionRule
                    {
                        Mode = RewardSelectionMode.Automatic,
                        ShowCount = 1,
                        ChooseCount = 1,
                    },
                    OptionSource = new AuthoredRewardOptionSourceDefinition
                    {
                        Options =
                        [
                            new RewardOptionDefinition
                            {
                                Id = new RewardOptionId("automatic"),
                                Grants = grants,
                            },
                        ],
                    },
                },
            ];

    private static ImmutableArray<RewardGrantDefinition> BuildCommonGrants(int gold) =>
        gold <= 0
            ? []
            :
            [
                new ResourceRewardGrantDefinition
                {
                    Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
                    ResourceId = "gold",
                    Amount = gold,
                },
            ];

    private static RewardOwnershipTarget SummonerTarget() =>
        new(RewardOwnershipScope.Summoner, "$summoner");

    private static RewardOfferId FirstClearOfferId(EventId battleId) =>
        new($"battle:{battleId.Value}:first_clear");
}

public readonly record struct BattleRewardCard(CardId CardId, string Rarity, int Count = 1);
