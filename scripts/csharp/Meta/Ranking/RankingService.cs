using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Nakama;
using Fateforged.Multiplayer.Backend;

namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// Unified service for managing player rankings, match history, and match reporting.
/// Persists ratings through ProfileRepo. Reports to Nakama when online, caches offline.
/// </summary>
public partial class RankingService : Node
{
    #region Configuration

    private const int MaxMatchHistorySize = 50;
    private const string MatchReportRpc = "report_match";
    private const int MaxOfflineCache = 50;
    private const string OfflineCachePath = "user://match_reports_cache.json";

    #endregion

    #region Signals

    [Signal]
    public delegate void MatchReportedEventHandler(string matchId, int ratingChange);

    [Signal]
    public delegate void MatchReportFailedEventHandler(string matchId, string error);

    [Signal]
    public delegate void RatingChangedEventHandler(int oldRating, int newRating, int change);

    #endregion

    #region State

    private Node? _profileRepo;
    private int? _cachedRating;
    private readonly List<MatchRecord> _offlineReportCache = new();

    public int PendingReportCount => _offlineReportCache.Count;

    #endregion

    public override void _Ready()
    {
        _profileRepo = GetNodeOrNull("/root/ProfileRepo");
        if (_profileRepo == null)
        {
            GD.PrintErr("[RankingService] ProfileRepo not found");
        }

        LoadOfflineCache();
    }

    #region Rating Management

    public int GetRating()
    {
        if (_cachedRating.HasValue)
            return _cachedRating.Value;

        var rating = LoadRatingFromProfile();
        _cachedRating = rating;
        return rating;
    }

    public void SetRating(int rating)
    {
        rating = Math.Clamp(rating, EloCalculator.EloFloor, EloCalculator.EloCeiling);
        _cachedRating = rating;
        SaveRatingToProfile(rating);
    }

    public RankTier GetTier()
    {
        return EloCalculator.GetTier(GetRating());
    }

    public int GetDivision()
    {
        return EloCalculator.GetDivision(GetRating());
    }

    public string GetFormattedRank()
    {
        return EloCalculator.FormatRating(GetRating());
    }

    public string GetTierName()
    {
        return GetTier().ToString();
    }

    public string GetTierNameForRating(int rating)
    {
        return EloCalculator.GetTier(rating).ToString();
    }

    public int GetDivisionForRating(int rating)
    {
        return EloCalculator.GetDivision(rating);
    }

    #endregion

    #region Match Recording

    /// <summary>
    /// Report a match result: update ELO, record history, fire signals,
    /// and fire-and-forget Nakama submission.
    /// </summary>
    public int ReportMatch(bool won, int opponentRating, string matchId,
                           string opponentId, float durationSeconds,
                           MatchEndReason endReason)
    {
        var currentRating = GetRating();

        int newRating;
        if (won)
        {
            var (winnerNew, _) = EloCalculator.CalculateNewRatings(currentRating, opponentRating);
            newRating = winnerNew;
        }
        else
        {
            var (_, loserNew) = EloCalculator.CalculateNewRatings(opponentRating, currentRating);
            newRating = loserNew;
        }

        var ratingChange = newRating - currentRating;
        SetRating(newRating);

        var record = new MatchRecord
        {
            MatchId = matchId,
            OpponentId = opponentId,
            Won = won,
            RatingBefore = currentRating,
            RatingAfter = newRating,
            RatingChange = ratingChange,
            OpponentRatingBefore = opponentRating,
            DurationSeconds = durationSeconds,
            EndReason = endReason,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        AddToMatchHistory(record);

        EmitSignal(SignalName.RatingChanged, currentRating, newRating, ratingChange);
        EmitSignal(SignalName.MatchReported, matchId, ratingChange);

        GD.Print($"[RANKED][REPORT] Match recorded: {(won ? "WIN" : "LOSS")} vs {opponentId}, " +
                 $"rating {currentRating} → {newRating} ({(ratingChange >= 0 ? "+" : "")}{ratingChange})");

        _ = SubmitOrCacheAsync(record);
        return ratingChange;
    }

    public List<MatchRecord> GetRecentMatches(int count = 10)
    {
        return LoadMatchHistoryFromProfile(count);
    }

    #endregion

    #region Match Reporting

    private async Task SubmitOrCacheAsync(MatchRecord record)
    {
        bool submitted = await SubmitReportToNakamaAsync(record);
        if (submitted)
        {
            GD.Print($"[RANKED][REPORT] Match submitted to Nakama: {record.MatchId}");
        }
        else
        {
            CacheOfflineReport(record);
            GD.Print($"[RANKED][REPORT] Match cached for later submission: {record.MatchId}");
        }
    }

    /// <summary>
    /// Try to submit any cached offline reports.
    /// </summary>
    public async Task FlushOfflineCacheAsync()
    {
        if (_offlineReportCache.Count == 0) return;

        GD.Print($"[RankingService] Flushing {_offlineReportCache.Count} cached reports");

        var toSubmit = new List<MatchRecord>(_offlineReportCache);
        _offlineReportCache.Clear();

        foreach (var report in toSubmit)
        {
            bool submitted = await SubmitReportToNakamaAsync(report);
            if (!submitted)
            {
                _offlineReportCache.Add(report);
            }
        }

        SaveOfflineCache();
    }

    private async Task<bool> SubmitReportToNakamaAsync(MatchRecord record)
    {
        var nakama = NakamaGameClient.Instance;
        if (nakama == null || nakama.Client == null)
        {
            GD.Print("[RANKED][REPORT] Cannot submit: not connected to Nakama");
            return false;
        }

        if (!nakama.IsAuthenticated || nakama.Session == null)
        {
            bool ensured = await nakama.EnsureAuthenticatedAsync();
            if (!ensured || nakama.Session == null)
            {
                GD.Print("[RANKED][REPORT] Cannot submit: not connected to Nakama");
                return false;
            }
        }

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(record);
            var response = await nakama.Client.RpcAsync(nakama.Session, MatchReportRpc, payload);
            GD.Print($"[RANKED][REPORT] Server response: {response.Payload}");
            return true;
        }
        catch (ApiResponseException ex)
        {
            GD.Print($"[RANKED][REPORT] Server RPC not available: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RANKED][REPORT] Failed to submit report: {ex.Message}");
            EmitSignal(SignalName.MatchReportFailed, record.MatchId, ex.Message);
            return false;
        }
    }

