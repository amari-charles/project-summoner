namespace Fateforged.Tests.Meta.Rewards;

using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RewardViewModelFactoryTest
{
    [TestCase]
    public void URS_C20_ViewModelCarriesNormalizedPendingAndClaimedState()
    {
        var offer = new RewardOfferDefinition
        {
            Id = new RewardOfferId("preview_offer"),
            PreviewPolicy = RewardPreviewPolicy.CategoryUntilEarned,
            Selection = new RewardSelectionRule
            {
                Mode = RewardSelectionMode.PlayerChoice,
                ShowCount = 3,
                ChooseCount = 1,
            },
            OptionSource = new AuthoredRewardOptionSourceDefinition(),
        };
        var claimId = new RewardClaimId("claim");
        var option = new RewardOptionDefinition
        {
            Id = new RewardOptionId("option"),
            LabelKey = "reward.option",
            Grants =
            [
                new ResourceRewardGrantDefinition
                {
                    ResourceId = "gold",
                    Amount = 25,
                    Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
                },
            ],
        };
        var snapshot = new ResolvedRewardOfferSnapshot
        {
            ClaimId = claimId,
            OfferId = offer.Id,
            Source = new RewardSourceContext { SourceType = "test", SourceId = "source" },
            SummonerId = new SummonerId("summoner"),
            SelectionMode = RewardSelectionMode.PlayerChoice,
            ChooseCount = 1,
            Options = [option],
        };
        var pending = new PendingRewardSelection
        {
            ClaimId = claimId,
            ChooseCount = 1,
            SelectedOptionIds = [option.Id],
        };
        var receipt = new RewardClaimReceipt
        {
            ClaimId = claimId,
            ClaimedOptionIds = [option.Id],
            AppliedGrants = option.Grants,
        };
        var factory = new RewardViewModelFactory();
        var previewView = factory.Create(offer, snapshot: null);
        var pendingView = factory.Create(offer, snapshot, pending);
        var claimedView = factory.Create(offer, snapshot, receipt: receipt);

        AssertThat(previewView.Status).IsEqual(RewardRuntimeStatus.Ready);
        AssertThat(previewView.DisplayState).IsEqual(RewardOfferDisplayState.Preview);
        AssertThat(pendingView.Id).IsEqual(offer.Id);
        AssertThat(pendingView.Status).IsEqual(RewardRuntimeStatus.Ready);
        AssertThat(pendingView.DisplayState).IsEqual(RewardOfferDisplayState.Pending);
        AssertThat(pendingView.CategoryKey).IsEmpty();
        AssertThat(pendingView.Options[0].IsSelected).IsTrue();
        AssertThat(pendingView.Options[0].Grants[0].Kind).IsEqual("resource");
        AssertThat(pendingView.Options[0].Grants[0].ContentId).IsEqual("gold");
        AssertThat(pendingView.Options[0].Grants[0].Amount).IsEqual(25);
        AssertThat(claimedView.Status).IsEqual(RewardRuntimeStatus.AlreadyClaimed);
        AssertThat(claimedView.DisplayState).IsEqual(RewardOfferDisplayState.Claimed);
        AssertThat(claimedView.Options[0].IsSelected).IsTrue();
        AssertThat(claimedView.Receipt).IsEqual(receipt);
    }
}
