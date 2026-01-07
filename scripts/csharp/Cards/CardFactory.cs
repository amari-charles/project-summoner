using Godot;
using System.Collections.Generic;
using ProjectSummoner.Cards.Configs;
using ProjectSummoner.Cards.Formations;
using ProjectSummoner.Services.Interfaces;
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

        // Validate battlefield
        if (battlefield == null)
        {
            GD.PushWarning("[CardFactory] Battlefield is null for safe spawn calculation");
            return result;
        }

        // Get card definition for formation info
        var cardCatalog = GetAutoloadNode("/root/CardCatalog");
        if (cardCatalog == null)
        {
            GD.PushWarning("[CardFactory] CardCatalog not found for safe spawn calculation");
            return result;
        }

        var cardDefResult = cardCatalog.Call("get_card", catalogId);
        if (cardDefResult.VariantType != Variant.Type.Dictionary)
        {
            GD.PushWarning($"[CardFactory] Card not found in catalog: {catalogId}");
            return result;
        }
        var cardDef = cardDefResult.AsGodotDictionary();

        // Get spawn count and formation
        int spawnCount = GetInt(cardDef, "spawn_count", 1);
        var formationConfig = CreateFormationConfig(cardDef);
        var formation = formationConfig.CreateFormation();

        // Ensure collision radius is valid
        if (collisionRadius <= 0) collisionRadius = 0.5f;

        // Calculate all positions at once against current state
        // Pass already-calculated positions to avoid units in same batch overlapping
        var batchPositions = new List<Vector3>();
        for (int i = 0; i < spawnCount; i++)
        {
            var offset = formation.GetOffset(i, spawnCount);
            var desiredPos = centerPosition + offset;
            var safePos = FindSafeSpawnPosition(desiredPos, battlefield.GetTree(), collisionRadius, null, batchPositions);
            batchPositions.Add(safePos);
            result.Add(safePos);
        }

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
    /// <param name="cardDef">Card definition dictionary from CardCatalog</param>
    /// <param name="effectiveStats">Stats with upgrades applied (from Card.get_effective_stats())</param>
    /// <param name="customOverrides">Custom stat overrides (from Card.custom_stat_overrides)</param>
    /// <param name="modifierSystem">Optional modifier system reference</param>
    /// <param name="instanceId">Card instance ID for modifier filtering</param>
    /// <param name="spawnDuration">Duration for spawn reveal animation</param>
    public void execute_summon(
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
        // Get unit scene path
        var unitScenePath = GetString(cardDef, "unit_scene_path", "");
        if (string.IsNullOrEmpty(unitScenePath))
        {
            GD.PrintErr($"[CardFactory] Summon '{catalogId}' has no unit_scene_path!");
            return;
        }

        // Load unit scene
        var unitScene = GD.Load<PackedScene>(unitScenePath);
        if (unitScene == null)
        {
            GD.PrintErr($"[CardFactory] Failed to load unit scene: {unitScenePath}");
            return;
        }

        // Get spawn count
        int spawnCount = GetInt(cardDef, "spawn_count", 1);

        // Create formation config and get strategy
        var formationConfig = CreateFormationConfig(cardDef);
        var formation = formationConfig.CreateFormation();

        // Get gameplay layer
        Node gameplayLayer = battlefield;
        if (battlefield.HasMethod("get_gameplay_layer"))
        {
            var result = battlefield.Call("get_gameplay_layer");
            if (result.VariantType != Variant.Type.Nil)
            {
                gameplayLayer = result.AsGodotObject() as Node ?? battlefield;
            }
        }

        // Get card categories for modifier system
        var categories = new Godot.Collections.Dictionary();
        if (cardDef.ContainsKey("categories"))
        {
            var catVal = cardDef["categories"];
            if (catVal.VariantType == Variant.Type.Dictionary)
            {
                categories = catVal.AsGodotDictionary();
            }
        }

        // Build modifier context
        var modifierContext = new Godot.Collections.Dictionary
        {
            { "card_name", GetString(cardDef, "card_name", "Unknown") },
            { "team", team },
            { "card_instance_id", instanceId }
        };

        // Get modifiers from ModifierService
        var modifiers = GetModifiersFromService("unit", categories, modifierContext);

        // Get SpatialGrid autoload
        var spatialGrid = GetAutoloadNode("/root/SpatialGrid");

        // Get collision radius from a temp unit instance (all units from same scene have same radius)
        float collisionRadius = 0.5f;
        var tempUnit = unitScene.Instantiate() as Node3D;
        if (tempUnit != null)
        {
            var radiusVal = tempUnit.Get("CollisionRadius");
            if (radiusVal.VariantType != Variant.Type.Nil)
                collisionRadius = radiusVal.AsSingle();
            tempUnit.Free();  // Not in tree, use Free() not QueueFree()
        }
        if (collisionRadius <= 0) collisionRadius = 0.5f;

        // Pre-calculate all safe spawn positions BEFORE spawning any units
        // This ensures preview and actual spawn match exactly
        // Pass already-calculated positions to avoid units in same batch overlapping
        var safePositions = new List<Vector3>();
        for (int i = 0; i < spawnCount; i++)
        {
            var offset = formation.GetOffset(i, spawnCount);
            var desiredPos = position + offset;
            var safePos = FindSafeSpawnPosition(desiredPos, battlefield.GetTree(), collisionRadius, null, safePositions);
            safePositions.Add(safePos);
        }

        // Spawn units at pre-calculated positions
        for (int i = 0; i < spawnCount; i++)
        {
            var unit = unitScene.Instantiate() as Node3D;
            if (unit == null)
            {
                GD.PrintErr($"[CardFactory] Failed to instantiate unit for '{catalogId}'");
                continue;
            }

            // Set team
            unit.Set("Team", team);

            // Apply stats from effective stats (includes upgrades)
            unit.Set("MaxHp", GetFloat(effectiveStats, "max_hp", 100f));
            unit.Set("AttackDamage", GetFloat(effectiveStats, "attack_damage", 10f));
            unit.Set("AttackSpeed", GetFloat(effectiveStats, "attack_speed", 1f));
            unit.Set("MoveSpeed", GetFloat(effectiveStats, "move_speed", 3f));

            // Attack range is optional
            if (effectiveStats.ContainsKey("attack_range"))
            {
                unit.Set("AttackRange", GetFloat(effectiveStats, "attack_range", 1.5f));
            }

            // Apply custom stat overrides
            if (customOverrides.ContainsKey("scale_multiplier"))
            {
                var multiplier = GetFloat(customOverrides, "scale_multiplier", 1f);
                unit.Scale = Vector3.One * multiplier;
                GD.Print($"[CardFactory] Applied scale_multiplier {multiplier} to '{catalogId}'");
            }

            // Use pre-calculated safe position
            var safePos = safePositions[i];

            // Handle flight altitude for flying units
            var movementLayer = unit.Get("MovementLayer");
            if (movementLayer.VariantType == Variant.Type.Int &&
                movementLayer.AsInt32() == (int)MovementLayer.Air)
            {
                var flightAlt = unit.Get("FlightAltitude");
                if (flightAlt.VariantType == Variant.Type.Float || flightAlt.VariantType == Variant.Type.Int)
                {
                    safePos = new Vector3(safePos.X, flightAlt.AsSingle(), safePos.Z);
                }
            }

            // Set position BEFORE adding to tree (prevents jitter)
            unit.Position = safePos;

            // Add to tree - visual components handle their own visibility during init
            gameplayLayer.AddChild(unit);

            // Initialize with modifiers
            if (unit is Unit3D unit3d)
            {
                unit3d.InitializeWithModifiers(modifiers);
            }

            // Update SpatialGrid after unit is in tree
            if (spatialGrid != null && spatialGrid.HasMethod("update_unit_position"))
            {
                spatialGrid.Call("update_unit_position", unit);
            }

            // Start spawn reveal animation if duration specified
            bool hasSpawnAnimation = spawnDuration > 0.0f && unit.HasMethod("start_spawn_reveal");
            if (hasSpawnAnimation)
            {
                unit.Call("start_spawn_reveal", spawnDuration);
            }

            // Activate unit if in battle phase and no spawn animation
            if (!hasSpawnAnimation)
            {
                var gameController = gameplayLayer.GetTree().CurrentScene;
                if (gameController != null)
                {
                    var currentPhase = gameController.Get("current_phase");
                    if (currentPhase.VariantType == Variant.Type.Int &&
                        currentPhase.AsInt32() == BattlePhaseBattle)
                    {
                        unit.Call("Activate");
                    }
                }
            }
        }

        GD.Print($"[CardFactory] Spawned {spawnCount} units for '{catalogId}'");
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

    /// <summary>
    /// Create the appropriate FormationConfig from card definition.
    /// Uses formation_type field to determine which subclass to create.
    /// </summary>
    private static FormationConfig CreateFormationConfig(Godot.Collections.Dictionary cardDef)
    {
        var formationType = GetString(cardDef, "formation_type", "grid");
        float spacing = GetFloat(cardDef, "formation_spacing", GridFormation.DefaultSpacing);

        return formationType switch
        {
            "grouped_line" => new GroupedLineFormationConfig
            {
                Spacing = spacing,
                GroupSpacing = GetFloat(cardDef, "group_spacing", GroupedLineFormation.DefaultGroupSpacing),
                UnitsPerGroup = GetInt(cardDef, "units_per_group", GroupedLineFormation.DefaultUnitsPerGroup)
            },
            _ => new FormationConfig
            {
                Spacing = spacing,
                RowOffset = GetFloat(cardDef, "formation_row_offset", GridFormation.DefaultRowOffset)
            }
        };
    }

    // =========================================================================
    // FORMATION API (for GDScript preview)
    // =========================================================================

    /// <summary>
    /// Get formation offset for a unit. Called by GDScript Card class for spawn preview.
    /// This ensures preview and actual spawning use the same formation logic.
    /// </summary>
    /// <param name="cardDef">Card definition dictionary from CardCatalog.</param>
    /// <param name="unitIndex">Index of the unit in the formation (0-based).</param>
    /// <param name="totalUnits">Total number of units being spawned.</param>
    /// <returns>Position offset from spawn center for this unit.</returns>
    public Vector3 get_formation_offset(Godot.Collections.Dictionary cardDef, int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
            return Vector3.Zero;

        var formationConfig = CreateFormationConfig(cardDef);
        var formation = formationConfig.CreateFormation();
        return formation.GetOffset(unitIndex, totalUnits);
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
    /// Find a safe spawn position that doesn't overlap with existing units
    /// or other positions in the same spawn batch.
    /// </summary>
    /// <param name="desiredPos">The desired spawn position</param>
    /// <param name="tree">Scene tree for querying existing units</param>
    /// <param name="collisionRadius">Collision radius of the unit being spawned</param>
    /// <param name="excludeUnit">Optional unit to exclude from collision checks</param>
    /// <param name="batchPositions">Positions already calculated in this spawn batch</param>
    private static Vector3 FindSafeSpawnPosition(
        Vector3 desiredPos,
        SceneTree? tree,
        float collisionRadius,
        Node3D? excludeUnit,
        List<Vector3>? batchPositions = null)
    {
        // Check for overlaps and find safe position
        const float minSeparation = 0.1f;
        const int maxAttempts = 12;

        var testPos = desiredPos;

        // Get all existing units (may be null/empty for first spawn)
        Godot.Collections.Array<Node>? units = tree?.GetNodesInGroup("UNITS");

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            bool hasOverlap = false;

            // Check against existing units in scene
            if (units != null)
            {
                foreach (var node in units)
                {
                    if (node == excludeUnit)
                        continue;

                    if (node is Node3D otherUnit)
                    {
                        var otherRadius = otherUnit.Get("CollisionRadius");
                        float otherRad = otherRadius.VariantType != Variant.Type.Nil ? otherRadius.AsSingle() : 0.5f;

                        var diff = new Vector3(testPos.X - otherUnit.GlobalPosition.X, 0, testPos.Z - otherUnit.GlobalPosition.Z);
                        float dist = diff.Length();
                        float minDist = collisionRadius + otherRad + minSeparation;

                        if (dist < minDist)
                        {
                            hasOverlap = true;
                            // Push away from overlapping unit
                            if (dist > 0.001f)
                            {
                                testPos += diff.Normalized() * (minDist - dist + 0.1f);
                            }
                            else
                            {
                                // Units at same position, push in random direction
                                testPos += new Vector3(0.5f, 0, 0.5f);
                            }
                            break;
                        }
                    }
                }
            }

            // Check against other positions in the same spawn batch
            if (!hasOverlap && batchPositions != null)
            {
                foreach (var otherPos in batchPositions)
                {
                    var diff = new Vector3(testPos.X - otherPos.X, 0, testPos.Z - otherPos.Z);
                    float dist = diff.Length();
                    // Same collision radius for units in same batch
                    float minDist = collisionRadius * 2 + minSeparation;

                    if (dist < minDist)
                    {
                        hasOverlap = true;
                        // Push away from batch position
                        if (dist > 0.001f)
                        {
                            testPos += diff.Normalized() * (minDist - dist + 0.1f);
                        }
                        else
                        {
                            // Positions at same spot, push in random direction
                            testPos += new Vector3(0.5f, 0, 0.5f);
                        }
                        break;
                    }
                }
            }

            if (!hasOverlap)
                break;
        }

        return testPos;
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
