namespace Fateforged.Tests.Cards;

using System.Linq;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CardCatalogWindEarthContentTest
{
    [TestCase]
    public void WindEarthContentCards_Registered()
    {
        var windCards = new[]
        {
            CardIds.WindWisp,
            CardIds.Puff,
            CardIds.WindEvasionTank,
            CardIds.WindPushbackUnit,
            CardIds.WindCleaveUnit,
            CardIds.TailWind,
        };
        var earthCards = new[]
        {
            CardIds.EarthWisp,
            CardIds.EarthFlatDamageReductionTank,
            CardIds.EarthBulletUnit,
            CardIds.Fortify,
        };

        foreach (var cardId in windCards)
        {
            var card = CardCatalog.GetCard(cardId);
            AssertThat(card).IsNotNull();
            AssertThat(card!.ElementalAffinity).IsEqual(Element.Wind);
        }

        foreach (var cardId in earthCards)
        {
            var card = CardCatalog.GetCard(cardId);
            AssertThat(card).IsNotNull();
            AssertThat(card!.ElementalAffinity).IsEqual(Element.Earth);
        }
    }

    [TestCase]
    public void WindEarthUnits_UsePlaceholderScenes_AndPushbackHasReach()
    {
        var placeholderUnitIds = new[]
        {
            UnitIds.WindEvasionTank,
            UnitIds.WindPushbackUnit,
            UnitIds.WindCleaveUnit,
            UnitIds.EarthFlatDamageReductionTank,
            UnitIds.EarthBulletUnit,
        };

        foreach (var unitId in placeholderUnitIds)
        {
            var def = UnitDefinitions.Get(unitId);
            AssertThat(def).IsNotNull();
            AssertThat(def!.ScenePath.Contains("_placeholder_3d.tscn")).IsTrue();
        }

        var pushback = UnitDefinitions.Get(UnitIds.WindPushbackUnit);
        AssertThat(pushback).IsNotNull();
        var knockbackAbility = pushback!.Abilities.FirstOrDefault(a =>
            a.Kind == UnitAbilityKind.TargetedKnockback
        );
        AssertThat(knockbackAbility).IsNotNull();
        AssertThat(knockbackAbility!.Range).IsGreaterEqual(pushback.Stats.AttackRange);
        AssertThat(knockbackAbility.CooldownSeconds).IsLessEqual(0f);
    }
}
