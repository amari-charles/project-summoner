using System.Collections.Generic;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Campaign progress data for a specific summoner.
/// </summary>
public class CampaignProgressData
{
    /// <summary>Array of completed battle IDs.</summary>
    public List<string> CompletedBattles { get; set; } = [];

    /// <summary>Current battle ID (if in progress).</summary>
    public string? CurrentBattle { get; set; }

    /// <summary>Pending reward from last victory (if any).</summary>
    public Dictionary<string, object>? PendingReward { get; set; }

    /// <summary>Per-story-arc progress tracking.</summary>
    public Dictionary<string, StoryArcProgress> StoryArcs { get; set; } = [];
}

/// <summary>
/// Progress within a specific story arc.
/// </summary>
public class StoryArcProgress
{
    /// <summary>Array of completed event/battle IDs in this arc.</summary>
    public List<string> CompletedEvents { get; set; } = [];

    /// <summary>Current event ID (if in progress).</summary>
    public string? CurrentEvent { get; set; }

    /// <summary>Arc-specific flags/state.</summary>
    public Dictionary<string, object> Flags { get; set; } = [];
}
