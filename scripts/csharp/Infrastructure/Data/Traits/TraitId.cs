namespace Fateforged.Data.Traits;

/// <summary>
/// Strongly-typed identifier for trait types.
/// Prevents typos and enables IDE autocomplete via TraitIds static class.
/// </summary>
public readonly record struct TraitId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(TraitId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator TraitId(string value) => new(value);

    /// <summary>Create a TraitId from a string. Standardized factory for facade boundaries.</summary>
    public static TraitId FromString(string id) => new(id);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset trait ID.</summary>
    public static readonly TraitId None = new("");
}

/// <summary>
/// All known trait IDs. Use these instead of raw strings.
/// Example: TraitIds.FireAffinity instead of "trait_fire_affinity"
/// </summary>
public static class TraitIds
{
    // =========================================================================
    // INNATE TRAITS - Fire Summoner
    // =========================================================================

    public static readonly TraitId FireAffinity = new("trait_fire_affinity");
    public static readonly TraitId BurningSpirit = new("trait_burning_spirit");

    // =========================================================================
    // INNATE TRAITS - Water Summoner
    // =========================================================================

    public static readonly TraitId WaterAffinity = new("trait_water_affinity");
    public static readonly TraitId TidalResilience = new("trait_tidal_resilience");

    // =========================================================================
    // INNATE TRAITS - Wind Summoner
    // =========================================================================

    public static readonly TraitId WindAffinity = new("trait_wind_affinity");
    public static readonly TraitId SwiftCasting = new("trait_swift_casting");

    // =========================================================================
    // INNATE TRAITS - Earth Summoner
    // =========================================================================

    public static readonly TraitId EarthAffinity = new("trait_earth_affinity");
    public static readonly TraitId StoneFortitude = new("trait_stone_fortitude");

    // =========================================================================
    // INNATE TRAITS - Lightning Summoner
    // =========================================================================

    public static readonly TraitId LightningAffinity = new("trait_lightning_affinity");

    // =========================================================================
    // INNATE TRAITS - Life Summoner
    // =========================================================================

    public static readonly TraitId LifeAffinity = new("trait_life_affinity");

    // =========================================================================
    // INNATE TRAITS - Death Summoner
    // =========================================================================

    public static readonly TraitId DeathAffinity = new("trait_death_affinity");

    // =========================================================================
    // ACQUIRABLE TRAITS - Summoner Identity Lines
    // =========================================================================

    // Cole
    public static readonly TraitId ColeSoulStrengthI = new("trait_cole_soul_strength_i");
    public static readonly TraitId ColeSoulStrengthII = new("trait_cole_soul_strength_ii");
    public static readonly TraitId ColeSoulStrengthIII = new("trait_cole_soul_strength_iii");
    public static readonly TraitId ColeSoulStrengthIV = new("trait_cole_soul_strength_iv");
    public static readonly TraitId ColeCastSpeedI = new("trait_cole_cast_speed_i");
    public static readonly TraitId ColeCastSpeedII = new("trait_cole_cast_speed_ii");
    public static readonly TraitId ColeCastSpeedIII = new("trait_cole_cast_speed_iii");
    public static readonly TraitId ColeCastSpeedIV = new("trait_cole_cast_speed_iv");

    // Selene
    public static readonly TraitId SeleneHealthI = new("trait_selene_health_i");
    public static readonly TraitId SeleneHealthII = new("trait_selene_health_ii");
    public static readonly TraitId SeleneHealthIII = new("trait_selene_health_iii");
    public static readonly TraitId SeleneHealthIV = new("trait_selene_health_iv");
    public static readonly TraitId SeleneMaxManaI = new("trait_selene_max_mana_i");
    public static readonly TraitId SeleneMaxManaII = new("trait_selene_max_mana_ii");
    public static readonly TraitId SeleneMaxManaIII = new("trait_selene_max_mana_iii");
    public static readonly TraitId SeleneMaxManaIV = new("trait_selene_max_mana_iv");

    // Mei
    public static readonly TraitId MeiCastSpeedI = new("trait_mei_cast_speed_i");
    public static readonly TraitId MeiCastSpeedII = new("trait_mei_cast_speed_ii");
    public static readonly TraitId MeiCastSpeedIII = new("trait_mei_cast_speed_iii");
    public static readonly TraitId MeiCastSpeedIV = new("trait_mei_cast_speed_iv");
    public static readonly TraitId MeiMaxManaI = new("trait_mei_max_mana_i");
    public static readonly TraitId MeiMaxManaII = new("trait_mei_max_mana_ii");
    public static readonly TraitId MeiMaxManaIII = new("trait_mei_max_mana_iii");
    public static readonly TraitId MeiMaxManaIV = new("trait_mei_max_mana_iv");

