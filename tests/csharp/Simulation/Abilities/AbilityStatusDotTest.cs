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
public class AbilityStatusDotTest
{
    [TestCase]
    public void PoisonProjectile_ReapplyStacksPotency_UpToMaxStacks()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var attacker = SimTestHelper.CreateRangedUnit(state, 0, x: 0f, z: 0f, damage: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 6f, z: 0f, hp: 300f);

        for (int i = 0; i < 4; i++)
        {
            SimProjectile.Spawn(
                state,
                sourceUnitId: attacker.UnitId,
                targetUnitId: target.UnitId,
                team: attacker.Team,
                damage: 0f,
                sourceElementId: 0,
                movementType: ProjectileMovementType.Straight,
                speed: 30f,
                lifetime: 2f,
                startPos: attacker.Position,
                targetPos: target.Position,
                hitRadius: 0.5f,
                projectileCatalogId: (string)ProjectileIds.PoisonNeedle,
                targetAffinity: AbilityTargetAffinity.Enemies,
                impactKind: ProjectileImpactKind.Damage,
                statusKind: StatusEffectKind.Poison,
                statusDuration: 4f,
                statusTickInterval: 1f,
                statusPotencyPerStack: 2f,
                statusMaxStacks: 3
            );

            for (int step = 0; step < 240 && state.Projectiles.Count > 0; step++)
                SimProjectile.TickAll(state, Simulation.FixedDeltaSeconds, events);
        }

        var poison = target.ActiveBuffs.FirstOrDefault(b => b.StatusKind == StatusEffectKind.Poison);
        AssertThat(poison).IsNotNull();
        AssertThat(poison!.StackCount).IsEqual(3);
        AssertThat(poison.Value).IsEqual(6f);
        AssertThat(events.OfType<StatusAppliedEvent>().Count()).IsGreater(0);
    }
}
