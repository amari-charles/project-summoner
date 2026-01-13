namespace ProjectSummoner.Combat.Hitbox;

/// <summary>
/// Determines how long a hitbox exists.
/// </summary>
public enum HitboxLifetime
{
    /// <summary>
    /// Hitbox exists until manually destroyed.
    /// Used for melee attack frames that are controlled by animation.
    /// </summary>
    UntilDestroyed,

    /// <summary>
    /// Hitbox destroys itself on first hit.
    /// Used for projectiles that disappear on impact.
    /// </summary>
    UntilFirstHit,

    /// <summary>
    /// Hitbox exists for a set duration.
    /// Used for damage zones, lingering effects.
    /// </summary>
    Timed,

    /// <summary>
    /// Hitbox persists indefinitely and can hit repeatedly.
    /// Used for environmental hazards.
    /// </summary>
    Persistent
}
