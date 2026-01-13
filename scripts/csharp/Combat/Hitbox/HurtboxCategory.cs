using System;

namespace ProjectSummoner.Combat.Hitbox;

/// <summary>
/// Categories of entities that can be hit.
/// Used by hitboxes to filter what they can damage.
/// </summary>
[Flags]
public enum HurtboxCategory
{
    None = 0,
    Unit = 1 << 0,
    Summoner = 1 << 1,
    Structure = 1 << 2,
    Projectile = 1 << 3,  // For interceptable projectiles
    All = Unit | Summoner | Structure | Projectile
}
