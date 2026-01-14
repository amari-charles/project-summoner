using Godot;
using System.Collections.Generic;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Combat;
using ProjectSummoner.Combat.Hitbox;
using ProjectSummoner.Constants;
using ProjectSummoner.Debug;
using ProjectSummoner.Services;
using ProjectSummoner.Systems;
using ProjectSummoner.Systems.Modifiers;
using ProjectSummoner.Targeting;
using ProjectSummoner.Units.Components;
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

    // Projectile target fallback: center mass at 50% of unit height
    private const float CenterMassHeightFraction = 0.5f;

    // Minimum horizontal movement threshold to update facing direction
    private const float MinFacingDirectionThreshold = 0.1f;

    // Shadow auto-sizing: multiply sprite height by this to get shadow diameter
    private const float ShadowAutoSizeMultiplier = 0.8f;

    // Shadow Y offset above ground to prevent z-fighting
    private const float ShadowGroundOffset = 0.01f;

    // Render priority bounds (Sprite3D.RenderPriority range)
    private const int MinRenderPriority = -128;
    private const int MaxRenderPriority = 127;

    // Scale factor for converting world position to render priority
    // With battlefield Z range ~-40 to +40 and priority range of 256, 3x gives good granularity
    private const float RenderPriorityScale = 3f;

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

    [Export]
    public float AggroRadius { get; set; } = 20f;

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

    /// <summary>
    /// HP bar Y offset override. Set to 0 to auto-calculate from sprite height.
    /// Use this for units where auto-calculation doesn't work well.
    /// </summary>
    [Export]
    public float HpBarOffsetY { get; set; } = 0f;

    /// <summary>
    /// Custom hurtbox size using a box shape. If non-zero, uses BoxShape3D instead of CapsuleShape3D.
    /// Format: (Width, Height, Depth). Leave at (0,0,0) to use default capsule based on CollisionRadius.
    /// </summary>
    [Export]
    public Vector3 HurtboxBoxSize { get; set; } = Vector3.Zero;

    /// <summary>
    /// Whether the hurtbox capsule should be horizontal (rotated 90 degrees).
    /// Used for wide, flat units like flying clouds.
    /// </summary>
    [Export]
    public bool HurtboxHorizontal { get; set; } = false;

    /// <summary>
    /// Override for hurtbox height (or width when horizontal).
    /// Set to 0 to auto-calculate from sprite height.
    /// </summary>
    [Export]
    public float HurtboxHeight { get; set; } = 0f;

    /// <summary>
    /// Override for hurtbox radius (or height when horizontal).
    /// Set to 0 to use CollisionRadius.
    /// </summary>
    [Export]
    public float HurtboxRadius { get; set; } = 0f;

    /// <summary>
    /// Offset for hurtbox position (X, Y, Z).
    /// Use to align hurtbox with off-center sprites.
    /// </summary>
    [Export]
    public Vector3 HurtboxOffset { get; set; } = Vector3.Zero;

    // =========================================================================
    // RUNTIME STATE (IDamageable implementation - delegates to UnitHealth)
    // =========================================================================

    private readonly UnitHealth _health = new();

    public float CurrentHp => _health.CurrentHp;
    public bool IsAlive => _health.IsAlive;
    public bool IsDying => _health.IsDying;

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

    // Spawn reveal animation component
    private SpawnRevealComponent? _spawnRevealComponent;

    // Movement component for steering, separation, and velocity calculation
    private readonly UnitMovement _movement = new();

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
        UpdateHurtboxOffset();
    }

    /// <summary>
    /// Update shadow position to match hurtbox offset (where the character visually appears).
    /// </summary>
    private void UpdateShadowOffset()
    {
        if (_shadowComponent == null)
            return;

        // Shadow should be under the hurtbox (where the character body is)
        float shadowX = _isFacingRight ? HurtboxOffset.X : -HurtboxOffset.X;
        _shadowComponent.Position = new Vector3(shadowX, ShadowGroundOffset, HurtboxOffset.Z);
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
    private HurtboxComponent? _hurtbox;

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

        // Initialize health component
        _health.Initialize(MaxHp);
        _health.OnHpChanged += (hp, max) => EmitSignal(SignalName.HpChanged, hp, max);
        _health.OnDeath += OnHealthDeath;

        // Find visual component (child node named "Visual")
        var visualNode = GetNodeOrNull<Node3D>("Visual");
        if (visualNode is IVisualComponent vc)
        {
            VisualComponent = vc;
        }

        // Find projectile target point (optional - fallback uses center mass)
        _projectileTargetPoint = GetNodeOrNull<Marker3D>("ProjectileTargetPoint");

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

        // Setup hurtbox for collision-based hit detection
        SetupHurtbox();

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
        UpdateHurtboxOffset();  // Update hurtbox now that facing is set

        // For flying units, position shadow at ground level (normally done in _PhysicsProcess
        // but that doesn't run during prep phase when unit is Inactive)
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            UpdateShadowForAltitude();
        }

        // Show shadow after initialization (deferred to run after visual components show)
        if (_shadowComponent != null)
        {
            CallDeferred(MethodName.ShowShadowDeferred);
        }

        // Initialize spawn reveal component
        _spawnRevealComponent = new SpawnRevealComponent(
            owner: this,
            getVisual: () => VisualComponent,
            getShadow: () => _shadowComponent,
            getTeam: () => (Team)Team
        );
        _spawnRevealComponent.OnRevealComplete += OnSpawnRevealComplete;

        // Register with external systems (GDScript autoloads)
        RegisterWithExternalSystems();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive || ActivationState == ActivationState.Inactive)
            return;

        // Performance instrumentation
        ulong startTime = Time.GetTicksUsec();
        PerformanceCounters.ActiveUnits++;

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

        // Update render priority based on world position
        // Higher priority = renders in front. Camera at Z=-42.85, so more negative Z = closer = higher priority
        int priority = (int)((-GlobalPosition.Z + GlobalPosition.Y) * RenderPriorityScale);
        VisualComponent?.SetRenderPriority(Mathf.Clamp(priority, MinRenderPriority, MaxRenderPriority));

        // Record physics process time
        PerformanceCounters.PhysicsProcessTimeUsec += Time.GetTicksUsec() - startTime;
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
        // Delegate HP management to health component (fires OnHpChanged -> signal, OnDeath -> Die)
        _health.TakeDamage(amount);

        // Visual feedback
        VisualComponent?.FlashWhite();
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
        return _health.Heal(amount);
    }

    /// <summary>
    /// Initialize unit with typed stat modifiers.
    /// </summary>
    public void InitializeWithModifiers(List<StatModifier> modifiers)
    {
        // Phase 1: Additive bonuses
        float hpAdd = 0f, damageAdd = 0f, speedAdd = 0f, moveSpeedAdd = 0f;

        // Phase 2: Multiplicative bonuses
        float hpMult = 1f, damageMult = 1f, speedMult = 1f, moveSpeedMult = 1f;

        foreach (var mod in modifiers)
        {
            // Process stat_adds
            if (mod.StatAdds.TryGetValue("max_hp", out var hp)) hpAdd += hp;
            if (mod.StatAdds.TryGetValue("attack_damage", out var dmg)) damageAdd += dmg;
            if (mod.StatAdds.TryGetValue("attack_speed", out var spd)) speedAdd += spd;
            if (mod.StatAdds.TryGetValue("move_speed", out var mvSpd)) moveSpeedAdd += mvSpd;

            // Process stat_mults
            if (mod.StatMults.TryGetValue("max_hp", out var hpM)) hpMult *= hpM;
            if (mod.StatMults.TryGetValue("attack_damage", out var dmgM)) damageMult *= dmgM;
            if (mod.StatMults.TryGetValue("attack_speed", out var spdM)) speedMult *= spdM;
            if (mod.StatMults.TryGetValue("move_speed", out var mvSpdM)) moveSpeedMult *= mvSpdM;

            // Process flags
            foreach (var kvp in mod.Flags)
            {
                _activeModifierFlags[kvp.Key] = kvp.Value;
            }
        }

        // Apply modifiers: (base + adds) * mults
        MaxHp = (_baseMaxHp + hpAdd) * hpMult;
        AttackDamage = (_baseAttackDamage + damageAdd) * damageMult;
        AttackSpeed = (_baseAttackSpeed + speedAdd) * speedMult;
        MoveSpeed = (_baseMoveSpeed + moveSpeedAdd) * moveSpeedMult;

        _health.SetMaxHp(MaxHp, healToMax: true);
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
        if (_spawnRevealComponent == null || _spawnRevealComponent.IsRevealing)
            return;

        ActivationState = ActivationState.Inactive;
        _spawnRevealComponent.StartReveal(duration);
    }

    /// <summary>
    /// Called when spawn reveal animation completes.
    /// </summary>
    private void OnSpawnRevealComplete()
    {
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
    /// Uses hurtbox center if offset is set, otherwise ProjectileTargetPoint or center mass.
    /// Method name uses snake_case for cross-language duck typing compatibility.
    /// </summary>
    public Vector3 get_projectile_target_position()
    {
        // If hurtbox has custom offset, use hurtbox center as target
        if (HurtboxOffset != Vector3.Zero)
        {
            float xOffset = _isFacingRight ? HurtboxOffset.X : -HurtboxOffset.X;
            float radius = HurtboxRadius > 0 ? HurtboxRadius : CollisionRadius;
            float yOffset = HurtboxHorizontal ? radius : (HurtboxHeight > 0 ? HurtboxHeight / 2 : 1.0f);
            return GlobalPosition + new Vector3(xOffset, yOffset + HurtboxOffset.Y, HurtboxOffset.Z);
        }

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

    /// <summary>
    /// Called when health component triggers death.
    /// </summary>
    private void OnHealthDeath()
    {
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
        var result = _movement.CalculateForwardMovement(this, delta);
        ApplyMovementResult(result);
    }

    protected void MoveTowardTarget(float delta)
    {
        if (CurrentTarget == null)
            return;

        var result = _movement.CalculateTowardTargetMovement(this, CurrentTarget, delta);
        ApplyMovementResult(result);
        _movement.UpdateBlockedState(this, delta);
    }

    /// <summary>
    /// Move perpendicular to target to circle around while maintaining distance.
    /// Used by ranged units when they can't get target in attack cone.
    /// </summary>
    protected void StrafeAroundTarget(float delta)
    {
        if (CurrentTarget == null)
            return;

        var result = _movement.CalculateStrafeMovement(this, CurrentTarget, _isFacingRight, delta);

        if (result.ExtendTargetLock)
        {
            _targetLockTimer = TargetLockDuration;
        }

        ApplyMovementResult(result);
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

    /// <summary>
    /// Apply a movement result: set velocity, call MoveAndSlide, correct overlaps, update facing.
    /// </summary>
    private void ApplyMovementResult(MovementResult result)
    {
        Velocity = result.Velocity;
        MoveAndSlide();
        _movement.CorrectOverlaps(this);

        // Enforce battlefield boundaries after all movement/physics
        EnforceBattlefieldBounds();

        // Update facing
        if (result.FacingExplicitlySet)
        {
            if (_isFacingRight != result.ShouldFaceRight)
            {
                _isFacingRight = result.ShouldFaceRight;
                VisualComponent?.SetFlipH(_isFacingRight);
                UpdateShadowOffset();
            }
        }
        else if (result.FacingDirection.LengthSquared() > 0)
        {
            UpdateFacing(result.FacingDirection);
        }
    }

    /// <summary>
    /// Clamp unit position to battlefield boundaries.
    /// For flying units, only clamps X/Z (Y altitude is already managed separately).
    /// </summary>
    private void EnforceBattlefieldBounds()
    {
        var pos = GlobalPosition;
        var clamped = BattlefieldBounds.ClampToBounds(pos);

        // Only update if actually out of bounds (avoid unnecessary position sets)
        if (pos.X != clamped.X || pos.Z != clamped.Z)
        {
            GlobalPosition = new Vector3(clamped.X, pos.Y, clamped.Z);
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
        // Track target acquisitions for performance profiling
        PerformanceCounters.TargetAcquisitions++;

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
        // Track summoner lookups for performance profiling
        PerformanceCounters.SummonerLookups++;

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

        // Register with HPBarService (C#)
        // Use custom offset if specified, otherwise use defaults (auto-calculates from sprite)
        if (HpBarOffsetY > 0)
        {
            var settings = HPBarSettings.Default with { OffsetY = HpBarOffsetY };
            HPBarService.Instance?.CreateBarForUnit(this, settings);
        }
        else
        {
            HPBarService.Instance?.CreateBarForUnit(this);
        }
    }

    private void UnregisterFromExternalSystems()
    {
        // Unregister from SpatialGrid
        SpatialGrid.Instance?.UnregisterUnit(this);

        // HP bar auto-cleans via TreeExiting signal, but we can explicitly remove
        // for immediate cleanup when the unit is properly dying (not just being freed)
        HPBarService.Instance?.RemoveBar(this);
    }

    private void UpdateSpatialGridPosition()
    {
        SpatialGrid.Instance?.UpdateUnitPosition(this);
    }

    private void UpdateShadowForAltitude()
    {
        // Shadow should be under the hurtbox (where the character body is)
        float shadowX = _isFacingRight ? HurtboxOffset.X : -HurtboxOffset.X;
        _shadowComponent?.UpdateForAltitude(Position.Y, shadowX, HurtboxOffset.Z);
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

    // =========================================================================
    // HURTBOX SETUP
    // =========================================================================

    /// <summary>
    /// Create and configure the hurtbox for collision-based hit detection.
    /// </summary>
    private void SetupHurtbox()
    {
        _hurtbox = new HurtboxComponent();
        AddChild(_hurtbox);  // Add first so Configure can create child nodes

        // Use box shape if HurtboxBoxSize is set, otherwise use capsule
        if (HurtboxBoxSize != Vector3.Zero)
        {
            _hurtbox.ConfigureBox(
                team: Team,
                category: HurtboxCategory.Unit,
                boxSize: HurtboxBoxSize
            );
        }
        else
        {
            // Use shared config (single source of truth)
            var (radius, height, _) = GetHurtboxConfig();
            _hurtbox.Configure(
                team: Team,
                category: HurtboxCategory.Unit,
                radius: radius,
                height: height,
                horizontal: HurtboxHorizontal
            );
        }

        // Apply offset (accounts for facing direction)
        UpdateHurtboxOffset();
    }

    /// <summary>
    /// Update hurtbox position based on facing direction.
    /// X offset flips when unit faces left.
    /// </summary>
    private void UpdateHurtboxOffset()
    {
        if (_hurtbox == null || HurtboxOffset == Vector3.Zero)
            return;

        var (_, _, offset) = GetHurtboxConfig();
        _hurtbox.Position = offset;
    }

    /// <summary>
    /// Get hurtbox configuration values. Single source of truth for both
    /// actual hurtbox setup and debug visualization.
    /// </summary>
    private (float radius, float height, Vector3 offset) GetHurtboxConfig()
    {
        float radius = HurtboxRadius > 0 ? HurtboxRadius : CollisionRadius;
        float height = HurtboxHeight > 0 ? HurtboxHeight : (VisualComponent?.GetSpriteHeight() ?? 2.0f);
        float xOffset = _isFacingRight ? HurtboxOffset.X : -HurtboxOffset.X;
        var offset = new Vector3(xOffset, HurtboxOffset.Y, HurtboxOffset.Z);
        return (radius, height, offset);
    }

    /// <summary>
    /// Get the unit's hurtbox component (for debug visualization).
    /// </summary>
    public HurtboxComponent? GetHurtbox() => _hurtbox;

    // =========================================================================
    // DEBUG VISUALIZATION
    // =========================================================================

    private static bool _debugHurtboxEnabled;
    private static bool _debugTargetPointEnabled;
    private static bool _debugAttackRangeEnabled;

    private MeshInstance3D? _debugHurtboxMarker;
    private MeshInstance3D? _debugTargetPointMarker;
    private MeshInstance3D? _debugAttackRangeMarker;

    /// <summary>
    /// Toggle debug hurtbox visualization for all units.
    /// </summary>
    public static void ToggleDebugHurtbox()
    {
        _debugHurtboxEnabled = !_debugHurtboxEnabled;
        GD.Print($"[Unit3D] Debug Hurtbox: {(_debugHurtboxEnabled ? "ON" : "OFF")}");
    }

    /// <summary>
    /// Toggle debug target point visualization for all units.
    /// </summary>
    public static void ToggleDebugTargetPoint()
    {
        _debugTargetPointEnabled = !_debugTargetPointEnabled;
        GD.Print($"[Unit3D] Debug Target Point: {(_debugTargetPointEnabled ? "ON" : "OFF")}");
    }

    /// <summary>
    /// Toggle debug attack range visualization for all units.
    /// </summary>
    public static void ToggleDebugAttackRange()
    {
        _debugAttackRangeEnabled = !_debugAttackRangeEnabled;
        GD.Print($"[Unit3D] Debug Attack Range: {(_debugAttackRangeEnabled ? "ON" : "OFF")}");
    }

    /// <summary>
    /// Check if debug hurtbox visualization is enabled.
    /// </summary>
    public static bool IsDebugHurtboxEnabled() => _debugHurtboxEnabled;

    /// <summary>
    /// Check if debug target point visualization is enabled.
    /// </summary>
    public static bool IsDebugTargetPointEnabled() => _debugTargetPointEnabled;

    /// <summary>
    /// Check if debug attack range visualization is enabled.
    /// </summary>
    public static bool IsDebugAttackRangeEnabled() => _debugAttackRangeEnabled;

    // Set methods for loading saved settings
    public static void SetDebugHurtboxEnabled(bool enabled) => _debugHurtboxEnabled = enabled;
    public static void SetDebugTargetPointEnabled(bool enabled) => _debugTargetPointEnabled = enabled;
    public static void SetDebugAttackRangeEnabled(bool enabled) => _debugAttackRangeEnabled = enabled;

    // Snake_case aliases for GDScript compatibility
    public static void toggle_debug_hurtbox() => ToggleDebugHurtbox();
    public static bool is_debug_hurtbox_enabled() => IsDebugHurtboxEnabled();
    public static void set_debug_hurtbox_enabled(bool enabled) => SetDebugHurtboxEnabled(enabled);
    public static void toggle_debug_target_point() => ToggleDebugTargetPoint();
    public static bool is_debug_target_point_enabled() => IsDebugTargetPointEnabled();
    public static void set_debug_target_point_enabled(bool enabled) => SetDebugTargetPointEnabled(enabled);
    public static void toggle_debug_attack_range() => ToggleDebugAttackRange();
    public static bool is_debug_attack_range_enabled() => IsDebugAttackRangeEnabled();
    public static void set_debug_attack_range_enabled(bool enabled) => SetDebugAttackRangeEnabled(enabled);

    public override void _Process(double delta)
    {
        // Early out if no debug mode is active and no cleanup needed
        bool anyDebugEnabled = _debugHurtboxEnabled || _debugTargetPointEnabled || _debugAttackRangeEnabled;
        bool anyMarkerExists = _debugHurtboxMarker != null || _debugTargetPointMarker != null || _debugAttackRangeMarker != null;

        if (!anyDebugEnabled && !anyMarkerExists)
            return;

        // Hurtbox debug visualization
        if (_debugHurtboxEnabled)
        {
            UpdateDebugHurtboxMarker();
        }
        else if (_debugHurtboxMarker != null)
        {
            _debugHurtboxMarker.QueueFree();
            _debugHurtboxMarker = null;
        }

        // Target point debug visualization
        if (_debugTargetPointEnabled)
        {
            UpdateDebugTargetPointMarker();
        }
        else if (_debugTargetPointMarker != null)
        {
            _debugTargetPointMarker.QueueFree();
            _debugTargetPointMarker = null;
        }

        // Attack range debug visualization
        if (_debugAttackRangeEnabled)
        {
            UpdateDebugAttackRangeMarker();
        }
        else if (_debugAttackRangeMarker != null)
        {
            _debugAttackRangeMarker.QueueFree();
            _debugAttackRangeMarker = null;
        }
    }

    private void UpdateDebugHurtboxMarker()
    {
        if (_hurtbox == null)
            return;

        if (_debugHurtboxMarker == null)
        {
            _debugHurtboxMarker = CreateDebugHurtboxMesh();
            AddChild(_debugHurtboxMarker);
        }

        // Use shared config (single source of truth)
        var (radius, height, offset) = GetHurtboxConfig();

        // Position at hurtbox center, accounting for offset
        if (HurtboxHorizontal)
        {
            _debugHurtboxMarker.Position = offset + new Vector3(0, radius, 0);
            _debugHurtboxMarker.Rotation = new Vector3(0, 0, Mathf.DegToRad(90));
        }
        else
        {
            _debugHurtboxMarker.Position = offset + new Vector3(0, height / 2, 0);
            _debugHurtboxMarker.Rotation = Vector3.Zero;
        }
    }

    private MeshInstance3D CreateDebugHurtboxMesh()
    {
        var mesh = new MeshInstance3D();

        // Use box or capsule based on hurtbox type
        if (HurtboxBoxSize != Vector3.Zero)
        {
            var boxMesh = new BoxMesh
            {
                Size = HurtboxBoxSize
            };
            mesh.Mesh = boxMesh;
        }
        else
        {
            var capsule = new CapsuleMesh
            {
                Radius = _hurtbox?.Radius ?? CollisionRadius,
                Height = _hurtbox?.Height ?? 2.0f
            };
            mesh.Mesh = capsule;
        }

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.2f, 1.0f, 0.2f, 0.3f),  // Green
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            NoDepthTest = true,
            RenderPriority = 100  // Render in front
        };
        mesh.MaterialOverride = material;

        return mesh;
    }

    private void UpdateDebugTargetPointMarker()
    {
        if (_debugTargetPointMarker == null)
        {
            _debugTargetPointMarker = CreateDebugTargetPointSphere();
            AddChild(_debugTargetPointMarker);
        }

        // Position at the projectile target point (global space converted to local)
        var targetPos = get_projectile_target_position();
        _debugTargetPointMarker.GlobalPosition = targetPos;
    }

    private MeshInstance3D CreateDebugTargetPointSphere()
    {
        var mesh = new MeshInstance3D();
        var sphere = new SphereMesh
        {
            Radius = 0.3f,
            Height = 0.6f
        };
        mesh.Mesh = sphere;

        // Green if explicit marker exists, orange if using fallback center-mass
        bool hasExplicitMarker = _projectileTargetPoint != null;
        var color = hasExplicitMarker
            ? new Color(0.2f, 1.0f, 0.2f, 0.7f)   // Green - explicit marker
            : new Color(1.0f, 0.6f, 0.2f, 0.7f);  // Orange - fallback center-mass

        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            NoDepthTest = true,
            RenderPriority = 100
        };
        mesh.MaterialOverride = material;

        return mesh;
    }

    private void UpdateDebugAttackRangeMarker()
    {
        float range = GetEffectiveAttackRange();

        // Get visualization data from constraint (single source of truth)
        var config = GetTargetingConfig();
        var vizData = config.AttackConstraint?.GetVisualizationData(this);

        if (_debugAttackRangeMarker == null)
        {
            _debugAttackRangeMarker = vizData?.IsCone == true && vizData.ConeHalfAngle.HasValue
                ? CreateDebugAttackCone(range, vizData.ConeHalfAngle.Value)
                : CreateDebugAttackRangeCircle(range);
            AddChild(_debugAttackRangeMarker);
        }

        // Position at hurtbox center height, accounting for offset
        var (hurtboxRadius, hurtboxHeight, hurtboxOffset) = GetHurtboxConfig();
        float yOffset = HurtboxHorizontal ? hurtboxRadius : (hurtboxHeight / 2);
        float markerY = GlobalPosition.Y + yOffset + hurtboxOffset.Y;
        _debugAttackRangeMarker.GlobalPosition = new Vector3(
            GlobalPosition.X + hurtboxOffset.X,
            markerY,
            GlobalPosition.Z + hurtboxOffset.Z
        );

        // Rotate cone based on facing direction
        if (vizData?.IsCone == true)
        {
            // Face right = 0°, face left = 180° (on Y axis)
            float yRotation = _isFacingRight ? 0f : Mathf.Pi;
            _debugAttackRangeMarker.Rotation = new Vector3(0, yRotation, 0);
        }
    }

    private MeshInstance3D CreateDebugAttackRangeCircle(float radius)
    {
        var mesh = new MeshInstance3D();
        var cylinder = new CylinderMesh
        {
            TopRadius = radius,
            BottomRadius = radius,
            Height = 0.05f  // Very thin disc
        };
        mesh.Mesh = cylinder;

        mesh.MaterialOverride = CreateAttackRangeMaterial();
        return mesh;
    }

    private MeshInstance3D CreateDebugAttackCone(float radius, float halfAngleDegrees)
    {
        var mesh = new MeshInstance3D();
        mesh.Mesh = CreateConeMesh(radius, halfAngleDegrees);
        mesh.MaterialOverride = CreateAttackRangeMaterial();
        return mesh;
    }

    private StandardMaterial3D CreateAttackRangeMaterial()
    {
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.8f, 0.2f, 0.3f),  // Yellow-orange
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            NoDepthTest = true,
            RenderPriority = 99  // Slightly below target point
        };
    }

    /// <summary>
    /// Create a wedge/cone mesh for attack range visualization.
    /// The cone points in the +X direction (right).
    /// </summary>
    private ArrayMesh CreateConeMesh(float radius, float halfAngleDegrees)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        const float height = 0.05f;
        const int segments = 16;  // Segments for the arc

        float halfAngleRad = Mathf.DegToRad(halfAngleDegrees);
        float startAngle = -halfAngleRad;
        float endAngle = halfAngleRad;
        float angleStep = (endAngle - startAngle) / segments;

        var center = new Vector3(0, height / 2, 0);

        // Create triangles from center to arc
        for (int i = 0; i < segments; i++)
        {
            float angle1 = startAngle + i * angleStep;
            float angle2 = startAngle + (i + 1) * angleStep;

            // Points on the arc (X is forward, Z is sideways)
            var p1 = new Vector3(Mathf.Cos(angle1) * radius, height / 2, Mathf.Sin(angle1) * radius);
            var p2 = new Vector3(Mathf.Cos(angle2) * radius, height / 2, Mathf.Sin(angle2) * radius);

            // Top face triangle
            surfaceTool.AddVertex(center);
            surfaceTool.AddVertex(p1);
            surfaceTool.AddVertex(p2);

            // Bottom face triangle (reversed winding)
            var centerBottom = new Vector3(0, -height / 2, 0);
            var p1Bottom = new Vector3(p1.X, -height / 2, p1.Z);
            var p2Bottom = new Vector3(p2.X, -height / 2, p2.Z);

            surfaceTool.AddVertex(centerBottom);
            surfaceTool.AddVertex(p2Bottom);
            surfaceTool.AddVertex(p1Bottom);
        }

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
    }
}
