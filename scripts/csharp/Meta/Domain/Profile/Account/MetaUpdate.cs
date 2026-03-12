using System.Collections.Generic;

namespace Fateforged.Domain.Profile.Account;

/// <summary>
/// Typed DTO for updating profile meta fields.
/// Null values indicate "do not update this field".
/// </summary>
public class MetaUpdate
{
    /// <summary>Update the selected deck ID.</summary>
    public string? SelectedDeck { get; set; }

    /// <summary>Update the selected summoner ID.</summary>
    public string? SelectedSummoner { get; set; }

    /// <summary>Update the selected campaign ID.</summary>
    public string? SelectedCampaign { get; set; }

    /// <summary>Update the analytics opt-in flag.</summary>
    public bool? AnalyticsOptIn { get; set; }

    /// <summary>Merge tutorial flags (only specified keys are updated).</summary>
    public Dictionary<string, bool>? TutorialFlags { get; set; }

    /// <summary>Merge achievement progress (only specified keys are updated).</summary>
    public Dictionary<string, object>? Achievements { get; set; }
}
