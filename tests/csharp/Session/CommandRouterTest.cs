namespace Fateforged.Tests.Session;

using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using GdUnit4;
using Fateforged.Tests.Simulation;
using static GdUnit4.Assertions;

[TestSuite]
public class CommandRouterTest
{
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
