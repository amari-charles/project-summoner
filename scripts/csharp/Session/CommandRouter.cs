using Fateforged.Simulation;

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
        throw new System.NotImplementedException();
    }
}
