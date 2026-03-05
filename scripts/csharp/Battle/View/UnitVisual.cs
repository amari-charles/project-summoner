using Fateforged.Session;
using Fateforged.Simulation;
using Godot;
using Fateforged.Meta;
using Fateforged.UI;
using Fateforged.Visual;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Infrastructure.Debug;
using Fateforged.Constants;
using Fateforged.Units;

namespace Fateforged.View;

/// <summary>
/// Self-syncing visual shell for one unit.
/// Reads its own UnitData from IGameSession.GetState() each frame.
/// Exposes reaction methods called by EntityManager on discrete events.
///
/// Replaces Unit3D — game logic moves to Simulation subsystems.
/// </summary>
public partial class UnitVisual : Node3D, IDamageableVisual
{
    private const float RenderPriorityScale = 3.0f;
    private const float DeathAnimationDuration = 1.0f;
    private const int RenderPriorityMin = -128;
    private const int RenderPriorityMax = 127;

    private IGameSession? _session;
    private int _unitId;
    private bool _isAlive = true;
    private bool _loggedMissing;

    private IVisualComponent? _visual;
    private ShadowComponent? _shadow;
    private SpawnRevealComponent? _spawnReveal;
    private float _attackAnimTimer;
    private bool _isFacingRight;
    private string _currentMoveAnim = "";
    private EntityManager? _entityManager;

    // Debug visualization markers (toggleable via BattlefieldDebugService autoload).
    private MeshInstance3D? _debugHurtboxMarker;
    private MeshInstance3D? _debugTargetPointMarker;
    private MeshInstance3D? _debugAttackRangeMarker;
    private MeshInstance3D? _debugSeparationRadiusMarker;
    private bool _debugAttackRangeIsCone;
    private float _debugAttackRangeRadius = -1f;
    private float _debugAttackConeHalfAngle = -1f;
    private float _debugSeparationRadius = -1f;

    // --- IDamageableVisual ---

    public bool IsAlive => _isAlive;

    // --- Initialization (called by EntityManager at spawn) ---

    public void Initialize(IGameSession session, int unitId)
    {
        _session = session;
        _unitId = unitId;
        _entityManager = GetParentOrNull<EntityManager>();

        // Find IVisualComponent child (may be named "Visual" or implement the interface)
        _visual = GetNodeOrNull<Node3D>("Visual") as IVisualComponent;
        if (_visual == null)
        {
            // Search children for any IVisualComponent
            foreach (var child in GetChildren())
            {
                if (child is IVisualComponent vc)
                {
                    _visual = vc;
                    break;
                }
            }
        }

        // Find optional shadow
        foreach (var child in GetChildren())
        {
            if (child is ShadowComponent sc)
            {
                _shadow = sc;
                break;
            }
        }

        // Set initial position from state so shell is never at origin
        var state = session.GetState();
        var simNode = SimulationNode.Current;
        if (simNode == null)
        {
            Visible = false;
            GD.PrintErr("[UnitVisual] SimulationNode.Current is null during Initialize");
            return;
        }

        if (state.Units.TryGetValue(unitId, out var unitData))
        {
            RegisterGroups(unitData);
            GlobalPosition = simNode.SimToLocal(unitData.Position);

            // Start spawn reveal if unit has a spawn timer
            if (unitData.SpawnTimer > 0)
            {
                _spawnReveal = new SpawnRevealComponent(
                    this,
                    () => _visual,
                    () => _shadow as Node3D,
                    () => unitData.Team
                );
                Visible = true; // Must be visible for reveal shader to render
                _spawnReveal.StartReveal(unitData.SpawnTimer);
            }
            else
            {
                Visible = true;
            }

            // Set initial facing
            _isFacingRight = unitData.IsFacingRight;
            if (!simNode.IsHost)
                _isFacingRight = !_isFacingRight;
            _visual?.SetFlipH(_isFacingRight);
        }
        else
        {
            Visible = false;
        }

        // Create floating HP bar as a child
        var hpBar = new FloatingHPBar();
        AddChild(hpBar);
        hpBar.Configure(HPBarSettings.Default);
        hpBar.TrackNode(this);
    }

