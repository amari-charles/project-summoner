using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Player's battle emotes.
/// </summary>
public class EmotesData
{
    /// <summary>Array of owned emote IDs.</summary>
    [JsonPropertyName("owned")]
    public List<string> Owned { get; set; } = [];

    /// <summary>Equipped emotes in the 4 emote slots.</summary>
    [JsonPropertyName("equipped")]
    public List<string> Equipped { get; set; } = ["", "", "", ""];
}
