using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Units;
using UnitType = Fateforged.Units.UnitType;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Sim-local card data. Holds everything the simulation needs about a card
/// without any Godot dependencies. Populated at match start by SimulationNode
/// from CardCatalog/UnitDefinitions.
///
/// Stored in MatchState.CardDataMap keyed by card catalog ID.
/// </summary>
public class SimCardData
{
    public SimCardCatalogId CatalogId { get; set; } = SimCardCatalogId.Empty;
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

    /// <summary>
    /// Projectile ID used by spell visuals/ballistics (empty = instant/non-projectile spell).
    /// </summary>
    public SimProjectileCatalogId SpellProjectileId { get; set; } = SimProjectileCatalogId.Empty;

    // =========================================================================
    // FACTORY
    // =========================================================================

    /// <summary>
    /// Create a SimCardData from a catalog CardDefinition.
    /// Populates base fields and spell effects. Unit templates are added separately
    /// via UnitDefinitions.BuildSimTemplate().
    /// </summary>
    public static SimCardData FromCardDefinition(CardDefinition card)
    {
        var simCard = new SimCardData
        {
            CatalogId = (string)card.Id,
            ManaCost = card.ManaCost,
            SummonTime = card.Summon?.SummonTime ?? card.SummonTime,
            IsSpell = card.Type == CardType.Spell,
            ElementId = (int)card.ElementalAffinity
        };

        if (card.Type == CardType.Spell)
        {
            simCard.SpellTargetingMode = card.SpellTargeting switch
            {
                SpellTargeting.SingleTarget => SpellTargetingMode.NearestEnemy,
                SpellTargeting.AreaOfEffect => SpellTargetingMode.Position,
                SpellTargeting.SelectionRadius => SpellTargetingMode.AlliesInRadius,
                _ => SpellTargetingMode.Position
            };
            simCard.SpellRadius = card.SpellTargeting == SpellTargeting.SelectionRadius
                ? card.SelectionRadius
                : card.SpellRadius;
            simCard.SpellProjectileId = (string)card.ProjectileId;

            if (card.SpellCategory == SpellCategory.Damage && card.SpellDamage > 0)
            {
                simCard.SpellEffects.Add(new SimSpellEffect
                {
                    EffectType = EffectType.Damage,
                    Value = card.SpellDamage,
                    DamageType = MapElementToDamageType(card.ElementalAffinity),
                    AoeRadius = card.SpellRadius,
                    Affinity = SpellAffinity.Enemies
                });
            }
        }

        return simCard;
    }

    /// <summary>
    /// Map an Element enum to the sim DamageType.
    /// Fire/Ice/Lightning etc. → Magic; Neutral → Physical.
    /// </summary>
    private static DamageType MapElementToDamageType(Element element)
    {
        return element == Element.Neutral ? DamageType.Physical : DamageType.Magic;
    }
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

    /// <summary>
    /// Unit type ID for scene/definition lookup (e.g., "earth_sprite").
    /// Distinct from the card catalog ID (e.g., "pebbloom").
    /// </summary>
    public string UnitTypeId { get; set; } = "";

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
    public UnitType UnitType { get; set; }
    public MovementLayer MovementLayer { get; set; }
    public int ElementId { get; set; }

    // Ranged config
    public float ProjectileDelay { get; set; }
    public float FlightAltitude { get; set; }

    // Targeting config (extracted from UnitDefinition at match start)
    public FallbackMovement FallbackMovement { get; set; }
    public bool HasConeConstraint { get; set; }
    public float ConeHalfAngle { get; set; } = 30f;
    public float CloseRangeThreshold { get; set; } = 0.5f;
    public TargetLayer TargetLayerFilter { get; set; }
    public float DistanceScorerWeight { get; set; } = 1f;
    public float HealthScorerWeight { get; set; }
    public TargetPolicyId TargetPolicyId { get; set; } = TargetPolicyId.PreferAttackableAndStick;
    public MovementIntentStrategy MovementIntentStrategy { get; set; } = MovementIntentStrategy.Context;

    // Phase 1 fields
    public DamageType AttackType { get; set; } = DamageType.Physical;
    public float PhysicalDefense { get; set; }
    public float MagicDefense { get; set; }
    public float Evasion { get; set; }
}
