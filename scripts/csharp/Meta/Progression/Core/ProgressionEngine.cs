using System;

namespace Fateforged.Meta.Progression.Core;

/// <summary>
/// Shared deterministic progression math for cards and summoners.
/// </summary>
public static class ProgressionEngine
{
    public static int GetXpCostForNextLevel(ProgressionState state, IProgressionCurve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);

        if (!state.IsValid || state.IsAtMaxLevel)
            return 0;

        return Math.Max(0, curve.GetXpCostForNextLevel(state.Level, state.MaxLevel));
    }

    public static bool CanLevelUp(ProgressionState state, IProgressionCurve curve)
    {
        var xpCost = GetXpCostForNextLevel(state, curve);
        return xpCost > 0 && state.XpTowardNext >= xpCost;
    }

    public static float GetProgress01(ProgressionState state, IProgressionCurve curve)
    {
        if (!state.IsValid)
            return 0f;

        if (state.IsAtMaxLevel)
            return 1f;

        var xpCost = GetXpCostForNextLevel(state, curve);
        if (xpCost <= 0)
            return 1f;

        return Math.Clamp((float)state.XpTowardNext / xpCost, 0f, 1f);
    }

    public static ProgressionApplyResult ApplyLevelUp(ProgressionState state, IProgressionCurve curve)
    {
        if (!state.IsValid)
            return ProgressionApplyResult.NoChange(ProgressionApplyStatus.InvalidState, state);

        if (state.IsAtMaxLevel)
            return ProgressionApplyResult.NoChange(ProgressionApplyStatus.MaxLevel, state);

        var xpCost = GetXpCostForNextLevel(state, curve);
        if (xpCost <= 0)
            return ProgressionApplyResult.NoChange(ProgressionApplyStatus.InvalidCost, state);

        if (state.XpTowardNext < xpCost)
            return ProgressionApplyResult.NoChange(ProgressionApplyStatus.InsufficientXp, state);

        var next = state with
        {
            Level = state.Level + 1,
            XpTowardNext = state.XpTowardNext - xpCost
        };

        return new ProgressionApplyResult(ProgressionApplyStatus.Success, state, next, xpCost);
    }
}
