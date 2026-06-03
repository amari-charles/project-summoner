namespace Fateforged.Tests.Cards;

using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Units;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CardCatalogFireWaterRosterContentTest
{
    [TestCase]
    public void FireWaterRosterCards_RegisteredAndShipEligible()
    {
        var fireCards = new[]
        {
            CardIds.Fireball,
            CardIds.FireAreaBurn,
            CardIds.BurnCashout,
            CardIds.Overheat,
            CardIds.IgnitionMark,
            CardIds.FlareShield,
            CardIds.CinderCaster,
            CardIds.EmberBombCarrier,
            CardIds.KindlingSwarm,
            CardIds.FireFrontliner,
            CardIds.OverheatBrawler,
            CardIds.FlameChanneler,
        };
        var waterCards = new[]
        {
            CardIds.BubbleShield,
            CardIds.Whirlpool,
            CardIds.Flow,
            CardIds.WaterRedistributor,
            CardIds.SlipperyMelee,
            CardIds.WaterRanged,
            CardIds.BarbedInflator,
        };

        foreach (var cardId in fireCards)
            AssertCard(cardId, Element.Fire);

        foreach (var cardId in waterCards)
            AssertCard(cardId, Element.Water);
    }

    [TestCase]
    public void FireWaterRosterUnits_ResolveToValidScenes()
    {
        var unitIds = new[]
        {
            UnitIds.CinderCaster,
            UnitIds.EmberBombCarrier,
            UnitIds.KindlingSwarmUnit,
            UnitIds.FireFrontliner,
            UnitIds.OverheatBrawler,
            UnitIds.FlameChanneler,
            UnitIds.WaterRedistributor,
            UnitIds.SlipperyMelee,
            UnitIds.WaterRanged,
            UnitIds.BarbedInflator,
        };

        foreach (var unitId in unitIds)
        {
            var def = UnitDefinitions.Get(unitId);
            AssertThat(def).IsNotNull();
            AssertThat(def!.ScenePath.Contains("_placeholder_3d.tscn")).IsTrue();
            AssertThat(ResourceLoader.Exists(def.ScenePath)).IsTrue();
        }
    }

    private static void AssertCard(CardId cardId, Element element)
    {
        var card = CardCatalog.GetCard(cardId);
        AssertThat(card).IsNotNull();
        AssertThat(card!.ElementalAffinity).IsEqual(element);
        AssertThat(card.UnlockCondition).IsEqual(UnlockCondition.Default);
        AssertThat((card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) == 0).IsTrue();
    }
}
