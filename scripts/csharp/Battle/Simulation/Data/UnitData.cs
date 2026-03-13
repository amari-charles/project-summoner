using System.Collections.Generic;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Per-unit gameplay state stored in MatchState.
/// Tracks ALL logical state for each spawned unit.
/// The simulation operates exclusively on UnitData — Unit3D is a visual puppet.
/// </summary>
public class UnitData
{
    public int UnitId { get; set; }
    public int NetworkId { get; set; } = -1;
    public SimUnitCatalogId CatalogId { get; set; } = SimUnitCatalogId.Empty;
    public Team Team { get; set; }

    // HP
    public float CurrentHp { get; set; }
    public float MaxHp { get; set; }
    public bool IsAlive { get; set; } = true;

    // Position — owned by simulation, read by Unit3D for visuals
    public SimVector3 Position { get; set; }

    // Stats
    public float AttackDamage { get; set; }
    public float AttackSpeed { get; set; }
    public float MoveSpeed { get; set; }
    public float AttackRange { get; set; }
    public float AggroRadius { get; set; } = 20f;
    public float SoulStrength { get; set; }
    public float SeparationRadius { get; set; } = 0.5f;
    public float NavigationRadius { get; set; } = 0.5f;
    public float HurtboxRadius { get; set; } = 0.5f;
    public float HurtboxHeight { get; set; }
    public bool HurtboxHorizontal { get; set; }
    public SimVector3 HurtboxOffset { get; set; } = SimVector3.Zero;
    public float CritChance { get; set; }
    public float CritDamage { get; set; } = 1.5f;

    // Damage type and defenses
    public DamageType AttackType { get; set; } = DamageType.Physical;
    public float PhysicalDamageRatio { get; set; } = 1f;
    public float ElementalDamageRatio { get; set; }
    public float PhysicalDefense { get; set; }
    public float MagicDefense { get; set; }
    public float Evasion { get; set; }

    // Attack vector fields (PASS 2 grouped state)
    public AttackVectorState Attack { get; set; } = AttackVectorState.Default();

    // Classification
    public UnitType UnitType { get; set; }
    public TacticalRole TacticalRole { get; set; } = TacticalRole.Auto;
    public MovementLayer MovementLayer { get; set; }
    public int AssignedLane { get; set; } = -1;

    // Element (int cast of Fateforged.Cards.Element enum)
    public int ElementId { get; set; } // 0=Neutral

    // Group relationships
    public int? GroupId { get; set; }
    public int? LeaderId { get; set; }

    // Combat behavior configuration
    public MovementStyle MovementStyle { get; set; } = MovementStyle.Direct;
    public TargetingPriority TargetingPriority { get; set; } = TargetingPriority.Nearest;
    public RetreatCondition RetreatCondition { get; set; } = RetreatCondition.Never;
    public float KiteRange { get; set; }

    // Buffs and triggers
    public List<ActiveBuff> ActiveBuffs { get; set; } = new();
    public List<TriggerConfig> Triggers { get; set; } = new();

    // Targeting profile (extracted from TargetingConfig at registration)
    public FallbackMovement FallbackMovement { get; set; }
    public EngageShape EngageShape { get; set; } = EngageShape.Circle;
    public float EngageRectLength { get; set; }
    public float EngageRectHalfWidth { get; set; }
    public float EngageRectForwardOffset { get; set; }
    public float EngageCloseRadius { get; set; } = 0.4f;
    public bool HasConeConstraint { get; set; }
    public float ConeHalfAngle { get; set; } = 30f;
    public float ConeCenterOffsetDegrees { get; set; }
    public float CloseRangeThreshold { get; set; } = 0.5f;
    public TargetLayer TargetLayerFilter { get; set; }
    public float DistanceScorerWeight { get; set; } = 1f;
    public float HealthScorerWeight { get; set; }
    public TargetPolicyId TargetPolicyId { get; set; } = TargetPolicyId.PreferAttackableAndStick;
    public MovementIntentStrategy MovementIntentStrategy { get; set; } =
        MovementIntentStrategy.Context;
    public float FlightAltitude { get; set; }

