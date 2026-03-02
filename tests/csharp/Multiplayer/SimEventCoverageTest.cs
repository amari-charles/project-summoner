namespace ProjectSummoner.Tests.Multiplayer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GdUnit4;
using Fateforged.Simulation;
using Fateforged.Multiplayer.Protocol;
using static GdUnit4.Assertions;

/// <summary>
/// Enforces that every SimEvent subclass is categorized and that
/// all Broadcast events have the full multiplayer pipeline wired up:
/// SimEvent → Protocol message → ClientRunner handler.
///
/// If you add a new SimEvent and these tests fail, you need to:
/// 1. Add [EventCategory(...)] to the new event class
/// 2. If Broadcast: add a protocol message type and a ClientRunner handler
/// 3. If Snapshot/HostOnly: no further action needed
/// </summary>
[TestSuite]
public class SimEventCoverageTest
{
    /// <summary>
    /// Explicit mapping from each Broadcast SimEvent to its protocol message type.
    /// When you add a new Broadcast event, add its mapping here — if you forget,
    /// AllBroadcastEvents_HaveProtocolMessage will fail.
    /// </summary>
    private static readonly Dictionary<Type, Type> BroadcastEventToMessage = new()
    {
        { typeof(UnitRegisteredEvent),     typeof(UnitSpawned) },
        { typeof(UnitDiedSimEvent),        typeof(UnitDied) },
        { typeof(UnitDamagedEvent),        typeof(DamageDealt) },
        { typeof(SummonerHpChangedEvent),  typeof(SummonerDamaged) },
        { typeof(GameOverEvent),           typeof(MatchEnded) },
        { typeof(SummonerDamagedEvent),   typeof(SummonerDamageFlash) },
        { typeof(SummonerDestroyedEvent), typeof(SummonerDestroyed) },
    };

    /// <summary>
    /// Protocol message types that ClientRunner.HandleMessage dispatches.
    /// Extracted from the switch statement in ClientRunner.
    /// </summary>
    private static readonly HashSet<Type> ClientHandledMessages = new()
    {
        typeof(CardPlayConfirmed),
        typeof(CardPlayRejected),
        typeof(StateSnapshot),
        typeof(UnitSpawned),
        typeof(UnitDied),
        typeof(DamageDealt),
        typeof(SummonerDamaged),
        typeof(Pong),
        typeof(MatchEnded),
        typeof(SummonerDamageFlash),
        typeof(SummonerDestroyed),
    };

    private static List<Type> GetAllSimEventTypes()
    {
        var assembly = typeof(SimEvent).Assembly;
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(SimEvent)))
            .OrderBy(t => t.Name)
            .ToList();
    }

    [TestCase]
    public void AllSimEvents_HaveCategory()
    {
        var eventTypes = GetAllSimEventTypes();
        AssertThat(eventTypes.Count).IsGreater(0);

        var missing = new List<string>();
        foreach (var type in eventTypes)
        {
            var attr = type.GetCustomAttribute<EventCategoryAttribute>();
            if (attr == null)
                missing.Add(type.Name);
        }

        AssertThat(missing)
            .OverrideFailureMessage(
                $"SimEvent types missing [EventCategory] attribute: {string.Join(", ", missing)}. " +
                "Add [EventCategory(EventCategory.Broadcast|Snapshot|HostOnly)] to each.")
            .IsEmpty();
    }

    [TestCase]
    public void AllBroadcastEvents_HaveProtocolMessage()
    {
        var eventTypes = GetAllSimEventTypes();

        var broadcastEvents = eventTypes
            .Where(t => t.GetCustomAttribute<EventCategoryAttribute>()?.Category == EventCategory.Broadcast)
            .ToList();

        var missing = new List<string>();
        foreach (var eventType in broadcastEvents)
        {
            if (!BroadcastEventToMessage.ContainsKey(eventType))
                missing.Add(eventType.Name);
        }

        AssertThat(missing)
            .OverrideFailureMessage(
                $"Broadcast SimEvents without protocol message mapping: {string.Join(", ", missing)}. " +
                "Add the mapping to BroadcastEventToMessage in SimEventCoverageTest " +
                "and create the corresponding protocol message type.")
            .IsEmpty();
    }

    [TestCase]
    public void AllBroadcastMessages_HandledByClientRunner()
    {
        var broadcastMessageTypes = BroadcastEventToMessage.Values.ToHashSet();

        var unhandled = new List<string>();
        foreach (var msgType in broadcastMessageTypes)
        {
            if (!ClientHandledMessages.Contains(msgType))
                unhandled.Add(msgType.Name);
        }

        AssertThat(unhandled)
            .OverrideFailureMessage(
                $"Broadcast protocol messages not handled by ClientRunner: {string.Join(", ", unhandled)}. " +
                "Add a case for each in ClientRunner.HandleMessage().")
            .IsEmpty();
    }

    [TestCase]
    public void ISimEventVisitor_HasVisitMethodForEverySimEvent()
    {
        var eventTypes = GetAllSimEventTypes();
        var visitorType = typeof(ISimEventVisitor);

        var visitMethods = visitorType.GetMethods()
            .Where(m => m.Name == "Visit" && m.GetParameters().Length == 1)
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToHashSet();

        var missing = new List<string>();
        foreach (var eventType in eventTypes)
        {
            if (!visitMethods.Contains(eventType))
                missing.Add(eventType.Name);
        }

        AssertThat(missing)
            .OverrideFailureMessage(
                $"SimEvent types missing Visit() overload in ISimEventVisitor: {string.Join(", ", missing)}. " +
                "Add void Visit(<EventType> e) to ISimEventVisitor.")
            .IsEmpty();
    }

    [TestCase]
    public void BroadcastEventToMessage_OnlyContainsBroadcastEvents()
    {
        var wrongCategory = new List<string>();
        foreach (var (eventType, _) in BroadcastEventToMessage)
        {
            var attr = eventType.GetCustomAttribute<EventCategoryAttribute>();
            if (attr?.Category != EventCategory.Broadcast)
                wrongCategory.Add($"{eventType.Name} is {attr?.Category.ToString() ?? "uncategorized"}");
        }

        AssertThat(wrongCategory)
            .OverrideFailureMessage(
                $"Events in BroadcastEventToMessage that are not [Broadcast]: {string.Join(", ", wrongCategory)}. " +
                "Either change the event's [EventCategory] to Broadcast or remove it from the mapping.")
            .IsEmpty();
    }
}
