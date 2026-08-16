namespace Fateforged.Tests.Simulation;

using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class MoveSummonerCommandTest
{
    [TestCase]
    public void CommandRouter_AcceptsInBoundsMovementForLivingMatch()
    {
        var state = new MatchState { Phase = GamePhase.Battle };
        var router = new CommandRouter();

        var result = router.Validate(
            new MoveSummonerCommand(0, new SimVector3(-10f, 0f, 4f)),
            state
        );

        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void CommandRouter_RejectsMovementOutsideBattlefield()
    {
        var state = new MatchState { Phase = GamePhase.Battle };
        var router = new CommandRouter();

        var result = router.Validate(
            new MoveSummonerCommand(0, new SimVector3(500f, 0f, 0f)),
            state
        );

        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("out of battlefield bounds");
    }

    [TestCase]
    public void Simulation_MoveSummonerCommand_UpdatesPositionAndTargetPointOffset()
    {
        var state = new MatchState { Phase = GamePhase.Preparation };
        state.Summoners[0].Position = new SimVector3(-18f, 0f, 0f);
        state.Summoners[0].TargetPointPosition = new SimVector3(-18f, 3f, 0f);
        state.PendingCommandBuffer.Add(
            new MoveSummonerCommand(0, new SimVector3(-12f, 0f, 5f)) { ExecuteFrame = 1 }
        );
        var simulation = new Fateforged.Simulation.Simulation(state);

        simulation.Tick(Fateforged.Simulation.Simulation.FixedDeltaSeconds);

        AssertThat(state.Summoners[0].Position).IsEqual(new SimVector3(-12f, 0f, 5f));
        AssertThat(state.Summoners[0].TargetPointPosition)
            .IsEqual(new SimVector3(-12f, 3f, 5f));
    }
}
