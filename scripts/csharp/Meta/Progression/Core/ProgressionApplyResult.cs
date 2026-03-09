namespace Fateforged.Meta.Progression.Core;

public enum ProgressionApplyStatus
{
    Success = 0,
    InvalidState,
    MaxLevel,
    InvalidCost,
    InsufficientXp
}

/// <summary>
/// Deterministic output envelope for progression level-up application.
/// </summary>
public readonly record struct ProgressionApplyResult(
    ProgressionApplyStatus Status,
    ProgressionState PreviousState,
    ProgressionState NextState,
    int XpCostSpent)
{
    public bool Success => Status == ProgressionApplyStatus.Success;

    public static ProgressionApplyResult NoChange(ProgressionApplyStatus status, ProgressionState state) =>
        new(status, state, state, 0);
}
