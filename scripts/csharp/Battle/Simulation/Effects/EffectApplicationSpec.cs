using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Effects;

/// <summary>
/// Policy used when a duration buff is applied to a target that already has a
/// matching buff key.
/// </summary>
public enum EffectStackPolicy
{
    Independent = 0,
    RefreshDuration = 1,
    StackAndRefreshDuration = 2,
}

/// <summary>
/// Lifecycle phase for data-driven effect presentation hooks.
/// </summary>
public enum EffectCuePhase
{
    Executed = 0,
    Active = 1,
    Removed = 2,
}

/// <summary>
/// Tag gates evaluated before an effect is applied.
/// </summary>
public sealed class EffectTagRequirements
{
    public List<string> RequiredSourceTags { get; set; } = new();
    public List<string> BlockedSourceTags { get; set; } = new();
    public List<string> RequiredTargetTags { get; set; } = new();
    public List<string> BlockedTargetTags { get; set; } = new();

    public bool IsEmpty =>
        RequiredSourceTags.Count == 0
        && BlockedSourceTags.Count == 0
        && RequiredTargetTags.Count == 0
        && BlockedTargetTags.Count == 0;

    public EffectTagRequirements DeepClone()
    {
        return new EffectTagRequirements
        {
            RequiredSourceTags = new List<string>(RequiredSourceTags),
            BlockedSourceTags = new List<string>(BlockedSourceTags),
            RequiredTargetTags = new List<string>(RequiredTargetTags),
            BlockedTargetTags = new List<string>(BlockedTargetTags),
        };
    }
}

/// <summary>
/// Runtime context that travels with an effect execution.
/// </summary>
public sealed class EffectApplicationContext
{
    public int SourceUnitId { get; init; }
    public Team SourceTeam { get; init; }
    public SimVector3? SourcePosition { get; init; }
    public string AbilityId { get; init; } = "";
    public string CardCatalogId { get; init; } = "";
    public bool TriggerSourceOnHit { get; init; }
    public bool TriggerTargetOnDamaged { get; init; } = true;
    public bool UseAttackDamageProfile { get; init; }
}

/// <summary>
/// Runtime effect payload. This is the simulation's lightweight equivalent of a
/// GameplayEffectSpec: immutable authoring data plus per-execution context.
/// </summary>
public sealed class EffectApplicationSpec
{
    public EffectType EffectType { get; init; } = EffectType.StatModifier;
    public float Value { get; init; }
    public float Duration { get; init; }
    public EffectLifetime Lifetime { get; init; } = EffectLifetime.Timed(0f);
    public DamageType DamageType { get; init; } = DamageType.Magic;
    public StatusEffectKind StatusKind { get; init; } = StatusEffectKind.None;
    public float StatusTickInterval { get; init; } = 1f;
    public float StatusPotencyPerStack { get; init; }
    public int StatusMaxStacks { get; init; } = 1;
    public BuffRemovalEffectConfig? RemovalEffect { get; init; }
    public int RequiredTargetElementId { get; init; } = -1;
    public EffectTagRequirements TagRequirements { get; init; } = new();
    public List<string> GrantedTags { get; init; } = new();
    public EffectStackPolicy StackPolicy { get; init; } = EffectStackPolicy.Independent;
    public string StackKey { get; init; } = "";
    public string CueId { get; init; } = "";
    public EffectApplicationContext Context { get; init; } = new();

    public float ResolvedDuration =>
        EffectLifetimeResolver.ResolveDuration(Lifetime, Duration);

    public string ResolvedStackKey =>
        string.IsNullOrWhiteSpace(StackKey)
            ? $"{EffectType}:{StatusKind}:{CueId}"
            : StackKey;
}

/// <summary>
/// Shared helpers for tag-based effect eligibility.
/// </summary>
public static class CombatTagSet
{
    public static HashSet<string> GetOwnedTags(UnitData? unit)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (unit == null)
            return result;

        foreach (var tag in unit.CombatTags)
            AddIfValid(result, tag);

        foreach (var buff in unit.ActiveBuffs)
        {
            foreach (var tag in buff.GrantedTags)
                AddIfValid(result, tag);
        }

        AddIfValid(result, $"unit_type.{unit.UnitType.ToString().ToLowerInvariant()}");
        AddIfValid(result, $"movement.{unit.MovementLayer.ToString().ToLowerInvariant()}");
        AddIfValid(result, $"role.{unit.TacticalRole.ToString().ToLowerInvariant()}");
        AddIfValid(result, $"element.{unit.ElementId}");
        return result;
    }

    public static bool HasAll(HashSet<string> owned, IEnumerable<string> required)
    {
        return required.All(owned.Contains);
    }

    public static bool HasAny(HashSet<string> owned, IEnumerable<string> blocked)
    {
        return blocked.Any(owned.Contains);
    }

    private static void AddIfValid(HashSet<string> tags, string? tag)
    {
        if (!string.IsNullOrWhiteSpace(tag))
            tags.Add(tag);
    }
}
