using Godot;
using System.Collections.Generic;
using ProjectSummoner.Cards;
using ProjectSummoner.Data;

namespace ProjectSummoner.Services;

/// <summary>
/// Level Cap Service - Normalizes card levels and upgrades for capped battles.
///
/// When a battle has a level cap, cards above the cap are normalized down.
/// Cards below the cap remain at their actual level.
///
/// Level cap logic:
/// - Effective level = min(card.level, cap)
/// - Effective upgrades = only upgrades from levels 1 through effective_level
/// - Since level 1 has 0 upgrades, level N has N-1 upgrades
/// - So effective upgrades count = effective_level - 1
/// </summary>
[GlobalClass]
public partial class LevelCapService : Node
{
    public static LevelCapService? Instance { get; private set; }

    /// <summary>No level cap (uncapped battle).</summary>
    public const int NoCap = 0;

    // =============================================================================
    // LIFECYCLE
    // =============================================================================

    public override void _Ready()
    {
        Instance = this;
        GD.Print("LevelCapService: Ready");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // =============================================================================
    // PUBLIC API
    // =============================================================================

    /// <summary>
    /// Get the effective level for a card under a level cap.
    /// Returns the card's actual level if uncapped or below cap.
    /// Returns the cap if card level exceeds it.
    /// </summary>
    public int GetEffectiveLevel(int cardLevel, int levelCap)
    {
        if (levelCap <= NoCap)
            return cardLevel;
        return Mathf.Min(cardLevel, levelCap);
    }

    /// <summary>
    /// Get the effective upgrades for a card under a level cap.
    /// Returns only upgrades that would have been acquired at or below the cap level.
    /// Since a card at level N has N-1 upgrades, cap level C means C-1 effective upgrades.
    /// </summary>
    public List<string> GetEffectiveUpgrades(List<string> upgrades, int levelCap)
    {
        if (levelCap <= NoCap || upgrades == null)
            return upgrades ?? new List<string>();

        // Number of upgrades to keep = cap - 1 (since level 1 = 0 upgrades)
        int maxUpgrades = levelCap - 1;
        if (maxUpgrades <= 0)
            return new List<string>();

        if (upgrades.Count <= maxUpgrades)
            return new List<string>(upgrades);

        return upgrades.GetRange(0, maxUpgrades);
    }

    /// <summary>
    /// Get the effective upgrades for a card under a level cap (Godot Array overload).
    /// </summary>
    public List<string> GetEffectiveUpgrades(Godot.Collections.Array<string> upgrades, int levelCap)
    {
        if (upgrades == null)
            return new List<string>();

        // Convert to List and delegate to main implementation
        var upgradeList = new List<string>();
        foreach (var upgrade in upgrades)
            upgradeList.Add(upgrade);

        return GetEffectiveUpgrades(upgradeList, levelCap);
    }

    /// <summary>
    /// Get the effective upgrades from a Godot Array (for GDScript interop).
    /// </summary>
    public Godot.Collections.Array<string> GetEffectiveUpgradesArray(Godot.Collections.Array upgrades, int levelCap)
    {
        var result = new Godot.Collections.Array<string>();

        if (levelCap <= NoCap || upgrades == null)
        {
            if (upgrades != null)
            {
                foreach (var upgrade in upgrades)
                    result.Add(upgrade.AsString());
            }
            return result;
        }

        int maxUpgrades = levelCap - 1;
        if (maxUpgrades <= 0)
            return result;

        int count = 0;
        foreach (var upgrade in upgrades)
        {
            if (count >= maxUpgrades)
                break;
            result.Add(upgrade.AsString());
            count++;
        }

        return result;
    }

    /// <summary>
    /// Get effective upgrade stat modifiers with level cap applied.
    /// </summary>
    public Dictionary<string, float> GetCappedUpgradeModifiers(string cardInstanceId, int levelCap)
    {
        var cardService = PlayerCardService.Instance;
        if (cardService == null)
            return new Dictionary<string, float>();

        // Get full modifiers if uncapped
        if (levelCap <= NoCap)
            return cardService.GetUpgradeStatModifiers(cardInstanceId);

        // Get card data to check level and upgrades
        var card = cardService.GetCard(cardInstanceId);
        if (card == null)
            return new Dictionary<string, float>();

        // If card is at or below cap, use all upgrades
        if (card.Level <= levelCap)
            return cardService.GetUpgradeStatModifiers(cardInstanceId);

        // Calculate capped upgrades
        var effectiveUpgrades = GetEffectiveUpgrades(card.Upgrades, levelCap);

        // Compute modifiers manually from capped upgrades
        return ComputeUpgradeModifiers(card.CatalogId, effectiveUpgrades);
    }

    /// <summary>
    /// Check if a battle configuration has a level cap.
    /// </summary>
    public bool HasLevelCap(Godot.Collections.Dictionary battleConfig)
    {
        return GetLevelCap(battleConfig) > NoCap;
    }

    /// <summary>
    /// Get level cap from battle configuration.
    /// Returns NoCap (0) if uncapped.
    /// </summary>
    public int GetLevelCap(Godot.Collections.Dictionary battleConfig)
    {
        if (battleConfig == null)
            return NoCap;

        if (battleConfig.TryGetValue("level_cap", out var capVar))
        {
            return capVar.VariantType == Variant.Type.Int ? capVar.AsInt32() : NoCap;
        }
        return NoCap;
    }

    /// <summary>
    /// Get path type from battle configuration.
    /// Returns "standard" if not specified.
    /// </summary>
    public string GetPathType(Godot.Collections.Dictionary battleConfig)
    {
        if (battleConfig == null)
            return "standard";

        if (battleConfig.TryGetValue("path_type", out var pathVar))
        {
            return pathVar.VariantType == Variant.Type.String ? pathVar.AsString() : "standard";
        }
        return "standard";
    }

    /// <summary>
    /// Get recommended level from battle configuration.
    /// Returns 0 if not specified.
    /// </summary>
    public int GetRecommendedLevel(Godot.Collections.Dictionary battleConfig)
    {
        if (battleConfig == null)
            return 0;

        if (battleConfig.TryGetValue("recommended_level", out var levelVar))
        {
            return levelVar.VariantType == Variant.Type.Int ? levelVar.AsInt32() : 0;
        }
        return 0;
    }

    // =============================================================================
    // INTERNAL HELPERS
    // =============================================================================

    /// <summary>
    /// Compute upgrade modifiers from a specific set of upgrade IDs.
    /// This replicates PlayerCardService.GetUpgradeStatModifiers logic for capped upgrades.
    /// </summary>
    private Dictionary<string, float> ComputeUpgradeModifiers(string catalogId, List<string> upgradeIds)
    {
        var modifiers = new Dictionary<string, float>();

        foreach (var upgradeId in upgradeIds)
        {
            var upgrade = CardUpgradeCatalog.GetUpgrade(catalogId, upgradeId);
            if (upgrade == null)
                continue;

            foreach (var (stat, mult) in upgrade.StatMods)
            {
                if (modifiers.ContainsKey(stat))
                    modifiers[stat] *= mult;
                else
                    modifiers[stat] = mult;
            }
        }

        return modifiers;
    }
}
