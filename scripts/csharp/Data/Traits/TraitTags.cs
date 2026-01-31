namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Constants for trait eligibility tags.
/// Tags determine which entities (summoners, summons, spells) can acquire which traits.
/// Eligibility rule: (has ANY of Tags) AND (has ALL of RequiredTags)
/// </summary>
public static class TraitTags
{
    // =========================================================================
    // ENTITY TYPES (required - traits must specify at least one)
    // =========================================================================

    /// <summary>Tag for summoner-only traits (affects the character).</summary>
    public const string Summoner = "summoner";

    /// <summary>Tag for summon traits (creature upgrades that affect spawned units).</summary>
    public const string Summon = "summon";

    /// <summary>Tag for spell traits (spell upgrades that affect spell stats).</summary>
    public const string Spell = "spell";

    // =========================================================================
    // GLOBAL TAG
    // =========================================================================

    /// <summary>Available to all entities with matching entity type.</summary>
    public const string Global = "global";

    // =========================================================================
    // ELEMENTS
    // =========================================================================

    public const string Fire = "fire";
    public const string Water = "water";
    public const string Wind = "wind";
    public const string Earth = "earth";
    public const string Lightning = "lightning";
    public const string Life = "life";
    public const string Death = "death";
    public const string Shadow = "shadow";
    public const string Neutral = "neutral";

    // =========================================================================
    // CREATURE TYPES (summons only)
    // =========================================================================

    public const string Beast = "beast";
    public const string Elemental = "elemental";
    public const string Humanoid = "humanoid";
    public const string Construct = "construct";
    public const string Insect = "insect";
    public const string Aerial = "aerial";
    public const string Amphibian = "amphibian";
    public const string Spirit = "spirit";
    public const string Nature = "nature";

    // =========================================================================
    // SUMMONER IDENTIFIERS (for summoner-exclusive traits)
    // =========================================================================

    public const string Cole = "cole";
    public const string Selene = "selene";
    public const string Mei = "mei";
    public const string Teo = "teo";

    // =========================================================================
    // SUMMON IDENTIFIERS (for summon-exclusive upgrades)
    // =========================================================================

    public const string FireWisp = "fire_wisp";
    public const string WaterWisp = "water_wisp";
    public const string WindWisp = "wind_wisp";
    public const string EarthWisp = "earth_wisp";
    public const string LightningWisp = "lightning_wisp";
    public const string LifeWisp = "life_wisp";
    public const string DeathWisp = "death_wisp";
    public const string ShadowWisp = "shadow_wisp";
}
