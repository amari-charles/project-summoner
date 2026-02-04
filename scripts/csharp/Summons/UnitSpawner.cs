using System.Collections.Generic;
using Godot;
using ProjectSummoner.Abilities;
using ProjectSummoner.Constants;
using ProjectSummoner.Stats;
using ProjectSummoner.Systems.Modifiers;
using ProjectSummoner.Units;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;

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
    /// Spawns a unit from a UnitDefinition with the given context.
    /// This is the preferred method - loads scene from definition and applies all configuration.
    /// </summary>
    /// <param name="definition">UnitDefinition containing all unit configuration</param>
    /// <param name="context">Spawn context with position, team, modifiers, etc.</param>
    /// <returns>Spawned Unit3D, or null if failed</returns>
    public static Unit3D? SpawnFromDefinition(UnitDefinition definition, UnitSpawnContext context)
    {
        var scene = GD.Load<PackedScene>(definition.ScenePath);
        if (scene == null)
        {
            GD.PrintErr($"[UnitSpawner] Failed to load scene: {definition.ScenePath}");
            return null;
        }

        var unit = scene.Instantiate() as Node3D;
        if (unit == null)
        {
            GD.PrintErr($"[UnitSpawner] Failed to instantiate unit from scene: {definition.ScenePath}");
            return null;
        }

        // Must be Unit3D for definition-based spawning
        if (unit is not Unit3D unit3d)
        {
            GD.PrintErr($"[UnitSpawner] Scene root is not Unit3D: {definition.ScenePath}");
            unit.Free();
            return null;
        }

        // Apply definition (sets all stats, targeting, visual config)
        unit3d.ApplyDefinition(definition);

        // Apply ranged config if applicable
        if (definition.Ranged != null && unit3d is RangedUnit3D rangedUnit)
        {
            rangedUnit.ApplyRangedDefinition(definition.Ranged);
        }

        // Set team
        unit3d.Team = context.Team;

        // Apply stat overrides from context (card modifiers, etc.)
        // Note: Stats from context override definition stats
        ApplyStatsToUnit(unit3d, context.Stats);

        // Apply non-stat custom overrides (scale_multiplier)
        if (context.CustomOverrides != null && context.CustomOverrides.ContainsKey("scale_multiplier"))
        {
            var multiplier = GetFloat(context.CustomOverrides, "scale_multiplier", 1f);
            unit3d.Scale = Vector3.One * multiplier;
        }

        // Add to tree first - C# exported properties are only accessible after tree entry
        context.GameplayLayer.AddChild(unit3d);

        // Calculate final position AFTER tree entry (now MovementLayer is accessible)
        var finalPos = CalculateFinalPosition(unit3d, context.Position);
        unit3d.Position = finalPos;

        // Instantiate and attach abilities from definition
        foreach (var abilityConfig in definition.Abilities)
        {
            var ability = abilityConfig.CreateAbility();
            unit3d.AddChild(ability);
            // Note: abilities are auto-discovered and Setup() called in Unit3D._Ready()
        }

        // Apply modifiers (partitioned into static and triggered)
        var modifiers = context.Modifiers ?? [];
        var staticMods = new List<StatModifier>();
        var triggeredMods = new List<StatModifier>();

        foreach (var mod in modifiers)
        {
            if (mod.IsTriggered)
                triggeredMods.Add(mod);
            else
                staticMods.Add(mod);
        }

        if (triggeredMods.Count > 0)
        {
            unit3d.InitializeWithPartitionedModifiers(staticMods, triggeredMods);
        }
        else
        {
            unit3d.InitializeWithModifiers(staticMods);
        }

        // Update SpatialGrid after unit is in tree
        if (context.SpatialGrid != null && context.SpatialGrid.HasMethod("update_unit_position"))
        {
            context.SpatialGrid.Call("update_unit_position", unit3d);
        }

        // Start spawn reveal animation if duration specified
        bool hasSpawnAnimation = context.SpawnDuration > 0.0f && unit3d.HasMethod("start_spawn_reveal");
        if (hasSpawnAnimation)
        {
            unit3d.Call("start_spawn_reveal", context.SpawnDuration);
        }

        // Activate unit if in battle phase and no spawn animation
        if (!hasSpawnAnimation && context.InBattlePhase)
        {
            unit3d.Activate();
        }

        // Register with multiplayer system (assigns NetworkId if host)
        RegisterWithMultiplayerSystem(unit3d, definition.Id.Value, context.Team);

        return unit3d;
    }

    /// <summary>
    /// Spawns a unit from a PackedScene with the given context.
    /// Legacy method for backwards compatibility - prefer SpawnFromDefinition.
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

        // Register with multiplayer system (assigns NetworkId if host)
        if (unit3d != null)
        {
            RegisterWithMultiplayerSystem(unit3d, unit3d.UnitId, context.Team);
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
    /// Gets the separation radius for a unit from UnitDefinitions.
    /// This is the preferred method - reads directly from definition without scene instantiation.
    /// </summary>
    /// <param name="unitId">Unit ID to look up</param>
    /// <returns>Separation radius from definition, or default if not found</returns>
    public static float GetSeparationRadiusFromDefinition(UnitId unitId)
    {
        if (UnitDefinitions.TryGet(unitId, out var def) && def != null)
        {
            return def.Visual.SeparationRadius;
        }
        return DefaultCollisionRadius;
    }

    /// <summary>
    /// Gets the separation radius for a unit scene, using a cache to avoid repeated instantiation.
    /// Legacy method - prefer GetSeparationRadiusFromDefinition.
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

    /// <summary>
    /// Registers a spawned unit with the multiplayer system if in a multiplayer match.
    /// Host: Assigns NetworkId, registers with NetworkIdRegistry, broadcasts UnitSpawned.
    /// Client: Should receive UnitSpawned from host and register with provided NetworkId.
    /// </summary>
    /// <param name="unit">The unit that was spawned</param>
    /// <param name="unitType">Unit type identifier (e.g., "puff", "fire_wisp")</param>
    /// <param name="team">Team the unit belongs to</param>
    private static void RegisterWithMultiplayerSystem(Unit3D unit, string unitType, int team)
    {
        var session = MatchSession.Current;
        if (session == null || !session.IsActive)
        {
            // Not in a multiplayer match - nothing to register
            return;
        }

        if (session.IsHost)
        {
            // Host assigns network ID and broadcasts to clients
            var networkId = session.NetworkIds.Register(unit);
            unit.NetworkId = networkId;

            // Broadcast UnitSpawned message to clients
            var spawnMessage = new UnitSpawned(
                networkId,
                unitType,
                team,
                unit.GlobalPosition
            );
            session.Broadcast(spawnMessage);

            GD.Print($"[UnitSpawner] Registered unit with NetworkId {networkId}: {unitType} (team {team})");
        }
        else
        {
            // Client should receive NetworkId from host via UnitSpawned message
            // For now, log that we're in client mode - full client spawn handling
            // will be implemented when we process UnitSpawned messages from host
            GD.Print($"[UnitSpawner] Client spawned unit locally: {unitType} (team {team}) - awaiting NetworkId from host");
        }
    }
}
