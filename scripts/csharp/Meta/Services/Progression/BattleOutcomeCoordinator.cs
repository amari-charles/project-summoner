using Fateforged.Domain.Progression;

namespace Fateforged.Meta.Progression;

/// <summary>Single application entry point for terminal battle outcomes.</summary>
public sealed class BattleOutcomeCoordinator
{
    private readonly IProgressionAuthority _authority;

    public BattleOutcomeCoordinator(IProgressionAuthority authority)
    {
        _authority = authority;
    }

    public ProgressionAuthorityResult Report(
        BattleAttemptId attemptId,
        BattleTerminalOutcome outcome
    ) =>
        _authority.CompleteBattleAttempt(
            new CompleteBattleAttemptRequest { AttemptId = attemptId, Outcome = outcome }
        );
}
