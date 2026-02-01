using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;

namespace ProjectSummoner.Services.Rewards;

/// <summary>
/// Defines reward pools used for flexible reward generation.
/// Pools filter cards by element, rarity, type, and tags.
/// Uses strongly-typed RewardPoolId for type-safe lookups.
/// </summary>
public static class RewardPoolCatalog
{
    // =========================================================================
    // POOL DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, RewardPoolDefinition> _pools = new()
    {
        // =====================================================================
        // CURATED POOLS (explicit card lists)
        // =====================================================================

        [RewardPoolIds.TutorialRewards] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.TutorialRewards,
            ExplicitCardIds = ["fire_wisp", "pebbloom", "puff"]
        },

        [RewardPoolIds.StarterRewards] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.StarterRewards,
            ExplicitCardIds = ["mana_bolt", "water_frog"]
        },

        [RewardPoolIds.BossLoot] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.BossLoot,
            ExplicitCardIds = []  // To be populated when pools are redone
        },

        // =====================================================================
        // COMBINED FILTER POOLS (element + rarity + type)
        // =====================================================================

        [RewardPoolIds.FireCommonUnits] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.FireCommonUnits,
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Fire],
                Rarities = [Rarity.Common],
                CardTypes = [CardType.Summon],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            }
        },

        [RewardPoolIds.WaterCommonUnits] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.WaterCommonUnits,
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Water],
                Rarities = [Rarity.Common],
                CardTypes = [CardType.Summon],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            }
        },

        [RewardPoolIds.WindCommonUnits] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.WindCommonUnits,
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Wind],
                Rarities = [Rarity.Common],
                CardTypes = [CardType.Summon],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            }
        },

        [RewardPoolIds.EarthCommonUnits] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.EarthCommonUnits,
            CardFilters = new CardFilterConfig
            {
                Elements = [Element.Earth],
                Rarities = [Rarity.Common],
                CardTypes = [CardType.Summon],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            }
        },

        [RewardPoolIds.AllSpells] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.AllSpells,
            CardFilters = new CardFilterConfig
            {
                CardTypes = [CardType.Spell],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            }
        },

        [RewardPoolIds.AllCommon] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.AllCommon,
            CardFilters = new CardFilterConfig
            {
                Rarities = [Rarity.Common],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            }
        },

        [RewardPoolIds.AllRare] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.AllRare,
            CardFilters = new CardFilterConfig
            {
                Rarities = [Rarity.Rare],
                ExcludeUnlockConditions = [UnlockCondition.DevOnly]
            }
        },

        // =====================================================================
        // COMPOSITE POOLS (union of other pools)
        // =====================================================================

        [RewardPoolIds.ElementalStarters] = new RewardPoolDefinition
        {
            PoolId = RewardPoolIds.ElementalStarters,
            CombinePools = [
                RewardPoolIds.FireCommonUnits,
                RewardPoolIds.WaterCommonUnits,
                RewardPoolIds.WindCommonUnits,
                RewardPoolIds.EarthCommonUnits
            ]
        },
    };


    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a pool definition by ID. Returns null if not found.</summary>
    public static RewardPoolDefinition? GetPool(RewardPoolId poolId) =>
        _pools.GetValueOrDefault(poolId);

    /// <summary>Get a pool definition by string ID. Returns null if not found.</summary>
    public static RewardPoolDefinition? GetPool(string poolId) =>
        _pools.GetValueOrDefault(poolId);

    /// <summary>Check if a pool exists.</summary>
    public static bool HasPool(RewardPoolId poolId) =>
        _pools.ContainsKey(poolId);

    /// <summary>Check if a pool exists by string ID.</summary>
    public static bool HasPool(string poolId) =>
        _pools.ContainsKey(poolId);

    /// <summary>Get all pool IDs.</summary>
    public static RewardPoolId[] GetAllPoolIds() =>
        _pools.Values.Select(p => p.PoolId).ToArray();

    // =========================================================================
    // CARD RESOLUTION
    // =========================================================================

    /// <summary>
    /// Get cards matching a pool's filters.
    /// Handles explicit card lists, filters, and pool composition.
    /// </summary>
    public static CardDefinition[] GetCardsForPool(RewardPoolId poolId, HashSet<string>? excludeCatalogIds = null)
    {
        var pool = GetPool(poolId);
        if (pool == null)
        {
            GD.PushWarning($"RewardPoolCatalog: Pool '{poolId}' not found");
            return [];
        }

        return ResolvePoolDefinition(pool, excludeCatalogIds);
    }

    /// <summary>
    /// Get cards matching a pool's filters by string ID.
    /// </summary>
    public static CardDefinition[] GetCardsForPool(string poolId, HashSet<string>? excludeCatalogIds = null)
    {
        var pool = GetPool(poolId);
        if (pool == null)
        {
            GD.PushWarning($"RewardPoolCatalog: Pool '{poolId}' not found");
            return [];
        }

        return ResolvePoolDefinition(pool, excludeCatalogIds);
    }

    /// <summary>
    /// Resolve a pool definition to card definitions.
    /// Handles explicit cards, filters, and pool composition.
    /// </summary>
    private static CardDefinition[] ResolvePoolDefinition(RewardPoolDefinition pool, HashSet<string>? excludeCatalogIds = null)
    {
        HashSet<CardDefinition> candidates = [];

        // Step 1: Handle composite pools (union)
        if (pool.CombinePools is { Count: > 0 })
        {
            foreach (var subPoolId in pool.CombinePools)
            {
                var subCards = GetCardsForPool(subPoolId, excludeCatalogIds: null); // Don't double-filter
                foreach (var card in subCards)
                    candidates.Add(card);
            }
        }
        // Step 2: Handle explicit card lists
        else if (pool.ExplicitCardIds is { Count: > 0 })
        {
            foreach (var cardId in pool.ExplicitCardIds)
            {
                var card = CardCatalog.GetCard(cardId);
                if (card != null)
                    candidates.Add(card);
            }
        }
        // Step 3: Handle filter-based pools
        else
        {
            var allCards = CardCatalog.GetAllCards();
            foreach (var card in allCards)
                candidates.Add(card);
        }

        // Step 4: Apply filters (if any)
        if (pool.CardFilters != null)
        {
            candidates = candidates.Where(card => MatchesFilters(card, pool.CardFilters)).ToHashSet();
        }

        // Step 5: Apply exclusions
        if (excludeCatalogIds != null)
        {
            candidates = candidates.Where(c => !excludeCatalogIds.Contains(c.Id)).ToHashSet();
        }

        return [.. candidates];
    }

    /// <summary>
    /// Get cards matching specific filter criteria (inline config).
    /// </summary>
    public static CardDefinition[] FilterCards(CardFilterConfig filters, HashSet<string>? excludeCatalogIds = null)
    {
        var allCards = CardCatalog.GetAllCards();

        return allCards.Where(card =>
        {
            // Exclude by catalog ID (owned cards)
            if (excludeCatalogIds != null && excludeCatalogIds.Contains(card.Id))
                return false;

            return MatchesFilters(card, filters);
        }).ToArray();
    }

    /// <summary>
    /// Check if a card matches filter criteria.
    /// </summary>
    private static bool MatchesFilters(CardDefinition card, CardFilterConfig filters)
    {
        // Filter by elements
        if (filters.Elements.Count > 0 && !filters.Elements.Contains(card.ElementalAffinity))
            return false;

        // Filter by rarities
        if (filters.Rarities.Count > 0 && !filters.Rarities.Contains(card.Rarity))
            return false;

        // Filter by card types
        if (filters.CardTypes.Count > 0 && !filters.CardTypes.Contains(card.Type))
            return false;

        // Filter by creature types (any match)
        if (filters.CreatureTypes.Count > 0)
        {
            bool hasMatch = filters.CreatureTypes.Any(ct => (card.CreatureTypes & ct) != 0);
            if (!hasMatch) return false;
        }

        // Filter by roles (any match)
        if (filters.Roles.Count > 0)
        {
            bool hasMatch = filters.Roles.Any(r => (card.Roles & r) != 0);
            if (!hasMatch) return false;
        }

        // Filter by spell categories
        if (filters.SpellCategories.Count > 0 && !filters.SpellCategories.Contains(card.SpellCategory))
            return false;

        // Exclude cards with DevOnly or Archived flags
        if ((card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) != 0)
            return false;

        // Exclude by unlock conditions
        if (filters.ExcludeUnlockConditions.Contains(card.UnlockCondition))
            return false;

        return true;
    }

    /// <summary>
    /// Get pool ID for a specific element.
    /// </summary>
    public static RewardPoolId? GetPoolIdForElement(Element element)
    {
        return element switch
        {
            Element.Fire => RewardPoolIds.FireCommonUnits,
            Element.Water => RewardPoolIds.WaterCommonUnits,
            Element.Wind => RewardPoolIds.WindCommonUnits,
            Element.Earth => RewardPoolIds.EarthCommonUnits,
            _ => null
        };
    }

    // =========================================================================
    // INLINE FILTER CONFIG FROM GDSCRIPT
    // =========================================================================

    /// <summary>
    /// Create a CardFilterConfig from a GDScript dictionary.
    /// Allows battle configs to specify inline filters.
    /// </summary>
    public static CardFilterConfig? FilterConfigFromDict(Godot.Collections.Dictionary dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        var config = new CardFilterConfig();

        // Element filter (int enum value)
        if (dict.TryGetValue("element", out var elemVar))
        {
            var elemInt = elemVar.AsInt32();
            if (Enum.IsDefined(typeof(Element), elemInt))
                config.Elements.Add((Element)elemInt);
        }

        // Rarity filter (int enum value)
        if (dict.TryGetValue("rarity", out var rarVar))
        {
            var rarInt = rarVar.AsInt32();
            if (Enum.IsDefined(typeof(Rarity), rarInt))
                config.Rarities.Add((Rarity)rarInt);
        }

        // Card type filter (int enum value)
        if (dict.TryGetValue("card_type", out var typeVar))
        {
            var typeInt = typeVar.AsInt32();
            if (Enum.IsDefined(typeof(CardType), typeInt))
                config.CardTypes.Add((CardType)typeInt);
        }

        // Always exclude DevOnly cards
        config.ExcludeUnlockConditions.Add(UnlockCondition.DevOnly);

        return config;
    }

    /// <summary>
    /// Draw cards using inline filter config from GDScript.
    /// </summary>
    public static CardDefinition[] DrawWithFilters(
        Godot.Collections.Dictionary filterDict,
        HashSet<string>? excludeCatalogIds = null)
    {
        var filters = FilterConfigFromDict(filterDict);
        if (filters == null)
            return CardCatalog.GetAllCards()
                .Where(c => c.UnlockCondition != UnlockCondition.DevOnly)
                .Where(c => excludeCatalogIds == null || !excludeCatalogIds.Contains(c.Id))
                .ToArray();

        return FilterCards(filters, excludeCatalogIds);
    }
}

