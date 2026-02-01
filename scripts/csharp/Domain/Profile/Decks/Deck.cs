using System.Collections.Generic;
using System.Text.Json.Serialization;
using ProjectSummoner.Cards;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Services.Deck;

namespace ProjectSummoner.Domain.Profile.Decks;

/// <summary>
/// Represents a player's deck configuration.
/// </summary>
public class Deck
{
    /// <summary>Unique deck ID (UUID).</summary>
    [JsonPropertyName("id")]
    public required DeckId Id { get; set; }

    /// <summary>Profile ID reference.</summary>
    [JsonPropertyName("profile_id")]
    public ProfileId ProfileId { get; set; } = Profile.ProfileId.None;

    /// <summary>Summoner ID this deck belongs to.</summary>
    [JsonPropertyName("summoner_id")]
    public required SummonerId SummonerId { get; set; }

    /// <summary>User-defined deck name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Deck";

    /// <summary>Deck slot index (0-based).</summary>
    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    /// <summary>Whether this is the active/selected deck.</summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    /// <summary>List of card instance IDs in this deck.</summary>
    [JsonPropertyName("card_instance_ids")]
    public List<CardInstanceId> CardInstanceIds { get; set; } = [];

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }
}
