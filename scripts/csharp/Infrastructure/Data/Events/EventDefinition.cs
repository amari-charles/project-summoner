using System.Collections.Generic;
using System.Collections.Immutable;
using Fateforged.Data.Rewards;
using Godot;

namespace Fateforged.Data.Events;

/// <summary>
/// Base class for all event definitions in the campaign system.
/// Subclasses provide type-specific properties for each event type.
/// </summary>
public abstract class EventDefinition
{
    /// <summary>Unique event identifier</summary>
    public EventId Id { get; set; } = EventId.None;

    /// <summary>Localization key for event name</summary>
    public string NameKey { get; set; } = "";

    /// <summary>Localization key for event description</summary>
    public string DescriptionKey { get; set; } = "";

    /// <summary>Position in the authored progression graph.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Whether the event can be replayed after completion</summary>
    public bool Repeatable { get; set; }

    /// <summary>Event type (determined by subclass)</summary>
    public abstract EventType Type { get; }
}

/// <summary>
/// Battle event definition - standard combat encounters.
/// </summary>
public class BattleEventDefinition : EventDefinition
{
    public override EventType Type => EventType.Battle;

    /// <summary>Biome ID for battlefield environment</summary>
    public BiomeId Biome { get; set; } = BiomeIds.Default;

    /// <summary>Relative difficulty rating (higher is harder).</summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>Whether this is a tutorial battle with special handling</summary>
    public bool IsTutorial { get; set; }

    /// <summary>Whether player must select a deck to start</summary>
    public bool RequiresDeck { get; set; } = true;

    /// <summary>Enemy deck composition</summary>
    public List<DeckEntry> EnemyDeck { get; set; } = new();

    /// <summary>Enemy summoner HP</summary>
    public float EnemyHp { get; set; } = 100f;

    /// <summary>XP earned for every distinct victorious attempt.</summary>
    public int CardXpReward { get; set; }

    /// <summary>Summoner XP earned for every distinct victorious attempt.</summary>
    public int SummonerXpReward { get; set; }

    /// <summary>Universal offers resolved only on this battle's first clear.</summary>
    public ImmutableArray<RewardOfferDefinition> FirstClearRewardOffers { get; set; } = [];

    /// <summary>AI type (heuristic, passive, etc.)</summary>
    public string AiType { get; set; } = "heuristic";

    /// <summary>AI difficulty rating used by heuristic decision making.</summary>
    public int AiDifficulty { get; set; } = 3;

    /// <summary>Minimum seconds between AI card plays.</summary>
    public float AiPlayIntervalMin { get; set; } = 3.0f;

    /// <summary>Maximum seconds between AI card plays.</summary>
    public float AiPlayIntervalMax { get; set; } = 6.0f;

    /// <summary>Dev-only player deck override (for test battles)</summary>
    public List<DeckEntry>? DevPlayerDeck { get; set; }

    /// <summary>Application runtime surface used to present this battle.</summary>
    public BattleRuntimeSurface RuntimeSurface { get; set; } = BattleRuntimeSurface.Standard;
}

/// <summary>
/// Elite battle event definition - harder battles with level caps.
/// </summary>
public class EliteEventDefinition : BattleEventDefinition
{
    public override EventType Type => EventType.Elite;

    /// <summary>Maximum card level allowed (null = no cap)</summary>
    public int? LevelCap { get; set; }
}

/// <summary>
/// Boss battle event definition - major boss encounters.
/// </summary>
public class BossEventDefinition : BattleEventDefinition
{
    public override EventType Type => EventType.Boss;
}

/// <summary>
/// Choice event definition - path branching decision points.
/// </summary>
public class ChoiceEventDefinition : EventDefinition
{
    public override EventType Type => EventType.Choice;

    /// <summary>Available choices at this node</summary>
    public List<ChoiceOption> Options { get; set; } = new();
}
