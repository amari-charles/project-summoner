namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Meta.Campaign;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class TestArenaWindEarthMissionTest
{
    [TestCase]
    public void ArenaWindEarthNewCards_UsesOnlyNewCardsPlusFireWisp()
    {
        var battle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.ArenaWindEarthNewCards);
        AssertThat(battle).IsNotNull();
        AssertThat(battle!.ScenePath).IsEqual("res://scenes/battle/battlefield/dev/debug_arena.tscn");

        var expected = new HashSet<CardId>
        {
            CardIds.FireWisp,
            CardIds.WindEvasionTank,
            CardIds.WindPushbackUnit,
            CardIds.WindCleaveUnit,
            CardIds.EarthFlatDamageReductionTank,
            CardIds.EarthBulletUnit,
            CardIds.TailWind,
            CardIds.Fortify,
        };

        AssertThat(battle.DevPlayerDeck).IsNotNull();
        var actual = battle.DevPlayerDeck!.Select(entry => entry.CardId).ToHashSet();
        AssertThat(actual.SetEquals(expected)).IsTrue();
    }

    [TestCase]
    public void TestArenaCampaign_IncludesArenaWindEarthNewCardsEvent()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.TestArena);
        AssertThat(campaign).IsNotNull();
        AssertThat(campaign!.EventIds.Contains(EventIds.ArenaWindEarthNewCards)).IsTrue();
    }

    [TestCase]
    public void ArenaAllUnits_UsesEveryActiveCoreElementUnit()
    {
        var battle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.ArenaAllUnits);
        AssertThat(battle).IsNotNull();
        AssertThat(battle!.ScenePath).IsEqual("res://scenes/battle/battlefield/dev/debug_arena.tscn");

        var allowedElements = new HashSet<Element>
        {
            Element.Fire,
            Element.Water,
            Element.Earth,
            Element.Wind,
        };

        var expected = CardCatalog
            .GetCardsByType(CardType.Summon)
            .Where(card => allowedElements.Contains(card.ElementalAffinity))
            .Where(card => (card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) == 0)
            .Select(card => card.Id)
            .ToHashSet();

        AssertThat(battle.DevPlayerDeck).IsNotNull();
        var actual = battle.DevPlayerDeck!.Select(entry => entry.CardId).ToHashSet();
        AssertThat(actual.SetEquals(expected)).IsTrue();
        AssertThat(actual.Count).IsGreaterEqual(40);
    }

    [TestCase]
    public void TestArenaCampaign_IncludesArenaAllUnitsEvent()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.TestArena);
        AssertThat(campaign).IsNotNull();
        AssertThat(campaign!.EventIds.Contains(EventIds.ArenaAllUnits)).IsTrue();
    }
}
