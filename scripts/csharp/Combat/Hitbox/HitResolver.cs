using Godot;

namespace ProjectSummoner.Combat.Hitbox;

/// <summary>
/// Service that resolves hits between hitboxes and hurtboxes.
/// Routes damage through the DamageSystem and emits events for VFX/audio.
/// Should be registered as an autoload.
/// </summary>
public partial class HitResolver : Node
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static HitResolver? Instance { get; private set; }

    // =========================================================================
    // SIGNALS
    // =========================================================================

    /// <summary>
    /// Emitted when a hit is confirmed and damage is dealt.
    /// Used by VFX, audio, and UI systems to react to hits.
    /// </summary>
    [Signal]
    public delegate void HitConfirmedEventHandler(
        Node3D attacker,
        Node3D target,
        float damage,
        Vector3 hitPosition,
        bool wasCrit,
        bool targetKilled);

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        GD.Print("HitResolver: Initialized");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================================
    // HIT RESOLUTION
    // =========================================================================

    /// <summary>
    /// Resolve a hit between a hitbox and hurtbox.
    /// Applies damage via DamageSystem and returns the result.
    /// </summary>
    public HitResult ResolveHit(HitboxComponent hitbox, HurtboxComponent hurtbox)
    {
        var result = new HitResult
        {
            Attacker = hitbox.Source!,
            Target = hurtbox.OwnerEntity!,
            HitPosition = hurtbox.GlobalPosition,
            DamageDealt = 0,
            WasCrit = false,
            TargetKilled = false
        };

        if (hurtbox.OwnerEntity == null)
        {
            GD.PushWarning("HitResolver: Hurtbox has no owner, hit ignored");
            return result;
        }

        // Apply damage through DamageSystem
        if (DamageSystem.Instance != null)
        {
            var flags = new Godot.Collections.Dictionary
            {
                { "from_hitbox", true }
            };

            float damageDealt = DamageSystem.Instance.apply_damage(
                hitbox.Source!,
                hurtbox.OwnerEntity,
                hitbox.Damage,
                hitbox.DamageType,
                flags
            );

            result.DamageDealt = damageDealt;

            // Check if it was a crit by comparing to base damage
            // DamageSystem applies crit multiplier internally
            result.WasCrit = damageDealt > hitbox.Damage * 1.5f;

            // Check if target was killed
            result.TargetKilled = IsTargetDead(hurtbox.OwnerEntity);
        }
        else
        {
            GD.PushWarning("HitResolver: DamageSystem not found, applying damage directly");
            ApplyDamageDirectly(hurtbox.OwnerEntity, hitbox.Damage);
            result.DamageDealt = hitbox.Damage;
            result.TargetKilled = IsTargetDead(hurtbox.OwnerEntity);
        }

        // Emit signal for VFX/audio systems
        EmitSignal(
            SignalName.HitConfirmed,
            result.Attacker,
            result.Target,
            result.DamageDealt,
            result.HitPosition,
            result.WasCrit,
            result.TargetKilled
        );

        return result;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static bool IsTargetDead(Node3D target)
    {
        // Check C# interface
        if (target is Capabilities.IDamageable damageable)
        {
            return !damageable.IsAlive;
        }

        // Check GDScript properties
        var isAliveVar = target.Get("IsAlive");
        if (isAliveVar.VariantType != Variant.Type.Nil)
        {
            return !isAliveVar.AsBool();
        }

        isAliveVar = target.Get("is_alive");
        if (isAliveVar.VariantType != Variant.Type.Nil)
        {
            return !isAliveVar.AsBool();
        }

        // Fallback to HP check
        var hpVar = target.Get("CurrentHp");
        if (hpVar.VariantType == Variant.Type.Nil)
        {
            hpVar = target.Get("current_hp");
        }

        if (hpVar.VariantType != Variant.Type.Nil)
        {
            return hpVar.AsSingle() <= 0;
        }

        return false;
    }

    private static void ApplyDamageDirectly(Node3D target, float damage)
    {
        // C# interface
        if (target is Capabilities.IDamageable damageable)
        {
            damageable.TakeDamage(damage);
            return;
        }

        // GDScript methods
        if (target.HasMethod("TakeDamage"))
        {
            target.Call("TakeDamage", damage);
        }
        else if (target.HasMethod("take_damage"))
        {
            target.Call("take_damage", damage);
        }
    }

    // =========================================================================
    // GDScript COMPATIBILITY (snake_case aliases)
    // =========================================================================

    public HitResult resolve_hit(HitboxComponent hitbox, HurtboxComponent hurtbox)
        => ResolveHit(hitbox, hurtbox);
}
