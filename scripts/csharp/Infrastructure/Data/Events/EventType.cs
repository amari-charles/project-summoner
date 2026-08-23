namespace Fateforged.Data.Events;

/// <summary>
/// Event type enum - categorizes campaign node types.
/// </summary>
public enum EventType
{
    /// <summary>Standard combat battle</summary>
    Battle,

    /// <summary>Elite battle with level caps and higher difficulty</summary>
    Elite,

    /// <summary>Major boss encounter</summary>
    Boss,

    /// <summary>Path branching decision point</summary>
    Choice,
}

/// <summary>
/// Extension methods for EventType enum.
/// </summary>
public static class EventTypeExtensions
{
    /// <summary>Check if event type requires combat system</summary>
    public static bool IsCombat(this EventType type) =>
        type is EventType.Battle or EventType.Elite or EventType.Boss;

    /// <summary>Convert to string ID matching GDScript NodeTypeIDs</summary>
    public static string ToStringId(this EventType type) =>
        type switch
        {
            EventType.Battle => "battle",
            EventType.Elite => "elite",
            EventType.Boss => "boss",
            EventType.Choice => "choice",
            _ => "battle",
        };

    /// <summary>Parse from string ID</summary>
    public static EventType FromStringId(string id) =>
        id switch
        {
            "battle" => EventType.Battle,
            "elite" => EventType.Elite,
            "boss" => EventType.Boss,
            "choice" => EventType.Choice,
            _ => EventType.Battle,
        };
}
