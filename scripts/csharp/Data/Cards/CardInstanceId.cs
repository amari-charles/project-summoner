namespace ProjectSummoner.Cards;

/// <summary>
/// Strongly-typed identifier for card instances (specific card in a player's collection).
/// Different from CardId which identifies the card type/template.
/// Note: For GDScript interop, use the string value via implicit conversion.
/// </summary>
public readonly record struct CardInstanceId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(CardInstanceId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator CardInstanceId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset card instance ID.</summary>
    public static readonly CardInstanceId None = new("");
}
