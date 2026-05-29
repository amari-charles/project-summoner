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
/// Example: EventIds.FirstTrial instead of "first_trial"
/// </summary>
public static class EventIds
{
    // =========================================================================
    // ACT 1: THE INITIATE'S PATH
    // =========================================================================

    /// <summary>Battle: First Trial - Learn basics with 1 card</summary>
    public static readonly EventId FirstTrial = new("first_trial");

    /// <summary>Battle: Second Challenge - Test 2-card combos</summary>
    public static readonly EventId SecondChallenge = new("second_challenge");

    /// <summary>Event: Caravan - First shop visit</summary>
    public static readonly EventId Caravan01 = new("caravan_01");

    /// <summary>Battle: Third Trial - Medium difficulty</summary>
    public static readonly EventId ThirdTrial = new("third_trial");

    /// <summary>Choice: Opening doctrine (aggressive vs prepared vs insight)</summary>
    public static readonly EventId OpeningDoctrine = new("opening_doctrine");

    /// <summary>Battle: Aggressive branch opener</summary>
    public static readonly EventId AggressivePush = new("aggressive_push");

    /// <summary>Battle: Scouting branch opener</summary>
    public static readonly EventId ScoutSkirmish = new("scout_skirmish");

    /// <summary>Battle: Stability branch follow-up</summary>
    public static readonly EventId StabilityLine = new("stability_line");

    /// <summary>Battle: Midline reconvergence test</summary>
    public static readonly EventId MidlineTrial = new("midline_trial");

    /// <summary>Choice: Mid-act route split</summary>
    public static readonly EventId RouteChoice = new("route_choice");

    /// <summary>Battle: Upper route encounter</summary>
    public static readonly EventId RidgeAssault = new("ridge_assault");

    /// <summary>Battle: Lower route encounter</summary>
    public static readonly EventId RiverHold = new("river_hold");

    /// <summary>Battle: Wide flank route encounter</summary>
    public static readonly EventId GrovePatrol = new("grove_patrol");

    /// <summary>Event: Caravan - Mid-act shop visit</summary>
    public static readonly EventId Caravan02 = new("caravan_02");

    /// <summary>Battle: Chokepoint encounter</summary>
    public static readonly EventId Chokepoint = new("chokepoint");

    /// <summary>Boss: Mini-boss gate encounter</summary>
    public static readonly EventId Gatekeeper = new("gatekeeper");

    /// <summary>Choice: Elite vs Standard vs Gambit Path Fork</summary>
    public static readonly EventId PathFork = new("path_fork");

    /// <summary>Battle: Elite Path Battle 1 - Higher difficulty with level cap</summary>
    public static readonly EventId EliteBattle01 = new("elite_battle_01");

    /// <summary>Battle: Elite Path Battle 2</summary>
    public static readonly EventId EliteBattle02 = new("elite_battle_02");

    /// <summary>Battle: Elite Path Battle 3</summary>
    public static readonly EventId EliteBattle03 = new("elite_battle_03");

    /// <summary>Battle: Elite Path Battle 4</summary>
    public static readonly EventId EliteBattle04 = new("elite_battle_04");

    /// <summary>Battle: Standard Path Battle 1 - Normal difficulty</summary>
    public static readonly EventId StandardBattle01 = new("standard_battle_01");

    /// <summary>Event: Caravan - Standard branch shop visit</summary>
    public static readonly EventId Caravan03 = new("caravan_03");

    /// <summary>Battle: Standard Path Battle 2</summary>
    public static readonly EventId StandardBattle02 = new("standard_battle_02");

    /// <summary>Battle: Standard Path Battle 3</summary>
    public static readonly EventId StandardBattle03 = new("standard_battle_03");

    /// <summary>Battle: Standard Path Battle 4</summary>
    public static readonly EventId StandardBattle04 = new("standard_battle_04");

    /// <summary>Battle: Gambit Path Battle 1</summary>
    public static readonly EventId GambitBattle01 = new("gambit_battle_01");

    /// <summary>Battle: Gambit Path Battle 2</summary>
    public static readonly EventId GambitBattle02 = new("gambit_battle_02");

    /// <summary>Battle: Gambit Path Battle 3</summary>
    public static readonly EventId GambitBattle03 = new("gambit_battle_03");

    /// <summary>Battle: Gambit Path Battle 4</summary>
    public static readonly EventId GambitBattle04 = new("gambit_battle_04");

    /// <summary>Battle: Branch reconvergence trial</summary>
    public static readonly EventId RejoinTrial = new("rejoin_trial");

    /// <summary>Battle: Final approach before boss</summary>
    public static readonly EventId FinalAnte = new("final_ante");

    /// <summary>Battle: Penultimate stormline encounter</summary>
    public static readonly EventId StormBreaker = new("storm_breaker");

    /// <summary>Boss: Act 1 Boss - First major boss</summary>
    public static readonly EventId Act1Boss = new("act1_boss");

    // =========================================================================
    // TEST ARENA (Debug/test battles with fixed decks)
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

    /// <summary>Test Arena: Debug Arena - Testing sandbox</summary>
    public static readonly EventId DebugArena = new("debug_arena");
}
