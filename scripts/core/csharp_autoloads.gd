class_name CSharpAutoloads
## Constants for C# autoload paths.
## C# autoloads can be accessed directly by name from GDScript (e.g., Economy.GetGold()).
## These constants exist for cases where get_node_or_null() is needed (e.g., optional dependencies).

# Core services
const PLAYER_CARD_SERVICE: String = "/root/CardServiceCS"
const LEVEL_CAP_SERVICE: String = "/root/LevelCapService"

# Meta-game services (C# autoloads with clean names — GDScript wrappers deleted)
const ECONOMY: String = "/root/Economy"
const DECKS: String = "/root/Decks"
const SUMMONER_PROGRESSION: String = "/root/SummonerProgression"
const SUMMONER_SELECTION: String = "/root/SummonerSelection"
const ITEMS: String = "/root/Items"
const REWARD_SERVICE: String = "/root/RewardService"
const CAMPAIGN: String = "/root/Campaign"

# Shop (GDScript wrapper kept — substantial local logic)
const SHOP_SERVICE_CS: String = "/root/ShopServiceCS"

# Data catalogs
const CARD_CATALOG: String = "/root/CardCatalog"
const SUMMONER_CATALOG: String = "/root/SummonerCatalog"
const TRAIT_CATALOG: String = "/root/TraitCatalog"
const PROJECTILE_CATALOG: String = "/root/ProjectileCatalog"

# Infrastructure
const PROFILE_REPO: String = "/root/ProfileRepo"

# Multiplayer services
const NAKAMA_GAME_CLIENT: String = "/root/NakamaGameClient"
const MATCHMAKING_SERVICE: String = "/root/MatchmakingService"
const RANKING_SERVICE: String = "/root/RankingService"
const MATCH_REPORTER: String = "/root/MatchReporter"
const LEADERBOARD_SERVICE: String = "/root/LeaderboardService"
