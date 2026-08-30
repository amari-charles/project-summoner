using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Events;

namespace Fateforged.Data.Encounters;

public enum EncounterExecutionKind
{
    Battle,
}

public enum EncounterOutcome
{
    Victory,
    Defeat,
    Abandoned,
}

public enum EncounterRole
{
    Standard,
    Practice,
    Assessment,
}

public enum EncounterDeckMode
{
    Fixed,
    Owned,
    Flexible,
}

public sealed class EncounterDefinition
{
    public string Id { get; init; } = "";

    public string NameKey { get; init; } = "";

    public EncounterExecutionKind ExecutionKind { get; init; } = EncounterExecutionKind.Battle;

    public EncounterRole Role { get; init; } = EncounterRole.Standard;

    public EncounterBattleConfig? BattleConfig { get; init; }

    public EncounterLoadoutDefinition Loadout { get; init; } = new();

    /// <summary>
    /// Optional authored-battle identity used by the progression authority.
    /// Encounter configuration still owns the battle setup; this identity only
    /// supplies authoritative XP and first-clear rewards.
    /// </summary>
    public string ProgressionBattleId { get; init; } = "";

}

public sealed class EncounterLoadoutDefinition
{
    public EncounterDeckMode Mode { get; init; } = EncounterDeckMode.Owned;

    public List<DeckEntry> SuppliedCards { get; init; } = [];

    public EncounterDeckRules Rules { get; init; } = new();
}

public sealed class EncounterDeckRules
{
    public List<CardType> AllowedCardTypes { get; init; } = [];
    public List<Element> AllowedElements { get; init; } = [];
    public int MinSummons { get; init; }
    public int MinSpells { get; init; }
    public int MaxDeckSize { get; init; }
    public List<CardId> RequiredOwnedCards { get; init; } = [];
    public List<CardId> BannedCards { get; init; } = [];

    public bool HasRules =>
        AllowedCardTypes.Count > 0
        || AllowedElements.Count > 0
        || MinSummons > 0
        || MinSpells > 0
        || MaxDeckSize > 0
        || RequiredOwnedCards.Count > 0
        || BannedCards.Count > 0;
}

public sealed class EncounterBattleConfig
{
    public BiomeId Biome { get; init; } = BiomeIds.Default;
    public List<DeckEntry> EnemyDeck { get; init; } = [];
    public float EnemyHp { get; init; } = 35f;
    public string AiType { get; init; } = "simple";
    public int AiDifficulty { get; init; }
    public float AiPlayIntervalMin { get; init; } = 7f;
    public float AiPlayIntervalMax { get; init; } = 10f;
    public EncounterAiConfig? EncounterAi { get; init; }
}

public sealed class EncounterAiConfig
{
    public string Preset { get; init; } = "default_trainer";
    public int Team { get; init; } = 1;
    public bool? UseTrainerAi { get; init; }
    public List<EncounterRule> Rules { get; init; } = [];
}

public sealed class EncounterRule
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "event";
    public bool Enabled { get; init; } = true;
    public float StartTime { get; init; }
    public float? EndTime { get; init; }
    public string Rhythm { get; init; } = "steady";
    public float? IntervalSeconds { get; init; }
    public int? MaxExecutions { get; init; }
    public int? MaxAlive { get; init; }
    public string Placement { get; init; } = "neutral";
    public string Source { get; init; } = "encounter";
    public string? AiType { get; init; }
    public string? AiPersonality { get; init; }
    public float? AiPlayIntervalMin { get; init; }
    public float? AiPlayIntervalMax { get; init; }
    public List<CardId> CardPool { get; init; } = [];
    public List<EncounterAction> Actions { get; init; } = [];
}

public sealed class EncounterAction
{
    public string Kind { get; init; } = "spawn_units";
    public string Source { get; init; } = "encounter";
    public int Team { get; init; } = 1;
    public CardId CardId { get; init; } = CardId.None;
    public List<CardId> CardIds { get; init; } = [];
    public EncounterPosition? Position { get; init; }
    public List<EncounterPosition> Positions { get; init; } = [];
    public string Placement { get; init; } = "neutral";
    public bool ActivateImmediately { get; init; } = true;
    public string? AiType { get; init; }
    public string? AiPersonality { get; init; }
    public float? AiPlayIntervalMin { get; init; }
    public float? AiPlayIntervalMax { get; init; }
    public bool AllowWhenOverwhelmed { get; init; }
    public bool IgnoreCaps { get; init; }
    public string RuleId { get; init; } = "";
    public bool Enabled { get; init; } = true;
}

public readonly record struct EncounterPosition(float X, float Z);
