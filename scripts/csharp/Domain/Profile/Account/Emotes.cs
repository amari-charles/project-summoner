using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Domain.Profile.Account;

/// <summary>
/// Player's battle emotes.
/// </summary>
public class Emotes
{
    /// <summary>Array of owned emote IDs.</summary>
    [JsonPropertyName("owned")]
    public List<EmoteId> Owned { get; set; } = [];

    /// <summary>Equipped emotes in the 4 emote slots.</summary>
    [JsonPropertyName("equipped")]
    public List<EmoteId> Equipped { get; set; } = [EmoteId.None, EmoteId.None, EmoteId.None, EmoteId.None];
}
