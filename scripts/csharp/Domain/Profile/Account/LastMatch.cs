using System.Text.Json.Serialization;

namespace ProjectSummoner.Domain.Profile.Account;

/// <summary>
/// Last match data for replay/analytics.
/// </summary>
public class LastMatch
{
    /// <summary>Random seed used for the match.</summary>
    [JsonPropertyName("seed")]
    public long? Seed { get; set; }

    /// <summary>Match result (win, loss, draw, etc.).</summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>Match duration in seconds.</summary>
    [JsonPropertyName("duration_seconds")]
    public float? DurationSeconds { get; set; }
}
