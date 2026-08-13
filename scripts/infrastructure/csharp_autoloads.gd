class_name CSharpAutoloads
## Constants for C# autoload paths.
## C# autoloads can be accessed directly by name from GDScript (e.g., EconomyApi.get_gold()).
## These constants exist for cases where get_node_or_null() is needed (e.g., optional dependencies).

# Core services
const CARD_SERVICE: String = "/root/CardService"
const LEVEL_CAP_SERVICE: String = "/root/LevelCapService"
const BATTLEFIELD_DEBUG: String = "/root/BattlefieldDebug"

# Meta-game services (C# autoloads with clean names — GDScript wrappers deleted)
const ECONOMY: String = "/root/Economy"
const DECKS: String = "/root/Decks"
const SUMMONER_PROGRESSION: String = "/root/SummonerProgression"
const SUMMONER_SELECTION: String = "/root/SummonerSelection"
const TRAIT_TREE_SERVICE: String = "/root/TraitTreeService"
const ITEMS: String = "/root/Items"
const REWARD_SERVICE: String = "/root/RewardService"
const CAMPAIGN: String = "/root/Campaign"
const PROGRESSION_AUTHORITY: String = "/root/ProgressionAuthority"
const NARRATIVE_DIRECTOR: String = "/root/NarrativeDirector"

# Shop (C# autoload — GDScript wrapper deleted)
const SHOP: String = "/root/Shop"

# Data catalogs
const CARD_CATALOG: String = "/root/CardCatalog"
const SUMMONER_CATALOG: String = "/root/SummonerCatalog"
const TRAIT_CATALOG: String = "/root/TraitCatalog"
const PROJECTILE_CATALOG: String = "/root/ProjectileCatalog"

# Infrastructure
const PROFILE_REPO: String = "/root/ProfileRepo"
const BILLING_CATALOG: String = "/root/BillingCatalog"
const PLATFORM_BILLING: String = "/root/PlatformBilling"

# Multiplayer services
const NAKAMA_GAME_CLIENT: String = "/root/NakamaGameClient"
const MATCHMAKING_SERVICE: String = "/root/MatchmakingService"
const RANKING_SERVICE: String = "/root/RankingService"
const LEADERBOARD_SERVICE: String = "/root/LeaderboardService"
