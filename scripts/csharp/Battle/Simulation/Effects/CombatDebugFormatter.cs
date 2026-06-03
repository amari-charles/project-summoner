using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fateforged.Constants;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Effects;

public sealed class UnitDebugSnapshot
{
    public int UnitId { get; init; }
    public string Name { get; init; } = "";
    public float Hp { get; init; }
    public bool IsAlive { get; init; }
    public SimVector3 Position { get; init; }
    public float ShieldHp { get; init; }
    public Dictionary<StatusEffectKind, StatusDebugSnapshot> Statuses { get; init; } = new();
    public Dictionary<EffectType, int> BuffCounts { get; init; } = new();
}

public sealed class StatusDebugSnapshot
{
    public int Stacks { get; init; }
    public float DamagePerTick { get; init; }
    public float Duration { get; init; }
}

public static class CombatDebugFormatter
{
    public static UnitDebugSnapshot Capture(UnitData unit)
    {
        var statuses = new Dictionary<StatusEffectKind, StatusDebugSnapshot>();
        foreach (var group in unit.ActiveBuffs.Where(IsStatusBuff).GroupBy(b => b.StatusKind))
        {
            var strongest = group
                .OrderByDescending(b => b.StackCount)
                .ThenByDescending(b => b.Value)
                .First();
            statuses[group.Key] = new StatusDebugSnapshot
            {
                Stacks = group.Sum(b => Math.Max(1, b.StackCount)),
                DamagePerTick = strongest.Value,
                Duration = EffectLifetimeResolver.ResolveDuration(strongest.Lifetime, strongest.Duration),
            };
        }

        return new UnitDebugSnapshot
        {
            UnitId = unit.UnitId,
            Name = UnitName(unit),
            Hp = unit.CurrentHp,
            IsAlive = unit.IsAlive,
            Position = unit.Position,
            ShieldHp = unit.ActiveBuffs
                .Where(b => b.EffectType == EffectType.Shield)
                .Sum(b => MathF.Max(0f, b.ShieldHp)),
            Statuses = statuses,
            BuffCounts = unit.ActiveBuffs
                .GroupBy(b => b.EffectType)
                .ToDictionary(g => g.Key, g => g.Count()),
        };
    }

    public static Dictionary<int, UnitDebugSnapshot> CaptureUnits(IEnumerable<UnitData> units)
    {
        var snapshots = new Dictionary<int, UnitDebugSnapshot>();
        foreach (var unit in units)
            snapshots[unit.UnitId] = Capture(unit);
        return snapshots;
    }

    public static string FormatAbilityActivation(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitData? contextTarget,
        IReadOnlyList<UnitData> targets,
        IReadOnlyList<UnitAbilityEffectState> effects,
        int appliedCount,
        IReadOnlyDictionary<int, UnitDebugSnapshot> before
    )
    {
        var participants = BuildParticipants(state, source, contextTarget, targets, before);
        var builder = new StringBuilder();
        builder.Append(TimeLabel(state));
        builder.Append(' ');
        builder.Append(UnitName(source, participants));
        builder.Append(' ');
        builder.Append(AbilityPhrase(ability));
        builder.Append(ContextPhrase(ability, contextTarget, participants));
        builder.Append('.');

        AppendAbilityOutcomeLines(builder, state, targets, effects, before, participants);
        if (builder.ToString().IndexOf('\n') < 0 && appliedCount > 0)
            builder.Append($"\n  Queued {appliedCount} effect{Plural(appliedCount)}.");

        return builder.ToString();
    }

    public static string FormatEffectApplied(
        MatchState state,
        EffectApplicationSpec spec,
        UnitData target,
        UnitDebugSnapshot before
    )
    {
        var participants = BuildParticipants(state, target, null, [target], null);
        var source = SourceName(state, spec.Context.SourceUnitId, participants);
        var builder = new StringBuilder();
        builder.Append(TimeLabel(state));
        builder.Append(' ');
        builder.Append(source);
        builder.Append(' ');
        builder.Append(EffectPhrase(spec));
        builder.Append(' ');
        builder.Append(UnitName(target, participants));
        builder.Append('.');
        AppendEffectOutcomeLines(builder, state, target, spec, before, participants);
        return builder.ToString();
    }

