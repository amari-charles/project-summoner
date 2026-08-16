using System;
using System.Collections.Generic;
using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using SimulationRuntime = Fateforged.Simulation.Simulation;

namespace Fateforged.Session;

/// <summary>
/// Validates ALL commands before they reach the simulation, regardless of session type.
/// Target version of RequestValidator — validates against MatchState directly
/// (no MatchSession or SimulationNode dependency).
/// </summary>
public class CommandRouter
{
    private const float MinPlayCardIntervalSeconds = 0.05f;
    private static readonly long MinPlayCardIntervalFrames = Math.Max(
        1L,
        (long)Math.Ceiling(MinPlayCardIntervalSeconds / SimulationRuntime.FixedDeltaSeconds)
    );

    public readonly record struct ValidationResult(bool IsValid, string Reason);

    public static readonly ValidationResult Valid = new(true, "");
    private readonly Dictionary<int, long> _lastAcceptedPlayFrameByTeam = new();

    public ValidationResult Validate(ICommand command, MatchState state)
    {
        return command switch
        {
            PlayCardCommand play => ValidatePlayCard(play, state),
            MoveSummonerCommand move => ValidateMoveSummoner(move, state),
            SpawnUnitCommand spawn => ValidateSpawnUnit(spawn, state),
            ForfeitCommand forfeit => ValidateForfeit(forfeit, state),
            _ => new ValidationResult(false, $"Unknown command type: {command.GetType().Name}"),
        };
    }

    private static ValidationResult ValidateMoveSummoner(
        MoveSummonerCommand move,
        MatchState state
    )
    {
        if (move.Team < 0 || move.Team >= state.Summoners.Length)
            return new ValidationResult(false, "Invalid player index");

        if (state.Phase == GamePhase.GameOver)
            return new ValidationResult(false, "Cannot move summoner after game over");

        if (!BattlefieldBounds.IsInBounds(move.TargetPosition))
            return new ValidationResult(false, "Summoner position out of battlefield bounds");

        return Valid;
    }

    private ValidationResult ValidatePlayCard(PlayCardCommand play, MatchState state)
    {
        if (play.Team < 0 || play.Team >= state.Summoners.Length)
            return new ValidationResult(false, "Invalid player index");

        if (state.Phase == GamePhase.GameOver)
            return new ValidationResult(false, "Cannot play cards after game over");

        var summoner = state.Summoners[play.Team];

        if (play.CardIndex < 0 || play.CardIndex >= summoner.Hand.Count)
            return new ValidationResult(false, "Card index out of range");

        if (summoner.IsCasting)
            return new ValidationResult(false, "Already casting");

        var catalogId = summoner.Hand[play.CardIndex];
        if (!state.CardDataMap.TryGetValue(catalogId, out var cardData))
            return new ValidationResult(false, "Card data not found");

        if (summoner.Mana < cardData.ManaCost)
            return new ValidationResult(false, "Not enough mana");

        if (
            !cardData.IsSpell
            && state.SummonPlacementMode == SummonPlacementMode.CardRangeFromSummoner
        )
        {
            play.SpawnPosition = SummonPlacementRules.ResolveCardRangePosition(
                state,
                play.Team,
                cardData,
                play.SpawnPosition
            );
        }

        if (!SummonPlacementRules.IsWithinBattlefield(state, play.SpawnPosition))
            return new ValidationResult(false, "Spawn position out of battlefield bounds");

        if (!cardData.IsSpell && !SummonPlacementRules.IsValid(state, play.Team, cardData, play.SpawnPosition))
        {
            string reason = state.SummonPlacementMode == SummonPlacementMode.CardRangeFromSummoner
                ? "Spawn position outside card summon range"
                : "Spawn position outside team spawn zone";
            return new ValidationResult(false, reason);
        }

        if (IsRateLimited(play.Team, state.FrameNumber))
            return new ValidationResult(false, "Command rate limit exceeded");

        _lastAcceptedPlayFrameByTeam[play.Team] = state.FrameNumber;
        return Valid;
    }

    private static ValidationResult ValidateSpawnUnit(SpawnUnitCommand spawn, MatchState state)
    {
        if (spawn.Team < 0 || spawn.Team >= state.Summoners.Length)
            return new ValidationResult(false, "Invalid team index");

        // SpawnUnitCommand is a debug/event bypass command and intentionally skips
        // normal gameplay validation (game phase, catalog presence, mana, etc.).
        // Execution path resolves missing catalog IDs safely at simulation layer.

        return Valid;
    }

    private static ValidationResult ValidateForfeit(ForfeitCommand forfeit, MatchState state)
    {
        if (forfeit.Team < 0 || forfeit.Team >= state.Summoners.Length)
            return new ValidationResult(false, "Invalid player index");

        if (state.Phase == GamePhase.GameOver)
            return new ValidationResult(false, "Game already over");

        return Valid;
    }

    private bool IsRateLimited(int team, long nowFrame)
    {
        if (!_lastAcceptedPlayFrameByTeam.TryGetValue(team, out var lastAcceptedFrame))
            return false;

        // Frame can reset between sessions while reusing this router instance.
        if (nowFrame < lastAcceptedFrame)
            return false;

        return nowFrame - lastAcceptedFrame < MinPlayCardIntervalFrames;
    }
}
