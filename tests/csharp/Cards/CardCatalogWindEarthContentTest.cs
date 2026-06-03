namespace Fateforged.Tests.Cards;

using System.Linq;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using GdUnit4;
using Godot;
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
            CardIds.Tornado,
            CardIds.Crosswind,
            CardIds.AirBullet,
            CardIds.Evacuate,
            CardIds.WindShear,
            CardIds.WindDiver,
            CardIds.WindSpeedSupport,
            CardIds.WindMissSupport,
            CardIds.WindSwarm,
            CardIds.DashStriker,
        };
        var earthCards = new[]
        {
            CardIds.EarthWisp,
            CardIds.EarthFlatDamageReductionTank,
            CardIds.EarthBulletUnit,
            CardIds.Fortify,
            CardIds.Quake,
            CardIds.StoneSpike,
            CardIds.GravityWell,
            CardIds.ReformEarth,
            CardIds.EarthenGrip,
            CardIds.EarthShieldSupport,
            CardIds.BurrowAmbusher,
        };

        foreach (var cardId in windCards)
        {
            var card = CardCatalog.GetCard(cardId);
            AssertThat(card).IsNotNull();
            AssertThat(card!.ElementalAffinity).IsEqual(Element.Wind);
            AssertThat(card.UnlockCondition).IsEqual(UnlockCondition.Default);
            AssertThat((card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) == 0).IsTrue();
        }

        foreach (var cardId in earthCards)
        {
            var card = CardCatalog.GetCard(cardId);
            AssertThat(card).IsNotNull();
            AssertThat(card!.ElementalAffinity).IsEqual(Element.Earth);
            AssertThat(card.UnlockCondition).IsEqual(UnlockCondition.Default);
            AssertThat((card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) == 0).IsTrue();
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
            UnitIds.EarthShieldSupport,
            UnitIds.BurrowAmbusher,
            UnitIds.WindDiver,
            UnitIds.WindSpeedSupport,
            UnitIds.WindMissSupport,
            UnitIds.WindSwarmUnit,
            UnitIds.DashStriker,
        };

        foreach (var unitId in placeholderUnitIds)
        {
            var def = UnitDefinitions.Get(unitId);
            AssertThat(def).IsNotNull();
            AssertThat(def!.ScenePath.Contains("_placeholder_3d.tscn")).IsTrue();
            AssertThat(ResourceLoader.Exists(def.ScenePath)).IsTrue();
        }

        var pushback = UnitDefinitions.Get(UnitIds.WindPushbackUnit);
        AssertThat(pushback).IsNotNull();
        var knockbackAbility = pushback!.Abilities.FirstOrDefault(a =>
            a.Trigger == UnitAbilityTrigger.OnHit
            && a.Targeting == UnitAbilityTargeting.HitTarget
            && a.Effects.Any(e => e.EffectType == EffectType.Knockback)
        );
        AssertThat(knockbackAbility).IsNotNull();
        AssertThat(knockbackAbility!.Range).IsGreaterEqual(pushback.Stats.AttackRange);
        AssertThat(knockbackAbility.CooldownSeconds).IsLessEqual(0f);

        var earthBullet = UnitDefinitions.Get(UnitIds.EarthBulletUnit);
        AssertThat(earthBullet).IsNotNull();
        var slowAbility = earthBullet!.Abilities.FirstOrDefault(a =>
            a.Trigger == UnitAbilityTrigger.OnHit
            && a.Targeting == UnitAbilityTargeting.HitTarget
            && a.Effects.Any(e => e.EffectType == EffectType.Slow)
        );
        AssertThat(slowAbility).IsNotNull();
    }
}
