using Godot;
using System.Collections.Generic;
using ProjectSummoner.Cards.Configs;
using ProjectSummoner.Cards.Formations;
using ProjectSummoner.Constants;
using ProjectSummoner.Services.Interfaces;
using ProjectSummoner.Stats;
using ProjectSummoner.Summons;
using ProjectSummoner.Systems.Modifiers;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards;

/// <summary>
/// Factory autoload for creating and executing C# cards from GDScript.
/// This bridges the GDScript CardCatalog with the C# card systems.
/// </summary>
public partial class CardFactory : Node, ICardFactory
{
    // =========================================================================
    // CONSTANTS (match GDScript enums)
    // =========================================================================

    /// <summary>GameController3D.BattlePhase.BATTLE value.</summary>
    private const int BattlePhaseBattle = 1;

    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static CardFactory? Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Print("CardFactory: Initialized");
    }

    // =========================================================================
    // SPELL API (GDScript-compatible snake_case methods)
    // =========================================================================

    /// <summary>
    /// Check if a spell effect exists for the given catalog ID.
    /// </summary>
    public bool has_effect(string catalogId)
    {
        return SpellBuilder.HasEffect(catalogId);
    }

    /// <summary>
    /// Create a SpellCard with the appropriate effect attached.
    /// </summary>
    public Resource? create_spell_card(string catalogId, Godot.Collections.Dictionary cardDef)
    {
        if (!SpellBuilder.HasEffect(catalogId))
        {
            GD.PrintErr($"[CardFactory] No effect for catalog ID: {catalogId}");
            return null;
        }

        var config = CreateSpellCardConfig(catalogId, cardDef);
        var spellCard = new SpellCard
        {
            Config = config,
            Effect = SpellBuilder.GetEffect(catalogId)
        };

        GD.Print($"[CardFactory] Created C# SpellCard for '{catalogId}'");
        return spellCard;
    }

    /// <summary>
    /// Execute a spell effect at the given position.
    /// </summary>
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
            GD.PrintErr($"[CardFactory] Cannot execute spell - no effect for: {catalogId}");
            return;
        }

        var effect = SpellBuilder.GetEffect(catalogId);
        var context = new Effects.Core.SpellContext
        {
            Position = position,
            Team = (Team)team,
            Battlefield = battlefield,
            ModifierService = modifierSystem,
            CardInstanceId = instanceId,
            SceneTree = battlefield?.GetTree()
        };

        effect.Execute(context);
        GD.Print($"[CardFactory] Executed C# spell effect for '{catalogId}'");
    }

    // =========================================================================
    // SUMMON API (GDScript-compatible snake_case methods)
    // =========================================================================

    /// <summary>
    /// Check if summon execution is supported for the given catalog ID.
    /// Currently all summons are supported (they use the GridFormation by default).
    /// </summary>
    public bool has_summon(string catalogId)
    {
        // All summons are supported - they use GridFormation by default
        return true;
    }

    /// <summary>
    /// Calculate safe spawn positions for all units in a formation.
    /// Single source of truth - used by both preview and actual spawn.
    /// Calculates all positions at once against the current state to ensure
    /// preview matches actual spawn positions.
    /// </summary>
    /// <param name="catalogId">The summon card's catalog ID</param>
    /// <param name="centerPosition">Center position for the formation</param>
    /// <param name="battlefield">Reference to the battlefield node</param>
    /// <param name="collisionRadius">Collision radius of units being spawned</param>
    /// <returns>Array of safe spawn positions for each unit</returns>
    public Godot.Collections.Array<Vector3> get_safe_spawn_positions(
        string catalogId,
        Vector3 centerPosition,
        Node? battlefield,
        float collisionRadius)
    {
        var result = new Godot.Collections.Array<Vector3>();

        if (battlefield == null)
        {
            GD.PushWarning("[CardFactory] Battlefield is null for safe spawn calculation");
            return result;
        }

        var card = CardCatalog.GetCard(catalogId);
        if (card == null)
        {
            GD.PushWarning($"[CardFactory] Card not found in catalog: {catalogId}");
            return result;
        }

        // Use SpawnPositionCalculator
        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            card.Formation,
            centerPosition,
            card.SpawnCount,
            battlefield.GetTree(),
            collisionRadius > 0 ? collisionRadius : 0.5f);

        foreach (var pos in positions)
            result.Add(pos);

        return result;
    }

    /// <summary>
    /// Execute a summon at the given position.
    /// This is called by GDScript Card when _csharp_summon_id is set.
    /// </summary>
    /// <param name="catalogId">The summon card's catalog ID</param>
    /// <param name="position">World position to spawn units</param>
    /// <param name="team">Team of the summoner (int for GDScript compatibility)</param>
    /// <param name="battlefield">Reference to the battlefield node</param>
    /// <param name="cardDef">Card definition dictionary from CardCatalog (used for categories)</param>
    /// <param name="effectiveStats">Stats with upgrades applied (from Card.get_effective_stats())</param>
    /// <param name="customOverrides">Custom stat overrides (from Card.custom_stat_overrides)</param>
    /// <param name="modifierSystem">Optional modifier system reference</param>
    /// <param name="instanceId">Card instance ID for modifier filtering</param>
    /// <param name="spawnDuration">Duration for spawn reveal animation</param>
    /// <returns>SummonResult containing the UnitSummon tracker or error</returns>
    public SummonResult execute_summon(
        string catalogId,
        Vector3 position,
        int team,
        Node battlefield,
        Godot.Collections.Dictionary cardDef,
        Godot.Collections.Dictionary effectiveStats,
        Godot.Collections.Dictionary customOverrides,
        Node? modifierSystem = null,
        string instanceId = "",
        float spawnDuration = 0.0f)
    {
        // 1. Validate and load card data
        var card = CardCatalog.GetCard(catalogId);
        if (card == null)
            return SummonResult.Fail($"Summon '{catalogId}' not found in C# CardCatalog");

        if (string.IsNullOrEmpty(card.UnitScenePath))
            return SummonResult.Fail($"Summon '{catalogId}' has no unit_scene_path");

        var unitScene = GD.Load<PackedScene>(card.UnitScenePath);
        if (unitScene == null)
            return SummonResult.Fail($"Failed to load unit scene: {card.UnitScenePath}");

        // 2. Create summon tracker
        var summon = new UnitSummon(catalogId, instanceId, team);

        // 3. Get gameplay layer
        Node gameplayLayer = GetGameplayLayer(battlefield);

        // 4. Get modifiers
        var modifiers = GetModifiersForCard(cardDef, team, instanceId);

        // 5. Calculate stats
        var unitStats = UnitStatCalculator.CalculateFromGodotDictionary(effectiveStats, modifiers, customOverrides);

        // 6. Calculate spawn positions
        float collisionRadius = UnitSpawner.GetCollisionRadius(unitScene);
        var safePositions = SpawnPositionCalculator.CalculateFormationPositions(
            card.Formation, position, card.SpawnCount, battlefield.GetTree(), collisionRadius);

        // 7. Determine if in battle phase
        bool inBattlePhase = IsInBattlePhase(gameplayLayer);

        // 8. Get SpatialGrid for position updates
        var spatialGrid = GetAutoloadNode("/root/SpatialGrid");

        // 9. Spawn units
        for (int i = 0; i < card.SpawnCount; i++)
        {
            var context = new UnitSpawnContext
            {
                Position = safePositions[i],
                Team = team,
                Stats = unitStats,
                Modifiers = modifiers,
                CustomOverrides = customOverrides,
                GameplayLayer = gameplayLayer,
                SpatialGrid = spatialGrid,
                SpawnDuration = spawnDuration,
                InBattlePhase = inBattlePhase
            };

            var unit3d = UnitSpawner.SpawnUnit(unitScene, context);
            if (unit3d != null)
            {
                summon.AddUnit(unit3d);
            }
            else
            {
                GD.PrintErr($"[CardFactory] Failed to spawn unit {i} for '{catalogId}'");
            }
        }

        GD.Print($"[CardFactory] Spawned {card.SpawnCount} units for '{catalogId}'");
        return SummonResult.Ok(summon);
    }

    /// <summary>Get the gameplay layer from battlefield.</summary>
    private static Node GetGameplayLayer(Node battlefield)
    {
        if (battlefield.HasMethod("get_gameplay_layer"))
        {
            var result = battlefield.Call("get_gameplay_layer");
            if (result.VariantType != Variant.Type.Nil)
            {
                return result.AsGodotObject() as Node ?? battlefield;
            }
        }
        return battlefield;
    }

    /// <summary>Get modifiers for a card spawn.</summary>
    private static List<StatModifier> GetModifiersForCard(Godot.Collections.Dictionary cardDef, int team, string instanceId)
    {
        var categories = new Godot.Collections.Dictionary();
        if (cardDef.ContainsKey("categories"))
        {
            var catVal = cardDef["categories"];
            if (catVal.VariantType == Variant.Type.Dictionary)
                categories = catVal.AsGodotDictionary();
        }

        var modifierContext = new Godot.Collections.Dictionary
        {
            { "card_name", GetString(cardDef, "card_name", "Unknown") },
            { "team", team },
            { "card_instance_id", instanceId }
        };

        return GetModifiersFromService("unit", categories, modifierContext);
    }

    /// <summary>Check if game is in battle phase.</summary>
    private static bool IsInBattlePhase(Node gameplayLayer)
    {
        var gameController = gameplayLayer.GetTree()?.CurrentScene;
        if (gameController != null)
        {
            var currentPhase = gameController.Get("current_phase");
            if (currentPhase.VariantType == Variant.Type.Int)
                return currentPhase.AsInt32() == BattlePhaseBattle;
        }
        return false;
    }

    // =========================================================================
    // SPELL CONFIG HELPERS
    // =========================================================================

    private static SpellCardConfig CreateSpellCardConfig(string catalogId, Godot.Collections.Dictionary cardDef)
    {
        var config = new SpellCardConfig
        {
            CatalogId = catalogId,
            CardName = GetString(cardDef, "card_name", "Unknown Spell"),
            CardType = CardType.Spell,
            Description = GetString(cardDef, "description", ""),
            ManaCost = GetInt(cardDef, "mana_cost", 1),
            Cooldown = GetFloat(cardDef, "cooldown", 2.0f),
            SpellDamage = GetFloat(cardDef, "spell_damage", 0f),
            SpellRadius = GetFloat(cardDef, "spell_radius", 0f),
            SpellDuration = GetFloat(cardDef, "spell_duration", 0f),
            SpellVFX = GetString(cardDef, "spell_vfx", ""),
            ProjectileId = GetString(cardDef, "projectile_id", ""),
            CommandType = GetString(cardDef, "command_type", ""),
            SelectionRadius = GetFloat(cardDef, "selection_radius", 8.0f)
        };

        var iconPath = GetString(cardDef, "card_icon_path", "");
        if (!string.IsNullOrEmpty(iconPath))
        {
            config.CardIcon = GD.Load<Texture2D>(iconPath);
        }

        return config;
    }

    // =========================================================================
    // FORMATION API (for GDScript preview)
    // =========================================================================

    /// <summary>
    /// Get formation offset for a unit by catalog ID.
    /// Called by GDScript Card class for spawn preview.
    /// </summary>
    /// <param name="catalogId">The card's catalog ID.</param>
    /// <param name="unitIndex">Index of the unit in the formation (0-based).</param>
    /// <param name="totalUnits">Total number of units being spawned.</param>
    /// <returns>Position offset from spawn center for this unit.</returns>
    public Vector3 get_formation_offset_by_id(string catalogId, int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
            return Vector3.Zero;

        var card = CardCatalog.GetCard(catalogId);
        if (card == null)
        {
            GD.PushWarning($"[CardFactory] Card not found for formation offset: {catalogId}");
            return Vector3.Zero;
        }

        return card.Formation.GetOffset(unitIndex, totalUnits);
    }


    // =========================================================================
    // SUMMON HELPERS
    // =========================================================================

    /// <summary>
    /// Get modifiers from the ModifierService.
    /// </summary>
    private static List<StatModifier> GetModifiersFromService(
        string targetType,
        Godot.Collections.Dictionary categories,
        Godot.Collections.Dictionary context)
    {
        var service = ModifierService.Instance;
        if (service == null)
            return new List<StatModifier>();

        var modContext = ModifierContext.FromDictionaries(categories, context);
        modContext.TargetType = targetType;

        return service.GetModifiers(modContext);
    }

    /// <summary>
    /// Get autoload node safely.
    /// </summary>
    private Node? GetAutoloadNode(string path)
    {
        var tree = GetTree();
        return tree?.Root?.GetNodeOrNull(path);
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
