using Godot;

namespace ProjectSummoner.Units;

/// <summary>
/// Concrete implementation for melee combat units.
/// Deals damage directly to targets in range.
/// </summary>
[GlobalClass]
public partial class MeleeUnit3D : Unit3D
{
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

        // Deal damage
        DealDamageTo(_pendingAttackTarget);
        _pendingAttackTarget = null;
    }
}
