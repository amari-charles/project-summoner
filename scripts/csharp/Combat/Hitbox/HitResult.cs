using Godot;

namespace ProjectSummoner.Combat.Hitbox;

/// <summary>
/// Result of a hit being resolved.
/// Contains information about what happened when a hitbox hit a hurtbox.
/// </summary>
public struct HitResult
{
    /// <summary>The entity that dealt the damage.</summary>
    public Node3D Attacker;

    /// <summary>The entity that received the damage.</summary>
    public Node3D Target;

    /// <summary>World position where the hit occurred.</summary>
    public Vector3 HitPosition;

    /// <summary>Actual damage dealt after modifiers.</summary>
    public float DamageDealt;

    /// <summary>Whether the hit was a critical hit.</summary>
    public bool WasCrit;

    /// <summary>Whether the target was killed by this hit.</summary>
    public bool TargetKilled;

    /// <summary>Whether the hit was blocked by a shield/barrier.</summary>
    public bool WasBlocked;
}
