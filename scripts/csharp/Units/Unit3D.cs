using Godot;
using System.Collections.Generic;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Combat;
using ProjectSummoner.Constants;
using ProjectSummoner.Movement;
using ProjectSummoner.Systems;
using ProjectSummoner.Targeting;
using ProjectSummoner.Visual;

namespace ProjectSummoner.Units;

/// <summary>
/// Abstract base class for all combat units.
/// Provides core functionality that every unit needs.
///
/// MUST IMPLEMENT (abstract):
/// - PerformAttackAction() - What happens when the unit attacks
/// - GetEffectiveAttackRange() - How this unit's range is calculated
///
/// CAN OVERRIDE (virtual):
/// - AcquireTarget() - Custom targeting logic
/// - IsInAttackRange() - Custom range checking
/// - CanEverReachTarget() - Custom reachability logic
/// - OnDeath() - Custom death behavior
/// - OnTakeDamage() - Custom damage handling
/// </summary>
[GlobalClass]
public abstract partial class Unit3D : CharacterBody3D, IDamageable
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    private const float MaxFlightAltitude = 10.0f;
    private const float MinFlightAltitude = 0.5f;
    private const float DeathCleanupDelay = 1.0f;
    private const float TargetLockDuration = 0.5f;

    // Spawn reveal glow colors - blue for player team, red for enemy team (matches team color scheme)
    private static readonly Color SpawnGlowColorPlayer = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Color SpawnGlowColorEnemy = new(1.0f, 0.4f, 0.4f, 1.0f);

    // Minimum horizontal displacement to determine facing direction.
    // Below this threshold, target is considered directly above/below and attack is allowed regardless of facing.
    private const float MinHorizontalDisplacement = 0.01f;

    // Half-plane angle threshold for facing direction during strafe.
    // If target angle is within ±90° of forward (+X), face right; otherwise face left.
    private const float StrafeFacingHalfAngle = 90f;

    // Projectile target fallback: center mass at 50% of unit height
    private const float CenterMassHeightFraction = 0.5f;

    // Minimum horizontal movement threshold to update facing direction
    private const float MinFacingDirectionThreshold = 0.1f;

    // Shadow auto-sizing: multiply sprite height by this to get shadow diameter
    private const float ShadowAutoSizeMultiplier = 0.8f;

    // Cached shader (shared across all instances)
    private static Shader? _spawnRevealShader;

    // =========================================================================
    // GODOT SIGNALS (accessible from GDScript)
    // =========================================================================

    [Signal]
    public delegate void HpChangedEventHandler(float newHp, float maxHp);

    [Signal]
    public delegate void UnitDiedEventHandler(Unit3D unit);

    [Signal]
    public delegate void UnitAttackedEventHandler(Node3D target);

    // =========================================================================
    // EXPORTED PROPERTIES - Combat Stats
    // =========================================================================

    [ExportGroup("Combat Stats")]
    [Export]
    public float MaxHp { get; set; } = 100f;

    [Export]
    public float AttackDamage { get; set; } = 10f;

    [Export]
    public float AttackSpeed { get; set; } = 1f;

    [Export]
    public float MoveSpeed { get; set; } = 3f;

    [Export]
    public float AttackRange { get; set; } = 2f;

    [Export]
    public float CollisionRadius { get; set; } = 0.5f;

    // =========================================================================
    // EXPORTED PROPERTIES - Targeting
    // =========================================================================

    [ExportGroup("Targeting")]
    [Export]
    public TargetingConfig? TargetingConfig { get; set; }

    // =========================================================================
    // EXPORTED PROPERTIES - Classification
    // =========================================================================

    [ExportGroup("Classification")]

    /// <summary>
    /// Unique identifier for this unit type (e.g., "puff", "slime").
    /// Used to look up targeting config from registry.
    /// </summary>
    [Export]
    public string UnitId { get; set; } = "";

    [Export]
    public int Team { get; set; } = (int)Units.Team.Player;

    [Export]
    public int UnitType { get; set; } = (int)Units.UnitType.Melee;

    [Export]
    public int MovementLayer { get; set; } = (int)Units.MovementLayer.Ground;

    // =========================================================================
    // EXPORTED PROPERTIES - Flying Configuration
    // =========================================================================

    [ExportGroup("Flying Configuration")]
    [Export]
    public float FlightAltitude { get; set; } = 2.5f;

    [Export]
    public FlyingAttackStyle FlyingAttackStyle { get; set; } = FlyingAttackStyle.Hover;

    [Export]
    public bool CanReturnToAir { get; set; } = true;

    [Export]
    public float ReturnToAirDelay { get; set; } = 1.0f;

    [Export]
    public FlyingDeathStyle FlyingDeathStyle { get; set; } = FlyingDeathStyle.Fall;

    // =========================================================================
    // EXPORTED PROPERTIES - Visual Configuration
    // =========================================================================

    [ExportGroup("Visual Configuration")]
    [Export]
    public SpriteFrames? SpriteFrames { get; set; }

    [Export]
    public float SpriteScale { get; set; } = 2.5f;

    [Export]
    public float ViewportScale { get; set; } = 1.0f;

    [Export]
    public bool EnableBobbing { get; set; } = false;

    [Export]
    public bool EnableBreathing { get; set; } = false;

    [Export]
    public bool ShadowEnabled { get; set; } = true;

    /// <summary>
    /// Shadow size in world units. Set to 0 to auto-calculate from visual dimensions.
    /// </summary>
    [Export]
    public float ShadowSize { get; set; } = 0f;

    [Export]
    public float ShadowOpacity { get; set; } = 0.6f;

    // =========================================================================
    // RUNTIME STATE (IDamageable implementation)
    // =========================================================================

    public float CurrentHp { get; protected set; }
    public bool IsAlive { get; protected set; } = true;
    public bool IsDying { get; protected set; } = false;

    // GDScript interop - snake_case aliases for duck typing in battlefield_constants.gd
    public bool is_alive => IsAlive;
    public float collision_radius => CollisionRadius;

    // =========================================================================
    // RUNTIME STATE - Targeting & Combat
    // =========================================================================

    public Node3D? CurrentTarget { get; protected set; }
    public Node3D? ForcedTarget { get; set; }
    public float ForcedTargetTimer { get; set; }
    public Vector3 RallyPoint { get; set; }
    public bool IsInRallyMode { get; set; }
    public ActivationState ActivationState { get; protected set; } = ActivationState.Inactive;

    protected float _attackCooldown;
    protected float _targetLockTimer;
    protected float _attackAnimationTimer;  // Prevents animation override during attack
    protected Dictionary<string, bool> _activeModifierFlags = new();

    // Spawn reveal state
    private bool _isSpawning;
    private ShaderMaterial? _spawnRevealMaterial;
    private Tween? _spawnRevealTween;
    private readonly Dictionary<CanvasItem, Material?> _originalMaterials = new();

    // Steering behavior for separation and flanking
    private readonly UnitSteering _steering = new();

    /// <summary>
    /// True if unit is facing right (positive X). Player team starts right, enemy left.
    /// </summary>
    protected bool _isFacingRight;

    /// <summary>
    /// Public accessor for facing direction.
    /// </summary>
    public bool IsFacingRight => _isFacingRight;

    /// <summary>
    /// Set the unit's facing direction and update visuals.
    /// </summary>
    public void SetFacing(bool facingRight)
    {
        _isFacingRight = facingRight;
        VisualComponent?.SetFlipH(_isFacingRight);
        UpdateShadowOffset();
    }

    /// <summary>
    /// Update shadow position to match sprite offset (accounts for flip).
    /// </summary>
    private void UpdateShadowOffset()
    {
        if (_shadowComponent == null || VisualComponent == null)
            return;

        var offset = VisualComponent.GetShadowOffset();
        _shadowComponent.Position = new Vector3(offset.X, 0.01f, 0);
    }

    /// <summary>
    /// Show shadow after deferred initialization completes.
    /// </summary>
    private void ShowShadowDeferred()
    {
        if (_shadowComponent != null)
        {
            _shadowComponent.Visible = true;
        }
    }

    // Base stats for modifier calculations
    protected float _baseMaxHp;
    protected float _baseAttackDamage;
    protected float _baseAttackSpeed;
    protected float _baseMoveSpeed;

    // =========================================================================
    // COMPONENT REFERENCES
    // =========================================================================

    protected IVisualComponent? VisualComponent { get; set; }
    private Marker3D? _projectileTargetPoint;
    private ShadowComponent? _shadowComponent;

    // =========================================================================
    // TARGETING HELPER
    // =========================================================================

    /// <summary>
    /// Get the targeting config for this unit.
    /// Priority: 1) Registry by UnitId, 2) Exported TargetingConfig, 3) Default config.
    /// </summary>
    protected TargetingConfig GetTargetingConfig()
    {
        // First try registry lookup by UnitId (bypasses .tres loading issues)
        if (!string.IsNullOrEmpty(UnitId))
        {
            return TargetingConfigRegistry.GetConfig(UnitId);
        }

        // Fall back to exported config or default
        return TargetingConfig ?? DefaultTargetingConfig.Get();
    }

    // =========================================================================
    // ABSTRACT METHODS - Subclasses MUST implement
    // =========================================================================

    /// <summary>
    /// Execute the attack action. Called when cooldown is ready and target is in range.
    /// Melee units deal damage directly, ranged units spawn projectiles.
    /// </summary>
    protected abstract void PerformAttackAction();

    /// <summary>
    /// Get the effective attack range for this unit type.
    /// </summary>
    protected abstract float GetEffectiveAttackRange();

    // =========================================================================
    // LIFECYCLE METHODS
    // =========================================================================

    public override void _Ready()
    {
        // Store base stats for modifier calculations
        _baseMaxHp = MaxHp;
        _baseAttackDamage = AttackDamage;
        _baseAttackSpeed = AttackSpeed;
        _baseMoveSpeed = MoveSpeed;

        CurrentHp = MaxHp;

        // Find visual component (child node named "Visual")
        var visualNode = GetNodeOrNull<Node3D>("Visual");
        if (visualNode is IVisualComponent vc)
        {
            VisualComponent = vc;
        }

        // Find projectile target point (required for proper projectile aiming)
        _projectileTargetPoint = GetNodeOrNull<Marker3D>("ProjectileTargetPoint");
        if (_projectileTargetPoint == null)
        {
            GD.PushError($"Unit {Name} missing required ProjectileTargetPoint Marker3D node");
        }

        // Create shadow if enabled
        if (ShadowEnabled)
        {
            // Calculate shadow size: use explicit value if set, otherwise auto-calculate from sprite width
            float effectiveShadowSize = ShadowSize;
            if (ShadowSize <= 0 && VisualComponent != null)
            {
                float spriteWidth = VisualComponent.GetSpriteWidth();
                effectiveShadowSize = spriteWidth * ShadowAutoSizeMultiplier;
            }

            if (effectiveShadowSize > 0)
            {
                _shadowComponent = new ShadowComponent();
                AddChild(_shadowComponent);
                _shadowComponent.Initialize(effectiveShadowSize, ShadowOpacity);

                // Hide shadow during initialization to prevent jitter
                // (offset depends on facing which is set later in _Ready)
                _shadowComponent.Visible = false;
            }
        }

        // Setup groups for targeting
        SetupGroups();

        // Handle flying units
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            FlightAltitude = Mathf.Clamp(FlightAltitude, MinFlightAltitude, MaxFlightAltitude);
            Position = new Vector3(Position.X, FlightAltitude, Position.Z);
        }

        // Set initial facing based on team (sprites are drawn facing left, flip for player)
        _isFacingRight = Team == (int)Units.Team.Player;
        VisualComponent?.SetFlipH(_isFacingRight);
        UpdateShadowOffset();

        // Show shadow after initialization (deferred to run after visual components show)
        if (_shadowComponent != null)
        {
            CallDeferred(MethodName.ShowShadowDeferred);
        }

        // Register with external systems (GDScript autoloads)
        RegisterWithExternalSystems();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive || ActivationState == ActivationState.Inactive)
            return;

        float deltaF = (float)delta;

        UpdateCooldowns(deltaF);
        UpdateTargeting(deltaF);
        UpdateBehavior(deltaF);

        // Update shadow for flying units (dynamic altitude scaling)
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            UpdateShadowForAltitude();
        }

        // Update position in spatial grid
        UpdateSpatialGridPosition();
    }

    // =========================================================================
    // VIRTUAL METHODS - Subclasses CAN override
    // =========================================================================

    /// <summary>
    /// Acquire a target using the scoring system.
    /// Override for units with special targeting (prefer wounded, prefer specific types, etc.)
    /// </summary>
    protected virtual Node3D? AcquireTarget()
    {
        return DefaultTargetAcquisition();
    }

    /// <summary>
    /// Check if target is within attack range using 3D distance.
    /// </summary>
    protected virtual bool IsInAttackRange(Node3D target)
    {
        float distance = GlobalPosition.DistanceTo(target.GlobalPosition);
        return distance <= GetEffectiveAttackRange();
    }

    /// <summary>
    /// Check if this unit can ever reach the target (reachability check).
    /// Prevents targeting enemies that are impossible to attack.
    /// Note: This is now mostly handled by constraint's CanEverReach in TargetingConfig.
    /// </summary>
    protected virtual bool CanEverReachTarget(Node3D target)
    {
        // Delegate to the targeting config's constraint
        return GetTargetingConfig().AttackConstraint?.CanEverReach(this, target) ?? true;
    }

    /// <summary>
    /// Called when unit dies. Override for death effects, drops, etc.
    /// </summary>
    protected virtual void OnDeath()
    {
        EmitSignal(SignalName.UnitDied, this);

        // Handle flying death style
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            HandleFlyingDeath();
        }

        // Cleanup after death animation
        var tween = CreateTween();
        tween.TweenInterval(DeathCleanupDelay);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    /// <summary>
    /// Called when unit takes damage. Override for damage reduction, shields, etc.
    /// </summary>
    protected virtual void OnTakeDamage(float amount, string damageType)
    {
        CurrentHp = Mathf.Max(CurrentHp - amount, 0);
        EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);

        // Visual feedback
        VisualComponent?.FlashWhite();

        if (CurrentHp <= 0)
        {
            Die();
        }
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Apply damage to this unit. Called by DamageSystem.
    /// Overload for GDScript compatibility (Godot bug #59025: default params don't work cross-language).
    /// </summary>
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, "physical");
    }

    /// <summary>
    /// Apply damage to this unit with damage type.
    /// </summary>
    public void TakeDamage(float amount, string damageType)
    {
        if (!IsAlive || IsDying)
            return;

        OnTakeDamage(amount, damageType);
    }

    /// <summary>
    /// Heal this unit by the specified amount.
    /// Returns the actual amount healed (may be less if at max HP).
    /// </summary>
    public float Heal(float amount)
    {
        if (!IsAlive || IsDying)
            return 0f;

        float previousHp = CurrentHp;
        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        float actualHeal = CurrentHp - previousHp;

        if (actualHeal > 0)
        {
            EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
        }

        return actualHeal;
    }

    /// <summary>
    /// Initialize unit with stat modifiers from cards/abilities.
    /// </summary>
    public void InitializeWithModifiers(Godot.Collections.Array modifiers, Godot.Collections.Dictionary? cardData = null)
    {
        // Phase 1: Additive bonuses
        float hpAdd = 0f, damageAdd = 0f, speedAdd = 0f, moveSpeedAdd = 0f;

        // Phase 2: Multiplicative bonuses
        float hpMult = 1f, damageMult = 1f, speedMult = 1f, moveSpeedMult = 1f;

        foreach (var mod in modifiers)
        {
            if (mod.Obj is not Godot.Collections.Dictionary modDict)
                continue;

            // Process stat_adds
            if (modDict.TryGetValue("stat_adds", out var statAddsVar) &&
                statAddsVar.Obj is Godot.Collections.Dictionary statAdds)
            {
                if (statAdds.TryGetValue("max_hp", out var hp)) hpAdd += hp.AsSingle();
                if (statAdds.TryGetValue("attack_damage", out var dmg)) damageAdd += dmg.AsSingle();
                if (statAdds.TryGetValue("attack_speed", out var spd)) speedAdd += spd.AsSingle();
                if (statAdds.TryGetValue("move_speed", out var mvSpd)) moveSpeedAdd += mvSpd.AsSingle();
            }

            // Process stat_mults
            if (modDict.TryGetValue("stat_mults", out var statMultsVar) &&
                statMultsVar.Obj is Godot.Collections.Dictionary statMults)
            {
                if (statMults.TryGetValue("max_hp", out var hp)) hpMult *= hp.AsSingle();
                if (statMults.TryGetValue("attack_damage", out var dmg)) damageMult *= dmg.AsSingle();
                if (statMults.TryGetValue("attack_speed", out var spd)) speedMult *= spd.AsSingle();
                if (statMults.TryGetValue("move_speed", out var mvSpd)) moveSpeedMult *= mvSpd.AsSingle();
            }

            // Process flags
            if (modDict.TryGetValue("flags", out var flagsVar) &&
                flagsVar.Obj is Godot.Collections.Dictionary flags)
            {
                foreach (var key in flags.Keys)
                {
                    _activeModifierFlags[key.AsString()] = flags[key].AsBool();
                }
            }
        }

        // Apply modifiers: (base + adds) * mults
        MaxHp = (_baseMaxHp + hpAdd) * hpMult;
        AttackDamage = (_baseAttackDamage + damageAdd) * damageMult;
        AttackSpeed = (_baseAttackSpeed + speedAdd) * speedMult;
        MoveSpeed = (_baseMoveSpeed + moveSpeedAdd) * moveSpeedMult;

        CurrentHp = MaxHp;
    }

    /// <summary>
    /// Activate the unit (after spawn reveal completes).
    /// </summary>
    public void Activate()
    {
        if (ActivationState == ActivationState.Inactive)
        {
            ActivationState = ActivationState.Active;
        }
    }

    /// <summary>
    /// Deactivate the unit (during spawn reveal).
    /// </summary>
    public void Deactivate()
    {
        ActivationState = ActivationState.Inactive;
    }

    /// <summary>
    /// Check if this unit is currently active.
    /// </summary>
    public bool IsActive()
    {
        return ActivationState == ActivationState.Active;
    }

    // =========================================================================
    // SPAWN REVEAL ANIMATION
    // =========================================================================

    /// <summary>
    /// Start the spawn reveal animation (ghost materialize effect).
    /// Unit will be inactive until the animation completes.
    /// </summary>
    public void start_spawn_reveal(float duration)
    {
        if (_isSpawning)
            return;

        _isSpawning = true;
        ActivationState = ActivationState.Inactive;

        // Start shadow at scale 0 (will grow during reveal)
        if (_shadowComponent != null)
        {
            _shadowComponent.Scale = Vector3.Zero;
        }

        // Load shader if not cached
        _spawnRevealShader ??= GD.Load<Shader>("res://shaders/vfx/spawn_reveal.gdshader");

        if (_spawnRevealShader == null)
        {
            GD.PushError("Unit3D: Failed to load spawn_reveal shader!");
            CompleteSpawnReveal();
            return;
        }

        // Create shader material
        _spawnRevealMaterial = new ShaderMaterial();
        _spawnRevealMaterial.Shader = _spawnRevealShader;
        _spawnRevealMaterial.SetShaderParameter("progress", 0.0f);

        // Set glow color based on team
        var glowColor = Team == (int)Units.Team.Player ? SpawnGlowColorPlayer : SpawnGlowColorEnemy;
        _spawnRevealMaterial.SetShaderParameter("glow_color", glowColor);

        // Apply shader and start animation (deferred to allow visual component initialization)
        CallDeferred(MethodName.ApplySpawnRevealDeferred, duration);
    }

    private void ApplySpawnRevealDeferred(float duration)
    {
        if (!IsInstanceValid(this) || !_isSpawning)
            return;

        ApplySpawnShaderToVisual();

        // Animate progress from 0 to 1
        _spawnRevealTween = CreateTween();
        _spawnRevealTween.TweenMethod(Callable.From<float>(UpdateSpawnProgress), 0.0f, 1.0f, duration);

        // Animate shadow growing alongside
        if (_shadowComponent != null)
        {
            _spawnRevealTween.Parallel().TweenProperty(_shadowComponent, "scale", Vector3.One, duration);
        }

        // Complete when done
        _spawnRevealTween.TweenCallback(Callable.From(CompleteSpawnReveal));
    }

    private void ApplySpawnShaderToVisual()
    {
        if (VisualComponent == null || _spawnRevealMaterial == null)
            return;

        _originalMaterials.Clear();

        // Find all CanvasItems in the visual component and apply shader
        if (VisualComponent is Node visualNode)
        {
            var sprites = FindAllCanvasItems(visualNode);
            foreach (var sprite in sprites)
            {
                _originalMaterials[sprite] = sprite.Material;
                sprite.Material = _spawnRevealMaterial;
            }
        }
    }

    private List<CanvasItem> FindAllCanvasItems(Node root)
    {
        var result = new List<CanvasItem>();
        FindCanvasItemsRecursive(root, result);
        return result;
    }

    private void FindCanvasItemsRecursive(Node node, List<CanvasItem> result)
    {
        if (node is CanvasItem canvasItem)
        {
            result.Add(canvasItem);
        }

        foreach (var child in node.GetChildren())
        {
            FindCanvasItemsRecursive(child, result);
        }
    }

    private void UpdateSpawnProgress(float progress)
    {
        _spawnRevealMaterial?.SetShaderParameter("progress", progress);
    }

    private void CompleteSpawnReveal()
    {
        _isSpawning = false;

        // Restore original materials
        foreach (var (sprite, originalMaterial) in _originalMaterials)
        {
            if (IsInstanceValid(sprite))
            {
                sprite.Material = originalMaterial;
            }
        }
        _originalMaterials.Clear();

        // Clean up shader material
        _spawnRevealMaterial?.Dispose();
        _spawnRevealMaterial = null;

        // Activate if game is in battle phase (unit was spawned during battle)
        // BattlePhase enum: PREPARATION = 0, BATTLE = 1
        var gameController = GetTree().CurrentScene;
        if (gameController != null)
        {
            var currentPhase = gameController.Get("current_phase");
            if (currentPhase.VariantType != Variant.Type.Nil && currentPhase.AsInt32() == 1)
            {
                Activate();
            }
        }
    }

    /// <summary>
    /// Force this unit to target a specific enemy.
    /// </summary>
    public void ApplyRedirect(Node3D target, float duration)
    {
        ForcedTarget = target;
        ForcedTargetTimer = duration;
    }

    /// <summary>
    /// Clear forced targeting.
    /// </summary>
    public void ClearRedirect()
    {
        ForcedTarget = null;
        ForcedTargetTimer = 0;
    }

    /// <summary>
    /// Get the position where projectiles should aim at this unit.
    /// Returns ProjectileTargetPoint position if available, otherwise center mass.
    /// Method name uses snake_case for cross-language duck typing compatibility.
    /// </summary>
    public Vector3 get_projectile_target_position()
    {
        if (_projectileTargetPoint != null)
        {
            return _projectileTargetPoint.GlobalPosition;
        }

        // Fallback: center mass based on visual height
        float height = VisualComponent?.GetSpriteHeight() ?? 1.0f;
        return GlobalPosition + new Vector3(0, height * CenterMassHeightFraction, 0);
    }

    // =========================================================================
    // PROTECTED HELPERS
    // =========================================================================

    protected void Die()
    {
        if (IsDying)
            return;

        IsDying = true;
        IsAlive = false;

        // Unregister from external systems
        UnregisterFromExternalSystems();

        OnDeath();
    }

    protected void DealDamageTo(Node3D target)
    {
        // Use C# DamageSystem directly
        DamageSystem.Instance?.ApplyDamage(this, target, AttackDamage, "physical");
    }

    protected void UpdateCooldowns(float delta)
    {
        if (_attackCooldown > 0)
        {
            _attackCooldown -= delta;
        }

        if (_attackAnimationTimer > 0)
        {
            _attackAnimationTimer -= delta;
        }

        if (ForcedTargetTimer > 0)
        {
            ForcedTargetTimer -= delta;
            if (ForcedTargetTimer <= 0)
            {
                ForcedTarget = null;
            }
        }

        if (_targetLockTimer > 0)
        {
            _targetLockTimer -= delta;
        }
    }

    protected void UpdateTargeting(float delta)
    {
        // Use forced target if available
        if (ForcedTarget != null && IsInstanceValid(ForcedTarget))
        {
            CurrentTarget = ForcedTarget;
            return;
        }

        // Re-acquire target if needed
        if (_targetLockTimer <= 0 || CurrentTarget == null || !IsValidTarget(CurrentTarget))
        {
            CurrentTarget = AcquireTarget();
            if (CurrentTarget != null)
            {
                _targetLockTimer = TargetLockDuration;
            }
        }
    }

    protected void UpdateBehavior(float delta)
    {
        if (CurrentTarget == null)
        {
            // No target - move forward
            MoveForward(delta);
            // Only update animation if not in attack animation
            if (_attackAnimationTimer <= 0)
            {
                UpdateAnimation("walk");
            }
            return;
        }

        if (IsInAttackRange(CurrentTarget))
        {
            // Check attack constraints (cone, etc.) - try to resolve if needed
            var config = GetTargetingConfig();
            if (!config.CanAttack(this, CurrentTarget))
            {
                bool resolved = config.TryResolveConstraint(this, CurrentTarget);

                if (!resolved)
                {
                    // Constraint not resolved - use configured fallback movement
                    switch (config.FallbackMovement)
                    {
                        case Targeting.FallbackMovementStyle.Strafe:
                            StrafeAroundTarget(delta);
                            break;
                        case Targeting.FallbackMovementStyle.Idle:
                            break;
                        case Targeting.FallbackMovementStyle.MoveToward:
                        default:
                            MoveTowardTarget(delta);
                            break;
                    }

                    if (_attackAnimationTimer <= 0)
                    {
                        UpdateAnimation(config.FallbackMovement == Targeting.FallbackMovementStyle.Idle ? "idle" : "walk");
                    }
                }
                else
                {
                    if (_attackAnimationTimer <= 0)
                    {
                        UpdateAnimation("idle");
                    }
                }
                return;  // Wait until next frame to attack
            }

            // In range and constraints satisfied - attack if cooldown ready
            // Units with 0 attack speed cannot attack (e.g., target dummies)
            if (_attackCooldown <= 0 && AttackSpeed > 0)
            {
                PerformAttackAction();
                _attackCooldown = 1.0f / AttackSpeed;
                // Set attack animation timer to prevent override
                float attackDuration = VisualComponent?.GetAnimationDuration("attack") ?? 0.5f;
                _attackAnimationTimer = attackDuration;
                EmitSignal(SignalName.UnitAttacked, CurrentTarget);
            }
            // Only update to idle if not in attack animation
            else if (_attackAnimationTimer <= 0)
            {
                UpdateAnimation("idle");
            }
        }
        else
        {
            // Move toward target
            MoveTowardTarget(delta);
            // Only update animation if not in attack animation
            if (_attackAnimationTimer <= 0)
            {
                UpdateAnimation("walk");
            }
        }
    }

    protected void MoveForward(float delta)
    {
        // Move toward enemy base direction
        float direction = Team == (int)Units.Team.Player ? 1.0f : -1.0f;
        Vector3 moveDir = new(direction, 0, 0);

        // Apply separation steering to avoid stacking with nearby units
        Vector3 separation = _steering.CalculateSeparationForce(this, null);
        Vector3 finalDir = (moveDir + separation).Normalized();
        Vector3 velocity = finalDir * MoveSpeed;

        // Maintain altitude for flying units
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            velocity.Y = 0;
        }

        Velocity = velocity;
        MoveAndSlide();

        // Correct any severe overlaps after movement
        _steering.CorrectOverlaps(this);

        // Update facing direction (use base direction, not separation-adjusted)
        UpdateFacing(moveDir);
    }

    protected void MoveTowardTarget(float delta)
    {
        if (CurrentTarget == null)
            return;

        Vector3 targetPos = CurrentTarget.GlobalPosition;

        // For ground units, ignore Y difference when moving
        if (MovementLayer == (int)Units.MovementLayer.Ground)
        {
            targetPos.Y = GlobalPosition.Y;
        }

        Vector3 direction = (targetPos - GlobalPosition).Normalized();

        // Apply separation steering to avoid stacking with nearby units
        Vector3 separation = _steering.CalculateSeparationForce(this, CurrentTarget);
        Vector3 flank = _steering.CalculateFlankForce(this, CurrentTarget, delta);
        Vector3 finalDir = (direction * MoveSpeed + separation + flank).Normalized();
        Vector3 velocity = finalDir * MoveSpeed;

        // Maintain altitude for flying units
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            velocity.Y = 0;
        }

        Velocity = velocity;
        MoveAndSlide();

        // Correct any severe overlaps after movement
        _steering.CorrectOverlaps(this);

        // Track blocked state for flanking behavior
        _steering.UpdateBlockedState(this, delta);

        // Update facing direction (use base direction, not separation-adjusted)
        UpdateFacing(direction);
    }

    /// <summary>
    /// Move perpendicular to target to circle around while maintaining distance.
    /// Used by ranged units when they can't get target in attack cone.
    /// </summary>
    protected void StrafeAroundTarget(float delta)
    {
        if (CurrentTarget == null)
            return;

        // Extend target lock while strafing - prevents mid-strafe target switches
        _targetLockTimer = TargetLockDuration;

        Vector3 toTarget = CurrentTarget.GlobalPosition - GlobalPosition;
        Vector3 horizontalToTarget = new(toTarget.X, 0, toTarget.Z);

        if (horizontalToTarget.LengthSquared() < MinHorizontalDisplacement * MinHorizontalDisplacement)
            return; // Target directly on top of us

        // Calculate angle to target (0° = +X, 90° = +Z)
        float angleToTarget = Mathf.RadToDeg(Mathf.Atan2(horizontalToTarget.Z, horizontalToTarget.X));

        // Determine optimal facing: face toward the half-plane where target is
        // If target angle is within ±StrafeFacingHalfAngle of forward, face right (0°)
        // If target angle is outside that range, face left (180°)
        bool shouldFaceRight = Mathf.Abs(angleToTarget) <= StrafeFacingHalfAngle;

        // Update facing (bypassing the threshold check since we calculated it properly)
        if (_isFacingRight != shouldFaceRight)
        {
            _isFacingRight = shouldFaceRight;
            VisualComponent?.SetFlipH(_isFacingRight);
            UpdateShadowOffset();
        }

        // Calculate angle difference from our facing
        float facingAngle = _isFacingRight ? 0f : 180f;
        float angleDiff = angleToTarget - facingAngle;
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        // Get perpendicular strafe direction (90° counterclockwise from toTarget)
        Vector3 strafeDir = new Vector3(-horizontalToTarget.Z, 0, horizontalToTarget.X).Normalized();

        // Choose strafe direction that reduces |angleDiff|
        // If angleDiff > 0, target is counterclockwise from facing, keep base direction
        // If angleDiff < 0, target is clockwise from facing, flip direction
        if (angleDiff < 0)
            strafeDir = -strafeDir;

        // Apply separation steering to avoid stacking with nearby units
        Vector3 separation = _steering.CalculateSeparationForce(this, CurrentTarget);
        Vector3 finalDir = (strafeDir + separation).Normalized();
        Vector3 velocity = finalDir * MoveSpeed;

        // Maintain altitude for flying units
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            velocity.Y = 0;
        }

        Velocity = velocity;
        MoveAndSlide();

        // Correct any severe overlaps after movement
        _steering.CorrectOverlaps(this);
    }

    protected void UpdateFacing(Vector3 direction)
    {
        // Sprites are drawn facing left, flip when moving right
        if (Mathf.Abs(direction.X) > MinFacingDirectionThreshold)
        {
            bool shouldFaceRight = direction.X > 0;
            if (_isFacingRight != shouldFaceRight)
            {
                _isFacingRight = shouldFaceRight;
                VisualComponent?.SetFlipH(_isFacingRight);
                UpdateShadowOffset();
            }
        }
    }

    protected void UpdateAnimation(string animName)
    {
        if (VisualComponent != null && VisualComponent.GetCurrentAnimation() != animName)
        {
            VisualComponent.PlayAnimation(animName, true);
        }
    }

    protected Node3D? DefaultTargetAcquisition()
    {
        // Get enemies from spatial grid
        if (SpatialGrid.Instance == null)
            return null;

        var config = GetTargetingConfig();
        var enemies = SpatialGrid.Instance.GetUnitsInRadius(
            GlobalPosition, config.AggroRadius, GetEnemyTeam());

        var bestTarget = config.AcquireTarget(this, enemies);

        // If no units found, target the enemy summoner
        return bestTarget ?? GetEnemySummoner();
    }

    /// <summary>
    /// Get the enemy summoner as a fallback target when no units are in range.
    /// </summary>
    protected Node3D? GetEnemySummoner()
    {
        // Determine which summoner group to target based on our team
        string summonerGroup = GroupIDs.EnemySummonersFor(Team);

        var summoners = GetTree().GetNodesInGroup(summonerGroup);

        foreach (var summoner in summoners)
        {
            if (summoner is Node3D summoner3D)
            {
                // Check if summoner is alive (GDScript uses snake_case)
                var isAliveVar = summoner3D.Get("is_alive");
                if (isAliveVar.VariantType != Variant.Type.Nil && isAliveVar.AsBool())
                {
                    return summoner3D;
                }
            }
        }

        return null;
    }

    protected bool IsValidTarget(Node3D target)
    {
        if (target == null || !IsInstanceValid(target))
            return false;

        // Check if target is alive
        // C# units implement IDamageable
        if (target is IDamageable damageable && !damageable.IsAlive)
            return false;

        // GDScript nodes (like Summoner) use is_alive property
        var isAliveVar = target.Get("is_alive");
        if (isAliveVar.VariantType != Variant.Type.Nil && !isAliveVar.AsBool())
            return false;

        return true;
    }

    protected int GetTargetMovementLayer(Node3D target)
    {
        if (target is Unit3D unit)
            return unit.MovementLayer;

        // Fallback for non-unit targets
        return (int)Units.MovementLayer.Ground;
    }

    protected float GetTargetAltitude(Node3D target)
    {
        if (target is Unit3D unit && unit.MovementLayer == (int)Units.MovementLayer.Air)
            return unit.FlightAltitude;

        return target.GlobalPosition.Y;
    }

    protected int GetEnemyTeam()
    {
        return Team == (int)Units.Team.Player ? (int)Units.Team.Enemy : (int)Units.Team.Player;
    }

    // =========================================================================
    // EXTERNAL SYSTEM INTEGRATION
    // =========================================================================

    private void SetupGroups()
    {
        AddToGroup(GroupIDs.Units);
        AddToGroup(GroupIDs.AllyUnitsFor(Team));

        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            AddToGroup(GroupIDs.FlyingUnits);
        }
    }

    private void RegisterWithExternalSystems()
    {
        // Register with SpatialGrid
        SpatialGrid.Instance?.RegisterUnit(this);

        // Register with HPBarManager (still GDScript)
        var hpBarManager = GetNodeOrNull("/root/HPBarManager");
        hpBarManager?.Call("create_bar_for_unit", this);
    }

    private void UnregisterFromExternalSystems()
    {
        // Unregister from SpatialGrid
        SpatialGrid.Instance?.UnregisterUnit(this);

        // Remove HP bar (still GDScript)
        var hpBarManager = GetNodeOrNull("/root/HPBarManager");
        hpBarManager?.Call("remove_bar_from_unit", this);
    }

    private void UpdateSpatialGridPosition()
    {
        SpatialGrid.Instance?.UpdateUnitPosition(this);
    }

    private void UpdateShadowForAltitude()
    {
        _shadowComponent?.UpdateForAltitude(Position.Y);
    }

    private void HandleFlyingDeath()
    {
        switch (FlyingDeathStyle)
        {
            case FlyingDeathStyle.Fall:
                // Body falls - will be handled by physics or tween
                break;
            case FlyingDeathStyle.Fade:
                // Fade out at altitude - visual component handles this
                break;
            case FlyingDeathStyle.Explode:
                // Spawn explosion VFX
                var vfxManager = GetNodeOrNull("/root/VFXManager");
                vfxManager?.Call("play_effect", "death_explosion", GlobalPosition);
                break;
        }
    }
}
