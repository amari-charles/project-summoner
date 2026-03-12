namespace Fateforged.Data.Events;

/// <summary>
/// Reward type enum - categorizes battle reward types.
/// </summary>
public enum RewardType
{
    /// <summary>No card reward (shop events, etc.)</summary>
    None,

    /// <summary>Fixed reward - player receives specific predetermined cards</summary>
    Fixed,

    /// <summary>Flexible reward - player picks from options</summary>
    Flexible,
}

/// <summary>
/// Extension methods for RewardType enum.
/// </summary>
public static class RewardTypeExtensions
{
    /// <summary>Convert to string ID matching GDScript RewardTypeIDs</summary>
    public static string ToStringId(this RewardType type) =>
        type switch
        {
            RewardType.None => "none",
            RewardType.Fixed => "fixed",
            RewardType.Flexible => "flexible",
            _ => "fixed",
        };

    /// <summary>Parse from string ID</summary>
    public static RewardType FromStringId(string id) =>
        id switch
        {
            "none" => RewardType.None,
            "fixed" => RewardType.Fixed,
            "flexible" => RewardType.Flexible,
            _ => RewardType.Fixed,
        };

    /// <summary>Check if the reward type provides a card</summary>
    public static bool HasCardReward(this RewardType type) => type != RewardType.None;

    /// <summary>Check if the reward type requires player selection</summary>
    public static bool RequiresSelection(this RewardType type) => type == RewardType.Flexible;
}
