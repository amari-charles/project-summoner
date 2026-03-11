namespace Fateforged.Meta.Campaign;

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
    /// <summary>Aggressive doctrine path.</summary>
    public static readonly ChoiceId Aggressive = new("aggressive");

    /// <summary>Prepared doctrine path.</summary>
    public static readonly ChoiceId Prepared = new("prepared");

    /// <summary>Scouting doctrine path.</summary>
    public static readonly ChoiceId Insight = new("insight");

    /// <summary>Upper route choice.</summary>
    public static readonly ChoiceId Ridge = new("ridge");

    /// <summary>Lower route choice.</summary>
    public static readonly ChoiceId River = new("river");

    /// <summary>Wide flank route choice.</summary>
    public static readonly ChoiceId Grove = new("grove");

    /// <summary>Elite path - harder difficulty with better rewards.</summary>
    public static readonly ChoiceId Elite = new("elite");

    /// <summary>Standard path - normal difficulty.</summary>
    public static readonly ChoiceId Standard = new("standard");

    /// <summary>High-variance gambit path.</summary>
    public static readonly ChoiceId Gambit = new("gambit");
}
