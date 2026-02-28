class_name CSharpAutoloads
## Constants for C# autoload paths.
## C# autoloads cannot be accessed directly from GDScript - use get_node_or_null() with these paths.

# Core services
const MODIFIER_SERVICE: String = "/root/ModifierService"
const PLAYER_CARD_SERVICE: String = "/root/CardServiceCS"
const HP_BAR_SERVICE: String = "/root/HPBarService"
const PROJECTILE_CATALOG: String = "/root/ProjectileCatalog"
const PROJECTILE_SERVICE: String = "/root/ProjectileService"
const CARD_FACTORY: String = "/root/CardFactory"
const DAMAGE_SYSTEM: String = "/root/DamageSystem"
const HIT_RESOLVER: String = "/root/HitResolver"
const SPATIAL_GRID: String = "/root/SpatialGrid"
const TARGETING_CONFIG_REGISTRY: String = "/root/TargetingConfigRegistryCS"
const LEVEL_CAP_SERVICE: String = "/root/LevelCapService"
const REWARD_SERVICE_CS: String = "/root/RewardServiceCS"
const UNIT_DEBUG_SERVICE: String = "/root/UnitDebugService"

# Multiplayer services
const NAKAMA_GAME_CLIENT: String = "/root/NakamaGameClient"
const MATCHMAKING_SERVICE: String = "/root/MatchmakingService"
const RANKING_SERVICE: String = "/root/RankingService"
const MATCH_REPORTER: String = "/root/MatchReporter"
const LEADERBOARD_SERVICE: String = "/root/LeaderboardService"
const RECONNECTION_HANDLER: String = "/root/ReconnectionHandler"

# Bridge autoloads (GDScript wrappers around C# implementations)
const CARD_CATALOG_CS: String = "/root/CardCatalogCS"
const SUMMONER_CATALOG_CS: String = "/root/SummonerCatalogCS"
const TRAIT_CATALOG_CS: String = "/root/TraitCatalogCS"
const PROFILE_REPOSITORY_CS: String = "/root/ProfileRepositoryCS"
const ECONOMY_SERVICE_CS: String = "/root/EconomyServiceCS"
