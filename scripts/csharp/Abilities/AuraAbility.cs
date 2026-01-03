using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Abilities;

/// <summary>
/// Affects nearby units periodically.
///
/// Highly configurable aura that can damage, heal, buff, or debuff targets.
/// Triggers every TickRate seconds and affects units within radius.
///
/// Examples:
///   - Damage Aura: DamagePerTick=5, AffectsEnemies=true
///   - Heal Aura: HealPerTick=10, AffectsAllies=true
///   - Speed Buff: SpeedModifier=0.3, AffectsAllies=true
/// </summary>
[GlobalClass]
public partial class AuraAbility : BaseAbility
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    public enum AuraType
    {
        /// <summary>Damages targets over time.</summary>
        Damage,
        /// <summary>Heals targets over time.</summary>
        Heal,
        /// <summary>Increases movement speed.</summary>
        BuffSpeed,
        /// <summary>Decreases movement speed.</summary>
        DebuffSlow,
        /// <summary>For future custom effects.</summary>
        Custom
    }

    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void AbilityTriggeredEventHandler(Godot.Collections.Dictionary data);

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>Type of aura effect.</summary>
    [Export]
    public AuraType Type { get; set; } = AuraType.Damage;

    /// <summary>Radius of aura effect.</summary>
    [Export]
    public float Radius { get; set; } = 4.0f;

    /// <summary>How often the aura triggers (in seconds).</summary>
    [Export]
    public float TickRate { get; set; } = 1.0f;

    // =========================================================================
    // EFFECT VALUES
    // =========================================================================

    /// <summary>Damage dealt per tick (for Damage type).</summary>
    [Export]
    public float DamagePerTick { get; set; } = 5.0f;

    /// <summary>Healing amount per tick (for Heal type).</summary>
    [Export]
    public float HealPerTick { get; set; } = 0.0f;

    /// <summary>
    /// Speed modifier (for BuffSpeed/DebuffSlow).
    /// 0.3 = +30% speed, -0.5 = -50% speed.
    /// </summary>
    [Export]
    public float SpeedModifier { get; set; } = 0.0f;

    /// <summary>How long speed modifiers last (should be >= TickRate).</summary>
    [Export]
    public float ModifierDuration { get; set; } = 1.5f;

    // =========================================================================
    // TARGETING
    // =========================================================================

    /// <summary>Whether aura affects enemy units.</summary>
    [Export]
    public bool AffectsEnemies { get; set; } = true;

    /// <summary>Whether aura affects allied units.</summary>
    [Export]
    public bool AffectsAllies { get; set; } = false;

    /// <summary>Whether aura affects the owner unit.</summary>
    [Export]
    public bool AffectsSelf { get; set; } = false;

    // =========================================================================
    // VISUAL/AUDIO
    // =========================================================================

    /// <summary>Persistent aura visual effect (attached to unit).</summary>
    [Export]
    public string PersistentAuraVfx { get; set; } = "";

    /// <summary>VFX played each tick.</summary>
    [Export]
    public string TickVfx { get; set; } = "";

    /// <summary>Sound played each tick.</summary>
    [Export]
    public string TickSound { get; set; } = "";

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================

    private float _tickTimer;
    private Node? _persistentVfxNode;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    protected override void Initialize()
    {
        // Start tick timer at random offset to prevent all auras syncing
        _tickTimer = GD.Randf() * TickRate;

        // Spawn persistent aura VFX
        if (!string.IsNullOrEmpty(PersistentAuraVfx) && OwnerUnit != null)
        {
            _persistentVfxNode = SpawnVfx(PersistentAuraVfx, OwnerUnit.GlobalPosition, OwnerUnit);
            if (_persistentVfxNode != null)
            {
                GD.Print($"AuraAbility: Spawned persistent VFX '{PersistentAuraVfx}' for {OwnerUnit.Name}");
            }
        }
    }

    protected override void ConnectToUnitEvents()
    {
        if (OwnerUnit != null)
        {
            // Clean up VFX when unit dies
            OwnerUnit.UnitDied += OnOwnerDied;
        }
    }

    // =========================================================================
    // UPDATE
    // =========================================================================

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || OwnerUnit == null || !OwnerUnit.IsAlive)
            return;

        _tickTimer -= (float)delta;
        if (_tickTimer <= 0)
        {
            TriggerAura();
            _tickTimer = TickRate;
        }
    }

    // =========================================================================
    // AURA LOGIC
    // =========================================================================

    private void TriggerAura()
    {
        if (OwnerUnit == null)
            return;

        var targets = GetUnitsInRadius(OwnerUnit.GlobalPosition, Radius, AffectsEnemies, AffectsAllies, AffectsSelf);

        if (targets.Count == 0)
            return;

        // Apply effect based on aura type
        switch (Type)
        {
            case AuraType.Damage:
                ApplyDamageAura(targets);
                break;

            case AuraType.Heal:
                ApplyHealAura(targets);
                break;

            case AuraType.BuffSpeed:
                ApplySpeedBuffAura(targets);
                break;

            case AuraType.DebuffSlow:
                ApplySpeedDebuffAura(targets);
                break;
        }

        // Play tick VFX
        if (!string.IsNullOrEmpty(TickVfx))
        {
            SpawnVfx(TickVfx, OwnerUnit.GlobalPosition);
        }

        // Emit signal
        EmitSignal(SignalName.AbilityTriggered, new Godot.Collections.Dictionary
        {
            ["targets_affected"] = targets.Count,
            ["aura_type"] = Type.ToString()
        });
    }

    /// <summary>Apply damage to all targets.</summary>
    private void ApplyDamageAura(System.Collections.Generic.List<Unit3D> targets)
    {
        if (DamagePerTick <= 0)
            return;

        foreach (var target in targets)
        {
            ApplyDamage(target, DamagePerTick, "fire");
        }
    }

    /// <summary>Apply healing to all targets.</summary>
    private void ApplyHealAura(System.Collections.Generic.List<Unit3D> targets)
    {
        if (HealPerTick <= 0)
            return;

        foreach (var target in targets)
        {
            if (target.HasMethod("Heal"))
            {
                target.Call("Heal", HealPerTick);
            }
            else if (target.HasMethod("heal"))
            {
                target.Call("heal", HealPerTick);
            }
        }
    }

    /// <summary>Apply speed buff to all targets.</summary>
    private void ApplySpeedBuffAura(System.Collections.Generic.List<Unit3D> targets)
    {
        if (SpeedModifier == 0)
            return;

        foreach (var target in targets)
        {
            ApplySpeedModifier(target, SpeedModifier, $"aura_speed_buff_{OwnerUnit?.Name}");
        }
    }

    /// <summary>Apply speed debuff to all targets.</summary>
    private void ApplySpeedDebuffAura(System.Collections.Generic.List<Unit3D> targets)
    {
        if (SpeedModifier == 0)
            return;

        float debuffValue = -Mathf.Abs(SpeedModifier);  // Ensure negative
        foreach (var target in targets)
        {
            ApplySpeedModifier(target, debuffValue, $"aura_slow_{OwnerUnit?.Name}");
        }
    }

    /// <summary>Apply a temporary speed modifier to a target.</summary>
    private void ApplySpeedModifier(Unit3D target, float modifier, string source)
    {
        if (target.HasMethod("apply_modifier"))
        {
            target.Call("apply_modifier", new Godot.Collections.Dictionary
            {
                ["source"] = source,
                ["duration"] = ModifierDuration,
                ["stats"] = new Godot.Collections.Dictionary { ["move_speed"] = modifier },
                ["amplification"] = "MULTIPLICATIVE"
            });
        }
        else if (target.HasMethod("ApplyModifier"))
        {
            target.Call("ApplyModifier", new Godot.Collections.Dictionary
            {
                ["source"] = source,
                ["duration"] = ModifierDuration,
                ["stats"] = new Godot.Collections.Dictionary { ["move_speed"] = modifier },
                ["amplification"] = "MULTIPLICATIVE"
            });
        }
    }

    // =========================================================================
    // CLEANUP
    // =========================================================================

    private void OnOwnerDied(Unit3D unit)
    {
        // Remove persistent VFX
        if (_persistentVfxNode != null && IsInstanceValid(_persistentVfxNode))
        {
            _persistentVfxNode.QueueFree();
        }
        Deactivate();
    }
}
