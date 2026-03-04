using System.Collections.Generic;
using Godot;
using Fateforged.Meta.Shop;

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

    /// <summary>Position on campaign map</summary>
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

    /// <summary>Difficulty rating (1-10)</summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>Whether this is a tutorial battle with special handling</summary>
    public bool IsTutorial { get; set; }

    /// <summary>Whether player must select a deck to start</summary>
    public bool RequiresDeck { get; set; } = true;

    /// <summary>Enemy deck composition</summary>
    public List<DeckEntry> EnemyDeck { get; set; } = new();

    /// <summary>Enemy summoner HP</summary>
    public float EnemyHp { get; set; } = 100f;

    /// <summary>Reward configuration</summary>
    public BattleRewardConfig Rewards { get; set; } = new();

    /// <summary>AI type (heuristic, passive, etc.)</summary>
    public string AiType { get; set; } = "heuristic";

    /// <summary>Dev-only player deck override (for test battles)</summary>
    public List<DeckEntry>? DevPlayerDeck { get; set; }

    /// <summary>Custom scene path (for special battles like debug arena)</summary>
    public string? ScenePath { get; set; }
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

/// <summary>
/// Caravan event definition - traveling merchant shop.
/// </summary>
public class CaravanEventDefinition : EventDefinition
{
    public override EventType Type => EventType.Caravan;

    /// <summary>Shop configuration ID</summary>
    public ShopId ShopId { get; set; } = Fateforged.Meta.Shop.ShopId.None;
}

/// <summary>
/// Rest event definition - heal/recovery opportunity.
/// </summary>
public class RestEventDefinition : EventDefinition
{
    public override EventType Type => EventType.Rest;

    /// <summary>HP recovery percentage (0.0 - 1.0)</summary>
    public float HealPercent { get; set; } = 0.3f;
}

/// <summary>
/// Story event definition - narrative events without combat.
/// </summary>
public class StoryEventDefinition : EventDefinition
{
    public override EventType Type => EventType.Story;

    /// <summary>Story sequence ID to play</summary>
    public SequenceId SequenceId { get; set; } = SequenceId.None;
}
