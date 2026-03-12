using Fateforged.Cards.Formations;
using Fateforged.Cards.Spawning;
using Fateforged.Constants;
using Fateforged.Data.Traits;
using Fateforged.Projectiles;
using Fateforged.Stats;
using Fateforged.Vfx;

namespace Fateforged.Cards;

/// <summary>
/// Defines a card's static data - stats, costs, formation, and metadata.
/// Cards reference FormationPresets directly for type-safe formation configuration.
/// </summary>
public class CardDefinition
{
    // =========================================================================
    // IDENTITY
    // =========================================================================

    /// <summary>Unique identifier for this card (e.g., "cloud_swarm").</summary>
    public required CardId Id { get; init; }

    /// <summary>Display name shown in UI.</summary>
    public required string Name { get; init; }

    /// <summary>Card description text.</summary>
    public required string Description { get; init; }

    /// <summary>Rarity tier.</summary>
    public required Rarity Rarity { get; init; }

    // =========================================================================
    // CARD PROPERTIES
    // =========================================================================

    /// <summary>Type of card (Summon or Spell).</summary>
    public required CardType Type { get; init; }

    /// <summary>Mana cost to play this card.</summary>
    public required int ManaCost { get; init; }

    /// <summary>Cooldown in seconds before card can be played again.</summary>
    public float Cooldown { get; init; } = 2.0f;

    /// <summary>Time in seconds for summon animation before units appear.</summary>
    public float SummonTime { get; init; } = 1.0f;

    // =========================================================================
    // SUMMON PROPERTIES
    // =========================================================================

    /// <summary>
    /// Data-driven spawning specification for multi-unit cards.
    /// When set, this takes precedence over UnitId/SpawnCount for spawning logic.
    /// Enables cards like Mama Duck (1 mama + 3 ducklings) without hardcoded special cases.
    /// </summary>
    public SummonSpec? Summon { get; init; }

    /// <summary>
    /// References a unit type in UnitDefinitions. When set, base stats come from UnitDefinitions
    /// instead of the stat properties on this card. Use UnitModifier to apply variants.
    /// See docs/technical/runtime/unit-stat-pipeline.md for the full stat pipeline.
    /// Note: If Summon is set, this is ignored for spawning (but may still be used for UI display).
    /// </summary>
    public UnitId UnitId { get; init; } = UnitId.None;

    /// <summary>
    /// Optional modifier to apply to base stats from UnitDefinitions.
    /// Used for variant cards like swarms that spawn weaker versions of a unit.
    /// Example: Fire Wisp Swarm applies {max_hp: 0.75, attack_damage: 0.75} to fire_wisp.
    /// Note: If Summon is set, use UnitSpawnEntry.Modifier instead.
    /// </summary>
    public StatModifier? UnitModifier { get; init; } = null;

    /// <summary>Path to unit scene (for summon cards). Deprecated: Use UnitId instead.</summary>
    public string UnitScenePath { get; init; } = "";

    /// <summary>
    /// Number of units to spawn.
    /// Note: If Summon is set, this is ignored (use SummonSpec.TotalUnitCount).
    /// </summary>
    public int SpawnCount { get; init; } = 1;

    /// <summary>Formation strategy for positioning spawned units.</summary>
    public IFormationStrategy Formation { get; init; } = FormationPresets.StandardGrid;

    /// <summary>Unit combat type.</summary>
    public UnitType UnitType { get; init; } = UnitType.Melee;

    // =========================================================================
    // UNIT STATS
    // =========================================================================

    /// <summary>Maximum health points.</summary>
    public float MaxHp { get; init; }

    /// <summary>Damage dealt per attack.</summary>
    public float AttackDamage { get; init; }

    /// <summary>Range at which unit can attack.</summary>
    public float AttackRange { get; init; } = 2.0f;

    /// <summary>Attacks per second.</summary>
    public float AttackSpeed { get; init; } = 1.0f;

    /// <summary>Movement speed in units per second.</summary>
    public float MoveSpeed { get; init; } = 3.0f;

    /// <summary>Range at which unit detects and acquires targets.</summary>
    public float AggroRadius { get; init; } = 20.0f;

    /// <summary>Bonus damage dealt by this unit specifically to summoners.</summary>
    public float SoulStrength { get; init; }

    /// <summary>Whether this unit attacks from range.</summary>
    public bool IsRanged { get; init; } = false;

    /// <summary>Path to projectile scene (for ranged units).</summary>
    public string ProjectileScenePath { get; init; } = "";

    // =========================================================================
    // SPELL PROPERTIES
    // =========================================================================

    /// <summary>Damage dealt by spell.</summary>
    public float SpellDamage { get; init; }

    /// <summary>Area of effect radius.</summary>
    public float SpellRadius { get; init; }

    /// <summary>Duration of spell effect.</summary>
    public float SpellDuration { get; init; }

    /// <summary>Projectile ID for spells that use projectiles.</summary>
    public ProjectileId ProjectileId { get; init; } = ProjectileId.None;

    /// <summary>VFX ID for spell visual effects.</summary>
    public VfxId SpellVfx { get; init; } = VfxId.None;

    // =========================================================================
    // TACTICAL COMMAND PROPERTIES (for command spells)
    // =========================================================================

    /// <summary>Command type for tactical command spells.</summary>
    public CommandType? CommandType { get; init; } = null;

    /// <summary>Radius to select units for command.</summary>
    public float SelectionRadius { get; init; }

    /// <summary>Duration of formation/command effect.</summary>
    public float FormationDuration { get; init; }

    // =========================================================================
    // METADATA
    // =========================================================================

    /// <summary>Creature type classification for summons (flags, supports multiple).</summary>
    public CreatureType CreatureTypes { get; init; } = CreatureType.None;

    /// <summary>Combat role for summons (flags, supports multiple).</summary>
    public SummonRole Roles { get; init; } = SummonRole.None;

    /// <summary>Spell category (single value).</summary>
    public SpellCategory SpellCategory { get; init; } = SpellCategory.None;

    /// <summary>Spell targeting mode (single value).</summary>
    public SpellTargeting SpellTargeting { get; init; } = SpellTargeting.None;

    /// <summary>Visual traits (flags, supports multiple).</summary>
    public VisualTrait VisualTraits { get; init; } = VisualTrait.None;

    /// <summary>Special card flags.</summary>
    public CardFlags Flags { get; init; } = CardFlags.None;

    /// <summary>Unlock condition for this card.</summary>
    public UnlockCondition UnlockCondition { get; init; } = UnlockCondition.Default;

    /// <summary>Elemental affinity.</summary>
    public Element ElementalAffinity { get; init; } = Element.Neutral;

    /// <summary>Path to card icon image.</summary>
    public string CardIconPath { get; init; } = "";

    // =========================================================================
    // TRAIT ELIGIBILITY TAGS
    // =========================================================================

    /// <summary>
    /// Tags for trait eligibility matching (NOT modifier tags for amplification).
    /// These determine which traits/upgrades can be acquired by this card.
    /// Use TraitTags constants for type safety.
    /// - Summon cards: [Summon, Global, element, creature_type, summon_id]
    /// - Spell cards: [Spell, Global, element]
    ///
    /// Note: This is different from StatModifier.Tags which are used for
    /// amplification targeting. See docs/features/modifier-system.md for details.
    /// </summary>
    public string[] TraitEligibilityTags { get; init; } =
    [Data.Traits.TraitTags.Summon, Data.Traits.TraitTags.Global];
}
