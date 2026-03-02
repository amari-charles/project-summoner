namespace ProjectSummoner.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using GdUnit4;
using ProjectSummoner.Units;
using static GdUnit4.Assertions;

[TestSuite]
public class SimTargetingTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
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

    // =========================================================================
    // Group Targeting
    // =========================================================================

    [TestCase]
    public void AcquireTarget_FollowerCopiesLeaderTarget()
    {
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f);

        var leader = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        leader.TargetUnitId = enemy.UnitId;
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