    private void CacheOfflineReport(MatchRecord record)
    {
        _offlineReportCache.Add(record);

        while (_offlineReportCache.Count > MaxOfflineCache)
        {
            _offlineReportCache.RemoveAt(0);
        }

        SaveOfflineCache();
    }

    #endregion

    #region Statistics

    public int GetTotalMatches()
    {
        return GetStatistic("total_matches", 0);
    }

    public int GetWins()
    {
        return GetStatistic("wins", 0);
    }

    public int GetLosses()
    {
        return GetStatistic("losses", 0);
    }

    /// <summary>
    /// Get win rate as a 0.0-1.0 ratio.
    /// </summary>
    public float GetWinRate()
    {
        var total = GetTotalMatches();
        if (total == 0) return 0;
        return (float)GetWins() / total;
    }

    public int GetPeakRating()
    {
        return GetStatistic("peak_rating", EloCalculator.StartingElo);
    }

    public int GetWinStreak()
    {
        return GetStatistic("win_streak", 0);
    }

    #endregion

    #region Persistence

    private int LoadRatingFromProfile()
    {
        if (_profileRepo == null) return EloCalculator.StartingElo;

        try
        {
            var profile = _profileRepo.Call("GetActiveProfileDict").AsGodotDictionary();
            if (profile == null || !profile.ContainsKey("ranked"))
                return EloCalculator.StartingElo;

            var ranked = profile["ranked"].AsGodotDictionary();
            if (ranked != null && ranked.ContainsKey("rating"))
                return ranked["rating"].AsInt32();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to load rating: {ex.Message}");
        }

        return EloCalculator.StartingElo;
    }

    private void SaveRatingToProfile(int rating)
    {
        if (_profileRepo == null) return;

        try
        {
            var profile = _profileRepo.Call("GetActiveProfileDict").AsGodotDictionary();
            if (profile == null) return;

            Godot.Collections.Dictionary ranked;
            if (profile.ContainsKey("ranked"))
            {
                ranked = profile["ranked"].AsGodotDictionary();
            }
            else
            {
                ranked = new Godot.Collections.Dictionary();
                profile["ranked"] = ranked;
            }

            ranked["rating"] = rating;

            var peakRating = ranked.ContainsKey("peak_rating") ? ranked["peak_rating"].AsInt32() : 0;
            if (rating > peakRating)
            {
                ranked["peak_rating"] = rating;
            }

            _profileRepo.Call("SaveProfile", true);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to save rating: {ex.Message}");
        }
    }

