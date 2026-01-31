namespace ProjectSummoner.Data.Events;

/// <summary>
/// Represents a card entry in a deck (player or enemy).
/// </summary>
public class DeckEntry
{
    /// <summary>Card catalog ID</summary>
    public string CardId { get; set; } = "";

    /// <summary>Number of copies of this card</summary>
    public int Count { get; set; } = 1;

    public DeckEntry() { }

    public DeckEntry(string cardId, int count = 1)
    {
        CardId = cardId;
        Count = count;
    }
}
