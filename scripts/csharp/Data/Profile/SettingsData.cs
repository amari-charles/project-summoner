namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Player settings.
/// </summary>
public class SettingsData
{
    /// <summary>Sound effects volume (0.0 - 1.0).</summary>
    public float SfxVolume { get; set; } = 1.0f;

    /// <summary>Music volume (0.0 - 1.0).</summary>
    public float MusicVolume { get; set; } = 1.0f;

    /// <summary>Language code (e.g., "en", "es", "fr").</summary>
    public string Lang { get; set; } = "en";
}
