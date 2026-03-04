using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Nakama;
using Fateforged.Multiplayer.Backend;

namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// Reports match outcomes to Nakama and updates local ratings.
/// Handles both online reporting and offline caching.
/// </summary>
public partial class MatchReporter : Node
{
    #region Configuration

    /// <summary>
    /// Nakama RPC function name for match reporting.
    /// </summary>
    private const string MatchReportRpc = "report_match";

    /// <summary>
    /// Storage collection for match history.
    /// </summary>
    private const string MatchHistoryCollection = "match_history";

    /// <summary>
    /// Maximum number of match reports to cache offline.
    /// </summary>
    private const int MaxOfflineCache = 50;

    /// <summary>
    /// Maximum number of match history entries to retain locally.
    /// </summary>
    private const int MaxMatchHistorySize = 100;

    #endregion

    #region State

    private readonly List<MatchReport> _offlineReportCache = new();
    private readonly List<MatchReport> _matchHistory = new();

    /// <summary>
    /// Number of pending offline reports.
    /// </summary>
    public int PendingReportCount => _offlineReportCache.Count;

    /// <summary>
    /// Recent match history.
    /// </summary>
    public IReadOnlyList<MatchReport> MatchHistory => _matchHistory.AsReadOnly();

    #endregion

    #region Signals

    /// <summary>
    /// Emitted when a match report is submitted successfully.
    /// </summary>
    [Signal]
    public delegate void MatchReportedEventHandler(string matchId, int ratingChange);

    /// <summary>
    /// Emitted when match reporting fails.
    /// </summary>
    [Signal]
    public delegate void MatchReportFailedEventHandler(string matchId, string error);

    /// <summary>
    /// Emitted when rating changes after a match.
    /// </summary>
    [Signal]
    public delegate void RatingChangedEventHandler(int oldRating, int newRating, int change);

    #endregion

    #region Singleton

    /// <summary>
    /// The global MatchReporter instance.
    /// </summary>
    public static MatchReporter? Instance { get; private set; }

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
        // Load any cached offline reports and match history
        LoadOfflineCache();
        LoadMatchHistory();

        GD.Print("[MatchReporter] Initialized");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Report a match result. Updates ratings and submits to Nakama.
    /// </summary>
    /// <param name="result">The match result to report</param>
    /// <returns>True if report was processed successfully</returns>
    public async Task<bool> ReportMatchAsync(MatchResult result)
    {
        GD.Print($"[MatchReporter] Reporting match: {result.MatchId}, winner: {result.WinnerUserId}");

        // Get current ratings
        var rankingService = GetNodeOrNull<RankingService>("RankingService");
        int localRating = rankingService?.GetRating() ?? EloCalculator.StartingElo;

        // Determine if local player won or lost
        var nakama = NakamaGameClient.Instance;
        bool isLocalWinner = nakama?.UserId == result.WinnerUserId;

        // Calculate new ratings
        int winnerOldRating = isLocalWinner ? localRating : result.OpponentRating;
        int loserOldRating = isLocalWinner ? result.OpponentRating : localRating;

        var (winnerNewRating, loserNewRating) = EloCalculator.CalculateNewRatings(winnerOldRating, loserOldRating);

        int localNewRating = isLocalWinner ? winnerNewRating : loserNewRating;
        int ratingChange = localNewRating - localRating;

        // Create match report
        var report = new MatchReport
        {
            MatchId = result.MatchId,
            WinnerId = result.WinnerUserId,
            LoserId = result.LoserUserId,
            WinnerRatingBefore = winnerOldRating,
            LoserRatingBefore = loserOldRating,
            WinnerRatingAfter = winnerNewRating,
            LoserRatingAfter = loserNewRating,
            DurationSeconds = result.DurationSeconds,
            EndReason = result.EndReason,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            LocalPlayerWon = isLocalWinner
        };

        // Update local rating immediately
        if (rankingService != null)
        {
            int oldRating = rankingService.GetRating();
            rankingService.SetRating(localNewRating);
            EmitSignal(SignalName.RatingChanged, oldRating, localNewRating, ratingChange);
        }

        // Add to match history
        AddToHistory(report);

        // Try to submit to Nakama
        bool submitted = await SubmitReportToNakamaAsync(report);

        if (submitted)
        {
            GD.Print($"[MatchReporter] Match reported successfully. Rating change: {ratingChange:+#;-#;0}");
            EmitSignal(SignalName.MatchReported, result.MatchId, ratingChange);
        }
        else
        {
            // Cache for later submission
            CacheOfflineReport(report);
            GD.Print($"[MatchReporter] Match cached for later submission. Rating change: {ratingChange:+#;-#;0}");
            EmitSignal(SignalName.MatchReported, result.MatchId, ratingChange);
        }

        return true;
    }

