using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Data.Events;
using Fateforged.Domain.Progression;
using Fateforged.Meta.Campaign;

namespace Fateforged.Domain.Profile.Campaign;

/// <summary>
/// Campaign progress data (per-summoner or shared).
/// </summary>
public class CampaignProgress
{
    /// <summary>IDs of completed battles.</summary>
    [JsonPropertyName("completed_battles")]
    public List<BattleId> CompletedBattles { get; set; } = [];

    /// <summary>Authority-created battle occurrence currently in progress.</summary>
    [JsonPropertyName("active_battle_attempt")]
    public BattleAttempt? ActiveBattleAttempt { get; set; }

    /// <summary>Terminal attempt receipts keyed by BattleAttemptId.</summary>
    [JsonPropertyName("battle_attempt_completions")]
    public Dictionary<string, BattleAttemptCompletion> BattleAttemptCompletions { get; set; } = [];

    /// <summary>Purchased offering IDs for the active campaign run.</summary>
    [JsonPropertyName("caravan_purchases")]
    public List<string> CaravanPurchases { get; set; } = [];

    /// <summary>Per-story-arc progress tracking.</summary>
    [JsonPropertyName("story_arcs")]
    public Dictionary<string, StoryArcProgress> StoryArcs { get; set; } = [];

    /// <summary>Campaign gold (currency during campaign runs).</summary>
    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    /// <summary>Academy curriculum progress for the new campaign model.</summary>
    [JsonPropertyName("academy")]
    public AcademyProgress Academy { get; set; } = new();

    /// <summary>Generic quest state shared by every quest context.</summary>
    [JsonPropertyName("quests")]
    public QuestProgress Quests { get; set; } = new();

    /// <summary>Choices made at choice nodes (node_id -> choice_id).</summary>
    [JsonPropertyName("choices")]
    public Dictionary<NodeId, ChoiceId> Choices { get; set; } = [];
}

/// <summary>
/// Progress within a specific story arc.
/// </summary>
public class StoryArcProgress
{
    /// <summary>Array of completed event/battle IDs in this arc.</summary>
    [JsonPropertyName("completed_events")]
    public List<EventId> CompletedEvents { get; set; } = [];

    /// <summary>Current event ID (if in progress).</summary>
    [JsonPropertyName("current_event")]
    public EventId? CurrentEvent { get; set; }

    /// <summary>Arc-specific flags/state.</summary>
    [JsonPropertyName("flags")]
    public Dictionary<string, object> Flags { get; set; } = [];
}
