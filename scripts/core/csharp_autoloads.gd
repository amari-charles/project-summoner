class_name CSharpAutoloads
## Constants for C# autoload paths.
## C# autoloads cannot be accessed directly from GDScript - use get_node_or_null() with these paths.

# Core services
const MODIFIER_SERVICE: String = "/root/ModifierService"
const PLAYER_CARD_SERVICE: String = "/root/PlayerCardService"
const HP_BAR_SERVICE: String = "/root/HPBarService"
const PROJECTILE_SERVICE: String = "/root/ProjectileService"
const CARD_FACTORY: String = "/root/CardFactory"
const DAMAGE_SYSTEM: String = "/root/DamageSystem"
const HIT_RESOLVER: String = "/root/HitResolver"
const SPATIAL_GRID: String = "/root/SpatialGrid"
const TARGETING_CONFIG_REGISTRY: String = "/root/TargetingConfigRegistryCS"

# Bridge autoloads (GDScript wrappers around C# implementations)
const CARD_CATALOG_CS: String = "/root/CardCatalogCS"
const SUMMONER_CATALOG_CS: String = "/root/SummonerCatalogCS"
const TRAIT_CATALOG_CS: String = "/root/TraitCatalogCS"
const PROFILE_REPOSITORY_CS: String = "/root/ProfileRepositoryCS"
const ECONOMY_SERVICE_CS: String = "/root/EconomyServiceCS"
