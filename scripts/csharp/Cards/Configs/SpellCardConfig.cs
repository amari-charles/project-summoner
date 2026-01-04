using Godot;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Configuration for spell cards.
/// Contains spell-specific properties like radius, damage, duration.
/// </summary>
public partial class SpellCardConfig : CardConfig
{
    // =========================================================================
    // SPELL PROPERTIES
    // =========================================================================

    /// <summary>
    /// Cast time (seconds player is locked during cast).
    /// </summary>
    [Export]
    public float CastTime { get; set; } = 0.5f;

    /// <summary>
    /// Effect radius (for area spells).
    /// </summary>
    [Export]
    public float SpellRadius { get; set; } = 10.0f;

    /// <summary>
    /// Base damage (for damage spells).
    /// </summary>
    [Export]
    public float SpellDamage { get; set; } = 0.0f;

    /// <summary>
    /// Duration (for over-time effects).
    /// </summary>
    [Export]
    public float SpellDuration { get; set; } = 0.0f;

    // =========================================================================
    // VFX/PROJECTILE
    // =========================================================================

    /// <summary>
    /// VFX effect ID (uses VFXManager).
    /// </summary>
    [Export]
    public string SpellVFX { get; set; } = "";

    /// <summary>
    /// Projectile ID (uses ProjectileManager).
    /// </summary>
    [Export]
    public string ProjectileId { get; set; } = "";

    // =========================================================================
    // COMMAND SPELLS
    // =========================================================================

    /// <summary>
    /// Command type for tactical spells (rally, guard, charge).
    /// Empty string means not a command spell.
    /// </summary>
    [Export]
    public string CommandType { get; set; } = "";

    /// <summary>
    /// Selection radius for command spells.
    /// </summary>
    [Export]
    public float SelectionRadius { get; set; } = 8.0f;
}
