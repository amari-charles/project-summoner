using Fateforged.Simulation.Data;
using Fateforged.Units;

namespace Fateforged.Simulation;

/// <summary>
/// Type of win condition for a battle. Determines how a match is won or lost.
/// </summary>
public enum WinConditionType
{
    /// <summary>Destroy the enemy summoner. Default mode.</summary>
    DestroySummoner,

    /// <summary>Survive for a specified duration.</summary>
    SurviveTime,

    /// <summary>Destroy enemy summoner within a time limit.</summary>
    TimedDestroy,

    /// <summary>Kill a target number of enemy units.</summary>
    KillCount,
}

/// <summary>
/// Reason a win condition was triggered.
/// </summary>
public static class WinReason
{
    public const string Survived = "Survived";
    public const string TimeExpired = "Time expired";
    public const string KillTargetReached = "Kill target reached";
    public const string SummonerDestroyed = "Summoner destroyed";
}

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
    public WinConditionResult? Evaluate(MatchState state) =>
        WinConditionHelper.CheckSummonerDeath(state);
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
        var death = WinConditionHelper.CheckSummonerDeath(state);
        if (death != null)
            return death;

        // Survived long enough — player wins
        if (state.MatchTime >= TimeLimit)
            return new WinConditionResult((int)Team.Player, WinReason.Survived);

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
        var death = WinConditionHelper.CheckSummonerDeath(state);
        if (death != null)
            return death;

        // Time ran out — player loses
        if (state.MatchTime >= TimeLimit)
            return new WinConditionResult((int)Team.Enemy, WinReason.TimeExpired);

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
        var death = WinConditionHelper.CheckSummonerDeath(state);
        if (death != null)
            return death;

        // Kill target reached
        if (state.KillCount >= KillTarget)
            return new WinConditionResult((int)Team.Player, WinReason.KillTargetReached);

        return null;
    }
}

/// <summary>
/// Shared helpers for win condition evaluation.
/// </summary>
internal static class WinConditionHelper
{
    /// <summary>
    /// Check if any summoner has been destroyed.
    /// Returns the winner (opposing team) or null if both are alive.
    /// </summary>
    internal static WinConditionResult? CheckSummonerDeath(MatchState state)
    {
        for (int i = 0; i < state.Summoners.Length; i++)
        {
            if (!state.Summoners[i].IsAlive)
            {
                int winner = i == (int)Team.Player ? (int)Team.Enemy : (int)Team.Player;
                return new WinConditionResult(winner, WinReason.SummonerDestroyed);
            }
        }
        return null;
    }
}

/// <summary>
/// Factory for creating IWinCondition from WinConditionType and MatchState parameters.
/// </summary>
public static class WinConditionFactory
{
    /// <summary>
    /// Create a win condition from the MatchState's WinConditionType and parameters.
    /// </summary>
    public static IWinCondition Create(MatchState state)
    {
        return state.WinCondition switch
        {
            WinConditionType.SurviveTime => new SurviveTimeWinCondition(
                state.WinConditionTimeLimit
            ),
            WinConditionType.TimedDestroy => new TimedDestroyWinCondition(
                state.WinConditionTimeLimit
            ),
            WinConditionType.KillCount => new KillCountWinCondition(state.WinConditionKillTarget),
            _ => new DestroySummonerWinCondition(),
        };
    }

    /// <summary>
    /// Parse a GDScript win condition string (from WinConditionIDs.gd) into the enum.
    /// Returns DestroySummoner for unrecognized values.
    /// </summary>
    public static WinConditionType Parse(string value)
    {
        return value switch
        {
            "destroy_base" => WinConditionType.DestroySummoner,
            "survive_time" => WinConditionType.SurviveTime,
            "timed_destroy" => WinConditionType.TimedDestroy,
            "kill_count" => WinConditionType.KillCount,
            _ => WinConditionType.DestroySummoner,
        };
    }
}
