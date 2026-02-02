namespace ProjectSummoner.Data.Traits;

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
    // TRIGGERED TRAITS (conditional effects)
    // =========================================================================

    /// <summary>Berserker: +20% damage when below 50% HP.</summary>
    public static readonly TraitId Berserker = new("trait_berserker");

    /// <summary>Vengeful: +10% attack speed for 5s after taking damage.</summary>
    public static readonly TraitId Vengeful = new("trait_vengeful");

    /// <summary>Soul Harvest: Heal 5 HP on kill.</summary>
    public static readonly TraitId SoulHarvest = new("trait_soul_harvest");

    // =========================================================================
    // ACQUIRABLE TRAITS - Global Summoner Pool
    // =========================================================================

    /// <summary>Iron Will: +5 damage reduction.</summary>
    public static readonly TraitId IronWill = new("trait_iron_will");

    /// <summary>Quick Recovery: +10% mana regen.</summary>
    public static readonly TraitId QuickRecovery = new("trait_quick_recovery");

    /// <summary>Vitality Boost: +100 max health.</summary>
    public static readonly TraitId VitalityBoost = new("trait_vitality_boost");

    /// <summary>Swift Strike: +10% attack speed for units.</summary>
    public static readonly TraitId SwiftStrike = new("trait_swift_strike");

    // =========================================================================
    // ACQUIRABLE TRAITS - Element Mastery (element-exclusive)
    // =========================================================================

    /// <summary>Inferno Mastery: Fire damage and fire unit buffs.</summary>
    public static readonly TraitId InfernoMastery = new("trait_inferno_mastery");

    /// <summary>Tidal Mastery: Water damage and water unit buffs.</summary>
    public static readonly TraitId TidalMastery = new("trait_tidal_mastery");

    // =========================================================================
    // UNIT TRAITS - Global Pool (available to all units/cards)
    // =========================================================================

    /// <summary>Fortitude: +8% HP per level.</summary>
    public static readonly TraitId Fortitude = new("trait_fortitude");

    /// <summary>Power: +6% attack damage per level.</summary>
    public static readonly TraitId Power = new("trait_power");

    /// <summary>Swiftness: +5% attack speed per level.</summary>
    public static readonly TraitId Swiftness = new("trait_swiftness");

    /// <summary>Agility: +5% move speed per level.</summary>
    public static readonly TraitId Agility = new("trait_agility");
}