    private void RegisterGroups(UnitData unitData)
    {
        AddToGroup(GroupIDs.Units);

        if (unitData.Team == Team.Player)
            AddToGroup(GroupIDs.PlayerUnits);
        else
            AddToGroup(GroupIDs.EnemyUnits);

        if (unitData.MovementLayer == MovementLayer.Air)
            AddToGroup(GroupIDs.FlyingUnits);
    }

    // --- Self-Sync (continuous, every frame) ---

    public override void _PhysicsProcess(double delta)
    {
        if (_session == null || !_isAlive) return;
        var simNode = SimulationNode.Current;
        if (simNode == null) return;

        var state = _session.GetState();
        if (!state.Units.TryGetValue(_unitId, out var unitData))
        {
            if (!_loggedMissing)
            {
                GD.PrintErr($"[UnitVisual] UnitData not found for unitId={_unitId}");
                _loggedMissing = true;
            }
            return;
        }

        // Death fallback from snapshot
        if (!unitData.IsAlive && _isAlive)
        {
            BeginDeath();
            return;
        }

        // Reveal on first active frame
        if (!Visible) Visible = true;

        // Sync position
        var authoritativePosition = simNode.SimToLocal(unitData.Position);
        GlobalPosition = _entityManager?.ResolveUnitRenderPosition(_unitId, authoritativePosition) ?? authoritativePosition;

        // Sync facing (flip for client since X axis is mirrored)
        bool localFacing = unitData.IsFacingRight;
        if (!simNode.IsHost)
            localFacing = !localFacing;

        if (_isFacingRight != localFacing)
        {
            _isFacingRight = localFacing;
            _visual?.SetFlipH(_isFacingRight);
        }

        // Animation from BehaviorState (attack anim timer has priority)
        if (unitData.AttackAnimationTimer > 0f && _attackAnimTimer <= 0f)
            PlayAttackAnimation();

        if (_attackAnimTimer > 0)
        {
            _attackAnimTimer -= (float)delta;
        }
        else if (_visual != null)
        {
            string desiredMoveAnim = unitData.BehaviorState switch
            {
                BehaviorState.Attacking => "idle",
                BehaviorState.InRange => "idle",
                _ => "walk"
            };

            if (_currentMoveAnim != desiredMoveAnim || !_visual.IsPlaying())
            {
                _visual.PlayAnimation(desiredMoveAnim);
                _currentMoveAnim = desiredMoveAnim;
            }
        }

        // Update render priority for correct sprite layering
        float rawPriority = (-GlobalPosition.Z + GlobalPosition.Y) * RenderPriorityScale;
        int priority = (int)Mathf.Clamp(rawPriority, RenderPriorityMin, RenderPriorityMax);
        _visual?.SetRenderPriority(priority);
    }

    public override void _Process(double delta)
    {
        bool anyDebugEnabled = BattlefieldDebugService.Instance?.AnyUnitDebugEnabled ?? false;
        bool anyMarkerExists = _debugHurtboxMarker != null || _debugTargetPointMarker != null ||
                               _debugAttackRangeMarker != null || _debugSeparationRadiusMarker != null;

        if (!anyDebugEnabled && !anyMarkerExists)
            return;

        var unitData = GetCurrentUnitData();
        if (unitData == null || !unitData.IsAlive || !_isAlive)
        {
            CleanupDebugMarkers();
            return;
        }

        if (BattlefieldDebugService.Instance?.HurtboxEnabled == true)
            UpdateDebugHurtboxMarker(unitData);
        else
            FreeMarker(ref _debugHurtboxMarker);

        if (BattlefieldDebugService.Instance?.TargetPointEnabled == true)
            UpdateDebugTargetPointMarker(unitData);
        else
            FreeMarker(ref _debugTargetPointMarker);

        if (BattlefieldDebugService.Instance?.AttackRangeEnabled == true)
            UpdateDebugAttackRangeMarker(unitData);
        else
            FreeMarker(ref _debugAttackRangeMarker);

        if (BattlefieldDebugService.Instance?.SeparationRadiusEnabled == true)
            UpdateDebugSeparationRadiusMarker(unitData);
        else
            FreeMarker(ref _debugSeparationRadiusMarker);
    }

    // --- Event Reactions (called by EntityManager) ---

    public void PlayAttackAnimation()
    {
        if (_visual == null) return;
        _visual.PlayAnimation("attack");
        _attackAnimTimer = _visual.GetAnimationDuration("attack");
        _currentMoveAnim = "attack";
    }

