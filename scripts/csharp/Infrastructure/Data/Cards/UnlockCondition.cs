namespace Fateforged.Cards;

/// <summary>
/// Conditions for unlocking cards.
/// </summary>
public enum UnlockCondition
{
    /// <summary>Available from the start (starter cards).</summary>
    Default,

    /// <summary>Only available in development/debug builds.</summary>
    DevOnly,
}
