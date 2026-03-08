namespace Fateforged.Cards;

/// <summary>
/// Strongly-typed identifier for card traits.
/// Prevents typos and enables IDE autocomplete.
/// </summary>
public readonly record struct CardTraitId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(CardTraitId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator CardTraitId(string value) => new(value);

    /// <summary>Create a CardTraitId from a string. Standardized factory for facade boundaries.</summary>
    public static CardTraitId FromString(string id) => new(id);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset trait ID.</summary>
    public static readonly CardTraitId None = new("");
}
