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
    // ACQUIRED BOONS (earned through gameplay)
    // =========================================================================

    public const string BoonVeteran = "boon_veteran";
    public const string BoonManaWell = "boon_mana_well";
    public const string BoonBattleHardened = "boon_battle_hardened";
    public const string BoonFortuneFavors = "boon_fortune_favors";

    // Special boons
    public const string FortuneFavorsBold = "trait_fortune_favors_bold";
}
