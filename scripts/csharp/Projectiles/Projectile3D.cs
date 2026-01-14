using Godot;
using ProjectSummoner.Combat.Hitbox;

namespace ProjectSummoner.Projectiles;

/// <summary>
/// Data-driven 3D projectile system.
/// Supports multiple movement types: straight, homing, arc, ballistic.
/// Uses collision-based hit detection via HitboxComponent/HurtboxComponent.
/// </summary>
[GlobalClass]
public partial class Projectile3D : Area3D
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>Minimum distance to prevent division by zero in arc calculations.</summary>
    private const float MinArcDistance = 0.1f;

    /// <summary>Distance at which full arc height is used.</summary>
    private const float FullArcDistance = 5.0f;

    /// <summary>Dot product threshold for disabling homing.</summary>
    private const float HomingDisableDotThreshold = 0.2f;

    /// <summary>Grace period before ground collision activates (seconds).</summary>
    private const float GroundCollisionGracePeriod = 0.1f;

    /// <summary>Ground Y level.</summary>
    private const float GroundY = 0f;

    /// <summary>Collision layer for hitboxes (Layer 6 = bit 5).</summary>
    private const uint HitboxLayer = 1u << 5;

    /// <summary>Collision mask for hurtboxes (Layer 5 = bit 4).</summary>
    private const uint HurtboxMask = 1u << 4;

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

    private Vector3 _direction = Vector3.Forward;
    private Vector3 _startPosition = Vector3.Zero;
    private Vector3 _targetPosition = Vector3.Zero;
    private float _travelTime = 0f;
    private float _timeAlive = 0f;
    private int _hitsRemaining = 0;
    private bool _homingDisabled = false;
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
        _travelTime += dt;

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

        // Update movement based on type
        switch (MovementType)
        {
            case ProjectileMovementType.Straight:
                MoveStraight(dt);
                break;
            case ProjectileMovementType.Homing:
                MoveHoming(dt);
                break;
            case ProjectileMovementType.Arc:
                MoveArc(dt);
                break;
            case ProjectileMovementType.Ballistic:
                MoveBallistic(dt);
                break;
        }

        // Rotate to face movement direction
        if (_direction.LengthSquared() > 0.001f)
        {
            LookAt(GlobalPosition + _direction, Vector3.Up);
        }
    }

    // =========================================================================
    // MOVEMENT STRATEGIES
    // =========================================================================

    private void MoveStraight(float delta)
    {
        GlobalPosition += _direction * CurrentSpeed * delta;
    }

    private void MoveHoming(float delta)
    {
        if (IsInstanceValid(Target) && !_homingDisabled)
        {
            _targetPosition = Target.GlobalPosition;
            var toTarget = _targetPosition - GlobalPosition;
            float distanceToTarget = toTarget.Length();

            if (distanceToTarget < MinArcDistance)
            {
                _homingDisabled = true;
            }
            else
            {
                var targetDir = toTarget / distanceToTarget;
                float dot = _direction.Dot(targetDir);

                if (dot < HomingDisableDotThreshold)
                {
                    _homingDisabled = true;
                }
                else
                {
                    _direction = _direction.Lerp(targetDir, HomingStrength * delta).Normalized();
                }
            }
        }

        GlobalPosition += _direction * CurrentSpeed * delta;

        // Apply arc offset if configured
        if (ArcHeight > 0f)
        {
            float totalDistance = Mathf.Max(_startPosition.DistanceTo(_targetPosition), MinArcDistance);
            float traveled = _startPosition.DistanceTo(GlobalPosition);
            float progress = Mathf.Clamp(traveled / totalDistance, 0f, 1f);
            float arcOffset = ArcHeight * Mathf.Sin(progress * Mathf.Pi);
            GlobalPosition = new Vector3(
                GlobalPosition.X,
                _startPosition.Y + arcOffset + (_targetPosition.Y - _startPosition.Y) * progress,
                GlobalPosition.Z
            );
        }
    }

    private void MoveArc(float delta)
    {
        float distance = Mathf.Max(_startPosition.DistanceTo(_targetPosition), MinArcDistance);
        float arcScale = Mathf.Clamp(distance / FullArcDistance, 0f, 1f);
        float effectiveArcHeight = ArcHeight * arcScale;

        float progress = _travelTime * Speed / distance;
        progress = Mathf.Clamp(progress, 0f, 1f);

        var horizontalPos = _startPosition.Lerp(_targetPosition, progress);
        float arcOffset = effectiveArcHeight * Mathf.Sin(progress * Mathf.Pi);
        horizontalPos.Y += arcOffset;

        _direction = (horizontalPos - GlobalPosition).Normalized();
        GlobalPosition = horizontalPos;

        if (progress >= 1f)
        {
            TriggerImpactEffects(GlobalPosition);
            if (FadeOnHit)
                ExpireWithFade();
            else
                ExpireImmediate();
        }
    }

    private void MoveBallistic(float delta)
    {
        var displacement = _targetPosition - _startPosition;
        float horizontalDist = new Vector2(displacement.X, displacement.Z).Length();
        float verticalDist = displacement.Y;

        float gravityForce = 9.8f;
        float timeToTarget = horizontalDist / Speed;
        float initialVelocityY = (verticalDist + 0.5f * gravityForce * timeToTarget * timeToTarget) / timeToTarget;

        var horizontalDir = new Vector3(displacement.X, 0, displacement.Z).Normalized();
        var velocity = horizontalDir * Speed;
        velocity.Y = initialVelocityY - gravityForce * _travelTime;

        _direction = velocity.Normalized();
        GlobalPosition += velocity * delta;

        if (GlobalPosition.DistanceTo(_targetPosition) < 0.5f)
        {
            TriggerImpactEffects(GlobalPosition);
            if (FadeOnHit)
                ExpireWithFade();
            else
                ExpireImmediate();
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
        // Apply damage via DamageSystem
        if (IsInstanceValid(target) && IsInstanceValid(Source))
        {
            var damageSystem = GetNodeOrNull("/root/DamageSystem");
            if (damageSystem != null)
            {
                damageSystem.Call("apply_damage", Source, target, Damage, DamageType);
            }
        }

        EmitSignal(SignalName.ProjectileHit, target, this);
        TriggerImpactEffects(GlobalPosition);
        HandlePierce();
    }

    private void HitTargetViaHurtbox(Node3D target, Area3D hurtbox)
    {
        // Use DamageSystem with projectile flag
        if (IsInstanceValid(target) && IsInstanceValid(Source))
        {
            var damageSystem = GetNodeOrNull("/root/DamageSystem");
            if (damageSystem != null)
            {
                var flags = new Godot.Collections.Dictionary { { "from_projectile", true } };
                damageSystem.Call("apply_damage", Source, target, Damage, DamageType, flags);
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

        var damageSystem = GetNodeOrNull("/root/DamageSystem");

        foreach (var targetNode in targets)
        {
            if (targetNode is not Node3D target3D)
                continue;

            if (!IsValidTarget(target3D))
                continue;

            float distance = target3D.GlobalPosition.DistanceTo(center);
            if (distance <= radius && damageSystem != null)
            {
                damageSystem.Call("apply_damage", Source, target3D, Damage, DamageType);
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
            _targetPosition = Target.GlobalPosition;
        }

        // Set direction
        if (data.TryGetValue("direction", out var dirVal) && dirVal.VariantType == Variant.Type.Vector3)
        {
            _direction = dirVal.AsVector3().Normalized();
        }
        else if (_targetPosition != Vector3.Zero)
        {
            _direction = (_targetPosition - _startPosition).Normalized();
        }

        // Reset state
        _travelTime = 0f;
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
        PierceCount = data.PierceCount;
        AoeRadius = data.AoeRadius;
        VisualScene = data.VisualScene;
        HitVfx = data.HitVfx;
        TrailVfx = data.TrailVfx;
        FadeInDuration = data.FadeInDuration;

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
        _travelTime = 0f;
        _timeAlive = 0f;
        _hitsRemaining = 0;
        IsActive = false;
        _isFading = false;
        _impactTriggered = false;
        _homingDisabled = false;

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
