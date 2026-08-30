namespace Fateforged.Data.Events;

/// <summary>
/// Strongly-typed identifier for event types.
/// Prevents typos and enables IDE autocomplete via EventIds static class.
/// </summary>
public readonly record struct EventId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(EventId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator EventId(string value) => new(value);

    /// <summary>Create an EventId from a string. Standardized factory for facade boundaries.</summary>
    public static EventId FromString(string id) => new(id);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset event ID.</summary>
    public static readonly EventId None = new("");
}

/// <summary>
/// All known event IDs. Use these instead of raw strings.
/// Example: EventIds.ArenaEarthSprite instead of "arena_earth_sprite"
/// </summary>
public static class EventIds
{
    /// <summary>First Academy practice encounter used by the UI showcase flow.</summary>
    public static readonly EventId IntroSummoningPractice = new("intro_summoning_practice");

    // =========================================================================
    // DEBUG BATTLES (Debug/test battles with fixed decks)
    // =========================================================================

    /// <summary>Test Arena: Earth Sprite Test</summary>
    public static readonly EventId ArenaEarthSprite = new("arena_earth_sprite");

    /// <summary>Test Arena: Puff Test</summary>
    public static readonly EventId ArenaPuff = new("arena_puff");

    /// <summary>Test Arena: Fire Wisp Test</summary>
    public static readonly EventId ArenaFireWisp = new("arena_fire_wisp");

    /// <summary>Test Arena: Cloud Swarm Test</summary>
    public static readonly EventId ArenaCloudSwarm = new("arena_cloud_swarm");

    /// <summary>Test Arena: Mana Bolt Spell Test</summary>
    public static readonly EventId ArenaManaBolt = new("arena_mana_bolt");

    /// <summary>Test Arena: Wind/Earth New Card Set (+ Fire Wisp reference)</summary>
    public static readonly EventId ArenaWindEarthNewCards = new("arena_wind_earth_new_cards");

    /// <summary>Test Arena: All active Fire/Water/Earth/Wind units</summary>
    public static readonly EventId ArenaAllUnits = new("arena_all_units");

    /// <summary>Test Arena: All active Fire/Water/Earth/Wind cards</summary>
    public static readonly EventId ArenaAllCards = new("arena_all_cards");

    /// <summary>Test Arena: All active Fire/Water/Earth/Wind spells with a small real-art unit set</summary>
    public static readonly EventId ArenaAllSpells = new("arena_all_spells");

    /// <summary>Test Arena: Debug battle using only summon cards with production sprite scenes</summary>
    public static readonly EventId ArenaSpriteUnits = new("arena_sprite_units");

    /// <summary>Test Arena: Debug Arena - Testing sandbox</summary>
    public static readonly EventId DebugArena = new("debug_arena");
}
