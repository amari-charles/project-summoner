namespace ProjectSummoner.Cards;

/// <summary>
/// Card ID constants - mirrors GDScript CardIDs for C# code.
/// Use these instead of string literals for type safety.
/// </summary>
public static class CardId
{
    // =========================================================================
    // SPELLS
    // =========================================================================

    public const string Fireball = "fireball";
    public const string Rally = "rally";
    public const string Guard = "guard";
    public const string Charge = "charge";
    public const string ManaBolt = "mana_bolt";

    // =========================================================================
    // WISPS (Basic starter units for each element)
    // =========================================================================

    public const string FireWisp = "fire_wisp";
    public const string WaterWisp = "water_wisp";
    public const string WindWisp = "wind_wisp";
    public const string EarthWisp = "earth_wisp";
    public const string LightningWisp = "lightning_wisp";
    public const string LifeWisp = "life_wisp";
    public const string DeathWisp = "death_wisp";
    public const string ShadowWisp = "shadow_wisp";
    public const string FireWispSwarm = "fire_wisp_swarm";

    // =========================================================================
    // FIRE ELEMENT UNITS
    // =========================================================================

    public const string FireTitan = "fire_titan";
    public const string FireAnt = "fire_ant";
    public const string FireAntSwarm = "fire_ant_swarm";

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public const string EarthSprite = "earth_sprite";
    public const string Rock = "rock";

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public const string Puff = "puff";
    public const string CloudSwarm = "cloud_swarm";

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public const string WaterFrog = "water_frog";
}
