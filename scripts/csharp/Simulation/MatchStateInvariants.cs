using System.Collections.Generic;

namespace Fateforged.Simulation;

/// <summary>
/// Static validator that asserts MatchState is in a viable gameplay state
/// after initialization. Used in tests and as a debug-mode runtime check.
///
/// Catches bugs like init_as_client() skipping RegisterSummoner(),
/// which would leave summoners with 0/0 HP → instant death.
/// </summary>
public static class MatchStateInvariants
{
    /// <summary>
    /// Validate that MatchState is in a viable post-init state.
    /// Returns a list of violation messages. Empty list = valid.
    /// </summary>
    public static List<string> ValidatePostInit(MatchState state)
    {
        var violations = new List<string>();

        for (int team = 0; team < state.Summoners.Length; team++)
        {
            var summoner = state.Summoners[team];

            if (summoner.MaxHp <= 0)
                violations.Add($"Summoner[{team}].MaxHp is {summoner.MaxHp}, expected > 0");

            if (summoner.CurrentHp <= 0)
                violations.Add($"Summoner[{team}].CurrentHp is {summoner.CurrentHp}, expected > 0");

            if (summoner.MaxMana <= 0)
                violations.Add($"Summoner[{team}].MaxMana is {summoner.MaxMana}, expected > 0");

            if (!summoner.IsAlive)
                violations.Add($"Summoner[{team}].IsAlive is false, expected true");
        }

        return violations;
    }
}
