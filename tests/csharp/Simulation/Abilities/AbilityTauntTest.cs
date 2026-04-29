namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityTauntTest
{
    [TestCase]
    public void TauntPulse_UsesSoftOverride_WhenExistingForcedTargetIsActive()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var taunter = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, hp: 200f, aggroRadius: 20f);
        taunter.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "taunt_pulse",
                Kind = UnitAbilityKind.TauntPulse,
                CooldownSeconds = 3f,
                Radius = 8f,
                DurationSeconds = 2f,
            }
        );

        var enemyA = SimTestHelper.CreateMeleeUnit(state, 1, x: 4f, z: 0f, hp: 100f);
        var enemyB = SimTestHelper.CreateMeleeUnit(state, 1, x: 5f, z: 0f, hp: 100f);
        enemyB.Engagement.ForcedTargetUnitId = 9999;
        enemyB.Engagement.ForcedTargetTimer = 1.5f;

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(enemyA.Engagement.ForcedTargetUnitId.HasValue).IsTrue();
        AssertThat(enemyA.Engagement.ForcedTargetUnitId ?? -1).IsEqual(taunter.UnitId);
        AssertThat(enemyA.Engagement.ForcedTargetTimer).IsGreater(0f);
        AssertThat(enemyB.Engagement.ForcedTargetUnitId.HasValue).IsTrue();
        AssertThat(enemyB.Engagement.ForcedTargetUnitId ?? -1).IsEqual(9999);
        AssertThat(events.OfType<StatusAppliedEvent>().Any(e => e.TargetUnitId == enemyA.UnitId)).IsTrue();
        AssertThat(events.OfType<AbilityActivatedEvent>().Any(e => e.SourceUnitId == taunter.UnitId)).IsTrue();
    }
}
