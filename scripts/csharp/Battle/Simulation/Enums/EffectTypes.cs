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
}

/// <summary>
/// How a unit moves during combat.
/// </summary>
public enum MovementStyle
{
    Direct, // Move straight toward target
    Kite, // Maintain distance, retreat if too close
    Strafe, // Circle around target at range
    Stationary, // Never moves (turrets, etc.)
}

/// <summary>
/// What a unit prioritizes when choosing targets.
/// </summary>
public enum TargetingPriority
{
    Nearest, // Closest enemy
    LowestHp, // Enemy with least HP
    HighestHp, // Enemy with most HP
    Ranged, // Prefer ranged enemies
    Summoner, // Always target summoner if possible
}

/// <summary>
/// When a kiting unit retreats.
/// </summary>
public enum RetreatCondition
{
    Never, // Never retreats (direct fighters)
    TooClose, // Retreat when enemy is within KiteRange
    HpThreshold, // Retreat when HP drops below threshold
    Always, // Always maintain max distance (extreme kiters)
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
/// Which team a spell effect targets.
/// </summary>
public enum SpellAffinity
{
    Enemies,
    Allies,
    Both,
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

    /// <summary>Damage type for damage effects.</summary>
    public DamageType DamageType { get; set; }

    /// <summary>AoE radius override (0 = use card's SpellRadius).</summary>
    public float AoeRadius { get; set; }

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
}
