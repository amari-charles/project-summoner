using System.Text.Json.Serialization;
using Fateforged.Domain.Profile;

namespace Fateforged.Domain.Profile.Account;

/// <summary>
/// Player resource/currency data.
/// </summary>
public class Resources
{
    /// <summary>Gold - earned through gameplay, used for in-game purchases.</summary>
    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    /// <summary>Gems - premium currency (purchased with real money).</summary>
    [JsonPropertyName("gems")]
    public int Gems { get; set; }

    /// <summary>Essence - used for card upgrades.</summary>
    [JsonPropertyName("essence")]
    public int Essence { get; set; }

    /// <summary>Fragments - collectible currency.</summary>
    [JsonPropertyName("fragments")]
    public int Fragments { get; set; }

    /// <summary>Profile ID reference.</summary>
    [JsonPropertyName("profile_id")]
    public ProfileId ProfileId { get; set; } = ProfileId.None;

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }
}
