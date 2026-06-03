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
    private const int RenderPriorityMin = -128;
    private const int RenderPriorityMax = 127;
    private const float SingleTargetDamageShapeDepthScale = 0.62f;
    private IGameSession? _session;
    private int _unitId;
    private bool _isAlive = true;
    private bool _loggedMissing;

    private IVisualComponent? _visual;
    private SpawnRevealComponent? _spawnReveal;
    private FloatingHPBar? _hpBar;
    private float _attackAnimTimer;
    private bool _isFacingRight;
    private string _currentMoveAnim = "";
    private EntityManager? _entityManager;
    private bool _tornadoVisualRotationActive;

    // Debug visualization markers (toggleable via BattlefieldDebugService autoload).
    private MeshInstance3D? _debugHurtboxMarker;
    private MeshInstance3D? _debugTargetPointMarker;
    private MeshInstance3D? _debugEngageRangeMarker;
    private MeshInstance3D? _debugEngageRangeSecondaryMarker;
    private MeshInstance3D? _debugDamageShapeMarker;
    private MeshInstance3D? _debugDamageShapeSecondaryMarker;
    private MeshInstance3D? _debugNavigationFootprintMarker;
    private int _debugEngageSignature;
    private float _debugNavigationFootprintRadius = -1f;
    private int _debugDamageShapeSignature;

    // --- IDamageableVisual ---

    public bool IsAlive => _isAlive;
    public int UnitId => _unitId;
    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; } = 1f;

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
            CurrentHp = unitData.CurrentHp;
            MaxHp = unitData.MaxHp;

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
            ApplyAuthoredVisualConfig(unitData);
        }
        else
        {
            Visible = false;
        }

        // Create floating HP bar as a child
        _hpBar = new FloatingHPBar();
        AddChild(_hpBar);
        _hpBar.Configure(HPBarSettings.Default);
        _hpBar.TrackNode(this);
        _hpBar.UpdateHp(CurrentHp, MaxHp);
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

    private void ApplyAuthoredVisualConfig(UnitData unitData)
    {
        if (_visual is not SkeletalVisualComponent skeletal || !unitData.CatalogId.HasValue)
            return;

        var unitDefinition = UnitDefinitions.Get(unitData.CatalogId.Value);
        if (unitDefinition == null || unitDefinition.Visual.DisplayScale <= 0f)
            return;

        float displayScale = unitDefinition.Visual.DisplayScale;
        if (Mathf.IsEqualApprox(displayScale, 1f))
            return;

        skeletal.SetScaleFactor(skeletal.ScaleFactor * displayScale);
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

        float nextHp = unitData.CurrentHp;
        float nextMaxHp = unitData.MaxHp;
        bool hpChanged =
            !Mathf.IsEqualApprox(CurrentHp, nextHp) || !Mathf.IsEqualApprox(MaxHp, nextMaxHp);
        CurrentHp = nextHp;
        MaxHp = nextMaxHp;
        if (hpChanged)
            _hpBar?.UpdateHp(CurrentHp, MaxHp);

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
        ApplyTornadoVisualRotation(unitData);

        bool isActive = unitData.ActivationState == ActivationState.Active;

        // Keep visuals aligned with simulation activation state.
        // Inactive units should not play attack/walk animations.
        if (!isActive)
            _attackAnimTimer = 0f;

        // Animation from BehaviorState (attack anim timer has priority while active)
        if (isActive && unitData.Action.AttackAnimationTimer > 0f && _attackAnimTimer <= 0f)
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
            || _debugDamageShapeSecondaryMarker != null
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
        {
            FreeMarker(ref _debugDamageShapeMarker);
            FreeMarker(ref _debugDamageShapeSecondaryMarker);
        }

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

        CurrentHp = 0f;
        _hpBar?.UpdateHpImmediate(0f, MaxHp);

        _spawnReveal?.Cancel();

        CleanupDebugMarkers();
        ResetTornadoVisualRotation();
        _isAlive = false;
        Visible = false;
        CallDeferred(MethodName.QueueFree);
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

    private void ApplyTornadoVisualRotation(UnitData unitData)
    {
        var visual = _visual;
        if (visual is not Node3D visualNode)
            return;

        ActiveBuff? carry = null;
        foreach (var buff in unitData.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.TornadoCarry)
            {
                carry = buff;
                break;
            }
        }

        if (carry == null)
        {
            ResetTornadoVisualRotation();
            return;
        }

        float yaw = ResolveTornadoTangentYaw(carry, SimulationNode.Current);
        visual.SetBillboardSuppressed(true);
        visualNode.Rotation = new Vector3(0f, yaw, 0f);
        _tornadoVisualRotationActive = true;
    }

    private static float ResolveTornadoTangentYaw(ActiveBuff carry, SimulationNode? simNode)
    {
        float spinDirection = carry.TornadoOrbitAngularSpeedRadians >= 0f ? 1f : -1f;
        float tangentX = -Mathf.Sin(carry.TornadoOrbitAngleRadians) * spinDirection;
        float tangentZ = Mathf.Cos(carry.TornadoOrbitAngleRadians) * spinDirection;

        if (simNode != null && !simNode.IsHost)
            tangentX = -tangentX;

        if (Mathf.Abs(tangentX) < 0.0001f && Mathf.Abs(tangentZ) < 0.0001f)
            return 0f;

        return Mathf.Atan2(-tangentZ, tangentX);
    }

    private void ResetTornadoVisualRotation()
    {
        if (!_tornadoVisualRotationActive)
            return;

        var visual = _visual;
        if (visual is Node3D visualNode)
            visualNode.Rotation = Vector3.Zero;
        visual?.SetBillboardSuppressed(false);

        _tornadoVisualRotationActive = false;
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

    private void UpdateDebugTargetPointMarker(UnitData unitData)
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
        Vector3 targetPointOffset = ResolveVisualTargetPointOffset(unitData);
        _debugTargetPointMarker.GlobalPosition = new Vector3(
            GlobalPosition.X + targetPointOffset.X,
            GlobalPosition.Y + (spriteHeight * 0.5f) + targetPointOffset.Y,
            GlobalPosition.Z + targetPointOffset.Z
        );
    }

    private Vector3 ResolveVisualTargetPointOffset(UnitData unitData)
    {
        if (!unitData.CatalogId.HasValue)
            return Vector3.Zero;

        var unitDefinition = UnitDefinitions.Get(unitData.CatalogId.Value);
        if (unitDefinition == null)
            return Vector3.Zero;

        var offset = unitDefinition.Visual.TargetPointOffset;
        float mirroredOffsetX = _isFacingRight ? offset.X : -offset.X;
        return new Vector3(mirroredOffsetX, offset.Y, offset.Z);
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
            FreeMarker(ref _debugDamageShapeSecondaryMarker);
            _debugDamageShapeSignature = 0;
            return;
        }

        bool isForwardOffsetCorridor =
            shapeSpec.Kind == DamageShapeMarkerKind.Corridor && shapeSpec.ForwardOffset > 0.01f;

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
                        isForwardOffsetCorridor
                            ? new Color(0.2f, 0.8f, 1.0f, 0.09f)
                            : new Color(0.2f, 0.8f, 1.0f, 0.24f),
                        97
                    );
            AddChild(_debugDamageShapeMarker);

            FreeMarker(ref _debugDamageShapeSecondaryMarker);
            if (isForwardOffsetCorridor)
            {
                _debugDamageShapeSecondaryMarker = CreateDebugCorridorOutline(
                    shapeSpec.Length,
                    shapeSpec.HalfWidth,
                    new Color(0.2f, 0.8f, 1.0f, 0.55f),
                    98
                );
                AddChild(_debugDamageShapeSecondaryMarker);
            }
            _debugDamageShapeSignature = signature;
        }

        if (_debugDamageShapeMarker == null)
            return;

        var targetPosition = ResolveDamageShapeTargetPosition(unitData);

        if (shapeSpec.Kind == DamageShapeMarkerKind.Disc)
        {
            _debugDamageShapeMarker.Scale =
                unitData.Attack.Selection.Mode == AttackSelectionMode.Single
                    ? new Vector3(1f, 1f, SingleTargetDamageShapeDepthScale)
                    : Vector3.One;
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
            FreeMarker(ref _debugDamageShapeSecondaryMarker);
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
        if (_debugDamageShapeSecondaryMarker != null)
        {
            _debugDamageShapeSecondaryMarker.GlobalPosition = _debugDamageShapeMarker.GlobalPosition;
            _debugDamageShapeSecondaryMarker.Rotation = _debugDamageShapeMarker.Rotation;
        }
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

    private float ResolveSingleTargetDamageShapeRadius(UnitData unitData)
    {
        float authoredRadius = unitData.Attack.Area.SingleTargetRadius;
        if (authoredRadius > 0f)
            return Mathf.Max(0.1f, authoredRadius);

        float geometryRadius = Mathf.Max(
            ResolveHurtboxRadius(unitData),
            ResolveNavigationFootprintRadius(unitData)
        );
        float spriteWidthRadius = _visual != null ? Mathf.Max(0f, _visual.GetSpriteWidth() * 0.5f) : 0f;
        if (spriteWidthRadius > 0f)
            return Mathf.Max(geometryRadius, spriteWidthRadius);

        return geometryRadius;
    }

    private DamageShapeSpec BuildDamageShapeSpec(UnitData unitData)
    {
        switch (unitData.Attack.Selection.Mode)
        {
            case AttackSelectionMode.Single:
            {
                return DamageShapeSpec.Disc(ResolveSingleTargetDamageShapeRadius(unitData));
            }

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

        int? targetId = unitData.Engagement.TargetUnitId ?? unitData.TargetNetworkId;
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

    private static MeshInstance3D CreateDebugCorridorOutline(
        float length,
        float halfWidth,
        Color color,
        int renderPriority
    )
    {
        float halfLen = Mathf.Max(0.05f, length * 0.5f);
        float halfW = Mathf.Max(0.05f, halfWidth);
        const float y = 0.031f;
        var mesh = new ImmediateMesh();
        var mat = CreateDebugMaterial(color, renderPriority);

        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, mat);
        var p0 = new Vector3(-halfLen, y, -halfW);
        var p1 = new Vector3(-halfLen, y, halfW);
        var p2 = new Vector3(halfLen, y, halfW);
        var p3 = new Vector3(halfLen, y, -halfW);
        mesh.SurfaceAddVertex(p0);
        mesh.SurfaceAddVertex(p1);
        mesh.SurfaceAddVertex(p1);
        mesh.SurfaceAddVertex(p2);
        mesh.SurfaceAddVertex(p2);
        mesh.SurfaceAddVertex(p3);
        mesh.SurfaceAddVertex(p3);
        mesh.SurfaceAddVertex(p0);
        mesh.SurfaceEnd();

        return new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = mat,
        };
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
        FreeMarker(ref _debugDamageShapeSecondaryMarker);
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
