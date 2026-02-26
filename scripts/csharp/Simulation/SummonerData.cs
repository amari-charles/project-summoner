using System.Collections.Generic;
using ProjectSummoner.Cards;

namespace Fateforged.Simulation;

/// <summary>
/// Per-summoner gameplay state stored in MatchState.
/// Contains all mutable summoner data: HP, mana, casting, deck/hand/discard.
/// </summary>
public class SummonerData
{
    public int Team { get; set; }

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

    // Element (int cast of ProjectSummoner.Cards.Element enum)
    public int ElementId { get; set; } // 0=Neutral

    // Damage bonuses (from summoner traits)
    public float DamageBonus { get; set; } // General % damage bonus
    public float DamageReduction { get; set; } // Flat damage reduction
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

    // Casting state
    public bool IsCasting { get; set; }
    public float CastingTimeRemaining { get; set; }
    public float CastingTimeTotal { get; set; }
    public int CastingCardIndex { get; set; } = -1;
    public SimVector3 CastingSpawnPosition { get; set; }
    public int CastingNetworkId { get; set; } = -1;

    // Deck management (catalog IDs)
    public List<string> Deck { get; set; } = new();
    public List<string> Hand { get; set; } = new();
    public List<string> DiscardPile { get; set; } = new();
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
            foreach (var id in Deck)
                hash = hash * 31 + DeterministicStringHash(id);
            hash = hash * 37; // Separator between collections
            foreach (var id in Hand)
                hash = hash * 31 + DeterministicStringHash(id);
            hash = hash * 37;
            foreach (var id in DiscardPile)
                hash = hash * 31 + DeterministicStringHash(id);
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
