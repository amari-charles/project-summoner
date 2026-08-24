using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fateforged.Domain.Profile.Account;

/// <summary>
/// Miscellaneous profile metadata.
/// </summary>
public class AccountMeta
{
    /// <summary>Currently selected deck ID.</summary>
    [JsonPropertyName("selected_deck")]
    public string SelectedDeck { get; set; } = "";

    /// <summary>Ranked deck selection keyed by summoner ID.</summary>
    [JsonPropertyName("ranked_decks_by_summoner")]
    public Dictionary<string, string> RankedDecksBySummoner { get; set; } = [];

    /// <summary>Currently selected summoner ID.</summary>
    [JsonPropertyName("selected_summoner")]
    public string SelectedSummoner { get; set; } = "";

    /// <summary>Tutorial completion flags.</summary>
    [JsonPropertyName("tutorial_flags")]
    public Dictionary<string, bool> TutorialFlags { get; set; } = [];

    [JsonPropertyName("narrative_flags")]
    public Dictionary<string, bool> NarrativeFlags { get; set; } = [];

    /// <summary>Achievement progress.</summary>
    [JsonPropertyName("achievements")]
    public Dictionary<string, object> Achievements { get; set; } = [];

    /// <summary>Whether user has opted into analytics.</summary>
    [JsonPropertyName("analytics_opt_in")]
    public bool AnalyticsOptIn { get; set; }
}
