using System.Collections.Generic;

namespace Fateforged.Simulation;

/// <summary>
/// Sim-local card data. Holds everything the simulation needs about a card
/// without any Godot dependencies. Populated at match start by SimulationNode
/// from CardCatalog/UnitDefinitions.
///
/// Stored in MatchState.CardDataMap keyed by catalog ID (string).
/// </summary>
public class SimCardData
{
    public string CatalogId { get; set; } = "";
    public int ManaCost { get; set; }
    public float SummonTime { get; set; }
    public bool IsSpell { get; set; }
    public int ElementId { get; set; }

    /// <summary>
    /// Unit templates to spawn when this summon card's casting completes.
    /// Each template represents a unit type with a count (e.g., 3x fire wisp).
    /// Empty for spell cards.
    /// </summary>
    public List<SimUnitTemplate> UnitTemplates { get; set; } = new();

    // =========================================================================
    // SPELL FIELDS (populated for IsSpell == true)
    // =========================================================================

    /// <summary>How this spell selects its targets.</summary>
    public SpellTargetingMode SpellTargetingMode { get; set; }

    /// <summary>AoE/selection radius for position-based and ally-selection spells.</summary>
    public float SpellRadius { get; set; }

    /// <summary>Effects applied when this spell card is cast.</summary>
    public List<SimSpellEffect> SpellEffects { get; set; } = new();
}

/// <summary>
/// Template for creating UnitData entries when a card's casting completes.
/// Pre-calculated stats from UnitDefinitions + CardDefinition + modifiers,
/// baked at match start for deterministic simulation.
/// </summary>
public class SimUnitTemplate
{
    /// <summary>Number of units to spawn with these stats.</summary>
    public int Count { get; set; } = 1;

    // Core stats
    public float MaxHp { get; set; }
    public float AttackDamage { get; set; }
    public float AttackSpeed { get; set; } = 1f;
    public float MoveSpeed { get; set; } = 3f;
    public float AttackRange { get; set; } = 2f;
    public float AggroRadius { get; set; } = 20f;
    public float SeparationRadius { get; set; } = 0.5f;
    public float CritChance { get; set; }
    public float CritDamage { get; set; } = 1.5f;

    // Classification
    public int UnitType { get; set; } // 0=Melee, 1=Ranged
    public int MovementLayer { get; set; } // 0=Ground, 1=Air
    public int ElementId { get; set; }

    // Ranged config
    public float ProjectileDelay { get; set; }
    public float FlightAltitude { get; set; }

    // Targeting config (extracted from UnitDefinition at match start)
    public int FallbackMovement { get; set; }
    public bool HasConeConstraint { get; set; }
    public float ConeHalfAngle { get; set; } = 30f;
    public float CloseRangeThreshold { get; set; } = 0.5f;
    public int TargetLayerFilter { get; set; }
    public float DistanceScorerWeight { get; set; } = 1f;
    public float HealthScorerWeight { get; set; }

    // Phase 1 fields
    public DamageType AttackType { get; set; } = DamageType.Physical;
    public float PhysicalDefense { get; set; }
    public float MagicDefense { get; set; }
    public float Evasion { get; set; }
}