    /// <summary>
    /// GDScript-callable wrapper for ReportMatchAsync.
    /// Creates a MatchResult from individual parameters and reports it.
    /// </summary>
    public void ReportMatchFromGDScript(
        string matchId,
        string winnerUserId,
        string loserUserId,
        int opponentRating,
        float durationSeconds,
        string endReason)
    {
        var result = new MatchResult
        {
            MatchId = matchId,
            WinnerUserId = winnerUserId,
            LoserUserId = loserUserId,
            OpponentRating = opponentRating,
            DurationSeconds = durationSeconds,
            EndReason = endReason
        };

        // Fire and forget - the async method handles everything
        _ = ReportMatchAsync(result);
    }

    /// <summary>
    /// Try to submit any cached offline reports.
    /// Call this when connection is restored.
    /// </summary>
    public async Task FlushOfflineCacheAsync()
    {
        if (_offlineReportCache.Count == 0) return;

        GD.Print($"[MatchReporter] Flushing {_offlineReportCache.Count} cached reports");

        var toSubmit = new List<MatchReport>(_offlineReportCache);
        _offlineReportCache.Clear();

        foreach (var report in toSubmit)
        {
            bool submitted = await SubmitReportToNakamaAsync(report);
            if (!submitted)
            {
                // Re-cache failed submissions
                _offlineReportCache.Add(report);
            }
        }

        SaveOfflineCache();
    }

    /// <summary>
    /// Get match history for a specific time period.
    /// </summary>
    public List<MatchReport> GetRecentMatches(int count = 10)
    {
        int start = Math.Max(0, _matchHistory.Count - count);
        return _matchHistory.GetRange(start, Math.Min(count, _matchHistory.Count - start));
    }

    /// <summary>
    /// Calculate win rate from match history.
    /// </summary>
    public float GetWinRate()
    {
        if (_matchHistory.Count == 0) return 0f;

        int wins = 0;
        foreach (var match in _matchHistory)
        {
            if (match.LocalPlayerWon) wins++;
        }

        return (float)wins / _matchHistory.Count;
    }

    #endregion

    #region Private Methods

    private async Task<bool> SubmitReportToNakamaAsync(MatchReport report)
    {
        var nakama = NakamaGameClient.Instance;
        if (nakama == null || !nakama.IsAuthenticated || nakama.Client == null || nakama.Session == null)
        {
            GD.Print("[MatchReporter] Cannot submit: not connected to Nakama");
            return false;
        }

        try
        {
            // Serialize report to JSON
            var payload = System.Text.Json.JsonSerializer.Serialize(report);

            // Call server RPC to report match
            // Note: This requires a matching RPC function on the Nakama server
            var response = await nakama.Client.RpcAsync(nakama.Session, MatchReportRpc, payload);

            GD.Print($"[MatchReporter] Server response: {response.Payload}");
            return true;
        }
        catch (ApiResponseException ex)
        {
            // RPC not found or server error - expected during development
            GD.Print($"[MatchReporter] Server RPC not available: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchReporter] Failed to submit report: {ex.Message}");
            EmitSignal(SignalName.MatchReportFailed, report.MatchId, ex.Message);
            return false;
        }
    }

    private void AddToHistory(MatchReport report)
    {
        _matchHistory.Add(report);

        // Keep history bounded
        while (_matchHistory.Count > MaxMatchHistorySize)
        {
            _matchHistory.RemoveAt(0);
        }

        SaveMatchHistory();
    }

    private void CacheOfflineReport(MatchReport report)
    {
        _offlineReportCache.Add(report);

        // Keep cache bounded
        while (_offlineReportCache.Count > MaxOfflineCache)
        {
            _offlineReportCache.RemoveAt(0);
        }

        SaveOfflineCache();
    }