    public static string FormatEffectSkipped(
        MatchState state,
        EffectApplicationSpec spec,
        UnitData target,
        string reason
    )
    {
        var participants = BuildParticipants(state, target, null, [target], null);
        var source = SourceName(state, spec.Context.SourceUnitId, participants);
        string effect = EffectDisplayName(spec);
        string targetName = UnitName(target, participants);
        string plainReason = reason switch
        {
            "target_dead" => $"{targetName} was already defeated",
            "requirements_failed" => $"{targetName} did not meet the effect requirements",
            _ => reason.Replace('_', ' '),
        };
        return $"{TimeLabel(state)} {source}'s {effect} did nothing: {plainReason}.";
    }

    public static string FormatReviveConsumed(UnitData target, ActiveBuff buff, float hpBefore)
    {
        float revivePercent = buff.Value > 0f ? buff.Value : 0.5f;
        return $"{UnitNameWithId(target)} revived instead of dying.\n"
            + $"  {UnitNameWithId(target)}: {Amount(hpBefore)} -> {Amount(target.CurrentHp)} hp "
            + $"({Percent(revivePercent)} revive).";
    }

    public static string FormatAttackAvoided(
        MatchState state,
        UnitData? attacker,
        UnitData target,
        float chance,
        bool attackerMissed
    )
    {
        string targetName = UnitNameWithId(target);
        if (attackerMissed)
        {
            string attackerName = attacker != null ? UnitNameWithId(attacker) : "Unknown attacker";
            return $"{TimeLabel(state)} {attackerName} missed {targetName}.\n"
                + $"  Miss chance: {Percent(chance)}. No damage was applied.";
        }

        string source = attacker != null ? $" from {UnitNameWithId(attacker)}" : "";
        return $"{TimeLabel(state)} {targetName} dodged an attack{source}.\n"
            + $"  Dodge chance: {Percent(chance)}. No damage was applied.";
    }

    private static void AppendAbilityOutcomeLines(
        StringBuilder builder,
        MatchState state,
        IReadOnlyList<UnitData> targets,
        IReadOnlyList<UnitAbilityEffectState> effects,
        IReadOnlyDictionary<int, UnitDebugSnapshot> before,
        IReadOnlyList<UnitData> participants
    )
    {
        bool wrote = false;
        foreach (var effect in effects)
        {
            if (effect.EffectType == EffectType.TransferHealth)
            {
                wrote |= AppendHealthRedistributionLines(builder, state, before, participants);
                continue;
            }

            if (effect.EffectType == EffectType.Shield)
                wrote |= AppendShieldTargetCountLine(builder, targets, before);

            foreach (var target in targets)
            {
                if (!before.TryGetValue(target.UnitId, out var snapshot))
                    continue;
                wrote |= AppendEffectOutcomeLines(
                    builder,
                    state,
                    target,
                    EffectSpecView.FromAbilityEffect(effect),
                    snapshot,
                    participants
                );
            }
        }

        if (!wrote)
        {
            foreach (var target in targets)
            {
                if (before.TryGetValue(target.UnitId, out var snapshot))
                    wrote |= AppendUnitStateDelta(builder, state, target, snapshot, participants);
            }
        }
    }

    private static bool AppendShieldTargetCountLine(
        StringBuilder builder,
        IReadOnlyList<UnitData> targets,
        IReadOnlyDictionary<int, UnitDebugSnapshot> before
    )
    {
        int shielded = 0;
        foreach (var target in targets)
        {
            if (!before.TryGetValue(target.UnitId, out var snapshot))
                continue;
            if (CurrentShield(target) > snapshot.ShieldHp)
                shielded++;
        }

        if (shielded <= 0)
            return false;

        builder.Append('\n');
        builder.Append($"  Shielded {shielded} {AllyWord(shielded)}.");
        return true;
    }

    private static bool AppendEffectOutcomeLines(
        StringBuilder builder,
        MatchState state,
        UnitData target,
        EffectApplicationSpec spec,
        UnitDebugSnapshot before,
        IReadOnlyList<UnitData> participants
    )
    {
        return AppendEffectOutcomeLines(
            builder,
            state,
            target,
            EffectSpecView.FromApplicationSpec(spec),
            before,
            participants
        );
    }

