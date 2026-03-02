using System;
using Fateforged.Session;
using Godot;

namespace Fateforged.View;

/// <summary>
/// Self-syncing visual shell for one projectile.
/// Reads its own SimProjectileData from IGameSession.GetState() each frame.
/// Exposes impact method called by EntityManager on ProjectileHitSimEvent.
///
/// Replaces Projectile3D — collision/damage logic moves to SimProjectile.
/// </summary>
public partial class ProjectileVisual : Node3D
{
    private IGameSession? _session;
    private int _projectileId;

    private Node3D? _visualModel;
    private GpuParticles3D? _trail;

    // --- Initialization (called by EntityManager at spawn) ---

    public void Initialize(IGameSession session, int projectileId)
    {
        throw new NotImplementedException();
    }

    // --- Self-Sync (continuous, every frame) ---

    public override void _PhysicsProcess(double delta)
    {
        // Read SimProjectileData from _session.GetState().Projectiles[_projectileId]
        // Sync: position, rotation toward movement direction
        throw new NotImplementedException();
    }

    // --- Event Reactions (called by EntityManager) ---

    public void PlayImpactAndDestroy()
    {
        // 1. Stop trail emission
        // 2. VFXManager impact VFX at position
        // 3. Hide model
        // 4. Timer for trail fade, then QueueFree
        throw new NotImplementedException();
    }
}
