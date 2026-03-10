using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fateforged.Data.Traits;
using Fateforged.Stats;

namespace Fateforged.Meta.Cards;

internal static class TraitSummaryFormatter
{
    private const string FallbackSummary = "Special Effect";

    private static readonly Dictionary<StatKey, string> StatLabels = new()
    {
        [StatKey.MaxHp] = "Max HP",
        [StatKey.MaxHealth] = "Max HP",
        [StatKey.AttackDamage] = "Attack Damage",
        [StatKey.AttackSpeed] = "Attack Speed",
        [StatKey.MoveSpeed] = "Move Speed",
        [StatKey.AttackRange] = "Attack Range",
        [StatKey.AggroRadius] = "Aggro Radius",
        [StatKey.CritChance] = "Crit Chance",
        [StatKey.CritDamage] = "Crit Damage",
        [StatKey.Armor] = "Armor",
        [StatKey.MagicResist] = "Magic Resist",
        [StatKey.SoulStrength] = "Soul Strength",
        [StatKey.ManaRegen] = "Mana Regen",
        [StatKey.CastSpeed] = "Cast Speed",
        [StatKey.SoulGuard] = "Soul Guard",
        [StatKey.DamageReduction] = "Damage Reduction",
        [StatKey.Lifesteal] = "Lifesteal",
        [StatKey.HealingBonus] = "Healing",
        [StatKey.HealOnKill] = "Heal on Kill",
        [StatKey.DamageBonus] = "Damage",
        [StatKey.FireDamageBonus] = "Fire Damage",
        [StatKey.WaterDamageBonus] = "Water Damage",
        [StatKey.WindDamageBonus] = "Wind Damage",
        [StatKey.EarthDamageBonus] = "Earth Damage",
        [StatKey.LightningDamageBonus] = "Lightning Damage",
        [StatKey.LifeDamageBonus] = "Life Damage",
        [StatKey.DeathDamageBonus] = "Death Damage",
        [StatKey.ShadowDamageBonus] = "Shadow Damage",
    };

    public static string BuildSummaryShort(TraitDefinition? trait)
    {
        if (trait == null || trait.Modifiers.Count == 0)
            return FallbackSummary;

        var fragments = new List<string>();
        foreach (var modifier in trait.Modifiers)
        {
            fragments.AddRange(BuildModifierFragments(modifier));
        }

        var distinctFragments = fragments
            .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (distinctFragments.Count == 0)
            return FallbackSummary;

        if (distinctFragments.Count <= 2)
            return string.Join(", ", distinctFragments);

        return $"{distinctFragments[0]}, {distinctFragments[1]}, +{distinctFragments.Count - 2} more";
    }

    private static IEnumerable<string> BuildModifierFragments(TraitModifier modifier)
    {
        var suffix = BuildSuffix(modifier);
        var fragments = new List<string>();

        if (modifier.HasSummonerStat && modifier.Stat.HasValue)
        {
            var summonerFragment = modifier.Type switch
            {
                ModifierType.Percent => FormatPercent(modifier.Value, GetStatLabel(modifier.Stat.Value)),
                ModifierType.Flat => FormatFlat(modifier.Value, GetStatLabel(modifier.Stat.Value)),
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(summonerFragment))
                fragments.Add(ApplySuffix(summonerFragment, suffix));
        }

        if (modifier.StatMults != null && modifier.StatMults.Count > 0)
        {
            foreach (var (statKey, multiplier) in modifier.StatMults.OrderBy(kvp => kvp.Key.ToString(), StringComparer.Ordinal))
            {
                var deltaPercent = (multiplier - 1f) * 100f;
                var fragment = FormatPercent(deltaPercent, GetStatLabel(statKey));
                if (!string.IsNullOrWhiteSpace(fragment))
                    fragments.Add(ApplySuffix(fragment, suffix));
            }
        }

        if (modifier.StatAdds != null && modifier.StatAdds.Count > 0)
        {
            foreach (var (statKey, value) in modifier.StatAdds.OrderBy(kvp => kvp.Key.ToString(), StringComparer.Ordinal))
            {
                var fragment = FormatFlat(value, GetStatLabel(statKey));
                if (!string.IsNullOrWhiteSpace(fragment))
                    fragments.Add(ApplySuffix(fragment, suffix));
            }
        }

        return fragments;
    }

    private static string BuildSuffix(TraitModifier modifier)
    {
        var suffixParts = new List<string>();

        var triggerSuffix = BuildTriggerSuffix(modifier);
        if (!string.IsNullOrWhiteSpace(triggerSuffix))
            suffixParts.Add(triggerSuffix);

        var conditionSuffix = BuildConditionSuffix(modifier);
        if (!string.IsNullOrWhiteSpace(conditionSuffix))
            suffixParts.Add(conditionSuffix);

        return string.Join(" ", suffixParts);
    }

    private static string BuildTriggerSuffix(TraitModifier modifier)
    {
        if (string.IsNullOrWhiteSpace(modifier.Trigger))
            return string.Empty;

        var trigger = modifier.Trigger.Trim().ToLowerInvariant();
        return trigger switch
        {
            "belowhppercent" => $"below {FormatThresholdPercent(modifier.TriggerThreshold)} HP",
            "abovehppercent" => $"above {FormatThresholdPercent(modifier.TriggerThreshold)} HP",
            "ontakehit" => "on hit taken",
            "onhit" => "on hit",
            "onkill" => "on kill",
            "ondeath" => "on death",
            "periodic" => modifier.TriggerCooldown > 0f ? $"every {FormatNumeric(modifier.TriggerCooldown)}s" : "periodically",
            _ => string.Empty
        };
    }

    private static string BuildConditionSuffix(TraitModifier modifier)
    {
        if (modifier.Conditions == null || modifier.Conditions.Count == 0)
            return string.Empty;

        if (modifier.Conditions.TryGetValue("elemental_affinity", out var affinityValue) && affinityValue is string affinity && !string.IsNullOrWhiteSpace(affinity))
        {
            return $"for {ToTitleCase(affinity)} units";
        }

        return "when conditioned";
    }

    private static string ApplySuffix(string fragment, string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
            return fragment;

        return $"{fragment} {suffix}";
    }

    private static string FormatPercent(float value, string label)
    {
        if (Math.Abs(value) < 0.001f)
            return string.Empty;

        var sign = value >= 0f ? "+" : "";
        return $"{sign}{FormatNumeric(value)}% {label}";
    }

    private static string FormatFlat(float value, string label)
    {
        if (Math.Abs(value) < 0.001f)
            return string.Empty;

        var sign = value >= 0f ? "+" : "";
        return $"{sign}{FormatNumeric(value)} {label}";
    }

    private static string FormatNumeric(float value)
    {
        var roundedInt = (int)MathF.Round(value);
        if (Math.Abs(value - roundedInt) < 0.001f)
            return roundedInt.ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static string FormatThresholdPercent(float threshold)
    {
        var clamped = Math.Clamp(threshold, 0f, 1f);
        return FormatNumeric(clamped * 100f) + "%";
    }

    private static string GetStatLabel(StatKey statKey)
    {
        if (StatLabels.TryGetValue(statKey, out var label))
            return label;

        return ToTitleCase(statKey.ToSnakeCase().Replace("_", " ", StringComparison.Ordinal));
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return string.Join(" ",
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word =>
                {
                    var lower = word.ToLowerInvariant();
                    return char.ToUpperInvariant(lower[0]) + lower[1..];
                }));
    }
}
