namespace Fateforged.Constants;

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
/// Example: UnitIds.FireWisp instead of "fire_wisp"
/// </summary>
public static class UnitIds
{
    // =========================================================================
    // WISPS (Basic starter units for each element)
    // =========================================================================

    public static readonly UnitId FireWisp = new("fire_wisp");
    public static readonly UnitId WaterWisp = new("water_wisp");
    public static readonly UnitId WindWisp = new("wind_wisp");
    public static readonly UnitId EarthWisp = new("earth_wisp");
    public static readonly UnitId LightningWisp = new("lightning_wisp");
    public static readonly UnitId LifeWisp = new("life_wisp");
    public static readonly UnitId DeathWisp = new("death_wisp");
    public static readonly UnitId ShadowWisp = new("shadow_wisp");

    // =========================================================================
    // FIRE ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId FireTitan = new("fire_titan");
    public static readonly UnitId FireAnt = new("fire_ant");
    public static readonly UnitId FireBoar = new("fire_boar");
    public static readonly UnitId FireWolf = new("fire_wolf");
    public static readonly UnitId FireSpider = new("fire_spider");

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId EarthSprite = new("earth_sprite");
    public static readonly UnitId EarthKomodoDragon = new("earth_komodo_dragon");
    public static readonly UnitId Rock = new("rock");
    public static readonly UnitId StoneApe = new("stone_ape");
    public static readonly UnitId EarthRockThrower = new("earth_rock_thrower");

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId Puff = new("puff");

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId WaterFrog = new("water_frog");
    public static readonly UnitId MamaDuck = new("mama_duck");
    public static readonly UnitId Duckling = new("duckling");
}
