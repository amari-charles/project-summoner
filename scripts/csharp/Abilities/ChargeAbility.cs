using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Abilities;

/// <summary>
/// Bonus damage after moving a distance.
///
/// Tracks unit movement and applies bonus damage when the unit has traveled
/// far enough since their last attack. Rewards aggressive positioning.
///
/// Example:
///   ChargeThreshold = 5.0  // Must travel 5 units
///   DamageBonus = 30.0     // +30 damage (or +30% if Percentage)
///   BonusType = Flat       // Flat damage increase
/// </summary>
[GlobalClass]
public partial class ChargeAbility : BaseAbility
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    public enum ChargeBonusType
    {
        /// <summary>Add bonus damage directly.</summary>
        Flat,
        /// <summary>Multiply base damage by (1 + bonus/100).</summary>
        Percentage
    }

    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void AbilityTriggeredEventHandler(Godot.Collections.Dictionary data);

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>Distance unit must travel to activate charge.</summary>
    [Export]
    public float ChargeThreshold { get; set; } = 5.0f;

    /// <summary>Damage bonus when charged.</summary>
    [Export]
    public float DamageBonus { get; set; } = 30.0f;

    /// <summary>How the bonus is applied.</summary>
    [Export]
    public ChargeBonusType BonusType { get; set; } = ChargeBonusType.Flat;

    /// <summary>Whether charge resets after ANY attack or only charged attacks.</summary>
    [Export]
    public bool ResetOnAnyAttack { get; set; } = true;

    /// <summary>VFX played when charge is ready.</summary>
    [Export]
    public string ChargeReadyVfx { get; set; } = "";

    /// <summary>VFX played on charged attack impact.</summary>
    [Export]
    public string ChargeImpactVfx { get; set; } = "";

    /// <summary>Sound played on charged attack.</summary>
    [Export]
    public string ChargeImpactSound { get; set; } = "";

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================

    private float _distanceTraveledSinceAttack;
    private Vector3 _lastPosition = Vector3.Zero;
    private bool _isCharged;
    private Node? _chargeReadyVfxNode;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    protected override void Initialize()
    {
        if (OwnerUnit != null)
        {
            _lastPosition = OwnerUnit.GlobalPosition;
        }
    }

    protected override void ConnectToUnitEvents()
    {
        if (OwnerUnit != null)
        {
            OwnerUnit.UnitAttacked += OnOwnerAttacked;
        }
    }

    // =========================================================================
    // UPDATE
    // =========================================================================

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || OwnerUnit == null || !OwnerUnit.IsAlive)
            return;

        // Track distance moved
        Vector3 currentPosition = OwnerUnit.GlobalPosition;
        float distanceMoved = currentPosition.DistanceTo(_lastPosition);
        _lastPosition = currentPosition;

        _distanceTraveledSinceAttack += distanceMoved;

        // Check if charge is ready
        bool wasCharged = _isCharged;
        _isCharged = _distanceTraveledSinceAttack >= ChargeThreshold;

        // Show VFX when charge becomes ready
        if (_isCharged && !wasCharged)
        {
            OnChargeReady();
        }
    }

    // =========================================================================
    // CHARGE LOGIC
    // =========================================================================

    private void OnChargeReady()
    {
        if (OwnerUnit == null)
            return;

        GD.Print($"ChargeAbility: {OwnerUnit.Name} charge ready! (traveled {_distanceTraveledSinceAttack:F1} / {ChargeThreshold:F1})");

        // Spawn "charge ready" VFX
        if (!string.IsNullOrEmpty(ChargeReadyVfx))
        {
            if (_chargeReadyVfxNode != null && IsInstanceValid(_chargeReadyVfxNode))
            {
                _chargeReadyVfxNode.QueueFree();
            }
            _chargeReadyVfxNode = SpawnVfx(ChargeReadyVfx, OwnerUnit.GlobalPosition, OwnerUnit);
        }

        EmitSignal(SignalName.AbilityTriggered, new Godot.Collections.Dictionary
        {
            ["event"] = "charge_ready",
            ["distance"] = _distanceTraveledSinceAttack
        });
    }

    private void OnOwnerAttacked(Node3D target)
    {
        if (!IsActive || target == null)
            return;

        // Apply charge bonus if charged
        if (_isCharged && target is Unit3D targetUnit)
        {
            ApplyChargeDamage(targetUnit);
            GD.Print($"ChargeAbility: {OwnerUnit?.Name} executed CHARGED attack on {target.Name}!");
        }

        // Reset charge (either after charged attack or any attack)
        if (_isCharged || ResetOnAnyAttack)
        {
            ResetCharge();
        }
    }

    private void ApplyChargeDamage(Unit3D target)
    {
        float bonusDamage = DamageBonus;

        // Calculate final damage based on bonus type
        switch (BonusType)
        {
            case ChargeBonusType.Flat:
                // Direct damage addition
                ApplyDamage(target, bonusDamage, "physical");
                break;

            case ChargeBonusType.Percentage:
                // Percentage of base attack damage
                float baseDamage = OwnerUnit?.AttackDamage ?? 0f;
                float percentageBonus = baseDamage * (bonusDamage / 100.0f);
                ApplyDamage(target, percentageBonus, "physical");
                break;
        }

        // Play charge impact VFX
        if (!string.IsNullOrEmpty(ChargeImpactVfx))
        {
            SpawnVfx(ChargeImpactVfx, target.GlobalPosition);
        }

        EmitSignal(SignalName.AbilityTriggered, new Godot.Collections.Dictionary
        {
            ["event"] = "charged_attack",
            ["target"] = target.Name,
            ["bonus_damage"] = bonusDamage,
            ["distance_traveled"] = _distanceTraveledSinceAttack
        });
    }

    private void ResetCharge()
    {
        _distanceTraveledSinceAttack = 0.0f;
        _isCharged = false;

        // Remove charge ready VFX
        if (_chargeReadyVfxNode != null && IsInstanceValid(_chargeReadyVfxNode))
        {
            _chargeReadyVfxNode.QueueFree();
            _chargeReadyVfxNode = null;
        }
    }

    // =========================================================================
    // DEBUG
    // =========================================================================

    /// <summary>
    /// Get current charge progress (0.0 to 1.0).
    /// </summary>
    public float GetChargeProgress()
    {
        if (ChargeThreshold <= 0)
            return 0.0f;
        return Mathf.Min(_distanceTraveledSinceAttack / ChargeThreshold, 1.0f);
    }
}
