using Fateforged.Data.Projectiles;
using Fateforged.Projectiles;
using Fateforged.Session;
using Fateforged.Simulation;
using Godot;
using Fateforged.Units;

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
    private bool _rotateToDirection = true;

    private Node3D? _visualModel;

    // --- Initialization (called by EntityManager at spawn) ---

    public void Initialize(IGameSession session, int projectileId)
    {
        _session = session;
        _projectileId = projectileId;

        // Hide until first position sync (prevents ghost at 0,0,0)
        Visible = false;

        // Set initial position
        var state = session.GetState();
        if (state.Projectiles.TryGetValue(projectileId, out var projData))
        {
            var simNode = SimulationNode.Current;
            if (simNode != null)
                GlobalPosition = simNode.SimToLocal(projData.CurrentPosition);

            var projectileData = ResolveProjectileData(state, projData.SourceUnitId);
            _rotateToDirection = projectileData?.RotateToDirection ?? true;
            SpawnVisual(projectileData);
        }
        else
        {
            SpawnVisual(null);
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
        var simNode = SimulationNode.Current;
        if (simNode == null)
            return;

        GlobalPosition = simNode.SimToLocal(projData.CurrentPosition);

        // Sync rotation toward movement direction
        if (_rotateToDirection)
        {
            var dir = projData.Direction;
            var godotDir = new Vector3(dir.X, dir.Y, dir.Z);
            if (godotDir.LengthSquared() > 0.001f)
            {
                LookAt(GlobalPosition + godotDir, Vector3.Up);
            }
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

    private void SpawnVisual(ProjectileData? projectileData)
    {
        Node3D? visual = null;
        if (projectileData?.VisualScene != null)
        {
            visual = projectileData.VisualScene.Instantiate<Node3D>();
        }
        else if (projectileData != null && !string.IsNullOrEmpty(projectileData.ModelScenePath)
                 && ResourceLoader.Exists(projectileData.ModelScenePath))
        {
            var packed = ResourceLoader.Load<PackedScene>(projectileData.ModelScenePath);
            if (packed != null)
                visual = packed.Instantiate<Node3D>();
        }

        if (visual == null)
        {
            var mesh = new MeshInstance3D();
            mesh.Mesh = new SphereMesh { Radius = 0.15f, Height = 0.3f };
            mesh.Name = "VisualFallback";
            visual = mesh;
        }

        AddChild(visual);
        _visualModel = visual;
    }

    private static ProjectileData? ResolveProjectileData(Fateforged.Simulation.Data.MatchState state, int sourceUnitId)
    {
        if (!state.Units.TryGetValue(sourceUnitId, out var sourceUnit))
            return null;
        if (string.IsNullOrEmpty(sourceUnit.CatalogId))
            return null;

        var unitDef = UnitDefinitions.Get(sourceUnit.CatalogId);
        if (unitDef?.Ranged == null)
            return null;

        return ProjectileDefinitions.Get(unitDef.Ranged.ProjectileId);
    }
}
