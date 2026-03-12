namespace Fateforged.Tests.Multiplayer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fateforged.Simulation;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
/// Enforces that every SimEvent subclass is categorized and that
/// ISimEventVisitor has a Visit method for every SimEvent type.
/// </summary>
[TestSuite]
public class SimEventCoverageTest
{
    private static List<Type> GetAllSimEventTypes()
    {
        var assembly = typeof(SimEvent).Assembly;
        return assembly
            .GetTypes()
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
                $"SimEvent types missing [EventCategory] attribute: {string.Join(", ", missing)}. "
                    + "Add [EventCategory(EventCategory.Broadcast|Snapshot|HostOnly)] to each."
            )
            .IsEmpty();
    }

    [TestCase]
    public void ISimEventVisitor_HasVisitMethodForEverySimEvent()
    {
        var eventTypes = GetAllSimEventTypes();
        var visitorType = typeof(ISimEventVisitor);

        var visitMethods = visitorType
            .GetMethods()
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
                $"SimEvent types missing Visit() overload in ISimEventVisitor: {string.Join(", ", missing)}. "
                    + "Add void Visit(<EventType> e) to ISimEventVisitor."
            )
            .IsEmpty();
    }
}
