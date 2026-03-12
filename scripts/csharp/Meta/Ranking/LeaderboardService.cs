using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fateforged.Multiplayer.Backend;
using Godot;
using Nakama;

namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// Service for fetching and displaying ranked leaderboards.
/// Uses Nakama leaderboards with local caching.
/// </summary>
public partial class LeaderboardService : Node
{
    #region Configuration

    /// <summary>
    /// Nakama leaderboard ID for ranked play.
    /// </summary>
    private const string RankedLeaderboardId = "ranked_1v1";

    /// <summary>
    /// Cache duration in seconds.
    /// </summary>
    private const float CacheDuration = 60f;

    /// <summary>
    /// Default number of entries to fetch for top players.
    /// </summary>
    private const int DefaultTopCount = 100;

    /// <summary>
    /// Number of nearby players to fetch around the current player.
    /// </summary>
    private const int NearbyRange = 5;

    #endregion

    #region State

    private List<LeaderboardEntry> _cachedTopPlayers = new();
    private LeaderboardEntry? _cachedPlayerRank;
    private float _cacheTimestamp;
    private bool _isFetching;

    /// <summary>
    /// Whether a fetch operation is currently in progress.
    /// </summary>
    public bool IsFetching => _isFetching;

    /// <summary>
    /// The cached top players list.
    /// </summary>
    public IReadOnlyList<LeaderboardEntry> TopPlayers => _cachedTopPlayers.AsReadOnly();

    /// <summary>
    /// The current player's rank entry.
    /// </summary>
    public LeaderboardEntry? PlayerRank => _cachedPlayerRank;

    #endregion

    #region Signals

    /// <summary>
    /// Emitted when leaderboard data is refreshed.
    /// </summary>
    [Signal]
    public delegate void LeaderboardRefreshedEventHandler();

    /// <summary>
    /// Emitted when fetching leaderboard fails.
    /// </summary>
    [Signal]
    public delegate void LeaderboardFetchFailedEventHandler(string error);

    #endregion

    #region Singleton

    /// <summary>
    /// The global LeaderboardService instance.
    /// </summary>
    public static LeaderboardService? Instance { get; private set; }

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Ready()
    {
        GD.Print("[LeaderboardService] Initialized");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get the top players on the leaderboard.
    /// Uses cached data if available and not expired.
    /// </summary>
    /// <param name="count">Number of players to fetch</param>
    /// <param name="forceRefresh">Force refresh even if cache is valid</param>
    /// <summary>
    /// GDScript-callable wrapper: fire-and-forget leaderboard fetch.
    /// Results are communicated via LeaderboardRefreshed signal.
    /// </summary>
    public void RefreshLeaderboard(int count = DefaultTopCount)
    {
        _ = RefreshLeaderboardAndRankAsync(count);
    }

    /// <summary>
    /// GDScript-callable wrapper: refresh top players and current-player rank in a single cycle.
    /// Emits LeaderboardRefreshed only after both caches are updated.
    /// </summary>
    public async Task RefreshLeaderboardAndRankAsync(int count = DefaultTopCount)
    {
        float now = (float)(Time.GetTicksMsec() / 1000.0);
        var entries = await FetchTopPlayersFromNakamaAsync(count);
        if (entries.Count > 0)
        {
            _cachedTopPlayers = entries;
        }

        var playerRank = await GetPlayerRankAsync(forceRefresh: true);
        if (playerRank != null)
        {
            _cachedPlayerRank = playerRank;
        }

        _cacheTimestamp = now;
        EmitSignal(SignalName.LeaderboardRefreshed);
    }

    public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(
        int count = DefaultTopCount,
        bool forceRefresh = false
    )
    {
        // Check cache
        float now = (float)(Time.GetTicksMsec() / 1000.0);
        if (!forceRefresh && _cachedTopPlayers.Count > 0 && now - _cacheTimestamp < CacheDuration)
        {
            return new List<LeaderboardEntry>(_cachedTopPlayers);
        }

        // Fetch from Nakama
        var entries = await FetchTopPlayersFromNakamaAsync(count);

        if (entries.Count > 0)
        {
            _cachedTopPlayers = entries;
            _cacheTimestamp = now;
        }

        return entries;
    }

    /// <summary>
    /// Get the current player's rank and surrounding players.
    /// </summary>
    public async Task<LeaderboardEntry?> GetPlayerRankAsync(bool forceRefresh = false)
    {
        var nakama = NakamaGameClient.Instance;
        if (nakama == null || !nakama.IsAuthenticated)
        {
            return CreateLocalPlayerEntry();
        }

        // Check cache
        float now = (float)(Time.GetTicksMsec() / 1000.0);
        if (!forceRefresh && _cachedPlayerRank != null && now - _cacheTimestamp < CacheDuration)
        {
            return _cachedPlayerRank;
        }

        // Fetch from Nakama
        var entry = await FetchPlayerRankFromNakamaAsync(nakama.UserId!);

        if (entry != null)
        {
            _cachedPlayerRank = entry;
        }
        else
        {
            // Fallback to local data
            _cachedPlayerRank = CreateLocalPlayerEntry();
        }

        return _cachedPlayerRank;
    }

    /// <summary>
    /// Get players near a specific rank.
    /// </summary>
    /// <param name="userId">User to center on</param>
    /// <param name="range">Number of players above and below</param>
    public async Task<List<LeaderboardEntry>> GetNearbyPlayersAsync(
        string userId,
        int range = NearbyRange
    )
    {
        var nakama = NakamaGameClient.Instance;
        if (
            nakama == null
            || !nakama.IsAuthenticated
            || nakama.Client == null
            || nakama.Session == null
        )
        {
            return new List<LeaderboardEntry> { CreateLocalPlayerEntry() };
        }

        try
        {
            _isFetching = true;

            // Fetch records around the user
            var result = await nakama.Client.ListLeaderboardRecordsAroundOwnerAsync(
                nakama.Session,
                RankedLeaderboardId,
                userId,
                limit: range * 2 + 1
            );

            var entries = new List<LeaderboardEntry>();
            foreach (var record in result.Records)
            {
                int rank = int.Parse(record.Rank);
                int score = int.Parse(record.Score);
                entries.Add(
                    new LeaderboardEntry
                    {
                        Rank = rank,
                        UserId = record.OwnerId,
                        DisplayName = record.Username ?? "Unknown",
                        Rating = score,
                        Tier = EloCalculator.GetTier(score),
                        Division = EloCalculator.GetDivision(score),
                        IsCurrentPlayer = record.OwnerId == nakama.UserId,
                    }
                );
            }

            return entries;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LeaderboardService] Failed to fetch nearby players: {ex.Message}");
            return new List<LeaderboardEntry> { CreateLocalPlayerEntry() };
        }
        finally
        {
            _isFetching = false;
        }
    }

