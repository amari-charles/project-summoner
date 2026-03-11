using System;
using Fateforged.Simulation;
using Fateforged.Units;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Builds simulation attack-vector state from unit-definition attack config.
/// </summary>
public static class AttackVectorStateBuilder
{
    public static AttackVectorState Build(AttackVectorConfig? config)
    {
        var source = config ?? AttackVectorConfig.Default;
        var timing = source.Timing ?? AttackTimingConfig.Default;
        var selection = source.Selection ?? AttackSelectionConfig.Default;
        bool hasExplicitTargetLimit = selection.TargetLimit.HasValue;
        var area = source.Area ?? AttackAreaConfig.Default;
        var propagation = source.Propagation ?? AttackPropagationConfig.Default;
        var rules = source.Rules ?? AttackRulesConfig.Default;
        var mapped = new AttackVectorState
        {
            Preset = source.Preset,
            Timing = new AttackTimingState
            {
                WindupSeconds = timing.WindupSeconds,
                ActiveSeconds = timing.ActiveSeconds,
                RecoverySeconds = timing.RecoverySeconds,
                TickIntervalSeconds = timing.TickIntervalSeconds
            },
            DeliveryMode = source.DeliveryMode,
            Selection = new AttackSelectionState
            {
                Mode = selection.Mode,
                TargetLimit = selection.TargetLimit ?? 1
            },
            Area = new AttackAreaState
            {
                Shape = area.Shape,
                Size = new SimVector3(area.Size.X, area.Size.Y, area.Size.Z),
                LineLength = area.LineLength,
                LineHalfWidth = area.LineHalfWidth,
                ForwardOffset = area.ForwardOffset
            },
            Propagation = new AttackPropagationState
            {
                Mode = propagation.Mode,
                ChainMaxJumps = propagation.ChainMaxJumps,
                ChainJumpRadius = propagation.ChainJumpRadius
            },
            Rules = new AttackRulesState
            {
                IncludeSummonerTargets = rules.IncludeSummonerTargets,
                AllowRepeatHits = rules.AllowRepeatHits,
                TriggerMode = rules.TriggerMode
            }
        };

        ApplyPresetDefaults(mapped, mapped.Preset, hasExplicitTargetLimit);
        return mapped;
    }

    private static void ApplyPresetDefaults(
        AttackVectorState attack,
        AttackPreset preset,
        bool hasExplicitTargetLimit)
    {
        switch (preset)
        {
            case AttackPreset.SingleTarget:
                attack.DeliveryMode = AttackDeliveryMode.Instant;
                attack.Selection.Mode = AttackSelectionMode.Single;
                attack.Area.Shape = AttackAreaShape.Sphere;
                attack.Selection.TargetLimit = 1;
                attack.Propagation.Mode = AttackPropagationMode.None;
                attack.Propagation.ChainMaxJumps = 0;
                attack.Propagation.ChainJumpRadius = 0f;
                attack.Area.LineLength = 0f;
                attack.Area.LineHalfWidth = 0f;
                return;

            case AttackPreset.AreaCleave:
                attack.DeliveryMode = AttackDeliveryMode.Instant;
                attack.Selection.Mode = AttackSelectionMode.AreaCollect;
                attack.Area.Shape = AttackAreaShape.Box;
                attack.Propagation.Mode = AttackPropagationMode.None;
                if (!hasExplicitTargetLimit)
                    attack.Selection.TargetLimit = 3;
                return;

            case AttackPreset.LinePierce:
                attack.DeliveryMode = AttackDeliveryMode.Instant;
                attack.Selection.Mode = AttackSelectionMode.LineCollect;
                attack.Area.Shape = AttackAreaShape.Line;
                attack.Propagation.Mode = AttackPropagationMode.Pierce;
                if (attack.Area.LineLength <= 0f)
                    attack.Area.LineLength = 4f;
                if (attack.Area.LineHalfWidth <= 0f)
                    attack.Area.LineHalfWidth = 0.75f;
                if (!hasExplicitTargetLimit)
                    attack.Selection.TargetLimit = 3;
                return;

            case AttackPreset.Chain:
                attack.DeliveryMode = AttackDeliveryMode.Instant;
                attack.Selection.Mode = AttackSelectionMode.ChainHops;
                attack.Area.Shape = AttackAreaShape.Sphere;
                attack.Propagation.Mode = AttackPropagationMode.Chain;
                if (attack.Propagation.ChainMaxJumps <= 0)
                    attack.Propagation.ChainMaxJumps = 2;
                if (attack.Propagation.ChainJumpRadius <= 0f)
                    attack.Propagation.ChainJumpRadius = 4f;
                if (!hasExplicitTargetLimit)
                    attack.Selection.TargetLimit = attack.Propagation.ChainMaxJumps + 1;
                return;

            case AttackPreset.Custom:
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown AttackPreset");
        }
    }
}
