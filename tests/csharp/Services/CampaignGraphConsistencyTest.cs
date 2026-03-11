namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Events;
using Fateforged.Meta.Campaign;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CampaignGraphConsistencyTest
{
    [TestCase]
    public void SummonersPath_GraphIsConnectedAndBossReachable()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.SummonersPath);
        AssertThat(campaign).IsNotNull();

        var graph = campaign!;
        var nodeSet = graph.EventIds.ToHashSet();
        AssertThat(nodeSet.Contains(graph.StartEventId)).IsTrue();
        AssertThat(nodeSet.Contains(EventIds.Act1Boss)).IsTrue();
        AssertThat(nodeSet.Count).IsEqual(graph.EventIds.Count);

        foreach (var eventId in graph.EventIds)
        {
            AssertThat(EventCatalog.GetEvent(eventId)).IsNotNull();
        }

        foreach (var edge in graph.Edges)
        {
            AssertThat(nodeSet.Contains(edge.FromEventId)).IsTrue();
            AssertThat(nodeSet.Contains(edge.ToEventId)).IsTrue();
        }

        var adjacency = BuildAdjacency(graph.Edges);
        var reachableFromStart = Traverse(adjacency, graph.StartEventId);
        AssertThat(reachableFromStart.Count).IsEqual(nodeSet.Count);

        foreach (var node in nodeSet)
        {
            AssertThat(CanReach(adjacency, node, EventIds.Act1Boss)).IsTrue();
        }
    }

    [TestCase]
    public void SummonersPath_ChoiceOptionsMatchConditionedOutgoingEdges()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.SummonersPath);
        AssertThat(campaign).IsNotNull();
        var graph = campaign!;

        var choiceNodes = graph.EventIds
            .Select(EventCatalog.GetEvent)
            .OfType<ChoiceEventDefinition>()
            .ToArray();

        AssertThat(choiceNodes.Length).IsGreater(0);

        foreach (var choiceNode in choiceNodes)
        {
            var expectedOptions = choiceNode.Options.Select(option => option.Id).ToHashSet();
            var outgoing = graph.Edges.Where(edge => edge.FromEventId == choiceNode.Id).ToArray();
            var conditioned = outgoing
                .Where(edge => edge.Condition?.ChoiceId is ChoiceId)
                .Select(edge => edge.Condition!.ChoiceId!.Value)
                .ToHashSet();

            AssertThat(outgoing.Length).IsEqual(expectedOptions.Count);
            AssertThat(conditioned.SetEquals(expectedOptions)).IsTrue();
        }
    }

    [TestCase]
    public void SummonersPath_PathForkBranchesMatchTheirRiskProfiles()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.SummonersPath);
        AssertThat(campaign).IsNotNull();
        var graph = campaign!;
        var adjacency = BuildAdjacency(graph.Edges);

        var forkEdges = graph.Edges
            .Where(edge => edge.FromEventId == EventIds.PathFork && edge.Condition?.ChoiceId is ChoiceId)
            .ToArray();

        AssertThat(forkEdges.Length).IsEqual(3);

        var eliteStart = forkEdges.Single(edge => edge.Condition!.ChoiceId == ChoiceIds.Elite).ToEventId;
        var standardStart = forkEdges.Single(edge => edge.Condition!.ChoiceId == ChoiceIds.Standard).ToEventId;
        var gambitStart = forkEdges.Single(edge => edge.Condition!.ChoiceId == ChoiceIds.Gambit).ToEventId;

        var eliteChain = GetLinearChain(adjacency, eliteStart, EventIds.RejoinTrial);
        var standardChain = GetLinearChain(adjacency, standardStart, EventIds.RejoinTrial);
        var gambitChain = GetLinearChain(adjacency, gambitStart, EventIds.RejoinTrial);

        AssertThat(eliteChain.Count).IsEqual(5);
        AssertThat(standardChain.Count).IsEqual(6);
        AssertThat(gambitChain.Count).IsEqual(5);

        foreach (var eventId in eliteChain.Take(eliteChain.Count - 1))
        {
            var evt = EventCatalog.GetEvent(eventId);
            AssertThat(evt is EliteEventDefinition).IsTrue();
            AssertThat(((EliteEventDefinition)evt!).LevelCap.HasValue).IsTrue();
        }

        AssertThat(standardChain.Contains(EventIds.Caravan03)).IsTrue();
        AssertThat(standardChain.Take(standardChain.Count - 1)
            .Any(eventId => EventCatalog.GetEvent(eventId) is CaravanEventDefinition)).IsTrue();

        AssertThat(gambitChain.Take(gambitChain.Count - 1)
            .Any(eventId => EventCatalog.GetEvent(eventId) is CaravanEventDefinition)).IsFalse();

        var gambitDifficulties = gambitChain.Take(gambitChain.Count - 1)
            .Select(eventId => EventCatalog.GetEvent(eventId))
            .OfType<BattleEventDefinition>()
            .Select(evt => evt.Difficulty)
            .ToArray();

        AssertThat(gambitDifficulties.Length).IsEqual(4);

        bool hasSpike = false;
        bool hasDip = false;
        for (int i = 1; i < gambitDifficulties.Length; i++)
        {
            if (gambitDifficulties[i] > gambitDifficulties[i - 1])
                hasSpike = true;
            if (gambitDifficulties[i] < gambitDifficulties[i - 1])
                hasDip = true;
        }

        AssertThat(hasSpike).IsTrue();
        AssertThat(hasDip).IsTrue();
    }

    private static Dictionary<EventId, List<CampaignEdge>> BuildAdjacency(IEnumerable<CampaignEdge> edges)
    {
        var adjacency = new Dictionary<EventId, List<CampaignEdge>>();
        foreach (var edge in edges)
        {
            if (!adjacency.TryGetValue(edge.FromEventId, out var list))
            {
                list = [];
                adjacency[edge.FromEventId] = list;
            }
            list.Add(edge);
        }

        return adjacency;
    }

    private static HashSet<EventId> Traverse(Dictionary<EventId, List<CampaignEdge>> adjacency, EventId start)
    {
        var seen = new HashSet<EventId> { start };
        var queue = new Queue<EventId>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!adjacency.TryGetValue(current, out var outgoing))
                continue;

            foreach (var edge in outgoing)
            {
                if (seen.Add(edge.ToEventId))
                    queue.Enqueue(edge.ToEventId);
            }
        }

        return seen;
    }

    private static bool CanReach(Dictionary<EventId, List<CampaignEdge>> adjacency, EventId start, EventId target)
    {
        var seen = new HashSet<EventId> { start };
        var stack = new Stack<EventId>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == target)
                return true;

            if (!adjacency.TryGetValue(current, out var outgoing))
                continue;

            foreach (var edge in outgoing)
            {
                if (seen.Add(edge.ToEventId))
                    stack.Push(edge.ToEventId);
            }
        }

        return false;
    }

    private static List<EventId> GetLinearChain(
        Dictionary<EventId, List<CampaignEdge>> adjacency,
        EventId start,
        EventId terminal)
    {
        var chain = new List<EventId> { start };
        var current = start;

        for (int guard = 0; guard < 32 && current != terminal; guard++)
        {
            if (!adjacency.TryGetValue(current, out var outgoing))
                break;

            var unconditional = outgoing
                .Where(edge => edge.Condition?.ChoiceId is null)
                .Select(edge => edge.ToEventId)
                .ToArray();

            if (unconditional.Length != 1)
                break;

            current = unconditional[0];
            chain.Add(current);
        }

        return chain;
    }
}
