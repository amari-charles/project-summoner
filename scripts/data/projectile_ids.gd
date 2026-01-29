class_name ProjectileIDs

## Projectile ID Constants
## Type-safe constants for all projectile definitions in ProjectileCatalog
##
## Usage:
##   card.projectile_id = ProjectileIDs.FIREBALL
##   var projectile_catalog: Node = get_node("/root/ProjectileCatalog")
##   var projectile = projectile_catalog.GetProjectile(ProjectileIDs.ARROW)
##
## IMPORTANT: These constants must match the projectile IDs in data/projectiles/*.json
## If they get out of sync, validation will fail at startup.

# Projectile IDs
const ARROW: StringName = &"arrow"
const EMBER: StringName = &"ember"
const FIREBALL: StringName = &"fireball"
const MANA_BOLT: StringName = &"mana_bolt"
const WIND_PUFF: StringName = &"wind_puff"
