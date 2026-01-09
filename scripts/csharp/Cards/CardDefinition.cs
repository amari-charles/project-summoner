using ProjectSummoner.Cards.Effects.Concrete;
using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards;

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
    public required string Id { get; init; }

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

    /// <summary>Path to unit scene (for summon cards).</summary>
    public string UnitScenePath { get; init; } = "";

    /// <summary>Number of units to spawn.</summary>
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
    public string ProjectileId { get; init; } = "";

    /// <summary>VFX ID for spell visual effects.</summary>
    public string SpellVfx { get; init; } = "";

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

    /// <summary>Tags for filtering and categorization.</summary>
    public string[] Tags { get; init; } = [];

    /// <summary>Unlock condition for this card.</summary>
    public UnlockCondition UnlockCondition { get; init; } = UnlockCondition.Default;

    /// <summary>Elemental affinity.</summary>
    public Element ElementalAffinity { get; init; } = Element.Neutral;

    /// <summary>Path to card icon image.</summary>
    public string CardIconPath { get; init; } = "";
}
