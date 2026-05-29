using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Subsystems;

namespace Fateforged.Simulation.Effects;

public static class SpellEffectExecutor
{
    public static int Apply(
        MatchState state,
        SimCardCatalogId cardCatalogId,
        EffectApplicationSpec spec,
        IReadOnlyList<UnitData> targets,
        List<SimEvent> events,
        bool delayed = false
    )
    {
        var before = CombatDebugFormatter.CaptureUnits(targets);
        int appliedCount = 0;
        foreach (var target in targets)
        {
            if (SimEffects.ApplyEffect(state, spec, target, events))
                appliedCount++;
        }

        if (
            Simulation.DebugAbilityLogsEnabled
            && !string.IsNullOrWhiteSpace(spec.Context.CardCatalogId)
        )
        {
            Simulation.DebugAbilityLog(
                SpellDebugFormatter.FormatApplication(
                    state,
                    cardCatalogId,
                    spec,
                    targets,
                    before,
                    appliedCount,
                    delayed
                )
            );
        }

        return appliedCount;
    }
}
