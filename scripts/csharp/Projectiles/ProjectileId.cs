namespace ProjectSummoner.Projectiles;

/// <summary>
/// Strongly-typed identifier for projectile types.
/// Prevents typos and enables IDE autocomplete via ProjectileIds static class.
/// </summary>
public readonly record struct ProjectileId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(ProjectileId id) => id.Value;

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset projectile ID.</summary>
    public static readonly ProjectileId None = new("");
}
