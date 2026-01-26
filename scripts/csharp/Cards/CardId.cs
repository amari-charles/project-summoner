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
    // FIRE ELEMENT UNITS
    // =========================================================================

    public const string FireElemental = "fire_elemental";
    public const string FireTitan = "fire_titan";
    public const string FireElementalSwarm = "fire_elemental_swarm";
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
