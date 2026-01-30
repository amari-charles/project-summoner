using Godot;
using ProjectSummoner.Combat;
using ProjectSummoner.Combat.Hitbox;
using ProjectSummoner.Projectiles.Paths;

namespace ProjectSummoner.Projectiles;

/// <summary>
/// Data-driven 3D projectile system with path-based movement.
/// Supports multiple movement types via IProjectilePath implementations:
/// - Straight: Linear path (StraightPath)
/// - Arc/Homing: Quadratic Bézier curve with optional target tracking (ArcPath)
/// - Ballistic: Pre-computed parabolic trajectory (BallisticPath)
/// Uses collision-based hit detection via HitboxComponent/HurtboxComponent.
/// </summary>
[GlobalClass]
public partial class Projectile3D : Area3D
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>Grace period before ground collision activates (seconds).</summary>
    private const float GroundCollisionGracePeriod = 0.1f;

    /// <summary>Ground Y level.</summary>
    private const float GroundY = 0f;

    /// <summary>Default height offset for center mass estimate when target lacks get_projectile_target_position.</summary>
    private const float DefaultTargetHeightOffset = 0.5f;

    /// <summary>Collision layer for hitboxes (Layer 6 = bit 5).</summary>
    private const uint HitboxLayer = 1u << 5;

    /// <summary>Collision mask for hurtboxes (Layer 5 = bit 4).</summary>
    private const uint HurtboxMask = 1u << 4;

    /// <summary>Interval for updating target position in homing mode (seconds).</summary>
    private const float TargetUpdateInterval = 0.1f;

    /// <summary>Distance threshold below which prediction is disabled to prevent oscillation.</summary>
    private const float PredictionDisableDistance = 2.0f;

    /// <summary>
    /// Distance threshold for direct hit when path completes.
    /// Accounts for typical hurtbox radius (~0.5-1.0) plus margin for frame timing.
    /// </summary>
    private const float DirectHitDistanceThreshold = 1.5f;

    // =========================================================================
    // CONFIGURATION (set by ProjectileData)
    // =========================================================================

    public string ProjectileId { get; set; } = "";
    public ProjectileMovementType MovementType { get; set; } = ProjectileMovementType.Straight;
    public float Speed { get; set; } = 15f;
    public float Acceleration { get; set; } = 0f;
    public float MinSpeed { get; set; } = 1f;
    public float CurrentSpeed { get; set; } = 15f;
    public float Lifetime { get; set; } = 5f;
    public float ArcHeight { get; set; } = 2f;
    public float HomingStrength { get; set; } = 5f;
    public bool Tracking { get; set; } = false;
    public int PierceCount { get; set; } = 0;
    public float AoeRadius { get; set; } = 0f;
    public PackedScene? VisualScene { get; set; }
    public string HitVfx { get; set; } = "";
    public string TrailVfx { get; set; } = "";
    public bool FadeOnHit { get; set; } = true;
    public float FadeDuration { get; set; } = 0.5f;
    public float FadeInDuration { get; set; } = 0f;

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================

    public Node3D? Source { get; set; }
    public Node3D? Target { get; set; }
    public int Team { get; set; } = -1;
    public float Damage { get; set; } = 10f;
    public string DamageType { get; set; } = "physical";

    // Path-based movement state
    private IProjectilePath? _path;
    private float _progress = 0f;
    private float _timeSinceTargetUpdate = 0f;
    private Vector3 _startPosition = Vector3.Zero;
    private Vector3 _targetPosition = Vector3.Zero;
    private Vector3 _direction = Vector3.Forward;
    private float _timeAlive = 0f;
    private int _hitsRemaining = 0;
    private bool _impactTriggered = false;
    private bool _isFading = false;
    private Tween? _fadeTween;

    public bool IsPooled { get; set; } = false;
    public bool IsActive { get; set; } = false;

    /// <summary>Visual component instance.</summary>
    private Node3D? _visualInstance;

    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void ProjectileHitEventHandler(Node3D target, Projectile3D projectile);

    [Signal]
    public delegate void ProjectileExpiredEventHandler(Projectile3D projectile);

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        // Configure as hitbox for collision-based hit detection
        CollisionLayer = HitboxLayer;
        CollisionMask = HurtboxMask;
        Monitoring = true;
        Monitorable = false;

        // Connect area signals
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        // Instance visual if available
        if (VisualScene != null && _visualInstance == null)
        {
            _visualInstance = VisualScene.Instantiate<Node3D>();
            AddChild(_visualInstance);
            DuplicateMaterials();
        }
    }

    public override void _ExitTree()
    {
        // Kill any active tweens
        if (_fadeTween != null && _fadeTween.IsValid())
        {
            _fadeTween.Kill();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive)
            return;

        float dt = (float)delta;
        _timeAlive += dt;

        // Check lifetime expiration
        if (_timeAlive >= Lifetime)
        {
            ExpireImmediate();
            return;
        }

        // Check ground collision (after grace period)
        if (_timeAlive > GroundCollisionGracePeriod && GlobalPosition.Y <= GroundY + 0.2f)
        {
            TriggerImpactEffects(new Vector3(GlobalPosition.X, GroundY, GlobalPosition.Z));
            if (FadeOnHit)
                ExpireWithFade();
            else
                ExpireImmediate();
            return;
        }

        // Apply acceleration
        if (Acceleration != 0f)
        {
            CurrentSpeed += Acceleration * dt;
            if (Acceleration < 0f)
            {
                CurrentSpeed = Mathf.Max(CurrentSpeed, MinSpeed);
            }
        }

        // Path-based movement
        if (_path != null)
        {
            MoveAlongPath(dt);
        }

        // Rotate to face movement direction
        if (_direction.LengthSquared() > 0.001f)
        {
            LookAt(GlobalPosition + _direction, Vector3.Up);
        }
    }

    // =========================================================================
    // PATH-BASED MOVEMENT
    // =========================================================================

    /// <summary>
    /// Move along the computed path.
    /// Updates endpoint for moving targets (homing).
    /// </summary>
    private void MoveAlongPath(float delta)
    {
        if (_path == null)
            return;

        // For homing or tracking projectiles, periodically update target position
        bool shouldTrack = MovementType == ProjectileMovementType.Homing || Tracking;
        if (shouldTrack && IsInstanceValid(Target))
        {
            _timeSinceTargetUpdate += delta;
            if (_timeSinceTargetUpdate >= TargetUpdateInterval)
            {
                _timeSinceTargetUpdate = 0f;
                UpdatePathTarget();
            }
        }

        // Advance progress based on speed and path length
        float pathLength = _path.GetLength();
        if (pathLength > 0.01f)
        {
            _progress += (CurrentSpeed * delta) / pathLength;
        }

        // Clamp progress
        _progress = Mathf.Clamp(_progress, 0f, 1f);

        // Update position from path
        GlobalPosition = _path.GetPosition(_progress);
        _direction = _path.GetDirection(_progress);

        // Check if path is complete
        if (_progress >= 1f)
        {
            // If we have a valid target and reach path end, deal damage directly
            // This handles cases where collision detection has a frame delay
            if (IsInstanceValid(Target) && IsValidTarget(Target))
            {
                float distToTarget = GlobalPosition.DistanceTo(GetTargetPosition(Target));
                if (distToTarget < DirectHitDistanceThreshold)
                {
                    HitTarget(Target);
                    return;
                }
            }

            // No valid target or missed - just expire
            TriggerImpactEffects(GlobalPosition);
            if (FadeOnHit)
                ExpireWithFade();
            else
                ExpireImmediate();
        }
    }

    /// <summary>
    /// Update path for tracking moving targets.
    /// Recreates the path from the current position to maintain correct geometry.
    /// Uses predictive targeting for better interception.
    /// </summary>
    private void UpdatePathTarget()
    {
        if (Target == null || _path == null)
            return;

        Vector3 currentTargetPos = GetTargetPosition(Target);
        Vector3 predictedPos = CalculateInterceptPoint(currentTargetPos, Target);

        // Recreate path from current position to new target
        // This prevents progress overshooting when path length changes
        _startPosition = GlobalPosition;
        _targetPosition = predictedPos;
        _progress = 0f;
        CreatePath();
    }

    /// <summary>
    /// Create the appropriate path based on movement type.
    /// Called during initialization.
    /// </summary>
    private void CreatePath()
    {
        switch (MovementType)
        {
            case ProjectileMovementType.Straight:
                _path = new StraightPath(_startPosition, _targetPosition);
                break;

            case ProjectileMovementType.Arc:
            case ProjectileMovementType.Homing:
                // Both arc and homing use arc paths
                // Homing just updates the endpoint periodically
                _path = new ArcPath(_startPosition, _targetPosition, ArcHeight);
                break;

            case ProjectileMovementType.Ballistic:
                _path = new BallisticPath(_startPosition, _targetPosition, Speed);
                break;

            default:
                _path = new StraightPath(_startPosition, _targetPosition);
                break;
        }
    }

    // =========================================================================
    // COLLISION HANDLING
    // =========================================================================

    private void OnBodyEntered(Node3D body)
    {
        if (!IsActive)
            return;

        if (!IsValidTarget(body))
            return;

        HitTarget(body);
    }

    private void OnAreaEntered(Area3D area)
    {
        if (!IsActive)
            return;

        // Check if this is a HurtboxComponent
        var ownerEntityVar = area.Get("OwnerEntity");
        if (ownerEntityVar.VariantType != Variant.Type.Nil)
        {
            var hurtboxOwner = ownerEntityVar.As<Node3D>();
            if (hurtboxOwner != null && IsValidTarget(hurtboxOwner))
            {
                HitTargetViaHurtbox(hurtboxOwner, area);
                return;
            }
        }

        // Legacy fallback: check if area belongs to a unit
        var body = area.GetParent() as Node3D;
        if (body != null && IsValidTarget(body))
        {
            HitTarget(body);
        }
    }

    private bool IsValidTarget(Node3D body)
    {
        // Don't hit the source or children of source
        if (body == Source)
            return false;
        if (Source != null && body.IsAncestorOf(Source))
            return false;
        if (Source != null && Source.IsAncestorOf(body))
            return false;

        // Check team (C# units use PascalCase)
        var teamVar = body.Get("Team");
        if (teamVar.VariantType != Variant.Type.Nil && teamVar.AsInt32() == Team)
            return false;

        // Check if alive
        var isAliveVar = body.Get("IsAlive");
        if (isAliveVar.VariantType != Variant.Type.Nil && !isAliveVar.AsBool())
            return false;

        return true;
    }

    private void HitTarget(Node3D target)
    {
        // Apply damage via HitResolver for consistent hit handling
        if (IsInstanceValid(target) && IsInstanceValid(Source))
        {
            if (HitResolver.Instance != null)
            {
                HitResolver.Instance.ResolveProjectileHit(
                    Source, target, Damage, DamageType, GlobalPosition);
            }
            else
            {
                GD.PushWarning($"Projectile3D: HitResolver.Instance is null, cannot apply damage for '{ProjectileId}'");
            }
        }

        EmitSignal(SignalName.ProjectileHit, target, this);
        TriggerImpactEffects(GlobalPosition);
        HandlePierce();
    }

    private void HitTargetViaHurtbox(Node3D target, Area3D hurtbox)
    {
        // Use hurtbox position for more accurate hit location
        Vector3 hitPosition = hurtbox?.GlobalPosition ?? GlobalPosition;

        // Apply damage via HitResolver for consistent hit handling
        if (IsInstanceValid(target) && IsInstanceValid(Source))
        {
            if (HitResolver.Instance != null)
            {
                HitResolver.Instance.ResolveProjectileHit(
                    Source, target, Damage, DamageType, hitPosition);
            }
            else
            {
                GD.PushWarning($"Projectile3D: HitResolver.Instance is null, cannot apply damage for '{ProjectileId}'");
            }
        }

        EmitSignal(SignalName.ProjectileHit, target, this);
        TriggerImpactEffects(GlobalPosition);
        HandlePierce();
    }

    private void HandlePierce()
    {
        if (PierceCount == -1)
        {
            // Infinite pierce
            return;
        }
        else if (_hitsRemaining > 0)
        {
            _hitsRemaining--;
            return;
        }
        else
        {
            if (FadeOnHit)
                ExpireWithFade();
            else
                ExpireImmediate();
        }
    }

    // =========================================================================
    // IMPACT EFFECTS
    // =========================================================================

    private void TriggerImpactEffects(Vector3 impactPosition)
    {
        if (_impactTriggered)
        {
            GD.PushError($"Projectile3D: TriggerImpactEffects called twice for '{ProjectileId}'");
            return;
        }
        _impactTriggered = true;

        // Spawn hit VFX
        if (!string.IsNullOrEmpty(HitVfx))
        {
            var vfxManager = GetNodeOrNull("/root/VFXManager");
            vfxManager?.Call("play_effect", HitVfx, impactPosition);
        }

        // Apply AOE damage if radius is set
        if (AoeRadius > 0f)
        {
            ApplyAoeDamage(impactPosition, AoeRadius);
        }
    }

    private void ApplyAoeDamage(Vector3 center, float radius)
    {
        if (!IsInstanceValid(Source))
            return;

        var sceneTree = GetTree();
        if (sceneTree == null)
            return;

        // Get enemy groups based on team
        string enemyUnitsGroup = Team == 0 ? "enemy_units" : "player_units";
        string enemyBasesGroup = Team == 0 ? "enemy_bases" : "player_bases";

        var targets = new Godot.Collections.Array<Node>();
        targets.AddRange(sceneTree.GetNodesInGroup(enemyUnitsGroup));
        targets.AddRange(sceneTree.GetNodesInGroup(enemyBasesGroup));

        foreach (var targetNode in targets)
        {
            if (targetNode is not Node3D target3D)
                continue;

            if (!IsValidTarget(target3D))
                continue;

            float distance = target3D.GlobalPosition.DistanceTo(center);
            if (distance <= radius)
            {
                // Apply damage via HitResolver for consistent hit handling
                if (HitResolver.Instance != null)
                {
                    HitResolver.Instance.ResolveProjectileHit(
                        Source, target3D, Damage, DamageType, center);
                }
            }
        }
    }

    // =========================================================================
    // EXPIRATION
    // =========================================================================

    private void ExpireWithFade()
    {
        if (_isFading)
            return;

        _isFading = true;
        IsActive = false;

        if (_visualInstance == null)
        {
            Visible = false;
            ExpireImmediate();
            return;
        }

        _fadeTween = CreateTween();
        _fadeTween.SetParallel(true);

        bool hasTweeners = false;
        foreach (var child in _visualInstance.GetChildren())
        {
            if (child is MeshInstance3D meshChild)
            {
                var material = meshChild.GetSurfaceOverrideMaterial(0);
                if (material is StandardMaterial3D stdMat)
                {
                    _fadeTween.TweenProperty(stdMat, "albedo_color:a", 0f, FadeDuration);
                    hasTweeners = true;
                }
                else if (material is ShaderMaterial shaderMat)
                {
                    _fadeTween.TweenMethod(
                        Callable.From<float>(v => shaderMat.SetShaderParameter("alpha", v)),
                        1f, 0f, FadeDuration);
                    hasTweeners = true;
                }
            }
            else if (child is GpuParticles3D particles)
            {
                particles.Emitting = false;
            }
        }

        if (hasTweeners)
        {
            _fadeTween.Finished += () =>
            {
                Visible = false;
                ExpireImmediate();
            };
        }
        else
        {
            Visible = false;
            ExpireImmediate();
        }
    }

    private void ExpireImmediate()
    {
        IsActive = false;
        _isFading = false;
        EmitSignal(SignalName.ProjectileExpired, this);

        if (!IsPooled)
        {
            QueueFree();
        }
    }

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    /// <summary>
    /// Initialize projectile with runtime data.
    /// </summary>
    public void Initialize(Godot.Collections.Dictionary data)
    {
        Source = data.TryGetValue("source", out var srcVal) ? srcVal.As<Node3D>() : null;
        Target = data.TryGetValue("target", out var tgtVal) ? tgtVal.As<Node3D>() : null;
        Team = data.TryGetValue("team", out var teamVal) ? teamVal.AsInt32() : -1;
        Damage = data.TryGetValue("damage", out var dmgVal) ? dmgVal.AsSingle() : 10f;
        DamageType = data.TryGetValue("damage_type", out var dtVal) ? dtVal.AsString() : "physical";

        // Set position
        if (data.TryGetValue("start_position", out var startPosVal))
        {
            _startPosition = startPosVal.AsVector3();
            GlobalPosition = _startPosition;
        }
        else if (Source != null)
        {
            _startPosition = Source.GlobalPosition;
            GlobalPosition = _startPosition;
        }

        // Set target position
        if (data.TryGetValue("target_position", out var tgtPosVal))
        {
            _targetPosition = tgtPosVal.AsVector3();
        }
        else if (Target != null && IsInstanceValid(Target))
        {
            _targetPosition = GetTargetPosition(Target);
        }

        // Set direction (used for initial rotation)
        if (data.TryGetValue("direction", out var dirVal) && dirVal.VariantType == Variant.Type.Vector3)
        {
            _direction = dirVal.AsVector3().Normalized();
        }
        else if (_targetPosition != Vector3.Zero)
        {
            _direction = (_targetPosition - _startPosition).Normalized();
        }

        // Create path based on movement type
        CreatePath();

        // Reset state
        _progress = 0f;
        _timeSinceTargetUpdate = 0f;
        _timeAlive = 0f;
        _hitsRemaining = PierceCount;
        IsActive = true;
        CurrentSpeed = Speed;

        Visible = true;
        if (_visualInstance != null)
        {
            _visualInstance.Visible = true;
        }

        // Apply fade-in if configured
        if (FadeInDuration > 0f && _visualInstance != null)
        {
            ApplyFadeIn();
        }

        // Spawn trail VFX
        if (!string.IsNullOrEmpty(TrailVfx))
        {
            var vfxManager = GetNodeOrNull("/root/VFXManager");
            vfxManager?.Call("play_effect", TrailVfx, GlobalPosition);
        }
    }

    /// <summary>
    /// Load configuration from ProjectileData.
    /// </summary>
    public void LoadFromData(ProjectileData data)
    {
        ProjectileId = data.ProjectileId;
        MovementType = data.MovementType;
        Speed = data.Speed;
        Acceleration = data.Acceleration;
        MinSpeed = data.MinSpeed;
        CurrentSpeed = Speed;
        Lifetime = data.Lifetime;
        ArcHeight = data.ArcHeight;
        HomingStrength = data.HomingStrength;
        Tracking = data.Tracking;
        PierceCount = data.PierceCount;
        AoeRadius = data.AoeRadius;
        VisualScene = data.VisualScene;
        HitVfx = data.HitVfx;
        TrailVfx = data.TrailVfx;
        FadeInDuration = data.FadeInDuration;
        FadeOnHit = data.FadeOnHit;
        FadeDuration = data.FadeDuration;

        // Instantiate visual if not already done
        if (VisualScene != null && _visualInstance == null)
        {
            _visualInstance = VisualScene.Instantiate<Node3D>();
            AddChild(_visualInstance);
            DuplicateMaterials();
            ResetParticleEmitters();
        }
        else if (VisualScene != null && _visualInstance != null)
        {
            ResetParticleEmitters();
        }
        else
        {
            GD.PushWarning($"Projectile3D: No VisualScene for '{ProjectileId}'");
        }
    }

    /// <summary>
    /// Reset for pooling.
    /// </summary>
    public void Reset()
    {
        Source = null;
        Target = null;
        Team = -1;
        _direction = Vector3.Forward;
        _startPosition = Vector3.Zero;
        _targetPosition = Vector3.Zero;
        _path = null;
        _progress = 0f;
        _timeSinceTargetUpdate = 0f;
        _timeAlive = 0f;
        _hitsRemaining = 0;
        IsActive = false;
        _isFading = false;
        _impactTriggered = false;

        if (_fadeTween != null && _fadeTween.IsValid())
        {
            _fadeTween.Kill();
        }
        _fadeTween = null;

        // Reset visual
        if (_visualInstance != null)
        {
            _visualInstance.Visible = false;
            foreach (var child in _visualInstance.GetChildren())
            {
                if (child is MeshInstance3D meshChild)
                {
                    meshChild.Visible = true;
                    var material = meshChild.GetSurfaceOverrideMaterial(0);
                    if (material is StandardMaterial3D stdMat)
                    {
                        stdMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                        var color = stdMat.AlbedoColor;
                        color.A = 1f;
                        stdMat.AlbedoColor = color;
                    }
                    else if (material is ShaderMaterial shaderMat)
                    {
                        shaderMat.SetShaderParameter("alpha", 1f);
                    }
                }
            }
        }

        if (IsInsideTree())
        {
            GlobalPosition = Vector3.Zero;
            Rotation = Vector3.Zero;
        }
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Get the proper target position for a unit.
    /// Uses the unit's get_projectile_target_position() if available, otherwise estimates center mass.
    /// Matches RangedUnit3D.GetTargetPosition() pattern.
    /// </summary>
    private static Vector3 GetTargetPosition(Node3D target)
    {
        if (target.HasMethod("get_projectile_target_position"))
        {
            return target.Call("get_projectile_target_position").AsVector3();
        }
        // Fallback: center mass estimate
        return target.GlobalPosition + new Vector3(0, DefaultTargetHeightOffset, 0);
    }

    /// <summary>
    /// Calculate intercept point for a moving target.
    /// Uses simple linear prediction based on target velocity and time-to-target.
    /// Prediction is scaled down when close to prevent oscillation.
    /// </summary>
    private Vector3 CalculateInterceptPoint(Vector3 targetPos, Node3D target)
    {
        Vector3 targetVelocity = GetTargetVelocity(target);

        // Stationary target - no prediction needed
        if (targetVelocity.LengthSquared() < 0.01f)
            return targetPos;

        float distance = GlobalPosition.DistanceTo(targetPos);

        // Disable prediction when very close to prevent oscillation
        if (distance < PredictionDisableDistance)
            return targetPos;

        float timeToTarget = distance / Mathf.Max(CurrentSpeed, MinSpeed);

        return targetPos + (targetVelocity * timeToTarget);
    }

    /// <summary>
    /// Get target's current velocity for intercept prediction.
    /// Supports both C# CharacterBody3D and GDScript nodes with velocity property.
    /// </summary>
    private static Vector3 GetTargetVelocity(Node3D target)
    {
        // C# CharacterBody3D
        if (target is CharacterBody3D charBody)
            return charBody.Velocity;

        // GDScript velocity property
        var velocityVar = target.Get("velocity");
        if (velocityVar.VariantType != Variant.Type.Nil)
            return velocityVar.AsVector3();

        return Vector3.Zero;
    }

    private void DuplicateMaterials()
    {
        if (_visualInstance == null)
            return;

        foreach (var child in _visualInstance.GetChildren())
        {
            if (child is MeshInstance3D meshChild)
            {
                var material = meshChild.GetSurfaceOverrideMaterial(0);
                if (material != null)
                {
                    meshChild.SetSurfaceOverrideMaterial(0, (Material)material.Duplicate());
                }
            }
        }
    }

    private void ResetParticleEmitters()
    {
        if (_visualInstance == null)
            return;

        foreach (var child in _visualInstance.GetChildren())
        {
            if (child is GpuParticles3D particles)
            {
                particles.Visible = true;
                particles.Emitting = true;
                particles.Restart();
            }
        }
    }

    private void ApplyFadeIn()
    {
        if (_visualInstance == null)
            return;

        var fadeInTween = CreateTween();
        fadeInTween.SetParallel(true);

        foreach (var child in _visualInstance.GetChildren())
        {
            if (child is MeshInstance3D meshChild)
            {
                var material = meshChild.GetSurfaceOverrideMaterial(0);
                if (material is StandardMaterial3D stdMat)
                {
                    var color = stdMat.AlbedoColor;
                    color.A = 0f;
                    stdMat.AlbedoColor = color;
                    fadeInTween.TweenProperty(stdMat, "albedo_color:a", 1f, FadeInDuration);
                }
                else if (material is ShaderMaterial shaderMat)
                {
                    shaderMat.SetShaderParameter("alpha", 0f);
                    fadeInTween.TweenMethod(
                        Callable.From<float>(v => shaderMat.SetShaderParameter("alpha", v)),
                        0f, 1f, FadeInDuration);
                }
            }
        }
    }

    // =========================================================================
    // GDSCRIPT COMPATIBILITY
    // =========================================================================

    // snake_case aliases for GDScript interop
    public void initialize(Godot.Collections.Dictionary data) => Initialize(data);
    public void load_from_data(ProjectileData data) => LoadFromData(data);
    public void reset() => Reset();
}
