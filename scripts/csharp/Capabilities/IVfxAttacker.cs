using Godot;
using ProjectSummoner.Vfx;

namespace ProjectSummoner.Capabilities;

/// <summary>
/// Interface for units with VFX-based attacks (lightning, beams, etc.).
/// These attacks don't spawn projectiles but instead use visual effects.
/// </summary>
public interface IVfxAttacker
{
    /// <summary>ID of the VFX to spawn for attacks.</summary>
    VfxId AttackVfxId { get; }

    /// <summary>
    /// Spawn attack VFX toward the target.
    /// </summary>
    /// <param name="target">The target being attacked.</param>
    void SpawnAttackVfx(Node3D target);
}
