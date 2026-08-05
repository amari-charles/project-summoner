using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Events;

namespace Fateforged.Meta.Campaign;

/// <summary>Pure source of truth for campaign graph membership and unlock rules.</summary>
public static class CampaignUnlockPolicy
{
    public static bool IsUnlocked(
        CampaignDefinition campaign,
        EventId eventId,
        IEnumerable<string> completedEventIds,
        IReadOnlyDictionary<NodeId, ChoiceId> choices
    )
    {
        if (!campaign.EventIds.Contains(eventId))
            return false;
        if (campaign.StartEventId == eventId)
            return true;

        var incoming = campaign.Edges.Where(edge => edge.ToEventId == eventId).ToArray();
        if (incoming.Length == 0)
            return true;

        var completed = completedEventIds.ToHashSet();
        return incoming.Any(edge =>
            completed.Contains(edge.FromEventId.Value)
            && (
                edge.Condition?.ChoiceId is not { } requiredChoice
                || (
                    choices.TryGetValue(new NodeId(edge.FromEventId.Value), out var actualChoice)
                    && actualChoice == requiredChoice
                )
            )
        );
    }
}
