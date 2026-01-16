using System.Collections.Generic;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Miscellaneous profile metadata.
/// </summary>
public class MetaData
{
    /// <summary>Currently selected deck ID.</summary>
    public string SelectedDeck { get; set; } = "";

    /// <summary>Tutorial completion flags.</summary>
    public Dictionary<string, bool> TutorialFlags { get; set; } = [];

    /// <summary>Achievement progress.</summary>
    public Dictionary<string, object> Achievements { get; set; } = [];

    /// <summary>Whether user has opted into analytics.</summary>
    public bool AnalyticsOptIn { get; set; }
}

/// <summary>
/// Last match data for replay/analytics.
/// </summary>
public class LastMatchData
{
    /// <summary>Random seed used for the match.</summary>
    public long? Seed { get; set; }

    /// <summary>Match result (win, loss, draw, etc.).</summary>
    public string? Result { get; set; }

    /// <summary>Match duration in seconds.</summary>
    public float? DurationSeconds { get; set; }
}
