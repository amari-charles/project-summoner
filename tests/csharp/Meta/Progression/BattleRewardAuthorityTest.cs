namespace Fateforged.Tests.Meta.Progression;

using System.Linq;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Progression;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class BattleRewardAuthorityTest
{
    [TestCase]
    public void BPA_C15_LegacyAuthoredBattleRewardsNormalizeToUniversalOffers()
    {
        var flexibleBattle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.FirstTrial)!;
        var flexible = flexibleBattle.FirstClearRewardOffers.Single();
        var fixedBattle = EventCatalog
            .GetAllBattles()
            .First(value =>
                value.FirstClearRewardOffers.Any(offer =>
                    offer.Selection.Mode == RewardSelectionMode.Automatic
                )
            );
        var fixedOffer = fixedBattle.FirstClearRewardOffers.Single();
        var noRewardBattle = EventCatalog
            .GetAllBattles()
            .First(value => value.FirstClearRewardOffers.IsEmpty);

        AssertThat(flexible.Selection.Mode).IsEqual(RewardSelectionMode.PlayerChoice);
        AssertThat(flexible.OptionSource).IsInstanceOf<AuthoredRewardOptionSourceDefinition>();
        AssertThat(fixedOffer.Selection.Mode).IsEqual(RewardSelectionMode.Automatic);
        AssertThat(fixedOffer.OptionSource).IsInstanceOf<AuthoredRewardOptionSourceDefinition>();
        AssertThat(noRewardBattle.FirstClearRewardOffers).IsEmpty();
    }
}
