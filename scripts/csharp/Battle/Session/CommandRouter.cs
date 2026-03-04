using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Session;

/// <summary>
/// Validates ALL commands before they reach the simulation, regardless of session type.
/// Target version of RequestValidator — validates against MatchState directly
/// (no MatchSession or SimulationNode dependency).
/// </summary>
public class CommandRouter
{
    public readonly record struct ValidationResult(bool IsValid, string Reason);
    public static readonly ValidationResult Valid = new(true, "");

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

    private static ValidationResult ValidatePlayCard(PlayCardCommand play, MatchState state)
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

        return Valid;
    }

    private static ValidationResult ValidateSpawnUnit(SpawnUnitCommand spawn, MatchState state)
    {
        if (spawn.Team < 0 || spawn.Team >= state.Summoners.Length)
            return new ValidationResult(false, "Invalid team index");

        if (state.Phase == GamePhase.GameOver)
            return new ValidationResult(false, "Cannot spawn units after game over");

        if (!state.CardDataMap.ContainsKey(spawn.CatalogId))
            return new ValidationResult(false, $"Unknown catalog ID: {spawn.CatalogId}");

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
}
