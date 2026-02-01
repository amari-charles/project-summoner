namespace ProjectSummoner.Data.Events;

/// <summary>
/// Strongly-typed identifier for story sequences.
/// Prevents typos and enables IDE autocomplete.
/// </summary>
public readonly record struct SequenceId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(SequenceId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator SequenceId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset sequence ID.</summary>
    public static readonly SequenceId None = new("");
}
