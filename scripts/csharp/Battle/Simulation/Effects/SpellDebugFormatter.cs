using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Effects;

public static class SpellDebugFormatter
{
    public static string FormatApplication(
        MatchState state,
        SimCardCatalogId cardCatalogId,
        EffectApplicationSpec spec,
        IReadOnlyList<UnitData> targets,
        IReadOnlyDictionary<int, UnitDebugSnapshot> before,
        int appliedCount,
        bool delayed = false
    )
    {
        var builder = new StringBuilder();
        builder.Append(TimeLabel(state));
        builder.Append(' ');
        builder.Append(CardName(cardCatalogId));
        builder.Append(delayed ? " resolved delayed " : " applied ");
        builder.Append(EffectName(spec));

        if (targets.Count == 0)
        {
            builder.Append($" but found no {TargetFilterName(spec)}.");
            return builder.ToString();
        }

        builder.Append($" to {appliedCount}/{targets.Count} {TargetWord(spec, targets, targets.Count)}.");
        AppendOutcomeLines(builder, state, targets, before, spec);
        return builder.ToString();
    }

    public static string FormatQueuedDelayed(
        MatchState state,
        SimCardCatalogId cardCatalogId,
        EffectApplicationSpec spec,
        float delaySeconds
    )
    {
        return $"{TimeLabel(state)} {CardName(cardCatalogId)} queued {EffectName(spec)} "
            + $"for {delaySeconds:0.0}s later.";
    }

    private static void AppendOutcomeLines(
        StringBuilder builder,
        MatchState state,
        IReadOnlyList<UnitData> targets,
        IReadOnlyDictionary<int, UnitDebugSnapshot> before,
        EffectApplicationSpec spec
    )
    {
        bool wrote = false;
        if (spec.EffectType == EffectType.Shield)
        {
            int shielded = targets.Count(t =>
                before.TryGetValue(t.UnitId, out var snapshot) && CurrentShield(t) > snapshot.ShieldHp
            );
            if (shielded > 0)
            {
                builder.Append('\n');
                builder.Append($"  Shielded {shielded} {TargetWord(spec, targets, shielded)}.");
                wrote = true;
            }
        }

        foreach (var target in targets)
        {
            if (!before.TryGetValue(target.UnitId, out var snapshot))
                continue;

            wrote |= AppendUnitDelta(builder, state, target, snapshot, spec);
        }

        if (!wrote)
        {
            builder.Append('\n');
            builder.Append("  No visible unit state changed.");
        }
    }

    private static bool AppendUnitDelta(
        StringBuilder builder,
        MatchState state,
        UnitData target,
        UnitDebugSnapshot before,
        EffectApplicationSpec spec
    )
    {
        bool wrote = false;
        string targetName = UnitNameWithId(target);
        if (MathF.Abs(target.CurrentHp - before.Hp) > 0.05f || target.IsAlive != before.IsAlive)
        {
            builder.Append('\n');
            if (!target.IsAlive && before.IsAlive)
                builder.Append($"  {targetName}: defeated.");
            else
                builder.Append($"  {targetName}: {Amount(before.Hp)} -> {Amount(target.CurrentHp)} hp.");
            wrote = true;
        }

        float shield = CurrentShield(target);
        if (MathF.Abs(shield - before.ShieldHp) > 0.05f)
        {
            builder.Append('\n');
            builder.Append($"  {targetName}: {Amount(before.ShieldHp)} -> {Amount(shield)} shield.");
            wrote = true;
        }

        if (spec.EffectType == EffectType.StatusApply || spec.EffectType == EffectType.StatusConsume)
        {
            var statusKind = spec.StatusKind == StatusEffectKind.None
                ? StatusEffectKind.Burn
                : spec.StatusKind;
            before.Statuses.TryGetValue(statusKind, out var beforeStatus);
            var afterStatus = CombatDebugFormatter.Capture(target).Statuses.GetValueOrDefault(statusKind);
            int beforeStacks = beforeStatus?.Stacks ?? 0;
            int afterStacks = afterStatus?.Stacks ?? 0;
            if (beforeStacks != afterStacks || spec.EffectType == EffectType.StatusApply)
            {
                builder.Append('\n');
                if (afterStacks <= 0)
                {
                    builder.Append($"  {targetName}: {StatusName(statusKind)} cleared.");
                }
                else
                {
                    builder.Append(
                        $"  {targetName}: {StatusName(statusKind)} {beforeStacks} -> {afterStacks} stack{Plural(afterStacks)}"
                    );
                    if (afterStatus != null)
                    {
                        builder.Append(
                            $", {Amount(afterStatus.DamagePerTick)} damage/tick, {afterStatus.Duration:0.0}s remaining"
                        );
                    }
                    builder.Append('.');
                }
                wrote = true;
            }
        }

        if (spec.EffectType is EffectType.Knockback or EffectType.Displacement)
        {
            float moved = target.KnockbackRemainingDistance;
            if (moved > 0.05f)
            {
                builder.Append('\n');
                builder.Append($"  {targetName}: moved {Amount(moved)} units.");
                wrote = true;
            }
        }

        int beforeBuffs = before.BuffCounts.GetValueOrDefault(spec.EffectType);
        int afterBuffs = target.ActiveBuffs.Count(b => b.EffectType == spec.EffectType);
        if (afterBuffs != beforeBuffs && IsBuffEffect(spec.EffectType))
        {
            builder.Append('\n');
            builder.Append(
                $"  {targetName}: {EffectName(spec)} {beforeBuffs} -> {afterBuffs} active."
            );
            wrote = true;
        }

        return wrote;
    }

