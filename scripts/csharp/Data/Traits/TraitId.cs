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
}
