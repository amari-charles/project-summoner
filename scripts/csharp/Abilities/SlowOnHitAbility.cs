using Godot;
using ProjectSummoner.Units;
using ProjectSummoner.Vfx;

namespace ProjectSummoner.Abilities;

/// <summary>
/// Applies a speed debuff to targets when the owning unit attacks.
///
/// Example:
///   SlowPercent = 0.3  // -30% speed
///   SlowDuration = 2.0 // lasts 2 seconds
/// </summary>
[GlobalClass]
public partial class SlowOnHitAbility : BaseAbility
{
    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void AbilityTriggeredEventHandler(Godot.Collections.Dictionary data);

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>Speed reduction as a percentage (0.3 = -30% speed).</summary>
    [Export]
    public float SlowPercent { get; set; } = 0.3f;

    /// <summary>How long the slow effect lasts in seconds.</summary>
    [Export]
    public float SlowDuration { get; set; } = 2.0f;

    /// <summary>VFX played when slow is applied to a target.</summary>
    public VfxId SlowAppliedVfx { get; set; } = VfxId.None;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    protected override void ConnectToUnitEvents()
    {
        if (OwnerUnit != null)
        {
            OwnerUnit.UnitAttacked += OnOwnerAttacked;
        }
    }

    // =========================================================================
    // ATTACK HANDLER
    // =========================================================================

    private void OnOwnerAttacked(Node3D target)
    {
        if (!IsActive || target == null)
            return;

        if (target is not Unit3D targetUnit)
            return;

        ApplySlowToTarget(targetUnit);
    }

    private void ApplySlowToTarget(Unit3D target)
    {
        string source = $"slow_on_hit_{OwnerUnit?.Name}_{GetInstanceId()}";
        float slowValue = -Mathf.Abs(SlowPercent); // Ensure negative

        // Use the modifier system if available
        if (target.HasMethod("apply_modifier"))
        {
            target.Call("apply_modifier", new Godot.Collections.Dictionary
            {
                ["source"] = source,
                ["duration"] = SlowDuration,
                ["stats"] = new Godot.Collections.Dictionary { ["move_speed"] = slowValue },
                ["amplification"] = "MULTIPLICATIVE"
            });
        }
        else if (target.HasMethod("ApplyModifier"))
        {
            target.Call("ApplyModifier", new Godot.Collections.Dictionary
            {
                ["source"] = source,
                ["duration"] = SlowDuration,
                ["stats"] = new Godot.Collections.Dictionary { ["move_speed"] = slowValue },
                ["amplification"] = "MULTIPLICATIVE"
            });
        }

        // Play VFX
        if (SlowAppliedVfx.HasValue)
        {
            SpawnVfx(SlowAppliedVfx, target.GlobalPosition);
        }

        GD.Print($"SlowOnHitAbility: {OwnerUnit?.Name} slowed {target.Name} by {SlowPercent * 100}% for {SlowDuration}s");

        EmitSignal(SignalName.AbilityTriggered, new Godot.Collections.Dictionary
        {
            ["target"] = target.Name,
            ["slow_percent"] = SlowPercent,
            ["duration"] = SlowDuration
        });
    }
}