    private static bool IsBuffEffect(EffectType effectType)
    {
        return effectType
            is EffectType.Slow
                or EffectType.Stun
                or EffectType.Root
                or EffectType.Haste
                or EffectType.DamageBoost
                or EffectType.StatModifier
                or EffectType.EvasionModifier
                or EffectType.AttackSpeedModifier
                or EffectType.FlatDamageReduction
                or EffectType.AccuracyModifier
                or EffectType.RangedDamageModifier
                or EffectType.ReviveOnDeath
                or EffectType.TornadoCarry;
    }

    private static string TargetFilterName(EffectApplicationSpec spec)
    {
        return spec.TargetAffinity switch
        {
            SpellAffinity.Allies => "valid allies",
            SpellAffinity.Enemies => "valid enemies",
            SpellAffinity.Both => "valid targets",
            _ => "valid targets",
        };
    }

    private static string TargetWord(
        EffectApplicationSpec spec,
        IReadOnlyList<UnitData> targets,
        int count
    )
    {
        string baseWord = ResolveTargetWord(spec, targets);
        return count == 1 ? baseWord : $"{baseWord}s";
    }

    private static string ResolveTargetWord(EffectApplicationSpec spec, IReadOnlyList<UnitData> targets)
    {
        if (targets.Count > 0)
        {
            bool hasAlly = targets.Any(target => target.Team == spec.Context.SourceTeam);
            bool hasEnemy = targets.Any(target => target.Team != spec.Context.SourceTeam);
            if (hasAlly && !hasEnemy)
                return "ally";
            if (hasEnemy && !hasAlly)
                return "enemy";
        }

        return spec.TargetAffinity switch
        {
            SpellAffinity.Allies => "ally",
            SpellAffinity.Enemies => "enemy",
            SpellAffinity.Both => "target",
            _ => "target",
        };
    }

    private static string CardName(SimCardCatalogId cardCatalogId)
    {
        if (cardCatalogId.HasValue)
        {
            var card = CardDefinitions.Get(cardCatalogId.Value);
            if (card != null)
                return card.Name;
            return Titleize(cardCatalogId.Value);
        }

        return "Spell";
    }

    private static string UnitNameWithId(UnitData unit)
    {
        string name = unit.CatalogId.HasValue
            && UnitDefinitions.TryGet(new UnitId(unit.CatalogId.Value), out var def)
            && def != null
                ? def.DisplayName
                : unit.CatalogId.HasValue
                    ? Titleize(unit.CatalogId.Value)
                    : "Unit";
        return $"{name} #{unit.UnitId}";
    }

    private static float CurrentShield(UnitData unit) =>
        unit.ActiveBuffs.Where(b => b.EffectType == EffectType.Shield).Sum(b => MathF.Max(0f, b.ShieldHp));

    private static string EffectName(EffectApplicationSpec spec)
    {
        if (spec.EffectType is EffectType.StatusApply or EffectType.StatusConsume)
        {
            var statusKind = spec.StatusKind == StatusEffectKind.None
                ? StatusEffectKind.Burn
                : spec.StatusKind;
            return spec.EffectType == EffectType.StatusConsume
                ? $"{StatusName(statusKind)} cashout"
                : StatusName(statusKind);
        }

        return spec.EffectType switch
        {
            EffectType.Damage => "damage",
            EffectType.Heal => "healing",
            EffectType.Shield => "shield",
            EffectType.Cleanse => "cleanse",
            EffectType.Knockback => "knockback",
            EffectType.Displacement => "displacement",
            EffectType.DamageBoost => $"damage boost ({Percent(spec.Value)})",
            EffectType.EvasionModifier => $"dodge chance ({Percent(spec.Value)})",
            EffectType.AttackSpeedModifier => $"attack speed ({Percent(spec.Value)})",
            EffectType.RangedDamageModifier => $"ranged damage ({Percent(spec.Value)})",
            EffectType.AccuracyModifier => $"accuracy ({Percent(spec.Value)})",
            EffectType.FlatDamageReduction => "flat damage reduction",
            EffectType.ReviveOnDeath => "revive preparation",
            EffectType.TornadoCarry => "tornado carry",
            _ => Titleize(spec.EffectType.ToString()).ToLowerInvariant(),
        };
    }

    private static string StatusName(StatusEffectKind statusKind) =>
        statusKind == StatusEffectKind.None ? "status" : Titleize(statusKind.ToString());

    private static string Titleize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var builder = new StringBuilder();
        char previous = '\0';
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i] is '_' or '-' ? ' ' : raw[i];
            if (i > 0 && char.IsUpper(c) && previous != ' ' && !char.IsUpper(previous))
                builder.Append(' ');
            builder.Append(i == 0 || previous == ' ' ? char.ToUpperInvariant(c) : c);
            previous = c;
        }

        return builder.ToString();
    }

    private static string TimeLabel(MatchState state) => $"{state.MatchTime:0.0}s";

    private static string Amount(float value) =>
        MathF.Abs(value - MathF.Round(value)) < 0.05f ? $"{MathF.Round(value):0}" : $"{value:0.0}";

    private static string Percent(float value) => $"{value * 100f:+0;-0;0}%";

    private static string Plural(int count) => count == 1 ? "" : "s";
}