    private static bool AppendEffectOutcomeLines(
        StringBuilder builder,
        MatchState state,
        UnitData target,
        EffectSpecView effect,
        UnitDebugSnapshot before,
        IReadOnlyList<UnitData> participants
    )
    {
        bool wrote = false;
        string targetName = UnitName(target, participants);
        switch (effect.EffectType)
        {
            case EffectType.Damage:
            case EffectType.AreaDamage:
                wrote |= AppendUnitStateDelta(builder, state, target, before, participants);
                break;

            case EffectType.Heal:
                wrote |= AppendUnitStateDelta(builder, state, target, before, participants);
                break;

            case EffectType.Shield:
                float shieldGain = CurrentShield(target) - before.ShieldHp;
                if (shieldGain > 0f)
                {
                    builder.Append('\n');
                    builder.Append($"  {targetName} gained {Amount(shieldGain)} shield");
                    AppendDuration(builder, effect.Duration);
                    builder.Append('.');
                    wrote = true;
                }
                break;

            case EffectType.StatusApply:
                wrote |= AppendStatusApplyLine(builder, target, effect, before, participants);
                break;

            case EffectType.StatusConsume:
                wrote |= AppendStatusConsumeLine(builder, target, effect, before, participants);
                wrote |= AppendUnitStateDelta(builder, state, target, before, participants);
                break;

            case EffectType.Knockback:
            case EffectType.Displacement:
            case EffectType.SourceLungeToTarget:
                wrote |= AppendMovementLine(builder, target, effect.EffectType, before, participants);
                break;

            case EffectType.Cleanse:
                builder.Append('\n');
                builder.Append($"  {targetName} was cleansed.");
                wrote = true;
                break;

            default:
                wrote |= AppendBuffLine(builder, target, effect, before, participants);
                break;
        }

        return wrote;
    }

    private static bool AppendUnitStateDelta(
        StringBuilder builder,
        MatchState state,
        UnitData target,
        UnitDebugSnapshot before,
        IReadOnlyList<UnitData> participants
    )
    {
        if (NearlyEqual(before.Hp, target.CurrentHp) && before.IsAlive == target.IsAlive)
            return false;

        string name = UnitName(target, participants);
        builder.Append('\n');
        builder.Append($"  {name}: {Amount(before.Hp)} -> {Amount(target.CurrentHp)} hp");
        if (before.IsAlive && !target.IsAlive)
            builder.Append(target.Team == Team.Enemy ? ", defeated" : ", died");
        else if (!before.IsAlive && target.IsAlive)
            builder.Append(", revived");
        builder.Append('.');
        return true;
    }

    private static bool AppendStatusApplyLine(
        StringBuilder builder,
        UnitData target,
        EffectSpecView effect,
        UnitDebugSnapshot before,
        IReadOnlyList<UnitData> participants
    )
    {
        var statusKind = ResolveStatusKind(effect);
        var after = Capture(target);
        before.Statuses.TryGetValue(statusKind, out var oldStatus);
        after.Statuses.TryGetValue(statusKind, out var newStatus);
        if (newStatus == null)
            return false;

        string targetName = UnitName(target, participants);
        string status = StatusName(statusKind);
        builder.Append('\n');
        builder.Append(
            $"  {targetName}: {status} {oldStatus?.Stacks ?? 0} -> {newStatus.Stacks} stack{Plural(newStatus.Stacks)}"
        );
        if (newStatus.DamagePerTick > 0f)
            builder.Append($", {Amount(newStatus.DamagePerTick)} damage per tick");
        AppendDuration(builder, newStatus.Duration);
        builder.Append('.');
        return true;
    }

