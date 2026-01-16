using System.Collections.Generic;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Player's battle emotes.
/// </summary>
public class EmotesData
{
    /// <summary>Array of owned emote IDs.</summary>
    public List<string> Owned { get; set; } = [];

    /// <summary>Equipped emotes in the 4 emote slots.</summary>
    public List<string> Equipped { get; set; } = ["", "", "", ""];
}
