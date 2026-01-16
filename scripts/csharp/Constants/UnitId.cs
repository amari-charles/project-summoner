namespace ProjectSummoner.Constants;

/// <summary>
/// Strongly-typed identifier for unit types.
/// Prevents typos and enables IDE autocomplete via UnitIds static class.
/// </summary>
public readonly record struct UnitId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(UnitId id) => id.Value;

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset unit ID.</summary>
    public static readonly UnitId None = new("");
}

/// <summary>
/// All known unit type IDs. Use these instead of raw strings.
/// Example: UnitIds.FireElemental instead of "fire_elemental"
/// </summary>
public static class UnitIds
{
    // =========================================================================
    // FIRE ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId FireElemental = new("fire_elemental");
    public static readonly UnitId FireTitan = new("fire_titan");
    public static readonly UnitId FireAnt = new("fire_ant");

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId EarthSprite = new("earth_sprite");
    public static readonly UnitId Rock = new("rock");

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId Puff = new("puff");
}
