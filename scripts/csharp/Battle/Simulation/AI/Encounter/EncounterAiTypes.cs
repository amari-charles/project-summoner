using System.Collections.Generic;

namespace Fateforged.Simulation.AI;

public enum EncounterAiPreset
{
    DefaultTrainer,
    ScriptedEncounter,
}

public enum EncounterDangerState
{
    Calm,
    Pressured,
    Danger,
    Overwhelmed,
}

public enum EncounterActionSource
{
    Trainer,
    Encounter,

    // Future intent: environmental pressure such as academy wards, falling rocks,
    // terrain pulses, or timed danger zones. Not active in v1.
    Hazard,

    // Future intent: objective-owned battle state such as escort targets, capture
    // points, rituals, or survival pylons. Not active in v1.
    Objective,
}

public enum EncounterActionKind
{
    PlayCard,
    SpawnUnits,
    SetBehavior,
    SetRuleEnabled,

    // Future intent: simulation-owned hazards that create positional pressure.
    SpawnHazard,

    // Future intent: temporary arena-level rule changes, such as movement speed
    // bands, temporary ward fields, or global summon modifiers.
    ApplyArenaModifier,

    // Future intent: objective state updates for escort, ritual, capture, or
    // survival encounters once those systems exist.
    SetObjectiveState,

    // Future intent: temporary cards added for the current battle only.
    GrantTemporaryCard,

    // Future intent: battle-specific mana changes that are explicit encounter
    // rules rather than hidden AI cheating.
    ModifyManaRule,

    // Future intent: authored dialogue/callout beats tied to battle state.
    TriggerDialogueBeat,

    // Future intent: objective battles that advance progress without directly
    // damaging the opposing summoner.
    SetWinConditionProgress,
}

public enum EncounterRuleKind
{
    RhythmRule,
    PoolRule,
    CapRule,
    PlacementRule,
    BehaviorRule,
    EventRule,

    // Future intent: reusable environmental pressure tracks.
    HazardRule,

    // Future intent: encounter objectives that can react to battle state.
    ObjectiveRule,

    // Future intent: battle dialogue/callout rules owned by encounter authoring.
    DialogueRule,

    // Future intent: temporary arena modifier tracks.
    ArenaModifierRule,

    // Future intent: preview-only reward/tutorial beats during battle.
    RewardPreviewRule,
}

public enum EncounterRhythm
{
    Sparse,
    Steady,
    Frequent,
    Relentless,
}

public enum EncounterPlacement
{
    Defensive,
    Neutral,
    Aggressive,
}

public enum EncounterActionStatus
{
    Executed,
    Blocked,
    NoValidTarget,
    Unsupported,
}

public sealed class EncounterActionResult
{
    public EncounterActionStatus Status { get; }
    public string Reason { get; }

    private EncounterActionResult(EncounterActionStatus status, string reason)
    {
        Status = status;
        Reason = reason;
    }

    public static EncounterActionResult Executed() => new(EncounterActionStatus.Executed, "");

    public static EncounterActionResult Blocked(string reason) =>
        new(EncounterActionStatus.Blocked, reason);

    public static EncounterActionResult NoValidTarget(string reason) =>
        new(EncounterActionStatus.NoValidTarget, reason);

    public static EncounterActionResult Unsupported(EncounterActionKind kind) =>
        new(EncounterActionStatus.Unsupported, $"{kind} is reserved for future Encounter AI work.");
}

public sealed class EncounterAiConfig
{
    public EncounterAiPreset Preset { get; set; } = EncounterAiPreset.DefaultTrainer;
    public int Team { get; set; } = 1;
    public bool UseTrainerAi { get; set; } = true;
    public bool PositionsAreCanonical { get; set; }
    public List<EncounterRule> Rules { get; set; } = [];

    public float LastActionTime { get; set; } = float.NegativeInfinity;
    public float LastPlayerHp { get; set; } = float.NaN;
    public float LastPlayerDamageTime { get; set; } = float.NegativeInfinity;
    public EncounterDangerState LastDangerState { get; set; } = EncounterDangerState.Calm;

    public static EncounterAiConfig DefaultTrainer(int team = 1) =>
        new()
        {
            Preset = EncounterAiPreset.DefaultTrainer,
            Team = team,
            UseTrainerAi = true,
        };

    public static EncounterAiConfig ScriptedEncounter(int team = 1) =>
        new()
        {
            Preset = EncounterAiPreset.ScriptedEncounter,
            Team = team,
            UseTrainerAi = false,
        };
}

public sealed class EncounterRule
{
    public string Id { get; set; } = "";
    public EncounterRuleKind Kind { get; set; }
    public bool Enabled { get; set; } = true;
    public float StartTime { get; set; } = 0f;
    public float? EndTime { get; set; }

    public EncounterRhythm Rhythm { get; set; } = EncounterRhythm.Steady;
    public float? IntervalSeconds { get; set; }
    public int? MaxExecutions { get; set; }
    public int ExecutionCount { get; set; }
    public float LastExecutionTime { get; set; } = float.NegativeInfinity;
    public bool Fired { get; set; }

    public int? MaxAlive { get; set; }
    public EncounterPlacement Placement { get; set; } = EncounterPlacement.Neutral;
    public AiType? AiType { get; set; }
    public AiPersonality? Personality { get; set; }
    public float? PlayIntervalMin { get; set; }
    public float? PlayIntervalMax { get; set; }

    public EncounterActionSource Source { get; set; } = EncounterActionSource.Encounter;
    public List<string> CardPool { get; set; } = [];
    public List<EncounterAction> Actions { get; set; } = [];

    public bool IsActive(float matchTime)
    {
        if (!Enabled || matchTime < StartTime)
            return false;

        return !EndTime.HasValue || matchTime < EndTime.Value;
    }
}

public sealed class EncounterAction
{
    public EncounterActionKind Kind { get; set; }
    public EncounterActionSource Source { get; set; } = EncounterActionSource.Encounter;
    public int Team { get; set; } = 1;
    public string CardId { get; set; } = "";
    public List<string> CardIds { get; set; } = [];
    public SimVector3? Position { get; set; }
    public List<SimVector3> Positions { get; set; } = [];
    public EncounterPlacement Placement { get; set; } = EncounterPlacement.Neutral;
    public bool ActivateImmediately { get; set; } = true;
    public bool AllowWhenOverwhelmed { get; set; }
    public bool IgnoreCaps { get; set; }
    public string RuleId { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public AiType? AiType { get; set; }
    public AiPersonality? Personality { get; set; }
    public float? PlayIntervalMin { get; set; }
    public float? PlayIntervalMax { get; set; }
}
