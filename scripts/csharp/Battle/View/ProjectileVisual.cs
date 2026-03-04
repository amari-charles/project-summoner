using Fateforged.Session;
using Fateforged.Simulation;
using Godot;

namespace Fateforged.View;

/// <summary>
/// Self-syncing visual shell for one projectile.
/// Reads its own SimProjectileData from IGameSession.GetState() each frame.
/// Exposes impact method called by EntityManager on ProjectileHitEvent.
///
/// Replaces Projectile3D — collision/damage logic moves to SimProjectile.
/// </summary>
public partial class ProjectileVisual : Node3D
{
    private IGameSession? _session;
    private int _projectileId;
    private bool _destroyed;

    private Node3D? _visualModel;

    // --- Initialization (called by EntityManager at spawn) ---

    public void Initialize(IGameSession session, int projectileId)
    {
        _session = session;
        _projectileId = projectileId;

        // Create placeholder sphere mesh (iterate on full visual in later pass)
        var mesh = new MeshInstance3D();
        mesh.Mesh = new SphereMesh { Radius = 0.15f, Height = 0.3f };
        mesh.Name = "Visual";
        AddChild(mesh);
        _visualModel = mesh;

        // Hide until first position sync (prevents ghost at 0,0,0)
        Visible = false;

        // Set initial position
        var state = session.GetState();
        if (state.Projectiles.TryGetValue(projectileId, out var projData))
        {
            GlobalPosition = SimulationNode.Current.SimToLocal(projData.CurrentPosition);
        }
    }

    // --- Self-Sync (continuous, every frame) ---

    public override void _PhysicsProcess(double delta)
    {
        if (_session == null || _destroyed) return;

        var state = _session.GetState();
        if (!state.Projectiles.TryGetValue(_projectileId, out var projData) || projData.IsDead)
        {
            PlayImpactAndDestroy();
            return;
        }

        // Reveal on first position sync
        if (!Visible) Visible = true;

        // Sync position
        GlobalPosition = SimulationNode.Current.SimToLocal(projData.CurrentPosition);

        // Sync rotation toward movement direction
        var dir = projData.Direction;
        var godotDir = new Vector3(dir.X, dir.Y, dir.Z);
        if (godotDir.LengthSquared() > 0.001f)
        {
            LookAt(GlobalPosition + godotDir, Vector3.Up);
        }
    }

    // --- Event Reactions (called by EntityManager) ---

    public void PlayImpactAndDestroy()
    {
        if (_destroyed) return;
        _destroyed = true;

        if (_visualModel != null)
            _visualModel.Visible = false;

        QueueFree();
    }
}
