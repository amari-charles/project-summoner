using System.Collections.Generic;
using ProjectSummoner.Cards;
using ProjectSummoner.Data;
using ProjectSummoner.Services;

namespace ProjectSummoner.Services.Interfaces;

/// <summary>
/// Interface for the PlayerCardService.
/// Enables dependency injection and testing.
/// </summary>
public interface IPlayerCardService
{
    // =========================================================================
    // CARD ACCESS
    // =========================================================================

    /// <summary>
    /// Get a card by instance ID.
    /// </summary>
    PlayerCardInstance? GetCard(string instanceId);

    /// <summary>
    /// Get all player cards.
    /// </summary>
    List<PlayerCardInstance> GetAllCards();

    // =========================================================================
    // XP OPERATIONS
    // =========================================================================

    /// <summary>
    /// Grant XP to a card instance.
    /// </summary>
    int GrantXp(string cardInstanceId, int amount);

    /// <summary>
    /// Grant XP to multiple cards.
    /// </summary>
    Dictionary<string, int> GrantXpToCards(IEnumerable<string> cardInstanceIds, int amount);

    /// <summary>
    /// Get XP required for a level.
    /// </summary>
    int GetXpForLevel(int level);

    /// <summary>
    /// Get XP required for a level with rarity multiplier.
    /// </summary>
    int GetXpForLevelWithRarity(int level, string rarity);

    /// <summary>
    /// Get gold cost for leveling with rarity multiplier.
    /// </summary>
    int GetGoldCostForLevelWithRarity(int level, string rarity);

    /// <summary>
    /// Get rarity multiplier.
    /// </summary>
    float GetRarityMultiplier(string rarity);

    /// <summary>
    /// Get XP needed to reach next level.
    /// </summary>
    int GetXpToNextLevel(string cardInstanceId);

    /// <summary>
    /// Get progress towards next level (0.0 - 1.0).
    /// </summary>
    float GetLevelProgress(string cardInstanceId);

    // =========================================================================
    // LEVEL-UP OPERATIONS
    // =========================================================================

    /// <summary>
    /// Check if card can level up.
    /// </summary>
    bool CanLevelUp(string cardInstanceId);

    /// <summary>
    /// Get gold cost for next level up.
    /// </summary>
    int GetLevelUpGoldCost(string cardInstanceId);

    /// <summary>
    /// Check if player can afford level up.
    /// </summary>
    bool CanAffordLevelUp(string cardInstanceId);

    /// <summary>
    /// Level up a card with the chosen upgrade.
    /// </summary>
    bool LevelUpCard(string cardInstanceId, string upgradeId);

    // =========================================================================
    // UPGRADES
    // =========================================================================

    /// <summary>
    /// Get available upgrades for a card.
    /// </summary>
    List<CardUpgrade> GetAvailableUpgrades(string cardInstanceId);

    /// <summary>
    /// Get upgrades already applied to a card.
    /// </summary>
    List<string> GetAppliedUpgrades(string cardInstanceId);

    /// <summary>
    /// Get combined stat modifiers from all applied upgrades.
    /// </summary>
    Dictionary<string, float> GetUpgradeStatModifiers(string cardInstanceId);

    // =========================================================================
    // EFFECTIVE STATS
    // =========================================================================

    /// <summary>
    /// Get effective stats for a card (base + upgrades).
    /// </summary>
    Dictionary<string, float> GetEffectiveStats(string cardInstanceId);

    // =========================================================================
    // QUERY HELPERS
    // =========================================================================

    /// <summary>
    /// Get card progression info for UI display.
    /// </summary>
    CardProgressionInfo? GetCardProgressionInfo(string cardInstanceId);

    /// <summary>
    /// Get all cards ready to level up.
    /// </summary>
    List<PlayerCardInstance> GetCardsReadyToLevelUp();
}
