namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// Why a match ended.
/// </summary>
public enum MatchEndReason
{
    SummonerDestroyed,
    Forfeit,
    Disconnect,
    Timeout
}

/// <summary>
/// Input DTO — what callers provide when reporting a match.
/// </summary>
public class MatchResult
{
    public string MatchId { get; set; } = "";
    public string WinnerUserId { get; set; } = "";
    public string LoserUserId { get; set; } = "";
    public int OpponentRating { get; set; } = EloCalculator.StartingElo;
    public float DurationSeconds { get; set; }
    public string EndReason { get; set; } = "summoner_destroyed";
}

/// <summary>
/// Stored history entry from the local player's perspective.
/// </summary>
public class MatchRecord
{
    public string MatchId { get; set; } = "";
    public string OpponentId { get; set; } = "";
    public bool Won { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public int RatingChange { get; set; }
    public int OpponentRatingBefore { get; set; }
    public float DurationSeconds { get; set; }
    public string EndReason { get; set; } = "";
    public long Timestamp { get; set; }
}
