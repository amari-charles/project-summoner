namespace Fateforged.Stats;

/// <summary>
/// Defines when a triggered modifier should activate.
/// Used for conditional effects that only apply in specific combat situations.
/// </summary>
public enum TriggerCondition
{
    /// <summary>
    /// Modifier is always active (default behavior).
    /// </summary>
    Always,

    /// <summary>
    /// Activates when the unit deals damage.
    /// </summary>
    OnHit,

    /// <summary>
    /// Activates when the unit takes damage.
    /// </summary>
    OnTakeHit,

    /// <summary>
    /// Activates when the unit kills an enemy.
    /// </summary>
    OnKill,

    /// <summary>
    /// Activates when the unit dies.
    /// </summary>
    OnDeath,

    /// <summary>
    /// Activates when HP falls below a threshold (use TriggerThreshold for percent).
    /// </summary>
    BelowHpPercent,

    /// <summary>
    /// Activates when HP is above a threshold (use TriggerThreshold for percent).
    /// </summary>
    AboveHpPercent,

    /// <summary>
    /// Activates periodically (use TriggerCooldown for interval).
    /// </summary>
    Periodic,
}
