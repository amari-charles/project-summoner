using Godot;
using ProjectSummoner.Combat.Hitbox;

namespace ProjectSummoner.Units;

/// <summary>
/// Concrete implementation for melee combat units.
/// Deals damage directly to targets in range.
/// </summary>
[GlobalClass]
public partial class MeleeUnit3D : Unit3D
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>Duration of melee hitbox in seconds (brief window for hit detection).</summary>
    private const float MeleeHitboxDuration = 0.1f;

    /// <summary>Hitbox spawns at this fraction of attack range from attacker toward target.</summary>
    private const float HitboxPositionFraction = 0.5f;

    /// <summary>Height offset for hitbox position (roughly chest height).</summary>
    private const float HitboxHeightOffset = 1.0f;

    /// <summary>Hitbox radius as fraction of attack range.</summary>
    private const float HitboxRadiusFraction = 0.6f;

    // =========================================================================
    // STATE
    // =========================================================================

    /// <summary>
    /// Target saved during attack animation, damage dealt on impact.
    /// </summary>
    protected Node3D? _pendingAttackTarget;

    /// <summary>
    /// Timer for attack damage fallback (if animation event doesn't fire).
    /// </summary>
    private float _attackFallbackTimer;
    private const float AttackFallbackDelay = 0.5f;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        base._Ready();
        UnitType = (int)Units.UnitType.Melee;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Handle attack fallback timer
        if (_attackFallbackTimer > 0)
        {
            _attackFallbackTimer -= (float)delta;
            if (_attackFallbackTimer <= 0 && _pendingAttackTarget != null)
            {
                // Fallback: deal damage if animation event didn't fire
                ExecuteAttackDamage();
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

        // Save target for damage on animation impact
        _pendingAttackTarget = CurrentTarget;

        // Play attack animation
        VisualComponent?.PlayAnimation("attack");

        // Start fallback timer in case animation event doesn't fire
        _attackFallbackTimer = AttackFallbackDelay;
    }

    // =========================================================================
    // ATTACK IMPACT
    // =========================================================================

    /// <summary>
    /// Called by animation event when attack impact frame is reached.
    /// Can also be called from rig scripts via signal.
    /// </summary>
    public void OnAttackImpact()
    {
        ExecuteAttackDamage();
    }

    private void ExecuteAttackDamage()
    {
        // Clear fallback timer
        _attackFallbackTimer = 0;

        if (_pendingAttackTarget == null)
            return;

        if (!IsInstanceValid(_pendingAttackTarget))
        {
            _pendingAttackTarget = null;
            return;
        }

        // Create a temporary hitbox for the melee attack
        SpawnMeleeHitbox(_pendingAttackTarget);
        _pendingAttackTarget = null;
    }

    /// <summary>
    /// Spawn a temporary hitbox for the melee attack.
    /// The hitbox exists briefly and damages any enemy hurtboxes it overlaps.
    /// </summary>
    private void SpawnMeleeHitbox(Node3D target)
    {
        var hitbox = new HitboxComponent
        {
            Damage = AttackDamage,
            DamageType = "physical",
            SourceTeam = Team,
            Source = this,
            Lifetime = HitboxLifetime.Timed,
            Duration = MeleeHitboxDuration,
            SingleHitPerTarget = true,
            TargetCategories = HurtboxCategory.Unit | HurtboxCategory.Summoner
        };

        // Position hitbox between attacker and target
        var attackDirection = (target.GlobalPosition - GlobalPosition).Normalized();
        var hitboxPosition = GlobalPosition + attackDirection * (AttackRange * HitboxPositionFraction);
        hitboxPosition.Y = GlobalPosition.Y + HitboxHeightOffset;

        // Add to scene first so _Ready() runs and configures collision
        GetTree().Root.AddChild(hitbox);

        // Set position after adding to tree
        hitbox.GlobalPosition = hitboxPosition;

        // Create attack hitbox shape
        hitbox.CreateSphereShape(AttackRange * HitboxRadiusFraction);
    }
}
