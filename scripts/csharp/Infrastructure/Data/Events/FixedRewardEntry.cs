using Fateforged.Cards;

namespace Fateforged.Data.Events;

/// <summary>
/// Represents a fixed reward card entry with specific rarity.
/// </summary>
public class FixedRewardEntry
{
    /// <summary>Card catalog ID</summary>
    public CardId CardId { get; set; } = Cards.CardId.None;

    /// <summary>Rarity of the reward card (common, rare, epic, legendary)</summary>
    public string Rarity { get; set; } = "common";

    /// <summary>Number of copies to grant</summary>
    public int Count { get; set; } = 1;

    public FixedRewardEntry() { }

    public FixedRewardEntry(CardId cardId, string rarity = "common", int count = 1)
    {
        CardId = cardId;
        Rarity = rarity;
        Count = count;
    }
}
