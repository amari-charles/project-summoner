namespace Fateforged.Data.Events;

/// <summary>
/// Categories of authored battle content.
/// </summary>
public enum EventType
{
    /// <summary>Standard combat battle</summary>
    Battle,

    /// <summary>Elite battle with level caps and higher difficulty</summary>
    Elite,

    /// <summary>Major boss encounter</summary>
    Boss,

}

/// <summary>
/// Extension methods for EventType enum.
/// </summary>
public static class EventTypeExtensions
{
    /// <summary>Check if event type requires combat system</summary>
    public static bool IsCombat(this EventType type) =>
        type is EventType.Battle or EventType.Elite or EventType.Boss;

    /// <summary>Convert to the serialized event-type identifier.</summary>
    public static string ToStringId(this EventType type) =>
        type switch
        {
            EventType.Battle => "battle",
            EventType.Elite => "elite",
            EventType.Boss => "boss",
            _ => "battle",
        };

    /// <summary>Parse from string ID</summary>
    public static EventType FromStringId(string id) =>
        id switch
        {
            "battle" => EventType.Battle,
            "elite" => EventType.Elite,
            "boss" => EventType.Boss,
            _ => EventType.Battle,
        };
}
