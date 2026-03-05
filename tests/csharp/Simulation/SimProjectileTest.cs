namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimProjectileTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void TickAll_UsesTargetSeparationRadiusForContact()
    {
        var source = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, z: 0f);
        source.ElementId = 0;
        source.CritChance = 0f;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 1.5f, z: 0.4f, hp: 100f);
        target.Evasion = 0f;
        target.SeparationRadius = 0.5f;

        SimProjectile.Spawn(
            _state,
            sourceUnitId: source.UnitId,
            targetUnitId: target.UnitId,
            team: source.Team,
            damage: 20f,
            sourceElementId: source.ElementId,
            movementType: ProjectileMovementType.Straight,
            speed: 10f,
            lifetime: 3f,
            startPos: new SimVector3(0f, 0f, 0f),
            targetPos: new SimVector3(3f, 0f, 0f),
            hitRadius: 0.1f,
            hitSpace: ProjectileHitSpace.GroundCylinder);

        var events = new List<SimEvent>();
        SimProjectile.TickAll(_state, 0.016f, events); // Spawn delay frame
        SimProjectile.TickAll(_state, 0.2f, events);   // Movement + collision

        AssertThat(target.CurrentHp).IsLess(100f);
        AssertThat(SimTestHelper.CountEvents<UnitDamagedEvent>(events)).IsEqual(1);
    }

    [TestCase]
    public void TickAll_DoesNotHitSameUnitTwice()
    {
        var source = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, z: 0f);
        source.ElementId = 0;
        source.CritChance = 0f;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 1f, z: 0f, hp: 200f);
        target.Evasion = 0f;
        target.SeparationRadius = 0.5f;

        SimProjectile.Spawn(
            _state,
            sourceUnitId: source.UnitId,
            targetUnitId: target.UnitId,
            team: source.Team,
            damage: 15f,
            sourceElementId: source.ElementId,
            movementType: ProjectileMovementType.Straight,
            speed: 1f,
            lifetime: 6f,
            startPos: new SimVector3(0f, 0f, 0f),
            targetPos: new SimVector3(10f, 0f, 0f),
            pierceCount: 5,
            hitRadius: 1.2f,
            hitSpace: ProjectileHitSpace.GroundCylinder);

        var events = new List<SimEvent>();
        SimProjectile.TickAll(_state, 0.016f, events); // Spawn delay frame
        for (int i = 0; i < 30; i++)
            SimProjectile.TickAll(_state, 0.1f, events);

        int targetDamageEvents = 0;
        foreach (var e in events)
        {
            if (e is UnitDamagedEvent damaged && damaged.TargetUnitId == target.UnitId)
                targetDamageEvents++;
        }

        AssertThat(targetDamageEvents).IsEqual(1);
    }

    [TestCase]
    public void TickAll_NoPierce_HitsNearestContactFirst()
    {
        var source = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, z: 0f);
        source.ElementId = 0;
        source.CritChance = 0f;

        // Insert farther enemy first to validate deterministic nearest-contact ordering.
        var farther = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, z: 0.1f, hp: 100f);
        farther.Evasion = 0f;
        farther.SeparationRadius = 0.2f;

        var nearer = SimTestHelper.CreateMeleeUnit(_state, 1, x: 1f, z: 0.1f, hp: 100f);
        nearer.Evasion = 0f;
        nearer.SeparationRadius = 0.2f;

        SimProjectile.Spawn(
            _state,
            sourceUnitId: source.UnitId,
            targetUnitId: farther.UnitId,
            team: source.Team,
            damage: 20f,
            sourceElementId: source.ElementId,
            movementType: ProjectileMovementType.Straight,
            speed: 40f,
            lifetime: 2f,
            startPos: new SimVector3(0f, 0f, 0f),
            targetPos: new SimVector3(3f, 0f, 0f),
            pierceCount: 0,
            hitRadius: 0.25f,
            hitSpace: ProjectileHitSpace.GroundCylinder);

        var events = new List<SimEvent>();
        SimProjectile.TickAll(_state, 0.016f, events); // Spawn delay frame
        SimProjectile.TickAll(_state, 0.2f, events);   // Covers both units in one segment

        AssertThat(nearer.CurrentHp).IsLess(nearer.MaxHp);
        AssertThat(farther.CurrentHp).IsEqual(farther.MaxHp);

        var hitEvent = SimTestHelper.FindEvent<ProjectileHitEvent>(events);
        AssertThat(hitEvent).IsNotNull();
        AssertThat(hitEvent!.TargetUnitId).IsEqual(nearer.UnitId);
    }
}
