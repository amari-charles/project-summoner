using Fateforged.Data.Projectiles;
using Fateforged.Infrastructure.Debug;
using Fateforged.Infrastructure.Pooling;
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
/// Implements IPoolable for reuse via NodePool. Only EntityManager manages lifecycle —
/// this class never self-destructs.
/// </summary>
public partial class ProjectileVisual : Node3D, IPoolable
{
    private IGameSession? _session;
    private int _projectileId;
    private bool _deactivated;
    private bool _rotateToDirection = true;

    private Node3D? _visualModel;
    private MeshInstance3D? _debugHitRadiusMarker;
    private MeshInstance3D? _debugAoeRadiusMarker;
    private float _debugHitRadius = -1f;
    private float _debugAoeRadius = -1f;
    private ProjectileHitSpace _debugHitSpace = ProjectileHitSpace.GroundCylinder;
    private ProjectileHitSpace _debugAoeHitSpace = ProjectileHitSpace.GroundCylinder;

    // --- IPoolable ---

    public void OnAcquired()
    {
        SetPhysicsProcess(true);
        SetProcess(true);
    }

    public void OnReleased()
    {
        Visible = false;
        SetPhysicsProcess(false);
        SetProcess(false);
    }

    public void ResetState()
    {
        _session = null;
        _projectileId = 0;
        _deactivated = false;
        _rotateToDirection = true;

        CleanupDebugMarkers();

        // Free the visual model child so a fresh one is created on next Initialize
        if (_visualModel != null)
        {
            _visualModel.QueueFree();
            _visualModel = null;
        }

        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
    }

    // --- Initialization (called by EntityManager at spawn) ---

