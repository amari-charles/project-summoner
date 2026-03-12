namespace Fateforged.Tests.Simulation.AI;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class ScriptedAiStrategyTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void Tick_NoScript_DoesNothing()
    {
        var summoner = _state.Summoners[1];
        summoner.Ai = new AiConfig { Type = AiType.Scripted, Script = null };

        int cmdsBefore = _state.PendingCommandBuffer.Count;
        ScriptedAiStrategy.Tick(_state, summoner, 1);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(cmdsBefore);
    }

    [TestCase]
    public void Tick_StepNotYetDue_DoesNothing()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 100;
        summoner.Hand.Add("test_card");
        _state.CardDataMap["test_card"] = SimTestHelper.CreateSummonCard("test_card", manaCost: 3);

        summoner.Ai = new AiConfig
        {
            Type = AiType.Scripted,
            Script = new[] { new ScriptedAiStep(5.0f, "test_card", SimVector3.Zero) },
        };

        _state.MatchTime = 2.0f; // Before trigger time

        ScriptedAiStrategy.Tick(_state, summoner, 1);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void Tick_StepDue_QueuesCommand()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 100;
        summoner.Hand.Add("test_card");
        _state.CardDataMap["test_card"] = SimTestHelper.CreateSummonCard("test_card", manaCost: 3);

        summoner.Ai = new AiConfig
        {
            Type = AiType.Scripted,
            Script = new[] { new ScriptedAiStep(5.0f, "test_card", new SimVector3(10f, 0f, 0f)) },
        };

        _state.MatchTime = 5.5f; // Past trigger time

        ScriptedAiStrategy.Tick(_state, summoner, 1);

        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(1);
        var cmd = _state.PendingCommandBuffer[0] as PlayCardCommand;
        AssertThat(cmd).IsNotNull();
        AssertThat(cmd!.Team).IsEqual(1);
        AssertThat(cmd.CardIndex).IsEqual(0);
    }

    [TestCase]
    public void Tick_AdvancesScriptIndex()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 100;
        summoner.Hand.Add("card_a");
        summoner.Hand.Add("card_b");
        _state.CardDataMap["card_a"] = SimTestHelper.CreateSummonCard("card_a", manaCost: 2);
        _state.CardDataMap["card_b"] = SimTestHelper.CreateSummonCard("card_b", manaCost: 2);

        summoner.Ai = new AiConfig
        {
            Type = AiType.Scripted,
            Script = new[]
            {
                new ScriptedAiStep(1.0f, "card_a", SimVector3.Zero),
                new ScriptedAiStep(10.0f, "card_b", SimVector3.Zero),
            },
        };

        _state.MatchTime = 3.0f; // Past first, before second

        ScriptedAiStrategy.Tick(_state, summoner, 1);

        AssertThat(summoner.AiScriptIndex).IsEqual(1);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(1);
    }

    [TestCase]
    public void Tick_CardNotInHand_SkipsStep()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 100;
        summoner.Hand.Add("other_card");
        _state.CardDataMap["other_card"] = SimTestHelper.CreateSummonCard(
            "other_card",
            manaCost: 2
        );

        summoner.Ai = new AiConfig
        {
            Type = AiType.Scripted,
            Script = new[] { new ScriptedAiStep(1.0f, "missing_card", SimVector3.Zero) },
        };

        _state.MatchTime = 5.0f;

        ScriptedAiStrategy.Tick(_state, summoner, 1);

        AssertThat(summoner.AiScriptIndex).IsEqual(1); // Advanced past missing card
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void Tick_NotEnoughMana_SkipsStep()
    {
        var summoner = _state.Summoners[1];
        summoner.Mana = 0; // No mana
        summoner.Hand.Add("test_card");
        _state.CardDataMap["test_card"] = SimTestHelper.CreateSummonCard("test_card", manaCost: 5);

        summoner.Ai = new AiConfig
        {
            Type = AiType.Scripted,
            Script = new[] { new ScriptedAiStep(1.0f, "test_card", SimVector3.Zero) },
        };

        _state.MatchTime = 5.0f;

        ScriptedAiStrategy.Tick(_state, summoner, 1);

        AssertThat(summoner.AiScriptIndex).IsEqual(1); // Skipped
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }
}
