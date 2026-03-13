namespace Fateforged.Cards;

/// <summary>
/// Strongly-typed identifier for card types.
/// Prevents typos and enables IDE autocomplete via CardIds static class.
/// Mirrors the UnitId pattern from Constants/UnitId.cs.
/// </summary>
public readonly record struct CardId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(CardId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator CardId(string value) => new(value);

    /// <summary>Create a CardId from a string. Standardized factory for facade boundaries.</summary>
    public static CardId FromString(string id) => new(id);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset card ID.</summary>
    public static readonly CardId None = new("");
}

/// <summary>
/// All known card IDs. Use these instead of raw strings.
/// Example: CardIds.FireWisp instead of "fire_wisp"
/// </summary>
public static class CardIds
{
    // =========================================================================
    // SPELLS
    // =========================================================================

    public static readonly CardId Fireball = new("fireball");
    public static readonly CardId Rally = new("rally");
    public static readonly CardId Guard = new("guard");
    public static readonly CardId Charge = new("charge");
    public static readonly CardId ManaBolt = new("mana_bolt");
    public static readonly CardId WeavingBolt = new("weaving_bolt");
    public static readonly CardId HealingField = new("healing_field");

    // =========================================================================
    // WISPS (Basic starter units for each element)
    // =========================================================================

    public static readonly CardId FireWisp = new("fire_wisp");
    public static readonly CardId WaterWisp = new("water_wisp");
    public static readonly CardId WindWisp = new("wind_wisp");
    public static readonly CardId EarthWisp = new("earth_wisp");
    public static readonly CardId LightningWisp = new("lightning_wisp");
    public static readonly CardId LifeWisp = new("life_wisp");
    public static readonly CardId DeathWisp = new("death_wisp");
    public static readonly CardId ShadowWisp = new("shadow_wisp");
    public static readonly CardId FireWispSwarm = new("fire_wisp_swarm");

    // =========================================================================
    // FIRE ELEMENT UNITS
    // =========================================================================

    public static readonly CardId FireTitan = new("fire_titan");
    public static readonly CardId FireAnt = new("fire_ant");
    public static readonly CardId FireAntSwarm = new("fire_ant_swarm");
    public static readonly CardId FireBoar = new("fire_boar");
    public static readonly CardId FireWolf = new("fire_wolf");
    public static readonly CardId FireSpider = new("fire_spider");

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly CardId Pebbloom = new("pebbloom");
    public static readonly CardId EarthKomodoDragon = new("earth_komodo_dragon");
    public static readonly CardId Rock = new("rock");
    public static readonly CardId StoneApe = new("stone_ape");
    public static readonly CardId EarthRockThrower = new("earth_rock_thrower");
    public static readonly CardId TauntPulseGuardian = new("taunt_pulse_guardian");

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly CardId Puff = new("puff");
    public static readonly CardId CloudSwarm = new("cloud_swarm");

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public static readonly CardId WaterFrog = new("water_frog");
    public static readonly CardId MamaDuck = new("mama_duck");
    public static readonly CardId LifeMedic = new("life_medic");
    public static readonly CardId PoisonNeedler = new("poison_needler");
    public static readonly CardId PiercingLaser = new("piercing_laser");
}
