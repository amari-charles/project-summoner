using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards.Effects.Core;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards.Effects.Concrete;

/// <summary>
/// Spell effect that issues tactical commands to friendly units.
/// Supports Rally (move to point), Guard (defensive formation), and Charge (focus fire).
/// </summary>
public class CommandEffect : SpellEffect
{
    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>
    /// Type of command to issue.
    /// </summary>
    public CommandType Command { get; set; }

    /// <summary>
    /// Duration for Guard/Charge commands (seconds).
    /// </summary>
    public float CommandDuration { get; set; } = 25f;

    /// <summary>
    /// Optional destination override for two-stage targeting.
    /// If set, used instead of spell position for Rally/Guard/Charge destination.
    /// </summary>
    public Vector3? DestinationOverride { get; set; }

    // =========================================================================
    // VFX
    // =========================================================================

    /// <summary>
    /// VFX ID for rally circle.
    /// </summary>
    public string RallyVFXId { get; set; } = "rally_circle";

    /// <summary>
    /// VFX ID for guard markers.
    /// </summary>
    public string GuardVFXId { get; set; } = "guard_marker";

    /// <summary>
    /// VFX ID for charge marker.
    /// </summary>
    public string ChargeVFXId { get; set; } = "charge_marker";

    /// <summary>
    /// VFX ID for failed cast (no targets).
    /// </summary>
    public string FizzleVFXId { get; set; } = "spell_fizzle";

    // =========================================================================
    // CONSTANTS (Formation)
    // =========================================================================

    private const float FormationBaseRadius = 2.0f;
    private const float FormationUnitSpacing = 1.5f;

    // =========================================================================
    // EXECUTION
    // =========================================================================

    public override void Execute(SpellContext context)
    {
        // Command effects target allies
        Affinity = Affinity.Allies;

        // Get target units
        var units = GetTargets(context)
            .Where(t => IsAlive(t))
            .ToList();

        // No targets = fizzle
        if (units.Count == 0)
        {
            PlayFizzleVFX(context);
            return;
        }

        // Check for two-stage targeting destination override
        var destination = DestinationOverride ?? CheckSpellTargetingManager(context) ?? context.Position;

        // Execute command
        switch (Command)
        {
            case CommandType.Rally:
                ApplyRally(units, destination, context);
                break;
            case CommandType.Guard:
                ApplyGuard(units, destination, context);
                break;
            case CommandType.Charge:
                ApplyCharge(units, destination, context);
                break;
        }

        // Execute completion hook
        ExecuteOnComplete(context);
    }

    // =========================================================================
    // COMMAND IMPLEMENTATIONS
    // =========================================================================

    /// <summary>
    /// Apply Rally command - units move to the rally point.
    /// </summary>
    private void ApplyRally(List<Node3D> units, Vector3 rallyPoint, SpellContext context)
    {
        foreach (var unit in units)
        {
            unit.Set("rally_point", rallyPoint);
            unit.Set("rally_mode", true);
        }

        PlayRallyVFX(rallyPoint, context);
        GD.Print($"[CommandEffect] Rally applied to {units.Count} units at {rallyPoint}");
    }

    /// <summary>
    /// Apply Guard command - units form defensive formation at the position.
    /// </summary>
    private void ApplyGuard(List<Node3D> units, Vector3 center, SpellContext context)
    {
        // Separate melee and ranged
        var meleeUnits = new List<Node3D>();
        var rangedUnits = new List<Node3D>();

        foreach (var unit in units)
        {
            var unitTypeVar = unit.Get("unit_type");
            if (unitTypeVar.VariantType != Variant.Type.Nil &&
                unitTypeVar.AsInt32() == (int)UnitType.Ranged)
            {
                rangedUnits.Add(unit);
            }
            else
            {
                meleeUnits.Add(unit);
            }
        }

        // Calculate formation radii
        float frontRadius = FormationBaseRadius + (meleeUnits.Count * FormationUnitSpacing / Mathf.Pi);
        float backRadius = frontRadius + 2.0f;

        // Position melee in front arc (180 degrees facing forward)
        for (int i = 0; i < meleeUnits.Count; i++)
        {
            float t = meleeUnits.Count > 1 ? (float)i / (meleeUnits.Count - 1) : 0.5f;
            float angle = Mathf.Pi + t * Mathf.Pi - Mathf.Pi / 2;
            var pos = center + new Vector3(
                Mathf.Cos(angle) * frontRadius,
                0,
                Mathf.Sin(angle) * frontRadius
            );

            var unit = meleeUnits[i];
            unit.Set("formation_position", pos);
            unit.Set("guard_mode", true);
            unit.Set("guard_timer", CommandDuration);
        }

        // Position ranged in back arc
        for (int i = 0; i < rangedUnits.Count; i++)
        {
            float t = rangedUnits.Count > 1 ? (float)i / (rangedUnits.Count - 1) : 0.5f;
            float angle = Mathf.Pi + t * Mathf.Pi - Mathf.Pi / 2;
            var pos = center + new Vector3(
                Mathf.Cos(angle) * backRadius,
                0,
                Mathf.Sin(angle) * backRadius
            );

            var unit = rangedUnits[i];
            unit.Set("formation_position", pos);
            unit.Set("guard_mode", true);
            unit.Set("guard_timer", CommandDuration);
        }

        PlayGuardVFX(units, context);
        GD.Print($"[CommandEffect] Guard formation applied to {units.Count} units at {center}");
    }

