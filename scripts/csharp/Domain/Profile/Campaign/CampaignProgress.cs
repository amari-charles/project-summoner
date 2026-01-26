using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Domain.Profile.Campaign;

/// <summary>
/// Campaign progress data (per-summoner or shared).
/// </summary>
public class CampaignProgress
{
    /// <summary>IDs of completed battles.</summary>
    [JsonPropertyName("completed_battles")]
    public List<string> CompletedBattles { get; set; } = [];

    /// <summary>Current battle being played (nullable).</summary>
    [JsonPropertyName("current_battle")]
    public string? CurrentBattle { get; set; }

    /// <summary>Pending reward from last victory (if any).</summary>
    [JsonPropertyName("pending_reward")]
    public Dictionary<string, object>? PendingReward { get; set; }

    /// <summary>Per-story-arc progress tracking.</summary>
    [JsonPropertyName("story_arcs")]
    public Dictionary<string, StoryArcProgress> StoryArcs { get; set; } = [];

    /// <summary>Campaign gold (currency during campaign runs).</summary>
    [JsonPropertyName("gold")]
    public int Gold { get; set; }
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
