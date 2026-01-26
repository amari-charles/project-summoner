using System.Text.Json.Serialization;

namespace ProjectSummoner.Domain.Profile.Account;

/// <summary>
/// Player settings.
/// </summary>
public class Settings
{
    /// <summary>Sound effects volume (0.0 - 1.0).</summary>
    [JsonPropertyName("sfx_volume")]
    public float SfxVolume { get; set; } = 1.0f;

    /// <summary>Music volume (0.0 - 1.0).</summary>
    [JsonPropertyName("music_volume")]
    public float MusicVolume { get; set; } = 1.0f;

    /// <summary>Language code (e.g., "en", "es", "fr").</summary>
    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "en";
}
