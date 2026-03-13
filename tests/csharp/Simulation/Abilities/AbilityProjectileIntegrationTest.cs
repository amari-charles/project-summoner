namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityProjectileIntegrationTest
{
    [TestCase]
    public void RockThrower_ArcProjectile_LandsAfterFlight()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var attacker = SimTestHelper.CreateRangedUnit(
            state,
            0,
            x: 0f,
            z: 0f,
            damage: 18f,
            attackSpeed: 1f,
            attackRange: 24f,
            projectileDelay: 0f,
            catalogId: "earth_rock_thrower"
        );
        attacker.ProjectileCatalogId = (string)ProjectileIds.Rock;
        attacker.TargetUnitId = null;

        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 16f, z: 0f, hp: 140f);
        attacker.TargetUnitId = target.UnitId;

        SimBehavior.TickBehavior(attacker, state, Simulation.FixedDeltaSeconds, events);
        AssertThat(state.Projectiles.Count).IsEqual(1);
        var projectile = state.Projectiles.Values.First();
        AssertThat(projectile.MovementType).IsEqual(ProjectileMovementType.Arc);

        float hpAfterAttackFrame = target.CurrentHp;
        SimProjectile.TickAll(state, Simulation.FixedDeltaSeconds, events);
        AssertThat(target.CurrentHp).IsEqual(hpAfterAttackFrame);

        for (int i = 0; i < 360 && state.Projectiles.Count > 0; i++)
            SimProjectile.TickAll(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(target.CurrentHp).IsLess(hpAfterAttackFrame);
    }

    [TestCase]
    public void LegacyMeleeAttack_NoAbilityLoadout_StillDamagesTarget()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, damage: 12f, attackSpeed: 1f, attackRange: 3f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 1f, z: 0f, hp: 100f);
        attacker.TargetUnitId = target.UnitId;

        float before = target.CurrentHp;
        SimBehavior.TickBehavior(attacker, state, Simulation.FixedDeltaSeconds, events);
        AssertThat(target.CurrentHp).IsLess(before);
    }
}