    private static bool AppendStatusConsumeLine(
        StringBuilder builder,
        UnitData target,
        EffectSpecView effect,
        UnitDebugSnapshot before,
        IReadOnlyList<UnitData> participants
    )
    {
        var statusKind = ResolveStatusKind(effect);
        var after = Capture(target);
        int beforeStacks = before.Statuses.TryGetValue(statusKind, out var oldStatus)
            ? oldStatus.Stacks
            : 0;
        int afterStacks = after.Statuses.TryGetValue(statusKind, out var newStatus)
            ? newStatus.Stacks
            : 0;
        string status = StatusName(statusKind);
        string targetName = UnitName(target, participants);

        builder.Append('\n');
        if (beforeStacks <= 0)
        {
            builder.Append($"  {targetName} had no {status} stacks to consume.");
            return true;
        }

        int consumed = Math.Max(0, beforeStacks - afterStacks);
        builder.Append(
            $"  Consumed {consumed} {status} stack{Plural(consumed)} from {targetName}"
        );
        if (effect.Value > 0f)
            builder.Append($" at {Amount(effect.Value)}x payout");
        builder.Append('.');
        return true;
    }

    private static bool AppendBuffLine(
        StringBuilder builder,
        UnitData target,
        EffectSpecView effect,
        UnitDebugSnapshot before,
        IReadOnlyList<UnitData> participants
    )
    {
        int beforeCount = before.BuffCounts.GetValueOrDefault(effect.EffectType);
        int afterCount = target.ActiveBuffs.Count(b => b.EffectType == effect.EffectType);
        if (afterCount <= beforeCount)
            return false;

        string targetName = UnitName(target, participants);
        builder.Append('\n');
        builder.Append("  ");
        builder.Append(effect.EffectType switch
        {
            EffectType.Slow => $"{targetName} was slowed",
            EffectType.Stun => $"{targetName} was stunned",
            EffectType.Root => $"{targetName} was rooted",
            EffectType.Haste => $"{targetName} moved faster",
            EffectType.DamageBoost => $"{targetName}'s damage increased by {Percent(effect.Value)}",
            EffectType.EvasionModifier => effect.Value >= 0f
                ? $"{targetName}'s dodge chance increased by {Percent(effect.Value)}"
                : $"{targetName}'s dodge chance decreased by {Percent(MathF.Abs(effect.Value))}",
            EffectType.AttackSpeedModifier => effect.Value >= 0f
                ? $"{targetName}'s attack speed increased by {Percent(effect.Value)}"
                : $"{targetName}'s attack speed decreased by {Percent(MathF.Abs(effect.Value))}",
            EffectType.FlatDamageReduction => $"{targetName} reduced incoming damage by {Amount(effect.Value)}",
            EffectType.AccuracyModifier => effect.Value < 0f
                ? $"{targetName}'s hit chance dropped by {Percent(MathF.Abs(effect.Value))}"
                : $"{targetName}'s hit chance increased by {Percent(effect.Value)}",
            EffectType.RangedDamageModifier => effect.Value < 0f
                ? $"{targetName}'s ranged damage dropped by {Percent(MathF.Abs(effect.Value))}"
                : $"{targetName}'s ranged damage increased by {Percent(effect.Value)}",
            EffectType.ReviveOnDeath => $"{targetName} will revive once at {Percent(effect.Value > 0f ? effect.Value : 0.5f)} hp",
            EffectType.TornadoCarry => $"{targetName} was lifted by the tornado",
            EffectType.Taunt => $"{targetName} was taunted",
            _ => $"{targetName} gained {EffectDisplayName(effect)}",
        });
        AppendDuration(builder, effect.Duration);
        builder.Append('.');
        return true;
    }

    private static bool AppendMovementLine(
        StringBuilder builder,
        UnitData target,
        EffectType effectType,
        UnitDebugSnapshot before,
        IReadOnlyList<UnitData> participants
    )
    {
        float distance = before.Position.DistanceTo(target.Position);
        if (distance <= 0.01f)
            return false;

        string verb = effectType == EffectType.SourceLungeToTarget ? "lunged" : "moved";
        builder.Append('\n');
        builder.Append($"  {UnitName(target, participants)} {verb} {Amount(distance)} units.");
        return true;
    }

