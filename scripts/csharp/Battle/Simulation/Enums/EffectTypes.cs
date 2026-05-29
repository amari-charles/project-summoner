using System;
using System.Collections.Generic;
using Fateforged.Units;

namespace Fateforged.Simulation.Enums;

// =========================================================================
// ENUMS
// =========================================================================

/// <summary>
/// Damage type determines which defense stat is used for reduction.
/// </summary>
public enum DamageType
{
    Physical,
    Magic,
    True, // Ignores all defenses
}

/// <summary>
/// When a trigger fires during combat.
/// </summary>
public enum TriggerType
{
    OnHit, // After this unit deals damage
    OnDamaged, // After this unit takes damage
    OnKill, // After this unit kills another
    OnDeath, // When this unit dies
    HpThreshold, // When HP drops below a percentage
    Periodic, // Every N seconds
    LeaderDeath, // When this unit's group leader dies
}

/// <summary>
/// What a trigger or spell does when it fires.
/// </summary>
public enum EffectType
{
    Damage, // Deal damage to target(s)
    Heal, // Restore HP to target(s)
    StatModifier, // Modify a stat (buff/debuff)
    Slow, // Reduce movement speed
    Stun, // Prevent actions
    Shield, // Absorb incoming damage
    Haste, // Increase movement speed
    DamageBoost, // Increase attack damage
    AreaDamage, // Deal damage in an area
    Cleanse, // Remove negative effects/statuses
    Knockback, // Displace a unit away from source
    EvasionModifier, // Modify evasion chance (+/-)
    AttackSpeedModifier, // Modify attack speed (+/-)
    FlatDamageReduction, // Flat post-mitigation reduction
    Taunt, // Soft forced-target override toward source unit
    StatusApply, // Apply a configured status payload
    StatusConsume, // Consume a configured status and convert remaining value
    TransferHealth, // Move HP from healthy allies to wounded allies
    AccuracyModifier, // Modify attacker's hit chance
    RangedDamageModifier, // Modify damage from ranged unit attacks
    Root, // Prevent movement without preventing attacks
    ReviveOnDeath, // Restore a dying unit once while buff is active
    Displacement, // Push or pull from an explicit origin
    SourceLungeToTarget, // Move the source unit next to the target
}

// =========================================================================
// SPELL ENUMS
// =========================================================================

/// <summary>
/// How a spell selects its targets.
/// </summary>
public enum SpellTargetingMode
{
    Position, // AoE at a position (Fireball)
    NearestEnemy, // Auto-target nearest enemy to position (ManaBolt)
    AlliesInRadius, // Select allied units in radius (Command spells)
}

/// <summary>
/// Geometric shape for spell area resolution.
/// </summary>
public enum SpellAreaShape
{
    Circle = 0,
    Square = 1,
    Line = 2,
    Cone = 3,
}

/// <summary>
/// Which team a spell effect targets.
/// </summary>
public enum SpellAffinity
{
    Enemies,
    Allies,
    Both,
}

/// <summary>
/// Typed lifetime kind for combat effects.
/// </summary>
public enum EffectLifetimeKind
{
    Timed = 0,
    Persistent = 1,
}

/// <summary>
/// Explicit lifetime payload for buffs/effects.
/// Legacy duration bridges are kept for PASS 2 compatibility.
/// </summary>
public readonly record struct EffectLifetime(EffectLifetimeKind Kind, float RemainingSeconds)
{
    public bool IsTimed => Kind == EffectLifetimeKind.Timed;

    public bool IsPersistent => Kind == EffectLifetimeKind.Persistent;

    public static EffectLifetime Timed(float seconds) =>
        new(EffectLifetimeKind.Timed, MathF.Max(0f, seconds));

    public static EffectLifetime Persistent() => new(EffectLifetimeKind.Persistent, 0f);

    public static EffectLifetime FromLegacyDuration(float duration) =>
        duration < 0f ? Persistent() : Timed(duration);

    public float ToLegacyDuration() => IsPersistent ? -1f : RemainingSeconds;
}

// =========================================================================
// DATA STRUCTS
// =========================================================================

