using System;
using System.Collections.Generic;
using Godot;

namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// Service for managing player rankings and match history.
/// Persists ratings through ProfileRepo.
/// </summary>
public partial class RankingService : Node
{
    #region Configuration

    /// <summary>
    /// Maximum number of match history entries to store.
    /// </summary>
    private const int MaxMatchHistorySize = 50;

    #endregion

    /// <summary>
    /// Profile repository for persistence.
    /// </summary>
    private Node? _profileRepo;

    /// <summary>
    /// Cached player rating (to avoid repeated lookups).
    /// </summary>
    private int? _cachedRating;

    public override void _Ready()
    {
        _profileRepo = GetNodeOrNull("/root/ProfileRepo");
        if (_profileRepo == null)
        {
            GD.PrintErr("[RankingService] ProfileRepo not found");
        }
    }

    #region Rating Management

    /// <summary>
    /// Get the current player's rating.
    /// </summary>
    public int GetRating()
    {
        if (_cachedRating.HasValue)
            return _cachedRating.Value;

        var rating = LoadRatingFromProfile();
        _cachedRating = rating;
        return rating;
    }

    /// <summary>
    /// Set the player's rating.
    /// </summary>
    public void SetRating(int rating)
    {
        rating = Math.Clamp(rating, EloCalculator.EloFloor, EloCalculator.EloCeiling);
        _cachedRating = rating;
        SaveRatingToProfile(rating);
    }

    /// <summary>
    /// Get the player's rank tier.
    /// </summary>
    public RankTier GetTier()
    {
        return EloCalculator.GetTier(GetRating());
    }

    /// <summary>
    /// Get the player's division within their tier.
    /// </summary>
    public int GetDivision()
    {
        return EloCalculator.GetDivision(GetRating());
    }

    /// <summary>
    /// Get formatted rank string (e.g., "Gold II (1150)").
    /// </summary>
    public string GetFormattedRank()
    {
        return EloCalculator.FormatRating(GetRating());
    }

    /// <summary>
    /// Get tier name as a string (for GDScript interop).
    /// </summary>
    public string GetTierName()
    {
        return GetTier().ToString();
    }

    /// <summary>
    /// Get tier name for a specific rating (for GDScript interop).
    /// </summary>
    public string GetTierNameForRating(int rating)
    {
        return EloCalculator.GetTier(rating).ToString();
    }

    /// <summary>
    /// Get division for a specific rating (for GDScript interop).
    /// </summary>
    public int GetDivisionForRating(int rating)
    {
        return EloCalculator.GetDivision(rating);
    }

    #endregion

    #region Match Recording

    /// <summary>
    /// Record a match result and update rating.
    /// </summary>
    /// <param name="won">Whether the local player won</param>
    /// <param name="opponentRating">Opponent's rating</param>
    /// <param name="matchId">Unique match identifier</param>
    /// <param name="opponentId">Opponent's player ID</param>
    /// <returns>Rating change (positive for gain, negative for loss)</returns>
    public int RecordMatch(bool won, int opponentRating, string matchId, string opponentId)
    {
        var currentRating = GetRating();

        // Calculate new ratings
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

        // Update rating
        SetRating(newRating);

        // Record in match history
        AddToMatchHistory(new MatchHistoryEntry
        {
            MatchId = matchId,
            OpponentId = opponentId,
            Won = won,
            RatingBefore = currentRating,
            RatingAfter = newRating,
            RatingChange = ratingChange,
            PlayedAt = DateTime.UtcNow
        });

        GD.Print($"[RankingService] Match recorded: {(won ? "WIN" : "LOSS")} vs {opponentId}, " +
                 $"rating {currentRating} → {newRating} ({(ratingChange >= 0 ? "+" : "")}{ratingChange})");

        return ratingChange;
    }

