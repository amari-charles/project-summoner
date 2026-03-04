using Fateforged.Constants;
using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.AI;

/// <summary>
/// Smart rule-based AI with strategic decision making.
/// Port of heuristic_ai.gd — all constants and logic preserved.
/// Pure static functions operating on MatchState — no Godot dependencies.
/// </summary>
public static class HeuristicAiStrategy
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    public enum BattlefieldState { LosingBadly, Losing, Even, Winning }

    // =========================================================================
    // CARD SCORING CONSTANTS
    // =========================================================================

    const float ScoreManaEfficiencyBase = 10.0f;
    const float ScoreBaseSummon = 10.0f;
    const float ScoreBaseSpell = 5.0f;
    const float ScoreLosingBadlySummonBonus = 15.0f;
    const float ScoreLosingSummonBonus = 10.0f;
    const float ScoreWinningSummonBonus = 5.0f;
    const float ScoreLosingBadlySpellBonus = 20.0f;
    const float ScoreLosingSpellBonus = 10.0f;
    const float ScoreWinningSpellBonus = 8.0f;
    const float ScoreNoEnemiesSpellPenalty = 10.0f;

    // =========================================================================
    // ENEMY COUNT THRESHOLDS
    // =========================================================================

    const int EnemyCountThresholdLosingBadly = 3;
    const int EnemyCountThresholdLosing = 2;

    // =========================================================================
    // PERSONALITY BONUSES
    // =========================================================================

    const float PersonalityAggressiveSummonBonus = 5.0f;
    const int PersonalityAggressiveCheapThreshold = 3;
    const float PersonalityAggressiveCheapBonus = 3.0f;
    const int PersonalityDefensiveExpensiveThreshold = 4;
    const float PersonalityDefensiveExpensiveBonus = 3.0f;
    const float PersonalitySpellFocusedBonus = 10.0f;
    const float PersonalitySpellFocusedPenalty = 3.0f;

    // =========================================================================
    // BATTLEFIELD STATE THRESHOLDS
    // =========================================================================

    const float StateUnitAdvantageWeight = 0.5f;
    const float StateHpAdvantageWeight = 0.5f;
    const float StateLosingBadlyThreshold = -0.4f;
    const float StateLosingBadlyHpRatio = 0.3f;
    const float StateLosingThreshold = -0.1f;
    const float StateWinningThreshold = 0.2f;

    // =========================================================================
    // DIFFICULTY / RANDOMNESS
    // =========================================================================

    const int DifficultyMax = 6;
    const float DifficultyRandomnessMultiplier = 5.0f;
    const int DifficultyTimingBaseline = 3;
    const float DifficultyTimingScale = 0.1f;

    // =========================================================================
    // PLAY TIMING
    // =========================================================================

    const float TimingLosingBadlyMultiplier = 0.5f;
    const float TimingLosingMultiplier = 0.7f;
    const float TimingWinningMultiplier = 1.3f;

    // =========================================================================
    // SPAWN POSITIONING
    // =========================================================================

    const float SpawnLaneCenterOffsetRatio = -0.1875f;
    const float SpawnLaneWidthRatio = 0.375f;

    const float SpawnZoneDefensiveMin = 0.75f;
    const float SpawnZoneDefensiveMax = 0.95f;
    const float SpawnZoneNeutralMin = 0.30f;
    const float SpawnZoneNeutralMax = 0.60f;
    const float SpawnZoneAggressiveMin = 0.05f;
    const float SpawnZoneAggressiveMax = 0.30f;
    const float SpawnZoneDefaultMin = 0.40f;
    const float SpawnZoneDefaultMax = 0.70f;

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Select the best card index to play from the summoner's hand.
    /// Returns -1 if no card is playable.
    /// </summary>
    public static int SelectCardIndex(MatchState state, SummonerData summoner)
    {
        if (summoner.Hand.Count == 0 || summoner.Ai == null || state.Rng == null)
            return -1;

        int team = (int)summoner.Team;
        var battleState = EvaluateBattlefieldState(state, team, summoner);
        int enemyCount = state.GetAliveActiveUnitsForTeam(MatchState.GetEnemyTeam(team)).Count;
        int mana = (int)summoner.Mana;

        int bestIndex = -1;
        float bestScore = float.MinValue;

        for (int i = 0; i < summoner.Hand.Count; i++)
        {
            var catalogId = summoner.Hand[i];
            if (!state.CardDataMap.TryGetValue(catalogId, out var cardData))
                continue;

            // Check affordability
            if (cardData.ManaCost > mana)
                continue;

            float score = ScoreCard(cardData, battleState, summoner.Ai.Personality,
                summoner.Ai.Difficulty, enemyCount, state.Rng);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Select spawn position based on strategy and battlefield state.
    /// </summary>
    public static SimVector3 SelectSpawnPosition(MatchState state, SummonerData summoner, string catalogId)
    {
        if (summoner.Ai == null || state.Rng == null)
            return summoner.Position;

        int team = (int)summoner.Team;
        bool isSpell = state.CardDataMap.TryGetValue(catalogId, out var cardData) && cardData.IsSpell;
        var battleState = EvaluateBattlefieldState(state, team, summoner);
        string zone = SelectSpawnZone(summoner.Ai.Personality, battleState);

        return GetRandomPositionInZone(zone, team, state.Rng);
    }

    /// <summary>
    /// Compute the next play interval based on battlefield state and difficulty.
    /// </summary>
    public static float ComputeNextPlayInterval(MatchState state, SummonerData summoner)
    {
        if (summoner.Ai == null || state.Rng == null)
            return 3.0f;

        int team = (int)summoner.Team;
        var battleState = EvaluateBattlefieldState(state, team, summoner);

        float baseMin = summoner.Ai.PlayIntervalMin;
        float baseMax = summoner.Ai.PlayIntervalMax;

        // Adjust by battlefield state
        switch (battleState)
        {
            case BattlefieldState.LosingBadly:
                baseMin *= TimingLosingBadlyMultiplier;
                baseMax *= TimingLosingBadlyMultiplier;
                break;
            case BattlefieldState.Losing:
                baseMin *= TimingLosingMultiplier;
                baseMax *= TimingLosingMultiplier;
                break;
            case BattlefieldState.Winning:
                baseMin *= TimingWinningMultiplier;
                baseMax *= TimingWinningMultiplier;
                break;
        }

        // Adjust by difficulty (higher difficulty = faster play)
        float difficultyFactor = 1.0f - (summoner.Ai.Difficulty - DifficultyTimingBaseline) * DifficultyTimingScale;
        baseMin *= difficultyFactor;
        baseMax *= difficultyFactor;

        return state.Rng.RangeFloat(baseMin, baseMax);
    }

    // =========================================================================
    // INTERNALS (public for testing)
    // =========================================================================

    /// <summary>
    /// Evaluate the current battlefield state for a given team.
    /// </summary>
    public static BattlefieldState EvaluateBattlefieldState(MatchState state, int team, SummonerData summoner)
    {
        int ourUnits = state.GetAliveActiveUnitsForTeam(team).Count;
        int enemyTeam = MatchState.GetEnemyTeam(team);
        int enemyUnits = state.GetAliveActiveUnitsForTeam(enemyTeam).Count;

        float ourHpRatio = summoner.MaxHp > 0 ? summoner.CurrentHp / summoner.MaxHp : 1f;
        float enemyHpRatio = state.Summoners[enemyTeam].MaxHp > 0
            ? state.Summoners[enemyTeam].CurrentHp / state.Summoners[enemyTeam].MaxHp
            : 1f;

        float unitAdvantage = ourUnits - enemyUnits;
        float hpAdvantage = ourHpRatio - enemyHpRatio;
        float totalAdvantage = unitAdvantage * StateUnitAdvantageWeight + hpAdvantage * StateHpAdvantageWeight;

        if (totalAdvantage < StateLosingBadlyThreshold || ourHpRatio < StateLosingBadlyHpRatio)
            return BattlefieldState.LosingBadly;
        if (totalAdvantage < StateLosingThreshold)
            return BattlefieldState.Losing;
        if (totalAdvantage > StateWinningThreshold)
            return BattlefieldState.Winning;

        return BattlefieldState.Even;
    }

    /// <summary>
    /// Score a card based on current situation, personality, and difficulty.
    /// </summary>
    public static float ScoreCard(SimCardData card, BattlefieldState battleState,
        AiPersonality personality, int difficulty, int enemyCount, DeterministicRng rng)
    {
        float score = ScoreManaEfficiencyBase - card.ManaCost;

        if (card.IsSpell)
            score += ScoreSpellCard(battleState, enemyCount);
        else
            score += ScoreSummonCard(battleState);

        score += ApplyPersonalityBonus(card, personality);

        // Difficulty affects randomness (higher difficulty = more optimal)
        float randomness = DifficultyRandomnessMultiplier * (DifficultyMax - difficulty);
        score += rng.RangeFloat(-randomness, randomness);

        return score;
    }

    private static float ScoreSummonCard(BattlefieldState state)
    {
        float score = ScoreBaseSummon;
        switch (state)
        {
            case BattlefieldState.LosingBadly:
                score += ScoreLosingBadlySummonBonus;
                break;
            case BattlefieldState.Losing:
                score += ScoreLosingSummonBonus;
                break;
            case BattlefieldState.Winning:
                score += ScoreWinningSummonBonus;
                break;
        }
        return score;
    }

    private static float ScoreSpellCard(BattlefieldState state, int enemyCount)
    {
        float score = ScoreBaseSpell;
        switch (state)
        {
            case BattlefieldState.LosingBadly:
                if (enemyCount > EnemyCountThresholdLosingBadly)
                    score += ScoreLosingBadlySpellBonus;
                break;
            case BattlefieldState.Losing:
                if (enemyCount > EnemyCountThresholdLosing)
                    score += ScoreLosingSpellBonus;
                break;
            case BattlefieldState.Winning:
                score += ScoreWinningSpellBonus;
                break;
        }

        if (enemyCount == 0)
            score -= ScoreNoEnemiesSpellPenalty;

        return score;
    }

    private static float ApplyPersonalityBonus(SimCardData card, AiPersonality personality)
    {
        float bonus = 0f;
        switch (personality)
        {
            case AiPersonality.Aggressive:
                if (!card.IsSpell)
                    bonus += PersonalityAggressiveSummonBonus;
                if (card.ManaCost <= PersonalityAggressiveCheapThreshold)
                    bonus += PersonalityAggressiveCheapBonus;
                break;
            case AiPersonality.Defensive:
                if (card.ManaCost >= PersonalityDefensiveExpensiveThreshold)
                    bonus += PersonalityDefensiveExpensiveBonus;
                break;
            case AiPersonality.SpellFocused:
                if (card.IsSpell)
                    bonus += PersonalitySpellFocusedBonus;
                else
                    bonus -= PersonalitySpellFocusedPenalty;
                break;
        }
        return bonus;
    }

    /// <summary>
    /// Select which spawn zone based on personality and battlefield state.
    /// </summary>
    public static string SelectSpawnZone(AiPersonality personality, BattlefieldState state)
    {
        switch (personality)
        {
            case AiPersonality.Aggressive:
                return state == BattlefieldState.Winning ? "aggressive" : "neutral";
            case AiPersonality.Defensive:
                return state is BattlefieldState.LosingBadly or BattlefieldState.Losing ? "defensive" : "neutral";
            case AiPersonality.SpellFocused:
                return "neutral";
            case AiPersonality.Balanced:
                return state switch
                {
                    BattlefieldState.Losing => "defensive",
                    BattlefieldState.LosingBadly => "defensive",
                    BattlefieldState.Winning => "aggressive",
                    _ => "neutral"
                };
            default:
                return "neutral";
        }
    }

    /// <summary>
    /// Get a random position within a named zone using BattlefieldBounds constants.
    /// </summary>
    public static SimVector3 GetRandomPositionInZone(string zone, int team, DeterministicRng rng)
    {
        float halfWidth = BattlefieldBounds.HalfWidth;
        float halfDepth = BattlefieldBounds.HalfDepth;

        // Determine X range based on zone and team
        float zoneMin, zoneMax;
        switch (zone)
        {
            case "defensive":
                zoneMin = SpawnZoneDefensiveMin;
                zoneMax = SpawnZoneDefensiveMax;
                break;
            case "aggressive":
                zoneMin = SpawnZoneAggressiveMin;
                zoneMax = SpawnZoneAggressiveMax;
                break;
            case "neutral":
                zoneMin = SpawnZoneNeutralMin;
                zoneMax = SpawnZoneNeutralMax;
                break;
            default:
                zoneMin = SpawnZoneDefaultMin;
                zoneMax = SpawnZoneDefaultMax;
                break;
        }

        float x;
        if (team == 1) // Enemy: positive X side
        {
            x = rng.RangeFloat(halfWidth * zoneMin, halfWidth * zoneMax);
        }
        else // Player AI: negative X side
        {
            x = rng.RangeFloat(-halfWidth * zoneMax, -halfWidth * zoneMin);
        }

        // Z position — spawn in the battle lane
        float zCenter = halfDepth * SpawnLaneCenterOffsetRatio;
        float laneSpread = (halfDepth * 2f) * SpawnLaneWidthRatio * 0.5f;
        float z = rng.RangeFloat(zCenter - laneSpread, zCenter + laneSpread);

        return new SimVector3(x, 0f, z);
    }
}