    private static bool AppendHealthRedistributionLines(
        StringBuilder builder,
        MatchState state,
        IReadOnlyDictionary<int, UnitDebugSnapshot> before,
        IReadOnlyList<UnitData> participants
    )
    {
        var donors = new List<(UnitData Unit, float Amount)>();
        var receivers = new List<(UnitData Unit, float Amount)>();
        foreach (var pair in before)
        {
            if (!state.Units.TryGetValue(pair.Key, out var unit))
                continue;
            float delta = unit.CurrentHp - pair.Value.Hp;
            if (delta < -0.01f)
                donors.Add((unit, -delta));
            else if (delta > 0.01f)
                receivers.Add((unit, delta));
        }

        if (donors.Count == 0 && receivers.Count == 0)
        {
            builder.Append('\n');
            builder.Append("  No ally needed health redistribution.");
            return true;
        }

        float moved = receivers.Sum(r => r.Amount);
        string donorText = string.Join(", ", donors.Select(d => UnitName(d.Unit, participants)));
        string receiverText = string.Join(", ", receivers.Select(r => UnitName(r.Unit, participants)));
        builder.Append('\n');
        builder.Append($"  Moved {Amount(moved)} hp from {donorText} to {receiverText}.");

        foreach (var change in donors.Concat(receivers).OrderBy(c => c.Unit.UnitId))
        {
            var snapshot = before[change.Unit.UnitId];
            builder.Append('\n');
            builder.Append(
                $"  {UnitName(change.Unit, participants)}: {Amount(snapshot.Hp)} -> {Amount(change.Unit.CurrentHp)} hp."
            );
        }
        return true;
    }

    private static string AbilityPhrase(UnitAbilityState ability)
    {
        return ability.AbilityId switch
        {
            "contact_self_destruct" => "detonated",
            "death_burst" => "released a death blast",
            "health_redistribution" => "balanced nearby ally health",
            "burn_on_hit" => "ignited its target",
            "channel_burn_tick" => "channeled flame",
            "overheat_growth" => "overheated",
            "burrow_lunge" => "ambushed from below",
            "miss_chance_aura" => "distorted enemy aim",
            "speed_aura" => "quickened nearby allies",
            _ => $"used {Titleize(ability.AbilityId)}",
        };
    }

    private static string ContextPhrase(
        UnitAbilityState ability,
        UnitData? contextTarget,
        IReadOnlyList<UnitData> participants
    )
    {
        if (contextTarget == null)
            return ability.Trigger switch
            {
                UnitAbilityTrigger.OnDeath => " on death",
                UnitAbilityTrigger.OnSpawn => " on spawn",
                UnitAbilityTrigger.Periodic => "",
                UnitAbilityTrigger.OnBuffRemoved => " after a buff ended",
                _ => "",
            };

        string targetName = UnitName(contextTarget, participants);
        return ability.Trigger switch
        {
            UnitAbilityTrigger.OnHit => $" after hitting {targetName}",
            UnitAbilityTrigger.OnDamaged => $" after being hit by {targetName}",
            UnitAbilityTrigger.OnDeath => $" after being killed by {targetName}",
            _ => $" near {targetName}",
        };
    }

    private static string EffectPhrase(EffectApplicationSpec spec)
    {
        return spec.EffectType switch
        {
            EffectType.Damage or EffectType.AreaDamage => "damaged",
            EffectType.Heal => "healed",
            EffectType.Shield => "shielded",
            EffectType.StatusApply => $"applied {StatusName(ResolveStatusKind(spec))} to",
            EffectType.StatusConsume => $"tried to cash out {StatusName(ResolveStatusKind(spec))} on",
            EffectType.Cleanse => "cleansed",
            EffectType.Knockback or EffectType.Displacement => "moved",
            EffectType.SourceLungeToTarget => "lunged toward",
            _ => $"affected",
        };
    }

    private static string EffectDisplayName(EffectApplicationSpec spec) =>
        spec.EffectType == EffectType.StatusApply || spec.EffectType == EffectType.StatusConsume
            ? StatusName(ResolveStatusKind(spec))
            : Titleize(spec.EffectType.ToString());

    private static string EffectDisplayName(EffectSpecView effect) =>
        effect.EffectType == EffectType.StatusApply || effect.EffectType == EffectType.StatusConsume
            ? StatusName(ResolveStatusKind(effect))
            : Titleize(effect.EffectType.ToString());

