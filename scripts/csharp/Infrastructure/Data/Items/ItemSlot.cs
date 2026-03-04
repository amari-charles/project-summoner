namespace Fateforged.Data.Items;

/// <summary>
/// Equipment slots for items.
/// Each summoner has 4 equipment slots.
/// </summary>
public enum ItemSlot
{
    /// <summary>Wand slot - typically attack/damage items.</summary>
    Wand,

    /// <summary>First ring slot - typically utility/miscellaneous items.</summary>
    Ring1,

    /// <summary>Second ring slot - typically utility/miscellaneous items.</summary>
    Ring2,

    /// <summary>Robes slot - typically defense/survivability items.</summary>
    Robes
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
