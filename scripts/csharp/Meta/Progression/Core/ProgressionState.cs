namespace Fateforged.Meta.Progression.Core;

/// <summary>
/// Canonical progression state used by shared progression math.
/// XP is stored as banked XP toward the next level cost.
/// </summary>
public readonly record struct ProgressionState(int Level, int XpTowardNext, int MaxLevel)
{
    public bool IsAtMaxLevel => Level >= MaxLevel;

    public bool IsValid =>
        MaxLevel >= 1 &&
        Level >= 1 &&
        Level <= MaxLevel &&
        XpTowardNext >= 0;
}
