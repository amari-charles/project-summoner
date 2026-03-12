using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Simulation.AI;
using Fateforged.Units;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Per-summoner gameplay state stored in MatchState.
/// Contains all mutable summoner data: HP, mana, casting, deck/hand/discard.
/// </summary>
public class SummonerData
{
    public Team Team { get; set; }

    // Position (used by simulation for summoner targeting)
    public SimVector3 Position { get; set; }

    // HP
    public float CurrentHp { get; set; }
    public float MaxHp { get; set; }
    public bool IsAlive { get; set; } = true;

    // Mana
    public float Mana { get; set; }
    public float MaxMana { get; set; }

    // Cast speed
    public float CastSpeed { get; set; } = 1.0f;

    // Element (int cast of Fateforged.Cards.Element enum)
    public int ElementId { get; set; } // 0=Neutral

    // Damage bonuses (from summoner traits)
    public float DamageBonus { get; set; } // General % damage bonus
    // Flat post-defense reduction applied to incoming unit damage in SimDamage.
    // Summoner-vs-summoner incoming damage uses SoulStrength lane modifiers instead.
    public float DamageReduction { get; set; }
    public float SoulStrength { get; set; } // Flat reduction against incoming summoner-targeted damage
    private readonly Dictionary<Element, float> _elementalDamageBonuses = new();

    /// <summary>
    /// Set per-element damage bonus percentage.
    /// </summary>
    public void SetElementalDamageBonus(Element element, float bonus)
    {
        _elementalDamageBonuses[element] = bonus;
    }

    /// <summary>
    /// Get per-element damage bonus percentage. Returns 0 if not set.
    /// </summary>
    public float GetElementalDamageBonus(Element element)
    {
        return _elementalDamageBonuses.TryGetValue(element, out float bonus) ? bonus : 0f;
    }

    /// <summary>
    /// Enumerate all configured per-element damage bonuses.
    /// </summary>
    public IEnumerable<KeyValuePair<Element, float>> EnumerateElementalDamageBonuses()
    {
        return _elementalDamageBonuses;
    }

    /// <summary>
    /// Clear all per-element damage bonus values.
    /// </summary>
    public void ClearElementalDamageBonuses()
    {
        _elementalDamageBonuses.Clear();
    }

    // AI state (null Ai = human-controlled)
    public AiConfig? Ai { get; set; }
    public float AiPlayTimer { get; set; }
    public float AiNextPlayTime { get; set; }
    public int AiScriptIndex { get; set; }

    // Casting state
    public bool IsCasting { get; set; }
    public float CastingTimeRemaining { get; set; }
    public float CastingTimeTotal { get; set; }
    public int CastingCardIndex { get; set; } = -1;
    public SimCardCatalogId CastingCatalogId { get; set; } = SimCardCatalogId.Empty;
    public SimCardInstanceId CastingCardInstanceId { get; set; } = SimCardInstanceId.Empty;
    public SimVector3 CastingSpawnPosition { get; set; }
    public int CastingNetworkId { get; set; } = -1;

    // Deck management (catalog IDs)
    public List<SimCardCatalogId> Deck { get; set; } = new();
    public List<SimCardCatalogId> Hand { get; set; } = new();
    public List<SimCardCatalogId> DiscardPile { get; set; } = new();
    public List<SimCardRuntimeRef> DeckRefs { get; set; } = new();
    public List<SimCardRuntimeRef> HandRefs { get; set; } = new();
    public List<SimCardRuntimeRef> DiscardRefs { get; set; } = new();
    public int MaxHandSize { get; set; } = 4;

    /// <summary>
    /// Compute a deterministic hash of deck + hand + discard pile (order-dependent).
    /// Used for desync detection of card state between host and client.
    /// IMPORTANT: Uses FNV-1a for strings instead of string.GetHashCode(),
    /// because .NET randomizes string hash seeds per-process.
    /// </summary>
    public int ComputeCardHash()
    {
        unchecked
        {
            int hash = 17;
            if (DeckRefs.Count > 0)
            {
                foreach (var card in DeckRefs)
                    hash = hash * 31 + DeterministicStringHash(card.CatalogId);
            }
            else
            {
                foreach (var id in Deck)
                    hash = hash * 31 + DeterministicStringHash(id.Value);
            }

            hash = hash * 37; // Separator between collections
            if (HandRefs.Count > 0)
            {
                foreach (var card in HandRefs)
                    hash = hash * 31 + DeterministicStringHash(card.CatalogId);
            }
            else
            {
                foreach (var id in Hand)
                    hash = hash * 31 + DeterministicStringHash(id.Value);
            }

            hash = hash * 37;
            if (DiscardRefs.Count > 0)
            {
                foreach (var card in DiscardRefs)
                    hash = hash * 31 + DeterministicStringHash(card.CatalogId);
            }
            else
            {
                foreach (var id in DiscardPile)
                    hash = hash * 31 + DeterministicStringHash(id.Value);
            }

            return hash;
        }
    }

    /// <summary>
    /// FNV-1a hash for strings. Deterministic across processes and machines,
    /// unlike string.GetHashCode() which is randomized per-process in .NET.
    /// </summary>
    private static int DeterministicStringHash(string? s)
    {
        if (s == null) return 0;
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in s)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }
    }
}
