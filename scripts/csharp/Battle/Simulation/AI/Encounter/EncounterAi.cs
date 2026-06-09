using System;
using System.Collections.Generic;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.AI;

public static class EncounterAi
{
    private const float RecentPlayerDamageWindowSeconds = 5f;
    private const float PlayerSideDangerX = 0f;
    private const float NearPlayerSummonerDistance = 9f;

    public static void Tick(MatchState state, float fixedDelta)
    {
        var config = state.EncounterAi;
        if (config == null)
        {
            SimAi.Tick(state, fixedDelta);
            return;
        }

        if (state.Phase != GamePhase.Battle && state.Phase != GamePhase.Preparation)
            return;

        UpdatePlayerDamageTelemetry(state, config);
        config.LastDangerState = EvaluateDangerState(state, config.Team);

        if (config.UseTrainerAi)
            SimAi.Tick(state, fixedDelta);

        foreach (var rule in config.Rules)
        {
            if (!rule.IsActive(state.MatchTime))
                continue;

            switch (rule.Kind)
            {
                case EncounterRuleKind.EventRule:
                    TickEventRule(state, config, rule);
                    break;

                case EncounterRuleKind.RhythmRule:
                    TickRhythmRule(state, config, rule);
                    break;

                case EncounterRuleKind.BehaviorRule:
                    TickBehaviorRule(state, config, rule);
                    break;

                case EncounterRuleKind.PoolRule:
                case EncounterRuleKind.CapRule:
                case EncounterRuleKind.PlacementRule:
                    break;

                case EncounterRuleKind.HazardRule:
                case EncounterRuleKind.ObjectiveRule:
                case EncounterRuleKind.DialogueRule:
                case EncounterRuleKind.ArenaModifierRule:
                case EncounterRuleKind.RewardPreviewRule:
                    Simulation.Log?.Invoke(
                        $"[EncounterAI] Rule '{rule.Id}' kind {rule.Kind} is reserved for future work."
                    );
                    break;
            }
        }
    }

    public static EncounterDangerState EvaluateDangerState(MatchState state, int enemyTeam)
    {
        int playerTeam = MatchState.GetEnemyTeam(enemyTeam);
        var enemyThreats = CountMeaningfulThreats(state, enemyTeam);
        var playerThreats = CountMeaningfulThreats(state, playerTeam);
        var enemyPastMidfield = AnyMeaningfulThreatPastMidfield(state, enemyTeam);
        var enemyNearPlayer = AnyMeaningfulThreatNearSummoner(state, enemyTeam, playerTeam);

        int score = 0;
        if (enemyThreats >= 3)
            score++;
        if (enemyPastMidfield)
            score++;
        if (enemyNearPlayer)
            score++;
        if (enemyThreats >= 2 && playerThreats <= 1)
            score++;

        var config = state.EncounterAi;
        if (
            config != null
            && state.MatchTime - config.LastPlayerDamageTime <= RecentPlayerDamageWindowSeconds
        )
            score++;

        return score switch
        {
            <= 0 => EncounterDangerState.Calm,
            1 => EncounterDangerState.Pressured,
            2 => EncounterDangerState.Danger,
            _ => EncounterDangerState.Overwhelmed,
        };
    }

    public static EncounterActionResult ExecuteAction(
        MatchState state,
        EncounterAiConfig config,
        EncounterAction action,
        EncounterRule? sourceRule = null
    )
    {
        if (!action.Enabled)
            return EncounterActionResult.Blocked("action disabled");

        if (!CanPassPacing(state, config, action, sourceRule, out var blockReason))
            return EncounterActionResult.Blocked(blockReason);

        var result = action.Kind switch
        {
            EncounterActionKind.PlayCard => ExecutePlayCardAction(state, config, action, sourceRule),
            EncounterActionKind.SpawnUnits => ExecuteSpawnUnitsAction(
                state,
                config,
                action,
                sourceRule
            ),
            EncounterActionKind.SetBehavior => ExecuteSetBehaviorAction(state, action),
            EncounterActionKind.SetRuleEnabled => ExecuteSetRuleEnabledAction(config, action),
            EncounterActionKind.SpawnHazard
            or EncounterActionKind.ApplyArenaModifier
            or EncounterActionKind.SetObjectiveState
            or EncounterActionKind.GrantTemporaryCard
            or EncounterActionKind.ModifyManaRule
            or EncounterActionKind.TriggerDialogueBeat
            or EncounterActionKind.SetWinConditionProgress => EncounterActionResult.Unsupported(
                action.Kind
            ),
            _ => EncounterActionResult.Unsupported(action.Kind),
        };

        if (result.Status == EncounterActionStatus.Executed)
            config.LastActionTime = state.MatchTime;

        if (result.Status == EncounterActionStatus.Unsupported)
            Simulation.Log?.Invoke($"[EncounterAI] {result.Reason}");

        return result;
    }

