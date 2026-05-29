namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class CombatPrimitivesV2Test
{
    [TestCase]
    public void StatusApply_ThenConsume_ConvertsRemainingBurnDamage()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var caster = SimTestHelper.CreateMeleeUnit(state, 0);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 4f, hp: 100f);

        SimEffects.ApplyEffect(
            state,
            EffectType.StatusApply,
            2f,
            4f,
            DamageType.Magic,
            target,
            caster.UnitId,
            caster.Team,
            events,
            StatusEffectKind.Burn,
            1f,
            2f,
            3
        );
        SimEffects.ApplyEffect(
            state,
            EffectType.StatusApply,
            2f,
            4f,
            DamageType.Magic,
            target,
            caster.UnitId,
            caster.Team,
            events,
            StatusEffectKind.Burn,
            1f,
            2f,
            3
        );

        AssertThat(target.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsTrue();

        SimEffects.ApplyEffect(
            state,
            EffectType.StatusConsume,
            1.5f,
            0f,
            DamageType.Magic,
            target,
            caster.UnitId,
            caster.Team,
            events,
            StatusEffectKind.Burn
        );

        AssertThat(target.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsFalse();
        AssertThat(target.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void TransferHealth_MovesHpFromHealthyDonor_ToWoundedReceiver()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var support = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        support.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "transfer_health",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.HealthRedistributionPool,
                Delivery = UnitAbilityDelivery.Instant,
                Radius = 8f,
                Effects =
                [
                    new UnitAbilityEffectState { EffectType = EffectType.TransferHealth, Value = 12f },
                ],
            }
        );

        var donor = SimTestHelper.CreateMeleeUnit(state, 0, x: 2f, hp: 100f);
        donor.CurrentHp = 90f;
        var receiver = SimTestHelper.CreateMeleeUnit(state, 0, x: 3f, hp: 100f);
        receiver.CurrentHp = 30f;

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(donor.CurrentHp).IsLess(90f);
        AssertThat(donor.CurrentHp).IsGreaterEqual(60f);
        AssertThat(receiver.CurrentHp).IsGreater(30f);
    }

    [TestCase]
    public void ReviveOnDeath_RestoresUnitOnce_AndPreventsDeathEvent()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var caster = SimTestHelper.CreateMeleeUnit(state, 0);
        var target = SimTestHelper.CreateMeleeUnit(state, 0, x: 4f, hp: 100f);

        SimEffects.ApplyEffect(
            state,
            EffectType.ReviveOnDeath,
            0.5f,
            5f,
            DamageType.Magic,
            target,
            caster.UnitId,
            caster.Team,
            events
        );
        SimEffects.ApplyEffect(
            state,
            EffectType.Damage,
            200f,
            0f,
            DamageType.True,
            target,
            caster.UnitId,
            caster.Team,
            events
        );

        AssertThat(target.IsAlive).IsTrue();
        AssertThat(target.CurrentHp).IsEqual(50f);
        AssertThat(events.OfType<UnitDiedEvent>().Any(e => e.UnitId == target.UnitId)).IsFalse();
    }

    [TestCase]
    public void CenterDisplacement_PushesAwayFromProvidedOrigin()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var caster = SimTestHelper.CreateMeleeUnit(state, 0, x: -10f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);

        SimEffects.ApplyEffect(
            state,
            EffectType.Displacement,
            3f,
            0f,
            DamageType.Magic,
            target,
            caster.UnitId,
            caster.Team,
            events,
            sourcePosition: SimVector3.Zero
        );

        AssertThat(target.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(target.KnockbackDirection.X).IsGreater(0f);
    }

    [TestCase]
    public void AccuracyAndRangedDamageModifiers_AffectDamageCalculation()
    {
        var state = SimTestHelper.CreateBattleState(seed: 1);
        var events = new List<SimEvent>();
        var ranged = SimTestHelper.CreateRangedUnit(state, 0, damage: 100f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, hp: 200f);

        ranged.ActiveBuffs.Add(
            new ActiveBuff { EffectType = EffectType.RangedDamageModifier, Value = -0.5f }
        );
        var (reducedDamage, _, _) = SimDamage.CalculateAttack(
            100f,
            ranged,
            target,
            state.Summoners[0],
            state.Summoners[1],
            state.Rng,
            events
        );
        AssertThat(reducedDamage).IsLess(100f);

        ranged.ActiveBuffs.Add(
            new ActiveBuff { EffectType = EffectType.AccuracyModifier, Value = -1f }
        );
        var (missedDamage, _, wasEvaded) = SimDamage.CalculateAttack(
            100f,
            ranged,
            target,
            state.Summoners[0],
            state.Summoners[1],
            state.Rng,
            events
        );
        AssertThat(wasEvaded).IsTrue();
        AssertThat(missedDamage).IsEqual(0f);
    }

    [TestCase]
    public void SpellAreaResolver_LineAndCone_SelectDirectionalTargets()
    {
        var origin = SimVector3.Zero;
        var targetPoint = new SimVector3(10f, 0f, 0f);
        var inLine = new SimVector3(5f, 0f, 0.5f);
        var outsideLine = new SimVector3(5f, 0f, 3f);
        var inCone = new SimVector3(5f, 0f, 2f);
        var behind = new SimVector3(-2f, 0f, 0f);

        AssertThat(
                SpellAreaResolver.IsWithinArea(
                    SpellAreaShape.Line,
                    targetPoint,
                    inLine,
                    10f,
                    origin
                )
            )
            .IsTrue();
        AssertThat(
                SpellAreaResolver.IsWithinArea(
                    SpellAreaShape.Line,
                    targetPoint,
                    outsideLine,
                    10f,
                    origin
                )
            )
            .IsFalse();
        AssertThat(
                SpellAreaResolver.IsWithinArea(
                    SpellAreaShape.Cone,
                    targetPoint,
                    inCone,
                    10f,
                    origin
                )
            )
            .IsTrue();
        AssertThat(
                SpellAreaResolver.IsWithinArea(
                    SpellAreaShape.Cone,
                    targetPoint,
                    behind,
                    10f,
                    origin
                )
            )
            .IsFalse();
    }
}
