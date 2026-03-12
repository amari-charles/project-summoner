using Fateforged.Constants;
using Fateforged.Infrastructure.Debug;
using Fateforged.Meta;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.UI;
using Fateforged.Units;
using Fateforged.Visual;
using Godot;

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
    private SpawnRevealComponent? _spawnReveal;
    private float _attackAnimTimer;
    private bool _isFacingRight;
    private string _currentMoveAnim = "";
    private EntityManager? _entityManager;

    // Debug visualization markers (toggleable via BattlefieldDebugService autoload).
    private MeshInstance3D? _debugHurtboxMarker;
    private MeshInstance3D? _debugTargetPointMarker;
    private MeshInstance3D? _debugEngageRangeMarker;
    private MeshInstance3D? _debugEngageRangeSecondaryMarker;
    private MeshInstance3D? _debugDamageShapeMarker;
    private MeshInstance3D? _debugNavigationFootprintMarker;
    private int _debugEngageSignature;
    private float _debugNavigationFootprintRadius = -1f;
    private int _debugDamageShapeSignature;

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
                _spawnReveal = new SpawnRevealComponent(this, () => _visual, () => unitData.Team);
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
        if (_session == null || !_isAlive)
            return;
        var simNode = SimulationNode.Current;
        if (simNode == null)
            return;

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
        if (!Visible)
            Visible = true;

        // Sync position
        var authoritativePosition = simNode.SimToLocal(unitData.Position);
        GlobalPosition =
            _entityManager?.ResolveUnitRenderPosition(_unitId, authoritativePosition)
            ?? authoritativePosition;

        // Sync facing (flip for client since X axis is mirrored)
        bool localFacing = unitData.IsFacingRight;
        if (!simNode.IsHost)
            localFacing = !localFacing;

        if (_isFacingRight != localFacing)
        {
            _isFacingRight = localFacing;
            _visual?.SetFlipH(_isFacingRight);
        }

        bool isActive = unitData.ActivationState == ActivationState.Active;

        // Keep visuals aligned with simulation activation state.
        // Inactive units should not play attack/walk animations.
        if (!isActive)
            _attackAnimTimer = 0f;

        // Animation from BehaviorState (attack anim timer has priority while active)
        if (isActive && unitData.AttackAnimationTimer > 0f && _attackAnimTimer <= 0f)
            PlayAttackAnimation();

        if (_attackAnimTimer > 0)
        {
            _attackAnimTimer -= (float)delta;
        }
        else if (_visual != null)
        {
            string desiredMoveAnim = !isActive
                ? "idle"
                : unitData.BehaviorState switch
                {
                    BehaviorState.Attacking => "idle",
                    BehaviorState.InRange => "idle",
                    _ => "walk",
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
        bool anyMarkerExists =
            _debugHurtboxMarker != null
            || _debugTargetPointMarker != null
            || _debugEngageRangeMarker != null
            || _debugEngageRangeSecondaryMarker != null
            || _debugDamageShapeMarker != null
            || _debugNavigationFootprintMarker != null;

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

        if (BattlefieldDebugService.Instance?.EngageRangeEnabled == true)
            UpdateDebugEngageRangeMarker(unitData);
        else
            ClearEngageMarkers();

        if (BattlefieldDebugService.Instance?.DamageShapeEnabled == true)
            UpdateDebugDamageShapeMarker(unitData);
        else
            FreeMarker(ref _debugDamageShapeMarker);

        if (BattlefieldDebugService.Instance?.NavigationFootprintEnabled == true)
            UpdateDebugNavigationFootprintMarker(unitData);
        else
            FreeMarker(ref _debugNavigationFootprintMarker);
    }

    // --- Event Reactions (called by EntityManager) ---

    public void PlayAttackAnimation()
    {
        if (_visual == null)
            return;
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
        if (!_isAlive)
            return;
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
        float radius = ResolveHurtboxRadius(unitData);
        float height = ResolveHurtboxHeight(unitData);
        bool horizontal = unitData.HurtboxHorizontal;
        var offset = new Vector3(
            unitData.HurtboxOffset.X,
            unitData.HurtboxOffset.Y,
            unitData.HurtboxOffset.Z
        );

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

        float yAnchor = horizontal ? radius : (height * 0.5f);
        _debugHurtboxMarker.Position = new Vector3(offset.X, offset.Y + yAnchor, offset.Z);
        _debugHurtboxMarker.Rotation = horizontal
            ? new Vector3(0f, 0f, Mathf.Pi * 0.5f)
            : Vector3.Zero;
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

    private void UpdateDebugEngageRangeMarker(UnitData unitData)
    {
        EngageShape engageShape = ResolveEngageShape(unitData);
        switch (engageShape)
        {
            case EngageShape.Cone:
            {
                float range = Mathf.Max(0.2f, unitData.AttackRange);
                float coneHalfAngle = Mathf.Clamp(unitData.ConeHalfAngle, 1f, 89f);
                float coneCenterOffset = unitData.ConeCenterOffsetDegrees;
                int signature = BuildEngageSignature(
                    engageShape,
                    range,
                    coneHalfAngle,
                    0f,
                    0f,
                    coneCenterOffset
                );

                if (_debugEngageRangeMarker == null || signature != _debugEngageSignature)
                {
                    ClearEngageMarkers();
                    _debugEngageRangeMarker = CreateDebugCone(
                        range,
                        coneHalfAngle,
                        new Color(1.0f, 0.8f, 0.2f, 0.3f),
                        99
                    );
                    AddChild(_debugEngageRangeMarker);
                    _debugEngageSignature = signature;
                }

                if (_debugEngageRangeMarker == null)
                    return;

                _debugEngageRangeMarker.GlobalPosition = new Vector3(
                    GlobalPosition.X,
                    0.05f,
                    GlobalPosition.Z
                );
                float yRotation =
                    (_isFacingRight ? 0f : Mathf.Pi) + Mathf.DegToRad(coneCenterOffset);
                _debugEngageRangeMarker.Rotation = new Vector3(0f, yRotation, 0f);
                return;
            }

            case EngageShape.ForwardRect:
            {
                float length =
                    unitData.EngageRectLength > 0f
                        ? unitData.EngageRectLength
                        : Mathf.Max(unitData.AttackRange * 0.9f, 0.1f);
                float halfWidth =
                    unitData.EngageRectHalfWidth > 0f ? unitData.EngageRectHalfWidth : 0.45f;
                float forwardOffset = Mathf.Max(unitData.EngageRectForwardOffset, 0f);
                float closeRadius = Mathf.Max(unitData.EngageCloseRadius, 0.05f);
                int signature = BuildEngageSignature(
                    engageShape,
                    length,
                    halfWidth,
                    forwardOffset,
                    closeRadius,
                    0f
                );

                if (
                    _debugEngageRangeMarker == null
                    || _debugEngageRangeSecondaryMarker == null
                    || signature != _debugEngageSignature
                )
                {
                    ClearEngageMarkers();
                    _debugEngageRangeMarker = CreateDebugCorridor(
                        length,
                        halfWidth,
                        new Color(1.0f, 0.8f, 0.2f, 0.26f),
                        99
                    );
                    _debugEngageRangeSecondaryMarker = CreateDebugDisc(
                        closeRadius,
                        new Color(1.0f, 0.8f, 0.2f, 0.22f),
                        99
                    );
                    AddChild(_debugEngageRangeMarker);
                    AddChild(_debugEngageRangeSecondaryMarker);
                    _debugEngageSignature = signature;
                }

                if (_debugEngageRangeMarker == null || _debugEngageRangeSecondaryMarker == null)
                    return;

                float directionSign = _isFacingRight ? 1f : -1f;
                float centerDistance = forwardOffset + (length * 0.5f);
                _debugEngageRangeMarker.GlobalPosition = new Vector3(
                    GlobalPosition.X + (centerDistance * directionSign),
                    0.05f,
                    GlobalPosition.Z
                );
                _debugEngageRangeMarker.Rotation = new Vector3(
                    0f,
                    _isFacingRight ? 0f : Mathf.Pi,
                    0f
                );

                _debugEngageRangeSecondaryMarker.GlobalPosition = new Vector3(
                    GlobalPosition.X,
                    0.05f,
                    GlobalPosition.Z
                );
                _debugEngageRangeSecondaryMarker.Rotation = Vector3.Zero;
                return;
            }

            default:
            {
                float range = Mathf.Max(0.2f, unitData.AttackRange);
                int signature = BuildEngageSignature(engageShape, range, 0f, 0f, 0f, 0f);

                if (_debugEngageRangeMarker == null || signature != _debugEngageSignature)
                {
                    ClearEngageMarkers();
                    _debugEngageRangeMarker = CreateDebugDisc(
                        range,
                        new Color(1.0f, 0.8f, 0.2f, 0.3f),
                        99
                    );
                    AddChild(_debugEngageRangeMarker);
                    _debugEngageSignature = signature;
                }

                if (_debugEngageRangeMarker == null)
                    return;

                _debugEngageRangeMarker.GlobalPosition = new Vector3(
                    GlobalPosition.X,
                    0.05f,
                    GlobalPosition.Z
                );
                _debugEngageRangeMarker.Rotation = Vector3.Zero;
                return;
            }
        }
    }

    private void UpdateDebugDamageShapeMarker(UnitData unitData)
    {
        var shapeSpec = BuildDamageShapeSpec(unitData);
        if (shapeSpec.Kind == DamageShapeMarkerKind.None)
        {
            FreeMarker(ref _debugDamageShapeMarker);
            _debugDamageShapeSignature = 0;
            return;
        }

        int signature = BuildDamageShapeSignature(shapeSpec);
        if (_debugDamageShapeMarker == null || signature != _debugDamageShapeSignature)
        {
            FreeMarker(ref _debugDamageShapeMarker);
            _debugDamageShapeMarker =
                shapeSpec.Kind == DamageShapeMarkerKind.Disc
                    ? CreateDebugDisc(shapeSpec.Radius, new Color(0.2f, 0.8f, 1.0f, 0.24f), 97)
                    : CreateDebugCorridor(
                        shapeSpec.Length,
                        shapeSpec.HalfWidth,
                        new Color(0.2f, 0.8f, 1.0f, 0.24f),
                        97
                    );
            AddChild(_debugDamageShapeMarker);
            _debugDamageShapeSignature = signature;
        }

        if (_debugDamageShapeMarker == null)
            return;

        var targetPosition = ResolveDamageShapeTargetPosition(unitData);

        if (shapeSpec.Kind == DamageShapeMarkerKind.Disc)
        {
            if (ShouldAnchorDamageDiscToTarget(unitData, targetPosition) && targetPosition.HasValue)
            {
                var anchoredPosition = targetPosition.Value;
                _debugDamageShapeMarker.GlobalPosition = new Vector3(
                    anchoredPosition.X,
                    0.04f,
                    anchoredPosition.Z
                );
            }
            else
            {
                _debugDamageShapeMarker.GlobalPosition = new Vector3(
                    GlobalPosition.X,
                    0.04f,
                    GlobalPosition.Z
                );
            }
            _debugDamageShapeMarker.Rotation = Vector3.Zero;
            return;
        }

        Vector3 direction = ResolveDamageShapeDirection(unitData, targetPosition);
        float centerDistance = shapeSpec.ForwardOffset + (shapeSpec.Length * 0.5f);
        _debugDamageShapeMarker.GlobalPosition = new Vector3(
            GlobalPosition.X + (centerDistance * direction.X),
            0.04f,
            GlobalPosition.Z + (centerDistance * direction.Z)
        );
        _debugDamageShapeMarker.Rotation = new Vector3(
            0f,
            Mathf.Atan2(direction.Z, direction.X),
            0f
        );
    }

    private void UpdateDebugNavigationFootprintMarker(UnitData unitData)
    {
        float radius = ResolveNavigationFootprintRadius(unitData);
        bool needsRebuild =
            _debugNavigationFootprintMarker == null
            || !Mathf.IsEqualApprox(radius, _debugNavigationFootprintRadius);
        if (needsRebuild)
        {
            FreeMarker(ref _debugNavigationFootprintMarker);
            _debugNavigationFootprintMarker = CreateDebugDisc(
                radius,
                new Color(0.8f, 0.4f, 1.0f, 0.4f),
                98
            );
            AddChild(_debugNavigationFootprintMarker);
            _debugNavigationFootprintRadius = radius;
        }

        if (_debugNavigationFootprintMarker == null)
            return;

        _debugNavigationFootprintMarker.GlobalPosition = new Vector3(
            GlobalPosition.X,
            0.03f,
            GlobalPosition.Z
        );
        _debugNavigationFootprintMarker.Rotation = Vector3.Zero;
    }

    private float ResolveNavigationFootprintRadius(UnitData unitData)
    {
        if (unitData.NavigationRadius > 0f)
            return Mathf.Max(0.1f, unitData.NavigationRadius);
        return 0.5f;
    }

    private float ResolveHurtboxRadius(UnitData unitData)
    {
        if (unitData.HurtboxRadius > 0f)
            return Mathf.Max(0.05f, unitData.HurtboxRadius);
        return 0.5f;
    }

    private float ResolveHurtboxHeight(UnitData unitData)
    {
        if (unitData.HurtboxHeight > 0f)
            return Mathf.Max(unitData.HurtboxHeight, ResolveHurtboxRadius(unitData) * 2f);
        return Mathf.Max(1.0f, _visual?.GetSpriteHeight() ?? 2.0f);
    }

    private DamageShapeSpec BuildDamageShapeSpec(UnitData unitData)
    {
        switch (unitData.Attack.Selection.Mode)
        {
            case AttackSelectionMode.Single:
                return DamageShapeSpec.Disc(0.24f);

            case AttackSelectionMode.ChainHops:
            {
                float chainRadius =
                    unitData.Attack.Propagation.ChainJumpRadius > 0f
                        ? unitData.Attack.Propagation.ChainJumpRadius
                        : 0.5f;
                return DamageShapeSpec.Disc(chainRadius);
            }

            case AttackSelectionMode.LineCollect:
            {
                float length =
                    unitData.Attack.Area.LineLength > 0f
                        ? unitData.Attack.Area.LineLength
                        : Mathf.Max(unitData.AttackRange, 0.5f);
                float halfWidth =
                    unitData.Attack.Area.LineHalfWidth > 0f
                        ? unitData.Attack.Area.LineHalfWidth
                        : 0.5f;
                return DamageShapeSpec.Corridor(
                    length,
                    halfWidth,
                    unitData.Attack.Area.ForwardOffset
                );
            }

            case AttackSelectionMode.AreaCollect:
                return unitData.Attack.Area.Shape switch
                {
                    AttackAreaShape.Sphere => DamageShapeSpec.Disc(
                        unitData.Attack.Area.Size.X > 0f ? unitData.Attack.Area.Size.X : 0.5f
                    ),
                    AttackAreaShape.Box => DamageShapeSpec.Corridor(
                        unitData.Attack.Area.Size.X > 0f
                            ? unitData.Attack.Area.Size.X
                            : Mathf.Max(unitData.AttackRange, 0.5f),
                        unitData.Attack.Area.Size.Z > 0f ? unitData.Attack.Area.Size.Z : 0.5f,
                        unitData.Attack.Area.ForwardOffset
                    ),
                    AttackAreaShape.Capsule => DamageShapeSpec.Corridor(
                        unitData.Attack.Area.Size.X > 0f
                            ? unitData.Attack.Area.Size.X
                            : Mathf.Max(unitData.AttackRange, 0.5f),
                        unitData.Attack.Area.Size.Z > 0f ? unitData.Attack.Area.Size.Z : 0.5f,
                        unitData.Attack.Area.ForwardOffset
                    ),
                    AttackAreaShape.Line => DamageShapeSpec.Corridor(
                        unitData.Attack.Area.LineLength > 0f
                            ? unitData.Attack.Area.LineLength
                            : Mathf.Max(unitData.AttackRange, 0.5f),
                        unitData.Attack.Area.LineHalfWidth > 0f
                            ? unitData.Attack.Area.LineHalfWidth
                            : 0.5f,
                        unitData.Attack.Area.ForwardOffset
                    ),
                    _ => DamageShapeSpec.None,
                };

            default:
                return DamageShapeSpec.None;
        }
    }

    private static int BuildDamageShapeSignature(DamageShapeSpec spec)
    {
        unchecked
        {
            int hash = (int)spec.Kind;
            hash = (hash * 397) ^ Mathf.RoundToInt(spec.Radius * 1000f);
            hash = (hash * 397) ^ Mathf.RoundToInt(spec.Length * 1000f);
            hash = (hash * 397) ^ Mathf.RoundToInt(spec.HalfWidth * 1000f);
            hash = (hash * 397) ^ Mathf.RoundToInt(spec.ForwardOffset * 1000f);
            return hash;
        }
    }

    private static int BuildEngageSignature(
        EngageShape shape,
        float a,
        float b,
        float c,
        float d,
        float e
    )
    {
        unchecked
        {
            int hash = (int)shape;
            hash = (hash * 397) ^ Mathf.RoundToInt(a * 1000f);
            hash = (hash * 397) ^ Mathf.RoundToInt(b * 1000f);
            hash = (hash * 397) ^ Mathf.RoundToInt(c * 1000f);
            hash = (hash * 397) ^ Mathf.RoundToInt(d * 1000f);
            hash = (hash * 397) ^ Mathf.RoundToInt(e * 1000f);
            return hash;
        }
    }

    private static EngageShape ResolveEngageShape(UnitData unitData)
    {
        if (unitData.EngageShape != EngageShape.Circle)
            return unitData.EngageShape;
        return unitData.HasConeConstraint ? EngageShape.Cone : EngageShape.Circle;
    }

    private Vector3? ResolveDamageShapeTargetPosition(UnitData unitData)
    {
        if (_session == null)
            return null;

        int? targetId = unitData.TargetUnitId ?? unitData.TargetNetworkId;
        if (!targetId.HasValue)
            return null;

        var simTarget = SimUtils.ResolveTargetPosition(targetId.Value, _session.GetState());
        if (!simTarget.HasValue)
            return null;

        var simNode = SimulationNode.Current;
        if (simNode == null)
            return new Vector3(simTarget.Value.X, simTarget.Value.Y, simTarget.Value.Z);

        return simNode.SimToLocal(simTarget.Value);
    }

    private static bool ShouldAnchorDamageDiscToTarget(UnitData unitData, Vector3? targetPosition)
    {
        if (!targetPosition.HasValue)
            return false;

        if (unitData.Attack.Selection.Mode == AttackSelectionMode.Single)
            return true;

        return unitData.Attack.Selection.Mode switch
        {
            AttackSelectionMode.ChainHops => true,
            AttackSelectionMode.AreaCollect => unitData.Attack.Area.Shape == AttackAreaShape.Sphere,
            _ => false,
        };
    }

    private Vector3 ResolveDamageShapeDirection(UnitData unitData, Vector3? targetPosition)
    {
        bool useTargetDirection = unitData.Attack.Selection.Mode switch
        {
            AttackSelectionMode.LineCollect => true,
            AttackSelectionMode.AreaCollect => unitData.Attack.Area.Shape == AttackAreaShape.Capsule
                || unitData.Attack.Area.Shape == AttackAreaShape.Line,
            _ => false,
        };

        if (useTargetDirection && targetPosition.HasValue)
        {
            var toTarget = new Vector3(
                targetPosition.Value.X - GlobalPosition.X,
                0f,
                targetPosition.Value.Z - GlobalPosition.Z
            );
            if (toTarget.LengthSquared() > 0.0001f)
                return toTarget.Normalized();
        }

        return new Vector3(_isFacingRight ? 1f : -1f, 0f, 0f);
    }

    private static MeshInstance3D CreateDebugCapsule(
        float radius,
        float height,
        Color color,
        int renderPriority
    )
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = radius, Height = height },
            MaterialOverride = CreateDebugMaterial(color, renderPriority),
        };
        return mesh;
    }

    private static MeshInstance3D CreateDebugSphere(float radius, Color color, int renderPriority)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
            MaterialOverride = CreateDebugMaterial(color, renderPriority),
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
                Height = 0.05f,
            },
            MaterialOverride = CreateDebugMaterial(color, renderPriority),
        };
        return mesh;
    }

    private static MeshInstance3D CreateDebugCorridor(
        float length,
        float halfWidth,
        Color color,
        int renderPriority
    )
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(Mathf.Max(0.1f, length), 0.05f, Mathf.Max(0.1f, halfWidth * 2f)),
            },
            MaterialOverride = CreateDebugMaterial(color, renderPriority),
        };
        return mesh;
    }

    private static MeshInstance3D CreateDebugCone(
        float radius,
        float halfAngleDegrees,
        Color color,
        int renderPriority
    )
    {
        var mesh = new MeshInstance3D
        {
            Mesh = CreateConeMesh(radius, halfAngleDegrees),
            MaterialOverride = CreateDebugMaterial(color, renderPriority),
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

            var pA = new Vector3(
                Mathf.Cos(angleA) * radius,
                height * 0.5f,
                Mathf.Sin(angleA) * radius
            );
            var pB = new Vector3(
                Mathf.Cos(angleB) * radius,
                height * 0.5f,
                Mathf.Sin(angleB) * radius
            );
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
            RenderPriority = renderPriority,
        };
    }

    private void CleanupDebugMarkers()
    {
        FreeMarker(ref _debugHurtboxMarker);
        FreeMarker(ref _debugTargetPointMarker);
        ClearEngageMarkers();
        FreeMarker(ref _debugDamageShapeMarker);
        FreeMarker(ref _debugNavigationFootprintMarker);
        _debugNavigationFootprintRadius = -1f;
        _debugDamageShapeSignature = 0;
    }

    private void ClearEngageMarkers()
    {
        FreeMarker(ref _debugEngageRangeMarker);
        FreeMarker(ref _debugEngageRangeSecondaryMarker);
        _debugEngageSignature = 0;
    }

    private static void FreeMarker(ref MeshInstance3D? marker)
    {
        if (marker == null)
            return;
        marker.QueueFree();
        marker = null;
    }

    private enum DamageShapeMarkerKind
    {
        None = 0,
        Disc = 1,
        Corridor = 2,
    }

    private readonly struct DamageShapeSpec
    {
        private DamageShapeSpec(
            DamageShapeMarkerKind kind,
            float radius,
            float length,
            float halfWidth,
            float forwardOffset
        )
        {
            Kind = kind;
            Radius = radius;
            Length = length;
            HalfWidth = halfWidth;
            ForwardOffset = forwardOffset;
        }

        public DamageShapeMarkerKind Kind { get; }
        public float Radius { get; }
        public float Length { get; }
        public float HalfWidth { get; }
        public float ForwardOffset { get; }

        public static DamageShapeSpec None => new(DamageShapeMarkerKind.None, 0f, 0f, 0f, 0f);

        public static DamageShapeSpec Disc(float radius) =>
            new(DamageShapeMarkerKind.Disc, Mathf.Max(0.1f, radius), 0f, 0f, 0f);

        public static DamageShapeSpec Corridor(
            float length,
            float halfWidth,
            float forwardOffset
        ) =>
            new(
                DamageShapeMarkerKind.Corridor,
                0f,
                Mathf.Max(0.1f, length),
                Mathf.Max(0.05f, halfWidth),
                Mathf.Max(0f, forwardOffset)
            );
    }
}
