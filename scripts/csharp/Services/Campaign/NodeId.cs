namespace Fateforged.Meta.Campaign;

/// <summary>
/// Strongly-typed identifier for campaign graph nodes.
/// Prevents typos and enables IDE autocomplete.
/// Note: For GDScript interop, use the string value via implicit conversion.
/// </summary>
public readonly record struct NodeId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(NodeId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator NodeId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset node ID.</summary>
    public static readonly NodeId None = new("");
}
