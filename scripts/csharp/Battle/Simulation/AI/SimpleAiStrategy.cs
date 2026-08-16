using System;
using Fateforged.Constants;
using Fateforged.Simulation.Data;
using Fateforged.Units;

namespace Fateforged.Simulation.AI;

/// <summary>
/// Simple AI that plays random playable cards at random positions.
/// Port of simple_ai.gd — minimal strategy, used as default/fallback.
/// </summary>
public static class SimpleAiStrategy
{
    /// <summary>
    /// Select a random playable card from the summoner's hand.
    /// Returns -1 if no card is playable.
    /// </summary>
    public static int SelectCardIndex(MatchState state, SummonerData summoner)
    {
        if (summoner.Hand.Count == 0 || state.Rng == null)
            return -1;

        int mana = (int)summoner.Mana;

        // Collect playable indices
        int playableCount = 0;
        Span<int> playable = stackalloc int[summoner.Hand.Count];

        for (int i = 0; i < summoner.Hand.Count; i++)
        {
            var catalogId = summoner.Hand[i];
            if (
                state.CardDataMap.TryGetValue(catalogId, out var cardData)
                && cardData.ManaCost <= mana
            )
            {
                playable[playableCount++] = i;
            }
        }

        if (playableCount == 0)
            return -1;

        // Pick random playable card
        int pick = state.Rng.Range(0, playableCount - 1);
        return playable[pick];
    }

    /// <summary>
    /// Select a random position in the team's territory.
    /// </summary>
    public static SimVector3 SelectSpawnPosition(
        MatchState state,
        SummonerData summoner,
        SimCardCatalogId catalogId
    )
    {
        if (state.Rng == null)
            return summoner.Position;

        if (
            state.SummonPlacementMode == SummonPlacementMode.CardRangeFromSummoner
            && state.CardDataMap.TryGetValue(catalogId, out var rangeCard)
            && !rangeCard.IsSpell
        )
            return SummonPlacementRules.SelectRandomPositionWithinCardRange(
                state,
                summoner,
                rangeCard
            );

        float halfWidth = BattlefieldBounds.HalfWidth;
        int team = (int)summoner.Team;

        float x;
        if ((Team)team == Team.Enemy) // Enemy: positive X
            x = state.Rng.RangeFloat(halfWidth * 0.1f, halfWidth * 0.9f);
        else // Player: negative X
            x = state.Rng.RangeFloat(-halfWidth * 0.9f, -halfWidth * 0.1f);

        float z = state.Rng.RangeFloat(
            -BattlefieldBounds.HalfDepth * 0.5f,
            BattlefieldBounds.HalfDepth * 0.5f
        );

        return new SimVector3(x, 0f, z);
    }
}