    public void Initialize(IGameSession session, int projectileId)
    {
        _session = session;
        _projectileId = projectileId;
        _deactivated = false;

        // Hide until first position sync (prevents ghost at 0,0,0)
        Visible = false;

        // Set initial position
        var state = session.GetState();
        if (state.Projectiles.TryGetValue(projectileId, out var projData))
        {
            var simNode = SimulationNode.Current;
            if (simNode != null)
                GlobalPosition = simNode.SimToLocal(projData.CurrentPosition);

            var projectileData = ResolveProjectileData(state, projData);
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
        if (_session == null || _deactivated) return;

        var state = _session.GetState();
        if (!state.Projectiles.TryGetValue(_projectileId, out var projData) || projData.IsDead)
        {
            // Don't self-destruct — EntityManager will handle cleanup via Deactivate()
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

    public override void _Process(double delta)
    {
        bool debugEnabled = BattlefieldDebugService.Instance?.ProjectileHitGeometryEnabled == true;
        bool anyMarkerExists = _debugHitRadiusMarker != null || _debugAoeRadiusMarker != null;
        if (!debugEnabled && !anyMarkerExists)
            return;

        if (_session == null || _deactivated)
        {
            CleanupDebugMarkers();
            return;
        }

        var state = _session.GetState();
        if (!state.Projectiles.TryGetValue(_projectileId, out var projData) || projData.IsDead)
        {
            CleanupDebugMarkers();
            return;
        }

        if (!debugEnabled)
        {
            CleanupDebugMarkers();
            return;
        }

        UpdateDebugHitRadiusMarker(projData);

        if (projData.AoeRadius > 0f)
            UpdateDebugAoeRadiusMarker(projData);
        else
            FreeMarker(ref _debugAoeRadiusMarker);
    }

    // --- Event Reactions (called by EntityManager) ---

    /// <summary>
    /// Deactivate this visual (hide model, cleanup debug markers).
    /// Does NOT free the node — EntityManager releases it back to the pool.
    /// </summary>
    public void Deactivate()
    {
        if (_deactivated) return;
        _deactivated = true;

        if (_visualModel != null)
            _visualModel.Visible = false;

        CleanupDebugMarkers();
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

    private void UpdateDebugHitRadiusMarker(Fateforged.Simulation.Data.SimProjectileData projData)
    {
        float radius = Mathf.Max(0.05f, projData.HitRadius);
        bool needsRebuild = _debugHitRadiusMarker == null ||
                            !Mathf.IsEqualApprox(radius, _debugHitRadius) ||
                            _debugHitSpace != projData.HitSpace;

        if (needsRebuild)
        {
            FreeMarker(ref _debugHitRadiusMarker);
            _debugHitRadiusMarker = CreateGeometryMarker(
                radius,
                projData.HitSpace,
                new Color(0.2f, 0.9f, 1.0f, 0.28f),
                100);
            AddChild(_debugHitRadiusMarker);
            _debugHitRadius = radius;
            _debugHitSpace = projData.HitSpace;
        }

        PositionGeometryMarker(_debugHitRadiusMarker, projData.HitSpace);
    }

    private void UpdateDebugAoeRadiusMarker(Fateforged.Simulation.Data.SimProjectileData projData)
    {
        float radius = Mathf.Max(0.05f, projData.AoeRadius);
        bool needsRebuild = _debugAoeRadiusMarker == null ||
                            !Mathf.IsEqualApprox(radius, _debugAoeRadius) ||
                            _debugAoeHitSpace != projData.HitSpace;

        if (needsRebuild)
        {
            FreeMarker(ref _debugAoeRadiusMarker);
            _debugAoeRadiusMarker = CreateGeometryMarker(
                radius,
                projData.HitSpace,
                new Color(1.0f, 0.4f, 0.2f, 0.2f),
                99);
            AddChild(_debugAoeRadiusMarker);
            _debugAoeRadius = radius;
            _debugAoeHitSpace = projData.HitSpace;
        }

        PositionGeometryMarker(_debugAoeRadiusMarker, projData.HitSpace);
    }

    private void PositionGeometryMarker(MeshInstance3D? marker, ProjectileHitSpace hitSpace)
    {
        if (marker == null)
            return;

        if (hitSpace == ProjectileHitSpace.GroundCylinder)
        {
            marker.GlobalPosition = new Vector3(GlobalPosition.X, 0.04f, GlobalPosition.Z);
            marker.Rotation = Vector3.Zero;
            return;
        }

        marker.Position = Vector3.Zero;
        marker.Rotation = Vector3.Zero;
    }

    private static MeshInstance3D CreateGeometryMarker(
        float radius, ProjectileHitSpace hitSpace, Color color, int renderPriority)
    {
        var marker = new MeshInstance3D
        {
            Mesh = hitSpace == ProjectileHitSpace.GroundCylinder
                ? new CylinderMesh
                {
                    TopRadius = radius,
                    BottomRadius = radius,
                    Height = 0.04f
                }
                : new SphereMesh
                {
                    Radius = radius,
                    Height = radius * 2f
                },
            MaterialOverride = CreateDebugMaterial(color, renderPriority)
        };
        return marker;
    }

    private static StandardMaterial3D CreateDebugMaterial(Color color, int renderPriority)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            NoDepthTest = true,
            RenderPriority = renderPriority
        };
    }

    private void CleanupDebugMarkers()
    {
        FreeMarker(ref _debugHitRadiusMarker);
        FreeMarker(ref _debugAoeRadiusMarker);
        _debugHitRadius = -1f;
        _debugAoeRadius = -1f;
        _debugHitSpace = ProjectileHitSpace.GroundCylinder;
        _debugAoeHitSpace = ProjectileHitSpace.GroundCylinder;
    }

    private static void FreeMarker(ref MeshInstance3D? marker)
    {
        if (marker == null)
            return;

        marker.QueueFree();
        marker = null;
    }

    private static ProjectileData? ResolveProjectileData(Fateforged.Simulation.Data.MatchState state, Fateforged.Simulation.Data.SimProjectileData projData)
    {
        if (!string.IsNullOrEmpty(projData.ProjectileCatalogId))
        {
            var byProjectileId = ProjectileDefinitions.Get(projData.ProjectileCatalogId);
            if (byProjectileId != null)
                return byProjectileId;
        }

        int sourceUnitId = projData.SourceUnitId;
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
