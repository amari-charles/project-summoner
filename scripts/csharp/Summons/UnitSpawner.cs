using System.Collections.Generic;
using Godot;
using ProjectSummoner.Constants;
using ProjectSummoner.Stats;
using ProjectSummoner.Systems.Modifiers;
using ProjectSummoner.Units;

namespace ProjectSummoner.Summons;

/// <summary>
/// Context for spawning a unit. Contains all information needed to spawn and configure a unit.
/// </summary>
public class UnitSpawnContext
{
    /// <summary>Position to spawn the unit at.</summary>
    public Vector3 Position { get; init; }

    /// <summary>Team the unit belongs to.</summary>
    public int Team { get; init; }

    /// <summary>Calculated stats for the unit.</summary>
    public UnitStats Stats { get; init; } = UnitStats.Default;

    /// <summary>Modifiers to apply to the unit.</summary>
    public List<StatModifier>? Modifiers { get; init; }

    /// <summary>Custom overrides dictionary (for scale_multiplier, etc.).</summary>
    public Godot.Collections.Dictionary? CustomOverrides { get; init; }

    /// <summary>Node to add the spawned unit to.</summary>
    public Node GameplayLayer { get; init; } = null!;

    /// <summary>SpatialGrid autoload for position updates (optional).</summary>
    public Node? SpatialGrid { get; init; }

    /// <summary>Duration for spawn reveal animation (0 = no animation).</summary>
    public float SpawnDuration { get; init; }

    /// <summary>Whether the game is currently in battle phase.</summary>
    public bool InBattlePhase { get; init; }
}

/// <summary>
/// Handles spawning individual units from a PackedScene.
/// Configures team, stats, position, modifiers, and handles spawn animation/activation.
///
/// IMPORTANT: All spawnable unit scenes must use Unit3D as their root node.
/// Non-Unit3D nodes will be spawned but cannot be tracked by UnitSummon.
/// </summary>
public static class UnitSpawner
{
    // Default collision radius when unit scene doesn't specify one.
    // 0.5 units is a reasonable default for standard-sized units.
    private const float DefaultCollisionRadius = 0.5f;

    // Cache for separation radius values by scene path.
    // Avoids instantiating scenes repeatedly just to read a property.
    private static readonly Dictionary<string, float> _separationRadiusCache = new();

    /// <summary>
    /// Spawns a unit from a PackedScene with the given context.
    /// Returns the spawned unit, or null if spawning failed.
    /// </summary>
    /// <param name="unitScene">PackedScene to instantiate</param>
    /// <param name="context">Spawn context with configuration</param>
    /// <returns>Spawned Unit3D, or null if failed</returns>
    public static Unit3D? SpawnUnit(PackedScene unitScene, UnitSpawnContext context)
    {
        var unit = unitScene.Instantiate() as Node3D;
        if (unit == null)
        {
            GD.PrintErr("[UnitSpawner] Failed to instantiate unit from scene");
            return null;
        }

        // Set team
        unit.Set("Team", context.Team);

        // Apply stats
        ApplyStatsToUnit(unit, context.Stats);

        // Apply non-stat custom overrides (scale_multiplier)
        if (context.CustomOverrides != null && context.CustomOverrides.ContainsKey("scale_multiplier"))
        {
            var multiplier = GetFloat(context.CustomOverrides, "scale_multiplier", 1f);
            unit.Scale = Vector3.One * multiplier;
        }

        // Add to tree first - C# exported properties (MovementLayer, FlightAltitude)
        // are only accessible after the node enters the scene tree
        context.GameplayLayer.AddChild(unit);

        // Calculate final position AFTER tree entry (now MovementLayer is accessible)
        var finalPos = CalculateFinalPosition(unit, context.Position);
        unit.Position = finalPos;

        // Initialize with modifiers if it's a Unit3D
        Unit3D? unit3d = null;
        if (unit is Unit3D u3d)
        {
            unit3d = u3d;

            // Partition modifiers into static (always active) and triggered (conditional)
            var modifiers = context.Modifiers ?? new List<StatModifier>();
            var staticMods = new List<StatModifier>();
            var triggeredMods = new List<StatModifier>();

            foreach (var mod in modifiers)
            {
                if (mod.IsTriggered)
                    triggeredMods.Add(mod);
                else
                    staticMods.Add(mod);
            }

            // Use partitioned initialization if there are triggered modifiers
            if (triggeredMods.Count > 0)
            {
                u3d.InitializeWithPartitionedModifiers(staticMods, triggeredMods);
            }
            else
            {
                u3d.InitializeWithModifiers(staticMods);
            }
        }
        else
        {
            GD.PushWarning($"[UnitSpawner] Spawned unit is not Unit3D - cannot be tracked by UnitSummon. Scene root should inherit from Unit3D.");
        }

        // Update SpatialGrid after unit is in tree
        if (context.SpatialGrid != null && context.SpatialGrid.HasMethod("update_unit_position"))
        {
            context.SpatialGrid.Call("update_unit_position", unit);
        }

        // Start spawn reveal animation if duration specified
        bool hasSpawnAnimation = context.SpawnDuration > 0.0f && unit.HasMethod("start_spawn_reveal");
        if (hasSpawnAnimation)
        {
            unit.Call("start_spawn_reveal", context.SpawnDuration);
        }

        // Activate unit if in battle phase and no spawn animation
        if (!hasSpawnAnimation && context.InBattlePhase)
        {
            unit.Call("Activate");
        }

        return unit3d;
    }

