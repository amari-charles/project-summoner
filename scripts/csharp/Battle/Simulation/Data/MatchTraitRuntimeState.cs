using System.Collections.Generic;
using Fateforged.Stats;

namespace Fateforged.Simulation.Data;

public enum TraitRuntimeDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public readonly record struct TraitRuntimeRulesetVersion(string Value)
{
    public static TraitRuntimeRulesetVersion Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(TraitRuntimeRulesetVersion value) => value.Value;
    public static implicit operator TraitRuntimeRulesetVersion(string value) => new(value ?? "");
}

public readonly record struct TraitRuntimeTeamId(int Value)
{
    public static implicit operator int(TraitRuntimeTeamId value) => value.Value;
    public static implicit operator TraitRuntimeTeamId(int value) => new(value);
}

public readonly record struct TraitRuntimePointCount(int Value)
{
    public static implicit operator int(TraitRuntimePointCount value) => value.Value;
    public static implicit operator TraitRuntimePointCount(int value) => new(value);
}

public readonly record struct TraitRuntimeCardCatalogId(string Value)
{
    public static TraitRuntimeCardCatalogId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(TraitRuntimeCardCatalogId value) => value.Value;
    public static implicit operator TraitRuntimeCardCatalogId(string value) => new(value ?? "");
}

public readonly record struct TraitRuntimeCardInstanceId(string Value)
{
    public static TraitRuntimeCardInstanceId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(TraitRuntimeCardInstanceId value) => value.Value;
    public static implicit operator TraitRuntimeCardInstanceId(string value) => new(value ?? "");
}

public sealed class TraitRuntimeDiagnostic
{
    public TraitRuntimeDiagnosticSeverity Severity { get; set; } = TraitRuntimeDiagnosticSeverity.Info;
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class TraitRuntimeSpawnContext
{
    public TraitRuntimeTeamId TeamId { get; set; } = new(0);
    public TraitRuntimeCardCatalogId CardCatalogId { get; set; } = TraitRuntimeCardCatalogId.Empty;
    public TraitRuntimeCardInstanceId CardInstanceId { get; set; } = TraitRuntimeCardInstanceId.Empty;
}

/// <summary>
/// Pass 2 unified trait runtime container.
/// Holds deterministic compiled trait state used by simulation.
/// </summary>
public sealed class MatchTraitRuntimeState
{
    public const string StubRulesetVersion = "pass2_stub_v1";
    public const string RulesetVersionV1 = "unified_trait_v1";

    /// <summary>
    /// Ruleset version used to compile this runtime snapshot.
    /// Host/client must match for deterministic outcomes.
    /// </summary>
    public TraitRuntimeRulesetVersion RulesetVersion { get; set; } = new(StubRulesetVersion);

    /// <summary>
    /// Optional diagnostics emitted by compiler/runtime wiring.
    /// </summary>
    public List<TraitRuntimeDiagnostic> Diagnostics { get; } = new();

    /// <summary>
    /// Team-indexed trait point snapshots for debug visibility.
    /// </summary>
    public Dictionary<TraitRuntimeTeamId, TraitRuntimePointCount> TeamTraitPointSnapshot { get; } = new();

    /// <summary>
    /// Runtime card-instance trait multipliers used at unit spawn time.
    /// </summary>
    public Dictionary<TraitRuntimeCardInstanceId, Dictionary<StatKey, float>> CardInstanceStatMultipliers { get; } = new();

    public static MatchTraitRuntimeState Empty() => new();

    public void ResetCardInstanceStatMultipliers()
    {
        CardInstanceStatMultipliers.Clear();
    }

    public void SetCardInstanceStatMultipliers(TraitRuntimeCardInstanceId cardInstanceId, Dictionary<StatKey, float> statMultipliers)
    {
        if (!cardInstanceId.HasValue || statMultipliers == null || statMultipliers.Count == 0)
            return;

        CardInstanceStatMultipliers[cardInstanceId] = new Dictionary<StatKey, float>(statMultipliers);
        RulesetVersion = new TraitRuntimeRulesetVersion(RulesetVersionV1);
    }

    public void ApplySpawnModifiers(UnitData unit, TraitRuntimeSpawnContext context)
    {
        if (!context.CardInstanceId.HasValue)
            return;
        if (!CardInstanceStatMultipliers.TryGetValue(context.CardInstanceId, out var statMultipliers))
            return;

        foreach (var (statKey, multiplier) in statMultipliers)
        {
            if (multiplier <= 0f)
                continue;

            switch (statKey)
            {
                case StatKey.MaxHp:
                case StatKey.MaxHealth:
                    unit.MaxHp *= multiplier;
                    unit.CurrentHp *= multiplier;
                    break;
                case StatKey.AttackDamage:
                    unit.AttackDamage *= multiplier;
                    break;
                case StatKey.AttackSpeed:
                    unit.AttackSpeed *= multiplier;
                    break;
                case StatKey.MoveSpeed:
                    unit.MoveSpeed *= multiplier;
                    break;
                case StatKey.AttackRange:
                    unit.AttackRange *= multiplier;
                    break;
                case StatKey.AggroRadius:
                    unit.AggroRadius *= multiplier;
                    break;
                case StatKey.CritChance:
                    unit.CritChance *= multiplier;
                    break;
                case StatKey.CritDamage:
                    unit.CritDamage *= multiplier;
                    break;
                case StatKey.Armor:
                    unit.PhysicalDefense *= multiplier;
                    break;
                case StatKey.MagicResist:
                    unit.MagicDefense *= multiplier;
                    break;
            }
        }
    }
}
