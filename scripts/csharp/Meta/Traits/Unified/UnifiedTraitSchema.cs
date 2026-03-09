using System.Collections.Generic;
using Fateforged.Stats;

namespace Fateforged.Meta.Traits.Unified;

// =============================================================================
// VALUE OBJECTS
// =============================================================================

public readonly record struct UnifiedTraitId(string Value)
{
    public static UnifiedTraitId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(UnifiedTraitId id) => id.Value;
    public static implicit operator UnifiedTraitId(string value) => new(value ?? "");
}

public readonly record struct UnifiedTraitEffectId(string Value)
{
    public static UnifiedTraitEffectId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(UnifiedTraitEffectId id) => id.Value;
    public static implicit operator UnifiedTraitEffectId(string value) => new(value ?? "");
}

public readonly record struct UnifiedTraitPoolId(string Value)
{
    public static UnifiedTraitPoolId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(UnifiedTraitPoolId id) => id.Value;
    public static implicit operator UnifiedTraitPoolId(string value) => new(value ?? "");
}

public readonly record struct UnifiedTraitTag(string Value)
{
    public static UnifiedTraitTag Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(UnifiedTraitTag tag) => tag.Value;
    public static implicit operator UnifiedTraitTag(string value) => new(value ?? "");
}

public readonly record struct UnifiedFieldPath(string Value)
{
    public static UnifiedFieldPath Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(UnifiedFieldPath value) => value.Value;
    public static implicit operator UnifiedFieldPath(string value) => new(value ?? "");
}

public readonly record struct UnifiedPredicateLiteral(string Value)
{
    public static UnifiedPredicateLiteral Empty => new("");
    public override string ToString() => Value;
    public static implicit operator string(UnifiedPredicateLiteral value) => value.Value;
    public static implicit operator UnifiedPredicateLiteral(string value) => new(value ?? "");
}

public readonly record struct UnifiedFlagKey(string Value)
{
    public static UnifiedFlagKey Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(UnifiedFlagKey value) => value.Value;
    public static implicit operator UnifiedFlagKey(string value) => new(value ?? "");
}

public readonly record struct UnifiedScalar(float Value)
{
    public static UnifiedScalar Zero => new(0f);
    public static implicit operator float(UnifiedScalar value) => value.Value;
    public static implicit operator UnifiedScalar(float value) => new(value);
}

public readonly record struct UnifiedSeconds(float Value)
{
    public static UnifiedSeconds Zero => new(0f);
    public static implicit operator float(UnifiedSeconds value) => value.Value;
    public static implicit operator UnifiedSeconds(float value) => new(value);
}

public readonly record struct UnifiedWeight(int Value)
{
    public static UnifiedWeight One => new(1);
    public static implicit operator int(UnifiedWeight value) => value.Value;
    public static implicit operator UnifiedWeight(int value) => new(value);
}

public readonly record struct UnifiedSlotCount(int Value)
{
    public static UnifiedSlotCount Zero => new(0);
    public static implicit operator int(UnifiedSlotCount value) => value.Value;
    public static implicit operator UnifiedSlotCount(int value) => new(value);
}

public readonly record struct UnifiedLevel(int Value)
{
    public static UnifiedLevel One => new(1);
    public static implicit operator int(UnifiedLevel value) => value.Value;
    public static implicit operator UnifiedLevel(int value) => new(value);
}

public readonly record struct UnifiedPointAmount(int Value)
{
    public static UnifiedPointAmount Zero => new(0);
    public static implicit operator int(UnifiedPointAmount value) => value.Value;
    public static implicit operator UnifiedPointAmount(int value) => new(value);
}

public readonly record struct UnifiedProgressionSource(string Value)
{
    public static UnifiedProgressionSource Empty => new("");
    public override string ToString() => Value;
    public static implicit operator string(UnifiedProgressionSource value) => value.Value;
    public static implicit operator UnifiedProgressionSource(string value) => new(value ?? "");
}

public sealed class UnifiedDisplayText
{
    public string LocalizationKey { get; set; } = "";
    public string Fallback { get; set; } = "";

    public string ResolveDisplayText()
    {
        return string.IsNullOrEmpty(Fallback) ? LocalizationKey : Fallback;
    }
}

public sealed class UnifiedActivationWindow
{
    public UnifiedSeconds StartInclusive { get; set; } = UnifiedSeconds.Zero;
    public UnifiedSeconds EndExclusive { get; set; } = UnifiedSeconds.Zero;
}

public sealed class UnifiedTriggerSettings
{
    public TriggerCondition Condition { get; set; } = TriggerCondition.Always;
    public UnifiedScalar Threshold { get; set; } = UnifiedScalar.Zero;
    public UnifiedSeconds Duration { get; set; } = UnifiedSeconds.Zero;
    public UnifiedSeconds Cooldown { get; set; } = UnifiedSeconds.Zero;
    public UnifiedTraitTriggerScope Scope { get; set; } = UnifiedTraitTriggerScope.PerUnit;
}

public sealed class UnifiedLevelRange
{
    public UnifiedLevel MinInclusive { get; set; } = UnifiedLevel.One;
    public UnifiedLevel MaxInclusive { get; set; } = UnifiedLevel.One;
}