    private void AddToMatchHistory(MatchRecord entry)
    {
        if (_profileRepo == null) return;

        try
        {
            var profile = _profileRepo.Call("GetActiveProfileDict").AsGodotDictionary();
            if (profile == null) return;

            Godot.Collections.Dictionary ranked;
            if (profile.ContainsKey("ranked"))
            {
                ranked = profile["ranked"].AsGodotDictionary();
            }
            else
            {
                ranked = new Godot.Collections.Dictionary();
                profile["ranked"] = ranked;
            }

            Godot.Collections.Array history;
            if (ranked.ContainsKey("match_history"))
            {
                history = ranked["match_history"].AsGodotArray();
            }
            else
            {
                history = new Godot.Collections.Array();
                ranked["match_history"] = history;
            }

            var entryDict = new Godot.Collections.Dictionary
            {
                ["match_id"] = entry.MatchId,
                ["opponent_id"] = entry.OpponentId,
                ["won"] = entry.Won,
                ["rating_before"] = entry.RatingBefore,
                ["rating_after"] = entry.RatingAfter,
                ["rating_change"] = entry.RatingChange,
                ["opponent_rating_before"] = entry.OpponentRatingBefore,
                ["duration_seconds"] = entry.DurationSeconds,
                ["end_reason"] = entry.EndReason.ToString(),
                ["timestamp"] = entry.Timestamp
            };
            history.Insert(0, entryDict);

            while (history.Count > MaxMatchHistorySize)
            {
                history.RemoveAt(history.Count - 1);
            }

            // Update statistics
            var totalMatches = ranked.ContainsKey("total_matches") ? ranked["total_matches"].AsInt32() : 0;
            var wins = ranked.ContainsKey("wins") ? ranked["wins"].AsInt32() : 0;
            var losses = ranked.ContainsKey("losses") ? ranked["losses"].AsInt32() : 0;
            var winStreak = ranked.ContainsKey("win_streak") ? ranked["win_streak"].AsInt32() : 0;

            ranked["total_matches"] = totalMatches + 1;
            if (entry.Won)
            {
                ranked["wins"] = wins + 1;
                ranked["win_streak"] = winStreak + 1;
            }
            else
            {
                ranked["losses"] = losses + 1;
                ranked["win_streak"] = 0;
            }

            _profileRepo.Call("SaveProfile", true);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to add match history: {ex.Message}");
        }
    }

    private List<MatchRecord> LoadMatchHistoryFromProfile(int count)
    {
        var result = new List<MatchRecord>();
        if (_profileRepo == null) return result;

        try
        {
            var profile = _profileRepo.Call("GetActiveProfileDict").AsGodotDictionary();
            if (profile == null || !profile.ContainsKey("ranked")) return result;

            var ranked = profile["ranked"].AsGodotDictionary();
            if (ranked == null || !ranked.ContainsKey("match_history")) return result;

            var history = ranked["match_history"].AsGodotArray();
            for (int i = 0; i < Math.Min(count, history.Count); i++)
            {
                var entryDict = history[i].AsGodotDictionary();
                result.Add(new MatchRecord
                {
                    MatchId = entryDict.ContainsKey("match_id") ? entryDict["match_id"].AsString() : "",
                    OpponentId = entryDict.ContainsKey("opponent_id") ? entryDict["opponent_id"].AsString() : "",
                    Won = entryDict.ContainsKey("won") && entryDict["won"].AsBool(),
                    RatingBefore = entryDict.ContainsKey("rating_before") ? entryDict["rating_before"].AsInt32() : 0,
                    RatingAfter = entryDict.ContainsKey("rating_after") ? entryDict["rating_after"].AsInt32() : 0,
                    RatingChange = entryDict.ContainsKey("rating_change") ? entryDict["rating_change"].AsInt32() : 0,
                    OpponentRatingBefore = entryDict.ContainsKey("opponent_rating_before") ? entryDict["opponent_rating_before"].AsInt32() : 0,
                    DurationSeconds = entryDict.ContainsKey("duration_seconds") ? entryDict["duration_seconds"].AsSingle() : 0f,
                    EndReason = entryDict.ContainsKey("end_reason")
                        && Enum.TryParse<MatchEndReason>(entryDict["end_reason"].AsString(), out var parsedReason)
                        ? parsedReason : MatchEndReason.SummonerDestroyed,
                    Timestamp = entryDict.ContainsKey("timestamp") ? entryDict["timestamp"].AsInt64() : 0L
                });
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to load match history: {ex.Message}");
        }

        return result;
    }

    private int GetStatistic(string key, int defaultValue)
    {
        if (_profileRepo == null) return defaultValue;

        try
        {
            var profile = _profileRepo.Call("GetActiveProfileDict").AsGodotDictionary();
            if (profile == null || !profile.ContainsKey("ranked")) return defaultValue;

            var ranked = profile["ranked"].AsGodotDictionary();
            if (ranked == null || !ranked.ContainsKey(key)) return defaultValue;

            return ranked[key].AsInt32();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to get statistic '{key}': {ex.Message}");
            return defaultValue;
        }
    }

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
            GD.PrintErr($"[RankingService] Failed to save offline cache: {ex.Message}");
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
            var reports = System.Text.Json.JsonSerializer.Deserialize<List<MatchRecord>>(json);
            if (reports != null)
            {
                _offlineReportCache.AddRange(reports);
                GD.Print($"[RankingService] Loaded {reports.Count} cached reports");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to load offline cache: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Reset all ranking data (for testing).
    /// </summary>
    public void ResetRankingData()
    {
        _cachedRating = null;

        if (_profileRepo == null) return;

        try
        {
            var profile = _profileRepo.Call("GetActiveProfileDict").AsGodotDictionary();
            if (profile != null && profile.ContainsKey("ranked"))
            {
                profile.Remove("ranked");
                _profileRepo.Call("SaveProfile", true);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to reset ranking data: {ex.Message}");
        }

        GD.Print("[RankingService] Ranking data reset");
    }
}