    // Velocity — computed by simulation each tick
    public SimVector3 Velocity { get; set; }

    // Facing
    public bool IsFacingRight { get; set; }
    public float FacingLockTimer { get; set; }

    // Blocked-navigation assist (yield + side-step escape)
    public int? NavigationTargetId { get; set; }
    public float NavigationLastTargetDistance { get; set; } = -1f;
    public float NavigationBlockedTime { get; set; }
    public float NavigationYieldTimer { get; set; }
    public float NavigationEscapeTimer { get; set; }
    public bool NavigationEscapeQueued { get; set; }
    public int NavigationEscapeDirectionSign { get; set; } = 1;

    // Targeting — simulation-owned
    public int? TargetUnitId { get; set; }
    public float TargetLockTimer { get; set; }

    // Commit-slot lifecycle runtime state.
    public CombatLifecycleState CombatLifecycleState { get; set; } =
        CombatLifecycleState.AcquireTarget;
    public int? LockedTargetUnitId { get; set; }
    public RetargetReason LastRetargetReason { get; set; } = RetargetReason.None;
    public float UnreachableTimer { get; set; }
    public float UnreachableTimeoutSeconds { get; set; } = 1.2f;

    // Slot runtime state.
    public int? SlotTargetId { get; set; }
    public int? ReservedSlotId { get; set; }
    public int? OccupiedSlotId { get; set; }
    public float SlotWaitTimer { get; set; }
    public float SlotWaitTimeoutSeconds { get; set; } = 0.7f;
    public float LastSlotDistance { get; set; } = -1f;
    public float LastTargetDistance { get; set; } = -1f;
    public float NoProgressTimer { get; set; }
    public float LastReservationDistanceSq { get; set; } = float.MaxValue;
    public int? DroppedTargetUnitId { get; set; }
    public float DroppedTargetCooldownTimer { get; set; }
    public float DroppedTargetCooldownSeconds { get; set; } = 0.75f;

    // Forced targeting (e.g., redirect spell)
    public int? ForcedTargetUnitId { get; set; }
    public float ForcedTargetTimer { get; set; }

    // Combat cooldowns
    public float AttackCooldown { get; set; }

    // Behavior state (used by SimBehavior)
    public BehaviorState BehaviorState { get; set; }
    public float AttackAnimationTimer { get; set; }

    // Attack loop state.
    public AttackPhase AttackPhase { get; set; } = AttackPhase.None;
    public float AttackPhaseTimer { get; set; }
    public int? AttackPhaseLockTargetId { get; set; }

    // Pending basic attack payload queued at attack start and resolved once at
    // windup->active commit.
    public int? PendingAttackTargetId { get; set; }
    public float PendingAttackBaseDamage { get; set; }
    public bool PendingAttackTargetsSummoner { get; set; }

    // Delayed ranged resolution buffers:
    // - Unit targets: windup before spawning SimProjectileData
    // - Summoner targets: windup before spawning a projectile (ranged),
    //   or direct damage resolution for melee-only paths
    public SimProjectileCatalogId ProjectileCatalogId { get; set; } = SimProjectileCatalogId.Empty;
    public float ProjectileDelay { get; set; }
    public float PendingDamageTimer { get; set; }
    public int? PendingDamageTargetId { get; set; }
    public float PendingDamageAmount { get; set; }

    // Charge tracking (distance traveled since last attack — for charge ability)
    public float DistanceTraveled { get; set; }

    // Death cleanup
    public float DeathCleanupTimer { get; set; }

    // Activation
    public ActivationState ActivationState { get; set; }

    // Spawn timer — unit stays Inactive until this counts down to 0, then self-activates.
    // Set to casting duration at spawn time. 0 = no pending activation.
    public float SpawnTimer { get; set; }

    // Legacy field — kept for snapshot compatibility during migration
    public int? TargetNetworkId { get; set; }

    /// <summary>
    /// Player units face right, enemy units face left.
    /// Sprites are drawn facing left, so player units get flipped.
    /// </summary>
    public static bool DefaultFacingForTeam(Team team) => team == Team.Player;
}
