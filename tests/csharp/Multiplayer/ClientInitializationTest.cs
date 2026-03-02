namespace ProjectSummoner.Tests.Multiplayer;

using GdUnit4;
using Fateforged.Simulation;
using ProjectSummoner.Units;
using static GdUnit4.Assertions;

/// <summary>
/// Validates that MatchState is in a viable gameplay state after initialization.
/// Catches bugs like init_as_client() skipping RegisterSummoner(), which leaves
/// summoners with 0/0 HP → instant death.
/// </summary>
[TestSuite]
public class ClientInitializationTest
{
    /// <summary>
    /// Simulate the RegisterSummoner path with realistic defaults.
    /// MatchStateInvariants should pass.
    /// </summary>
    [TestCase]
    public void RegisterSummoner_WithRealisticValues_PassesInvariants()
    {
        var state = new MatchState
        {
            Phase = GamePhase.Preparation,
            PrepTimeRemaining = 30f,
            Rng = new DeterministicRng(42)
        };

        // Simulate what RegisterSummoner does for both teams
        RegisterSummonerOnState(state, team: 0, hp: 100f, maxHp: 100f, mana: 5f, maxMana: 10f);
        RegisterSummonerOnState(state, team: 1, hp: 100f, maxHp: 100f, mana: 5f, maxMana: 10f);

        var violations = MatchStateInvariants.ValidatePostInit(state);

        AssertThat(violations)
            .OverrideFailureMessage($"Expected no violations but got: {string.Join("; ", violations)}")
            .IsEmpty();
    }

    /// <summary>
    /// Skip RegisterSummoner entirely — MatchState defaults leave summoners
    /// with 0 HP / 0 Mana / IsAlive=true (but effectively dead on first tick).
    /// MatchStateInvariants should catch this.
    /// </summary>
    [TestCase]
    public void SkippingRegisterSummoner_FailsInvariants()
    {
        var state = new MatchState
        {
            Phase = GamePhase.Preparation,
            PrepTimeRemaining = 30f,
            Rng = new DeterministicRng(42)
        };

        // Do NOT register summoners — simulates the bug where
        // init_as_client() skipped RegisterSummoner()

        var violations = MatchStateInvariants.ValidatePostInit(state);

        // Should have violations for both summoners: MaxHp, CurrentHp, MaxMana
        AssertThat(violations.Count).IsGreaterEqual(6);
        AssertThat(violations).Contains("Summoner[0].MaxHp is 0, expected > 0");
        AssertThat(violations).Contains("Summoner[1].MaxHp is 0, expected > 0");
    }

    /// <summary>
    /// Partial initialization — only one summoner registered.
    /// Should fail for the unregistered summoner.
    /// </summary>
    [TestCase]
    public void PartialRegistration_FailsForMissingSummoner()
    {
        var state = new MatchState
        {
            Phase = GamePhase.Preparation,
            PrepTimeRemaining = 30f,
            Rng = new DeterministicRng(42)
        };

        // Only register team 0
        RegisterSummonerOnState(state, team: 0, hp: 100f, maxHp: 100f, mana: 5f, maxMana: 10f);

        var violations = MatchStateInvariants.ValidatePostInit(state);

        // Team 0 should pass, team 1 should fail
        AssertThat(violations.Count).IsGreaterEqual(3);
        AssertThat(violations).Contains("Summoner[1].MaxHp is 0, expected > 0");
        AssertThat(violations).Contains("Summoner[1].CurrentHp is 0, expected > 0");
        AssertThat(violations).Contains("Summoner[1].MaxMana is 0, expected > 0");
    }

    /// <summary>
    /// Helper that mirrors what SimulationNode.RegisterSummoner does to MatchState.
    /// </summary>
    private static void RegisterSummonerOnState(MatchState state, int team, float hp, float maxHp, float mana, float maxMana)
    {
        var summoner = state.Summoners[team];
        summoner.Team = (Team)team;
        summoner.CurrentHp = hp;
        summoner.MaxHp = maxHp;
        summoner.Mana = mana;
        summoner.MaxMana = maxMana;
        summoner.IsAlive = true;
        summoner.CastSpeed = 1.0f;
        summoner.MaxHandSize = 4;
    }
}
