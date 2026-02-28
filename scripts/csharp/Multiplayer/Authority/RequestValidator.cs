using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Simulation;

namespace Fateforged.Multiplayer.Authority;

/// <summary>
/// Validates client requests before execution.
/// Reads from MatchState via SimulationNode to enforce game rules.
/// </summary>
public class RequestValidator
{
    /// <summary>
    /// Result of a validation check.
    /// </summary>
    public readonly record struct ValidationResult(bool IsValid, string Reason);

    /// <summary>
    /// Successful validation result.
    /// </summary>
    public static readonly ValidationResult Valid = new(true, "");

    /// <summary>
    /// Validate a card play request against MatchState.
    /// </summary>
    public ValidationResult ValidateCardPlay(MatchSession session, CardPlayRequest request)
    {
        // Check player index is valid
        if (request.PlayerIndex < 0 || request.PlayerIndex >= session.PlayerIds.Length)
        {
            return new ValidationResult(false, "Invalid player index");
        }

        // Check card index is non-negative
        if (request.CardIndex < 0)
        {
            return new ValidationResult(false, "Invalid card index");
        }

        // Validate against MatchState if simulation is available
        var sim = SimulationNode.Current;
        if (sim != null)
        {
            var state = sim.State;
            var summoner = state.Summoners[request.PlayerIndex];

            // Check card index is within hand bounds
            if (request.CardIndex >= summoner.Hand.Count)
            {
                return new ValidationResult(false, $"Card index {request.CardIndex} out of range (hand size {summoner.Hand.Count})");
            }

            // Check summoner is not already casting
            if (summoner.IsCasting)
            {
                return new ValidationResult(false, "Already casting");
            }

            // Check mana cost
            var catalogId = summoner.Hand[request.CardIndex];
            if (state.CardDataMap.TryGetValue(catalogId, out var cardData))
            {
                if (summoner.Mana < cardData.ManaCost)
                {
                    return new ValidationResult(false, $"Not enough mana ({summoner.Mana:F1}/{cardData.ManaCost:F1})");
                }
            }
        }

        return Valid;
    }

    /// <summary>
    /// Validate a forfeit request.
    /// </summary>
    public ValidationResult ValidateForfeit(MatchSession session, ForfeitRequest request)
    {
        // Check player index is valid
        if (request.PlayerIndex < 0 || request.PlayerIndex >= session.PlayerIds.Length)
        {
            return new ValidationResult(false, "Invalid player index");
        }

        return Valid;
    }
}
