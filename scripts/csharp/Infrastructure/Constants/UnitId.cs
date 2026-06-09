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
    // ACADEMY PLAYTEST PLACEHOLDERS
    // =========================================================================

    public static readonly UnitId NeutralStarterUnit = new("neutral_starter_unit");
    public static readonly UnitId TrainingTarget = new("training_target");
    public static readonly UnitId WeakEnemyUnit = new("weak_enemy_unit");

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
    public static readonly UnitId CinderCaster = new("cinder_caster");
    public static readonly UnitId EmberBombCarrier = new("ember_bomb_carrier");
    public static readonly UnitId KindlingSwarmUnit = new("kindling_swarm_unit");
    public static readonly UnitId FireFrontliner = new("fire_frontliner");
    public static readonly UnitId OverheatBrawler = new("overheat_brawler");
    public static readonly UnitId FlameChanneler = new("flame_channeler");

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId EarthSprite = new("earth_sprite");
    public static readonly UnitId EarthKomodoDragon = new("earth_komodo_dragon");
    public static readonly UnitId Rock = new("rock");
    public static readonly UnitId StoneApe = new("stone_ape");
    public static readonly UnitId EarthRockThrower = new("earth_rock_thrower");
    public static readonly UnitId TauntPulseGuardian = new("taunt_pulse_guardian");
    public static readonly UnitId EarthFlatDamageReductionTank = new(
        "earth_flat_damage_reduction_tank"
    );
    public static readonly UnitId EarthBulletUnit = new("earth_bullet_unit");
    public static readonly UnitId EarthShieldSupport = new("earth_shield_support");
    public static readonly UnitId BurrowAmbusher = new("burrow_ambusher");

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId Puff = new("puff");
    public static readonly UnitId WindEvasionTank = new("wind_evasion_tank");
    public static readonly UnitId WindPushbackUnit = new("wind_pushback_unit");
    public static readonly UnitId WindCleaveUnit = new("wind_cleave_unit");
    public static readonly UnitId WindDiver = new("wind_diver");
    public static readonly UnitId WindSpeedSupport = new("wind_speed_support");
    public static readonly UnitId WindMissSupport = new("wind_miss_support");
    public static readonly UnitId WindSwarmUnit = new("wind_swarm_unit");
    public static readonly UnitId DashStriker = new("dash_striker");

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public static readonly UnitId WaterFrog = new("water_frog");
    public static readonly UnitId MamaDuck = new("mama_duck");
    public static readonly UnitId Duckling = new("duckling");
    public static readonly UnitId WaterBulwark = new("water_bulwark");
    public static readonly UnitId WaterMender = new("water_mender");
    public static readonly UnitId WaterSkimmer = new("water_skimmer");
    public static readonly UnitId WaterRedistributor = new("water_redistributor");
    public static readonly UnitId SlipperyMelee = new("slippery_melee");
    public static readonly UnitId WaterRanged = new("water_ranged");
    public static readonly UnitId BarbedInflator = new("barbed_inflator");
    public static readonly UnitId LifeMedic = new("life_medic");
    public static readonly UnitId PoisonNeedler = new("poison_needler");
    public static readonly UnitId PiercingLaser = new("piercing_laser");
}
