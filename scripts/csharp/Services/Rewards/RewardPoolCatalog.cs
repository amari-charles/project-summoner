using System.Collections.Generic;
using System.Linq;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;

namespace ProjectSummoner.Services.Rewards;

/// <summary>
/// Defines reward pools used for flexible reward generation.
/// Pools filter cards by element, rarity, type, and tags.
/// </summary>
public static class RewardPoolCatalog
{
    // =========================================================================
    // POOL DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, RewardPoolDefinition> _pools = new()
    {
        // Standard pool: All unlockable cards (no dev-only cards)
        ["standard_cards"] = new RewardPoolDefinition
        {
            PoolId = "standard_cards",
            CardFilters = new CardFilterConfig
            {
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Common cards only
        ["common_cards"] = new RewardPoolDefinition
        {
            PoolId = "common_cards",
            CardFilters = new CardFilterConfig
            {
                Rarities = [Rarity.Common],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Rare+ cards
        ["rare_plus_cards"] = new RewardPoolDefinition
        {
            PoolId = "rare_plus_cards",
            CardFilters = new CardFilterConfig
            {
                Rarities = [Rarity.Rare, Rarity.Epic, Rarity.Legendary],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Fire element cards
        ["fire_cards"] = new RewardPoolDefinition
        {
            PoolId = "fire_cards",
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Fire],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Water element cards
        ["water_cards"] = new RewardPoolDefinition
        {
            PoolId = "water_cards",
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Water],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Wind element cards
        ["wind_cards"] = new RewardPoolDefinition
        {
            PoolId = "wind_cards",
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Wind],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Earth element cards
        ["earth_cards"] = new RewardPoolDefinition
        {
            PoolId = "earth_cards",
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Earth],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Neutral element cards
        ["neutral_cards"] = new RewardPoolDefinition
        {
            PoolId = "neutral_cards",
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Neutral],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Summon cards only
        ["summon_cards"] = new RewardPoolDefinition
        {
            PoolId = "summon_cards",
            CardFilters = new CardFilterConfig
            {
                CardTypes = [CardType.Summon],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        },

        // Spell cards only
        ["spell_cards"] = new RewardPoolDefinition
        {
            PoolId = "spell_cards",
            CardFilters = new CardFilterConfig
            {
                CardTypes = [CardType.Spell],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            },
            IncludeItems = false
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a pool definition by ID. Returns null if not found.</summary>
    public static RewardPoolDefinition? GetPool(string poolId)
    {
        return _pools.GetValueOrDefault(poolId);
    }

    /// <summary>Check if a pool exists.</summary>
    public static bool HasPool(string poolId)
    {
        return _pools.ContainsKey(poolId);
    }

    /// <summary>Get all pool IDs.</summary>
    public static string[] GetAllPoolIds()
    {
        return [.. _pools.Keys];
    }

    // =========================================================================
    // CARD FILTERING
    // =========================================================================

    /// <summary>
    /// Get cards matching a pool's filters.
    /// </summary>
    /// <param name="poolId">Pool ID to filter by.</param>
    /// <param name="excludeCatalogIds">Optional: catalog IDs to exclude (e.g., owned cards).</param>
    /// <returns>Matching card definitions.</returns>
    public static CardDefinition[] GetCardsForPool(string poolId, HashSet<string>? excludeCatalogIds = null)
    {
        var pool = GetPool(poolId);
        if (pool == null)
            return [];

        return FilterCards(pool.CardFilters, excludeCatalogIds);
    }

    /// <summary>
    /// Get cards matching specific filter criteria.
    /// </summary>
    public static CardDefinition[] FilterCards(CardFilterConfig filters, HashSet<string>? excludeCatalogIds = null)
    {
        var allCards = CardCatalog.GetAllCards();

        return allCards.Where(card =>
        {
            // Exclude by catalog ID (owned cards)
            if (excludeCatalogIds != null && excludeCatalogIds.Contains(card.Id))
                return false;

            // Filter by elements
            if (filters.Elements.Count > 0 && !filters.Elements.Contains(card.ElementalAffinity))
                return false;

            // Filter by rarities
            if (filters.Rarities.Count > 0 && !filters.Rarities.Contains(card.Rarity))
                return false;

            // Filter by card types
            if (filters.CardTypes.Count > 0 && !filters.CardTypes.Contains(card.Type))
                return false;

            // Filter by tags (any match)
            if (filters.Tags.Count > 0 && !filters.Tags.Any(tag => card.Tags.Contains(tag)))
                return false;

            // Exclude by unlock conditions
            if (filters.ExcludeUnlockConditions.Contains(card.UnlockCondition))
                return false;

            return true;
        }).ToArray();
    }

    /// <summary>
    /// Get pool ID for a specific element.
    /// </summary>
    public static string GetPoolIdForElement(Element element)
    {
        return element switch
        {
            Element.Fire => "fire_cards",
            Element.Water => "water_cards",
            Element.Wind => "wind_cards",
            Element.Earth => "earth_cards",
            Element.Neutral => "neutral_cards",
            _ => "standard_cards"
        };
    }
}

/// <summary>
/// Definition of a reward pool.
/// </summary>
public class RewardPoolDefinition
{
    /// <summary>Unique pool identifier.</summary>
    public required string PoolId { get; init; }

    /// <summary>Card filter configuration.</summary>
    public CardFilterConfig CardFilters { get; init; } = new();

    /// <summary>Whether this pool can include items (Phase 4).</summary>
    public bool IncludeItems { get; init; }
}

/// <summary>
/// Configuration for filtering cards.
/// </summary>
public class CardFilterConfig
{
    /// <summary>Filter to specific elements (empty = all elements).</summary>
    public List<Element> Elements { get; init; } = [];

    /// <summary>Filter to specific rarities (empty = all rarities).</summary>
    public List<Rarity> Rarities { get; init; } = [];

    /// <summary>Filter to specific card types (empty = all types).</summary>
    public List<CardType> CardTypes { get; init; } = [];

    /// <summary>Filter to cards with any of these tags (empty = no tag filter).</summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>Exclude cards with these unlock conditions.</summary>
    public List<UnlockCondition> ExcludeUnlockConditions { get; init; } = [];
}
