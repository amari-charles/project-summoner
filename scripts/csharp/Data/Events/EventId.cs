namespace ProjectSummoner.Data.Events;

/// <summary>
/// Event ID constants - type-safe event references.
/// Mirrors GDScript BattleIDs for C# code.
/// </summary>
public static class EventId
{
    // =========================================================================
    // ACT 1: THE INITIATE'S PATH
    // =========================================================================

    /// <summary>Battle: First Trial - Learn basics with 1 card</summary>
    public const string FirstTrial = "first_trial";

    /// <summary>Battle: Second Challenge - Test 2-card combos</summary>
    public const string SecondChallenge = "second_challenge";

    /// <summary>Event: Caravan - First shop visit</summary>
    public const string Caravan01 = "caravan_01";

    /// <summary>Battle: Third Trial - Medium difficulty</summary>
    public const string ThirdTrial = "third_trial";

    /// <summary>Choice: Elite vs Standard Path Fork</summary>
    public const string PathFork = "path_fork";

    /// <summary>Battle: Elite Path Battle 1 - Higher difficulty with level cap</summary>
    public const string EliteBattle01 = "elite_battle_01";

    /// <summary>Battle: Standard Path Battle 1 - Normal difficulty</summary>
    public const string StandardBattle01 = "standard_battle_01";

    /// <summary>Boss: Act 1 Boss - First major boss</summary>
    public const string Act1Boss = "act1_boss";

    // =========================================================================
    // TEST ARENA (Debug/test battles with fixed decks)
    // =========================================================================

    /// <summary>Test Arena: Earth Sprite Test</summary>
    public const string ArenaEarthSprite = "arena_earth_sprite";

    /// <summary>Test Arena: Puff Test</summary>
    public const string ArenaPuff = "arena_puff";

    /// <summary>Test Arena: Fire Wisp Test</summary>
    public const string ArenaFireWisp = "arena_fire_wisp";

    /// <summary>Test Arena: Cloud Swarm Test</summary>
    public const string ArenaCloudSwarm = "arena_cloud_swarm";

    /// <summary>Test Arena: Mana Bolt Spell Test</summary>
    public const string ArenaManaBolt = "arena_mana_bolt";

    /// <summary>Test Arena: Debug Arena - Testing sandbox</summary>
    public const string DebugArena = "debug_arena";
}
