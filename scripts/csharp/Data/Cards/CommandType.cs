namespace ProjectSummoner.Cards;

/// <summary>
/// Types of tactical command spells that affect unit behavior.
/// </summary>
public enum CommandType
{
    /// <summary>Units move to the target position.</summary>
    Rally,

    /// <summary>Units form defensive formation at the target position.</summary>
    Guard,

    /// <summary>Units focus fire on nearest enemy to the target position.</summary>
    Charge
}
