namespace Fateforged.Tests.Serialization;

using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Items;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Infrastructure.Persistence;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class RewardPersistenceTest
{
    [TestCase]
    public void URS_C07_C10_D05_ResolvedPromisePendingChoiceAndSeedRoundTripUnchanged()
    {
        var claimId = new RewardClaimId("summoner:quest:encounter:offer");
        var grant = new ResourceRewardGrantDefinition
        {
            ResourceId = "gold",
            Amount = 75,
            Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
        };
        var option = new RewardOptionDefinition
        {
            Id = new RewardOptionId("gold_option"),
            LabelKey = "reward.gold",
            Grants = [grant],
        };
        var profile = new ProfileData { ProfileId = new ProfileId("reward_roundtrip") };
        profile.Rewards.RewardSeedBySummoner["summoner_test"] = 42;
        profile.Rewards.ResolvedOffers[claimId.Value] = new ResolvedRewardOfferSnapshot
        {
            ClaimId = claimId,
            OfferId = new RewardOfferId("offer"),
            Source = new RewardSourceContext
            {
                SourceType = "encounter",
                SourceId = "quest",
                OccurrenceId = "encounter",
            },
            SummonerId = new SummonerId("summoner_test"),
            SelectionMode = RewardSelectionMode.PlayerChoice,
            ChooseCount = 1,
            Options = [option],
        };
        profile.Rewards.PendingSelections[claimId.Value] = new PendingRewardSelection
        {
            ClaimId = claimId,
            ChooseCount = 1,
            SelectedOptionIds = [option.Id],
        };

        var restored = ProfileDataMapper.FromDictionary(
            ProfileDataMapper.ToDictionary(profile),
            (string)profile.ProfileId
        );
        var restoredSnapshot = restored.Rewards.ResolvedOffers[claimId.Value];
        var restoredGrant = (ResourceRewardGrantDefinition)restoredSnapshot.Options[0].Grants[0];

        AssertThat(restored.Rewards.RewardSeedBySummoner["summoner_test"]).IsEqual(42UL);
        AssertThat(restoredSnapshot.Options[0].Id).IsEqual(option.Id);
        AssertThat(restoredGrant.ResourceId).IsEqual("gold");
        AssertThat(restoredGrant.Amount).IsEqual(75);
        AssertThat(restoredGrant.Target.Scope).IsEqual(RewardOwnershipScope.Account);
        AssertThat(restored.Rewards.PendingSelections[claimId.Value].SelectedOptionIds[0])
            .IsEqual(option.Id);
    }

    [TestCase]
    public void URS_C16_AllGrantDiscriminatorsRoundTripAsTheirConcreteTypes()
    {
        RewardGrantDefinition[] grants =
        [
            new CardRewardGrantDefinition
            {
                CardId = CardIds.FireWisp,
                Placement = CardRewardPlacement.SelectedDeckIfAvailable,
                Target = Account(),
            },
            new ResourceRewardGrantDefinition
            {
                ResourceId = "gold",
                Amount = 1,
                Target = Account(),
            },
            new ItemRewardGrantDefinition { ItemId = new ItemId("item"), Target = Account() },
            new SummonerUnlockRewardGrantDefinition
            {
                SummonerId = SummonerIds.Cole,
                Target = Account(),
            },
            new CosmeticRewardGrantDefinition { CosmeticId = "cosmetic", Target = Account() },
            new EmoteRewardGrantDefinition { EmoteId = "emote", Target = Account() },
            new SummonerExperienceRewardGrantDefinition
            {
                Amount = 1,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Summoner, "summoner"),
            },
            new CardExperienceRewardGrantDefinition
            {
                Amount = 1,
                Target = new RewardOwnershipTarget(RewardOwnershipScope.CardInstance, "card"),
            },
            new SummonerTraitRewardGrantDefinition
            {
                TraitId = new TraitId("trait"),
                Target = new RewardOwnershipTarget(RewardOwnershipScope.Summoner, "summoner"),
            },
            new CardTraitRewardGrantDefinition
            {
                TraitId = new CardTraitId("card_trait"),
                Target = new RewardOwnershipTarget(RewardOwnershipScope.CardInstance, "card"),
            },
        ];
        var profile = new ProfileData();
        profile.Rewards.ResolvedOffers["claim"] = new ResolvedRewardOfferSnapshot
        {
            ClaimId = new RewardClaimId("claim"),
            OfferId = new RewardOfferId("offer"),
            Source = new RewardSourceContext { SourceType = "test", SourceId = "source" },
            SummonerId = new SummonerId("summoner"),
            Options =
            [
                new RewardOptionDefinition
                {
                    Id = new RewardOptionId("option"),
                    Grants = [.. grants],
                },
            ],
        };

        var restored = ProfileDataMapper.FromDictionary(
            ProfileDataMapper.ToDictionary(profile),
            "profile"
        );
        var restoredTypes = restored
            .Rewards.ResolvedOffers["claim"]
            .Options[0]
            .Grants.Select(grant => grant.GetType())
            .ToArray();

        AssertThat(restoredTypes).ContainsExactly(grants.Select(grant => grant.GetType()));
        AssertThat(
                (
                    (CardRewardGrantDefinition)
                        restored.Rewards.ResolvedOffers["claim"].Options[0].Grants[0]
                ).Placement
            )
            .IsEqual(CardRewardPlacement.SelectedDeckIfAvailable);
    }

    [TestCase]
    public void MalformedPersistedGrantInvalidatesTheEntireResolvedPromise()
    {
        var profile = new ProfileData();
        profile.Rewards.ResolvedOffers["claim"] = new ResolvedRewardOfferSnapshot
        {
            ClaimId = new RewardClaimId("claim"),
            OfferId = new RewardOfferId("offer"),
            Source = new RewardSourceContext { SourceType = "test", SourceId = "source" },
            SummonerId = new SummonerId("summoner"),
            SelectionMode = RewardSelectionMode.PlayerChoice,
            ChooseCount = 1,
            Options =
            [
                new RewardOptionDefinition
                {
                    Id = new RewardOptionId("option"),
                    Grants =
                    [
                        new ResourceRewardGrantDefinition
                        {
                            ResourceId = "gold",
                            Amount = 10,
                            Target = Account(),
                        },
                    ],
                },
            ],
        };
        var persisted = ProfileDataMapper.ToDictionary(profile);
        var rewards = persisted["rewards"].AsGodotDictionary();
        var resolved = rewards["resolved_offers"].AsGodotDictionary();
        var snapshot = resolved["claim"].AsGodotDictionary();
        var option = snapshot["options"].AsGodotArray()[0].AsGodotDictionary();
        option["grants"].AsGodotArray()[0].AsGodotDictionary()["kind"] = "unknown_grant";

        var restored = ProfileDataMapper.FromDictionary(persisted, "profile");

        AssertThat(restored.Rewards.ResolvedOffers).IsEmpty();
    }

    private static RewardOwnershipTarget Account() => new(RewardOwnershipScope.Account);
}
