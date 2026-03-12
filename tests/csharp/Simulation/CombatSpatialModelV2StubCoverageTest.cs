namespace Fateforged.Tests.Simulation;

using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Geometry;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class CombatSpatialModelV2StubCoverageTest
{
    [TestCase]
    public void CSM_001_GeometryChannels_DoNotFallbackToSeparationRadius()
    {
        var unit = new UnitData
        {
            SeparationRadius = 0.9f,
            NavigationRadius = 0f,
            HurtboxRadius = 0f,
        };

        AssertThat(CombatGeometry.GetNavigationRadius(unit)).IsEqual(0.5f);
        AssertThat(CombatGeometry.GetHurtboxRadius(unit)).IsEqual(0.5f);
    }

    [TestCase]
    public void CSM_002_EngageArcDepthGate_OutOfArcRejected()
    {
        var attacker = new UnitData
        {
            Position = new SimVector3(0f, 0f, 0f),
            IsFacingRight = true,
            HasConeConstraint = true,
            ConeHalfAngle = 20f,
            CloseRangeThreshold = 0.5f,
        };
        var target = new UnitData { Position = new SimVector3(-5f, 0f, 3f) };

        AssertThat(SimTargeting.CanAttack(attacker, target)).IsFalse();
    }

    [TestCase]
    public void CSM_003_EngageArcDepthGate_ForwardTargetAccepted()
    {
        var attacker = new UnitData
        {
            Position = new SimVector3(0f, 0f, 0f),
            IsFacingRight = true,
            HasConeConstraint = true,
            ConeHalfAngle = 20f,
            CloseRangeThreshold = 0.5f,
        };
        var target = new UnitData { Position = new SimVector3(5f, 0f, 0f) };

        AssertThat(SimTargeting.CanAttack(attacker, target)).IsTrue();
    }

    [TestCase]
    public void CSM_004_LineDamageShape_ResolvesBeyondEngageDistance()
    {
        var state = SimTestHelper.CreateBattleState();
        var attacker = SimTestHelper.CreateMeleeUnit(
            state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 2.2f
        );
        attacker.IsFacingRight = true;
        attacker.Attack.Selection.Mode = AttackSelectionMode.LineCollect;
        attacker.Attack.Selection.TargetLimit = 3;
        attacker.Attack.Area.LineLength = 6f;
        attacker.Attack.Area.LineHalfWidth = 0.6f;

        var primary = SimTestHelper.CreateMeleeUnit(state, team: 1, x: 1.7f, z: 0f);
        var secondaryInLine = SimTestHelper.CreateMeleeUnit(state, team: 1, x: 5.0f, z: 0.2f);
        var outsideLine = SimTestHelper.CreateMeleeUnit(state, team: 1, x: 3.5f, z: 2.0f);

        var recipients = AttackRecipientResolver.ResolveRecipients(attacker, primary, state);

        AssertThat(recipients.Count).IsEqual(2);
        AssertThat(recipients[0].UnitId).IsEqual(primary.UnitId);
        AssertThat(recipients[1].UnitId).IsEqual(secondaryInLine.UnitId);
        AssertThat(recipients.Exists(u => u.UnitId == outsideLine.UnitId)).IsFalse();
    }

    [TestCase]
    public void CSM_007_ProjectileContact_UsesHurtboxChannel()
    {
        var unit = new UnitData { NavigationRadius = 1.0f, HurtboxRadius = 0.25f };

        AssertThat(CombatGeometry.GetHurtboxRadius(unit)).IsEqual(0.25f);
    }

    [TestCase]
    public void CSM_008_AoeInclusion_UsesGroundCylinderForGroundUnit()
    {
        var center = SimVector3.Zero;
        var unit = new UnitData
        {
            Position = new SimVector3(0.9f, 0f, 0f),
            MovementLayer = MovementLayer.Ground,
            HurtboxRadius = 0.25f,
        };

        bool inRange = CombatGeometry.CanHitUnitInRadius(
            ProjectileHitSpace.GroundCylinder,
            unit,
            center,
            1.2f
        );

        AssertThat(inRange).IsTrue();
    }

    [TestCase]
    public void CSM_009_MovementSystems_UseNavigationFootprint()
    {
        var unit = new UnitData { SeparationRadius = 1.2f, NavigationRadius = 0.4f };

        AssertThat(CombatGeometry.GetNavigationRadius(unit)).IsEqual(0.4f);
    }

    [TestCase]
    public void CSM_013_HitSpaceMode_GroundCylinderVsSphere3D()
    {
        var groundUnit = new UnitData { MovementLayer = MovementLayer.Ground };
        var airUnit = new UnitData { MovementLayer = MovementLayer.Air };

        AssertThat(CombatGeometry.UseGroundCylinder(ProjectileHitSpace.GroundCylinder, groundUnit))
            .IsTrue();
        AssertThat(CombatGeometry.UseGroundCylinder(ProjectileHitSpace.Sphere3D, groundUnit))
            .IsFalse();
        AssertThat(CombatGeometry.UseGroundCylinder(ProjectileHitSpace.GroundCylinder, airUnit))
            .IsFalse();
    }
}
