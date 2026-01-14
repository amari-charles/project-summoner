using System.Linq;
using Godot;
using ProjectSummoner.Cards.Effects.Core;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Constants;
using ProjectSummoner.Projectiles;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards.Effects.Concrete;

/// <summary>
/// Spell effect that deals damage to targets in an area.
/// Supports VFX, projectiles, and event hooks (OnHit, OnKill).
/// </summary>
public class DamageEffect : SpellEffect
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>
    /// Height above ground for projectile flight path.
    /// Set to 1.5f to match typical unit center height (most units are ~2-3 units tall).
    /// Prevents ground collision during travel and ensures projectiles originate from
    /// a visually appropriate height near the caster's "hands" level.
    /// </summary>
    private const float ProjectileFlightHeight = 1.5f;

    // =========================================================================
    // DAMAGE CONFIG
    // =========================================================================

    /// <summary>
    /// Base damage to deal to each target.
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// Type of damage (e.g., "physical", "fire", "frost").
    /// </summary>
    public string DamageType { get; set; } = "spell";

    // =========================================================================
    // VFX/PROJECTILE CONFIG
    // =========================================================================

    /// <summary>
    /// VFX effect ID to play at cast position.
    /// Uses VFXManager.play_effect() via GDScript interop.
    /// </summary>
    public string? VFXId { get; set; }

    /// <summary>
    /// Projectile ID to spawn (if spell uses a projectile instead of instant damage).
    /// Uses ProjectileService.SpawnProjectile() for projectile spawning.
    /// </summary>
    public string? ProjectileId { get; set; }

    // =========================================================================
    // EVENT HOOKS
    // =========================================================================

    /// <summary>
    /// Effect to execute for each unit hit by this damage.
    /// Context position is set to the hit unit's position.
    /// </summary>
    public ISpellEffect? OnHit { get; set; }

    /// <summary>
    /// Effect to execute when a unit is killed by this damage.
    /// Context position is set to the killed unit's position.
    /// </summary>
    public ISpellEffect? OnKill { get; set; }

    // =========================================================================
    // EXECUTION
    // =========================================================================

    public override void Execute(SpellContext context)
    {
        // If projectile specified, spawn it instead of instant damage
        if (!string.IsNullOrEmpty(ProjectileId))
        {
            SpawnProjectile(context);
            return;
        }

        // If VFX specified, play it (VFX may handle damage application itself)
        if (!string.IsNullOrEmpty(VFXId))
        {
            PlayVFX(context);
            // If VFX handles damage, we're done
            // Otherwise fall through to apply damage
        }

        // Apply damage to all targets
        ApplyDamage(context);

        // Execute completion hook
        ExecuteOnComplete(context);
    }

    /// <summary>
    /// Apply damage to all valid targets.
    /// </summary>
    private void ApplyDamage(SpellContext context)
    {
        var targets = GetTargets(context);

        foreach (var target in targets)
        {
            var wasAlive = IsAlive(target);
            if (!wasAlive) continue;

            // Apply damage
            DealDamageToTarget(target, Damage);

            // Execute OnHit hook
            OnHit?.Execute(context.WithPosition(target.GlobalPosition));

            // Check for kill and execute OnKill hook
            var isAliveAfter = IsAlive(target);
            if (wasAlive && !isAliveAfter)
            {
                OnKill?.Execute(context.WithPosition(target.GlobalPosition));
            }
        }
    }

    /// <summary>
    /// Deal damage to a single target.
    /// </summary>
    private void DealDamageToTarget(Node3D target, float damage)
    {
        // Try C# interface first
        if (target is IDamageable damageable)
        {
            if (!string.IsNullOrEmpty(DamageType) && DamageType != "physical")
            {
                damageable.TakeDamage(damage, DamageType);
            }
            else
            {
                damageable.TakeDamage(damage);
            }
            return;
        }

        // GDScript interop (Unit3D.TakeDamage or take_damage)
        if (target.HasMethod("TakeDamage"))
        {
            target.Call("TakeDamage", damage);
        }
        else if (target.HasMethod("take_damage"))
        {
            target.Call("take_damage", damage);
        }
    }

    /// <summary>
    /// Spawn a projectile using ProjectileService.
    /// </summary>
    private void SpawnProjectile(SpellContext context)
    {
        if (ProjectileService.Instance == null)
        {
            GD.PrintErr("[DamageEffect] ProjectileService not found");
            // Fallback to instant damage
            ApplyDamage(context);
            return;
        }

        // Find source base for projectile origin
        var source = FindBaseByTeam(context);
        var startPos = source?.GlobalPosition ?? context.Position;
        // Raise start position above ground to avoid ground collision
        startPos.Y = ProjectileFlightHeight;

        // Find target position - use first targeted unit if available, else click position
        var targets = GetTargets(context);
        var targetUnit = targets.FirstOrDefault();
        Vector3 targetPos;

        if (targetUnit != null)
        {
            // Target the unit's position
            targetPos = targetUnit.GlobalPosition;
        }
        else
        {
            // Fallback to click position
            targetPos = context.Position;
        }
        // Target at same height for straight-line travel
        targetPos.Y = ProjectileFlightHeight;

        // ProjectileId is guaranteed non-null here since we check before calling SpawnProjectile
        var projectileIdValue = ProjectileId ?? "";

        // Spawn projectile via ProjectileService
        var options = new Godot.Collections.Dictionary
        {
            { "start_position", startPos },
            { "target_position", targetPos }
        };

        ProjectileService.Instance.SpawnProjectile(
            projectileIdValue,
            source ?? (context.Battlefield as Node3D)!,
            targetUnit,
            Damage,
            "spell",
            options
        );

        // Note: OnHit/OnKill hooks would need to be wired into projectile system
        // That's a future enhancement
    }

    /// <summary>
    /// Play VFX using VFXManager.
    /// </summary>
    private void PlayVFX(SpellContext context)
    {
        var vfxManager = GetVFXManager(context);
        if (vfxManager == null)
        {
            GD.PrintErr("[DamageEffect] VFXManager not found");
            return;
        }

        // Get radius from targeting if it's a CircleTargeting
        // Default fallback used when targeting isn't CircleTargeting (shouldn't happen for AOE spells)
        const float DefaultVFXRadius = 10f;
        float radius = DefaultVFXRadius;
        if (Targeting is Targeting.CircleTargeting circleTargeting)
        {
            radius = circleTargeting.Radius;
        }

        // VFXId is guaranteed non-null here since we check before calling PlayVFX
        var vfxIdValue = VFXId ?? "";

        // Build VFX parameters dictionary, handling potential null battlefield
        var vfxParams = new Godot.Collections.Dictionary
        {
            { "radius", radius },
            { "damage", Damage },
            { "team", (int)context.Team }
        };
        if (context.Battlefield != null)
        {
            vfxParams["battlefield"] = context.Battlefield;
        }

        vfxManager.Call("play_effect", vfxIdValue, context.Position, vfxParams);
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Get VFXManager autoload.
    /// </summary>
    private static Node? GetVFXManager(SpellContext context)
    {
        return context.SceneTree?.Root?.GetNodeOrNull("/root/VFXManager");
    }

    /// <summary>
    /// Find the base node for the caster's team (for projectile origin).
    /// Returns null if no base found (caller should handle fallback).
    /// </summary>
    private static Node3D? FindBaseByTeam(SpellContext context)
    {
        if (context.SceneTree == null) return null;

        var bases = context.SceneTree.GetNodesInGroup(GroupIDs.Bases);
        foreach (var baseNode in bases)
        {
            if (baseNode is not Node3D node3D) continue;

            var teamVar = node3D.Get("team");
            if (teamVar.VariantType != Variant.Type.Nil)
            {
                if ((Team)teamVar.AsInt32() == context.Team)
                {
                    return node3D;
                }
            }
        }

        // Fallback to battlefield node (may be null)
        return context.Battlefield as Node3D;
    }
}