/// <summary>
/// Definition of a reward pool.
/// Supports three modes:
/// 1. Explicit card list (ExplicitCardIds)
/// 2. Filter-based (CardFilters)
/// 3. Composite (CombinePools - union of other pools)
/// </summary>
public class RewardPoolDefinition
{
    /// <summary>Pool identifier.</summary>
    public RewardPoolId PoolId { get; init; }

    /// <summary>Explicit card IDs (for curated pools).</summary>
    public List<string>? ExplicitCardIds { get; init; }

    /// <summary>Card filter configuration (for filter-based pools).</summary>
    public CardFilterConfig? CardFilters { get; init; }

    /// <summary>Other pools to combine (union, for composite pools).</summary>
    public List<RewardPoolId>? CombinePools { get; init; }
}

/// <summary>
/// Configuration for filtering cards using typed properties.
/// </summary>
public class CardFilterConfig
{
    /// <summary>Filter to specific elements (empty = all elements).</summary>
    public List<Element> Elements { get; init; } = [];

    /// <summary>Filter to specific rarities (empty = all rarities).</summary>
    public List<Rarity> Rarities { get; init; } = [];

    /// <summary>Filter to specific card types (empty = all types).</summary>
    public List<CardType> CardTypes { get; init; } = [];

    /// <summary>Filter to cards with any of these creature types (empty = no filter).</summary>
    public List<CreatureType> CreatureTypes { get; init; } = [];

    /// <summary>Filter to cards with any of these roles (empty = no filter).</summary>
    public List<SummonRole> Roles { get; init; } = [];

    /// <summary>Filter to spells with these categories (empty = no filter).</summary>
    public List<SpellCategory> SpellCategories { get; init; } = [];

    /// <summary>Exclude cards with these unlock conditions.</summary>
    public List<UnlockCondition> ExcludeUnlockConditions { get; init; } = [];
}
