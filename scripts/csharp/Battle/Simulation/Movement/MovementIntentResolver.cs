using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Chooses the movement intent strategy for each unit.
/// </summary>
public static class MovementIntentResolver
{
    private static readonly IMovementIntentGenerator _directGenerator = new DirectIntentGenerator();
    private static readonly IMovementIntentGenerator _contextGenerator =
        new ContextIntentGenerator();

    public static MovementIntent Resolve(
        UnitData unit,
        SimBehavior.BehaviorResult behavior,
        MatchState state,
        float delta
    )
    {
        var generator = SelectGenerator(unit);
        var intent = generator.Generate(unit, behavior, state, delta);
        return BlockedNavigationController.Apply(unit, behavior, state, intent, delta);
    }

    private static IMovementIntentGenerator SelectGenerator(UnitData unit)
    {
        return unit.MovementIntentStrategy switch
        {
            MovementIntentStrategy.Context => _contextGenerator,
            _ => _directGenerator,
        };
    }
}
