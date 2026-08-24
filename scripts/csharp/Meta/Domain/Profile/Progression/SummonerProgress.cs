using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Domain.Progression;

namespace Fateforged.Domain.Profile.Progression;

/// <summary>
/// Durable quest and authored-battle progress for one summoner.
/// </summary>
public class SummonerProgress
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

    /// <summary>Quest journal, curriculum capacity, and encounter selections.</summary>
    [JsonPropertyName("quests")]
    public QuestProgress Quests { get; set; } = new();
}