    private static StatusEffectKind ResolveStatusKind(EffectApplicationSpec spec) =>
        spec.StatusKind == StatusEffectKind.None ? StatusEffectKind.Burn : spec.StatusKind;

    private static StatusEffectKind ResolveStatusKind(EffectSpecView effect) =>
        effect.StatusKind == StatusEffectKind.None ? StatusEffectKind.Burn : effect.StatusKind;

    private static float CurrentShield(UnitData unit) =>
        unit.ActiveBuffs.Where(b => b.EffectType == EffectType.Shield).Sum(b => MathF.Max(0f, b.ShieldHp));

    private static bool IsStatusBuff(ActiveBuff buff) =>
        buff.EffectType == EffectType.Damage
        && buff.TickInterval > 0f
        && buff.StatusKind != StatusEffectKind.None;

    private static string SourceName(
        MatchState state,
        int sourceUnitId,
        IReadOnlyList<UnitData> participants
    )
    {
        if (state.Units.TryGetValue(sourceUnitId, out var source))
            return UnitName(source, participants);
        return sourceUnitId < 0 ? "Summoner" : "Unknown source";
    }

    private static IReadOnlyList<UnitData> BuildParticipants(
        MatchState state,
        UnitData source,
        UnitData? contextTarget,
        IReadOnlyList<UnitData> targets,
        IReadOnlyDictionary<int, UnitDebugSnapshot>? before
    )
    {
        var participants = new Dictionary<int, UnitData>();
        participants[source.UnitId] = source;
        if (contextTarget != null)
            participants[contextTarget.UnitId] = contextTarget;
        foreach (var target in targets)
            participants[target.UnitId] = target;
        if (before != null)
        {
            foreach (var id in before.Keys)
            {
                if (state.Units.TryGetValue(id, out var unit))
                    participants[id] = unit;
            }
        }
        return participants.Values.ToList();
    }

    private static string UnitName(UnitData unit) => unit.CatalogId.HasValue
        && UnitDefinitions.TryGet(new UnitId(unit.CatalogId.Value), out var def)
        && def != null
            ? def.DisplayName
            : unit.CatalogId.HasValue
                ? Titleize(unit.CatalogId.Value)
                : $"Unit {unit.UnitId}";

    private static string UnitName(UnitData unit, IReadOnlyList<UnitData> participants)
    {
        return UnitNameWithId(unit);
    }

    private static string UnitNameWithId(UnitData unit) => $"{UnitName(unit)} #{unit.UnitId}";

    private static string StatusName(StatusEffectKind statusKind) =>
        statusKind == StatusEffectKind.None ? "status" : Titleize(statusKind.ToString());

    private static string TimeLabel(MatchState state) => $"{state.MatchTime:0.0}s";

    private static void AppendDuration(StringBuilder builder, float duration)
    {
        if (duration > 0f)
            builder.Append($" for {duration:0.0}s");
    }

    private static string Amount(float value) =>
        MathF.Abs(value - MathF.Round(value)) < 0.05f ? $"{MathF.Round(value):0}" : $"{value:0.0}";

    private static bool NearlyEqual(float left, float right) => MathF.Abs(left - right) < 0.01f;

    private static string Percent(float value) => $"{MathF.Round(value * 100f):0}%";

    private static string Plural(int count) => count == 1 ? "" : "s";

    private static string AllyWord(int count) => count == 1 ? "ally" : "allies";

    private static string Titleize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        var parts = value
            .Replace('-', '_')
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return value;
        return string.Join(
            " ",
            parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant())
        );
    }

    private readonly record struct EffectSpecView(
        EffectType EffectType,
        float Value,
        float Duration,
        StatusEffectKind StatusKind
    )
    {
        public static EffectSpecView FromAbilityEffect(UnitAbilityEffectState effect) =>
            new(
                effect.EffectType,
                effect.Value,
                effect.DurationSeconds > 0f ? effect.DurationSeconds : effect.StatusDuration,
                effect.StatusKind
            );

        public static EffectSpecView FromApplicationSpec(EffectApplicationSpec spec) =>
            new(spec.EffectType, spec.Value, spec.ResolvedDuration, spec.StatusKind);
    }
}
