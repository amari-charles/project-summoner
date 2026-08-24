using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Cards;

namespace Fateforged.Domain.Profile.Progression;

public sealed class QuestProgress
{
    [JsonPropertyName("curriculum_capacity")]
    public int CurriculumCapacity { get; set; } = 3;

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

    [JsonPropertyName("encounter_loadouts")]
    public Dictionary<string, EncounterLoadoutState> EncounterLoadouts { get; set; } = [];
}

public sealed class EncounterLoadoutState
{
    [JsonPropertyName("selected_card_instance_ids")]
    public List<CardInstanceId> SelectedCardInstanceIds { get; set; } = [];
}
