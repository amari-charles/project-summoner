using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;

namespace Fateforged.Multiplayer.Authority;

/// <summary>
/// Validates client requests before execution.
/// Prevents cheating and ensures game rules are followed.
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
    /// Validate a card play request.
    /// </summary>
    public ValidationResult ValidateCardPlay(MatchSession session, CardPlayRequest request)
    {
        // Check player index is valid
        if (request.PlayerIndex < 0 || request.PlayerIndex >= session.PlayerIds.Length)
        {
            return new ValidationResult(false, "Invalid player index");
        }

        // Check card index is valid (basic bounds check)
        if (request.CardIndex < 0)
        {
            return new ValidationResult(false, "Invalid card index");
        }

        // TODO(Phase-4): Full validation requires integration with Summoner game state
        // - Check if player has this card in hand (request.CardIndex < hand.Count)
        // - Check if player has enough mana (summoner.Mana >= card.ManaCost)
        // - Check if position is in valid spawn zone for this player
        // - Rate limiting to prevent action spam
        // These checks require access to the GDScript Summoner node which will be
        // connected when the multiplayer authority is fully integrated with gameplay.

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
