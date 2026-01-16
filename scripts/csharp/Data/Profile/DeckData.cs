using System.Collections.Generic;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Represents a player's deck configuration.
/// </summary>
public class DeckData
{
    /// <summary>Unique deck ID (UUID).</summary>
    public required string Id { get; set; }

    /// <summary>Profile ID reference.</summary>
    public string ProfileId { get; set; } = "";

    /// <summary>Summoner ID this deck belongs to.</summary>
    public required string SummonerId { get; set; }

    /// <summary>Display name for the deck.</summary>
    public string Name { get; set; } = "Deck";

    /// <summary>Deck slot index.</summary>
    public int Slot { get; set; }

    /// <summary>Whether this is the active deck for the summoner.</summary>
    public bool IsActive { get; set; }

    /// <summary>Card instance IDs in this deck.</summary>
    public List<string> CardInstanceIds { get; set; } = [];

    /// <summary>Last update timestamp.</summary>
    public long UpdatedAt { get; set; }
}
