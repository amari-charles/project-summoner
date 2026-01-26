namespace ProjectSummoner.Domain.Profile.Inventory;

/// <summary>
/// Equipment slots for items.
/// Each summoner has 4 equipment slots.
/// </summary>
public enum ItemSlot
{
    /// <summary>Weapon slot - typically attack/damage items.</summary>
    Weapon,

    /// <summary>First ring slot - typically utility/miscellaneous items.</summary>
    Ring1,

    /// <summary>Second ring slot - typically utility/miscellaneous items.</summary>
    Ring2,

    /// <summary>Vestments slot - typically defense/survivability items.</summary>
    Vestments
}
