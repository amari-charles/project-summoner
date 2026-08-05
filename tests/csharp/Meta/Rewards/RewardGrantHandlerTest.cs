namespace Fateforged.Tests.Meta.Rewards;

using System;
using Fateforged.Data.Rewards;
using Fateforged.Cards;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Rewards;
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
                    Target = new RewardOwnershipTarget(
                        RewardOwnershipScope.CardInstance,
                        "first"
                    ),
                },
                new RewardGrantContext
                {
                    ClaimId = new RewardClaimId("claim"),
                    Source = new RewardSourceContext
                    {
                        SourceType = "test",
                        SourceId = "source",
                    },
                }
            );

        AssertThat(preparation.IsValid).IsTrue();
        AssertThat(preparation.Mutation!.TryApply(profile, out _)).IsTrue();
        AssertThat(first.Xp).IsEqual(25);
        AssertThat(second.Xp).IsEqual(0);
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
                Target = new RewardOwnershipTarget(
                    RewardOwnershipScope.Summoner,
                    "summoner"
                ),
            },
            context
        );

        AssertThat(undefinedResource.IsValid).IsFalse();
        AssertThat(wrongUnlockScope.IsValid).IsFalse();
    }
}
