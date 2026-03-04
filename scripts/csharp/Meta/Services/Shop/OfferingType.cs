namespace Fateforged.Meta.Shop;

/// <summary>
/// Type of shop offering.
/// </summary>
public enum OfferingType
{
    /// <summary>Single card purchase.</summary>
    Card = 0,

    /// <summary>Multiple cards in a bundle.</summary>
    CardPack = 1,

    /// <summary>Gold or other currency.</summary>
    Currency = 2,

    /// <summary>Special items (legacy - use Cosmetic/Emote instead).</summary>
    Special = 3,

    /// <summary>Unlock a new summoner.</summary>
    Summoner = 4,

    /// <summary>Skins, card backs, UI themes.</summary>
    Cosmetic = 5,

    /// <summary>Battle emotes/reactions.</summary>
    Emote = 6
}
