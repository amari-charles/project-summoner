using Godot;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Projectiles;

namespace ProjectSummoner.Units;

/// <summary>
/// Concrete implementation for ranged combat units.
/// Spawns projectiles to attack targets from distance.
/// </summary>
[GlobalClass]
public partial class RangedUnit3D : Unit3D, IRangedAttacker
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>
    /// Default height offset for targeting center mass of units.
    /// Most units have their origin at feet level, so we aim slightly above.
    /// </summary>
    private const float DefaultTargetHeightOffset = 0.5f;

    /// <summary>
    /// Fraction of sprite height used for chest-level projectile spawning.
    /// 0.6 = 60% up from feet, roughly chest height for humanoid units.
    /// </summary>
    private const float ChestHeightFraction = 0.6f;

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

    /// <summary>
    /// Speed used for intercept calculations when predicting target movement.
    /// Should roughly match the actual projectile speed from ProjectileManager.
    /// </summary>
    [Export]
    public float ProjectileSpeedEstimate { get; set; } = 15f;

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

    /// <summary>
    /// Timer for delayed projectile spawning (replaces async void pattern).
    /// </summary>
    private float _projectileDelayTimer;

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

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Handle delayed projectile timer
        if (_projectileDelayTimer > 0)
        {
            _projectileDelayTimer -= (float)delta;
            if (_projectileDelayTimer <= 0)
            {
                SpawnDelayedProjectileIfValid();
            }
        }
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
            // Charge-up attack: spawn projectile after delay (timer-based)
            _delayedProjectileTarget = CurrentTarget;
            _projectileDelayTimer = ProjectileDelay;
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

        // Spawn via ProjectileService (C# singleton)
        if (ProjectileService.Instance == null)
        {
            GD.PushError("RangedUnit3D: ProjectileService not found!");
            return;
        }

        var options = new Godot.Collections.Dictionary
        {
            ["start_position"] = spawnPos,
            ["target_position"] = targetPos
        };

        ProjectileService.Instance.SpawnProjectile(
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
        return GlobalPosition + new Vector3(0, height * ChestHeightFraction, 0);
    }

    // =========================================================================
    // DEFINITION APPLICATION
    // =========================================================================

    /// <summary>
    /// Apply ranged-specific configuration from a UnitDefinition.
    /// Called by UnitSpawner after base ApplyDefinition.
    /// </summary>
    public void ApplyRangedDefinition(RangedConfig rangedConfig)
    {
        ProjectileId = rangedConfig.ProjectileId.Value;
        ProjectileDelay = rangedConfig.ProjectileDelay;
        IsDelayedProjectile = rangedConfig.IsDelayedProjectile;
        ProjectileSpeedEstimate = rangedConfig.ProjectileSpeedEstimate;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Spawn the delayed projectile if target is still valid.
    /// Called by _PhysicsProcess when timer expires.
    /// </summary>
    private void SpawnDelayedProjectileIfValid()
    {
        Node3D? target = _delayedProjectileTarget;
        _delayedProjectileTarget = null;

        // Check if target is still valid
        if (target == null || !IsInstanceValid(target))
            return;

        // Verify attack constraints still allow attacking
        // (target may have moved out of cone during the charge-up)
        if (!GetTargetingConfig().CanAttack(this, target))
            return;

        SpawnProjectile(target);
    }

    private Vector3 GetTargetPosition(Node3D target)
    {
        // Check if target has a custom projectile target position
        if (target.HasMethod("get_projectile_target_position"))
        {
            return target.Call("get_projectile_target_position").AsVector3();
        }

        // Default: target center mass
        return target.GlobalPosition + new Vector3(0, DefaultTargetHeightOffset, 0);
    }

    private Vector3 CalculateInterceptPoint(Vector3 spawnPos, Vector3 targetPos, Node3D target)
    {
        // Get target velocity if available
        Vector3 targetVelocity = Vector3.Zero;
        if (target is CharacterBody3D charBody)
        {
            targetVelocity = charBody.Velocity;
        }
        else
        {
            // Check for velocity property on GDScript nodes
            var velocityVar = target.Get("velocity");
            if (velocityVar.VariantType != Variant.Type.Nil)
            {
                targetVelocity = velocityVar.AsVector3();
            }
        }

        // Simple linear prediction
        float distance = spawnPos.DistanceTo(targetPos);
        float timeToTarget = distance / ProjectileSpeedEstimate;

        return targetPos + (targetVelocity * timeToTarget);
    }
}