    private static void TickEventRule(
        MatchState state,
        EncounterAiConfig config,
        EncounterRule rule
    )
    {
        if (rule.Fired)
            return;

        bool anyExecuted = false;
        bool anyBlocked = false;
        foreach (var action in rule.Actions)
        {
            var result = ExecuteAction(state, config, action, rule);
            if (result.Status == EncounterActionStatus.Executed)
                anyExecuted = true;
            if (result.Status == EncounterActionStatus.Blocked)
                anyBlocked = true;
        }

        rule.Fired = anyExecuted || !anyBlocked;
        if (anyExecuted)
            rule.ExecutionCount++;
    }

    private static void TickRhythmRule(
        MatchState state,
        EncounterAiConfig config,
        EncounterRule rule
    )
    {
        if (rule.MaxExecutions.HasValue && rule.ExecutionCount >= rule.MaxExecutions.Value)
            return;

        float interval = rule.IntervalSeconds ?? ResolveRhythmInterval(rule.Rhythm);
        if (state.MatchTime - rule.LastExecutionTime < interval)
            return;

        bool anyExecuted = false;
        foreach (var action in rule.Actions)
        {
            var result = ExecuteAction(state, config, action, rule);
            if (result.Status == EncounterActionStatus.Executed)
                anyExecuted = true;
        }

        rule.LastExecutionTime = state.MatchTime;
        if (anyExecuted)
            rule.ExecutionCount++;
    }

    private static void TickBehaviorRule(MatchState state, EncounterAiConfig config, EncounterRule rule)
    {
        var summoner = state.Summoners[config.Team];
        summoner.Ai ??= new AiConfig();
        if (rule.AiType.HasValue)
            summoner.Ai.Type = rule.AiType.Value;
        if (rule.Personality.HasValue)
            summoner.Ai.Personality = rule.Personality.Value;
        if (rule.PlayIntervalMin.HasValue)
            summoner.Ai.PlayIntervalMin = rule.PlayIntervalMin.Value;
        if (rule.PlayIntervalMax.HasValue)
            summoner.Ai.PlayIntervalMax = rule.PlayIntervalMax.Value;
        rule.Enabled = false;
    }

    private static EncounterActionResult ExecutePlayCardAction(
        MatchState state,
        EncounterAiConfig config,
        EncounterAction action,
        EncounterRule? sourceRule
    )
    {
        if (action.Source != EncounterActionSource.Trainer)
            return EncounterActionResult.Blocked("PlayCard requires Trainer source.");

        var summoner = state.Summoners[action.Team];
        if (!summoner.IsAlive || summoner.IsCasting)
            return EncounterActionResult.Blocked("trainer unavailable");

        var cardId = ResolveActionCardId(state, config, action, sourceRule, requireInHand: true);
        if (string.IsNullOrWhiteSpace(cardId))
            return EncounterActionResult.NoValidTarget("no playable card");

        int cardIndex = FindCardInHand(summoner, cardId);
        if (cardIndex < 0)
            return EncounterActionResult.NoValidTarget("card not in hand");

        if (
            state.CardDataMap.TryGetValue(cardId, out var cardData)
            && cardData.ManaCost > summoner.Mana
        )
            return EncounterActionResult.Blocked("not enough mana");

        state.PendingCommandBuffer.Add(
            new PlayCardCommand(action.Team, cardIndex, ResolvePosition(state, action.Team, action))
            {
                ExecuteFrame = state.FrameNumber + 1,
            }
        );
        return EncounterActionResult.Executed();
    }

