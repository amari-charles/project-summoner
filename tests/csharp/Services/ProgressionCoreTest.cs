namespace Fateforged.Tests.Services;

using Fateforged.Meta.Progression.Core;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class ProgressionCoreTest
{
    private sealed class FixedCostCurve(int xpCost) : IProgressionCurve
    {
        public int GetXpCostForNextLevel(int currentLevel, int maxLevel)
        {
            _ = currentLevel;
            return currentLevel >= maxLevel ? 0 : xpCost;
        }
    }

    [TestCase]
    public void ApplyLevelUp_Success_ConsumesExactXpAndIncrementsLevel()
    {
        var state = new ProgressionState(Level: 2, XpTowardNext: 45, MaxLevel: 10);
        var curve = new FixedCostCurve(xpCost: 40);

        var result = ProgressionEngine.ApplyLevelUp(state, curve);

        AssertThat(result.Success).IsTrue();
        AssertThat(result.Status).IsEqual(ProgressionApplyStatus.Success);
        AssertThat(result.XpCostSpent).IsEqual(40);
        AssertThat(result.NextState.Level).IsEqual(3);
        AssertThat(result.NextState.XpTowardNext).IsEqual(5);
    }

    [TestCase]
    public void ApplyLevelUp_InsufficientXp_IsDeterministicNoOp()
    {
        var state = new ProgressionState(Level: 2, XpTowardNext: 39, MaxLevel: 10);
        var curve = new FixedCostCurve(xpCost: 40);

        var result = ProgressionEngine.ApplyLevelUp(state, curve);

        AssertThat(result.Success).IsFalse();
        AssertThat(result.Status).IsEqual(ProgressionApplyStatus.InsufficientXp);
        AssertThat(result.NextState).IsEqual(state);
        AssertThat(ProgressionEngine.CanLevelUp(state, curve)).IsFalse();
    }

    [TestCase]
    public void MaxLevel_ProgressAndCost_AreStableNoOp()
    {
        var state = new ProgressionState(Level: 10, XpTowardNext: 999, MaxLevel: 10);
        var curve = new FixedCostCurve(xpCost: 40);

        AssertThat(ProgressionEngine.GetXpCostForNextLevel(state, curve)).IsEqual(0);
        AssertThat(ProgressionEngine.GetProgress01(state, curve)).IsEqual(1f);
        AssertThat(ProgressionEngine.ApplyLevelUp(state, curve).Status)
            .IsEqual(ProgressionApplyStatus.MaxLevel);
    }

    [TestCase]
    public void InvalidState_ApplyLevelUp_ReturnsInvalidStateNoOp()
    {
        var state = new ProgressionState(Level: 0, XpTowardNext: -1, MaxLevel: 10);
        var curve = new FixedCostCurve(xpCost: 10);

        var result = ProgressionEngine.ApplyLevelUp(state, curve);

        AssertThat(result.Success).IsFalse();
        AssertThat(result.Status).IsEqual(ProgressionApplyStatus.InvalidState);
        AssertThat(result.NextState).IsEqual(state);
    }

    [TestCase]
    public void SameInputs_RepeatedEvaluation_IsDeterministic()
    {
        var state = new ProgressionState(Level: 3, XpTowardNext: 50, MaxLevel: 10);
        var curve = new FixedCostCurve(xpCost: 45);

        var resultA = ProgressionEngine.ApplyLevelUp(state, curve);
        var resultB = ProgressionEngine.ApplyLevelUp(state, curve);
        AssertThat(resultA).IsEqual(resultB);

        var progressA = ProgressionEngine.GetProgress01(state, curve);
        var progressB = ProgressionEngine.GetProgress01(state, curve);
        AssertThat(progressA).IsEqual(progressB);

        var canA = ProgressionEngine.CanLevelUp(state, curve);
        var canB = ProgressionEngine.CanLevelUp(state, curve);
        AssertThat(canA).IsEqual(canB);
    }
}