    public void FlashDamage()
    {
        _visual?.FlashWhite();
    }

    public void BeginDeath()
    {
        if (!_isAlive) return;
        _isAlive = false;

        _spawnReveal?.Cancel();

        if (_visual != null)
        {
            _visual.PlayAnimation("death");
            _currentMoveAnim = "death";
        }

        CleanupDebugMarkers();

        // Queue free after death animation completes
        GetTree().CreateTimer(DeathAnimationDuration).Timeout += QueueFree;
    }

    public void ShowBuffIcon(EffectType effectType)
    {
        // Stub — visual implementation pending
        GD.Print($"[UnitVisual] ShowBuffIcon: {effectType} on unit {_unitId}");
    }

    public void ShowEvadeText()
    {
        // Stub — visual implementation pending
        GD.Print($"[UnitVisual] ShowEvadeText on unit {_unitId}");
    }

    private UnitData? GetCurrentUnitData()
    {
        if (_session == null)
            return null;

        var state = _session.GetState();
        return state.Units.TryGetValue(_unitId, out var unitData) ? unitData : null;
    }

    private void UpdateDebugHurtboxMarker(UnitData unitData)
    {
        float radius = Mathf.Max(0.5f, unitData.SeparationRadius);
        float height = Mathf.Max(1.0f, _visual?.GetSpriteHeight() ?? 2.0f);

        if (_debugHurtboxMarker == null)
        {
            _debugHurtboxMarker = CreateDebugCapsule(
                radius,
                height,
                new Color(0.2f, 1.0f, 0.2f, 0.25f),
                100
            );
            AddChild(_debugHurtboxMarker);
        }

        if (_debugHurtboxMarker.Mesh is CapsuleMesh capsule)
        {
            capsule.Radius = radius;
            capsule.Height = height;
        }

        _debugHurtboxMarker.Position = new Vector3(0f, height * 0.5f, 0f);
    }

    private void UpdateDebugTargetPointMarker(UnitData _unitData)
    {
        if (_debugTargetPointMarker == null)
        {
            _debugTargetPointMarker = CreateDebugSphere(
                0.3f,
                new Color(1.0f, 0.6f, 0.2f, 0.7f),
                101
            );
            AddChild(_debugTargetPointMarker);
        }

        float spriteHeight = _visual?.GetSpriteHeight() ?? 2.0f;
        _debugTargetPointMarker.GlobalPosition = new Vector3(
            GlobalPosition.X,
            GlobalPosition.Y + spriteHeight * 0.5f,
            GlobalPosition.Z
        );
    }

    private void UpdateDebugAttackRangeMarker(UnitData unitData)
    {
        float range = Mathf.Max(0.2f, unitData.AttackRange);
        bool isCone = unitData.HasConeConstraint;
        float coneHalfAngle = Mathf.Clamp(unitData.ConeHalfAngle, 1f, 89f);

        bool needsRebuild = _debugAttackRangeMarker == null ||
                            !Mathf.IsEqualApprox(range, _debugAttackRangeRadius) ||
                            _debugAttackRangeIsCone != isCone ||
                            (isCone && !Mathf.IsEqualApprox(coneHalfAngle, _debugAttackConeHalfAngle));

        if (needsRebuild)
        {
            FreeMarker(ref _debugAttackRangeMarker);
            _debugAttackRangeMarker = isCone
                ? CreateDebugCone(range, coneHalfAngle, new Color(1.0f, 0.8f, 0.2f, 0.3f), 99)
                : CreateDebugDisc(range, new Color(1.0f, 0.8f, 0.2f, 0.3f), 99);
            AddChild(_debugAttackRangeMarker);
            _debugAttackRangeRadius = range;
            _debugAttackRangeIsCone = isCone;
            _debugAttackConeHalfAngle = coneHalfAngle;
        }

        if (_debugAttackRangeMarker == null)
            return;

        _debugAttackRangeMarker.GlobalPosition = new Vector3(GlobalPosition.X, 0.05f, GlobalPosition.Z);
        if (isCone)
        {
            float yRotation = _isFacingRight ? 0f : Mathf.Pi;
            _debugAttackRangeMarker.Rotation = new Vector3(0f, yRotation, 0f);
        }
        else
        {
            _debugAttackRangeMarker.Rotation = Vector3.Zero;
        }
    }