    /// <summary>
    /// Apply Charge command - units focus fire on nearest enemy to the destination.
    /// </summary>
    private void ApplyCharge(List<Node3D> units, Vector3 chargeDestination, SpellContext context)
    {
        // Find closest enemy to charge destination
        var closestEnemy = FindNearestEnemy(chargeDestination, context);

        if (closestEnemy == null)
        {
            PlayFizzleVFX(context);
            GD.Print("[CommandEffect] Charge failed - no enemies found");
            return;
        }

        // Set forced target for all units
        foreach (var unit in units)
        {
            unit.Set("forced_target", closestEnemy);
            unit.Set("forced_target_timer", CommandDuration);
            unit.Set("original_redirect_point", chargeDestination);
        }

        PlayChargeVFX(chargeDestination, context);
        GD.Print($"[CommandEffect] Charge applied to {units.Count} units targeting {closestEnemy.Name}");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Check if unit is alive.
    /// </summary>
    private static bool IsAlive(Node3D unit)
    {
        var aliveVar = unit.Get("IsAlive");
        if (aliveVar.VariantType == Variant.Type.Nil)
            aliveVar = unit.Get("is_alive");

        if (aliveVar.VariantType != Variant.Type.Nil)
        {
            return aliveVar.AsBool();
        }

        return true;
    }

    /// <summary>
    /// Check SpellTargetingManager for two-stage targeting destination.
    /// </summary>
    private static Vector3? CheckSpellTargetingManager(SpellContext context)
    {
        var stm = context.SceneTree?.Root?.GetNodeOrNull("/root/SpellTargetingManager");
        if (stm == null) return null;

        var destVar = stm.Call("get_rally_destination");
        if (destVar.VariantType == Variant.Type.Nil) return null;

        var dest = destVar.AsVector3();
        if (dest == Vector3.Zero) return null;

        // Clear the destination after reading
        stm.Call("clear_rally_destination");

        return dest;
    }

    /// <summary>
    /// Find nearest enemy to a position using RedirectManager.
    /// </summary>
    private static Node3D FindNearestEnemy(Vector3 position, SpellContext context)
    {
        var redirectManager = context.SceneTree?.Root?.GetNodeOrNull("/root/RedirectManager");
        if (redirectManager == null)
        {
            GD.PrintErr("[CommandEffect] RedirectManager not found");
            return null;
        }

        var result = redirectManager.Call("find_nearest_enemy",
            position,
            (int)context.Team,
            float.PositiveInfinity // Search entire battlefield
        );

        return result.As<Node3D>();
    }

    // =========================================================================
    // VFX
    // =========================================================================

    private void PlayRallyVFX(Vector3 position, SpellContext context)
    {
        var vfxManager = GetVFXManager(context);
        if (vfxManager != null && HasEffect(vfxManager, RallyVFXId))
        {
            vfxManager.Call("play_effect", RallyVFXId, position,
                new Godot.Collections.Dictionary { { "radius", 2.0f } });
        }
    }

    private void PlayGuardVFX(List<Node3D> units, SpellContext context)
    {
        var vfxManager = GetVFXManager(context);
        if (vfxManager != null && HasEffect(vfxManager, GuardVFXId))
        {
            foreach (var unit in units)
            {
                var posVar = unit.Get("formation_position");
                if (posVar.VariantType != Variant.Type.Nil)
                {
                    vfxManager.Call("play_effect", GuardVFXId, posVar.AsVector3());
                }
            }
        }
    }

    private void PlayChargeVFX(Vector3 position, SpellContext context)
    {
        var vfxManager = GetVFXManager(context);
        if (vfxManager != null && HasEffect(vfxManager, ChargeVFXId))
        {
            vfxManager.Call("play_effect", ChargeVFXId, position,
                new Godot.Collections.Dictionary { { "radius", 2.0f } });
        }
    }

    private void PlayFizzleVFX(SpellContext context)
    {
        var vfxManager = GetVFXManager(context);
        if (vfxManager != null && HasEffect(vfxManager, FizzleVFXId))
        {
            vfxManager.Call("play_effect", FizzleVFXId, context.Position);
        }
    }

    private static Node GetVFXManager(SpellContext context)
    {
        return context.SceneTree?.Root?.GetNodeOrNull("/root/VFXManager");
    }

    private static bool HasEffect(Node vfxManager, string effectId)
    {
        if (!vfxManager.HasMethod("has_effect")) return false;
        return vfxManager.Call("has_effect", effectId).AsBool();
    }
}

/// <summary>
/// Types of tactical commands.
/// </summary>
public enum CommandType
{
    /// <summary>Units move to the target position.</summary>
    Rally,

    /// <summary>Units form defensive formation at the target position.</summary>
    Guard,

    /// <summary>Units focus fire on nearest enemy to the target position.</summary>
    Charge
}