/// <summary>
/// A single effect that a spell card applies when cast.
/// Stored on SimCardData.SpellEffects (populated at match start from card catalog).
/// </summary>
public class SimSpellEffect
{
    /// <summary>What this effect does (Damage, Heal, Slow, etc.).</summary>
    public EffectType EffectType { get; set; }

    /// <summary>Effect magnitude (damage amount, heal amount, slow %, etc.).</summary>
    public float Value { get; set; }

    /// <summary>Duration in seconds (for buffs/debuffs). 0 = instant.</summary>
    public float Duration { get; set; }

    /// <summary>
    /// Typed lifetime payload for effects that create buffs/debuffs.
    /// Legacy Duration is retained for compatibility during migration.
    /// </summary>
    public EffectLifetime Lifetime { get; set; } = EffectLifetime.Timed(0f);

    /// <summary>Damage type for damage effects.</summary>
    public DamageType DamageType { get; set; }

    /// <summary>AoE radius override (0 = use card's SpellRadius).</summary>
    public float AoeRadius { get; set; }

    /// <summary>Area shape used to resolve AoE recipients.</summary>
    public SpellAreaShape AreaShape { get; set; } = SpellAreaShape.Circle;

    /// <summary>Which team this effect targets.</summary>
    public SpellAffinity Affinity { get; set; } = SpellAffinity.Enemies;

    /// <summary>Delay before first application (0 = immediate).</summary>
    public float DelaySeconds { get; set; }

    /// <summary>
    /// Additional applications after the first one (0 = single apply).
    /// Example: Delay=0.6, RepeatCount=4, RepeatInterval=0.6 => 5 total pulses.
    /// </summary>
    public int RepeatCount { get; set; }

    /// <summary>Interval between repeated applications.</summary>
    public float RepeatIntervalSeconds { get; set; }

    /// <summary>Status payload identity for status apply/consume effects.</summary>
    public StatusEffectKind StatusKind { get; set; } = StatusEffectKind.None;

    /// <summary>Status payload tick interval.</summary>
    public float StatusTickInterval { get; set; } = 1f;

    /// <summary>Status payload potency per stack.</summary>
    public float StatusPotencyPerStack { get; set; }

    /// <summary>Status payload max stacks.</summary>
    public int StatusMaxStacks { get; set; } = 1;

    /// <summary>Optional payload fired when a buff created by this effect is removed.</summary>
    public BuffRemovalEffectConfig? RemovalEffect { get; set; }

    /// <summary>Optional target element requirement (-1 = no requirement).</summary>
    public int RequiredTargetElementId { get; set; } = -1;

    /// <summary>Tags required/blocked before this effect can affect a target.</summary>
    public Fateforged.Simulation.Effects.EffectTagRequirements TagRequirements { get; set; } = new();

    /// <summary>Tags granted while a buff created by this effect is active.</summary>
    public List<string> GrantedTags { get; set; } = new();

    /// <summary>Policy used if a matching active buff already exists.</summary>
    public Fateforged.Simulation.Effects.EffectStackPolicy StackPolicy { get; set; } =
        Fateforged.Simulation.Effects.EffectStackPolicy.Independent;

    /// <summary>Optional stack key used by non-independent stack policies.</summary>
    public string StackKey { get; set; } = "";

    /// <summary>Optional cue identity emitted for this effect's lifecycle.</summary>
    public string CueId { get; set; } = "";
}

/// <summary>
/// An active buff or debuff on a unit.
/// Buffs are stored in a list on UnitData, oldest first.
/// Shield buffs are consumed oldest-first during damage calculation.
/// </summary>
public class ActiveBuff
{
    /// <summary>Unique ID for this buff instance (for removal/tracking).</summary>
    public int BuffId { get; set; }

    /// <summary>What this buff does.</summary>
    public EffectType EffectType { get; set; }

    /// <summary>Magnitude of the effect (damage amount, speed multiplier, shield HP, etc.).</summary>
    public float Value { get; set; }

