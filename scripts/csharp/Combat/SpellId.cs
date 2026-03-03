namespace Fateforged.Combat;

/// <summary>
/// Strongly-typed identifier for spells.
/// Prevents typos and enables IDE autocomplete.
/// Note: For GDScript interop, use the string value via implicit conversion.
/// </summary>
public readonly record struct SpellId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(SpellId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator SpellId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset spell ID.</summary>
    public static readonly SpellId None = new("");
}

/// <summary>
/// Well-known spell IDs.
/// </summary>
public static class SpellIds
{
    /// <summary>Fireball spell.</summary>
    public static readonly SpellId Fireball = new("fireball");

    /// <summary>Mana bolt spell.</summary>
    public static readonly SpellId ManaBolt = new("mana_bolt");

    /// <summary>Rally command spell.</summary>
    public static readonly SpellId Rally = new("rally");

    /// <summary>Guard command spell.</summary>
    public static readonly SpellId Guard = new("guard");

    /// <summary>Charge command spell.</summary>
    public static readonly SpellId Charge = new("charge");
}
