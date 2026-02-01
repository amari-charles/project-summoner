namespace ProjectSummoner.Services.Campaign;

/// <summary>
/// Strongly-typed identifier for campaign choices (branching paths).
/// Prevents typos and enables IDE autocomplete.
/// Note: For GDScript interop, use the string value via implicit conversion.
/// </summary>
public readonly record struct ChoiceId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(ChoiceId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator ChoiceId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset choice ID.</summary>
    public static readonly ChoiceId None = new("");
}

/// <summary>
/// Well-known choice IDs for campaign path branching.
/// </summary>
public static class ChoiceIds
{
    /// <summary>Elite path - harder difficulty with better rewards.</summary>
    public static readonly ChoiceId Elite = new("elite");

    /// <summary>Standard path - normal difficulty.</summary>
    public static readonly ChoiceId Standard = new("standard");
}
