namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Constants for all trait IDs in the game.
/// Provides compile-time safety for trait references.
/// </summary>
public static class TraitId
{
    // =========================================================================
    // INNATE TRAITS (assigned to summoners)
    // =========================================================================

    // Fire Summoner
    public const string FireAffinity = "trait_fire_affinity";
    public const string BurningSpirit = "trait_burning_spirit";

    // Water Summoner
    public const string WaterAffinity = "trait_water_affinity";
    public const string TidalResilience = "trait_tidal_resilience";

    // Wind Summoner
    public const string WindAffinity = "trait_wind_affinity";
    public const string SwiftCasting = "trait_swift_casting";

    // Earth Summoner
    public const string EarthAffinity = "trait_earth_affinity";
    public const string StoneFortitude = "trait_stone_fortitude";

    // Lightning Summoner
    public const string LightningAffinity = "trait_lightning_affinity";

    // Life Summoner
    public const string LifeAffinity = "trait_life_affinity";

    // Death Summoner
    public const string DeathAffinity = "trait_death_affinity";

    // =========================================================================
    // TRIGGERED TRAITS (conditional effects)
    // =========================================================================

    /// <summary>Berserker: +20% damage when below 50% HP.</summary>
    public const string Berserker = "trait_berserker";

    /// <summary>Vengeful: +10% attack speed for 5s after taking damage.</summary>
    public const string Vengeful = "trait_vengeful";

    /// <summary>Soul Harvest: Heal 5 HP on kill.</summary>
    public const string SoulHarvest = "trait_soul_harvest";

    // =========================================================================
    // ACQUIRABLE TRAITS - Global Summoner Pool
    // =========================================================================

    /// <summary>Iron Will: +5 damage reduction.</summary>
    public const string IronWill = "trait_iron_will";

    /// <summary>Quick Recovery: +10% mana regen.</summary>
    public const string QuickRecovery = "trait_quick_recovery";

    /// <summary>Vitality Boost: +100 max health.</summary>
    public const string VitalityBoost = "trait_vitality_boost";

    /// <summary>Swift Strike: +10% attack speed for units.</summary>
    public const string SwiftStrike = "trait_swift_strike";

    // =========================================================================
    // ACQUIRABLE TRAITS - Element Mastery (element-exclusive)
    // =========================================================================

    /// <summary>Inferno Mastery: Fire damage and fire unit buffs.</summary>
    public const string InfernoMastery = "trait_inferno_mastery";

    /// <summary>Tidal Mastery: Water damage and water unit buffs.</summary>
    public const string TidalMastery = "trait_tidal_mastery";

    // =========================================================================
    // UNIT TRAITS - Global Pool (available to all units/cards)
    // =========================================================================

    /// <summary>Fortitude: +8% HP per level.</summary>
    public const string Fortitude = "trait_fortitude";

    /// <summary>Power: +6% attack damage per level.</summary>
    public const string Power = "trait_power";

    /// <summary>Swiftness: +5% attack speed per level.</summary>
    public const string Swiftness = "trait_swiftness";

    /// <summary>Agility: +5% move speed per level.</summary>
    public const string Agility = "trait_agility";
}
