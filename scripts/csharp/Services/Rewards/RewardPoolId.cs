namespace ProjectSummoner.Services.Rewards;

/// <summary>
/// Strongly-typed identifier for reward pools.
/// Prevents typos and enables IDE autocomplete via RewardPoolIds static class.
/// </summary>
public readonly record struct RewardPoolId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(RewardPoolId id) => id.Value;

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset reward pool ID.</summary>
    public static readonly RewardPoolId None = new("");
}

/// <summary>
/// All known reward pool IDs. Use these instead of raw strings.
/// Example: RewardPoolIds.TutorialRewards instead of "tutorial_rewards"
/// </summary>
public static class RewardPoolIds
{
    // =========================================================================
    // CURATED POOLS (explicit card lists)
    // =========================================================================

    /// <summary>Basic cards for tutorial rewards.</summary>
    public static readonly RewardPoolId TutorialRewards = new("tutorial_rewards");

    /// <summary>Slightly better cards for early progression.</summary>
    public static readonly RewardPoolId StarterRewards = new("starter_rewards");

    /// <summary>Powerful cards from boss encounters.</summary>
    public static readonly RewardPoolId BossLoot = new("boss_loot");

    // =========================================================================
    // FILTER-BASED POOLS (element + rarity + type combinations)
    // =========================================================================

    /// <summary>Fire element, common rarity, summon type.</summary>
    public static readonly RewardPoolId FireCommonUnits = new("fire_common_units");

    /// <summary>Water element, common rarity, summon type.</summary>
    public static readonly RewardPoolId WaterCommonUnits = new("water_common_units");

    /// <summary>Wind element, common rarity, summon type.</summary>
    public static readonly RewardPoolId WindCommonUnits = new("wind_common_units");

    /// <summary>Earth element, common rarity, summon type.</summary>
    public static readonly RewardPoolId EarthCommonUnits = new("earth_common_units");

    /// <summary>All spells regardless of element.</summary>
    public static readonly RewardPoolId AllSpells = new("all_spells");

    /// <summary>All common rarity cards.</summary>
    public static readonly RewardPoolId AllCommon = new("all_common");

    /// <summary>All rare rarity cards.</summary>
    public static readonly RewardPoolId AllRare = new("all_rare");

    // =========================================================================
    // COMPOSITE POOLS (unions of other pools)
    // =========================================================================

    /// <summary>Union of all elemental common units.</summary>
    public static readonly RewardPoolId ElementalStarters = new("elemental_starters");
}
