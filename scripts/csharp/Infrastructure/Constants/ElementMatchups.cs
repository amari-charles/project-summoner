using System.Collections.Generic;
using Fateforged.Cards;

namespace Fateforged.Constants;

/// <summary>
/// Element-to-element damage multipliers for combat.
/// Determines advantage/disadvantage when elements interact.
/// </summary>
public static class ElementMatchups
{
    // Default multiplier when no specific matchup is defined
    private const float DefaultMultiplier = 1.0f;

    // Advantage multiplier (attacker has advantage)
    private const float AdvantageMultiplier = 1.25f;

    // Disadvantage multiplier (attacker has disadvantage)
    private const float DisadvantageMultiplier = 0.8f;

    /// <summary>
    /// Matchup table: _matchups[attacker][defender] = multiplier.
    /// Only stores non-default matchups for efficiency.
    /// </summary>
    private static readonly Dictionary<Element, Dictionary<Element, float>> _matchups = new()
    {
        // =====================================================================
        // CORE ELEMENT CYCLE: Fire > Wind > Earth > Water > Fire
        // =====================================================================

        [Element.Fire] = new()
        {
            [Element.Wind] = AdvantageMultiplier, // Fire burns through Wind
            [Element.Water] = DisadvantageMultiplier, // Water extinguishes Fire
        },
        [Element.Wind] = new()
        {
            [Element.Earth] = AdvantageMultiplier, // Wind erodes Earth
            [Element.Fire] = DisadvantageMultiplier, // Fire consumes Wind
        },
        [Element.Earth] = new()
        {
            [Element.Water] = AdvantageMultiplier, // Earth absorbs Water
            [Element.Wind] = DisadvantageMultiplier, // Wind erodes Earth
        },
        [Element.Water] = new()
        {
            [Element.Fire] = AdvantageMultiplier, // Water extinguishes Fire
            [Element.Earth] = DisadvantageMultiplier, // Earth absorbs Water
        },

        // =====================================================================
        // OUTER ELEMENT INTERACTIONS
        // =====================================================================

        [Element.Lightning] = new()
        {
            [Element.Water] = AdvantageMultiplier, // Conductivity
            [Element.Earth] = DisadvantageMultiplier, // Grounding
        },
        [Element.Life] = new()
        {
            [Element.Death] = AdvantageMultiplier, // Life force overwhelms entropy
            [Element.Shadow] = DisadvantageMultiplier, // Shadows drain vitality
        },
        [Element.Death] = new()
        {
            [Element.Life] = AdvantageMultiplier, // Death claims the living
        },
        [Element.Shadow] = new()
        {
            [Element.Life] = AdvantageMultiplier, // Shadows drain vitality
        },

        // =====================================================================
        // ELEMENTS WITH NO MATCHUPS DEFINED (TBD - future design work)
        // =====================================================================
        // The following elements intentionally have no matchups yet:
        // - Poison: Corruption/decay element, matchups TBD
        // - Occultist: Forbidden magic, matchups TBD
        // - Holy: Elevated Fire, matchups TBD
        // - Ice: Elevated Water, matchups TBD
        // - Metal: Elevated Earth, matchups TBD
        // - Spirit: Elevated Life, matchups TBD
    };

    /// <summary>
    /// Get the damage multiplier for attacker vs defender element matchup.
    /// Returns 1.0 for neutral matchups or when either element is Neutral.
    /// </summary>
    public static float GetMultiplier(Element attacker, Element defender)
    {
        // Neutral element has no advantages or disadvantages
        if (attacker == Element.Neutral || defender == Element.Neutral)
            return DefaultMultiplier;

        // Look up specific matchup
        if (_matchups.TryGetValue(attacker, out var defenderMap))
        {
            if (defenderMap.TryGetValue(defender, out float multiplier))
                return multiplier;
        }

        return DefaultMultiplier;
    }

    /// <summary>
    /// Check if attacker has advantage against defender.
    /// </summary>
    public static bool HasAdvantage(Element attacker, Element defender)
    {
        return GetMultiplier(attacker, defender) > DefaultMultiplier;
    }

    /// <summary>
    /// Check if attacker has disadvantage against defender.
    /// </summary>
    public static bool HasDisadvantage(Element attacker, Element defender)
    {
        return GetMultiplier(attacker, defender) < DefaultMultiplier;
    }
}
