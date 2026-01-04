using Godot;
using ProjectSummoner.Cards.Effects.Core;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards.Effects.Concrete;

/// <summary>
/// Spell effect that deals damage to targets in an area.
/// Supports VFX, projectiles, and event hooks (OnHit, OnKill).
/// </summary>
public class DamageEffect : SpellEffect
{
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
    /// Uses ProjectileManager.spawn_projectile() via GDScript interop.
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

        // GDScript fallback (Unit3D.TakeDamage or take_damage)
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
    /// Spawn a projectile using ProjectileManager.
    /// </summary>
    private void SpawnProjectile(SpellContext context)
    {
        var projectileManager = GetProjectileManager(context);
        if (projectileManager == null)
        {
            GD.PrintErr("[DamageEffect] ProjectileManager not found");
            // Fallback to instant damage
            ApplyDamage(context);
            return;
        }

        // Find source base for projectile origin
        var source = FindBaseByTeam(context);

        // ProjectileId is guaranteed non-null here since we check before calling SpawnProjectile
        var projectileIdValue = ProjectileId ?? "";

        // Spawn projectile via GDScript interop
        // Pass Variant.From<GodotObject?>(null) to avoid nullable warning
        projectileManager.Call("spawn_projectile",
            projectileIdValue,
            source ?? (GodotObject)context.Battlefield!,
            Variant.From<GodotObject?>(null), // No target unit, targeting position
            Damage,
            "spell",
            new Godot.Collections.Dictionary
            {
                { "start_position", source?.GlobalPosition ?? context.Position },
                { "target_position", context.Position }
            }
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
    /// Get ProjectileManager autoload.
    /// </summary>
    private static Node? GetProjectileManager(SpellContext context)
    {
        return context.SceneTree?.Root?.GetNodeOrNull("/root/ProjectileManager");
    }

    /// <summary>
    /// Find the base node for the caster's team (for projectile origin).
    /// Returns null if no base found (caller should handle fallback).
    /// </summary>
    private static Node3D? FindBaseByTeam(SpellContext context)
    {
        if (context.SceneTree == null) return null;

        var bases = context.SceneTree.GetNodesInGroup("BASES");
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