    private static EncounterActionResult ExecuteSpawnUnitsAction(
        MatchState state,
        EncounterAiConfig config,
        EncounterAction action,
        EncounterRule? sourceRule
    )
    {
        if (action.Source != EncounterActionSource.Encounter)
            return EncounterActionResult.Blocked("SpawnUnits requires Encounter source in v1.");

        var cardId = ResolveActionCardId(state, config, action, sourceRule, requireInHand: false);
        if (string.IsNullOrWhiteSpace(cardId))
            return EncounterActionResult.NoValidTarget("no spawn card available");

        if (!state.CardDataMap.ContainsKey(cardId))
            return EncounterActionResult.NoValidTarget($"card data missing for {cardId}");

        var positions = action.Positions.Count > 0
            ? action.Positions
            : [ResolvePosition(state, action.Team, action)];

        foreach (var position in positions)
        {
            state.PendingCommandBuffer.Add(
                new SpawnUnitCommand(new SimCardCatalogId(cardId), action.Team, position)
                {
                    ActivateImmediately = action.ActivateImmediately,
                    ExecuteFrame = state.FrameNumber + 1,
                }
            );
        }

        return EncounterActionResult.Executed();
    }

    private static EncounterActionResult ExecuteSetBehaviorAction(
        MatchState state,
        EncounterAction action
    )
    {
        var summoner = state.Summoners[action.Team];
        summoner.Ai ??= new AiConfig();
        if (action.AiType.HasValue)
            summoner.Ai.Type = action.AiType.Value;
        if (action.Personality.HasValue)
            summoner.Ai.Personality = action.Personality.Value;
        if (action.PlayIntervalMin.HasValue)
            summoner.Ai.PlayIntervalMin = action.PlayIntervalMin.Value;
        if (action.PlayIntervalMax.HasValue)
            summoner.Ai.PlayIntervalMax = action.PlayIntervalMax.Value;

        return EncounterActionResult.Executed();
    }

    private static EncounterActionResult ExecuteSetRuleEnabledAction(
        EncounterAiConfig config,
        EncounterAction action
    )
    {
        var rule = config.Rules.Find(candidate => candidate.Id == action.RuleId);
        if (rule == null)
            return EncounterActionResult.NoValidTarget($"rule '{action.RuleId}' not found");

        rule.Enabled = action.Enabled;
        return EncounterActionResult.Executed();
    }

    private static bool CanPassPacing(
        MatchState state,
        EncounterAiConfig config,
        EncounterAction action,
        EncounterRule? sourceRule,
        out string blockReason
    )
    {
        blockReason = "";
        if (
            config.LastDangerState == EncounterDangerState.Overwhelmed
            && !action.AllowWhenOverwhelmed
            && action.Kind is EncounterActionKind.SpawnUnits or EncounterActionKind.PlayCard
        )
        {
            blockReason = "player overwhelmed";
            return false;
        }

        if (
            action.IgnoreCaps
            || action.Kind is not (EncounterActionKind.SpawnUnits or EncounterActionKind.PlayCard)
        )
            return true;

        var cap = ResolveActiveCap(state, config, sourceRule);
        if (cap.HasValue && CountAliveActiveUnits(state, action.Team) >= cap.Value)
        {
            blockReason = $"alive cap {cap.Value} reached";
            return false;
        }

        return true;
    }

    private static int? ResolveActiveCap(
        MatchState state,
        EncounterAiConfig config,
        EncounterRule? sourceRule
    )
    {
        int? cap = sourceRule?.MaxAlive;
        foreach (var rule in config.Rules)
        {
            if (rule.Kind != EncounterRuleKind.CapRule || !rule.IsActive(state.MatchTime))
                continue;
            if (!rule.MaxAlive.HasValue)
                continue;
            cap = cap.HasValue ? Math.Min(cap.Value, rule.MaxAlive.Value) : rule.MaxAlive.Value;
        }
        return cap;
    }

