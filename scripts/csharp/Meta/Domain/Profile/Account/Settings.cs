using System.Text.Json.Serialization;

namespace Fateforged.Domain.Profile.Account;

/// <summary>
/// Player settings.
/// </summary>
public class Settings
{
    [JsonPropertyName("master_volume")]
    public float MasterVolume { get; set; } = 1.0f;

    /// <summary>Sound effects volume (0.0 - 1.0).</summary>
    [JsonPropertyName("sfx_volume")]
    public float SfxVolume { get; set; } = 1.0f;

    /// <summary>Music volume (0.0 - 1.0).</summary>
    [JsonPropertyName("music_volume")]
    public float MusicVolume { get; set; } = 1.0f;

    [JsonPropertyName("mute_when_unfocused")]
    public bool MuteWhenUnfocused { get; set; } = false;

    [JsonPropertyName("window_mode")]
    public string WindowMode { get; set; } = "fullscreen";

    [JsonPropertyName("resolution_width")]
    public int ResolutionWidth { get; set; } = 1920;

    [JsonPropertyName("resolution_height")]
    public int ResolutionHeight { get; set; } = 1080;

    [JsonPropertyName("vsync_enabled")]
    public bool VsyncEnabled { get; set; } = true;

    [JsonPropertyName("fps_limit")]
    public int FpsLimit { get; set; } = 60;

    [JsonPropertyName("edge_pan_enabled")]
    public bool EdgePanEnabled { get; set; } = true;

    [JsonPropertyName("camera_speed")]
    public float CameraSpeed { get; set; } = 1.0f;

    [JsonPropertyName("reduce_camera_motion")]
    public bool ReduceCameraMotion { get; set; } = false;

    [JsonPropertyName("ui_scale")]
    public float UiScale { get; set; } = 1.0f;

    /// <summary>Language code (e.g., "en", "es", "fr").</summary>
    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "en";
}
