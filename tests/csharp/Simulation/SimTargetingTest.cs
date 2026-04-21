namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimTargetingTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
        SummonerMeleeBubble.ClearOverrideRadius();
    }

    // =========================================================================
    // Basic Acquisition
    // =========================================================================

    [TestCase]
    public void AcquireTarget_SingleEnemy_ReturnsEnemyId()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == enemy.UnitId).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_NoEnemies_ReturnsSummonerTarget()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        // No team 1 units — should fall back to summoner

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(targetId)).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_DeadEnemy_Ignored()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        enemy.IsAlive = false;

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        // Dead enemy ignored, falls back to summoner
        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(targetId)).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_InactiveEnemy_Ignored()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        enemy.ActivationState = ActivationState.Inactive;

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        // Inactive enemy ignored
        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(targetId)).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_OutsideAggro_NotFound()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f, aggroRadius: 5f);
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 30f); // Far away

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        // Enemy outside aggro → fall back to summoner
        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(targetId)).IsTrue();
    }

    // =========================================================================
    // Layer Filter
    // =========================================================================

    [TestCase]
    public void AcquireTarget_GroundOnlyFilter_IgnoresAirUnits()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.TargetLayerFilter = TargetLayer.GroundOnly;

        SimTestHelper.CreateFlyingUnit(_state, 1, x: 5f); // Air unit

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        // Air unit ignored by ground-only filter
        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(targetId)).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_AirOnlyFilter_IgnoresGroundUnits()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.TargetLayerFilter = TargetLayer.AirOnly;

        SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f); // Ground unit

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        // Ground unit ignored by air-only filter
        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(targetId)).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_BothFilter_FindsAll()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.TargetLayerFilter = TargetLayer.Both;

        var groundEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3f);
        SimTestHelper.CreateFlyingUnit(_state, 1, x: 5f);

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        // Both filters pass — closest is the ground unit
        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == groundEnemy.UnitId).IsTrue();
    }

    // =========================================================================
    // Scoring
    // =========================================================================

    [TestCase]
    public void AcquireTarget_CloserEnemyPreferred()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);

        SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f); // Far
        var close = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f); // Close

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == close.UnitId).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_LowerHpPreferred_WhenHealthWeightHigh()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        attacker.HealthScorerWeight = 100f; // Very high health weight
        attacker.DistanceScorerWeight = 0f; // Disable distance scoring

        var highHp = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, hp: 100f);
        // Same max HP, but damaged to 20% — lower HP % → higher health score
        var lowHp = SimTestHelper.CreateMeleeUnit(_state, 1, x: 6f, hp: 100f);
        lowHp.CurrentHp = 20f; // 20/100 = 20% HP

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == lowHp.UnitId).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_PolicyEnabled_PrefersAttackableNowEvenIfScoreLower()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: 0f,
            attackRange: 5f,
            aggroRadius: 20f
        );
        attacker.HasConeConstraint = false;
        attacker.DistanceScorerWeight = 0f;
        attacker.HealthScorerWeight = 100f;
        attacker.TargetPolicyId = TargetPolicyId.PreferAttackable;

        var inRangeHealthy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 4f, hp: 100f);
        var outOfRangeLowHp = SimTestHelper.CreateMeleeUnit(_state, 1, x: 6f, hp: 100f);
        outOfRangeLowHp.CurrentHp = 10f;

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == inRangeHealthy.UnitId).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_ConeUnit_PrefersAttackableNowOverCloserOutOfCone()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: 0f,
            attackRange: 24f,
            aggroRadius: 24f
        );
        attacker.HasConeConstraint = true;
        attacker.ConeHalfAngle = 30f;
        attacker.CloseRangeThreshold = 0.5f;
        attacker.IsFacingRight = true;

        var inCone = SimTestHelper.CreateMeleeUnit(_state, 1, x: 20f, z: 0f);
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f, z: 10f); // Closer but outside 30° cone

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == inCone.UnitId).IsTrue();
    }

    // =========================================================================
    // Virtual Lanes + Roles
    // =========================================================================

    [TestCase]
    public void AcquireTarget_FlankerInSideLane_IgnoresFarCenterTarget()
    {
        var flanker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f, z: -18f, aggroRadius: 30f);
        flanker.TacticalRole = TacticalRole.Flanker;
        flanker.AssignedLane = 0; // bottom side lane

        var sideEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3f, z: -18f);
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 0f); // center lane enemy

        var targetId = SimTargeting.AcquireTarget(flanker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == sideEnemy.UnitId).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_Backliner_PrefersSameLaneTarget()
    {
        var backliner = SimTestHelper.CreateRangedUnit(_state, 0, x: -8f, z: 0f, aggroRadius: 30f);
        backliner.TacticalRole = TacticalRole.Backliner;
        backliner.AssignedLane = 1; // center lane
        backliner.DistanceScorerWeight = 1f;
        backliner.HealthScorerWeight = 0f;

        var crossLaneCloser = SimTestHelper.CreateMeleeUnit(_state, 1, x: -6f, z: 9f);
        var sameLaneSlightlyFarther = SimTestHelper.CreateMeleeUnit(_state, 1, x: 4f, z: 0f);

        var targetId = SimTargeting.AcquireTarget(backliner, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == sameLaneSlightlyFarther.UnitId).IsTrue();
        AssertThat(targetId!.Value == crossLaneCloser.UnitId).IsFalse();
    }

    // =========================================================================
    // Group Targeting
    // =========================================================================

    [TestCase]
    public void AcquireTarget_FollowerCopiesLeaderTarget()
    {
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f);

        var leader = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        leader.Engagement.TargetUnitId = enemy.UnitId;
        leader.GroupId = 1;

        var follower = SimTestHelper.CreateMeleeUnit(_state, 0, x: -4f);
        follower.LeaderId = leader.UnitId;

        var targetId = SimTargeting.AcquireTarget(follower, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == enemy.UnitId).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_LeaderDead_FollowerTargetsIndependently()
    {
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f);

        var leader = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        leader.IsAlive = false;
        leader.GroupId = 1;

        var follower = SimTestHelper.CreateMeleeUnit(_state, 0, x: -4f);
        follower.LeaderId = leader.UnitId;

        var targetId = SimTargeting.AcquireTarget(follower, _state);

        // Leader dead → follower targets independently
        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value == enemy.UnitId).IsTrue();
    }

    // =========================================================================
    // CanAttack Cone Check
    // =========================================================================

    [TestCase]
    public void CanAttack_NoCone_AlwaysTrue()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        attacker.HasConeConstraint = false;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 10f);

        AssertThat(SimTargeting.CanAttack(attacker, target)).IsTrue();
    }

    [TestCase]
    public void CanAttack_InCone_ReturnsTrue()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        attacker.HasConeConstraint = true;
        attacker.ConeHalfAngle = 45f;
        attacker.IsFacingRight = true;

        // Target directly to the right — 0 degrees (within 45 degree cone)
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 0f);

        AssertThat(SimTargeting.CanAttack(attacker, target)).IsTrue();
    }

    [TestCase]
    public void CanAttack_OutOfCone_ReturnsFalse()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        attacker.HasConeConstraint = true;
        attacker.ConeHalfAngle = 10f;
        attacker.IsFacingRight = true;
        attacker.CloseRangeThreshold = 0.5f;

        // Target behind the unit — 180 degrees off
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: -5f, z: 0f);

        AssertThat(SimTargeting.CanAttack(attacker, target)).IsFalse();
    }

    [TestCase]
    public void CanAttack_ConeCenterOffset_ShiftsFacingCone()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, z: 0f);
        attacker.HasConeConstraint = true;
        attacker.ConeHalfAngle = 30f;
        attacker.ConeCenterOffsetDegrees = -20f;
        attacker.IsFacingRight = true;

        var insideShiftedCone = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f, z: -4.7f); // ~ -25 deg
        var outsideShiftedCone = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f, z: 3.6f); // ~ +20 deg

        AssertThat(SimTargeting.CanAttack(attacker, insideShiftedCone)).IsTrue();
        AssertThat(SimTargeting.CanAttack(attacker, outsideShiftedCone)).IsFalse();
    }

    [TestCase]
    public void CanAttack_CloseRange_AlwaysTrue()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        attacker.HasConeConstraint = true;
        attacker.ConeHalfAngle = 10f;
        attacker.CloseRangeThreshold = 5f;

        // Target behind but within close range threshold
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: -1f, z: 0f);

        AssertThat(SimTargeting.CanAttack(attacker, target)).IsTrue();
    }

    [TestCase]
    public void CanAttack_ForwardRect_RejectsTargetBehindAttacker()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 3f);
        attacker.EngageShape = EngageShape.ForwardRect;
        attacker.EngageRectLength = 2.7f;
        attacker.EngageRectHalfWidth = 0.5f;
        attacker.EngageRectForwardOffset = 0f;
        attacker.EngageCloseRadius = 0.4f;
        attacker.IsFacingRight = true;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: -1.0f, z: 0.1f);

        AssertThat(SimTargeting.IsWithinEngageDistance(attacker, target.Position)).IsTrue();
        AssertThat(SimTargeting.CanAttack(attacker, target)).IsFalse();
    }

    [TestCase]
    public void CanAttack_ForwardRect_CloseBubbleAllowsVeryNearTarget()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 3f);
        attacker.EngageShape = EngageShape.ForwardRect;
        attacker.EngageRectLength = 2.7f;
        attacker.EngageRectHalfWidth = 0.5f;
        attacker.EngageRectForwardOffset = 0f;
        attacker.EngageCloseRadius = 0.4f;
        attacker.IsFacingRight = true;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: -0.2f, z: 0f);

        AssertThat(SimTargeting.CanAttack(attacker, target)).IsTrue();
    }

    [TestCase]
    public void AcquireTarget_ForwardRect_PrefersAttackableFrontTargetOverBehindLowHp()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: 0f,
            attackRange: 3f,
            aggroRadius: 15f
        );
        attacker.EngageShape = EngageShape.ForwardRect;
        attacker.EngageRectLength = 5.4f;
        attacker.EngageRectHalfWidth = 1.0f;
        attacker.EngageRectForwardOffset = 0f;
        attacker.EngageCloseRadius = 0.4f;
        attacker.IsFacingRight = true;
        attacker.DistanceScorerWeight = 0f;
        attacker.HealthScorerWeight = 100f;
        attacker.TargetPolicyId = TargetPolicyId.PreferAttackable;

        var frontAttackable = SimTestHelper.CreateMeleeUnit(_state, 1, x: 4f, z: 0.1f, hp: 100f);
        var behindLowHp = SimTestHelper.CreateMeleeUnit(_state, 1, x: -1f, z: 0f, hp: 100f);
        behindLowHp.CurrentHp = 10f;

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value).IsEqual(frontAttackable.UnitId);
    }

    [TestCase]
    public void IsTargetAttackableNow_ForwardRectSummoner_UsesBubbleEdgeForNearSide()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18.1f, z: 1.9f, attackRange: 3f, aggroRadius: 20f);
        attacker.EngageShape = EngageShape.ForwardRect;
        attacker.EngageRectLength = 2.7f;
        attacker.EngageRectHalfWidth = 0.8f;
        attacker.EngageRectForwardOffset = 0f;
        attacker.EngageCloseRadius = 0.4f;
        attacker.IsFacingRight = true;

        int summonerTarget = MatchState.GetSummonerTargetId(1);
        bool canAttack = SimTargeting.IsTargetAttackableNow(attacker, summonerTarget, _state);

        AssertThat(canAttack).IsTrue();
    }

    [TestCase]
    public void IsTargetAttackableNow_ForwardRectSummoner_OutsideBubbleStillRejected()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18.1f, z: 7.0f, attackRange: 3f, aggroRadius: 20f);
        attacker.EngageShape = EngageShape.ForwardRect;
        attacker.EngageRectLength = 2.7f;
        attacker.EngageRectHalfWidth = 0.8f;
        attacker.EngageRectForwardOffset = 0f;
        attacker.EngageCloseRadius = 0.4f;
        attacker.IsFacingRight = true;

        int summonerTarget = MatchState.GetSummonerTargetId(1);
        bool canAttack = SimTargeting.IsTargetAttackableNow(attacker, summonerTarget, _state);

        AssertThat(canAttack).IsFalse();
    }

    // =========================================================================
    // Summoner Fallback
    // =========================================================================

    [TestCase]
    public void AcquireTarget_NoEnemiesOrSummoner_ReturnsNull()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        _state.Summoners[1].IsAlive = false; // Enemy summoner dead

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsFalse();
    }

    [TestCase]
    public void AcquireTarget_SummonerTargetId_IsNegative()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        // No team 1 units, but summoner alive

        var targetId = SimTargeting.AcquireTarget(attacker, _state);

        AssertThat(targetId.HasValue).IsTrue();
        AssertThat(targetId!.Value < 0).IsTrue();

        // Verify round-trip: targetId → team → targetId
        int team = MatchState.GetSummonerTeamFromTargetId(targetId.Value);
        AssertThat(team == 1).IsTrue(); // Enemy team
        AssertThat(MatchState.GetSummonerTargetId(team) == targetId.Value).IsTrue();
    }
}
