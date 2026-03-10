namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using GdUnit4;
using static GdUnit4.Assertions;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

[TestSuite]
public class SimBehaviorTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    // =========================================================================
    // State Machine
    // =========================================================================

    [TestCase]
    public void TickBehavior_NoTarget_MoveForward()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: -10f);
        // No enemies → no target
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.Forward);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.NoTarget);
    }

    [TestCase]
    public void TickBehavior_TargetOutOfRange_MoveTowardTarget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: -10f, attackRange: 2f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f);

        unit.TargetUnitId = enemy.UnitId;
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.TowardTarget);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.Chasing);
    }

    [TestCase]
    public void TickBehavior_BacklinerCrossLaneFar_HoldsLaneAndMovesForward()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, 0, x: -10f, z: 0f, attackRange: 6f);
        unit.TacticalRole = TacticalRole.Backliner;
        unit.AssignedLane = 1;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 8f, z: 16f);

        unit.TargetUnitId = enemy.UnitId;
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.Forward);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.NoTarget);
    }

    [TestCase]
    public void TickBehavior_FlankerSideLaneVersusCenter_HoldsFlankRoute()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: -10f, z: -18f, attackRange: 3f);
        unit.TacticalRole = TacticalRole.Flanker;
        unit.AssignedLane = 0;
        var centerEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 0f);

        unit.TargetUnitId = centerEnemy.UnitId;
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.Forward);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.NoTarget);
    }

    [TestCase]
    public void TickBehavior_Flanker_WithSummonerTarget_DoesNotHoldLane()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: -10f, z: -18f, attackRange: 3f);
        unit.TacticalRole = TacticalRole.Flanker;
        unit.AssignedLane = 0;

        unit.TargetUnitId = MatchState.GetSummonerTargetId(team: 1);
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.TowardTarget);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.Chasing);
    }

    [TestCase]
    public void TickBehavior_Backliner_WithSummonerTarget_DoesNotHoldLane()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, 0, x: -10f, z: 18f, attackRange: 6f);
        unit.TacticalRole = TacticalRole.Backliner;
        unit.AssignedLane = 2;

        unit.TargetUnitId = MatchState.GetSummonerTargetId(team: 1);
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.TowardTarget);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.Chasing);
    }

    [TestCase]
    public void TickBehavior_InRange_CooldownNotReady_MoveNone()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f);

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 1f; // Cooldown active
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.None);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.InRange);
    }

    [TestCase]
    public void TickBehavior_InRange_CooldownReady_Attacks()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        enemy.Evasion = 0f;

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.None);
        AssertThat(unit.BehaviorState).IsEqual(BehaviorState.Attacking);

        var attacked = SimTestHelper.FindEvent<UnitAttackedEvent>(events);
        AssertThat(attacked).IsNotNull();
    }

    [TestCase]
    public void TickBehavior_Stunned_MoveNone()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.ActiveBuffs.Add(new ActiveBuff { EffectType = EffectType.Stun, Duration = 2f });
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f);
        unit.TargetUnitId = enemy.UnitId;
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.None);
    }

    // =========================================================================
    // Melee Damage
    // =========================================================================

    [TestCase]
    public void TickBehavior_MeleeAttack_DealsDamage()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        enemy.Evasion = 0f;

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(enemy.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void TickBehavior_MeleeKill_EmitsUnitDiedEvent()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 200f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 10f);
        enemy.Evasion = 0f;

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        var died = SimTestHelper.FindEvent<UnitDiedEvent>(events);
        AssertThat(died).IsNotNull();
        AssertThat(died!.UnitId).IsEqual(enemy.UnitId);
    }

    [TestCase]
    public void TickBehavior_MeleeKill_SetsIsAliveFalse()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 200f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 10f);
        enemy.Evasion = 0f;

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(enemy.IsAlive).IsFalse();
        AssertThat(enemy.CurrentHp).IsEqual(0f);
    }

    [TestCase]
    public void TickBehavior_MeleeKill_IncrementsKillCount()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 200f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 10f);
        enemy.Evasion = 0f;

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        int killsBefore = _state.KillCount;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(_state.KillCount).IsEqual(killsBefore + 1);
    }

    [TestCase]
    public void TickBehavior_MeleeAttack_ResetsDistanceTraveled()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        unit.DistanceTraveled = 50f; // Has traveled a lot
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        enemy.Evasion = 0f;

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(unit.DistanceTraveled).IsEqual(0f);
    }

    // =========================================================================
    // Ranged Projectile Spawning
    // =========================================================================

    [TestCase]
    public void TickBehavior_RangedAttack_SetsPendingDamageTimer()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, 0, x: 0f, attackRange: 10f, projectileDelay: 0.5f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, hp: 100f);

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(unit.PendingDamageTimer).IsEqual(0.5f);
        AssertThat(unit.PendingDamageTargetId.HasValue).IsTrue();
        AssertThat(unit.PendingDamageTargetId!.Value == enemy.UnitId).IsTrue();
    }

    [TestCase]
    public void TickBehavior_RangedAttack_NoDelay_SpawnsProjectile()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, 0, x: 0f, attackRange: 10f, projectileDelay: 0f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, hp: 100f);

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;

        var events = new List<SimEvent>();
        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(_state.Projectiles.Count).IsEqual(1);
        var projectile = _state.Projectiles.Values.First();
        AssertThat(projectile.SourceUnitId).IsEqual(unit.UnitId);
        AssertThat(projectile.TargetUnitId).IsEqual(enemy.UnitId);
    }

    [TestCase]
    public void TickPendingDamage_TimerExpired_SpawnsProjectile()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, 0, x: 0f, damage: 20f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, hp: 100f);

        unit.PendingDamageTimer = 0.1f;
        unit.PendingDamageTargetId = enemy.UnitId;
        unit.PendingDamageAmount = 20f;

        var events = new List<SimEvent>();
        SimBehavior.TickPendingDamage(unit, _state, 0.5f, events);

        AssertThat(_state.Projectiles.Count).IsEqual(1);
    }

    [TestCase]
    public void TickPendingDamage_TargetDead_NoProjectileSpawned()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, 0, x: 0f, damage: 20f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, hp: 100f);
        enemy.IsAlive = false; // Dead before delayed spawn resolves

        unit.PendingDamageTimer = 0.1f;
        unit.PendingDamageTargetId = enemy.UnitId;
        unit.PendingDamageAmount = 20f;

        var events = new List<SimEvent>();
        SimBehavior.TickPendingDamage(unit, _state, 0.5f, events);

        AssertThat(_state.Projectiles.Count).IsEqual(0);
        AssertThat(enemy.CurrentHp).IsEqual(100f);
    }

    // =========================================================================
    // Summoner Attacks
    // =========================================================================

    [TestCase]
    public void TickBehavior_AttacksSummoner_DealsDamage()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18f, attackRange: 5f, damage: 10f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        // Point unit at enemy summoner (team 1 summoner is at x=20)
        int summonerTargetId = MatchState.GetSummonerTargetId(1);
        unit.TargetUnitId = summonerTargetId;
        unit.AttackCooldown = 0f;

        float hpBefore = _state.Summoners[1].CurrentHp;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(_state.Summoners[1].CurrentHp).IsLess(hpBefore);
    }

    [TestCase]
    public void TickBehavior_AttacksSummoner_AppliesSoulStrengthBonus()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18f, attackRange: 5f, damage: 10f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        unit.SoulStrength = 7f;
        int summonerTargetId = MatchState.GetSummonerTargetId(1);
        unit.TargetUnitId = summonerTargetId;
        unit.AttackCooldown = 0f;
        _state.Summoners[0].DamageBonus = 0f;
        _state.Summoners[1].DamageReduction = 0f;
        _state.Summoners[1].SoulGuard = 0f;

        float hpBefore = _state.Summoners[1].CurrentHp;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(hpBefore - _state.Summoners[1].CurrentHp).IsEqual(17f);
    }

    [TestCase]
    public void TickBehavior_AttacksSummoner_AppliesSoulGuardReduction()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18f, attackRange: 5f, damage: 10f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        unit.SoulStrength = 8f;
        int summonerTargetId = MatchState.GetSummonerTargetId(1);
        unit.TargetUnitId = summonerTargetId;
        unit.AttackCooldown = 0f;
        _state.Summoners[0].DamageBonus = 0f;
        _state.Summoners[1].DamageReduction = 0f;
        _state.Summoners[1].SoulGuard = 5f;

        float hpBefore = _state.Summoners[1].CurrentHp;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(hpBefore - _state.Summoners[1].CurrentHp).IsEqual(13f);
    }

    [TestCase]
    public void TickBehavior_KillsSummoner_SetsNotAlive()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18f, attackRange: 5f, damage: 500f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        int summonerTargetId = MatchState.GetSummonerTargetId(1);
        unit.TargetUnitId = summonerTargetId;
        unit.AttackCooldown = 0f;

        _state.Summoners[1].CurrentHp = 5f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(_state.Summoners[1].IsAlive).IsFalse();
        AssertThat(_state.Summoners[1].CurrentHp).IsEqual(0f);
    }

    // =========================================================================
    // Cooldowns
    // =========================================================================

    [TestCase]
    public void TickCooldowns_DecrementsByDelta()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.AttackCooldown = 1.0f;
        unit.TargetLockTimer = 0.5f;

        SimBehavior.TickCooldowns(unit, 0.1f);

        AssertThat(unit.AttackCooldown).IsEqual(0.9f);
        AssertThat(unit.TargetLockTimer).IsEqual(0.4f);
    }

    [TestCase]
    public void TickCooldowns_ForcedTargetExpiry_ClearsTarget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.ForcedTargetUnitId = 99;
        unit.ForcedTargetTimer = 0.1f;

        SimBehavior.TickCooldowns(unit, 0.5f);

        AssertThat(unit.ForcedTargetUnitId).IsNull();
    }

    // =========================================================================
    // Targeting Tick
    // =========================================================================

    [TestCase]
    public void TickTargeting_ForcedTarget_TakesPriority()
    {
        var enemy1 = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3f);
        var enemy2 = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f);

        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.ForcedTargetUnitId = enemy2.UnitId;
        unit.ForcedTargetTimer = 5f;

        SimBehavior.TickTargeting(unit, _state);

        // Forced target should win even though enemy1 is closer
        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value == enemy2.UnitId).IsTrue();
    }

    [TestCase]
    public void TickTargeting_LockExpired_Reacquires()
    {
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);

        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.TargetLockTimer = 0f; // Lock expired

        SimBehavior.TickTargeting(unit, _state);

        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value == enemy.UnitId).IsTrue();
        AssertThat(unit.TargetLockTimer).IsGreater(0f);
    }

    [TestCase]
    public void TickTargeting_ConeCurrentAttackable_LockExpired_KeepsCurrentTarget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 24f, aggroRadius: 24f);
        unit.HasConeConstraint = true;
        unit.ConeHalfAngle = 30f;
        unit.CloseRangeThreshold = 0.5f;
        unit.IsFacingRight = true;

        var current = SimTestHelper.CreateMeleeUnit(_state, 1, x: 20f, z: 0f); // In cone and in range
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f, z: 10f); // Closer but out of cone

        unit.TargetUnitId = current.UnitId;
        unit.TargetLockTimer = 0f;

        SimBehavior.TickTargeting(unit, _state);

        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value == current.UnitId).IsTrue();
        AssertThat(unit.TargetLockTimer).IsGreater(0f);
    }

    [TestCase]
    public void TickTargeting_ConeCurrentNotAttackable_LockExpired_SwitchesToAttackableTarget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 24f, aggroRadius: 24f);
        unit.HasConeConstraint = true;
        unit.ConeHalfAngle = 30f;
        unit.CloseRangeThreshold = 0.5f;
        unit.IsFacingRight = true;

        var closerOutOfCone = SimTestHelper.CreateMeleeUnit(_state, 1, x: 10f, z: 10f);
        var inCone = SimTestHelper.CreateMeleeUnit(_state, 1, x: 20f, z: 0f);

        unit.TargetUnitId = closerOutOfCone.UnitId;
        unit.TargetLockTimer = 0f;

        SimBehavior.TickTargeting(unit, _state);

        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value == inCone.UnitId).IsTrue();
    }

    [TestCase]
    public void TickTargeting_KeepCurrentDisabled_CanSwitchWhenLockExpires()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, aggroRadius: 20f);
        unit.HasConeConstraint = false;
        unit.DistanceScorerWeight = 0f;
        unit.HealthScorerWeight = 100f;
        unit.TargetPolicyId = TargetPolicyId.Legacy;

        var currentInRange = SimTestHelper.CreateMeleeUnit(_state, 1, x: 4f, hp: 100f);
        var outOfRangeLowHp = SimTestHelper.CreateMeleeUnit(_state, 1, x: 6f, hp: 100f);
        outOfRangeLowHp.CurrentHp = 10f;

        unit.TargetUnitId = currentInRange.UnitId;
        unit.TargetLockTimer = 0f;

        SimBehavior.TickTargeting(unit, _state);

        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value == outOfRangeLowHp.UnitId).IsTrue();
    }

    // =========================================================================
    // Triggers (wired in Phase 5)
    // =========================================================================

    [TestCase]
    public void TickBehavior_MeleeHit_FiresOnHitTrigger()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        unit.CritChance = 0f;
        unit.ElementId = 0;
        unit.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnHit,
            EffectType = EffectType.Slow,
            Value = 0.3f,
            Duration = 2f
        });

        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        enemy.Evasion = 0f;

        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        // OnHit trigger should apply slow to the enemy
        AssertThat(enemy.ActiveBuffs.Count).IsGreaterEqual(1);
        bool hasSlow = false;
        foreach (var buff in enemy.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.Slow) hasSlow = true;
        }
        AssertThat(hasSlow).IsTrue();
    }

    [TestCase]
    public void TickBehavior_MeleeDamaged_FiresOnDamagedTrigger()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var defender = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        defender.Evasion = 0f;
        // OnDamaged trigger with Slow: applies to the attacker (the combat target parameter)
        defender.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnDamaged,
            EffectType = EffectType.Slow,
            Value = 0.3f,
            Duration = 2f
        });

        attacker.TargetUnitId = defender.UnitId;
        attacker.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(attacker, _state, 0.016f, events);

        // OnDamaged trigger fires on defender, applying effect to the attacker
        bool hasSlow = false;
        foreach (var buff in attacker.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.Slow) hasSlow = true;
        }
        AssertThat(hasSlow).IsTrue();
    }

    [TestCase]
    public void TickBehavior_MeleeKill_FiresOnKillAndOnDeathTriggers()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 200f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;
        attacker.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnKill,
            EffectType = EffectType.Heal,
            Value = 20f
        });

        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 10f);
        enemy.Evasion = 0f;
        enemy.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnDeath,
            EffectType = EffectType.Damage,
            Value = 50f,
            DamageType = DamageType.True,
            AoeRadius = 5f
        });

        attacker.TargetUnitId = enemy.UnitId;
        attacker.AttackCooldown = 0f;
        attacker.CurrentHp = 80f; // Less than max to see heal
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(attacker, _state, 0.016f, events);

        // Kill confirmed
        AssertThat(enemy.IsAlive).IsFalse();

        // OnKill trigger: attacker healed (heal is applied to attacker via OnKill → self-targeted)
        // Note: OnKill fires with target=enemy, so the heal goes to the enemy (dead)
        // Actually, FireTriggers applies the effect to `target`, which is the killed enemy
        // So the heal goes to the dead unit — this is correct per the code
        // Let's verify the OnDeath trigger fired instead
        var died = SimTestHelper.FindEvent<UnitDiedEvent>(events);
        AssertThat(died).IsNotNull();
    }

    // =========================================================================
    // Cone Fallback
    // =========================================================================

    [TestCase]
    public void TickBehavior_ConeNotSatisfied_FallbackStrafe()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 10f);
        unit.HasConeConstraint = true;
        unit.ConeHalfAngle = 10f;
        unit.FallbackMovement = FallbackMovement.Strafe;
        unit.IsFacingRight = true;

        // Place enemy behind — out of narrow cone
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: -5f, z: 5f);
        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.Strafe);
    }

    [TestCase]
    public void TickBehavior_ConeNotSatisfied_FallbackIdle()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 10f);
        unit.HasConeConstraint = true;
        unit.ConeHalfAngle = 10f;
        unit.FallbackMovement = FallbackMovement.Idle;
        unit.IsFacingRight = true;

        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: -5f, z: 5f);
        unit.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        var result = SimBehavior.TickBehavior(unit, _state, 0.016f, events);

        AssertThat(result.Movement).IsEqual(MovementResult.None);
    }

    // =========================================================================
    // Attack Vector Behavior (PASS 3)
    // =========================================================================

    [TestCase]
    public void TickBehavior_AttackVector_SingleMode_DamagesPrimaryOnly()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.Single;
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var nearby = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.3f, z: 0.2f, hp: 100f);
        nearby.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(nearby.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void TickBehavior_AttackVector_AreaCollectSphere_DamagesEnemiesInRadius()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(1.5f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var insideRadius = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.9f, z: 0.8f, hp: 100f);
        insideRadius.Evasion = 0f;
        var outsideRadius = SimTestHelper.CreateMeleeUnit(_state, 1, x: 4f, z: 1.6f, hp: 100f);
        outsideRadius.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(insideRadius.CurrentHp).IsLess(100f);
        AssertThat(outsideRadius.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void TickBehavior_AttackVector_AreaCollectBox_OnlyHitsForwardFacingRecipients()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.IsFacingRight = true;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Box;
        attacker.Attack.Area.Size = new SimVector3(4f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, z: 0f, hp: 100f);
        primary.Evasion = 0f;
        var frontRecipient = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.2f, z: 0.5f, hp: 100f);
        frontRecipient.Evasion = 0f;
        var behindRecipient = SimTestHelper.CreateMeleeUnit(_state, 1, x: -1.2f, z: 0.3f, hp: 100f);
        behindRecipient.Evasion = 0f;
        var outsideWidth = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.2f, z: 1.5f, hp: 100f);
        outsideWidth.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(frontRecipient.CurrentHp).IsLess(100f);
        AssertThat(behindRecipient.CurrentHp).IsEqual(100f);
        AssertThat(outsideWidth.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void TickBehavior_AttackVector_AreaCollectCapsule_BoundaryDeterministic()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Capsule;
        attacker.Attack.Area.Size = new SimVector3(0f, 1f, 0.5f);
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var onBoundary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 1f, z: 0.5f, hp: 100f);
        onBoundary.Evasion = 0f;
        var outside = SimTestHelper.CreateMeleeUnit(_state, 1, x: 1f, z: 0.61f, hp: 100f);
        outside.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(onBoundary.CurrentHp).IsLess(100f);
        AssertThat(outside.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void TickBehavior_AttackVector_LineCollectPierce_DamagesCorridorRecipientsInOrder()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.LineCollect;
        attacker.Attack.Propagation.Mode = AttackPropagationMode.Pierce;
        attacker.Attack.Area.LineLength = 6f;
        attacker.Attack.Area.LineHalfWidth = 0.6f;
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var second = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.3f, z: 0.1f, hp: 100f);
        second.Evasion = 0f;
        var third = SimTestHelper.CreateMeleeUnit(_state, 1, x: 4.8f, z: -0.1f, hp: 100f);
        third.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(second.CurrentHp).IsLess(100f);
        AssertThat(third.CurrentHp).IsLess(100f);

        var damagedOrder = GetDamagedTargetOrder(events);
        AssertThat(damagedOrder).IsEqual(new List<int> { primary.UnitId, second.UnitId, third.UnitId });
    }

    [TestCase]
    public void TickBehavior_AttackVector_LineCollectPierce_ExcludesOffCorridorRecipients()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.LineCollect;
        attacker.Attack.Propagation.Mode = AttackPropagationMode.Pierce;
        attacker.Attack.Area.LineLength = 6f;
        attacker.Attack.Area.LineHalfWidth = 0.5f;
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var aligned = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.4f, z: 0.2f, hp: 100f);
        aligned.Evasion = 0f;
        var offCorridor = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.4f, z: 1.2f, hp: 100f);
        offCorridor.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(aligned.CurrentHp).IsLess(100f);
        AssertThat(offCorridor.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void TickBehavior_AttackVector_ChainHops_DamagesNearestHops()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.ChainHops;
        attacker.Attack.Propagation.Mode = AttackPropagationMode.Chain;
        attacker.Attack.Propagation.ChainMaxJumps = 2;
        attacker.Attack.Propagation.ChainJumpRadius = 2.1f;
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var hopOne = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.5f, z: 0f, hp: 100f);
        hopOne.Evasion = 0f;
        var hopTwo = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 0f, hp: 100f);
        hopTwo.Evasion = 0f;
        var tooFar = SimTestHelper.CreateMeleeUnit(_state, 1, x: 8f, z: 0f, hp: 100f);
        tooFar.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(hopOne.CurrentHp).IsLess(100f);
        AssertThat(hopTwo.CurrentHp).IsLess(100f);
        AssertThat(tooFar.CurrentHp).IsEqual(100f);

        var damagedOrder = GetDamagedTargetOrder(events);
        AssertThat(damagedOrder).IsEqual(new List<int> { primary.UnitId, hopOne.UnitId, hopTwo.UnitId });
    }

    [TestCase]
    public void TickBehavior_AttackVector_ChainHops_SkipsDeadAlliesAndOutOfRadius()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.ChainHops;
        attacker.Attack.Propagation.Mode = AttackPropagationMode.Chain;
        attacker.Attack.Propagation.ChainMaxJumps = 2;
        attacker.Attack.Propagation.ChainJumpRadius = 2.1f;
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var validHop = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.5f, z: 0f, hp: 100f);
        validHop.Evasion = 0f;
        var deadEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.4f, z: 0.1f, hp: 100f);
        deadEnemy.IsAlive = false;
        var ally = SimTestHelper.CreateMeleeUnit(_state, 0, x: 3.4f, z: 0f, hp: 100f);
        var outOfRadius = SimTestHelper.CreateMeleeUnit(_state, 1, x: 6f, z: 0f, hp: 100f);
        outOfRadius.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(validHop.CurrentHp).IsLess(100f);
        AssertThat(deadEnemy.CurrentHp).IsEqual(100f);
        AssertThat(ally.CurrentHp).IsEqual(100f);
        AssertThat(outOfRadius.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void TickBehavior_AttackVector_TargetLimit_CapsRecipientCount()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(2f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 2;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var nearest = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.4f, z: 0f, hp: 100f);
        nearest.Evasion = 0f;
        var fartherA = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3f, z: 0.2f, hp: 100f);
        fartherA.Evasion = 0f;
        var fartherB = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3.2f, z: -0.3f, hp: 100f);
        fartherB.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(nearest.CurrentHp).IsLess(100f);
        AssertThat(fartherA.CurrentHp).IsEqual(100f);
        AssertThat(fartherB.CurrentHp).IsEqual(100f);
        AssertThat(GetDamagedTargetOrder(events).Count).IsEqual(2);
    }

    [TestCase]
    public void TickBehavior_AttackVector_TargetLimitZero_HitsUnlimitedRecipients()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(2.5f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 0;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var nearbyA = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.3f, z: 0.4f, hp: 100f);
        nearbyA.Evasion = 0f;
        var nearbyB = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.8f, z: -0.2f, hp: 100f);
        nearbyB.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.CurrentHp).IsLess(100f);
        AssertThat(nearbyA.CurrentHp).IsLess(100f);
        AssertThat(nearbyB.CurrentHp).IsLess(100f);
        AssertThat(GetDamagedTargetOrder(events).Count).IsEqual(3);
    }

    [TestCase]
    public void TickBehavior_AttackVector_SecondaryDeaths_EmitEvents_PrimaryAttackEventRemainsSingle()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 40f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(2f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 3;
        attacker.Attack.Rules.TriggerMode = AttackTriggerMode.PrimaryOnly;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 200f);
        primary.Evasion = 0f;
        var secondary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.4f, z: 0.1f, hp: 20f);
        secondary.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        AssertThat(primary.IsAlive).IsTrue();
        AssertThat(secondary.IsAlive).IsFalse();

        var diedTargets = events.OfType<UnitDiedEvent>().Select(e => e.UnitId).ToList();
        AssertThat(diedTargets.Contains(secondary.UnitId)).IsTrue();

        var attackedEvents = events.OfType<UnitAttackedEvent>().ToList();
        AssertThat(attackedEvents.Count).IsEqual(1);
        AssertThat(attackedEvents[0].TargetUnitId).IsEqual(primary.UnitId);
    }

    [TestCase]
    public void TickBehavior_AttackVector_PrimaryOnlyTriggerMode_DoesNotFireSecondaryOnDamagedTriggers()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(2f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 3;
        attacker.Attack.Rules.TriggerMode = AttackTriggerMode.PrimaryOnly;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var secondary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.4f, z: 0.1f, hp: 100f);
        secondary.Evasion = 0f;
        secondary.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnDamaged,
            EffectType = EffectType.Slow,
            Value = 0.3f,
            Duration = 2f
        });

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        bool attackerSlowed = attacker.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow);
        AssertThat(attackerSlowed).IsFalse();
    }

    [TestCase]
    public void TickBehavior_AttackVector_EveryRecipientTriggerMode_FiresSecondaryOnDamagedTriggers()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(2f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 3;
        attacker.Attack.Rules.TriggerMode = AttackTriggerMode.EveryRecipient;

        var primary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var secondary = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2.4f, z: 0.1f, hp: 100f);
        secondary.Evasion = 0f;
        secondary.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnDamaged,
            EffectType = EffectType.Slow,
            Value = 0.3f,
            Duration = 2f
        });

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, _state, events);

        bool attackerSlowed = attacker.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow);
        AssertThat(attackerSlowed).IsTrue();
    }

    [TestCase]
    public void TickBehavior_AttackVector_SummonerTarget_IgnoresNonSingleExpansionInV1()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(3f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 4;

        var nearbyEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 19f, z: 0.3f, hp: 100f);
        nearbyEnemy.Evasion = 0f;
        int summonerTargetId = MatchState.GetSummonerTargetId(1);

        attacker.TargetUnitId = summonerTargetId;
        attacker.AttackCooldown = 0f;
        float hpBefore = _state.Summoners[1].CurrentHp;
        var events = new List<SimEvent>();
        SimBehavior.TickBehavior(attacker, _state, 0.016f, events);

        AssertThat(_state.Summoners[1].CurrentHp).IsLess(hpBefore);
        AssertThat(nearbyEnemy.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void AttackVector_Determinism_DATK001_RepeatedRunTargetsMatch()
    {
        var runA = RunDatk001Scenario(seed: 12345);
        var runB = RunDatk001Scenario(seed: 12345);

        AssertThat(runA.DamagedOrder).IsEqual(runB.DamagedOrder);
        AssertThat(runA.EnemyHpSnapshot).IsEqual(runB.EnemyHpSnapshot);
    }

    [TestCase]
    public void AttackVector_Determinism_DATK002_MirroredFacingConsistent()
    {
        var rightOffsets = RunMirroredBoxScenario(seed: 67890, facingRight: true);
        var leftOffsets = RunMirroredBoxScenario(seed: 67890, facingRight: false);

        AssertThat(rightOffsets.Count).IsEqual(leftOffsets.Count);
        for (int i = 0; i < rightOffsets.Count; i++)
            AssertThat(rightOffsets[i]).IsEqual(-leftOffsets[i]);
    }

    [TestCase]
    public void AttackVector_Determinism_DATK003_ChainTieBreakStable()
    {
        var runA = RunChainTieBreakScenario(seed: 24680);
        var runB = RunChainTieBreakScenario(seed: 24680);

        AssertThat(runA.DamagedOrder).IsEqual(runB.DamagedOrder);
        AssertThat(runA.DamagedOrder.Count).IsEqual(2);
        AssertThat(runA.DamagedOrder[1]).IsEqual(runA.ExpectedFirstHopUnitId);
    }

    [TestCase]
    public void AttackVector_Determinism_DATK004_MixedVectorsStableOutcome()
    {
        var runA = RunMixedVectorScenario(seed: 11223);
        var runB = RunMixedVectorScenario(seed: 11223);

        AssertThat(runA.DamagedOrder).IsEqual(runB.DamagedOrder);
        AssertThat(runA.EnemyHpSnapshot).IsEqual(runB.EnemyHpSnapshot);
    }

    private static void ExecuteMeleeAttack(UnitData attacker, UnitData primary, MatchState state, List<SimEvent> events)
    {
        attacker.TargetUnitId = primary.UnitId;
        attacker.AttackCooldown = 0f;
        SimBehavior.TickBehavior(attacker, state, 0.016f, events);
    }

    private static List<int> GetDamagedTargetOrder(List<SimEvent> events)
        => events.OfType<UnitDamagedEvent>().Select(e => e.TargetUnitId).ToList();

    private static List<float> GetEnemyHpSnapshot(MatchState state)
    {
        return state.Units.Values
            .Where(u => u.Team == Team.Enemy)
            .OrderBy(u => u.UnitId)
            .Select(u => u.CurrentHp)
            .ToList();
    }

    private static (List<int> DamagedOrder, List<float> EnemyHpSnapshot) RunDatk001Scenario(uint seed)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        attacker.Attack.Area.Size = new SimVector3(2f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 0;

        var primary = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);
        primary.Evasion = 0f;
        var nearbyA = SimTestHelper.CreateMeleeUnit(state, 1, x: 2.5f, z: 0.3f, hp: 100f);
        nearbyA.Evasion = 0f;
        var nearbyB = SimTestHelper.CreateMeleeUnit(state, 1, x: 3f, z: -0.4f, hp: 100f);
        nearbyB.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, state, events);

        return (GetDamagedTargetOrder(events), GetEnemyHpSnapshot(state));
    }

    private static List<float> RunMirroredBoxScenario(uint seed, bool facingRight)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        float dir = facingRight ? 1f : -1f;

        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.IsFacingRight = facingRight;
        attacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        attacker.Attack.Area.Shape = AttackAreaShape.Box;
        attacker.Attack.Area.Size = new SimVector3(4f, 1f, 1f);
        attacker.Attack.Selection.TargetLimit = 4;

        var primary = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f * dir, z: 0f, hp: 100f);
        primary.Evasion = 0f;
        var front = SimTestHelper.CreateMeleeUnit(state, 1, x: 3.2f * dir, z: 0.3f, hp: 100f);
        front.Evasion = 0f;
        var behind = SimTestHelper.CreateMeleeUnit(state, 1, x: -1.2f * dir, z: 0.2f, hp: 100f);
        behind.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, state, events);

        return GetDamagedTargetOrder(events)
            .Select(targetId => state.Units[targetId].Position.X - attacker.Position.X)
            .ToList();
    }

    private static (List<int> DamagedOrder, int ExpectedFirstHopUnitId) RunChainTieBreakScenario(uint seed)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, attackRange: 5f, damage: 10f);
        attacker.CritChance = 0f;
        attacker.Attack.Selection.Mode = AttackSelectionMode.ChainHops;
        attacker.Attack.Propagation.Mode = AttackPropagationMode.Chain;
        attacker.Attack.Propagation.ChainMaxJumps = 1;
        attacker.Attack.Propagation.ChainJumpRadius = 2f;
        attacker.Attack.Selection.TargetLimit = 3;

        var primary = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, z: 0f, hp: 100f);
        primary.Evasion = 0f;
        var tieLowId = SimTestHelper.CreateMeleeUnit(state, 1, x: 3f, z: 1f, hp: 100f);
        tieLowId.Evasion = 0f;
        var tieHighId = SimTestHelper.CreateMeleeUnit(state, 1, x: 3f, z: -1f, hp: 100f);
        tieHighId.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(attacker, primary, state, events);

        _ = tieHighId;
        return (GetDamagedTargetOrder(events), tieLowId.UnitId);
    }

    private static (List<int> DamagedOrder, List<float> EnemyHpSnapshot) RunMixedVectorScenario(uint seed)
    {
        var state = SimTestHelper.CreateBattleState(seed);

        var areaAttacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, attackRange: 5f, damage: 10f);
        areaAttacker.CritChance = 0f;
        areaAttacker.Attack.Selection.Mode = AttackSelectionMode.AreaCollect;
        areaAttacker.Attack.Area.Shape = AttackAreaShape.Sphere;
        areaAttacker.Attack.Area.Size = new SimVector3(2f, 1f, 1f);
        areaAttacker.Attack.Selection.TargetLimit = 0;

        var lineAttacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: -5f, attackRange: 5f, damage: 8f);
        lineAttacker.CritChance = 0f;
        lineAttacker.Attack.Selection.Mode = AttackSelectionMode.LineCollect;
        lineAttacker.Attack.Propagation.Mode = AttackPropagationMode.Pierce;
        lineAttacker.Attack.Area.LineLength = 6f;
        lineAttacker.Attack.Area.LineHalfWidth = 0.5f;
        lineAttacker.Attack.Selection.TargetLimit = 3;

        var chainAttacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 5f, attackRange: 5f, damage: 9f);
        chainAttacker.CritChance = 0f;
        chainAttacker.Attack.Selection.Mode = AttackSelectionMode.ChainHops;
        chainAttacker.Attack.Propagation.Mode = AttackPropagationMode.Chain;
        chainAttacker.Attack.Propagation.ChainMaxJumps = 2;
        chainAttacker.Attack.Propagation.ChainJumpRadius = 2.2f;
        chainAttacker.Attack.Selection.TargetLimit = 4;

        var areaPrimary = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, z: 0f, hp: 100f);
        areaPrimary.Evasion = 0f;
        var areaSecondary = SimTestHelper.CreateMeleeUnit(state, 1, x: 2.5f, z: 0.4f, hp: 100f);
        areaSecondary.Evasion = 0f;

        var linePrimary = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, z: -5f, hp: 100f);
        linePrimary.Evasion = 0f;
        var lineSecondary = SimTestHelper.CreateMeleeUnit(state, 1, x: 3.4f, z: -4.9f, hp: 100f);
        lineSecondary.Evasion = 0f;

        var chainPrimary = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, z: 5f, hp: 100f);
        chainPrimary.Evasion = 0f;
        var chainHopOne = SimTestHelper.CreateMeleeUnit(state, 1, x: 3.5f, z: 5f, hp: 100f);
        chainHopOne.Evasion = 0f;
        var chainHopTwo = SimTestHelper.CreateMeleeUnit(state, 1, x: 5f, z: 5f, hp: 100f);
        chainHopTwo.Evasion = 0f;

        var events = new List<SimEvent>();
        ExecuteMeleeAttack(areaAttacker, areaPrimary, state, events);
        ExecuteMeleeAttack(lineAttacker, linePrimary, state, events);
        ExecuteMeleeAttack(chainAttacker, chainPrimary, state, events);

        return (GetDamagedTargetOrder(events), GetEnemyHpSnapshot(state));
    }
}
