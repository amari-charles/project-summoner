namespace ProjectSummoner.Services.Deck;

/// <summary>
/// Strongly-typed identifier for decks.
/// Prevents typos and enables IDE autocomplete.
/// </summary>
public readonly record struct DeckId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(DeckId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator DeckId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset deck ID.</summary>
    public static readonly DeckId None = new("");
}
