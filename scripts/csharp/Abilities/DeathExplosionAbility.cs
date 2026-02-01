using Godot;
using ProjectSummoner.Units;
using ProjectSummoner.Vfx;

namespace ProjectSummoner.Abilities;

/// <summary>
/// Explodes on death, dealing AoE damage.
///
/// Triggers an explosion when the unit dies, damaging nearby units.
/// Can be configured to affect enemies, allies, or both (friendly fire).
///
/// Example:
///   ExplosionRadius = 3.0    // 3 unit radius
///   ExplosionDamage = 50.0   // 50 damage
///   AffectsEnemies = true    // Damage enemies
///   AffectsAllies = false    // No friendly fire
/// </summary>
[GlobalClass]
public partial class DeathExplosionAbility : BaseAbility
{
    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void AbilityTriggeredEventHandler(Godot.Collections.Dictionary data);

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>Radius of explosion damage.</summary>
    [Export]
    public float ExplosionRadius { get; set; } = 3.0f;

    /// <summary>Damage dealt to units in explosion.</summary>
    [Export]
    public float ExplosionDamage { get; set; } = 50.0f;

    /// <summary>Damage type (physical, fire, etc.).</summary>
    [Export]
    public string DamageType { get; set; } = "fire";

    // =========================================================================
    // TARGETING
    // =========================================================================

    /// <summary>Whether explosion damages enemy units.</summary>
    [Export]
    public bool AffectsEnemies { get; set; } = true;

    /// <summary>Whether explosion damages allied units (friendly fire).</summary>
    [Export]
    public bool AffectsAllies { get; set; } = false;

    // =========================================================================
    // VISUAL/AUDIO
    // =========================================================================

    /// <summary>VFX played at explosion center.</summary>
    public VfxId ExplosionVfx { get; set; } = VfxIds.ExplosionDefault;

    /// <summary>Sound played on explosion.</summary>
    [Export]
    public string ExplosionSound { get; set; } = "";

    /// <summary>Delay before explosion triggers (in seconds).</summary>
    [Export]
    public float ExplosionDelay { get; set; } = 0.0f;

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================

    private bool _hasExploded;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    protected override void ConnectToUnitEvents()
    {
        if (OwnerUnit != null)
        {
            OwnerUnit.UnitDied += OnOwnerDied;
        }
    }

    // =========================================================================
    // EXPLOSION LOGIC
    // =========================================================================

    private async void OnOwnerDied(Unit3D unit)
    {
        if (!IsActive || _hasExploded)
            return;

        if (ExplosionDelay > 0)
        {
            await ToSignal(GetTree().CreateTimer(ExplosionDelay), SceneTreeTimer.SignalName.Timeout);

            // Guard: ability may be inactive or already exploded after await
            if (!IsActive || _hasExploded || !IsInstanceValid(this) || !IsInsideTree())
                return;
        }

        TriggerExplosion();
    }

    private void TriggerExplosion()
    {
        if (OwnerUnit == null)
            return;

        _hasExploded = true;

        Vector3 explosionCenter = OwnerUnit.GlobalPosition;

        // Spawn VFX first
        if (ExplosionVfx.HasValue)
        {
            SpawnVfx(ExplosionVfx, explosionCenter);
        }

        // Get targets in explosion radius
        var targets = GetUnitsInRadius(explosionCenter, ExplosionRadius, AffectsEnemies, AffectsAllies, false);

        // Apply damage to all targets
        int hitCount = 0;
        foreach (var target in targets)
        {
            ApplyDamage(target, ExplosionDamage, DamageType);
            hitCount++;
        }

        GD.Print($"DeathExplosionAbility: {OwnerUnit.Name} exploded! Hit {hitCount} units in {ExplosionRadius:F1} radius");

        // Emit signal
        EmitSignal(SignalName.AbilityTriggered, new Godot.Collections.Dictionary
        {
            ["event"] = "explosion",
            ["targets_hit"] = hitCount,
            ["damage"] = ExplosionDamage,
            ["radius"] = ExplosionRadius
        });

        Deactivate();
    }

    // =========================================================================
    // DEBUG
    // =========================================================================

    /// <summary>
    /// Manually trigger explosion (for testing).
    /// </summary>
    public void TriggerManualExplosion()
    {
        if (_hasExploded)
        {
            GD.PushWarning("DeathExplosionAbility: Already exploded");
            return;
        }
        TriggerExplosion();
    }
}