public sealed class UnifiedOfferLayout
{
    public UnifiedSlotCount OfferSlots { get; set; } = new(3);
    public UnifiedSlotCount GuaranteedSlots { get; set; } = UnifiedSlotCount.Zero;
}

public sealed class UnifiedTraitOfferRequest
{
    public UnifiedSlotCount Count { get; set; } = new(3);
    public UnifiedTraitPoolId PreferredPoolId { get; set; } = UnifiedTraitPoolId.Empty;
}

// =============================================================================
// ENUMS
// =============================================================================

public enum UnifiedTraitOwnerType
{
    Summoner,
    Card,
    Item,
    Global
}

public enum UnifiedTraitTargetType
{
    Summoner,
    SpawnedUnit,
    SpellCast,
    CardRuntime
}

public enum UnifiedTraitActivationType
{
    Always,
    TimeWindow,
    Triggered
}

public enum UnifiedTraitOperationType
{
    Add,
    Multiply,
    SetFlag
}

public enum UnifiedTraitTriggerScope
{
    PerUnit,
    PerTeam,
    PerCardCast
}

public enum UnifiedTraitPredicateKind
{
    True,
    All,
    Any,
    Not,
    Eq,
    Neq,
    In,
    NotIn,
    Gte,
    Lte,
    Between,
    HasTag
}

// =============================================================================
// SCHEMA MODELS
// =============================================================================

public sealed class UnifiedTraitDefinition
{
    public UnifiedTraitId TraitId { get; set; } = UnifiedTraitId.Empty;
    public UnifiedTraitOwnerType OwnerType { get; set; } = UnifiedTraitOwnerType.Summoner;
    public List<UnifiedTraitTag> Tags { get; set; } = new();
    public List<UnifiedTraitId> Prerequisites { get; set; } = new();
    public List<UnifiedTraitTag> PoolTags { get; set; } = new();
    public List<UnifiedTraitEffectDefinition> Effects { get; set; } = new();
}

public sealed class UnifiedTraitEffectDefinition
{
    public UnifiedTraitEffectId EffectId { get; set; } = UnifiedTraitEffectId.Empty;
    public UnifiedTraitTargetType TargetType { get; set; } = UnifiedTraitTargetType.SpawnedUnit;
    public UnifiedTraitActivationType ActivationType { get; set; } = UnifiedTraitActivationType.Always;
    public List<UnifiedTraitOperation> Operations { get; set; } = new();

    public UnifiedActivationWindow ActivationWindow { get; set; } = new();
    public UnifiedTriggerSettings TriggerSettings { get; set; } = new();
    public UnifiedTraitPredicate Predicate { get; set; } = UnifiedTraitPredicate.True();
}

public sealed class UnifiedTraitOperation
{
    public UnifiedTraitOperationType Type { get; set; } = UnifiedTraitOperationType.Multiply;
    public StatKey? Stat { get; set; }
    public UnifiedScalar Magnitude { get; set; } = UnifiedScalar.Zero;
    public UnifiedFlagKey FlagKey { get; set; } = UnifiedFlagKey.Empty;
    public bool FlagValue { get; set; } = true;
}

public sealed class UnifiedTraitPredicate
{
    public UnifiedTraitPredicateKind Kind { get; set; } = UnifiedTraitPredicateKind.True;
    public UnifiedFieldPath FieldPath { get; set; } = UnifiedFieldPath.Empty;
    public UnifiedPredicateLiteral Value { get; set; } = UnifiedPredicateLiteral.Empty;
    public UnifiedPredicateLiteral ValueUpper { get; set; } = UnifiedPredicateLiteral.Empty;
    public List<UnifiedPredicateLiteral> Values { get; set; } = new();
    public List<UnifiedTraitPredicate> Children { get; set; } = new();

    public static UnifiedTraitPredicate True() => new();
}

public sealed class UnifiedTraitPoolDefinition
{
    public UnifiedTraitPoolId PoolId { get; set; } = UnifiedTraitPoolId.Empty;
    public UnifiedTraitOwnerType AppliesTo { get; set; } = UnifiedTraitOwnerType.Summoner;
    public UnifiedOfferLayout Layout { get; set; } = new();
    public List<UnifiedTraitPoolEntry> Entries { get; set; } = new();
}

public sealed class UnifiedTraitPoolEntry
{
    public UnifiedTraitId TraitId { get; set; } = UnifiedTraitId.Empty;
    public UnifiedWeight Weight { get; set; } = UnifiedWeight.One;
    public UnifiedLevelRange LevelRange { get; set; } = new();
}

public sealed class UnifiedTraitOffer
{
    public UnifiedTraitId TraitId { get; set; } = UnifiedTraitId.Empty;
    public UnifiedDisplayText DisplayName { get; set; } = new();
    public UnifiedDisplayText Description { get; set; } = new();
    public UnifiedWeight Weight { get; set; } = UnifiedWeight.One;
}

public sealed class UnifiedTraitPointLedger
{
    public UnifiedPointAmount UnspentPoints { get; set; } = UnifiedPointAmount.Zero;
    public List<UnifiedTraitId> SpentTraitIds { get; set; } = new();
}
