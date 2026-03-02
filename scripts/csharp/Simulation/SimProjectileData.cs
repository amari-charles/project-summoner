using ProjectSummoner.Projectiles;
using ProjectSummoner.Units;

namespace Fateforged.Simulation;

/// <summary>
/// Per-projectile gameplay state stored in MatchState.
/// Tracks ALL logical state for each active projectile.
/// The simulation operates exclusively on SimProjectileData — Projectile3D is a visual puppet.
/// </summary>
public class SimProjectileData
{
    // Identity
    public int ProjectileId { get; set; }
    public int SourceUnitId { get; set; }
    public int TargetUnitId { get; set; }
    public Team Team { get; set; }

    // Damage
    public float Damage { get; set; }
    public int SourceElementId { get; set; }

    // Movement type
    public ProjectileMovementType MovementType { get; set; }

    // Path state
    public SimVector3 StartPosition { get; set; }
    public SimVector3 TargetPosition { get; set; }
    public SimVector3 CurrentPosition { get; set; }
    public SimVector3 LastPosition { get; set; }
    public SimVector3 Direction { get; set; }

    // Progress (0 to 1 for path-based movement)
    public float Progress { get; set; }
    public float Speed { get; set; }
    public float TimeAlive { get; set; }
    public float Lifetime { get; set; } = 5f;

    // Arc path
    public float ArcHeight { get; set; }

    // Ballistic path (pre-computed at spawn)
    public SimVector3 HorizontalVelocity { get; set; }
    public float InitialVerticalVelocity { get; set; }
    public float TotalTime { get; set; }
    public float Gravity { get; set; } = 9.8f;

    // WeavingHoming state
    public WeavingPhase WeavingPhase { get; set; }
    public float PhaseTimer { get; set; }
    public SimVector3 Velocity { get; set; }
    public SimVector3 VeerDirection { get; set; }
    public float ScaledVeerDelay { get; set; }
    public float ScaledVeerDuration { get; set; }
    public float SteerStrength { get; set; } = 180f;

    // Hit detection
    public float HitRadius { get; set; } = 2.5f;
    public int PierceRemaining { get; set; }
    public float AoeRadius { get; set; }

    // Cached path length
    public float PathLength { get; set; }

    // Lifecycle
    public bool IsDead { get; set; }
}
