using System.Collections.Generic;

namespace Fateforged.Simulation;

/// <summary>
/// Per-unit gameplay state stored in MatchState.
/// Tracks ALL logical state for each spawned unit.
/// The simulation operates exclusively on UnitData — Unit3D is a visual puppet.
/// </summary>
public class UnitData
{
    public int UnitId { get; set; }
    public int NetworkId { get; set; } = -1;
    public string CatalogId { get; set; } = "";
    public int Team { get; set; }

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
    public float SeparationRadius { get; set; } = 0.5f;
    public float CritChance { get; set; }
    public float CritDamage { get; set; } = 1.5f;

    // Damage type and defenses
    public DamageType AttackType { get; set; } = DamageType.Physical;
    public float PhysicalDefense { get; set; }
    public float MagicDefense { get; set; }
    public float Evasion { get; set; }

    // Classification
    public int UnitType { get; set; } // 0=Melee, 1=Ranged
    public int MovementLayer { get; set; } // 0=Ground, 1=Air

    // Element (int cast of ProjectSummoner.Cards.Element enum)
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
    public int FallbackMovement { get; set; } // 0=MoveToward, 1=Strafe, 2=Idle
    public bool HasConeConstraint { get; set; }
    public float ConeHalfAngle { get; set; } = 30f;
    public float CloseRangeThreshold { get; set; } = 0.5f;
    public int TargetLayerFilter { get; set; } // 0=GroundOnly, 1=AirOnly, 2=Both
    public float DistanceScorerWeight { get; set; } = 1f;
    public float HealthScorerWeight { get; set; }
    public float FlightAltitude { get; set; }

    // Velocity — computed by simulation each tick
    public SimVector3 Velocity { get; set; }

    // Facing
    public bool IsFacingRight { get; set; }

    // Targeting — simulation-owned
    public int? TargetUnitId { get; set; }
    public float TargetLockTimer { get; set; }

    // Forced targeting (e.g., redirect spell)
    public int? ForcedTargetUnitId { get; set; }
    public float ForcedTargetTimer { get; set; }

    // Combat cooldowns
    public float AttackCooldown { get; set; }

    // Behavior state (used by SimBehavior)
    public int BehaviorState { get; set; } // 0=NoTarget, 1=Chasing, 2=InRange, 3=Attacking
    public float AttackAnimationTimer { get; set; }

    // Ranged unit pending damage (replaces projectile physics)
    public float ProjectileDelay { get; set; }
    public float PendingDamageTimer { get; set; }
    public int? PendingDamageTargetId { get; set; }
    public float PendingDamageAmount { get; set; }

    // Charge tracking (distance traveled since last attack — for charge ability)
    public float DistanceTraveled { get; set; }

    // Steering state (used by SimSteering)
    public float BlockedTime { get; set; }
    public SimVector3 LastPosition { get; set; }
    public float FlankAngle { get; set; } = 90f;
    public int FlankDirection { get; set; } // -1=left, 0=none, 1=right
    public float FlankProgressTimer { get; set; }

    // Death cleanup
    public float DeathCleanupTimer { get; set; }

    // Activation
    public int ActivationState { get; set; }

    // Legacy field — kept for snapshot compatibility during migration
    public int? TargetNetworkId { get; set; }
}