    /// <summary>Remaining duration in seconds. -1 = permanent (removed on death).</summary>
    public float Duration { get; set; }

    /// <summary>
    /// Typed lifetime model replacing sentinel duration semantics.
    /// Duration remains as a bridge in PASS 2.
    /// </summary>
    public EffectLifetime Lifetime { get; set; } = EffectLifetime.Timed(0f);

    /// <summary>For periodic buffs: interval between ticks.</summary>
    public float TickInterval { get; set; }

    /// <summary>For periodic buffs: time until next tick.</summary>
    public float TickTimer { get; set; }

    /// <summary>Source unit ID (for kill credit on DoT damage).</summary>
    public int SourceUnitId { get; set; }

    /// <summary>Source team (for team-based effects).</summary>
    public Team SourceTeam { get; set; }

    /// <summary>For shield buffs: remaining absorb HP.</summary>
    public float ShieldHp { get; set; }

    /// <summary>Damage type for damage/shield effects.</summary>
    public DamageType DamageType { get; set; }

    /// <summary>Optional status identity used by stacking payload effects.</summary>
    public StatusEffectKind StatusKind { get; set; } = StatusEffectKind.None;

    /// <summary>Optional stack count for status payload effects.</summary>
    public int StackCount { get; set; } = 1;

    /// <summary>Optional effect fired when this buff is removed for configured reasons.</summary>
    public BuffRemovalEffectConfig? RemovalEffect { get; set; }

    /// <summary>Owner HP captured when this buff was applied, for scaling removal effects.</summary>
    public float OwnerHpAtApply { get; set; }

    /// <summary>Tags granted to the owning unit while this buff is active.</summary>
    public List<string> GrantedTags { get; set; } = new();

    /// <summary>Stacking identity for refresh/stack policies.</summary>
    public string StackKey { get; set; } = "";

    /// <summary>Visual/audio cue identity for active/removed lifecycle events.</summary>
    public string CueId { get; set; } = "";
}

/// <summary>
/// Optional payload fired when a buff expires, breaks, or its owner dies.
/// Used for generic mark/shield-burst style mechanics without content-specific switches.
/// </summary>
public class BuffRemovalEffectConfig
{
    public bool TriggerOnExpire { get; set; }
    public bool TriggerOnShieldBreak { get; set; }
    public bool TriggerOnOwnerDeath { get; set; }
    public EffectType EffectType { get; set; } = EffectType.Damage;
    public float Value { get; set; }
    public bool ScaleValueByOwnerHpAtApply { get; set; }
    public float OwnerHpAtApplyMultiplier { get; set; }
    public DamageType DamageType { get; set; } = DamageType.Magic;
    public float Radius { get; set; }
    public SpellAffinity Affinity { get; set; } = SpellAffinity.Enemies;
    public float Duration { get; set; }
    public EffectLifetime Lifetime { get; set; } = EffectLifetime.Timed(0f);
}

/// <summary>
/// Configuration for a trigger that fires during combat.
/// Stored on UnitData as part of unit definition (from card catalog).
/// </summary>
public class TriggerConfig
{
    /// <summary>When this trigger fires.</summary>
    public TriggerType TriggerType { get; set; }

    /// <summary>What happens when it fires.</summary>
    public EffectType EffectType { get; set; }

    /// <summary>Effect magnitude.</summary>
    public float Value { get; set; }

    /// <summary>Effect duration (for buffs/debuffs). -1 = permanent.</summary>
    public float Duration { get; set; }

    /// <summary>
    /// Typed effect lifetime replacing sentinel duration semantics.
    /// Duration remains as a bridge in PASS 2.
    /// </summary>
    public EffectLifetime Lifetime { get; set; } = EffectLifetime.Timed(0f);

    /// <summary>For HpThreshold: the HP percentage threshold (0-1).</summary>
    public float Threshold { get; set; }

    /// <summary>For Periodic: the fixed interval in seconds between ticks.</summary>
    public float Interval { get; set; }

    /// <summary>For Periodic: countdown timer until next tick. Decremented each frame, resets to Interval.</summary>
    public float PeriodicTimer { get; set; }

