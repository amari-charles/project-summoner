using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Campaign progress data for a specific summoner.
/// </summary>
public class CampaignProgressData
{
    /// <summary>Array of completed battle IDs.</summary>
    [JsonPropertyName("completed_battles")]
    public List<string> CompletedBattles { get; set; } = [];

    /// <summary>Current battle ID (if in progress).</summary>
    [JsonPropertyName("current_battle")]
    public string? CurrentBattle { get; set; }

    /// <summary>Pending reward from last victory (if any).</summary>
    [JsonPropertyName("pending_reward")]
    public Dictionary<string, object>? PendingReward { get; set; }

    /// <summary>Per-story-arc progress tracking.</summary>
    [JsonPropertyName("story_arcs")]
    public Dictionary<string, StoryArcProgress> StoryArcs { get; set; } = [];

    /// <summary>
    /// Campaign-scoped gold. This gold is only valid within the current campaign
    /// and is lost when the campaign ends (win or lose).
    /// </summary>
    [JsonPropertyName("gold")]
    public int Gold { get; set; } = 0;
}

/// <summary>
/// Progress within a specific story arc.
/// </summary>
public class StoryArcProgress
{
    /// <summary>Array of completed event/battle IDs in this arc.</summary>
    [JsonPropertyName("completed_events")]
    public List<string> CompletedEvents { get; set; } = [];

    /// <summary>Current event ID (if in progress).</summary>
    [JsonPropertyName("current_event")]
    public string? CurrentEvent { get; set; }

    /// <summary>Arc-specific flags/state.</summary>
    [JsonPropertyName("flags")]
    public Dictionary<string, object> Flags { get; set; } = [];
}
