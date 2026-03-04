namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using GdUnit4;
using static GdUnit4.Assertions;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;

[TestSuite]
public class SimEffectsTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    // =========================================================================
    // ApplyEffect Dispatch
    // =========================================================================

    [TestCase]
    public void ApplyEffect_Damage_ReducesHp()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, hp: 100f);
        target.Evasion = 0f;
        var events = new List<SimEvent>();

        SimEffects.ApplyEffect(_state, EffectType.Damage, 30f, 0f, DamageType.True, target, -1, 0, events);

        AssertThat(target.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void ApplyEffect_Heal_RestoresHp()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, hp: 100f);
        target.CurrentHp = 50f;
        var events = new List<SimEvent>();

        SimEffects.ApplyEffect(_state, EffectType.Heal, 20f, 0f, DamageType.Physical, target, -1, 0, events);

        AssertThat(target.CurrentHp).IsEqual(70f);
    }

    [TestCase]
    public void ApplyEffect_Heal_CappedAtMaxHp()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, hp: 100f);
        target.CurrentHp = 90f;
        var events = new List<SimEvent>();

        SimEffects.ApplyEffect(_state, EffectType.Heal, 50f, 0f, DamageType.Physical, target, -1, 0, events);

        AssertThat(target.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void ApplyEffect_Shield_AddsBuff()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        var events = new List<SimEvent>();

        SimEffects.ApplyEffect(_state, EffectType.Shield, 50f, -1f, DamageType.Physical, target, -1, 0, events);

        AssertThat(target.ActiveBuffs.Count).IsEqual(1);
        AssertThat(target.ActiveBuffs[0].EffectType).IsEqual(EffectType.Shield);
        AssertThat(target.ActiveBuffs[0].ShieldHp).IsEqual(50f);
    }

    [TestCase]
    public void ApplyEffect_Slow_AddsBuff()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        var events = new List<SimEvent>();

        SimEffects.ApplyEffect(_state, EffectType.Slow, 0.3f, 5f, DamageType.Physical, target, -1, 0, events);

        AssertThat(target.ActiveBuffs.Count).IsEqual(1);
        AssertThat(target.ActiveBuffs[0].EffectType).IsEqual(EffectType.Slow);
        AssertThat(target.ActiveBuffs[0].Value).IsEqual(0.3f);
        AssertThat(target.ActiveBuffs[0].Duration).IsEqual(5f);
    }

    [TestCase]
    public void ApplyEffect_Stun_AddsBuff()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        var events = new List<SimEvent>();

        SimEffects.ApplyEffect(_state, EffectType.Stun, 0f, 2f, DamageType.Physical, target, -1, 0, events);

        AssertThat(target.ActiveBuffs.Count).IsEqual(1);
        AssertThat(target.ActiveBuffs[0].EffectType).IsEqual(EffectType.Stun);
    }

    [TestCase]
    public void ApplyEffect_DeadTarget_NoOp()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, hp: 100f);
        target.IsAlive = false;
        var events = new List<SimEvent>();

        SimEffects.ApplyEffect(_state, EffectType.Damage, 50f, 0f, DamageType.Physical, target, -1, 0, events);

        // HP unchanged, no events (other than what might already exist)
        AssertThat(target.CurrentHp).IsEqual(100f);
    }

    // =========================================================================
    // TickBuffs
    // =========================================================================

    [TestCase]
    public void TickBuffs_DecrementsDuration()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.Slow,
            Value = 0.3f,
            Duration = 5f
        });
        var events = new List<SimEvent>();

        SimEffects.TickBuffs(_state, 1f, events);

        AssertThat(unit.ActiveBuffs[0].Duration).IsEqual(4f);
    }

    [TestCase]
    public void TickBuffs_ExpiredBuff_Removed()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            BuffId = 99,
            EffectType = EffectType.Slow,
            Value = 0.3f,
            Duration = 0.5f // Will expire with 1s tick
        });
        var events = new List<SimEvent>();

        SimEffects.TickBuffs(_state, 1f, events);

        AssertThat(unit.ActiveBuffs.Count).IsEqual(0);

        var expired = SimTestHelper.FindEvent<BuffExpiredEvent>(events);
        AssertThat(expired).IsNotNull();
        AssertThat(expired!.BuffId).IsEqual(99);
    }

    [TestCase]
    public void TickBuffs_PermanentBuff_NeverExpires()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.DamageBoost,
            Value = 0.5f,
            Duration = -1f // Permanent
        });
        var events = new List<SimEvent>();

        // Tick many times
        for (int i = 0; i < 100; i++)
            SimEffects.TickBuffs(_state, 1f, events);

        AssertThat(unit.ActiveBuffs.Count).IsEqual(1);
    }

    [TestCase]
    public void TickBuffs_PeriodicDamage_TicksDot()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, hp: 100f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.Damage,
            Value = 10f, // 10 damage per tick
            Duration = 5f,
            TickInterval = 1f,
            TickTimer = 1f, // Ticks after 1s
            SourceUnitId = -1
        });
        var events = new List<SimEvent>();

        SimEffects.TickBuffs(_state, 1f, events);

        AssertThat(unit.CurrentHp).IsEqual(90f);
    }

    [TestCase]
    public void TickBuffs_DotKill_EmitsUnitDiedEvent()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, hp: 5f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.Damage,
            Value = 10f,
            Duration = 5f,
            TickInterval = 1f,
            TickTimer = 1f,
            SourceUnitId = -1
        });
        var events = new List<SimEvent>();

        SimEffects.TickBuffs(_state, 1f, events);

        AssertThat(unit.IsAlive).IsFalse();
        AssertThat(unit.CurrentHp).IsEqual(0f);

        var died = SimTestHelper.FindEvent<UnitDiedEvent>(events);
        AssertThat(died).IsNotNull();
    }

    // =========================================================================
    // TickDelayedEffects
    // =========================================================================

    [TestCase]
    public void TickDelayedEffects_CountsDown()
    {
        _state.DelayedEffects.Add(new DelayedEffect
        {
            Timer = 2f,
            EffectType = EffectType.Damage,
            Value = 50f,
            AoeRadius = 5f,
            Position = SimVector3.Zero,
            SourceTeam = 0
        });
        var events = new List<SimEvent>();

        SimEffects.TickDelayedEffects(_state, 1f, events);

        AssertThat(_state.DelayedEffects.Count).IsEqual(1);
        AssertThat(_state.DelayedEffects[0].Timer).IsEqual(1f);
    }

    [TestCase]
    public void TickDelayedEffects_Expired_ExecutesAoeDamage()
    {
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 0f, hp: 100f);
        enemy.Evasion = 0f;

        _state.DelayedEffects.Add(new DelayedEffect
        {
            Timer = 0.5f,
            EffectType = EffectType.Damage,
            Value = 30f,
            DamageType = DamageType.True,
            AoeRadius = 10f,
            Position = SimVector3.Zero,
            SourceUnitId = -1,
            SourceTeam = 0
        });
        var events = new List<SimEvent>();

        SimEffects.TickDelayedEffects(_state, 1f, events);

        AssertThat(_state.DelayedEffects.Count).IsEqual(0);
        AssertThat(enemy.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void TickDelayedEffects_Removed_AfterExecution()
    {
        _state.DelayedEffects.Add(new DelayedEffect
        {
            Timer = 0.1f,
            EffectType = EffectType.Damage,
            Value = 10f,
            AoeRadius = 0f,
            Position = SimVector3.Zero,
            SourceTeam = 0
        });
        var events = new List<SimEvent>();

        SimEffects.TickDelayedEffects(_state, 1f, events);

        AssertThat(_state.DelayedEffects.Count).IsEqual(0);
    }

    // =========================================================================
    // FireTriggers
    // =========================================================================

    [TestCase]
    public void FireTriggers_OnHit_AppliesEffect()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        attacker.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnHit,
            EffectType = EffectType.Slow,
            Value = 0.5f,
            Duration = 3f
        });

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f);
        var events = new List<SimEvent>();

        SimEffects.FireTriggers(_state, attacker, TriggerType.OnHit, target, events);

        AssertThat(target.ActiveBuffs.Count).IsEqual(1);
        AssertThat(target.ActiveBuffs[0].EffectType).IsEqual(EffectType.Slow);
    }

    [TestCase]
    public void FireTriggers_HpThreshold_FiresBelowThreshold()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, hp: 100f);
        unit.CurrentHp = 30f; // 30% HP
        unit.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.HpThreshold,
            EffectType = EffectType.DamageBoost,
            Value = 0.5f,
            Duration = -1f,
            Threshold = 0.5f // Fire at 50% HP
        });
        var events = new List<SimEvent>();

        // TickBuffs checks HP threshold triggers
        SimEffects.TickBuffs(_state, 0.016f, events);

        AssertThat(unit.ActiveBuffs.Count).IsEqual(1);
        AssertThat(unit.ActiveBuffs[0].EffectType).IsEqual(EffectType.DamageBoost);
    }

    [TestCase]
    public void FireTriggers_HpThreshold_OnlyFiresOnce()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, hp: 100f);
        unit.CurrentHp = 30f;
        unit.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.HpThreshold,
            EffectType = EffectType.DamageBoost,
            Value = 0.5f,
            Duration = -1f,
            Threshold = 0.5f
        });
        var events = new List<SimEvent>();

        SimEffects.TickBuffs(_state, 0.016f, events);
        SimEffects.TickBuffs(_state, 0.016f, events);
        SimEffects.TickBuffs(_state, 0.016f, events);

        // Should only fire once despite being below threshold multiple ticks
        AssertThat(unit.ActiveBuffs.Count).IsEqual(1);
    }

    [TestCase]
    public void FireTriggers_AoeTrigger_HitsEnemiesInRadius()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        attacker.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnHit,
            EffectType = EffectType.Damage,
            Value = 20f,
            DamageType = DamageType.True,
            AoeRadius = 10f
        });

        var nearEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3f, hp: 100f);
        nearEnemy.Evasion = 0f;
        var farEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 30f, hp: 100f);
        farEnemy.Evasion = 0f;

        var events = new List<SimEvent>();

        // Fire OnHit trigger — AoE should hit enemies in radius
        SimEffects.FireTriggers(_state, attacker, TriggerType.OnHit, nearEnemy, events);

        AssertThat(nearEnemy.CurrentHp).IsLess(100f);
        AssertThat(farEnemy.CurrentHp).IsEqual(100f);
    }

    // =========================================================================
    // FireDeathTriggers
    // =========================================================================

    [TestCase]
    public void FireDeathTriggers_OnDeath_Fires()
    {
        var dying = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        dying.IsAlive = false; // About to die
        dying.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnDeath,
            EffectType = EffectType.Damage,
            Value = 50f,
            DamageType = DamageType.True,
            AoeRadius = 5f
        });

        var nearEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        nearEnemy.Evasion = 0f;
        var events = new List<SimEvent>();

        // Temporarily mark as alive so ApplyEffect doesn't skip
        dying.IsAlive = true;
        SimEffects.FireDeathTriggers(_state, dying, null, events);

        // AoE death trigger should hit nearby enemy
        AssertThat(nearEnemy.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void FireDeathTriggers_LeaderDeath_FiresOnGroupMembers()
    {
        var leader = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        leader.GroupId = 1;
        leader.IsAlive = false;

        var follower = SimTestHelper.CreateMeleeUnit(_state, 0, x: 2f);
        follower.LeaderId = leader.UnitId;
        // LeaderDeath with AoE — damages nearby enemies when leader dies
        follower.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.LeaderDeath,
            EffectType = EffectType.Damage,
            Value = 30f,
            DamageType = DamageType.True,
            AoeRadius = 10f
        });

        var nearEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 3f, hp: 100f);
        nearEnemy.Evasion = 0f;
        var events = new List<SimEvent>();

        SimEffects.FireDeathTriggers(_state, leader, null, events);

        // LeaderDeath trigger should fire on follower, dealing AoE damage to nearby enemies
        AssertThat(nearEnemy.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void FireDeathTriggers_DelayedExplosion_QueuesDelayedEffect()
    {
        var dying = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        dying.Triggers.Add(new TriggerConfig
        {
            TriggerType = TriggerType.OnDeath,
            EffectType = EffectType.Damage,
            Value = 100f,
            DamageType = DamageType.True,
            AoeRadius = 5f,
            Delay = 1.5f // Delayed explosion
        });
        var events = new List<SimEvent>();

        SimEffects.FireDeathTriggers(_state, dying, null, events);

        // Should be queued as delayed effect, not instant
        AssertThat(_state.DelayedEffects.Count).IsEqual(1);
        AssertThat(_state.DelayedEffects[0].Timer).IsEqual(1.5f);
        AssertThat(_state.DelayedEffects[0].Value).IsEqual(100f);
    }

    // =========================================================================
    // Shield System
    // =========================================================================

    [TestCase]
    public void AbsorbWithShields_FullAbsorption()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        SimEffects.ApplyShield(_state, target, 100f, -1, 0);

        float remaining = SimEffects.AbsorbWithShields(target, 50f, null);

        AssertThat(remaining).IsEqual(0f);
        AssertThat(target.ActiveBuffs.Count).IsEqual(1); // Shield still exists
        AssertThat(target.ActiveBuffs[0].ShieldHp).IsEqual(50f); // Partially consumed
    }

    [TestCase]
    public void AbsorbWithShields_PartialAbsorption()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        SimEffects.ApplyShield(_state, target, 30f, -1, 0);

        float remaining = SimEffects.AbsorbWithShields(target, 50f, null);

        AssertThat(remaining).IsEqual(20f);
        AssertThat(target.ActiveBuffs.Count).IsEqual(0); // Shield fully consumed
    }

    [TestCase]
    public void AbsorbWithShields_OldestFirst()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        SimEffects.ApplyShield(_state, target, 20f, -1, 0); // Oldest
        SimEffects.ApplyShield(_state, target, 50f, -1, 0); // Newer

        float remaining = SimEffects.AbsorbWithShields(target, 30f, null);

        // 20 (oldest) consumed, 10 from newer
        AssertThat(remaining).IsEqual(0f);
        AssertThat(target.ActiveBuffs.Count).IsEqual(1); // Only newer shield remains
        AssertThat(target.ActiveBuffs[0].ShieldHp).IsEqual(40f);
    }

    // =========================================================================
    // Stat Queries
    // =========================================================================

    [TestCase]
    public void GetEffectiveMoveSpeed_NoBuffs_ReturnsBase()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, moveSpeed: 5f);

        float speed = SimEffects.GetEffectiveMoveSpeed(unit);

        AssertThat(speed).IsEqual(5f);
    }

    [TestCase]
    public void GetEffectiveMoveSpeed_Slow_Reduces()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, moveSpeed: 10f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.Slow,
            Value = 0.3f // 30% slow
        });

        float speed = SimEffects.GetEffectiveMoveSpeed(unit);

        AssertThat(speed).IsEqual(7f); // 10 * (1 - 0.3) = 7
    }

    [TestCase]
    public void GetEffectiveMoveSpeed_Haste_Increases()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, moveSpeed: 10f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.Haste,
            Value = 0.5f // 50% haste
        });

        float speed = SimEffects.GetEffectiveMoveSpeed(unit);

        AssertThat(speed).IsEqual(15f); // 10 * (1 + 0.5) = 15
    }

    [TestCase]
    public void GetEffectiveAttackDamage_DamageBoost_Increases()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, damage: 20f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.DamageBoost,
            Value = 0.5f // 50% boost
        });

        float damage = SimEffects.GetEffectiveAttackDamage(unit);

        AssertThat(damage).IsEqual(30f); // 20 * (1 + 0.5) = 30
    }

    [TestCase]
    public void IsStunned_WithStun_ReturnsTrue()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.ActiveBuffs.Add(new ActiveBuff
        {
            EffectType = EffectType.Stun,
            Duration = 2f
        });

        AssertThat(SimEffects.IsStunned(unit)).IsTrue();
    }

    [TestCase]
    public void IsStunned_NoStun_ReturnsFalse()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);

        AssertThat(SimEffects.IsStunned(unit)).IsFalse();
    }
}
