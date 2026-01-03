using Godot;
using ProjectSummoner.Capabilities;

namespace ProjectSummoner.Units;

/// <summary>
/// Concrete implementation for ranged combat units.
/// Spawns projectiles to attack targets from distance.
/// </summary>
[GlobalClass]
public partial class RangedUnit3D : Unit3D, IRangedAttacker
{
    // =========================================================================
    // EXPORTED PROPERTIES - Ranged Configuration
    // =========================================================================

    [ExportGroup("Ranged Configuration")]
    [Export]
    public string ProjectileId { get; set; } = "";

    [Export]
    public float ProjectileDelay { get; set; } = 0f;

    [Export]
    public bool IsDelayedProjectile { get; set; } = false;

    // =========================================================================
    // STATE
    // =========================================================================

    /// <summary>
    /// Projectile spawn point marker (optional child node).
    /// </summary>
    private Marker3D? _projectileSpawnPoint;

    /// <summary>
    /// Target saved during delayed projectile attack.
    /// </summary>
    private Node3D? _delayedProjectileTarget;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        base._Ready();
        UnitType = (int)Units.UnitType.Ranged;

        // Find projectile spawn point if it exists
        _projectileSpawnPoint = GetNodeOrNull<Marker3D>("ProjectileSpawnPoint");
    }

    // =========================================================================
    // ABSTRACT METHOD IMPLEMENTATIONS
    // =========================================================================

    protected override float GetEffectiveAttackRange()
    {
        return AttackRange;
    }

    protected override void PerformAttackAction()
    {
        if (CurrentTarget == null)
            return;

        // Play attack animation
        VisualComponent?.PlayAnimation("attack");

        if (IsDelayedProjectile && ProjectileDelay > 0)
        {
            // Charge-up attack: spawn projectile after delay
            _delayedProjectileTarget = CurrentTarget;
            SpawnProjectileDelayed();
        }
        else
        {
            // Normal ranged: spawn immediately
            SpawnProjectile(CurrentTarget);
        }
    }

    // =========================================================================
    // IRangedAttacker IMPLEMENTATION
    // =========================================================================

    public void SpawnProjectile(Node3D target)
    {
        if (string.IsNullOrEmpty(ProjectileId) || target == null)
            return;

        Vector3 spawnPos = GetProjectileSpawnPosition();
        Vector3 targetPos = GetTargetPosition(target);

        // Apply predictive targeting for moving targets
        targetPos = CalculateInterceptPoint(spawnPos, targetPos, target);

        // Spawn via ProjectileManager (GDScript autoload)
        var projectileManager = GetNode("/root/ProjectileManager");

        var options = new Godot.Collections.Dictionary
        {
            ["start_position"] = spawnPos,
            ["target_position"] = targetPos
        };

        projectileManager.Call("spawn_projectile",
            ProjectileId,
            this,
            target,
            AttackDamage,
            "physical",
            options);
    }

    public Vector3 GetProjectileSpawnPosition()
    {
        if (_projectileSpawnPoint != null)
        {
            return _projectileSpawnPoint.GlobalPosition;
        }

        // Fallback: spawn from chest height
        float height = VisualComponent?.GetSpriteHeight() ?? 1f;
        return GlobalPosition + new Vector3(0, height * 0.6f, 0);
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private async void SpawnProjectileDelayed()
    {
        Node3D? target = _delayedProjectileTarget;

        // Wait for delay
        await ToSignal(GetTree().CreateTimer(ProjectileDelay), SceneTreeTimer.SignalName.Timeout);

        // Check if target is still valid
        if (target != null && IsInstanceValid(target))
        {
            SpawnProjectile(target);
        }

        _delayedProjectileTarget = null;
    }

    private Vector3 GetTargetPosition(Node3D target)
    {
        // Check if target has a custom projectile target position
        if (target.HasMethod("get_projectile_target_position"))
        {
            return target.Call("get_projectile_target_position").AsVector3();
        }

        // Default: target center mass
        return target.GlobalPosition + new Vector3(0, 0.5f, 0);
    }

    private Vector3 CalculateInterceptPoint(Vector3 spawnPos, Vector3 targetPos, Node3D target)
    {
        // Default projectile speed for intercept calculation
        float projectileSpeed = 15f;

        // Get target velocity if available
        Vector3 targetVelocity = Vector3.Zero;
        if (target is CharacterBody3D charBody)
        {
            targetVelocity = charBody.Velocity;
        }
        else if (target.HasMethod("get") && target.Get("velocity").VariantType != Variant.Type.Nil)
        {
            targetVelocity = target.Get("velocity").AsVector3();
        }

        // Simple linear prediction
        float distance = spawnPos.DistanceTo(targetPos);
        float timeToTarget = distance / projectileSpeed;

        return targetPos + (targetVelocity * timeToTarget);
    }
}
