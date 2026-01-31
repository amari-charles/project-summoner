using System.Collections.Generic;

namespace ProjectSummoner.Data.Events;

/// <summary>
/// Configuration for battle rewards.
/// </summary>
public class BattleRewardConfig
{
    /// <summary>Type of reward (none, fixed, flexible)</summary>
    public RewardType Type { get; set; } = RewardType.None;

    /// <summary>Gold reward amount</summary>
    public int GoldReward { get; set; }

    /// <summary>Card XP reward amount</summary>
    public int CardXpReward { get; set; }

    /// <summary>Summoner XP reward amount</summary>
    public int SummonerXpReward { get; set; }

    // =========================================================================
    // FIXED REWARD OPTIONS
    // =========================================================================

    /// <summary>Fixed reward cards (for RewardType.Fixed)</summary>
    public List<FixedRewardEntry> FixedCards { get; set; } = new();

    // =========================================================================
    // FLEXIBLE REWARD OPTIONS
    // =========================================================================

    /// <summary>Card options for flexible rewards (player picks one)</summary>
    public List<string> CardOptions { get; set; } = new();

    /// <summary>Whether player selects from options (false = auto-grant first)</summary>
    public bool PlayerSelects { get; set; } = true;

    /// <summary>Number of cards to draw from pool (if using pool-based generation)</summary>
    public int DrawCount { get; set; } = 3;

    /// <summary>Exclude cards player already owns</summary>
    public bool ExcludeOwned { get; set; }

    /// <summary>Ensure no duplicate cards in options</summary>
    public bool UniqueOptions { get; set; } = true;
}
