using System.Text.Json.Serialization;

namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// Why a match ended.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MatchEndReason
{
    SummonerDestroyed,
    Forfeit,
    Disconnect,
    Timeout,
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
    public MatchEndReason EndReason { get; set; } = MatchEndReason.SummonerDestroyed;
    public long Timestamp { get; set; }
}
