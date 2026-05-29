namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class EffectApplicationSpecTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void ApplyEffectSpec_RequiresTargetTag()
    {
        var source = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        var events = new List<SimEvent>();

        var spec = new EffectApplicationSpec
        {
            EffectType = EffectType.Damage,
            Value = 20f,
            DamageType = DamageType.True,
            TagRequirements = new EffectTagRequirements
            {
                RequiredTargetTags = ["state.marked"],
            },
            Context = new EffectApplicationContext
            {
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
                SourcePosition = source.Position,
            },
        };

        AssertThat(SimEffects.ApplyEffect(_state, spec, target, events)).IsFalse();
        AssertThat(target.CurrentHp).IsEqual(100f);

        target.CombatTags.Add("state.marked");

        AssertThat(SimEffects.ApplyEffect(_state, spec, target, events)).IsTrue();
        AssertThat(target.CurrentHp).IsEqual(80f);
    }

    [TestCase]
    public void ApplyEffectSpec_BlockedTargetTagPreventsApplication()
    {
        var source = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, hp: 100f);
        target.ActiveBuffs.Add(
            new ActiveBuff
            {
                EffectType = EffectType.Shield,
                Duration = 5f,
                Lifetime = EffectLifetime.Timed(5f),
                GrantedTags = ["immunity.burn"],
            }
        );
        var events = new List<SimEvent>();

        var spec = new EffectApplicationSpec
        {
            EffectType = EffectType.StatusApply,
            Value = 3f,
            Duration = 4f,
            Lifetime = EffectLifetime.Timed(4f),
            StatusKind = StatusEffectKind.Burn,
            StatusPotencyPerStack = 3f,
            StatusTickInterval = 1f,
            TagRequirements = new EffectTagRequirements
            {
                BlockedTargetTags = ["immunity.burn"],
            },
            Context = new EffectApplicationContext
            {
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
            },
        };

        AssertThat(SimEffects.ApplyEffect(_state, spec, target, events)).IsFalse();
        AssertThat(target.ActiveBuffs.Count(b => b.StatusKind == StatusEffectKind.Burn)).IsEqual(0);
    }

    [TestCase]
    public void ApplyEffectSpec_RefreshStackPolicyUpdatesSingleBuff()
    {
        var source = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 2f);
        var events = new List<SimEvent>();

        var spec = new EffectApplicationSpec
        {
            EffectType = EffectType.AttackSpeedModifier,
            Value = 0.25f,
            Duration = 3f,
            Lifetime = EffectLifetime.Timed(3f),
            StackPolicy = EffectStackPolicy.RefreshDuration,
            StackKey = "flow_aura",
            Context = new EffectApplicationContext
            {
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
            },
        };

        SimEffects.ApplyEffect(_state, spec, target, events);
        var refreshed = new EffectApplicationSpec
        {
            EffectType = EffectType.AttackSpeedModifier,
            Value = 0.25f,
            Duration = 5f,
            Lifetime = EffectLifetime.Timed(5f),
            StackPolicy = EffectStackPolicy.RefreshDuration,
            StackKey = "flow_aura",
            Context = spec.Context,
        };
        SimEffects.ApplyEffect(_state, refreshed, target, events);

        AssertThat(
                target.ActiveBuffs.Count(b =>
                    b.EffectType == EffectType.AttackSpeedModifier && b.StackKey == "flow_aura"
                )
            )
            .IsEqual(1);
        AssertThat(target.ActiveBuffs[0].Duration).IsEqual(5f);
    }

    [TestCase]
    public void ApplyEffectSpec_EmitsActiveAndRemovedCueEvents()
    {
        var source = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        var target = SimTestHelper.CreateMeleeUnit(_state, 0, x: 2f);
        var events = new List<SimEvent>();

        var spec = new EffectApplicationSpec
        {
            EffectType = EffectType.Shield,
            Value = 10f,
            Duration = 0.05f,
            Lifetime = EffectLifetime.Timed(0.05f),
            CueId = "cue.test.shield",
            Context = new EffectApplicationContext
            {
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
            },
        };

        SimEffects.ApplyEffect(_state, spec, target, events);
        SimEffects.TickBuffs(_state, 0.1f, events);

        AssertThat(
                events.OfType<EffectCueEvent>().Any(e =>
                    e.CueId == "cue.test.shield" && e.Phase == EffectCuePhase.Active
                )
            )
            .IsTrue();
        AssertThat(
                events.OfType<EffectCueEvent>().Any(e =>
                    e.CueId == "cue.test.shield" && e.Phase == EffectCuePhase.Removed
                )
            )
            .IsTrue();
    }

    [TestCase]
    public void OnBuffRemovedAbility_FiresWhenBuffExpires()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f, hp: 100f);
        unit.CurrentHp = 60f;
        unit.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 77,
                EffectType = EffectType.Slow,
                Value = 0.2f,
                Duration = 0.05f,
                Lifetime = EffectLifetime.Timed(0.05f),
            }
        );
        unit.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "recover_on_clean",
                Trigger = UnitAbilityTrigger.OnBuffRemoved,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Heal,
                        Value = 15f,
                    },
                ],
            }
        );
        var events = new List<SimEvent>();

        SimEffects.TickBuffs(_state, 0.1f, events);

        AssertThat(unit.CurrentHp).IsEqual(75f);
        AssertThat(
                events.OfType<AbilityActivatedEvent>().Any(e =>
                    e.SourceUnitId == unit.UnitId && e.AbilityId == "recover_on_clean"
                )
            )
            .IsTrue();
    }

    [TestCase]
    public void DebugAbilityLogs_EmitForAbilityActivationAndEffects()
    {
        var state = SimTestHelper.CreateBattleState();
        var source = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        source.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "debug_log_test",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Damage,
                        Value = 5f,
                        DamageType = DamageType.True,
                    },
                ],
            }
        );
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);
        var events = new List<SimEvent>();
        var logs = new List<string>();
        var oldEnabled = Simulation.DebugAbilityLogsEnabled;
        var oldLog = Simulation.Log;

        try
        {
            Simulation.DebugAbilityLogsEnabled = true;
            Simulation.Log = logs.Add;

            SimAbilityOrchestrator.TryActivateOnHitEffects(state, source, target, events);
        }
        finally
        {
            Simulation.DebugAbilityLogsEnabled = oldEnabled;
            Simulation.Log = oldLog;
        }

        AssertThat(logs.Any(line => line.Contains("used Debug Log Test after hitting"))).IsTrue();
        AssertThat(logs.Any(line => line.Contains("100 -> 95 hp"))).IsTrue();
        AssertThat(logs.Any(line => line.Contains("ability=debug_log_test"))).IsFalse();
        AssertThat(logs.Any(line => line.Contains("effect=Damage"))).IsFalse();
    }

    [TestCase]
    public void DebugAbilityLogs_StatusConsumeReportsMissingStacks()
    {
        var state = SimTestHelper.CreateBattleState();
        var source = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);
        var events = new List<SimEvent>();
        var logs = new List<string>();
        var oldEnabled = Simulation.DebugAbilityLogsEnabled;
        var oldLog = Simulation.Log;

        try
        {
            Simulation.DebugAbilityLogsEnabled = true;
            Simulation.Log = logs.Add;

            SimEffects.ApplyEffect(
                state,
                new EffectApplicationSpec
                {
                    EffectType = EffectType.StatusConsume,
                    Value = 1.5f,
                    StatusKind = StatusEffectKind.Burn,
                    Context = new EffectApplicationContext
                    {
                        SourceUnitId = source.UnitId,
                        SourceTeam = source.Team,
                    },
                },
                target,
                events
            );
        }
        finally
        {
            Simulation.DebugAbilityLogsEnabled = oldEnabled;
            Simulation.Log = oldLog;
        }

        AssertThat(logs.Any(line => line.Contains("tried to cash out Burn"))).IsTrue();
        AssertThat(logs.Any(line => line.Contains("had no Burn stacks to consume"))).IsTrue();
    }

    [TestCase]
    public void DebugAbilityLogs_HealthRedistributionSummarizesMovedHp()
    {
        var state = SimTestHelper.CreateBattleState();
        var source = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        source.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "health_redistribution",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.HealthRedistributionPool,
                Delivery = UnitAbilityDelivery.Instant,
                Radius = 10f,
                CooldownSeconds = 2f,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.TransferHealth,
                        Value = 12f,
                    },
                ],
            }
        );
        var donor = SimTestHelper.CreateMeleeUnit(state, 0, x: 1f, hp: 100f);
        var receiver = SimTestHelper.CreateMeleeUnit(state, 0, x: 2f, hp: 20f);
        receiver.MaxHp = 100f;
        var events = new List<SimEvent>();
        var logs = new List<string>();
        var oldEnabled = Simulation.DebugAbilityLogsEnabled;
        var oldLog = Simulation.Log;

        try
        {
            Simulation.DebugAbilityLogsEnabled = true;
            Simulation.Log = logs.Add;

            SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);
        }
        finally
        {
            Simulation.DebugAbilityLogsEnabled = oldEnabled;
            Simulation.Log = oldLog;
        }

        AssertThat(donor.CurrentHp).IsEqual(88f);
        AssertThat(receiver.CurrentHp).IsEqual(32f);
        AssertThat(logs.Any(line => line.Contains("balanced nearby ally health"))).IsTrue();
        AssertThat(logs.Any(line => line.Contains("Moved 12 hp"))).IsTrue();
    }
}
