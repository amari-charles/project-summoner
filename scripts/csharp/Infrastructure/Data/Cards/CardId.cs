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
    public static readonly CardId MagicBolt = new("magic_bolt");
    public static readonly CardId WeavingBolt = new("weaving_bolt");
    public static readonly CardId HealingField = new("healing_field");
    public static readonly CardId Cleanse = new("cleanse");
    public static readonly CardId WaterJet = new("water_jet");
    public static readonly CardId RainField = new("rain_field");
    public static readonly CardId TailWind = new("tail_wind");
    public static readonly CardId Fortify = new("fortify");
    public static readonly CardId FireAreaBurn = new("fire_area_burn");
    public static readonly CardId BurnCashout = new("burn_cashout");
    public static readonly CardId Overheat = new("overheat");
    public static readonly CardId IgnitionMark = new("ignition_mark");
    public static readonly CardId FlareShield = new("flare_shield");
    public static readonly CardId BubbleShield = new("bubble_shield");
    public static readonly CardId Whirlpool = new("whirlpool");
    public static readonly CardId Flow = new("flow");
    public static readonly CardId Quake = new("quake");
    public static readonly CardId StoneSpike = new("stone_spike");
    public static readonly CardId GravityWell = new("gravity_well");
    public static readonly CardId ReformEarth = new("reform_earth");
    public static readonly CardId EarthenGrip = new("earthen_grip");
    public static readonly CardId Tornado = new("tornado");
    public static readonly CardId Crosswind = new("crosswind");
    public static readonly CardId AirBullet = new("air_bullet");
    public static readonly CardId Evacuate = new("evacuate");
    public static readonly CardId WindShear = new("wind_shear");

    // =========================================================================
    // ACADEMY PLAYTEST PLACEHOLDERS
    // =========================================================================

    public static readonly CardId NeutralStarterUnit = new("neutral_starter_unit");
    public static readonly CardId TrainingTarget = new("training_target");
    public static readonly CardId WeakEnemyUnit = new("weak_enemy_unit");

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
    public static readonly CardId CinderCaster = new("cinder_caster");
    public static readonly CardId EmberBombCarrier = new("ember_bomb_carrier");
    public static readonly CardId KindlingSwarm = new("kindling_swarm");
    public static readonly CardId FireFrontliner = new("fire_frontliner");
    public static readonly CardId OverheatBrawler = new("overheat_brawler");
    public static readonly CardId FlameChanneler = new("flame_channeler");

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly CardId Pebbloom = new("pebbloom");
    public static readonly CardId EarthKomodoDragon = new("earth_komodo_dragon");
    public static readonly CardId Rock = new("rock");
    public static readonly CardId StoneApe = new("stone_ape");
    public static readonly CardId EarthRockThrower = new("earth_rock_thrower");
    public static readonly CardId TauntPulseGuardian = new("taunt_pulse_guardian");
    public static readonly CardId EarthFlatDamageReductionTank = new(
        "earth_flat_damage_reduction_tank"
    );
    public static readonly CardId EarthBulletUnit = new("earth_bullet_unit");
    public static readonly CardId EarthShieldSupport = new("earth_shield_support");
    public static readonly CardId BurrowAmbusher = new("burrow_ambusher");

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly CardId Puff = new("puff");
    public static readonly CardId CloudSwarm = new("cloud_swarm");
    public static readonly CardId WindEvasionTank = new("wind_evasion_tank");
    public static readonly CardId WindPushbackUnit = new("wind_pushback_unit");
    public static readonly CardId WindCleaveUnit = new("wind_cleave_unit");
    public static readonly CardId WindDiver = new("wind_diver");
    public static readonly CardId WindSpeedSupport = new("wind_speed_support");
    public static readonly CardId WindMissSupport = new("wind_miss_support");
    public static readonly CardId WindSwarm = new("wind_swarm");
    public static readonly CardId DashStriker = new("dash_striker");

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public static readonly CardId WaterFrog = new("water_frog");
    public static readonly CardId MamaDuck = new("mama_duck");
    public static readonly CardId WaterBulwark = new("water_bulwark");
    public static readonly CardId WaterMender = new("water_mender");
    public static readonly CardId WaterSkimmer = new("water_skimmer");
    public static readonly CardId WaterRedistributor = new("water_redistributor");
    public static readonly CardId SlipperyMelee = new("slippery_melee");
    public static readonly CardId WaterRanged = new("water_ranged");
    public static readonly CardId BarbedInflator = new("barbed_inflator");
    public static readonly CardId LifeMedic = new("life_medic");
    public static readonly CardId PoisonNeedler = new("poison_needler");
    public static readonly CardId PiercingLaser = new("piercing_laser");
}
