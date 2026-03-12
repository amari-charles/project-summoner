using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Movement;

public interface IMovementIntentGenerator
{
    MovementIntent Generate(
        UnitData unit,
        SimBehavior.BehaviorResult behavior,
        MatchState state,
        float delta
    );
}
