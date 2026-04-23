namespace Fateforged.Tests.Simulation;

using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Movement;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimMovementFacingStabilityTest
{
    private const float Delta = 1f / 60f;

    private MatchState _state = null!;
    private Fateforged.Simulation.Simulation _sim = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
        SummonerMeleeBubble.ClearOverrideRadius();
        _sim = new Fateforged.Simulation.Simulation(_state);
    }

    [TestCase]
    public void FacingController_TowardTarget_TinyXAxisNoise_DoesNotFlip()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, z: 0f);
        unit.IsFacingRight = true;

        for (int i = 0; i < 120; i++)
        {
            float noisyX = (i % 2 == 0) ? 0.05f : -0.05f;
            var intent = new MovementIntent
            {
                Mode = MovementResult.TowardTarget,
                TargetId = null,
                DesiredVelocity = new SimVector3(0f, 0f, 1f),
                DesiredFacingDirection = new SimVector3(noisyX, 0f, 1f).Normalized(),
            };

            FacingController.Tick(unit, Delta);
            FacingController.Update(unit, intent, _state);
        }

        AssertThat(unit.IsFacingRight).IsTrue();
    }

    [TestCase]
    public void Tick_StrafeNearVerticalTarget_DoesNotRapidlyFlipFacing()
    {
        var strafingUnit = SimTestHelper.CreateRangedUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 8f,
            moveSpeed: 2.5f,
            projectileDelay: 0.2f
        );
        strafingUnit.HasConeConstraint = true;
        strafingUnit.ConeHalfAngle = 8f;
        strafingUnit.CloseRangeThreshold = 0.1f;
        strafingUnit.FallbackMovement = FallbackMovement.Strafe;
        strafingUnit.TargetPolicyId = TargetPolicyId.PreferAttackableAndStick;

        // Nearby friendly creates close-contact avoidance pressure.
        SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: -0.30f,
            z: 0.08f,
            moveSpeed: 0f,
            attackSpeed: 0f,
            aggroRadius: 0f
        );

        // Place target nearly vertical in X so tiny jitter would normally thrash facing.
        SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 0.02f,
            z: 2.20f,
            moveSpeed: 0f,
            attackSpeed: 0f,
            aggroRadius: 0f
        );

        int facingFlips = 0;
        bool lastFacing = strafingUnit.IsFacingRight;

        for (int i = 0; i < 300; i++)
        {
            _sim.Tick(Delta);
            if (strafingUnit.IsFacingRight != lastFacing)
            {
                facingFlips++;
                lastFacing = strafingUnit.IsFacingRight;
            }
        }

        AssertThat(facingFlips).IsLess(25);
    }

    [TestCase]
    public void TickCommitMeleeMovement_MovementNone_SummonerTarget_FacesTowardSummonerCenter()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 24f, z: 0f, moveSpeed: 2f);
        unit.IsFacingRight = true;
        unit.TargetUnitId = MatchState.GetSummonerTargetId(team: 1);

        var behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };
        SimMovement.Tick(unit, behavior, _state, Delta);

        AssertThat(unit.IsFacingRight).IsFalse();
    }

    [TestCase]
    public void TickCommitMeleeMovement_MovementNone_UnitTarget_FacesTowardUnit()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, moveSpeed: 2f);
        unit.IsFacingRight = true;
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: -2.5f, z: 0f, moveSpeed: 0f);
        unit.TargetUnitId = target.UnitId;

        var behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };
        SimMovement.Tick(unit, behavior, _state, Delta);

        AssertThat(unit.IsFacingRight).IsFalse();
    }
}
