namespace Fateforged.Tests.Meta.Rewards;

using System;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Rewards;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Meta.Deck;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RewardGrantHandlerTest
{
    [TestCase]
    public void URS_C15_OwnershipTargetRequiresScopeAndOptionalExplicitTarget()
    {
        var target = new RewardOwnershipTarget(
            RewardOwnershipScope.CardInstance,
            "card_instance_1"
        );

        AssertThat(target.Scope).IsEqual(RewardOwnershipScope.CardInstance);
        AssertThat(target.TargetId).IsEqual("card_instance_1");
    }

    [TestCase]
    public void URS_C16_C26_BaselineGrantDefinitionsAreSeparateTypes()
    {
        Type[] grantTypes =
        [
            typeof(CardRewardGrantDefinition),
            typeof(ResourceRewardGrantDefinition),
            typeof(ItemRewardGrantDefinition),
            typeof(SummonerUnlockRewardGrantDefinition),
            typeof(CosmeticRewardGrantDefinition),
            typeof(EmoteRewardGrantDefinition),
            typeof(SummonerExperienceRewardGrantDefinition),
            typeof(CardExperienceRewardGrantDefinition),
            typeof(SummonerTraitRewardGrantDefinition),
            typeof(CardTraitRewardGrantDefinition),
            typeof(AcademyProgressFlagRewardGrantDefinition),
        ];

        AssertThat(grantTypes.Length).IsEqual(11);
        var handled = RewardGrantHandlerRegistry.CreateDefault().HandledGrantTypes;
        AssertThat(handled).HasSize(11);
        foreach (var grantType in grantTypes)
        {
            AssertThat(grantType.IsSubclassOf(typeof(RewardGrantDefinition))).IsTrue();
            AssertThat(handled.Contains(grantType)).IsTrue();
        }
    }

    [TestCase]
    public void URS_C15_CardInstanceGrantMutatesOnlyItsExplicitTarget()
    {
        var first = new CardInstance
        {
            Id = new CardInstanceId("first"),
            CatalogId = CardIds.FireWisp,
        };
        var second = new CardInstance
        {
            Id = new CardInstanceId("second"),
            CatalogId = CardIds.FireWisp,
        };
        var profile = new ProfileData { Collection = [first, second] };
        var preparation = RewardGrantHandlerRegistry
            .CreateDefault()
            .Prepare(
                new CardExperienceRewardGrantDefinition
                {
                    Amount = 25,
                    Target = new RewardOwnershipTarget(RewardOwnershipScope.CardInstance, "first"),
                },
                new RewardGrantContext
                {
                    ClaimId = new RewardClaimId("claim"),
                    Source = new RewardSourceContext { SourceType = "test", SourceId = "source" },
                }
            );

        AssertThat(preparation.IsValid).IsTrue();
        AssertThat(preparation.Mutation!.TryApply(profile, out _)).IsTrue();
        AssertThat(first.Xp).IsEqual(25);
        AssertThat(second.Xp).IsEqual(0);
    }

    [TestCase]
    public void CardGrantCanExplicitlyPlaceCreatedInstancesInSelectedSummonerDeck()
    {
        var summonerId = SummonerIds.Cole;
        var deckId = new DeckId("selected");
        var profile = new ProfileData
        {
            Meta = new() { SelectedDeck = deckId.Value },
            Decks =
            [
                new Deck
                {
                    Id = deckId,
                    SummonerId = summonerId,
                    Name = "Tutorial",
                },
            ],
        };
        var preparation = RewardGrantHandlerRegistry
            .CreateDefault()
            .Prepare(
                new CardRewardGrantDefinition
                {
                    CardId = CardIds.FireWisp,
                    Count = 2,
                    Placement = CardRewardPlacement.SelectedDeckIfAvailable,
                    Target = new RewardOwnershipTarget(
                        RewardOwnershipScope.Summoner,
                        summonerId.Value
                    ),
                },
                new RewardGrantContext
                {
                    ClaimId = new RewardClaimId("claim"),
                    Source = new RewardSourceContext { SourceType = "test", SourceId = "source" },
                }
            );

        AssertThat(preparation.IsValid).IsTrue();
        AssertThat(preparation.Mutation!.TryApply(profile, out _)).IsTrue();
        AssertThat(profile.Collection).HasSize(2);
        AssertThat(profile.Decks[0].CardInstanceIds)
            .ContainsExactly(profile.Collection.Select(card => card.Id));
    }

    [TestCase]
    public void InvalidResourceAndUnlockOwnershipFailBeforeMutation()
    {
        var registry = RewardGrantHandlerRegistry.CreateDefault();
        var context = new RewardGrantContext
        {
            ClaimId = new RewardClaimId("claim"),
            Source = new RewardSourceContext { SourceType = "test", SourceId = "source" },
        };

        var undefinedResource = registry.Prepare(
            new ResourceRewardGrantDefinition
            {
                ResourceId = "999",
                Amount = 10,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
            },
            context
        );
        var wrongUnlockScope = registry.Prepare(
            new SummonerUnlockRewardGrantDefinition
            {
                SummonerId = Fateforged.Data.Summoners.SummonerIds.Cole,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Summoner, "summoner"),
            },
            context
        );

        AssertThat(undefinedResource.IsValid).IsFalse();
        AssertThat(wrongUnlockScope.IsValid).IsFalse();
    }

    [TestCase]
    public void ItemRewardsEnforceDefinitionOwnershipTargets()
    {
        var registry = RewardGrantHandlerRegistry.CreateDefault();
        var context = new RewardGrantContext
        {
            ClaimId = new RewardClaimId("claim"),
            Source = new RewardSourceContext { SourceType = "test", SourceId = "source" },
        };

        var missingOwner = registry.Prepare(
            new ItemRewardGrantDefinition
            {
                ItemId = ItemIds.TrainingBlade,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
            },
            context
        );
        var normal = registry.Prepare(
            new ItemRewardGrantDefinition
            {
                ItemId = ItemIds.TrainingBlade,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Summoner, SummonerIds.Cole.Value),
            },
            context
        );
        var sharedEvent = registry.Prepare(
            new ItemRewardGrantDefinition
            {
                ItemId = ItemIds.VeteransMedal,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
            },
            context
        );

        AssertThat(missingOwner.IsValid).IsFalse();
        AssertThat(normal.IsValid).IsTrue();
        AssertThat(sharedEvent.IsValid).IsTrue();
    }
}
