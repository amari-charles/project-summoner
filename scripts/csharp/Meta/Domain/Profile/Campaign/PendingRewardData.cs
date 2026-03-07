using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fateforged.Domain.Profile.Campaign;

/// <summary>
/// Typed pending reward from a battle victory.
/// Replaces the former Dictionary&lt;string, object&gt; on CampaignProgress.
/// </summary>
public class PendingRewardData
{
    [JsonPropertyName("battle_id")]
    public string BattleId { get; set; } = "";

    [JsonPropertyName("reward_type")]
    public string RewardType { get; set; } = "fixed";

    [JsonPropertyName("choice_index")]
    public int ChoiceIndex { get; set; } = -1;

    [JsonPropertyName("caravan_purchases")]
    public List<string> CaravanPurchases { get; set; } = [];
}
