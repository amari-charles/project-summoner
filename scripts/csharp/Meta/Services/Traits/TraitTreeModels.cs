using System.Collections.Generic;

namespace Fateforged.Meta.Services.Traits;

public enum TraitTreeNodeState
{
    Owned,
    Available,
    Locked
}

public static class TraitTreeNodeStateExtensions
{
    public static string ToStringValue(this TraitTreeNodeState state) => state switch
    {
        TraitTreeNodeState.Owned => "owned",
        TraitTreeNodeState.Available => "available",
        _ => "locked"
    };
}

public sealed class TraitTreeOwnerContext
{
    public required string OwnerTypeTag { get; init; }
    public required HashSet<string> EligibilityTags { get; init; }
    public required HashSet<string> OwnedTraitIds { get; init; }
    public required int CurrentLevel { get; init; }
    public required int UnspentTraitPoints { get; init; }
}

public sealed class TraitUnlockEvaluation
{
    public required bool IsOwned { get; init; }
    public required bool IsAcquirableTrait { get; init; }
    public required bool MatchesTags { get; init; }
    public required bool MeetsLevelRequirements { get; init; }
    public required bool MeetsPrerequisites { get; init; }
    public required bool HasTraitPoint { get; init; }
    public required bool IsEligibleWithoutPoints { get; init; }
    public required bool CanUnlockNow { get; init; }
    public required string LockedReason { get; init; }
    public required string UnlockBlockedReason { get; init; }
    public required List<string> MissingPrerequisiteIds { get; init; }

    public TraitTreeNodeState NodeState =>
        IsOwned
            ? TraitTreeNodeState.Owned
            : IsEligibleWithoutPoints
                ? TraitTreeNodeState.Available
                : TraitTreeNodeState.Locked;
}
