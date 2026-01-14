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
    Summoner = 1 << 1,              // Legacy - kept for backwards compat
    Structure = 1 << 2,
    Projectile = 1 << 3,            // For interceptable projectiles
    SummonerMelee = 1 << 4,         // Large circle around summoner for melee attacks
    SummonerProjectile = 1 << 5,    // Sprite-sized area for projectile hits
    All = Unit | Summoner | Structure | Projectile | SummonerMelee | SummonerProjectile
}
