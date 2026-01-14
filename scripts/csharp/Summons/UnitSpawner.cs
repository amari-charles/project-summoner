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
            u3d.InitializeWithModifiers(context.Modifiers ?? new List<StatModifier>());
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
    /// <returns>Collision radius, defaults to DefaultCollisionRadius</returns>
    public static float GetCollisionRadius(PackedScene unitScene)
    {
        float collisionRadius = DefaultCollisionRadius;
        var tempUnit = unitScene.Instantiate() as Node3D;
        if (tempUnit != null)
        {
            var radiusVal = tempUnit.Get("CollisionRadius");
            if (radiusVal.VariantType != Variant.Type.Nil)
                collisionRadius = radiusVal.AsSingle();
            tempUnit.Free(); // Not in tree, use Free() not QueueFree()
        }
        return collisionRadius <= 0 ? DefaultCollisionRadius : collisionRadius;
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
    /// </summary>
    private static Vector3 CalculateFinalPosition(Node3D unit, Vector3 basePosition)
    {
        var movementLayer = unit.Get("MovementLayer");
        if (movementLayer.VariantType == Variant.Type.Int &&
            movementLayer.AsInt32() == (int)MovementLayer.Air)
        {
            var flightAlt = unit.Get("FlightAltitude");
            if (flightAlt.VariantType == Variant.Type.Float || flightAlt.VariantType == Variant.Type.Int)
            {
                return new Vector3(basePosition.X, flightAlt.AsSingle(), basePosition.Z);
            }
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