    private void UpdateDebugSeparationRadiusMarker(UnitData unitData)
    {
        float radius = Mathf.Max(0.1f, unitData.SeparationRadius);
        bool needsRebuild = _debugSeparationRadiusMarker == null || !Mathf.IsEqualApprox(radius, _debugSeparationRadius);
        if (needsRebuild)
        {
            FreeMarker(ref _debugSeparationRadiusMarker);
            _debugSeparationRadiusMarker = CreateDebugDisc(radius, new Color(0.8f, 0.4f, 1.0f, 0.4f), 98);
            AddChild(_debugSeparationRadiusMarker);
            _debugSeparationRadius = radius;
        }

        if (_debugSeparationRadiusMarker == null)
            return;

        _debugSeparationRadiusMarker.GlobalPosition = new Vector3(GlobalPosition.X, 0.03f, GlobalPosition.Z);
        _debugSeparationRadiusMarker.Rotation = Vector3.Zero;
    }

    private static MeshInstance3D CreateDebugCapsule(float radius, float height, Color color, int renderPriority)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new CapsuleMesh
            {
                Radius = radius,
                Height = height
            },
            MaterialOverride = CreateDebugMaterial(color, renderPriority)
        };
        return mesh;
    }

    private static MeshInstance3D CreateDebugSphere(float radius, Color color, int renderPriority)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = radius,
                Height = radius * 2f
            },
            MaterialOverride = CreateDebugMaterial(color, renderPriority)
        };
        return mesh;
    }

    private static MeshInstance3D CreateDebugDisc(float radius, Color color, int renderPriority)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new CylinderMesh
            {
                TopRadius = radius,
                BottomRadius = radius,
                Height = 0.05f
            },
            MaterialOverride = CreateDebugMaterial(color, renderPriority)
        };
        return mesh;
    }

    private static MeshInstance3D CreateDebugCone(float radius, float halfAngleDegrees, Color color, int renderPriority)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = CreateConeMesh(radius, halfAngleDegrees),
            MaterialOverride = CreateDebugMaterial(color, renderPriority)
        };
        return mesh;
    }

    private static ArrayMesh CreateConeMesh(float radius, float halfAngleDegrees)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        const float height = 0.05f;
        const int segments = 16;

        float halfAngleRad = Mathf.DegToRad(halfAngleDegrees);
        float startAngle = -halfAngleRad;
        float endAngle = halfAngleRad;
        float angleStep = (endAngle - startAngle) / segments;

        var centerTop = new Vector3(0f, height * 0.5f, 0f);
        var centerBottom = new Vector3(0f, -height * 0.5f, 0f);

        for (int i = 0; i < segments; i++)
        {
            float angleA = startAngle + i * angleStep;
            float angleB = startAngle + (i + 1) * angleStep;

            var pA = new Vector3(Mathf.Cos(angleA) * radius, height * 0.5f, Mathf.Sin(angleA) * radius);
            var pB = new Vector3(Mathf.Cos(angleB) * radius, height * 0.5f, Mathf.Sin(angleB) * radius);
            var pABottom = new Vector3(pA.X, -height * 0.5f, pA.Z);
            var pBBottom = new Vector3(pB.X, -height * 0.5f, pB.Z);

            // Top face
            surfaceTool.AddVertex(centerTop);
            surfaceTool.AddVertex(pA);
            surfaceTool.AddVertex(pB);

            // Bottom face (reverse winding)
            surfaceTool.AddVertex(centerBottom);
            surfaceTool.AddVertex(pBBottom);
            surfaceTool.AddVertex(pABottom);
        }

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
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
        FreeMarker(ref _debugHurtboxMarker);
        FreeMarker(ref _debugTargetPointMarker);
        FreeMarker(ref _debugAttackRangeMarker);
        FreeMarker(ref _debugSeparationRadiusMarker);
        _debugAttackRangeRadius = -1f;
        _debugAttackConeHalfAngle = -1f;
        _debugSeparationRadius = -1f;
    }

    private static void FreeMarker(ref MeshInstance3D? marker)
    {
        if (marker == null)
            return;
        marker.QueueFree();
        marker = null;
    }
}
