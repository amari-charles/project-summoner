namespace ProjectSummoner.Cards;

/// <summary>
/// Strongly-typed identifier for card upgrades.
/// Prevents typos and enables IDE autocomplete.
/// </summary>
public readonly record struct CardUpgradeId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(CardUpgradeId id) => id.Value;

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset upgrade ID.</summary>
    public static readonly CardUpgradeId None = new("");
}
