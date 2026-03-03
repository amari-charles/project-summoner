using System.Collections.Generic;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.AI;

/// <summary>
/// AI tick entry point. Called by Simulation.Tick() as step 1.5
/// (after frame increment, before command drain).
/// Iterates AI-controlled summoners, dispatches to strategy, queues PlayCardCommands.
/// </summary>
public static class SimAi
{
    /// <summary>
    /// Tick all AI-controlled summoners. Produces PlayCardCommands into PendingCommandBuffer.
    /// Only runs during Battle phase (AI doesn't play during Preparation).
    /// </summary>
    public static void Tick(MatchState state, float fixedDelta)
    {
        if (state.Phase != Enums.GamePhase.Battle)
            return;

        for (int team = 0; team < state.Summoners.Length; team++)
        {
            var summoner = state.Summoners[team];
            if (summoner.Ai == null || !summoner.IsAlive || summoner.IsCasting)
                continue;

            switch (summoner.Ai.Type)
            {
                case AiType.Heuristic:
                    TickTimerAi(state, summoner, team, fixedDelta, HeuristicAiStrategy.SelectCardIndex, HeuristicAiStrategy.SelectSpawnPosition);
                    break;
                case AiType.Simple:
                    TickTimerAi(state, summoner, team, fixedDelta, SimpleAiStrategy.SelectCardIndex, SimpleAiStrategy.SelectSpawnPosition);
                    break;
                case AiType.Scripted:
                    ScriptedAiStrategy.Tick(state, summoner, team);
                    break;
            }
        }
    }

    /// <summary>
    /// Timer-based AI tick shared by Heuristic and Simple strategies.
    /// Accumulates time, fires when threshold reached, picks card + position, queues command.
    /// </summary>
    private static void TickTimerAi(
        MatchState state, SummonerData summoner, int team, float fixedDelta,
        SelectCardFunc selectCard, SelectPositionFunc selectPosition)
    {
        summoner.AiPlayTimer += fixedDelta;

        if (summoner.AiPlayTimer < summoner.AiNextPlayTime)
            return;

        // Time to play — select card
        int cardIndex = selectCard(state, summoner);
        if (cardIndex < 0)
        {
            // No playable card, reset timer and try again later
            summoner.AiPlayTimer = 0f;
            summoner.AiNextPlayTime = ComputeNextInterval(state, summoner);
            return;
        }

        var catalogId = summoner.Hand[cardIndex];
        var spawnPosition = selectPosition(state, summoner, catalogId);

        var cmd = new PlayCardCommand(team, cardIndex, spawnPosition)
        {
            ExecuteFrame = state.FrameNumber + 1
        };
        state.PendingCommandBuffer.Add(cmd);

        // Reset timer
        summoner.AiPlayTimer = 0f;
        summoner.AiNextPlayTime = ComputeNextInterval(state, summoner);
    }

    public delegate int SelectCardFunc(MatchState state, SummonerData summoner);
    public delegate SimVector3 SelectPositionFunc(MatchState state, SummonerData summoner, string catalogId);

    /// <summary>
    /// Initialize AI timer for a summoner (called once at config time).
    /// </summary>
    public static void InitializeTimer(MatchState state, SummonerData summoner)
    {
        summoner.AiPlayTimer = 0f;
        summoner.AiNextPlayTime = ComputeNextInterval(state, summoner);
    }

    /// <summary>
    /// Compute the next play interval. Delegates to HeuristicAiStrategy for state-aware
    /// timing, or falls back to simple random range.
    /// </summary>
    private static float ComputeNextInterval(MatchState state, SummonerData summoner)
    {
        if (summoner.Ai == null || state.Rng == null)
            return 3.0f;

        if (summoner.Ai.Type == AiType.Heuristic)
            return HeuristicAiStrategy.ComputeNextPlayInterval(state, summoner);

        return state.Rng.RangeFloat(summoner.Ai.PlayIntervalMin, summoner.Ai.PlayIntervalMax);
    }
}
