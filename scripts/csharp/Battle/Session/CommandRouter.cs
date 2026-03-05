using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Constants;
using System.Collections.Generic;

namespace Fateforged.Session;

/// <summary>
/// Validates ALL commands before they reach the simulation, regardless of session type.
/// Target version of RequestValidator — validates against MatchState directly
/// (no MatchSession or SimulationNode dependency).
/// </summary>
public class CommandRouter
{
    private const float MinPlayCardIntervalSeconds = 0.05f;

    public readonly record struct ValidationResult(bool IsValid, string Reason);
    public static readonly ValidationResult Valid = new(true, "");
    private readonly Dictionary<int, float> _lastAcceptedPlayTimeByTeam = new();

    public ValidationResult Validate(ICommand command, MatchState state)
    {
        return command switch
        {
            PlayCardCommand play => ValidatePlayCard(play, state),
            SpawnUnitCommand spawn => ValidateSpawnUnit(spawn, state),
            ForfeitCommand forfeit => ValidateForfeit(forfeit, state),
            _ => new ValidationResult(false, $"Unknown command type: {command.GetType().Name}")
        };
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

        if (!BattlefieldBounds.IsInBounds(play.SpawnPosition))
            return new ValidationResult(false, "Spawn position out of battlefield bounds");

        if (!cardData.IsSpell && !BattlefieldBounds.IsValidSpawnPositionForTeam(play.SpawnPosition, play.Team))
            return new ValidationResult(false, "Spawn position outside team spawn zone");

        if (IsRateLimited(play.Team, state.MatchTime))
            return new ValidationResult(false, "Command rate limit exceeded");

        _lastAcceptedPlayTimeByTeam[play.Team] = state.MatchTime;
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

    private bool IsRateLimited(int team, float nowSeconds)
    {
        if (!_lastAcceptedPlayTimeByTeam.TryGetValue(team, out var lastAcceptedSeconds))
            return false;

        // Match time can reset between sessions while reusing this router instance.
        if (nowSeconds < lastAcceptedSeconds)
            return false;

        return nowSeconds - lastAcceptedSeconds < MinPlayCardIntervalSeconds;
    }
}