    /// <summary>
    /// Gets the collision radius from a unit scene by instantiating a temp instance.
    /// </summary>
    /// <param name="unitScene">Scene to check</param>
    /// <returns>Separation radius, defaults to DefaultCollisionRadius</returns>
    public static float GetSeparationRadius(PackedScene unitScene)
    {
        float separationRadius = DefaultCollisionRadius;
        var tempUnit = unitScene.Instantiate() as Node3D;
        if (tempUnit != null)
        {
            var radiusVal = tempUnit.Get("SeparationRadius");
            if (radiusVal.VariantType != Variant.Type.Nil)
                separationRadius = radiusVal.AsSingle();
            tempUnit.Free(); // Not in tree, use Free() not QueueFree()
        }
        return separationRadius <= 0 ? DefaultCollisionRadius : separationRadius;
    }

    /// <summary>
    /// Gets the separation radius for a unit scene, using a cache to avoid repeated instantiation.
    /// This is used by CardCatalog to include separation_radius in card dictionaries.
    /// </summary>
    /// <param name="scenePath">Resource path to the unit scene (e.g., "res://scenes/units/fire_wisp.tscn")</param>
    /// <returns>Separation radius, defaults to DefaultCollisionRadius</returns>
    public static float GetSeparationRadiusCached(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
            return DefaultCollisionRadius;

        // Check cache first
        if (_separationRadiusCache.TryGetValue(scenePath, out float cached))
            return cached;

        // Load scene and extract radius
        var scene = GD.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            _separationRadiusCache[scenePath] = DefaultCollisionRadius;
            return DefaultCollisionRadius;
        }

        float radius = GetSeparationRadius(scene);
        _separationRadiusCache[scenePath] = radius;
        return radius;
    }

    /// <summary>
    /// Clears the separation radius cache. Useful for testing or when scenes change.
    /// </summary>
    public static void ClearSeparationRadiusCache()
    {
        _separationRadiusCache.Clear();
    }

    /// <summary>
    /// Applies UnitStats to a unit node.
    /// </summary>
    private static void ApplyStatsToUnit(Node3D unit, UnitStats stats)
    {
        unit.Set("MaxHp", stats.MaxHp);
        unit.Set("AttackDamage", stats.AttackDamage);
        unit.Set("AttackSpeed", stats.AttackSpeed);
        unit.Set("MoveSpeed", stats.MoveSpeed);
        unit.Set("AttackRange", stats.AttackRange);

        // AggroRadius may not be a direct property on all units
        if (unit is Unit3D)
        {
            unit.Set("AggroRadius", stats.AggroRadius);
        }
    }

    /// <summary>
    /// Calculates final spawn position, adjusting for flight altitude if needed.
    /// Uses Unit3D.GetSpawnAltitude() as single source of truth.
    /// </summary>
    private static Vector3 CalculateFinalPosition(Node3D unit, Vector3 basePosition)
    {
        if (unit is Unit3D unit3d)
        {
            float spawnY = unit3d.GetSpawnAltitude();
            return new Vector3(basePosition.X, spawnY, basePosition.Z);
        }
        return basePosition;
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
