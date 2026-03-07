using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Data.Events;
using Fateforged.Meta.Campaign;

namespace Fateforged.Domain.Profile.Campaign;

/// <summary>
/// Typed pending reward from a battle victory.
/// Replaces the former Dictionary&lt;string, object&gt; on CampaignProgress.
/// </summary>
public class PendingRewardData
{
    [JsonPropertyName("battle_id")]
    public BattleId BattleId { get; set; }

    [JsonPropertyName("reward_type")]
    public RewardType RewardType { get; set; } = RewardType.Fixed;

    [JsonPropertyName("choice_index")]
    public int ChoiceIndex { get; set; } = -1;

    [JsonPropertyName("caravan_purchases")]
    public List<string> CaravanPurchases { get; set; } = [];
}
