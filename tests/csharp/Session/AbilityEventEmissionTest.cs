namespace Fateforged.Tests.Session;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityEventEmissionTest
{
    [TestCase]
    public void LocalSession_EmitsAbilityAndStatusEvents_FromSimulationTick()
    {
        var state = SimTestHelper.CreateBattleState();
        var taunter = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, hp: 200f);
        taunter.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "taunt_pulse",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.EnemiesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 2.0f,
                Radius = 8.0f,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Taunt,
                        DurationSeconds = 2.0f,
                    },
                ],
            }
        );
        SimTestHelper.CreateMeleeUnit(state, 1, x: 4f, z: 0f, hp: 100f);

        var simulation = new Fateforged.Simulation.Simulation(state);
        var session = new LocalSession(simulation, new CommandRouter(), state);
        var emitted = new List<SimEvent>();
        session.SimEventsEmitted += e => emitted.AddRange(e);

        session.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(emitted.OfType<AbilityActivatedEvent>().Any()).IsTrue();
        AssertThat(emitted.OfType<StatusAppliedEvent>().Any()).IsTrue();
    }
}
