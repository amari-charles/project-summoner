using Godot;

namespace Fateforged.Units;

/// <summary>
/// Grouped vector-based attack authoring config.
/// </summary>
public record AttackVectorConfig
{
    public AttackPreset Preset { get; init; } = AttackPreset.SingleTarget;
    public AttackTimingConfig Timing { get; init; } = AttackTimingConfig.Default;
    public AttackDeliveryMode DeliveryMode { get; init; } = AttackDeliveryMode.Instant;
    public AttackSelectionConfig Selection { get; init; } = AttackSelectionConfig.Default;
    public AttackAreaConfig Area { get; init; } = AttackAreaConfig.Default;
    public AttackPropagationConfig Propagation { get; init; } = AttackPropagationConfig.Default;
    public AttackRulesConfig Rules { get; init; } = AttackRulesConfig.Default;

    public static AttackVectorConfig Default => new();
}

/// <summary>
/// Timing vector for attack cadence.
/// </summary>
public record AttackTimingConfig
{
    public float WindupSeconds { get; init; }
    public float ActiveSeconds { get; init; }
    public float RecoverySeconds { get; init; }
    public float TickIntervalSeconds { get; init; }

    public static AttackTimingConfig Default => new();
}

/// <summary>
/// Selection vector for recipient acquisition.
/// </summary>
public record AttackSelectionConfig
{
    public AttackSelectionMode Mode { get; init; } = AttackSelectionMode.Single;

    /// <summary>
    /// Max recipients for this attack vector.
    /// null = preset/default behavior, 1 = primary only, 0 = unlimited.
    /// </summary>
    public int? TargetLimit { get; init; }

    public static AttackSelectionConfig Default => new();
}

/// <summary>
/// Area vector used by area/line recipient selection.
/// </summary>
public record AttackAreaConfig
{
    public AttackAreaShape Shape { get; init; } = AttackAreaShape.Sphere;
    public Vector3 Size { get; init; } = new(1f, 1f, 1f);
    /// <summary>Single-target damage-shape debug radius. 0 = derive from unit geometry.</summary>
    public float SingleTargetRadius { get; init; }
    public float LineLength { get; init; }
    public float LineHalfWidth { get; init; }
    public float ForwardOffset { get; init; }

    public static AttackAreaConfig Default => new();
}

/// <summary>
/// Propagation vector used for chain/pierce behavior.
/// </summary>
public record AttackPropagationConfig
{
    public AttackPropagationMode Mode { get; init; } = AttackPropagationMode.None;
    public int ChainMaxJumps { get; init; }
    public float ChainJumpRadius { get; init; }

    public static AttackPropagationConfig Default => new();
}

/// <summary>
/// Rule vector for recipient and trigger behavior.
/// </summary>
public record AttackRulesConfig
{
    /// <summary>Whether non-single attack vectors can include summoners.</summary>
    public bool IncludeSummonerTargets { get; init; }

    /// <summary>Whether the same recipient can be hit multiple times in one attack resolve.</summary>
    public bool AllowRepeatHits { get; init; }

    /// <summary>Which recipients can fire trigger hooks.</summary>
    public AttackTriggerMode TriggerMode { get; init; } = AttackTriggerMode.PrimaryOnly;

    /// <summary>
    /// Melee approach model used by commit targeting/lifecycle logic.
    /// </summary>
    public MeleeEngagementModel MeleeEngagementModel { get; init; } =
        MeleeEngagementModel.Direct;

    public static AttackRulesConfig Default => new();
}