    /// <summary>Area of effect radius (0 = single target).</summary>
    public float AoeRadius { get; set; }

    /// <summary>Damage type for damage effects.</summary>
    public DamageType DamageType { get; set; }

    /// <summary>Whether this trigger has already fired (for one-shot triggers like HpThreshold).</summary>
    public bool HasFired { get; set; }

    /// <summary>For delayed effects: delay in seconds before the effect applies.</summary>
    public float Delay { get; set; }
}

/// <summary>
/// A queued effect that fires after a delay (e.g., death explosion).
/// Stored in MatchState.DelayedEffects, ticked by SimEffects.TickDelayedEffects.
/// </summary>
public class DelayedEffect
{
    /// <summary>Time remaining before the effect fires.</summary>
    public float Timer { get; set; }

    /// <summary>What the effect does.</summary>
    public EffectType EffectType { get; set; }

    /// <summary>Effect magnitude (damage, heal amount, etc.).</summary>
    public float Value { get; set; }

    /// <summary>Damage type for damage effects.</summary>
    public DamageType DamageType { get; set; }

    /// <summary>Area of effect radius (0 = single target).</summary>
    public float AoeRadius { get; set; }

    /// <summary>Area shape for delayed effect recipient resolution.</summary>
    public SpellAreaShape AreaShape { get; set; } = SpellAreaShape.Circle;

    /// <summary>Position where the effect originates (for AoE).</summary>
    public SimVector3 Position { get; set; }

    /// <summary>Source unit ID (for kill credit).</summary>
    public int SourceUnitId { get; set; }

    /// <summary>Source team (for friendly fire filtering).</summary>
    public Team SourceTeam { get; set; }

    /// <summary>Spell affinity filter for delayed spell effects.</summary>
    public SpellAffinity Affinity { get; set; } = SpellAffinity.Enemies;

    /// <summary>Targeting mode for delayed spell effects.</summary>
    public SpellTargetingMode TargetingMode { get; set; } = SpellTargetingMode.Position;

    /// <summary>Optional pinned unit target for single-target delayed effects.</summary>
    public int? TargetUnitId { get; set; }

    /// <summary>Duration payload for buff/debuff effects.</summary>
    public float Duration { get; set; }

    /// <summary>
    /// Typed duration payload for delayed buff/debuff effects.
    /// Duration remains as a bridge in PASS 2.
    /// </summary>
    public EffectLifetime Lifetime { get; set; } = EffectLifetime.Timed(0f);

    /// <summary>Status payload identity for delayed status apply/consume effects.</summary>
    public StatusEffectKind StatusKind { get; set; } = StatusEffectKind.None;

    /// <summary>Status payload tick interval.</summary>
    public float StatusTickInterval { get; set; } = 1f;

    /// <summary>Status payload potency per stack.</summary>
    public float StatusPotencyPerStack { get; set; }

    /// <summary>Status payload max stacks.</summary>
    public int StatusMaxStacks { get; set; } = 1;

    /// <summary>Optional payload fired when a buff created by this delayed effect is removed.</summary>
    public BuffRemovalEffectConfig? RemovalEffect { get; set; }

    /// <summary>Optional target element requirement (-1 = no requirement).</summary>
    public int RequiredTargetElementId { get; set; } = -1;

    /// <summary>Tags required/blocked before this effect can affect a target.</summary>
    public Fateforged.Simulation.Effects.EffectTagRequirements TagRequirements { get; set; } = new();

    /// <summary>Tags granted while a buff created by this effect is active.</summary>
    public List<string> GrantedTags { get; set; } = new();

    /// <summary>Policy used if a matching active buff already exists.</summary>
    public Fateforged.Simulation.Effects.EffectStackPolicy StackPolicy { get; set; } =
        Fateforged.Simulation.Effects.EffectStackPolicy.Independent;

    /// <summary>Optional stack key used by non-independent stack policies.</summary>
    public string StackKey { get; set; } = "";

    /// <summary>Optional cue identity emitted for this effect's lifecycle.</summary>
    public string CueId { get; set; } = "";
}