    // Teo
    public static readonly TraitId TeoHealthI = new("trait_teo_health_i");
    public static readonly TraitId TeoHealthII = new("trait_teo_health_ii");
    public static readonly TraitId TeoHealthIII = new("trait_teo_health_iii");
    public static readonly TraitId TeoHealthIV = new("trait_teo_health_iv");
    public static readonly TraitId TeoSoulStrengthI = new("trait_teo_soul_strength_i");
    public static readonly TraitId TeoSoulStrengthII = new("trait_teo_soul_strength_ii");
    public static readonly TraitId TeoSoulStrengthIII = new("trait_teo_soul_strength_iii");
    public static readonly TraitId TeoSoulStrengthIV = new("trait_teo_soul_strength_iv");

    // =========================================================================
    // UNIT TRAITS - Global Pool (available to all units/cards)
    // =========================================================================

    /// <summary>Fortitude: +8% HP per level.</summary>
    public static readonly TraitId Fortitude = new("trait_fortitude");
    public static readonly TraitId FortitudeII = new("trait_fortitude_ii");
    public static readonly TraitId FortitudeIII = new("trait_fortitude_iii");
    public static readonly TraitId FortitudeIV = new("trait_fortitude_iv");

    /// <summary>Power: +6% attack damage per level.</summary>
    public static readonly TraitId Power = new("trait_power");
    public static readonly TraitId PowerII = new("trait_power_ii");
    public static readonly TraitId PowerIII = new("trait_power_iii");
    public static readonly TraitId PowerIV = new("trait_power_iv");

    /// <summary>Swiftness: +5% attack speed per level.</summary>
    public static readonly TraitId Swiftness = new("trait_swiftness");
    public static readonly TraitId SwiftnessII = new("trait_swiftness_ii");
    public static readonly TraitId SwiftnessIII = new("trait_swiftness_iii");
    public static readonly TraitId SwiftnessIV = new("trait_swiftness_iv");

    /// <summary>Agility: +5% move speed per level.</summary>
    public static readonly TraitId Agility = new("trait_agility");
    public static readonly TraitId AgilityII = new("trait_agility_ii");
    public static readonly TraitId AgilityIII = new("trait_agility_iii");
    public static readonly TraitId AgilityIV = new("trait_agility_iv");

    /// <summary>Reach: +attack range tiers.</summary>
    public static readonly TraitId Reach = new("trait_reach");
    public static readonly TraitId ReachII = new("trait_reach_ii");
    public static readonly TraitId ReachIII = new("trait_reach_iii");
    public static readonly TraitId ReachIV = new("trait_reach_iv");

    /// <summary>Plating: +armor tiers.</summary>
    public static readonly TraitId Plating = new("trait_plating");
    public static readonly TraitId PlatingII = new("trait_plating_ii");
    public static readonly TraitId PlatingIII = new("trait_plating_iii");
    public static readonly TraitId PlatingIV = new("trait_plating_iv");

    /// <summary>Warding: +magic resist tiers.</summary>
    public static readonly TraitId Warding = new("trait_warding");
    public static readonly TraitId WardingII = new("trait_warding_ii");
    public static readonly TraitId WardingIII = new("trait_warding_iii");
    public static readonly TraitId WardingIV = new("trait_warding_iv");

    /// <summary>Soulforce: +soul strength tiers.</summary>
    public static readonly TraitId Soulforce = new("trait_soulforce");
    public static readonly TraitId SoulforceII = new("trait_soulforce_ii");
    public static readonly TraitId SoulforceIII = new("trait_soulforce_iii");
    public static readonly TraitId SoulforceIV = new("trait_soulforce_iv");

    /// <summary>Arcana: +magic damage tiers.</summary>
    public static readonly TraitId Arcana = new("trait_arcana");
    public static readonly TraitId ArcanaII = new("trait_arcana_ii");
    public static readonly TraitId ArcanaIII = new("trait_arcana_iii");
    public static readonly TraitId ArcanaIV = new("trait_arcana_iv");

    /// <summary>Legion: +unit count tiers.</summary>
    public static readonly TraitId Legion = new("trait_legion");
    public static readonly TraitId LegionII = new("trait_legion_ii");
    public static readonly TraitId LegionIII = new("trait_legion_iii");
    public static readonly TraitId LegionIV = new("trait_legion_iv");

    // =========================================================================
    // SPECIAL TRAITS - Granted by specific game events
    // =========================================================================

    /// <summary>Fortune Favors the Bold: +10% damage when selecting random summoner.</summary>
    public static readonly TraitId FortuneFavorsTheBold = new("trait_fortune_favors_the_bold");
}