    /// <summary>
    /// Refresh all leaderboard data.
    /// </summary>
    public async Task RefreshAsync()
    {
        await GetTopPlayersAsync(DefaultTopCount, forceRefresh: true);
        await GetPlayerRankAsync(forceRefresh: true);
    }

    /// <summary>
    /// Submit or update the player's score on the leaderboard.
    /// Called automatically when rating changes.
    /// </summary>
    public async Task SubmitScoreAsync(int rating)
    {
        var nakama = NakamaGameClient.Instance;
        if (nakama == null || nakama.Client == null)
        {
            GD.Print("[LeaderboardService] Cannot submit score: not connected");
            return;
        }

        if (!nakama.IsAuthenticated || nakama.Session == null)
        {
            bool ensured = await nakama.EnsureAuthenticatedAsync();
            if (!ensured || nakama.Session == null)
            {
                GD.Print("[LeaderboardService] Cannot submit score: not connected");
                return;
            }
        }

        try
        {
            await nakama.Client.WriteLeaderboardRecordAsync(
                nakama.Session,
                RankedLeaderboardId,
                rating
            );
            GD.Print($"[LeaderboardService] Submitted score: {rating}");

            // Invalidate cache
            _cacheTimestamp = 0;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LeaderboardService] Failed to submit score: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    private async Task<List<LeaderboardEntry>> FetchTopPlayersFromNakamaAsync(int count)
    {
        var nakama = NakamaGameClient.Instance;
        if (nakama == null || nakama.Client == null)
        {
            GD.Print("[LeaderboardService] Not connected to Nakama, using local data");
            return CreateMockLeaderboard();
        }

        if (!nakama.IsAuthenticated || nakama.Session == null)
        {
            bool ensured = await nakama.EnsureAuthenticatedAsync();
            if (!ensured || nakama.Session == null)
            {
                GD.Print("[LeaderboardService] Not connected to Nakama, using local data");
                return CreateMockLeaderboard();
            }
        }

        try
        {
            _isFetching = true;

            var result = await nakama.Client.ListLeaderboardRecordsAsync(
                nakama.Session,
                RankedLeaderboardId,
                ownerIds: null,
                limit: count
            );

            var entries = new List<LeaderboardEntry>();
            foreach (var record in result.Records)
            {
                int rank = int.Parse(record.Rank);
                int score = int.Parse(record.Score);
                entries.Add(
                    new LeaderboardEntry
                    {
                        Rank = rank,
                        UserId = record.OwnerId,
                        DisplayName = record.Username ?? "Unknown",
                        Rating = score,
                        Tier = EloCalculator.GetTier(score),
                        Division = EloCalculator.GetDivision(score),
                        IsCurrentPlayer = record.OwnerId == nakama.UserId,
                    }
                );
            }

            GD.Print($"[LeaderboardService] Fetched {entries.Count} leaderboard entries");
            return entries;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LeaderboardService] Failed to fetch leaderboard: {ex.Message}");
            EmitSignal(SignalName.LeaderboardFetchFailed, ex.Message);
            return CreateMockLeaderboard();
        }
        finally
        {
            _isFetching = false;
        }
    }

