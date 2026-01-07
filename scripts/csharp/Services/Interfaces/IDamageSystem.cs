using Godot;

namespace ProjectSummoner.Services.Interfaces;

/// <summary>
/// Interface for the DamageSystem service.
/// Enables dependency injection and testing.
/// </summary>
public interface IDamageSystem
{
    // =========================================================================
    // DAMAGE APPLICATION
    // =========================================================================

    /// <summary>
    /// Apply damage to a target.
    /// </summary>
    float ApplyDamage(Node3D attacker, Node3D target, float baseDamage);

    /// <summary>
    /// Apply damage with damage type.
    /// </summary>
    float ApplyDamage(Node3D attacker, Node3D target, float baseDamage, string damageType);

    /// <summary>
    /// Apply damage (snake_case for GDScript).
    /// </summary>
    float apply_damage(Node3D attacker, Node3D target, float baseDamage);

    /// <summary>
    /// Apply damage with type (snake_case for GDScript).
    /// </summary>
    float apply_damage(Node3D attacker, Node3D target, float baseDamage, string damageType);

    /// <summary>
    /// Apply damage with full parameters.
    /// </summary>
    float apply_damage(
        Node3D attacker,
        Node3D target,
        float baseDamage,
        string damageType,
        Godot.Collections.Dictionary? flags);

    // =========================================================================
    // HEALING
    // =========================================================================

    /// <summary>
    /// Apply healing to a target.
    /// </summary>
    float ApplyHealing(Node3D healer, Node3D target, float healAmount);

    /// <summary>
    /// Apply healing with metadata.
    /// </summary>
    float ApplyHealing(
        Node3D healer,
        Node3D target,
        float healAmount,
        Godot.Collections.Dictionary? metadata);

    // =========================================================================
    // COMBAT EVENTS
    // =========================================================================

    /// <summary>
    /// Emit attack started event.
    /// </summary>
    void EmitAttackStarted(Node3D attacker, Node3D target);

    /// <summary>
    /// Emit attack started with metadata.
    /// </summary>
    void EmitAttackStarted(Node3D attacker, Node3D target, Godot.Collections.Dictionary? metadata);

    /// <summary>
    /// Emit attack completed event.
    /// </summary>
    void EmitAttackCompleted(Node3D attacker, Node3D target);

    /// <summary>
    /// Emit attack completed with metadata.
    /// </summary>
    void EmitAttackCompleted(Node3D attacker, Node3D target, Godot.Collections.Dictionary? metadata);

    /// <summary>
    /// Emit spell cast event.
    /// </summary>
    void EmitSpellCast(Node3D caster, string spellId);

    /// <summary>
    /// Emit spell cast with metadata.
    /// </summary>
    void EmitSpellCast(Node3D caster, string spellId, Godot.Collections.Dictionary? metadata);

    // =========================================================================
    // PREVIEW
    // =========================================================================

    /// <summary>
    /// Preview damage without applying.
    /// </summary>
    Godot.Collections.Dictionary PreviewDamage(Node3D target, float baseDamage);

    /// <summary>
    /// Preview damage with type.
    /// </summary>
    Godot.Collections.Dictionary PreviewDamage(Node3D target, float baseDamage, string damageType);

    /// <summary>
    /// Preview damage with full parameters.
    /// </summary>
    Godot.Collections.Dictionary PreviewDamage(
        Node3D target,
        float baseDamage,
        string damageType,
        bool isCritical);
}
