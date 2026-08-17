using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fateforged.Domain.Profile.Campaign;

public sealed class QuestProgress
{
    [JsonPropertyName("discovered_quest_ids")]
    public List<string> DiscoveredQuestIds { get; set; } = [];

    [JsonPropertyName("active_quest_ids")]
    public List<string> ActiveQuestIds { get; set; } = [];

    [JsonPropertyName("completed_quest_ids")]
    public List<string> CompletedQuestIds { get; set; } = [];

    [JsonPropertyName("current_step_by_quest_id")]
    public Dictionary<string, int> CurrentStepByQuestId { get; set; } = [];

    [JsonPropertyName("tracked_quest_id")]
    public string TrackedQuestId { get; set; } = "";
}
