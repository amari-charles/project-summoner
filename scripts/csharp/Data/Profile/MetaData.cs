using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Miscellaneous profile metadata.
/// </summary>
public class MetaData
{
    /// <summary>Currently selected deck ID.</summary>
    [JsonPropertyName("selected_deck")]
    public string SelectedDeck { get; set; } = "";

    /// <summary>Currently selected summoner ID.</summary>
    [JsonPropertyName("selected_summoner")]
    public string SelectedSummoner { get; set; } = "";

    /// <summary>Tutorial completion flags.</summary>
    [JsonPropertyName("tutorial_flags")]
    public Dictionary<string, bool> TutorialFlags { get; set; } = [];

    /// <summary>Achievement progress.</summary>
    [JsonPropertyName("achievements")]
    public Dictionary<string, object> Achievements { get; set; } = [];

    /// <summary>Whether user has opted into analytics.</summary>
    [JsonPropertyName("analytics_opt_in")]
    public bool AnalyticsOptIn { get; set; }
}

/// <summary>
/// Last match data for replay/analytics.
/// </summary>
public class LastMatchData
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