    #endregion

    #region Persistence

    private const string OfflineCachePath = "user://match_reports_cache.json";
    private const string MatchHistoryPath = "user://match_history.json";

    private void SaveOfflineCache()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_offlineReportCache);
            using var file = FileAccess.Open(OfflineCachePath, FileAccess.ModeFlags.Write);
            file?.StoreString(json);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchReporter] Failed to save offline cache: {ex.Message}");
        }
    }

    private void LoadOfflineCache()
    {
        if (!FileAccess.FileExists(OfflineCachePath)) return;

        try
        {
            using var file = FileAccess.Open(OfflineCachePath, FileAccess.ModeFlags.Read);
            if (file == null) return;

            var json = file.GetAsText();
            var reports = System.Text.Json.JsonSerializer.Deserialize<List<MatchReport>>(json);
            if (reports != null)
            {
                _offlineReportCache.AddRange(reports);
                GD.Print($"[MatchReporter] Loaded {reports.Count} cached reports");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchReporter] Failed to load offline cache: {ex.Message}");
        }
    }

    private void SaveMatchHistory()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_matchHistory);
            using var file = FileAccess.Open(MatchHistoryPath, FileAccess.ModeFlags.Write);
            file?.StoreString(json);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchReporter] Failed to save match history: {ex.Message}");
        }
    }

    private void LoadMatchHistory()
    {
        if (!FileAccess.FileExists(MatchHistoryPath)) return;

        try
        {
            using var file = FileAccess.Open(MatchHistoryPath, FileAccess.ModeFlags.Read);
            if (file == null) return;

            var json = file.GetAsText();
            var history = System.Text.Json.JsonSerializer.Deserialize<List<MatchReport>>(json);
            if (history != null)
            {
                _matchHistory.AddRange(history);
                GD.Print($"[MatchReporter] Loaded {history.Count} match history entries");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchReporter] Failed to load match history: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Match result data passed to the reporter.
/// </summary>
public class MatchResult
{
    /// <summary>
    /// Unique match identifier.
    /// </summary>
    public string MatchId { get; set; } = "";

    /// <summary>
    /// Winner's user ID.
    /// </summary>
    public string WinnerUserId { get; set; } = "";

    /// <summary>
    /// Loser's user ID.
    /// </summary>
    public string LoserUserId { get; set; } = "";

    /// <summary>
    /// Opponent's rating at match start.
    /// </summary>
    public int OpponentRating { get; set; } = EloCalculator.StartingElo;

    /// <summary>
    /// Match duration in seconds.
    /// </summary>
    public float DurationSeconds { get; set; }

    /// <summary>
    /// Reason the match ended (summoner_destroyed, forfeit, disconnect).
    /// </summary>
    public string EndReason { get; set; } = "summoner_destroyed";
}

/// <summary>
/// Complete match report for storage and submission.
/// </summary>
public class MatchReport
{
    /// <summary>
    /// Unique match identifier.
    /// </summary>
    public string MatchId { get; set; } = "";

    /// <summary>
    /// Winner's user ID.
    /// </summary>
    public string WinnerId { get; set; } = "";

    /// <summary>
    /// Loser's user ID.
    /// </summary>
    public string LoserId { get; set; } = "";

    /// <summary>
    /// Winner's rating before the match.
    /// </summary>
    public int WinnerRatingBefore { get; set; }

    /// <summary>
    /// Loser's rating before the match.
    /// </summary>
    public int LoserRatingBefore { get; set; }

    /// <summary>
    /// Winner's rating after the match.
    /// </summary>
    public int WinnerRatingAfter { get; set; }

    /// <summary>
    /// Loser's rating after the match.
    /// </summary>
    public int LoserRatingAfter { get; set; }

    /// <summary>
    /// Match duration in seconds.
    /// </summary>
    public float DurationSeconds { get; set; }

    /// <summary>
    /// Reason the match ended.
    /// </summary>
    public string EndReason { get; set; } = "";

    /// <summary>
    /// Unix timestamp when the match ended.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Whether the local player won this match.
    /// </summary>
    public bool LocalPlayerWon { get; set; }
}
