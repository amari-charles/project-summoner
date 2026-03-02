using Godot;
using System.Collections.Generic;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Cards;
using ProjectSummoner.Combat;
using ProjectSummoner.Combat.Hitbox;
using ProjectSummoner.Constants;
using ProjectSummoner.Debug;
using ProjectSummoner.Services;
using ProjectSummoner.Stats;
using ProjectSummoner.Systems;
using ProjectSummoner.Systems.Modifiers;
using ProjectSummoner.Targeting;
using ProjectSummoner.Units.Components;
using ProjectSummoner.UI;
using ProjectSummoner.Visual;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Simulation;

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

    // Minimum position change (squared) to trigger render priority update
    // Prevents recalculating every frame when unit is stationary or barely moving
    private const float RenderPriorityPositionThresholdSq = 0.25f; // 0.5 units squared

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
    public float SeparationRadius { get; set; } = 0.5f;

    [Export]
    public float AggroRadius { get; set; } = 20f;

    [Export]
    public float CritChance { get; set; } = 0f;

    [Export]
    public float CritDamage { get; set; } = 1.5f;

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

    /// <summary>
    /// Network ID for multiplayer synchronization.
    /// Assigned by the host when the unit is spawned in multiplayer.
    /// -1 means not synchronized (single-player or not yet assigned).
    /// </summary>
    public int NetworkId { get; set; } = -1;

    /// <summary>
    /// Simulation unit ID linking this visual node to its UnitData in MatchState.
    /// null means not linked to the simulation (legacy mode).
    /// When set, this unit reads position/facing from MatchState during Battle
    /// and damage/death are driven by sim events (not local combat).
    /// </summary>
    public int? SimUnitId { get; set; } = null;

    /// <summary>
    /// True if this unit is linked to a simulation slot.
    /// In sim-driven mode, Unit3D is a pure visual puppet of MatchState.
    /// </summary>
    public bool IsSimDriven => SimUnitId.HasValue;

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

    /// <summary>
    /// Get the Y position where this unit should spawn.
    /// Flying units spawn at FlightAltitude; ground units at Y=0.
    /// Single source of truth for spawn altitude calculation.
    /// </summary>
    public float GetSpawnAltitude()
    {
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            return FlightAltitude;
        }
        return 0f;
    }

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
    /// Format: (Width, Height, Depth). Leave at (0,0,0) to use default capsule based on HurtboxRadius.
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
    /// Set to 0 to use default (0.5).
    /// </summary>
    [Export]
    public float HurtboxRadius { get; set; } = 0f;

    /// <summary>
    /// Offset for hurtbox position (X, Y, Z).
    /// Use to align hurtbox with off-center sprites.
    /// </summary>
    [Export]
    public Vector3 HurtboxOffset { get; set; } = Vector3.Zero;

    /// <summary>
    /// Offset from calculated center mass for projectile targeting.
    /// X flips with facing direction. Use for units with off-center bodies.
    /// </summary>
    [Export]
    public Vector3 TargetPointOffset { get; set; } = Vector3.Zero;

    // =========================================================================
    // RUNTIME STATE (IDamageable implementation - delegates to UnitHealth)
    // =========================================================================

    private readonly UnitHealth _health = new();

    public float CurrentHp => _health.CurrentHp;
    public bool IsAlive => _health.IsAlive;
    public bool IsDying => _health.IsDying;

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

    // Trigger modifier system
    private List<StatModifier> _triggeredModifiers = new();
    private List<ActiveTrigger> _activeTriggers = new();

    /// <summary>
    /// Represents an active trigger effect with duration/cooldown tracking.
    /// </summary>
    private class ActiveTrigger
    {
        public required StatModifier Modifier;
        public float RemainingDuration;
        public float CooldownRemaining;
        public bool IsActive;
    }

    // Spawn reveal animation component
    private SpawnRevealComponent? _spawnRevealComponent;

    // Movement component for steering, separation, and velocity calculation
    private readonly UnitMovement _movement = new();

    // Last position used for render priority calculation (throttling)
    private Vector3 _lastRenderPriorityPosition = Vector3.Zero;

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

    /// <summary>
    /// Apply element tint to placeholder visuals after visual component initializes.
    /// Uses the unit's card definition to determine element color.
    /// Fire units are not tinted since the fire wisp art is the original.
    /// </summary>
    private void ApplyElementTintDeferred()
    {
        if (VisualComponent == null || string.IsNullOrEmpty(UnitId))
            return;

        // Look up card definition to get element and visual traits
        var card = CardCatalog.GetCard(UnitId);
        if (card == null)
            return;

        // Only tint units with UsesWispVisuals trait
        // Other units like puff, earth_sprite have their own distinct art
        if ((card.VisualTraits & VisualTrait.UsesWispVisuals) == 0)
            return;

        var element = card.ElementalAffinity;

        // Skip fire element - the fire wisp art is the original, no tint needed
        if (element == Element.Fire || element == Element.Neutral)
            return;

        // Get element color and apply as tint
        var tintColor = ElementColors.GetColor(element);

        // Apply tint via the visual component's ghost tint method
        // This sets Modulate which persists and FlashWhite will return to it
        VisualComponent.ApplyGhostTint(tintColor);
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
    private ShadowComponent? _shadowComponent;
    private HurtboxComponent? _hurtbox;

    // =========================================================================
    // TARGETING HELPER
    // =========================================================================

    /// <summary>
    /// Cached targeting config from UnitDefinition (if applied).
    /// </summary>
    private TargetingConfig? _definitionTargetingConfig;

    /// <summary>
    /// Get the targeting config for this unit.
    /// Priority: 1) Definition config, 2) UnitDefinitions lookup, 3) Exported TargetingConfig, 4) Default melee.
    /// </summary>
    protected TargetingConfig GetTargetingConfig()
    {
        // First check if we have a config from ApplyDefinition
        if (_definitionTargetingConfig != null)
        {
            return _definitionTargetingConfig;
        }

        // Fallback: try UnitDefinitions lookup by UnitId (for backwards compatibility)
        if (!string.IsNullOrEmpty(UnitId) && UnitDefinitions.TryGet(new Constants.UnitId(UnitId), out var def) && def != null)
        {
            return def.Targeting.BuildConfig();
        }

        // Fall back to exported config or default melee targeting
        return TargetingConfig ?? MeleeTargeting.Default.BuildConfig();
    }

    // =========================================================================
    // DEFINITION APPLICATION
    // =========================================================================

    /// <summary>
    /// Apply a UnitDefinition's configuration to this unit.
    /// Called by UnitSpawner after instantiation.
    /// </summary>
    public void ApplyDefinition(UnitDefinition definition)
    {
        // Identity
        UnitId = definition.Id.Value;

        // Stats
        MaxHp = definition.Stats.MaxHp;
        AttackDamage = definition.Stats.AttackDamage;
        AttackSpeed = definition.Stats.AttackSpeed;
        MoveSpeed = definition.Stats.MoveSpeed;
        AttackRange = definition.Stats.AttackRange;
        AggroRadius = definition.Stats.AggroRadius;
        CritChance = definition.Stats.CritChance;
        CritDamage = definition.Stats.CritDamage;

        // Store base stats for modifier calculations
        _baseMaxHp = MaxHp;
        _baseAttackDamage = AttackDamage;
        _baseAttackSpeed = AttackSpeed;
        _baseMoveSpeed = MoveSpeed;

        // Classification
        UnitType = (int)definition.UnitType;
        MovementLayer = (int)definition.MovementLayer;

        // Build and cache targeting config from behavior
        _definitionTargetingConfig = definition.Targeting.BuildConfig();

        // Visual config
        var visual = definition.Visual;
        SeparationRadius = visual.SeparationRadius;
        ShadowOpacity = visual.ShadowOpacity;
        ShadowEnabled = visual.ShadowEnabled;
        HpBarOffsetY = visual.HpBarOffsetY;
        TargetPointOffset = visual.TargetPointOffset;

        // Hurtbox config
        if (visual.Hurtbox != null)
        {
            HurtboxHorizontal = visual.Hurtbox.Horizontal;
            HurtboxHeight = visual.Hurtbox.Height;
            HurtboxRadius = visual.Hurtbox.Radius;
            HurtboxOffset = visual.Hurtbox.Offset;
        }

        // Flying config
        if (definition.Flying != null)
        {
            FlightAltitude = definition.Flying.Altitude;
            FlyingAttackStyle = definition.Flying.AttackStyle;
            CanReturnToAir = definition.Flying.CanReturnToAir;
            ReturnToAirDelay = definition.Flying.ReturnToAirDelay;
            FlyingDeathStyle = definition.Flying.DeathStyle;
        }
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

        // Apply element tint for placeholder visuals (deferred to wait for visual initialization)
        CallDeferred(MethodName.ApplyElementTintDeferred);

        // Claim sim unit ID from MatchState (links this visual node to its simulation data)
        ClaimSimUnitId();

        // Connect to sim event signals for visual feedback
        ConnectSimSignals();
    }

    /// <summary>
    /// Claim a sim unit ID from MatchState, linking this visual unit to its simulation data.
    /// Called during _Ready(). Uses team matching to find the next unclaimed UnitData.
    /// </summary>
    private void ClaimSimUnitId()
    {
        var simNode = SimulationNode.Current;
        if (simNode == null) return;

        SimUnitId = simNode.ClaimNextSimUnitId(Team);
        if (IsSimDriven)
        {
            GD.Print($"[Unit3D] Claimed SimUnitId={SimUnitId} for {UnitId} team={Team}");
            simNode.RegisterUnit3D(SimUnitId.Value, this);
        }
    }

    /// <summary>
    /// Connect to SimulationNode signals for sim-driven visual feedback.
    /// </summary>
    private void ConnectSimSignals()
    {
        var simNode = SimulationNode.Current;
        if (simNode == null || !IsSimDriven) return;

        simNode.UnitAttacked += OnSimUnitAttacked;
        simNode.UnitDamaged += OnSimUnitDamaged;
        simNode.UnitDiedSim += OnSimUnitDied;
        simNode.UnitStateRemoved += OnSimUnitRemoved;
    }

    /// <summary>
    /// Disconnect sim event signals. Called on tree exit.
    /// </summary>
    private void DisconnectSimSignals()
    {
        var simNode = SimulationNode.Current;
        if (simNode == null || !IsSimDriven) return;

        simNode.UnitAttacked -= OnSimUnitAttacked;
        simNode.UnitDamaged -= OnSimUnitDamaged;
        simNode.UnitDiedSim -= OnSimUnitDied;
        simNode.UnitStateRemoved -= OnSimUnitRemoved;
    }

    // =========================================================================
    // SIM EVENT HANDLERS (visual feedback from simulation)
    // =========================================================================

    /// <summary>
    /// Sim: this unit attacked a target. Play attack animation.
    /// </summary>
    private void OnSimUnitAttacked(int attackerUnitId, int targetUnitId)
    {
        if (attackerUnitId != SimUnitId) return;

        // Play attack animation
        UpdateAnimation("attack");
        float attackDuration = VisualComponent?.GetAnimationDuration("attack") ?? 0.5f;
        _attackAnimationTimer = attackDuration;

        // Emit Godot signal for any listeners (e.g., sound effects)
        var targetNode = FindUnit3DBySimId(targetUnitId);
        if (targetNode != null)
            EmitSignal(SignalName.UnitAttacked, targetNode);
    }

    /// <summary>
    /// Sim: this unit took damage. Flash white and update HP bar.
    /// </summary>
    private void OnSimUnitDamaged(int targetUnitId, int attackerUnitId, float damage, bool isCrit)
    {
        if (targetUnitId != SimUnitId) return;

        // Visual feedback
        VisualComponent?.FlashWhite();
    }

    /// <summary>
    /// Sim: this unit died. Play death animation.
    /// </summary>
    private void OnSimUnitDied(int unitId, int killerUnitId)
    {
        if (unitId != SimUnitId) return;

        // Trigger the existing death path (unregisters from systems, plays death anim)
        _health.Die();
    }

    /// <summary>
    /// Sim: this unit was removed from MatchState (death cleanup timer expired).
    /// Destroy the visual node.
    /// </summary>
    private void OnSimUnitRemoved(int unitId)
    {
        if (unitId != SimUnitId) return;

        // Disconnect before freeing to avoid dangling handlers
        DisconnectSimSignals();
        QueueFree();
    }

    /// <summary>
    /// Find a Unit3D in the scene tree by its SimUnitId.
    /// Uses SimulationNode's O(1) cache, falls back to group scan.
    /// </summary>
    private Node3D? FindUnit3DBySimId(int simUnitId)
    {
        var cached = SimulationNode.Current?.FindUnit3DBySimId(simUnitId);
        if (cached is Node3D node3D) return node3D;
        return null;
    }

    public override void _ExitTree()
    {
        DisconnectSimSignals();
        if (SimUnitId.HasValue)
            SimulationNode.Current?.UnregisterUnit3D(SimUnitId.Value);
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;

        if (IsSimDriven)
        {
            // Sim-driven path: sync state from UnitData BEFORE activation gate.
            // This ensures ActivationState propagates from sim → visual even when
            // the unit starts Inactive (prevents the client-side freeze bug).
            SyncFromSimulation(deltaF);
        }
        else
        {
            // Legacy path: activation gate applies normally
            if (!IsAlive || ActivationState == ActivationState.Inactive)
                return;

            ulong startTime = Time.GetTicksUsec();
            PerformanceCounters.ActiveUnits++;

            UpdateCooldowns(deltaF);
            UpdateTargeting(deltaF);
            UpdateBehavior(deltaF);
            UpdateTriggers(deltaF);

            PerformanceCounters.PhysicsProcessTimeUsec += Time.GetTicksUsec() - startTime;
        }

        // Common visual updates (both paths, only when alive + active)
        if (!IsAlive || ActivationState == ActivationState.Inactive)
            return;

        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            UpdateShadowForAltitude();
        }

        UpdateSpatialGridPosition();

        float positionDeltaSq = GlobalPosition.DistanceSquaredTo(_lastRenderPriorityPosition);
        if (positionDeltaSq >= RenderPriorityPositionThresholdSq)
        {
            int priority = (int)((-GlobalPosition.Z + GlobalPosition.Y) * RenderPriorityScale);
            VisualComponent?.SetRenderPriority(Mathf.Clamp(priority, MinRenderPriority, MaxRenderPriority));
            _lastRenderPriorityPosition = GlobalPosition;
        }
    }

    /// <summary>
    /// Sync ALL visual state from UnitData when in sim-driven mode.
    /// Called BEFORE the activation gate so ActivationState propagates from sim → visual.
    /// Position, facing, HP, and animation come from UnitData.
    /// Damage and death handled by sim event signals.
    /// </summary>
    private bool _loggedSimSyncWarning;

    private void SyncFromSimulation(float delta)
    {
        var simNode = SimulationNode.Current;
        if (simNode == null)
        {
            if (!_loggedSimSyncWarning)
            {
                GD.PrintErr($"[Unit3D] SyncFromSimulation: SimulationNode.Current is null (SimUnitId={SimUnitId}, UnitId={UnitId})");
                _loggedSimSyncWarning = true;
            }
            return;
        }

        var unitData = simNode.GetUnitData(SimUnitId!.Value);
        if (unitData == null)
        {
            if (!_loggedSimSyncWarning)
            {
                GD.PrintErr($"[Unit3D] SyncFromSimulation: UnitData not found (SimUnitId={SimUnitId}, UnitId={UnitId})");
                _loggedSimSyncWarning = true;
            }
            return;
        }

        // Sync ActivationState from sim → visual (the critical fix)
        var simActivation = (ActivationState)unitData.ActivationState;
        if (ActivationState != simActivation)
        {
            ActivationState = simActivation;
        }
        if (simActivation == ActivationState.Inactive) return;

        // Sync alive state — snapshot says dead but unit is still alive visually
        if (!unitData.IsAlive && IsAlive)
        {
            _health.Die();  // Fallback: trigger death from snapshot (idempotent with UnitDied message path)
            return;
        }

        // Position: read from sim and convert to local Godot coordinates
        GlobalPosition = simNode.SimToLocal(unitData.Position);

        // Facing — flip for client since X axis is mirrored via CoordinateTransform
        bool localFacing = unitData.IsFacingRight;
        if (SimulationNode.Current != null && !SimulationNode.Current.IsHost)
            localFacing = !localFacing;
        if (_isFacingRight != localFacing)
        {
            SetFacing(localFacing);
        }

        // HP sync (updates HP bar without triggering death)
        _health.SyncFromSim(unitData.CurrentHp, unitData.MaxHp);

        // Animation from behavior state (attack anim has priority via timer)
        if (_attackAnimationTimer > 0)
        {
            _attackAnimationTimer -= delta;
        }
        else
        {
            string anim = unitData.BehaviorState switch
            {
                BehaviorState.Attacking => "idle",  // attack anim triggered by event, show idle between
                BehaviorState.InRange => "idle",    // waiting for cooldown
                _ => "walk"                         // NoTarget or Chasing
            };
            UpdateAnimation(anim);
        }
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

        // Unregister from multiplayer system if in a match
        UnregisterFromMultiplayerSystem();

        // Clear trigger state to prevent further processing
        _triggeredModifiers.Clear();
        _activeTriggers.Clear();

        // Handle flying death style
        if (MovementLayer == (int)Units.MovementLayer.Air)
        {
            HandleFlyingDeath();
        }

        // In sim-driven mode, QueueFree is handled by UnitRemovedEvent → OnSimUnitRemoved().
        // Host: Tick() fires UnitRemovedEvent after DeathCleanupTimer expires.
        // Client: Tick() doesn't run, so emit UnitStateRemoved after a delay to route
        // through the same OnSimUnitRemoved() cleanup path (single cleanup entry point).
        if (IsSimDriven)
        {
            if (SimulationNode.Current != null && !SimulationNode.Current.IsHost)
            {
                int capturedId = SimUnitId!.Value;
                var timer = GetTree()?.CreateTimer(DeathCleanupDelay);
                timer?.Connect("timeout", Callable.From(() =>
                {
                    SimulationNode.Current?.EmitSignal(SimulationNode.SignalName.UnitStateRemoved, capturedId);
                }), (uint)GodotObject.ConnectFlags.OneShot);
            }
            return;
        }

        // Legacy mode: cleanup after death animation delay
        var legacyTween = CreateTween();
        legacyTween.TweenInterval(DeathCleanupDelay);
        legacyTween.TweenCallback(Callable.From(QueueFree));
    }

    /// <summary>
    /// Unregister this unit from the multiplayer system.
    /// Called on death to clean up NetworkIdRegistry.
    /// Does NOT broadcast UnitDied — HostRunner handles that via UnitDiedSimEvent.
    /// </summary>
    private void UnregisterFromMultiplayerSystem()
    {
        if (NetworkId < 0) return; // Not registered with multiplayer

        var session = MatchSession.Current;
        if (session == null || !session.IsActive) return;

        // Unregister from NetworkIdRegistry (visual cleanup only)
        session.NetworkIds.Unregister(this);

        NetworkId = -1;
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

        // Check OnTakeHit triggers
        CheckOnTakeHitTriggers();

        // Check HP-based triggers after damage
        CheckHpTriggers();
    }

    // =========================================================================
    // TRIGGER SYSTEM
    // =========================================================================

    /// <summary>
    /// Update active trigger durations and deactivate expired triggers.
    /// Called every physics frame.
    /// </summary>
    private void UpdateTriggers(float delta)
    {
        bool needsStatRecalc = false;

        foreach (var trigger in _activeTriggers)
        {
            // Update cooldowns
            if (trigger.CooldownRemaining > 0)
            {
                trigger.CooldownRemaining -= delta;
            }

            // Update durations for active triggers
            if (trigger.IsActive && trigger.RemainingDuration > 0)
            {
                trigger.RemainingDuration -= delta;
                if (trigger.RemainingDuration <= 0)
                {
                    trigger.IsActive = false;
                    needsStatRecalc = true;
                }
            }
        }

        if (needsStatRecalc)
        {
            RecalculateStatsWithTriggers();
        }
    }

    /// <summary>
    /// Check HP-based triggers (BelowHpPercent, AboveHpPercent).
    /// </summary>
    private void CheckHpTriggers()
    {
        if (_triggeredModifiers.Count == 0)
            return;

        float hpPercent = MaxHp > 0 ? CurrentHp / MaxHp : 1f;
        bool needsStatRecalc = false;

        for (int i = 0; i < _activeTriggers.Count; i++)
        {
            var trigger = _activeTriggers[i];
            var mod = trigger.Modifier;

            if (mod.Trigger == TriggerCondition.BelowHpPercent)
            {
                bool shouldBeActive = hpPercent < mod.TriggerThreshold;
                if (shouldBeActive != trigger.IsActive)
                {
                    trigger.IsActive = shouldBeActive;
                    trigger.RemainingDuration = mod.TriggerDuration; // 0 = permanent while condition holds
                    needsStatRecalc = true;
                }
            }
            else if (mod.Trigger == TriggerCondition.AboveHpPercent)
            {
                bool shouldBeActive = hpPercent > mod.TriggerThreshold;
                if (shouldBeActive != trigger.IsActive)
                {
                    trigger.IsActive = shouldBeActive;
                    trigger.RemainingDuration = mod.TriggerDuration;
                    needsStatRecalc = true;
                }
            }
        }

        if (needsStatRecalc)
        {
            RecalculateStatsWithTriggers();
        }
    }

    /// <summary>
    /// Check OnTakeHit triggers when unit takes damage.
    /// </summary>
    private void CheckOnTakeHitTriggers()
    {
        bool needsStatRecalc = false;

        for (int i = 0; i < _activeTriggers.Count; i++)
        {
            var trigger = _activeTriggers[i];
            var mod = trigger.Modifier;

            if (mod.Trigger == TriggerCondition.OnTakeHit)
            {
                // Check cooldown
                if (trigger.CooldownRemaining > 0)
                    continue;

                // Activate the trigger
                trigger.IsActive = true;
                trigger.RemainingDuration = mod.TriggerDuration;
                trigger.CooldownRemaining = mod.TriggerCooldown;
                needsStatRecalc = true;
            }
        }

        if (needsStatRecalc)
        {
            RecalculateStatsWithTriggers();
        }
    }

    /// <summary>
    /// Called when this unit deals damage. Check OnHit triggers.
    /// </summary>
    public void OnDealDamage(float amount, Node3D target)
    {
        bool needsStatRecalc = false;

        for (int i = 0; i < _activeTriggers.Count; i++)
        {
            var trigger = _activeTriggers[i];
            var mod = trigger.Modifier;

            if (mod.Trigger == TriggerCondition.OnHit)
            {
                if (trigger.CooldownRemaining > 0)
                    continue;

                trigger.IsActive = true;
                trigger.RemainingDuration = mod.TriggerDuration;
                trigger.CooldownRemaining = mod.TriggerCooldown;
                needsStatRecalc = true;
            }
        }

        if (needsStatRecalc)
        {
            RecalculateStatsWithTriggers();
        }
    }

    /// <summary>
    /// Called when this unit kills an enemy. Check OnKill triggers.
    /// </summary>
    public void OnKill(Node3D target)
    {
        bool needsStatRecalc = false;

        for (int i = 0; i < _activeTriggers.Count; i++)
        {
            var trigger = _activeTriggers[i];
            var mod = trigger.Modifier;

            if (mod.Trigger == TriggerCondition.OnKill)
            {
                if (trigger.CooldownRemaining > 0)
                    continue;

                trigger.IsActive = true;
                trigger.RemainingDuration = mod.TriggerDuration;
                trigger.CooldownRemaining = mod.TriggerCooldown;
                needsStatRecalc = true;

                // Handle special on-kill effects like healing
                if (mod.StatAdds.TryGetValue(StatKey.HealOnKill, out var healAmount))
                {
                    Heal(healAmount);
                }
            }
        }

        if (needsStatRecalc)
        {
            RecalculateStatsWithTriggers();
        }
    }

    /// <summary>
    /// Recalculate stats including currently active trigger modifiers.
    /// </summary>
    private void RecalculateStatsWithTriggers()
    {
        // Start with base stats
        float hpAdd = 0f, damageAdd = 0f, speedAdd = 0f, moveSpeedAdd = 0f;
        float hpMult = 1f, damageMult = 1f, speedMult = 1f, moveSpeedMult = 1f;

        // Apply active trigger modifiers
        foreach (var trigger in _activeTriggers)
        {
            if (!trigger.IsActive)
                continue;

            var mod = trigger.Modifier;

            // Process stat_adds
            if (mod.StatAdds.TryGetValue(StatKey.MaxHp, out var hp)) hpAdd += hp;
            if (mod.StatAdds.TryGetValue(StatKey.AttackDamage, out var dmg)) damageAdd += dmg;
            if (mod.StatAdds.TryGetValue(StatKey.AttackSpeed, out var spd)) speedAdd += spd;
            if (mod.StatAdds.TryGetValue(StatKey.MoveSpeed, out var mvSpd)) moveSpeedAdd += mvSpd;

            // Process stat_mults
            if (mod.StatMults.TryGetValue(StatKey.MaxHp, out var hpM)) hpMult *= hpM;
            if (mod.StatMults.TryGetValue(StatKey.AttackDamage, out var dmgM)) damageMult *= dmgM;
            if (mod.StatMults.TryGetValue(StatKey.AttackSpeed, out var spdM)) speedMult *= spdM;
            if (mod.StatMults.TryGetValue(StatKey.MoveSpeed, out var mvSpdM)) moveSpeedMult *= mvSpdM;

            // Process flags
            foreach (var kvp in mod.Flags)
            {
                _activeModifierFlags[kvp.Key] = kvp.Value;
            }
        }

        // Apply modifiers: (base + adds) * mults
        // Note: We use the current stats as base since static modifiers were already applied
        float currentMaxHp = MaxHp;
        MaxHp = (_baseMaxHp + hpAdd) * hpMult;
        AttackDamage = (_baseAttackDamage + damageAdd) * damageMult;
        AttackSpeed = (_baseAttackSpeed + speedAdd) * speedMult;
        MoveSpeed = (_baseMoveSpeed + moveSpeedAdd) * moveSpeedMult;

        // Update HP bar if max HP changed (don't heal to max, keep current HP ratio)
        if (MaxHp != currentMaxHp)
        {
            _health.SetMaxHp(MaxHp, healToMax: false);
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
    /// In sim-driven mode, damage is handled by the simulation — this method is a no-op.
    /// </summary>
    public void TakeDamage(float amount, string damageType)
    {
        if (!IsAlive || IsDying)
            return;

        // In sim-driven mode, all damage goes through Simulation.Tick() → SimBehavior → SimDamage.
        // Visual feedback comes via UnitDamagedEvent signal, not this method.
        if (IsSimDriven)
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
            if (mod.StatAdds.TryGetValue(StatKey.MaxHp, out var hp)) hpAdd += hp;
            if (mod.StatAdds.TryGetValue(StatKey.AttackDamage, out var dmg)) damageAdd += dmg;
            if (mod.StatAdds.TryGetValue(StatKey.AttackSpeed, out var spd)) speedAdd += spd;
            if (mod.StatAdds.TryGetValue(StatKey.MoveSpeed, out var mvSpd)) moveSpeedAdd += mvSpd;

            // Process stat_mults
            if (mod.StatMults.TryGetValue(StatKey.MaxHp, out var hpM)) hpMult *= hpM;
            if (mod.StatMults.TryGetValue(StatKey.AttackDamage, out var dmgM)) damageMult *= dmgM;
            if (mod.StatMults.TryGetValue(StatKey.AttackSpeed, out var spdM)) speedMult *= spdM;
            if (mod.StatMults.TryGetValue(StatKey.MoveSpeed, out var mvSpdM)) moveSpeedMult *= mvSpdM;

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
    /// Initialize unit with partitioned modifiers (static applied immediately, triggered stored).
    /// </summary>
    public void InitializeWithPartitionedModifiers(List<StatModifier> staticModifiers, List<StatModifier> triggeredModifiers)
    {
        // Apply static modifiers immediately
        InitializeWithModifiers(staticModifiers);

        // Store triggered modifiers for event-based activation
        _triggeredModifiers = triggeredModifiers;
        _activeTriggers.Clear();

        // Initialize trigger tracking for each triggered modifier
        foreach (var mod in _triggeredModifiers)
        {
            _activeTriggers.Add(new ActiveTrigger
            {
                Modifier = mod,
                RemainingDuration = 0f,
                CooldownRemaining = 0f,
                IsActive = false
            });
        }

        // Check HP-based triggers on initialization
        CheckHpTriggers();
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
    /// Uses visual component to calculate center mass, plus optional TargetPointOffset.
    /// Method name uses snake_case for cross-language duck typing compatibility.
    /// </summary>
    public Vector3 get_projectile_target_position()
    {
        // Calculate base target from visual component
        float height = VisualComponent?.GetSpriteHeight() ?? 1.0f;
        Vector3 baseTarget = GlobalPosition + new Vector3(0, height * CenterMassHeightFraction, 0);

        // Apply configurable offset (X flips with facing direction)
        if (TargetPointOffset != Vector3.Zero)
        {
            float xOffset = _isFacingRight ? TargetPointOffset.X : -TargetPointOffset.X;
            return baseTarget + new Vector3(xOffset, TargetPointOffset.Y, TargetPointOffset.Z);
        }

        return baseTarget;
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
        if (!TryCreateHpBar())
        {
            // HPBarService may not be initialized yet (e.g., client-side timing).
            // Retry on next frame via CallDeferred.
            GD.PushWarning($"[Unit3D] HP bar creation deferred for {UnitId} — HPBarService not ready");
            CallDeferred(MethodName.RetryCreateHpBar);
        }
    }

    private bool TryCreateHpBar()
    {
        if (HPBarService.Instance == null)
            return false;

        FloatingHPBar? bar;
        if (HpBarOffsetY > 0)
        {
            var settings = HPBarSettings.Default with { OffsetY = HpBarOffsetY };
            bar = HPBarService.Instance.CreateBarForUnit(this, settings);
        }
        else
        {
            bar = HPBarService.Instance.CreateBarForUnit(this);
        }
        return bar != null;
    }

    private void RetryCreateHpBar()
    {
        if (!IsInsideTree() || !IsAlive) return;
        if (!TryCreateHpBar())
        {
            GD.PrintErr($"[Unit3D] HP bar creation failed on retry for {UnitId}");
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
        // Default hurtbox radius - units should explicitly set HurtboxRadius for non-standard sizes
        const float DefaultHurtboxRadius = 0.5f;
        float radius = HurtboxRadius > 0 ? HurtboxRadius : DefaultHurtboxRadius;
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

    private MeshInstance3D? _debugHurtboxMarker;
    private MeshInstance3D? _debugTargetPointMarker;
    private MeshInstance3D? _debugAttackRangeMarker;
    private MeshInstance3D? _debugSeparationRadiusMarker;

    // Debug state is held by UnitDebugService (autoload, accessible from GDScript).
    // These helpers read from the singleton for convenience within Unit3D.
    private static bool IsDebugHurtboxEnabled() => UnitDebugService.Instance?.HurtboxEnabled ?? false;
    private static bool IsDebugTargetPointEnabled() => UnitDebugService.Instance?.TargetPointEnabled ?? false;
    private static bool IsDebugAttackRangeEnabled() => UnitDebugService.Instance?.AttackRangeEnabled ?? false;
    private static bool IsDebugSeparationRadiusEnabled() => UnitDebugService.Instance?.SeparationRadiusEnabled ?? false;

    public override void _Process(double delta)
    {
        // Early out if no debug mode is active and no cleanup needed
        bool anyDebugEnabled = (UnitDebugService.Instance?.AnyEnabled) ?? false;
        bool anyMarkerExists = _debugHurtboxMarker != null || _debugTargetPointMarker != null || _debugAttackRangeMarker != null || _debugSeparationRadiusMarker != null;

        if (!anyDebugEnabled && !anyMarkerExists)
            return;

        // Hurtbox debug visualization
        if (IsDebugHurtboxEnabled())
        {
            UpdateDebugHurtboxMarker();
        }
        else if (_debugHurtboxMarker != null)
        {
            _debugHurtboxMarker.QueueFree();
            _debugHurtboxMarker = null;
        }

        // Target point debug visualization
        if (IsDebugTargetPointEnabled())
        {
            UpdateDebugTargetPointMarker();
        }
        else if (_debugTargetPointMarker != null)
        {
            _debugTargetPointMarker.QueueFree();
            _debugTargetPointMarker = null;
        }

        // Attack range debug visualization
        if (IsDebugAttackRangeEnabled())
        {
            UpdateDebugAttackRangeMarker();
        }
        else if (_debugAttackRangeMarker != null)
        {
            _debugAttackRangeMarker.QueueFree();
            _debugAttackRangeMarker = null;
        }

        // Separation radius debug visualization
        if (IsDebugSeparationRadiusEnabled())
        {
            UpdateDebugSeparationRadiusMarker();
        }
        else if (_debugSeparationRadiusMarker != null)
        {
            _debugSeparationRadiusMarker.QueueFree();
            _debugSeparationRadiusMarker = null;
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
            const float DefaultHurtboxRadius = 0.5f;
            var capsule = new CapsuleMesh
            {
                Radius = _hurtbox?.Radius ?? DefaultHurtboxRadius,
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

        // Orange - calculated center-mass position
        var color = new Color(1.0f, 0.6f, 0.2f, 0.7f);

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

    private void UpdateDebugSeparationRadiusMarker()
    {
        if (_debugSeparationRadiusMarker == null)
        {
            _debugSeparationRadiusMarker = CreateDebugSeparationRadiusCircle();
            AddChild(_debugSeparationRadiusMarker);
        }

        // Position at ground level (slight offset to prevent z-fighting)
        _debugSeparationRadiusMarker.GlobalPosition = new Vector3(
            GlobalPosition.X,
            0.03f,
            GlobalPosition.Z
        );
    }

    private MeshInstance3D CreateDebugSeparationRadiusCircle()
    {
        var mesh = new MeshInstance3D();
        var cylinder = new CylinderMesh
        {
            TopRadius = SeparationRadius,
            BottomRadius = SeparationRadius,
            Height = 0.05f  // Very thin disc
        };
        mesh.Mesh = cylinder;

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.8f, 0.4f, 1.0f, 0.4f),  // Purple
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            NoDepthTest = true,
            RenderPriority = 98  // Below attack range and hurtbox
        };
        mesh.MaterialOverride = material;

        return mesh;
    }
}
