using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.AI;

/// <summary>
/// Scripted AI for tutorial/event battles.
/// Executes a predetermined sequence of card plays at specific match times.
/// Port of scripted_ai.gd — no timer accumulation, uses MatchState.MatchTime directly.
/// </summary>
public static class ScriptedAiStrategy
{
    /// <summary>
    /// Check if any script steps are due and queue PlayCardCommands for them.
    /// </summary>
    public static void Tick(MatchState state, SummonerData summoner, int team)
    {
        if (summoner.Ai?.Script == null)
            return;

        var script = summoner.Ai.Script;

        while (summoner.AiScriptIndex < script.Length)
        {
            var step = script[summoner.AiScriptIndex];

            if (state.MatchTime < step.TriggerTime)
                break;

            // Find card in hand by catalog ID
            int cardIndex = FindCardInHand(summoner, step.CardId);
            if (cardIndex < 0)
            {
                Simulation.Log?.Invoke($"[ScriptedAI] Card '{step.CardId}' not found in hand for team {team}, skipping step {summoner.AiScriptIndex}");
                summoner.AiScriptIndex++;
                continue;
            }

            // Check affordability
            if (state.CardDataMap.TryGetValue(step.CardId, out var cardData))
            {
                if (cardData.ManaCost > (int)summoner.Mana)
                {
                    Simulation.Log?.Invoke($"[ScriptedAI] Not enough mana for '{step.CardId}' (need {cardData.ManaCost}, have {summoner.Mana})");
                    summoner.AiScriptIndex++;
                    continue;
                }
            }

            var cmd = new PlayCardCommand(team, cardIndex, step.SpawnPosition)
            {
                ExecuteFrame = state.FrameNumber + 1
            };
            state.PendingCommandBuffer.Add(cmd);

            summoner.AiScriptIndex++;
        }
    }

    /// <summary>
    /// Find a card in the summoner's hand by catalog ID (case-insensitive).
    /// </summary>
    private static int FindCardInHand(SummonerData summoner, string cardId)
    {
        for (int i = 0; i < summoner.Hand.Count; i++)
        {
            if (string.Equals(summoner.Hand[i], cardId, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}
