using System;

namespace Fateforged.Simulation;

/// <summary>
/// Categories for SimEvent types, determining how they are handled in multiplayer.
/// Used by the [EventCategory] attribute and enforced by SimEventCoverageTest.
/// </summary>
public enum EventCategory
{
    /// <summary>Event must be broadcast as a network message for client-side handling.</summary>
    Broadcast,
    /// <summary>Event state is covered by periodic StateSnapshot. No message needed.</summary>
    Snapshot,
    /// <summary>Event is host-only (no client equivalent needed yet).</summary>
    HostOnly
}

/// <summary>
/// Annotate each SimEvent subclass with its multiplayer category.
/// SimEventCoverageTest enforces that every SimEvent has this attribute
/// and that all [Broadcast] events have corresponding protocol messages and client handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EventCategoryAttribute : Attribute
{
    public EventCategory Category { get; }
    public EventCategoryAttribute(EventCategory category) => Category = category;
}