    private static string ResolveActionCardId(
        MatchState state,
        EncounterAiConfig config,
        EncounterAction action,
        EncounterRule? sourceRule,
        bool requireInHand
    )
    {
        if (!string.IsNullOrWhiteSpace(action.CardId))
            return action.CardId;

        var candidates = new List<string>();
        candidates.AddRange(action.CardIds);
        if (sourceRule != null)
            candidates.AddRange(sourceRule.CardPool);
        foreach (var rule in config.Rules)
        {
            if (rule.Kind == EncounterRuleKind.PoolRule && rule.IsActive(state.MatchTime))
                candidates.AddRange(rule.CardPool);
        }

        if (candidates.Count == 0)
            return "";

        if (requireInHand)
        {
            foreach (var candidate in candidates)
            {
                if (FindCardInHand(state.Summoners[action.Team], candidate) >= 0)
                    return candidate;
            }
            return "";
        }

        int index = state.Rng?.Range(0, candidates.Count - 1) ?? 0;
        return candidates[index];
    }

    private static SimVector3 ResolvePosition(
        MatchState state,
        int team,
        EncounterAction action
    )
    {
        if (action.Position.HasValue)
            return action.Position.Value;

        float x = action.Placement switch
        {
            EncounterPlacement.Defensive => team == 0 ? -18f : 18f,
            EncounterPlacement.Aggressive => team == 0 ? -4f : 4f,
            _ => team == 0 ? -10f : 10f,
        };

        float z = state.Rng?.RangeFloat(-4f, 4f) ?? 0f;
        return new SimVector3(x, 0f, z);
    }

    private static int FindCardInHand(SummonerData summoner, string cardId)
    {
        for (int i = 0; i < summoner.Hand.Count; i++)
        {
            if (string.Equals(summoner.Hand[i], cardId, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static float ResolveRhythmInterval(EncounterRhythm rhythm) =>
        rhythm switch
        {
            EncounterRhythm.Sparse => 16f,
            EncounterRhythm.Steady => 10f,
            EncounterRhythm.Frequent => 6f,
            EncounterRhythm.Relentless => 3f,
            _ => 10f,
        };

    private static void UpdatePlayerDamageTelemetry(MatchState state, EncounterAiConfig config)
    {
        var player = state.Summoners[0];
        if (float.IsNaN(config.LastPlayerHp))
        {
            config.LastPlayerHp = player.CurrentHp;
            return;
        }

        if (player.CurrentHp < config.LastPlayerHp - 0.01f)
            config.LastPlayerDamageTime = state.MatchTime;

        config.LastPlayerHp = player.CurrentHp;
    }

    private static int CountMeaningfulThreats(MatchState state, int team)
    {
        int count = 0;
        foreach (var unit in state.Units.Values)
        {
            if (IsMeaningfulThreat(unit, team))
                count++;
        }
        return count;
    }

    private static int CountAliveActiveUnits(MatchState state, int team)
    {
        int count = 0;
        foreach (var unit in state.Units.Values)
        {
            if (
                unit.Team == (Team)team
                && unit.IsAlive
                && unit.ActivationState == ActivationState.Active
            )
                count++;
        }
        return count;
    }

    private static bool AnyMeaningfulThreatPastMidfield(MatchState state, int enemyTeam)
    {
        foreach (var unit in state.Units.Values)
        {
            if (!IsMeaningfulThreat(unit, enemyTeam))
                continue;

            if (enemyTeam == 1 && unit.Position.X < PlayerSideDangerX)
                return true;
            if (enemyTeam == 0 && unit.Position.X > PlayerSideDangerX)
                return true;
        }
        return false;
    }

    private static bool AnyMeaningfulThreatNearSummoner(
        MatchState state,
        int enemyTeam,
        int playerTeam
    )
    {
        var playerPosition = state.Summoners[playerTeam].Position;
        foreach (var unit in state.Units.Values)
        {
            if (!IsMeaningfulThreat(unit, enemyTeam))
                continue;

            if (unit.Position.DistanceTo(playerPosition) <= NearPlayerSummonerDistance)
                return true;
        }
        return false;
    }

    private static bool IsMeaningfulThreat(UnitData unit, int team) =>
        unit.Team == (Team)team
        && unit.IsAlive
        && unit.ActivationState == ActivationState.Active
        && unit.AttackDamage > 0.01f;
}