    /// <summary>
    /// Get recent match history.
    /// </summary>
    public List<MatchHistoryEntry> GetRecentMatches(int count = 10)
    {
        return LoadMatchHistoryFromProfile(count);
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get total matches played.
    /// </summary>
    public int GetTotalMatches()
    {
        return GetStatistic("total_matches", 0);
    }

    /// <summary>
    /// Get total wins.
    /// </summary>
    public int GetWins()
    {
        return GetStatistic("wins", 0);
    }

    /// <summary>
    /// Get total losses.
    /// </summary>
    public int GetLosses()
    {
        return GetStatistic("losses", 0);
    }

    /// <summary>
    /// Get win rate as percentage (0-100).
    /// </summary>
    public float GetWinRate()
    {
        var total = GetTotalMatches();
        if (total == 0) return 0;
        return (float)GetWins() / total * 100f;
    }

    /// <summary>
    /// Get highest rating achieved.
    /// </summary>
    public int GetPeakRating()
    {
        return GetStatistic("peak_rating", EloCalculator.StartingElo);
    }

    /// <summary>
    /// Get current win streak (0 if on loss streak).
    /// </summary>
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
            var profile = _profileRepo.Call("get_active_profile").AsGodotDictionary();
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
            var profile = _profileRepo.Call("get_active_profile").AsGodotDictionary();
            if (profile == null) return;

            // Ensure ranked section exists
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

            // Update peak rating if needed
            var peakRating = ranked.ContainsKey("peak_rating") ? ranked["peak_rating"].AsInt32() : 0;
            if (rating > peakRating)
            {
                ranked["peak_rating"] = rating;
            }

            _profileRepo.Call("save_profile", true);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to save rating: {ex.Message}");
        }
    }

    private void AddToMatchHistory(MatchHistoryEntry entry)
    {
        if (_profileRepo == null) return;

        try
        {
            var profile = _profileRepo.Call("get_active_profile").AsGodotDictionary();
            if (profile == null) return;

            // Ensure ranked section exists
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

            // Get or create match history array
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

            // Add new entry at beginning
            var entryDict = new Godot.Collections.Dictionary
            {
                ["match_id"] = entry.MatchId,
                ["opponent_id"] = entry.OpponentId,
                ["won"] = entry.Won,
                ["rating_before"] = entry.RatingBefore,
                ["rating_after"] = entry.RatingAfter,
                ["rating_change"] = entry.RatingChange,
                ["played_at"] = entry.PlayedAt.ToString("o")
            };
            history.Insert(0, entryDict);

            // Keep only recent matches
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

            _profileRepo.Call("save_profile", true);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to add match history: {ex.Message}");
        }
    }

    private List<MatchHistoryEntry> LoadMatchHistoryFromProfile(int count)
    {
        var result = new List<MatchHistoryEntry>();
        if (_profileRepo == null) return result;

        try
        {
            var profile = _profileRepo.Call("get_active_profile").AsGodotDictionary();
            if (profile == null || !profile.ContainsKey("ranked")) return result;

            var ranked = profile["ranked"].AsGodotDictionary();
            if (ranked == null || !ranked.ContainsKey("match_history")) return result;

            var history = ranked["match_history"].AsGodotArray();
            for (int i = 0; i < Math.Min(count, history.Count); i++)
            {
                var entryDict = history[i].AsGodotDictionary();
                result.Add(new MatchHistoryEntry
                {
                    MatchId = entryDict["match_id"].AsString(),
                    OpponentId = entryDict["opponent_id"].AsString(),
                    Won = entryDict["won"].AsBool(),
                    RatingBefore = entryDict["rating_before"].AsInt32(),
                    RatingAfter = entryDict["rating_after"].AsInt32(),
                    RatingChange = entryDict["rating_change"].AsInt32(),
                    PlayedAt = DateTime.Parse(entryDict["played_at"].AsString())
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
            var profile = _profileRepo.Call("get_active_profile").AsGodotDictionary();
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
            var profile = _profileRepo.Call("get_active_profile").AsGodotDictionary();
            if (profile != null && profile.ContainsKey("ranked"))
            {
                profile.Remove("ranked");
                _profileRepo.Call("save_profile", true);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RankingService] Failed to reset ranking data: {ex.Message}");
        }

        GD.Print("[RankingService] Ranking data reset");
    }
}

/// <summary>
/// Represents a match history entry.
/// </summary>
public class MatchHistoryEntry
{
    public string MatchId { get; set; } = "";
    public string OpponentId { get; set; } = "";
    public bool Won { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public int RatingChange { get; set; }
    public DateTime PlayedAt { get; set; }
}
