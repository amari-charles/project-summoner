namespace Fateforged.Tests.Services;

using System.Linq;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class RewardServiceWindEarthTest
{
    [TestCase]
    public void RewardPools_WindEarth_IncludeNewCommonUnits()
    {
        var windCards = RewardPoolCatalog.GetCardsForPool(RewardPoolIds.WindCommonUnits);
        var earthCards = RewardPoolCatalog.GetCardsForPool(RewardPoolIds.EarthCommonUnits);

        AssertThat(windCards.Length).IsGreater(0);
        AssertThat(earthCards.Length).IsGreater(0);
        AssertThat(windCards.All(c => c.ElementalAffinity == Element.Wind)).IsTrue();
        AssertThat(earthCards.All(c => c.ElementalAffinity == Element.Earth)).IsTrue();

        var windCardIds = windCards.Select(c => (string)c.Id).ToHashSet();
        var earthCardIds = earthCards.Select(c => (string)c.Id).ToHashSet();
        AssertThat(windCardIds.Contains((string)CardIds.WindEvasionTank)).IsTrue();
        AssertThat(windCardIds.Contains((string)CardIds.WindPushbackUnit)).IsTrue();
        AssertThat(windCardIds.Contains((string)CardIds.WindCleaveUnit)).IsTrue();
        AssertThat(earthCardIds.Contains((string)CardIds.EarthFlatDamageReductionTank)).IsTrue();
        AssertThat(earthCardIds.Contains((string)CardIds.EarthBulletUnit)).IsTrue();
    }
}
