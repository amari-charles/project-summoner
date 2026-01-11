class_name ProjectileIDs

## Projectile ID Constants
## Type-safe constants for all projectile definitions in ContentCatalog
##
## Usage:
##   card.projectile_id = ProjectileIDs.FIREBALL
##   var projectile = ContentCatalog.get_projectile(ProjectileIDs.ARROW)
##
## IMPORTANT: These constants must match the projectile IDs defined in ContentCatalog._load_projectiles()
## If they get out of sync, validation will fail at startup.

# Projectile IDs
const FIREBALL: StringName = &"fireball"
const ARROW: StringName = &"arrow"
const EMBER: StringName = &"ember"
const MANA_BOLT: StringName = &"mana_bolt"
