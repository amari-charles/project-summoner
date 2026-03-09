namespace Fateforged.Tests.Services;

using Fateforged.Meta.Progression.Core;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class ProgressionCoreParityTest
{
    private sealed class EquivalentCurveA : IProgressionCurve
    {
        public int GetXpCostForNextLevel(int currentLevel, int maxLevel)
        {
            _ = maxLevel;
            return currentLevel <= 2 ? 30 : 45;
        }
    }

    private sealed class EquivalentCurveB : IProgressionCurve
    {
        public int GetXpCostForNextLevel(int currentLevel, int maxLevel)
        {
            _ = maxLevel;
            return currentLevel <= 2 ? 30 : 45;
        }
    }

    [TestCase]
    public void EquivalentCurves_WithSameState_ProduceIdenticalOutputs()
    {
        var state = new ProgressionState(Level: 2, XpTowardNext: 80, MaxLevel: 10);

        var costA = ProgressionEngine.GetXpCostForNextLevel(state, new EquivalentCurveA());
        var costB = ProgressionEngine.GetXpCostForNextLevel(state, new EquivalentCurveB());
        AssertThat(costA).IsEqual(costB);

        var progressA = ProgressionEngine.GetProgress01(state, new EquivalentCurveA());
        var progressB = ProgressionEngine.GetProgress01(state, new EquivalentCurveB());
        AssertThat(progressA).IsEqual(progressB);

        var applyA = ProgressionEngine.ApplyLevelUp(state, new EquivalentCurveA());
        var applyB = ProgressionEngine.ApplyLevelUp(state, new EquivalentCurveB());
        AssertThat(applyA).IsEqual(applyB);
    }
}
