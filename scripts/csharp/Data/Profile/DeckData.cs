using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Represents a player's deck configuration.
/// </summary>
public class DeckData
{
    /// <summary>Unique deck ID (UUID).</summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>Profile ID reference.</summary>
    [JsonPropertyName("profile_id")]
    public string ProfileId { get; set; } = "";

    /// <summary>Summoner ID this deck belongs to.</summary>
    [JsonPropertyName("summoner_id")]
    public required string SummonerId { get; set; }

    /// <summary>Display name for the deck.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Deck";

    /// <summary>Deck slot index.</summary>
    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    /// <summary>Whether this is the active deck for the summoner.</summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    /// <summary>Card instance IDs in this deck.</summary>
    [JsonPropertyName("card_instance_ids")]
    public List<string> CardInstanceIds { get; set; } = [];

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }
}
