namespace ProjectSummoner.Services.Rewards;

/// <summary>
/// Identifiers for predefined reward pools.
/// Each pool can define filters, explicit card lists, or combine other pools.
/// </summary>
public enum RewardPoolId
{
    // =========================================================================
    // CURATED POOLS (explicit card lists)
    // =========================================================================

    /// <summary>Basic cards for tutorial rewards.</summary>
    TutorialRewards,

    /// <summary>Slightly better cards for early progression.</summary>
    StarterRewards,

    /// <summary>Powerful cards from boss encounters.</summary>
    BossLoot,

    // =========================================================================
    // FILTER-BASED POOLS (element + rarity + type combinations)
    // =========================================================================

    /// <summary>Fire element, common rarity, summon type.</summary>
    FireCommonUnits,

    /// <summary>Water element, common rarity, summon type.</summary>
    WaterCommonUnits,

    /// <summary>Wind element, common rarity, summon type.</summary>
    WindCommonUnits,

    /// <summary>Earth element, common rarity, summon type.</summary>
    EarthCommonUnits,

    /// <summary>All spells regardless of element.</summary>
    AllSpells,

    /// <summary>All common rarity cards.</summary>
    AllCommon,

    /// <summary>All rare rarity cards.</summary>
    AllRare,

    // =========================================================================
    // COMPOSITE POOLS (unions of other pools)
    // =========================================================================

    /// <summary>Union of all elemental common units.</summary>
    ElementalStarters,
}
