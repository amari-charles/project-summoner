using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Cards;
using Fateforged.Data.Events;

namespace Fateforged.Meta.Rewards;

/// <summary>
/// Typed specification for battle rewards.
/// Replaces the Dictionary-based get_reward_spec() return value.
/// </summary>
public class BattleRewardSpec
{
    /// <summary>Whether this battle has already been completed (replay).</summary>
    public bool IsReplay { get; set; }

    /// <summary>Reward type (none, fixed, flexible).</summary>
    public Data.Events.RewardType Type { get; set; } = Data.Events.RewardType.Fixed;

    /// <summary>Gold reward amount.</summary>
    public int GoldReward { get; set; }

    /// <summary>Summoner XP reward amount.</summary>
    public int SummonerXp { get; set; }

    /// <summary>Card XP reward amount.</summary>
    public int CardXp { get; set; }

    /// <summary>
    /// Card options available for this reward.
    /// Unified format: both fixed rewards (predetermined cards) and flexible rewards
    /// (player selection) use the same CardRewardOption structure for consistent UI handling.
    /// </summary>
    public List<CardRewardOption> CardOptions { get; set; } = new();

    /// <summary>Whether player must select from options.</summary>
    public bool RequiresChoice { get; set; }

    /// <summary>Index of previously chosen option (-1 if not chosen).</summary>
    public int ChosenIndex { get; set; } = -1;

    // =========================================================================
    // FACTORY METHODS
    // =========================================================================

    /// <summary>
    /// Create a reward spec from a battle ID using EventCatalog.
    /// </summary>
    /// <param name="battleId">Battle ID to get spec for.</param>
    /// <param name="isCompleted">Whether the battle has been completed (replay).</param>
    /// <param name="chosenIndex">Previously chosen option index (-1 if not chosen).</param>
    /// <param name="ownedCatalogIds">Set of card catalog IDs the player already owns (for filtering).</param>
    public static BattleRewardSpec FromBattleId(string battleId, bool isCompleted = false, int chosenIndex = -1, HashSet<string>? ownedCatalogIds = null)
    {
        var spec = new BattleRewardSpec
        {
            IsReplay = isCompleted,
            ChosenIndex = chosenIndex
        };

        var battle = EventCatalog.GetEvent<BattleEventDefinition>(new EventId(battleId));
        if (battle == null)
        {
            GD.PushWarning($"BattleRewardSpec: Battle not found: {battleId}");
            return spec;
        }

        var rewards = battle.Rewards;
        spec.Type = rewards.Type;
        spec.GoldReward = rewards.GoldReward;
        spec.SummonerXp = rewards.SummonerXpReward;
        spec.CardXp = rewards.CardXpReward;

        // Build card options based on reward type
        switch (rewards.Type)
        {
            case Data.Events.RewardType.Fixed:
                spec.CardOptions = BuildFixedOptions(rewards.FixedCards);
                spec.RequiresChoice = false;
                break;

            case Data.Events.RewardType.Flexible:
                // Filter out owned cards if ExcludeOwned is set
                var cardOptionStrings = rewards.CardOptions.Select(id => (string)id).ToList();
                if (rewards.ExcludeOwned && ownedCatalogIds != null && ownedCatalogIds.Count > 0)
                {
                    var filtered = cardOptionStrings.Where(id => !ownedCatalogIds.Contains(id)).ToList();
                    // Only use filtered list if it still has options; otherwise show all (player owns everything)
                    if (filtered.Count > 0)
                    {
                        cardOptionStrings = filtered;
                    }
                }
                spec.CardOptions = BuildFlexibleOptions(cardOptionStrings);
                spec.RequiresChoice = rewards.PlayerSelects && spec.CardOptions.Count > 1;
                break;

            case Data.Events.RewardType.None:
                spec.CardOptions = new List<CardRewardOption>();
                spec.RequiresChoice = false;
                break;
        }

        return spec;
    }

    /// <summary>
    /// Build card options from fixed reward entries.
    /// </summary>
    private static List<CardRewardOption> BuildFixedOptions(List<FixedRewardEntry> fixedCards)
    {
        var options = new List<CardRewardOption>();

        foreach (var entry in fixedCards)
        {
            var cardDef = CardCatalog.GetCard(entry.CardId);
            options.Add(new CardRewardOption
            {
                CatalogId = entry.CardId,
                Rarity = entry.Rarity,
                Count = entry.Count,
                DisplayName = cardDef?.Name ?? entry.CardId
            });
        }

        return options;
    }

    /// <summary>
    /// Build card options from flexible reward card IDs.
    /// </summary>
    private static List<CardRewardOption> BuildFlexibleOptions(List<string> cardIds)
    {
        var options = new List<CardRewardOption>();

        foreach (var cardId in cardIds)
        {
            var cardDef = CardCatalog.GetCard(cardId);
            var rarity = cardDef?.Rarity.ToString().ToLowerInvariant() ?? "common";

            options.Add(new CardRewardOption
            {
                CatalogId = cardId,
                Rarity = rarity,
                Count = 1,
                DisplayName = cardDef?.Name ?? cardId
            });
        }

        return options;
    }

    // =========================================================================
    // GDSCRIPT BRIDGE
    // =========================================================================

    /// <summary>
    /// Convert to Godot Dictionary for GDScript interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        var cardOptionsArray = new Godot.Collections.Array();
        foreach (var option in CardOptions)
        {
            cardOptionsArray.Add(option.ToDictionary());
        }

        return new Godot.Collections.Dictionary
        {
            ["is_replay"] = IsReplay,
            ["reward_type"] = Type.ToStringId(),
            ["gold_reward"] = GoldReward,
            ["summoner_xp"] = SummonerXp,
            ["card_xp"] = CardXp,
            ["card_options"] = cardOptionsArray,
            ["requires_choice"] = RequiresChoice,
            ["chosen_index"] = ChosenIndex
        };
    }
}

/// <summary>
/// A card reward option with known structure.
/// Replaces the "array of unknown dicts" pattern.
/// </summary>
public class CardRewardOption
{
    /// <summary>Card catalog ID.</summary>
    public string CatalogId { get; set; } = "";

    /// <summary>
    /// Card rarity as lowercase string (common, rare, epic, legendary).
    /// Uses string instead of Rarity enum for GDScript interop - GDScript receives
    /// this via ToDictionary() and expects lowercase string values for UI styling.
    /// </summary>
    public string Rarity { get; set; } = "common";

    /// <summary>Number of copies to grant.</summary>
    public int Count { get; set; } = 1;

    /// <summary>Display name for UI.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Convert to Godot Dictionary for GDScript interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        return new Godot.Collections.Dictionary
        {
            ["catalog_id"] = CatalogId,
            ["rarity"] = Rarity,
            ["count"] = Count,
            ["display_name"] = DisplayName
        };
    }
}
