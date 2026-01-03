using Godot;
using System.Collections.Generic;
using ProjectSummoner.Combat;
using ProjectSummoner.Units;

namespace ProjectSummoner.Abilities;

/// <summary>
/// Abstract base class for all unit abilities.
///
/// Abilities are components that attach to units and modify their behavior.
/// They react to unit events (attack, movement, death) to trigger special effects.
///
/// Usage:
///   1. Extend this class for custom abilities
///   2. Override Initialize() for setup
///   3. Override ConnectToUnitEvents() to listen to unit signals
///   4. Add as child node to unit in scene or via code
/// </summary>
[GlobalClass]
public abstract partial class BaseAbility : Node
{
    // =========================================================================
    // STATE
    // =========================================================================

    /// <summary>Reference to the unit this ability is attached to.</summary>
    protected Unit3D? OwnerUnit { get; private set; }

    /// <summary>Whether this ability is currently active.</summary>
    public bool IsActive { get; protected set; } = true;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    /// <summary>
    /// Called when ability is added to a unit.
    /// Do not override - use Initialize() instead.
    /// </summary>
    public void Setup(Unit3D unit)
    {
        if (unit == null)
        {
            GD.PushError("BaseAbility.Setup: unit is null");
            return;
        }

        OwnerUnit = unit;
        ConnectToUnitEvents();
        Initialize();

        string scriptName = GetType().Name;
        GD.Print($"BaseAbility: {scriptName} setup complete for unit {unit.Name}");
    }

    /// <summary>
    /// Override in subclasses for custom initialization.
    /// Called after OwnerUnit is set and events are connected.
    /// </summary>
    protected virtual void Initialize()
    {
    }

    /// <summary>
    /// Override in subclasses to connect to unit signals.
    /// Example: OwnerUnit.UnitDied += OnOwnerDied;
    /// </summary>
    protected virtual void ConnectToUnitEvents()
    {
    }

    // =========================================================================
    // ABILITY CONTROL
    // =========================================================================

    /// <summary>Enable this ability.</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Disable this ability.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>Toggle ability on/off.</summary>
    public void Toggle()
    {
        IsActive = !IsActive;
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Get units within radius of a position.
    /// </summary>
    protected List<Unit3D> GetUnitsInRadius(
        Vector3 center,
        float radius,
        bool targetEnemies = true,
        bool targetAllies = false,
        bool includeSelf = false)
    {
        var units = new List<Unit3D>();

        if (OwnerUnit == null)
            return units;

        // Get GroupIDs autoload for group name lookup
        var groupIds = GetNodeOrNull("/root/GroupIDs");
        if (groupIds == null)
        {
            GD.PushWarning("BaseAbility: GroupIDs autoload not found");
            return units;
        }

        // Determine which groups to check
        var groupsToCheck = new List<string>();
        if (targetEnemies)
        {
            var enemyGroup = groupIds.Call("enemy_units_for", (int)OwnerUnit.Team).AsString();
            groupsToCheck.Add(enemyGroup);
        }
        if (targetAllies)
        {
            var allyGroup = groupIds.Call("ally_units_for", (int)OwnerUnit.Team).AsString();
            groupsToCheck.Add(allyGroup);
        }

        // Find units in range
        foreach (var group in groupsToCheck)
        {
            var groupUnits = GetTree().GetNodesInGroup(group);
            foreach (var node in groupUnits)
            {
                if (node is Unit3D unit && unit.IsAlive)
                {
                    float distance = unit.GlobalPosition.DistanceTo(center);
                    if (distance <= radius)
                    {
                        if (unit != OwnerUnit || includeSelf)
                        {
                            units.Add(unit);
                        }
                    }
                }
            }
        }

        return units;
    }

    /// <summary>
    /// Apply damage to a target via DamageSystem.
    /// </summary>
    protected void ApplyDamage(Unit3D target, float damage, string damageType = "physical")
    {
        if (OwnerUnit == null || target == null)
            return;

        if (DamageSystem.Instance != null)
        {
            DamageSystem.Instance.ApplyDamage(OwnerUnit, target, damage, damageType);
        }
        else
        {
            // Fallback: direct damage application
            target.TakeDamage(damage, damageType);
        }
    }

    /// <summary>
    /// Spawn VFX at position via VFXManager.
    /// </summary>
    protected Node? SpawnVfx(string vfxId, Vector3 position, Node? parent = null)
    {
        if (string.IsNullOrEmpty(vfxId))
            return null;

        var vfxManager = GetNodeOrNull("/root/VFXManager");
        if (vfxManager == null)
        {
            GD.PushWarning("BaseAbility: VFXManager autoload not found");
            return null;
        }

        var vfx = vfxManager.Call("play_effect", vfxId, position).AsGodotObject() as Node;

        if (vfx != null && parent != null)
        {
            vfx.Reparent(parent);
            if (vfx is Node3D vfx3D)
            {
                vfx3D.Position = Vector3.Zero;
            }
        }

        return vfx;
    }
}
