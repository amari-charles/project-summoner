namespace Fateforged.Simulation;

/// <summary>
/// Result of evaluating a win condition.
/// Null means the condition is not yet met.
/// </summary>
public class WinConditionResult
{
    public int WinnerTeam { get; }
    public string Reason { get; }

    public WinConditionResult(int winnerTeam, string reason)
    {
        WinnerTeam = winnerTeam;
        Reason = reason;
    }
}

/// <summary>
/// Interface for evaluating win conditions inside Simulation.Tick() (step 10).
/// Implementations are pure — they read MatchState and return a result.
/// </summary>
public interface IWinCondition
{
    /// <summary>
    /// Evaluate whether the win condition is met.
    /// Returns null if not met, or a WinConditionResult with the winner.
    /// </summary>
    WinConditionResult? Evaluate(MatchState state);
}

/// <summary>
/// Default win condition: destroy the enemy summoner.
/// Game ends when either summoner's HP reaches 0 (already handled by SimBehavior
/// emitting GameOverEvent). This acts as a safety net evaluated at step 10.
/// </summary>
public class DestroySummonerWinCondition : IWinCondition
{
    public WinConditionResult? Evaluate(MatchState state)
    {
        for (int i = 0; i < state.Summoners.Length; i++)
        {
            if (!state.Summoners[i].IsAlive)
            {
                int winner = i == 0 ? 1 : 0;
                return new WinConditionResult(winner, "Summoner destroyed");
            }
        }
        return null;
    }
}

/// <summary>
/// Survive for a specified duration. Player (team 0) wins if MatchTime >= TimeLimit.
/// If the player's summoner dies before the timer, they lose.
/// </summary>
public class SurviveTimeWinCondition : IWinCondition
{
    public float TimeLimit { get; }

    public SurviveTimeWinCondition(float timeLimit)
    {
        TimeLimit = timeLimit;
    }

    public WinConditionResult? Evaluate(MatchState state)
    {
        // Check summoner deaths first (either side)
        for (int i = 0; i < state.Summoners.Length; i++)
        {
            if (!state.Summoners[i].IsAlive)
            {
                int winner = i == 0 ? 1 : 0;
                return new WinConditionResult(winner, "Summoner destroyed");
            }
        }

        // Survived long enough — player wins
        if (state.MatchTime >= TimeLimit)
        {
            return new WinConditionResult(0, "Survived");
        }

        return null;
    }
}

/// <summary>
/// Destroy enemy summoner within a time limit. Player (team 0) loses on timeout.
/// </summary>
public class TimedDestroyWinCondition : IWinCondition
{
    public float TimeLimit { get; }

    public TimedDestroyWinCondition(float timeLimit)
    {
        TimeLimit = timeLimit;
    }

    public WinConditionResult? Evaluate(MatchState state)
    {
        // Check summoner deaths first
        for (int i = 0; i < state.Summoners.Length; i++)
        {
            if (!state.Summoners[i].IsAlive)
            {
                int winner = i == 0 ? 1 : 0;
                return new WinConditionResult(winner, "Summoner destroyed");
            }
        }

        // Time ran out — player loses
        if (state.MatchTime >= TimeLimit)
        {
            return new WinConditionResult(1, "Time expired");
        }

        return null;
    }
}

/// <summary>
/// Kill a target number of enemy units. Player (team 0) wins when KillCount >= Target.
/// Summoner death still ends the game immediately.
/// </summary>
public class KillCountWinCondition : IWinCondition
{
    public int KillTarget { get; }

    public KillCountWinCondition(int killTarget)
    {
        KillTarget = killTarget;
    }

    public WinConditionResult? Evaluate(MatchState state)
    {
        // Check summoner deaths first
        for (int i = 0; i < state.Summoners.Length; i++)
        {
            if (!state.Summoners[i].IsAlive)
            {
                int winner = i == 0 ? 1 : 0;
                return new WinConditionResult(winner, "Summoner destroyed");
            }
        }

        // Kill target reached
        if (state.KillCount >= KillTarget)
        {
            return new WinConditionResult(0, "Kill target reached");
        }

        return null;
    }
}

/// <summary>
/// Factory for creating IWinCondition from string identifiers (matches WinConditionIDs.gd).
/// </summary>
public static class WinConditionFactory
{
    // Constants matching WinConditionIDs.gd
    public const string DESTROY_BASE = "destroy_base";
    public const string SURVIVE_TIME = "survive_time";
    public const string TIMED_DESTROY = "timed_destroy";
    public const string KILL_COUNT = "kill_count";

    /// <summary>
    /// Create a win condition from its string identifier and parameters from MatchState.
    /// </summary>
    public static IWinCondition Create(MatchState state)
    {
        return state.WinCondition switch
        {
            SURVIVE_TIME => new SurviveTimeWinCondition(state.WinConditionTimeLimit),
            TIMED_DESTROY => new TimedDestroyWinCondition(state.WinConditionTimeLimit),
            KILL_COUNT => new KillCountWinCondition(state.WinConditionKillTarget),
            _ => new DestroySummonerWinCondition()
        };
    }
}
