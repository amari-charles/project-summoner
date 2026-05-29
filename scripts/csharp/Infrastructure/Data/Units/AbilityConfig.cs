using Fateforged.Projectiles;
using Fateforged.Simulation.Enums;

namespace Fateforged.Units;

/// <summary>
/// Authoring config for simulation-owned unit abilities.
/// </summary>
public record UnitAbilityConfig
{
    /// <summary>Stable authoring ID for debug/events.</summary>
    public string AbilityId { get; init; } = "";

    /// <summary>When this ability attempts to activate.</summary>
    public UnitAbilityTrigger Trigger { get; init; } = UnitAbilityTrigger.Periodic;

    /// <summary>How this ability resolves targets.</summary>
    public UnitAbilityTargeting Targeting { get; init; } = UnitAbilityTargeting.Self;

    /// <summary>How this ability delivers effects after target resolution.</summary>
    public UnitAbilityDelivery Delivery { get; init; } = UnitAbilityDelivery.Instant;

    /// <summary>Cooldown between ability activations.</summary>
    public float CooldownSeconds { get; init; } = 1f;

    /// <summary>Primary ability range for target queries.</summary>
    public float Range { get; init; }

    /// <summary>Radius used by pulse/area ability logic.</summary>
    public float Radius { get; init; }

    /// <summary>Primary scalar value (heal amount, etc.).</summary>
    public float Value { get; init; }

    /// <summary>Primary duration value (taunt duration, etc.).</summary>
    public float DurationSeconds { get; init; }

    /// <summary>
    /// Effect payload used by generic self-effect abilities.
    /// Ignored by ability kinds that have fixed behavior.
    /// </summary>
    public EffectType EffectType { get; init; } = EffectType.StatModifier;

    /// <summary>Typed lifetime payload for generic effect application.</summary>
    public EffectLifetime Lifetime { get; init; } = EffectLifetime.Timed(0f);

    /// <summary>Optional windup before resolve.</summary>
    public float WindupSeconds { get; init; }

    /// <summary>Optional projectile used by projectile-delivery abilities.</summary>
    public ProjectileId ProjectileId { get; init; } = ProjectileId.None;

    /// <summary>Target affinity filter used by this ability.</summary>
    public AbilityTargetAffinity TargetAffinity { get; init; } = AbilityTargetAffinity.Enemies;

    /// <summary>Effect payloads delivered by the ability.</summary>
    public UnitAbilityEffectConfig[] Effects { get; init; } = [];
}

/// <summary>
/// Effect payload used by simulation-owned unit abilities.
/// </summary>
public record UnitAbilityEffectConfig
{
    /// <summary>Gameplay mutation to apply.</summary>
    public EffectType EffectType { get; init; } = EffectType.StatModifier;

    /// <summary>Primary scalar value for the effect.</summary>
    public float Value { get; init; }

    /// <summary>Primary duration value for buffs/debuffs.</summary>
    public float DurationSeconds { get; init; }

    /// <summary>Typed lifetime payload for buffs/debuffs.</summary>
    public EffectLifetime Lifetime { get; init; } = EffectLifetime.Timed(0f);

    /// <summary>Damage lane used by damage-class effects.</summary>
    public DamageType DamageType { get; init; } = DamageType.Magic;

    /// <summary>Optional projectile/status payload for status application.</summary>
    public ProjectileStatusConfig? Status { get; init; }
}

/// <summary>
/// Optional projectile impact status payload configuration.
/// </summary>
public record ProjectileStatusConfig
{
    /// <summary>Status kind to apply on projectile impact.</summary>
    public StatusEffectKind Kind { get; init; } = StatusEffectKind.None;

    /// <summary>Status duration (seconds).</summary>
    public float DurationSeconds { get; init; }

    /// <summary>Periodic tick cadence (seconds).</summary>
    public float TickIntervalSeconds { get; init; } = 1f;

    /// <summary>Potency added per stack (damage per tick for DoT).</summary>
    public float PotencyPerStack { get; init; }

    /// <summary>Maximum potency stacks for this status.</summary>
    public int MaxStacks { get; init; } = 1;
}

/// <summary>
/// Optional projectile impact behavior override for ranged units/abilities.
/// </summary>
public record ProjectileImpactConfig
{
    /// <summary>Who this projectile can impact.</summary>
    public AbilityTargetAffinity TargetAffinity { get; init; } = AbilityTargetAffinity.Enemies;

    /// <summary>What happens on impact.</summary>
    public ProjectileImpactKind ImpactKind { get; init; } = ProjectileImpactKind.Damage;

    /// <summary>Optional status payload applied on unit hit.</summary>
    public ProjectileStatusConfig? Status { get; init; }

    public static ProjectileImpactConfig DamageDefault => new();
}
