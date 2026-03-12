namespace Fateforged.Tests.Session;

using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using GdUnit4;
using static GdUnit4.Assertions;
using SimulationRuntime = Fateforged.Simulation.Simulation;

[TestSuite]
public class CommandRouterTest
{
    private const float MinPlayCardIntervalSeconds = 0.05f;
    private CommandRouter _router = null!;
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _router = new CommandRouter();
        _state = SimTestHelper.CreateBattleState();
        // Set up a hand with a known card
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand.Add("test_unit");
        _state.Summoners[0].Mana = 10f;
    }

    // =========================================================================
    // PlayCardCommand validation
    // =========================================================================

    [TestCase]
    public void ValidPlayCard_ReturnsValid()
    {
        var cmd = new PlayCardCommand(0, 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void PlayCard_InvalidTeam_Rejected()
    {
        var cmd = new PlayCardCommand(5, 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Invalid player index");
    }

    [TestCase]
    public void PlayCard_NegativeTeam_Rejected()
    {
        var cmd = new PlayCardCommand(-1, 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
    }

    [TestCase]
    public void PlayCard_GameOver_Rejected()
    {
        _state.Phase = GamePhase.GameOver;
        var cmd = new PlayCardCommand(0, 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("game over");
    }

    [TestCase]
    public void PlayCard_CardIndexOutOfRange_Rejected()
    {
        var cmd = new PlayCardCommand(0, 5, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Card index out of range");
    }

    [TestCase]
    public void PlayCard_NegativeCardIndex_Rejected()
    {
        var cmd = new PlayCardCommand(0, -1, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
    }

    [TestCase]
    public void PlayCard_AlreadyCasting_Rejected()
    {
        _state.Summoners[0].IsCasting = true;
        var cmd = new PlayCardCommand(0, 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Already casting");
    }

    [TestCase]
    public void PlayCard_CardNotInCatalog_Rejected()
    {
        _state.Summoners[0].Hand.Clear();
        _state.Summoners[0].Hand.Add("nonexistent_card");
        var cmd = new PlayCardCommand(0, 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Card data not found");
    }

    [TestCase]
    public void PlayCard_NotEnoughMana_Rejected()
    {
        _state.Summoners[0].Mana = 1f; // card costs 3
        var cmd = new PlayCardCommand(0, 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Not enough mana");
    }

    [TestCase]
    public void PlayCard_SpawnOutOfBounds_Rejected()
    {
        var cmd = new PlayCardCommand(0, 0, new SimVector3(999f, 0f, 0f));
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("out of battlefield bounds");
    }

    [TestCase]
    public void PlayCard_SpawnOnEnemySide_RejectedForSummon()
    {
        var cmd = new PlayCardCommand(0, 0, new SimVector3(5f, 0f, 0f));
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("outside team spawn zone");
    }

    [TestCase]
    public void PlayCard_Spell_AllowsEnemySideTarget()
    {
        _state.CardDataMap.Clear();
        _state.Summoners[0].Hand.Clear();

        var spell = SimTestHelper.CreateSpellCard("test_spell", manaCost: 3);
        _state.CardDataMap["test_spell"] = spell;
        _state.Summoners[0].Hand.Add("test_spell");
        _state.Summoners[0].Mana = 10f;

        var cmd = new PlayCardCommand(0, 0, new SimVector3(5f, 0f, 0f));
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void PlayCard_RateLimited_RejectedForRapidRepeat()
    {
        _state.FrameNumber = 100;

        var first = new PlayCardCommand(0, 0, SimVector3.Zero);
        var firstResult = _router.Validate(first, _state);
        AssertThat(firstResult.IsValid).IsTrue();

        var second = new PlayCardCommand(0, 0, SimVector3.Zero);
        var secondResult = _router.Validate(second, _state);
        AssertThat(secondResult.IsValid).IsFalse();
        AssertThat(secondResult.Reason).Contains("rate limit");
    }

    [TestCase]
    public void PlayCard_RateLimitWindowElapsed_AcceptsCommand()
    {
        long requiredFrameGap = (long)
            System.Math.Ceiling(MinPlayCardIntervalSeconds / SimulationRuntime.FixedDeltaSeconds);
        _state.FrameNumber = 100;
        var first = _router.Validate(new PlayCardCommand(0, 0, SimVector3.Zero), _state);
        AssertThat(first.IsValid).IsTrue();

        _state.FrameNumber = 100 + requiredFrameGap;
        var second = _router.Validate(new PlayCardCommand(0, 0, SimVector3.Zero), _state);
        AssertThat(second.IsValid).IsTrue();
    }

    [TestCase]
    public void PlayCard_RateLimit_DoesNotDependOnMatchTimeAdvancing()
    {
        long requiredFrameGap = (long)
            System.Math.Ceiling(MinPlayCardIntervalSeconds / SimulationRuntime.FixedDeltaSeconds);
        _state.Phase = GamePhase.Preparation;
        _state.MatchTime = 0f;
        _state.FrameNumber = 10;

        var first = _router.Validate(new PlayCardCommand(0, 0, SimVector3.Zero), _state);
        AssertThat(first.IsValid).IsTrue();

        // Preparation phase can keep MatchTime constant while FrameNumber advances.
        _state.MatchTime = 0f;
        _state.FrameNumber = 10 + requiredFrameGap;
        var second = _router.Validate(new PlayCardCommand(0, 0, SimVector3.Zero), _state);
        AssertThat(second.IsValid).IsTrue();
    }

    // =========================================================================
    // ForfeitCommand validation
    // =========================================================================

    [TestCase]
    public void ValidForfeit_ReturnsValid()
    {
        var cmd = new ForfeitCommand(0);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void Forfeit_InvalidTeam_Rejected()
    {
        var cmd = new ForfeitCommand(5);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Invalid player index");
    }

    [TestCase]
    public void Forfeit_GameAlreadyOver_Rejected()
    {
        _state.Phase = GamePhase.GameOver;
        var cmd = new ForfeitCommand(0);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Game already over");
    }

    // =========================================================================
    // SpawnUnitCommand validation
    // =========================================================================

    [TestCase]
    public void SpawnUnitCommand_AlwaysValid()
    {
        var cmd = new SpawnUnitCommand("test_unit", 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void SpawnUnitCommand_ValidEvenDuringGameOver()
    {
        _state.Phase = GamePhase.GameOver;
        var cmd = new SpawnUnitCommand("test_unit", 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void SpawnUnitCommand_ValidWithUnknownCatalogId()
    {
        var cmd = new SpawnUnitCommand("nonexistent_card", 0, SimVector3.Zero);
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsTrue();
    }

    // =========================================================================
    // Unknown command type
    // =========================================================================

    [TestCase]
    public void UnknownCommand_Rejected()
    {
        var cmd = new TestUnknownCommand();
        var result = _router.Validate(cmd, _state);
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Reason).Contains("Unknown command type");
    }

    private class TestUnknownCommand : ICommand
    {
        public long ExecuteFrame { get; set; }
    }
}
