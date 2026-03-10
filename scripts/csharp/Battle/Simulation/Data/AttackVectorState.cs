using Fateforged.Units;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Runtime simulation state for vector-based attack behavior.
/// </summary>
public sealed class AttackVectorState
{
    public AttackPreset Preset { get; set; } = AttackPreset.SingleTarget;
    public AttackTimingState Timing { get; set; } = AttackTimingState.Default();
    public AttackDeliveryMode DeliveryMode { get; set; } = AttackDeliveryMode.Instant;
    public AttackSelectionState Selection { get; set; } = AttackSelectionState.Default();
    public AttackAreaState Area { get; set; } = AttackAreaState.Default();
    public AttackPropagationState Propagation { get; set; } = AttackPropagationState.Default();
    public AttackRulesState Rules { get; set; } = AttackRulesState.Default();

    public static AttackVectorState Default() => new();

    public AttackVectorState DeepClone()
    {
        return new AttackVectorState
        {
            Preset = Preset,
            Timing = Timing.DeepClone(),
            DeliveryMode = DeliveryMode,
            Selection = Selection.DeepClone(),
            Area = Area.DeepClone(),
            Propagation = Propagation.DeepClone(),
            Rules = Rules.DeepClone()
        };
    }
}

public sealed class AttackTimingState
{
    public float WindupSeconds { get; set; }
    public float ActiveSeconds { get; set; }
    public float RecoverySeconds { get; set; }
    public float TickIntervalSeconds { get; set; }

    public static AttackTimingState Default() => new();

    public AttackTimingState DeepClone()
    {
        return new AttackTimingState
        {
            WindupSeconds = WindupSeconds,
            ActiveSeconds = ActiveSeconds,
            RecoverySeconds = RecoverySeconds,
            TickIntervalSeconds = TickIntervalSeconds
        };
    }
}

public sealed class AttackSelectionState
{
    public AttackSelectionMode Mode { get; set; } = AttackSelectionMode.Single;
    public int TargetLimit { get; set; } = 1;

    public static AttackSelectionState Default() => new();

    public AttackSelectionState DeepClone()
    {
        return new AttackSelectionState
        {
            Mode = Mode,
            TargetLimit = TargetLimit
        };
    }
}

public sealed class AttackAreaState
{
    public AttackAreaShape Shape { get; set; } = AttackAreaShape.Sphere;
    public SimVector3 Size { get; set; } = new(1f, 1f, 1f);
    public float LineLength { get; set; }
    public float LineHalfWidth { get; set; }

    public static AttackAreaState Default() => new();

    public AttackAreaState DeepClone()
    {
        return new AttackAreaState
        {
            Shape = Shape,
            Size = Size,
            LineLength = LineLength,
            LineHalfWidth = LineHalfWidth
        };
    }
}

public sealed class AttackPropagationState
{
    public AttackPropagationMode Mode { get; set; } = AttackPropagationMode.None;
    public int ChainMaxJumps { get; set; }
    public float ChainJumpRadius { get; set; }

    public static AttackPropagationState Default() => new();

    public AttackPropagationState DeepClone()
    {
        return new AttackPropagationState
        {
            Mode = Mode,
            ChainMaxJumps = ChainMaxJumps,
            ChainJumpRadius = ChainJumpRadius
        };
    }
}

public sealed class AttackRulesState
{
    public bool IncludeSummonerTargets { get; set; }
    public bool AllowRepeatHits { get; set; }
    public AttackTriggerMode TriggerMode { get; set; } = AttackTriggerMode.PrimaryOnly;

    public static AttackRulesState Default() => new();

    public AttackRulesState DeepClone()
    {
        return new AttackRulesState
        {
            IncludeSummonerTargets = IncludeSummonerTargets,
            AllowRepeatHits = AllowRepeatHits,
            TriggerMode = TriggerMode
        };
    }
}
