using Godot;

namespace ProjectSummoner.Services.Interfaces;

/// <summary>
/// Interface for the PlayerCardService.
/// Enables dependency injection and testing.
/// </summary>
public interface IPlayerCardService
{
    // =========================================================================
    // XP & LEVELING
    // =========================================================================

    /// <summary>
    /// Grant XP to a card instance.
    /// </summary>
    int grant_card_xp(string cardInstanceId, int amount);

    /// <summary>
    /// Grant XP to multiple cards.
    /// </summary>
    Godot.Collections.Dictionary grant_xp_to_cards(Godot.Collections.Array cardInstanceIds, int amount);

    /// <summary>
    /// Get XP required for a level.
    /// </summary>
    int get_xp_for_level(int level);

    /// <summary>
    /// Get XP required for a level with rarity multiplier.
    /// </summary>
    int get_xp_for_level_with_rarity(int level, string rarity);

    /// <summary>
    /// Get gold cost for leveling with rarity multiplier.
    /// </summary>
    int get_gold_cost_for_level_with_rarity(int level, string rarity);

    /// <summary>
    /// Get rarity multiplier.
    /// </summary>
    float get_rarity_multiplier(string rarity);

    /// <summary>
    /// Get XP needed to reach next level.
    /// </summary>
    int get_xp_to_next_level(string cardInstanceId);

    /// <summary>
    /// Get progress towards next level (0.0 - 1.0).
    /// </summary>
    float get_level_progress(string cardInstanceId);

    /// <summary>
    /// Check if card can level up.
    /// </summary>
    bool can_level_up(string cardInstanceId);

    /// <summary>
    /// Get gold cost for next level up.
    /// </summary>
    int get_level_up_gold_cost(string cardInstanceId);

    /// <summary>
    /// Check if player can afford level up.
    /// </summary>
    bool can_afford_level_up(string cardInstanceId);

    /// <summary>
    /// Level up a card with the chosen upgrade.
    /// </summary>
    bool level_up_card(string cardInstanceId, string upgradeId);

    // =========================================================================
    // UPGRADES
    // =========================================================================

    /// <summary>
    /// Get available upgrades for a card.
    /// </summary>
    Godot.Collections.Array get_available_upgrades(string cardInstanceId);

    /// <summary>
    /// Get upgrades already applied to a card.
    /// </summary>
    Godot.Collections.Array get_applied_upgrades(string cardInstanceId);

    /// <summary>
    /// Get combined stat modifiers from all applied upgrades.
    /// </summary>
    Godot.Collections.Dictionary get_upgrade_stat_modifiers(string cardInstanceId);

    // =========================================================================
    // STATS
    // =========================================================================

    /// <summary>
    /// Get effective stats for a card (base + upgrades).
    /// </summary>
    Godot.Collections.Dictionary get_effective_stats(string cardInstanceId);

    /// <summary>
    /// Get card progression info for UI display.
    /// </summary>
    Godot.Collections.Dictionary get_card_progression_info(string cardInstanceId);

    /// <summary>
    /// Get all cards ready to level up.
    /// </summary>
    Godot.Collections.Array<string> get_cards_ready_to_level_up();
}