    private async Task<LeaderboardEntry?> FetchPlayerRankFromNakamaAsync(string userId)
    {
        var nakama = NakamaGameClient.Instance;
        if (nakama == null || nakama.Client == null)
        {
            return null;
        }

        if (!nakama.IsAuthenticated || nakama.Session == null)
        {
            bool ensured = await nakama.EnsureAuthenticatedAsync();
            if (!ensured || nakama.Session == null)
                return null;
        }

        try
        {
            var result = await nakama.Client.ListLeaderboardRecordsAroundOwnerAsync(
                nakama.Session,
                RankedLeaderboardId,
                userId,
                limit: 1
            );

            foreach (var record in result.Records)
            {
                if (record.OwnerId == userId)
                {
                    int rank = int.Parse(record.Rank);
                    int score = int.Parse(record.Score);
                    return new LeaderboardEntry
                    {
                        Rank = rank,
                        UserId = record.OwnerId,
                        DisplayName = record.Username ?? "Unknown",
                        Rating = score,
                        Tier = EloCalculator.GetTier(score),
                        Division = EloCalculator.GetDivision(score),
                        IsCurrentPlayer = true,
                    };
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LeaderboardService] Failed to fetch player rank: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Check if a player is in the top 20 on the leaderboard.
    /// Returns false if offline or player not found.
    /// </summary>
    public bool IsTopTwenty(string userId)
    {
        var position = GetPlayerPosition(userId);
        return position.HasValue && position.Value <= 20;
    }

    /// <summary>
    /// Get a player's position on the cached leaderboard.
    /// Returns null if player not found or no data available.
    /// </summary>
    public int? GetPlayerPosition(string userId)
    {
        foreach (var entry in _cachedTopPlayers)
        {
            if (entry.UserId == userId)
                return entry.Rank;
        }
        return null;
    }

    private LeaderboardEntry CreateLocalPlayerEntry()
    {
        var rankingService = GetNodeOrNull<RankingService>("/root/RankingService");
        int rating = rankingService?.GetRating() ?? EloCalculator.StartingElo;

        var nakama = NakamaGameClient.Instance;
        string displayName = nakama?.Username ?? "You";
        string ownerId = nakama?.UserId ?? "local";

        return new LeaderboardEntry
        {
            Rank = 0, // Unknown without server
            UserId = ownerId,
            DisplayName = displayName,
            Rating = rating,
            Tier = EloCalculator.GetTier(rating),
            Division = EloCalculator.GetDivision(rating),
            IsCurrentPlayer = true,
        };
    }

    /// <summary>
    /// Create mock leaderboard data for offline/development use.
    /// </summary>
    private List<LeaderboardEntry> CreateMockLeaderboard()
    {
        var entries = new List<LeaderboardEntry>();
        var random = new Random(42); // Fixed seed for consistent mock data

        string[] mockNames =
        {
            "PLACEHOLDER_PLAYER_1",
            "PLACEHOLDER_PLAYER_2",
            "PLACEHOLDER_PLAYER_3",
            "PLACEHOLDER_PLAYER_4",
            "PLACEHOLDER_PLAYER_5",
            "PLACEHOLDER_PLAYER_6",
            "PLACEHOLDER_PLAYER_7",
            "PLACEHOLDER_PLAYER_8",
            "PLACEHOLDER_PLAYER_9",
            "PLACEHOLDER_PLAYER_10",
        };

        for (int i = 0; i < 10; i++)
        {
            int rating = 1800 - (i * 80) + random.Next(-20, 20);
            entries.Add(
                new LeaderboardEntry
                {
                    Rank = i + 1,
                    UserId = $"mock_user_{i}",
                    DisplayName = mockNames[i],
                    Rating = rating,
                    Tier = EloCalculator.GetTier(rating),
                    Division = EloCalculator.GetDivision(rating),
                    IsCurrentPlayer = false,
                }
            );
        }

        // Add local player at position 5 if not in top 10
        var localEntry = CreateLocalPlayerEntry();
        localEntry.Rank = 5;
        entries.Insert(4, localEntry);

        // Re-rank
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].Rank = i + 1;
        }

        return entries;
    }

    #endregion
}

/// <summary>
/// A single entry on the leaderboard.
/// </summary>
public class LeaderboardEntry
{
    /// <summary>
    /// Player's rank on the leaderboard (1-based).
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Player's unique user ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// Player's display name.
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Player's ELO rating.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Player's rank tier.
    /// </summary>
    public RankTier Tier { get; set; }

    /// <summary>
    /// Player's division within their tier.
    /// </summary>
    public int Division { get; set; }

    /// <summary>
    /// Whether this is the current player.
    /// </summary>
    public bool IsCurrentPlayer { get; set; }

    /// <summary>
    /// Get formatted rank string (e.g., "Mage II").
    /// </summary>
    public string GetFormattedTier()
    {
        if (Tier == RankTier.Fateforged)
            return "Fateforged";

        string divisionStr = Division switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => "",
        };
        return $"{Tier} {divisionStr}".Trim();
    }
}
