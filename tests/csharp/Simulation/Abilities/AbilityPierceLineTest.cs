namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityPierceLineTest
{
    [TestCase]
    public void LaserProjectile_PiercesMultipleEnemies()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var attacker = SimTestHelper.CreateRangedUnit(state, 0, x: 0f, z: 0f, damage: 20f);
        var targetA = SimTestHelper.CreateMeleeUnit(state, 1, x: 10f, z: 0f, hp: 100f);
        var targetB = SimTestHelper.CreateMeleeUnit(state, 1, x: 12f, z: 0f, hp: 100f);

        SimProjectile.Spawn(
            state,
            sourceUnitId: attacker.UnitId,
            targetUnitId: targetA.UnitId,
            team: attacker.Team,
            damage: 20f,
            sourceElementId: 0,
            movementType: ProjectileMovementType.Straight,
            speed: 45f,
            lifetime: 1.2f,
            startPos: attacker.Position,
            targetPos: new SimVector3(20f, 0f, 0f),
            hitRadius: 0.45f,
            pierceCount: 3,
            projectileCatalogId: (string)ProjectileIds.LaserBeam,
            targetAffinity: AbilityTargetAffinity.Enemies,
            impactKind: ProjectileImpactKind.Damage
        );

        for (int i = 0; i < 240 && state.Projectiles.Count > 0; i++)
            SimProjectile.TickAll(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(targetA.CurrentHp).IsLess(100f);
        AssertThat(targetB.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void LaserUnitAttack_HitscanBeam_HitsEnemiesBehindPrimaryTargetImmediately()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var attacker = SimTestHelper.CreateRangedUnit(
            state,
            0,
            x: 0f,
            z: 0f,
            damage: 20f,
            attackSpeed: 1f,
            attackRange: 22f,
            projectileDelay: 0f,
            catalogId: "piercing_laser"
        );
        var frontTarget = SimTestHelper.CreateMeleeUnit(state, 1, x: 9f, z: 0f, hp: 100f);
        var behindTarget = SimTestHelper.CreateMeleeUnit(state, 1, x: 17f, z: 0f, hp: 100f);
        attacker.Engagement.TargetUnitId = frontTarget.UnitId;

        float frontBefore = frontTarget.CurrentHp;
        float behindBefore = behindTarget.CurrentHp;
        SimBehavior.TickBehavior(attacker, state, Simulation.FixedDeltaSeconds, events);
        SimBehavior.ResolvePendingAttackCommit(attacker, state, events);

        // Hitscan resolves in the same behavior tick and does not enqueue a traveling projectile.
        AssertThat(state.Projectiles.Count).IsEqual(0);
        AssertThat(frontTarget.CurrentHp).IsLess(frontBefore);
        AssertThat(behindTarget.CurrentHp).IsLess(behindBefore);
        AssertThat(events.OfType<HitscanBeamFiredEvent>().Any()).IsTrue();
    }
}
