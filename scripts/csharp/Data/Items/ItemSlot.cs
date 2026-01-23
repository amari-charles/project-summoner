namespace ProjectSummoner.Data.Items;

/// <summary>
/// Equipment slots for items.
/// Each summoner has 4 equipment slots.
/// </summary>
public enum ItemSlot
{
    /// <summary>Grimoire slot - typically magic/spell enhancement items.</summary>
    Grimoire,

    /// <summary>Weapon slot - typically attack/damage items.</summary>
    Weapon,

    /// <summary>Ring slot - typically utility/miscellaneous items.</summary>
    Ring,

    /// <summary>Vestments slot - typically defense/survivability items.</summary>
    Vestments
}

/// <summary>
/// Item binding determines who can use an item.
/// </summary>
public enum ItemBinding
{
    /// <summary>Item is bound to a specific summoner and cannot be shared.</summary>
    SummonerBound,

    /// <summary>Item can be equipped by any summoner on the account.</summary>
    AccountWide
}
