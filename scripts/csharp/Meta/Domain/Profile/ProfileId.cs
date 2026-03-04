namespace Fateforged.Domain.Profile;

/// <summary>
/// Strongly-typed identifier for player profiles.
/// Prevents typos and enables IDE autocomplete.
/// Note: For GDScript interop, use the string value via implicit conversion.
/// </summary>
public readonly record struct ProfileId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(ProfileId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator ProfileId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset profile ID.</summary>
    public static readonly ProfileId None = new("");
}
