using Godot;
using ProjectSummoner.Cards.Configs;

namespace ProjectSummoner.Cards;

/// <summary>
/// Factory autoload for creating C# SpellCard instances from GDScript.
/// This bridges the GDScript CardCatalog with the C# spell effect system.
/// </summary>
public partial class SpellCardFactory : Node
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static SpellCardFactory? Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Print("SpellCardFactory: Initialized");
    }

    // =========================================================================
    // PUBLIC API (GDScript-compatible snake_case methods)
    // =========================================================================

    /// <summary>
    /// Check if a spell effect exists for the given catalog ID.
    /// Call this before create_spell_card() to verify the spell is supported.
    /// </summary>
    public bool has_effect(string catalogId)
    {
        return SpellBuilder.HasEffect(catalogId);
    }

    /// <summary>
    /// Create a SpellCard with the appropriate effect attached.
    /// Returns the SpellCard as a Resource (for GDScript compatibility).
    /// Returns null if the catalog ID is not a supported spell.
    /// </summary>
    /// <param name="catalogId">The spell's catalog ID (e.g., "fireball", "rally")</param>
    /// <param name="cardDef">Dictionary from CardCatalog.get_card()</param>
    public Resource? create_spell_card(string catalogId, Godot.Collections.Dictionary cardDef)
    {
        // Verify we have an effect for this spell
        if (!SpellBuilder.HasEffect(catalogId))
        {
            GD.PrintErr($"[SpellCardFactory] No effect for catalog ID: {catalogId}");
            return null;
        }

        // Create SpellCardConfig from dictionary
        var config = CreateSpellCardConfig(catalogId, cardDef);

        // Create SpellCard and attach config + effect
        var spellCard = new SpellCard
        {
            Config = config,
            Effect = SpellBuilder.GetEffect(catalogId)
        };

        GD.Print($"[SpellCardFactory] Created C# SpellCard for '{catalogId}'");
        return spellCard;
    }

    /// <summary>
    /// Execute a spell effect at the given position.
    /// This is called by GDScript Card when _csharp_spell_id is set.
    /// </summary>
    /// <param name="catalogId">The spell's catalog ID</param>
    /// <param name="position">World position to cast the spell</param>
    /// <param name="team">Team of the caster (int for GDScript compatibility)</param>
    /// <param name="battlefield">Reference to the battlefield node</param>
    /// <param name="modifierSystem">Optional modifier system reference</param>
    /// <param name="instanceId">Card instance ID for modifier filtering</param>
    public void execute_spell(
        string catalogId,
        Vector3 position,
        int team,
        Node battlefield,
        Node? modifierSystem = null,
        string instanceId = "")
    {
        if (!SpellBuilder.HasEffect(catalogId))
        {
            GD.PrintErr($"[SpellCardFactory] Cannot execute - no effect for: {catalogId}");
            return;
        }

        var effect = SpellBuilder.GetEffect(catalogId);

        var context = new Effects.Core.SpellContext
        {
            Position = position,
            Team = (Units.Team)team,
            Battlefield = battlefield,
            ModifierSystem = modifierSystem,
            CardInstanceId = instanceId,
            SceneTree = battlefield?.GetTree()
        };

        effect.Execute(context);
        GD.Print($"[SpellCardFactory] Executed C# spell effect for '{catalogId}'");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Create a SpellCardConfig from a catalog dictionary.
    /// </summary>
    private static SpellCardConfig CreateSpellCardConfig(string catalogId, Godot.Collections.Dictionary cardDef)
    {
        var config = new SpellCardConfig
        {
            // Identity
            CatalogId = catalogId,
            CardName = GetString(cardDef, "card_name", "Unknown Spell"),
            CardType = CardType.Spell,
            Description = GetString(cardDef, "description", ""),

            // Gameplay
            ManaCost = GetInt(cardDef, "mana_cost", 1),
            Cooldown = GetFloat(cardDef, "cooldown", 2.0f),

            // Spell-specific
            SpellDamage = GetFloat(cardDef, "spell_damage", 0f),
            SpellRadius = GetFloat(cardDef, "spell_radius", 0f),
            SpellDuration = GetFloat(cardDef, "spell_duration", 0f),
            SpellVFX = GetString(cardDef, "spell_vfx", ""),
            ProjectileId = GetString(cardDef, "projectile_id", ""),

            // Command spells
            CommandType = GetString(cardDef, "command_type", ""),
            SelectionRadius = GetFloat(cardDef, "selection_radius", 8.0f)
        };

        // Load card icon if path provided
        var iconPath = GetString(cardDef, "card_icon_path", "");
        if (!string.IsNullOrEmpty(iconPath))
        {
            config.CardIcon = GD.Load<Texture2D>(iconPath);
        }

        return config;
    }

    // =========================================================================
    // DICTIONARY HELPERS
    // =========================================================================

    private static string GetString(Godot.Collections.Dictionary dict, string key, string defaultValue)
    {
        if (!dict.ContainsKey(key)) return defaultValue;
        var value = dict[key];
        return value.VariantType == Variant.Type.String ? value.AsString() : defaultValue;
    }

    private static int GetInt(Godot.Collections.Dictionary dict, string key, int defaultValue)
    {
        if (!dict.ContainsKey(key)) return defaultValue;
        var value = dict[key];
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => (int)value.AsSingle(),
            _ => defaultValue
        };
    }

    private static float GetFloat(Godot.Collections.Dictionary dict, string key, float defaultValue)
    {
        if (!dict.ContainsKey(key)) return defaultValue;
        var value = dict[key];
        return value.VariantType switch
        {
            Variant.Type.Float => value.AsSingle(),
            Variant.Type.Int => value.AsInt32(),
            _ => defaultValue
        };
    }
}
