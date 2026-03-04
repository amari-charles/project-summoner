using Godot;
using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data;
using Fateforged.Meta.Cards;

namespace Fateforged.Meta;

/// <summary>
/// Level Cap Service - Normalizes card levels and traits for capped battles.
///
/// When a battle has a level cap, cards above the cap are normalized down.
/// Cards below the cap remain at their actual level.
///
/// Level cap logic:
/// - Effective level = min(card.level, cap)
/// - Effective traits = only traits from levels 1 through effective_level
/// - Since level 1 has 0 traits, level N has N-1 traits
/// - So effective traits count = effective_level - 1
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
    /// Get the effective traits for a card under a level cap.
    /// Returns only traits that would have been acquired at or below the cap level.
    /// Since a card at level N has N-1 traits, cap level C means C-1 effective traits.
    /// </summary>
    public List<string> GetEffectiveTraits(List<string> traits, int levelCap)
    {
        if (levelCap <= NoCap || traits == null)
            return traits ?? new List<string>();

        // Number of traits to keep = cap - 1 (since level 1 = 0 traits)
        int maxTraits = levelCap - 1;
        if (maxTraits <= 0)
            return new List<string>();

        if (traits.Count <= maxTraits)
            return new List<string>(traits);

        return traits.GetRange(0, maxTraits);
    }

    /// <summary>
    /// Get the effective traits for a card under a level cap (Godot Array overload).
    /// </summary>
    public List<string> GetEffectiveTraits(Godot.Collections.Array<string> traits, int levelCap)
    {
        if (traits == null)
            return new List<string>();

        // Convert to List and delegate to main implementation
        var traitList = new List<string>();
        foreach (var trait in traits)
            traitList.Add(trait);

        return GetEffectiveTraits(traitList, levelCap);
    }

    /// <summary>
    /// Get the effective traits from a Godot Array (for GDScript interop).
    /// </summary>
    public Godot.Collections.Array<string> GetEffectiveTraitsArray(Godot.Collections.Array traits, int levelCap)
    {
        var result = new Godot.Collections.Array<string>();

        if (levelCap <= NoCap || traits == null)
        {
            if (traits != null)
            {
                foreach (var trait in traits)
                    result.Add(trait.AsString());
            }
            return result;
        }

        int maxTraits = levelCap - 1;
        if (maxTraits <= 0)
            return result;

        int count = 0;
        foreach (var trait in traits)
        {
            if (count >= maxTraits)
                break;
            result.Add(trait.AsString());
            count++;
        }

        return result;
    }

    /// <summary>
    /// Get effective trait stat modifiers with level cap applied.
    /// </summary>
    public Dictionary<string, float> GetCappedTraitModifiers(string cardInstanceId, int levelCap)
    {
        var cardService = CardService.Instance;
        if (cardService == null)
            return new Dictionary<string, float>();

        // Get full modifiers if uncapped
        if (levelCap <= NoCap)
            return cardService.GetTraitStatModifiersTyped(cardInstanceId);

        // Get card data to check level and traits
        var card = cardService.GetCard(cardInstanceId);
        if (card == null)
            return new Dictionary<string, float>();

        // If card is at or below cap, use all traits
        if (card.Level <= levelCap)
            return cardService.GetTraitStatModifiersTyped(cardInstanceId);

        // Calculate capped traits - convert CardTraitId to string for this API
        var traitStrings = card.Traits.ConvertAll(t => t.Value);
        var effectiveTraits = GetEffectiveTraits(traitStrings, levelCap);

        // Compute modifiers manually from capped traits
        return ComputeTraitModifiers(card.CatalogId, effectiveTraits);
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
    /// Compute trait modifiers from a specific set of trait IDs.
    /// This replicates CardService.GetTraitStatModifiers logic for capped traits.
    /// </summary>
    private Dictionary<string, float> ComputeTraitModifiers(string catalogId, List<string> traitIds)
    {
        var modifiers = new Dictionary<string, float>();

        foreach (var traitId in traitIds)
        {
            var trait = CardTraitCatalog.GetTrait(catalogId, traitId);
            if (trait == null)
                continue;

            foreach (var (stat, mult) in trait.StatMods)
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
