namespace Fateforged.Meta.Progression.Core;

/// <summary>
/// Domain-specific curve policy for progression level-up costs.
/// </summary>
public interface IProgressionCurve
{
    int GetXpCostForNextLevel(int currentLevel, int maxLevel);
}
