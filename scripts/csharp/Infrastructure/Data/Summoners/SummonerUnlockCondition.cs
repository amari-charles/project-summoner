namespace Fateforged.Data.Summoners;

/// <summary>
/// Conditions for unlocking summoners.
/// </summary>
public enum SummonerUnlockCondition
{
    /// <summary>Available as one of the 4 starting choices.</summary>
    StartingChoice,

    /// <summary>Only available through the "Random" starting option.</summary>
    RandomStarterOnly,

    /// <summary>Must be purchased from the Premium Store.</summary>
    PremiumPurchase,

    /// <summary>Developer/test only, not available to players.</summary>
    DevOnly,
}

/// <summary>
/// Extension methods for SummonerUnlockCondition.
/// </summary>
public static class SummonerUnlockConditionExtensions
{
    /// <summary>Convert unlock condition to GDScript-compatible string.</summary>
    public static string ToGdString(this SummonerUnlockCondition condition) =>
        condition switch
        {
            SummonerUnlockCondition.StartingChoice => "starting_choice",
            SummonerUnlockCondition.RandomStarterOnly => "random_starter_only",
            SummonerUnlockCondition.PremiumPurchase => "premium_purchase",
            SummonerUnlockCondition.DevOnly => "dev_only",
            _ => condition.ToString().ToLowerInvariant(),
        };

    /// <summary>Parse unlock condition from GDScript string.</summary>
    public static SummonerUnlockCondition FromGdString(string value) =>
        value switch
        {
            "starting_choice" => SummonerUnlockCondition.StartingChoice,
            "random_starter_only" => SummonerUnlockCondition.RandomStarterOnly,
            "premium_purchase" => SummonerUnlockCondition.PremiumPurchase,
            "dev_only" => SummonerUnlockCondition.DevOnly,
            _ => SummonerUnlockCondition.DevOnly,
        };
}
