using Godot;

namespace Fateforged.Cards.Configs;

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

    // =========================================================================
    // CONVERSION
    // =========================================================================

    /// <summary>
    /// Create a SpellCardConfig from a GDScript dictionary.
    /// </summary>
    public static new SpellCardConfig FromDictionary(Godot.Collections.Dictionary dict)
    {
        var config = new SpellCardConfig();
        PopulateFromDictionary(config, dict);
        PopulateSpellFromDictionary(config, dict);
        return config;
    }

    /// <summary>
    /// Populate spell-specific fields from dictionary.
    /// </summary>
    protected static void PopulateSpellFromDictionary(
        SpellCardConfig config,
        Godot.Collections.Dictionary dict
    )
    {
        if (dict.TryGetValue("summon_time", out var castTime))
            config.CastTime = castTime.AsSingle();

        if (dict.TryGetValue("spell_radius", out var spellRadius))
            config.SpellRadius = spellRadius.AsSingle();

        if (dict.TryGetValue("spell_damage", out var spellDamage))
            config.SpellDamage = spellDamage.AsSingle();

        if (dict.TryGetValue("spell_duration", out var spellDuration))
            config.SpellDuration = spellDuration.AsSingle();

        if (dict.TryGetValue("spell_vfx", out var spellVfx))
            config.SpellVFX = spellVfx.AsString();

        if (dict.TryGetValue("projectile_id", out var projectileId))
            config.ProjectileId = projectileId.AsString();

        if (dict.TryGetValue("command_type", out var commandType))
            config.CommandType = commandType.AsString();

        if (dict.TryGetValue("selection_radius", out var selectionRadius))
            config.SelectionRadius = selectionRadius.AsSingle();
    }

    /// <summary>
    /// Convert to GDScript-compatible dictionary.
    /// </summary>
    public override Godot.Collections.Dictionary ToDictionary()
    {
        var dict = base.ToDictionary();

        // Spell properties
        dict["summon_time"] = CastTime;
        dict["spell_radius"] = SpellRadius;
        dict["spell_damage"] = SpellDamage;
        dict["spell_duration"] = SpellDuration;
        dict["spell_vfx"] = SpellVFX;
        dict["projectile_id"] = ProjectileId;
        dict["command_type"] = CommandType;
        dict["selection_radius"] = SelectionRadius;

        return dict;
    }
}
