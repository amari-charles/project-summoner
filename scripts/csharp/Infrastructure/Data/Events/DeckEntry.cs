using Fateforged.Cards;

namespace Fateforged.Data.Events;

/// <summary>
/// Represents a card entry in a deck (player or enemy).
/// </summary>
public class DeckEntry
{
    /// <summary>Card catalog ID</summary>
    public CardId CardId { get; set; } = CardId.None;

    /// <summary>Number of copies of this card</summary>
    public int Count { get; set; } = 1;

    public DeckEntry() { }

    public DeckEntry(CardId cardId, int count = 1)
    {
        CardId = cardId;
        Count = count;
    }

    /// <summary>Constructor accepting string for backwards compatibility.</summary>
    public DeckEntry(string cardId, int count = 1)
    {
        CardId = new CardId(cardId);
        Count = count;
    }
}
