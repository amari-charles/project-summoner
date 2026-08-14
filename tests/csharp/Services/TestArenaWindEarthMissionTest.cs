namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Meta.Campaign;
using Fateforged.Units;
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
        AssertThat(battle!.RuntimeSurface).IsEqual(BattleRuntimeSurface.DebugArena);

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
        AssertThat(battle!.RuntimeSurface).IsEqual(BattleRuntimeSurface.DebugArena);

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
        AssertThat(
                battle.DevPlayerDeck!.All(entry =>
                    CardCatalog.GetCard(entry.CardId)?.Type == CardType.Summon
                )
            )
            .IsTrue();
    }

    [TestCase]
    public void ArenaAllCards_UsesEveryActiveCoreElementCard_AndUnitTargets()
    {
        var battle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.ArenaAllCards);
        AssertThat(battle).IsNotNull();
        AssertThat(battle!.RuntimeSurface).IsEqual(BattleRuntimeSurface.DebugArena);

        var allowedElements = new HashSet<Element>
        {
            Element.Fire,
            Element.Water,
            Element.Earth,
            Element.Wind,
        };

        var expected = CardCatalog
            .GetAllCards()
            .Where(card => allowedElements.Contains(card.ElementalAffinity))
            .Where(card => (card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) == 0)
            .Select(card => card.Id)
            .ToHashSet();

        AssertThat(battle.DevPlayerDeck).IsNotNull();
        var actual = battle.DevPlayerDeck!.Select(entry => entry.CardId).ToHashSet();
        AssertThat(actual.SetEquals(expected)).IsTrue();
        AssertThat(
                battle.DevPlayerDeck!.Any(entry =>
                    CardCatalog.GetCard(entry.CardId)?.Type == CardType.Spell
                )
            )
            .IsTrue();
        AssertThat(
                battle.DevPlayerDeck!.Any(entry =>
                    CardCatalog.GetCard(entry.CardId)?.Type == CardType.Summon
                )
            )
            .IsTrue();

        AssertThat(battle.EnemyDeck).IsNotNull();
        AssertThat(battle.EnemyDeck!.Count).IsGreater(0);
        AssertThat(
                battle.EnemyDeck!.All(entry => CardCatalog.GetCard(entry.CardId)?.Type == CardType.Summon)
            )
            .IsTrue();
    }

    [TestCase]
    public void ArenaAllSpells_UsesEveryActiveCoreElementSpell_AndOnlyRealArtUnitTargets()
    {
        var battle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.ArenaAllSpells);
        AssertThat(battle).IsNotNull();
        AssertThat(battle!.RuntimeSurface).IsEqual(BattleRuntimeSurface.DebugArena);
        AssertThat(battle.AiType).IsEqual("none");

        var allowedElements = new HashSet<Element>
        {
            Element.Fire,
            Element.Water,
            Element.Earth,
            Element.Wind,
        };

        var expectedSpells = CardCatalog
            .GetCardsByType(CardType.Spell)
            .Where(card => allowedElements.Contains(card.ElementalAffinity))
            .Where(card => (card.Flags & (CardFlags.DevOnly | CardFlags.Archived)) == 0)
            .Select(card => card.Id)
            .ToHashSet();

        AssertThat(battle.DevPlayerDeck).IsNotNull();
        var playerCardIds = battle.DevPlayerDeck!.Select(entry => entry.CardId).ToList();
        var actualSpells = playerCardIds
            .Where(cardId => CardCatalog.GetCard(cardId)?.Type == CardType.Spell)
            .ToHashSet();
        var actualSummons = playerCardIds
            .Where(cardId => CardCatalog.GetCard(cardId)?.Type == CardType.Summon)
            .ToHashSet();

        AssertThat(actualSpells.SetEquals(expectedSpells)).IsTrue();
        AssertThat(actualSummons.Count).IsEqual(6);
        AssertThat(actualSummons.SetEquals(RealArtTargetCards())).IsTrue();

        AssertThat(battle.EnemyDeck).IsNotNull();
        var enemyTargets = battle.EnemyDeck!.Select(entry => entry.CardId).ToHashSet();
        AssertThat(enemyTargets.SetEquals(RealArtTargetCards())).IsTrue();

        foreach (var cardId in actualSummons.Concat(enemyTargets))
        {
            var card = CardCatalog.GetCard(cardId);
            AssertThat(card).IsNotNull();
            var unitDef = UnitDefinitions.Get(card!.UnitId);
            AssertThat(unitDef).IsNotNull();
            AssertThat(unitDef!.ScenePath.Contains("placeholder")).IsFalse();
            AssertThat(Godot.ResourceLoader.Exists(unitDef.ScenePath)).IsTrue();
        }
    }

    [TestCase]
    public void ArenaSpriteUnits_UsesOnlyRealSpriteSummons_ForDebugArena()
    {
        var battle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.ArenaSpriteUnits);
        AssertThat(battle).IsNotNull();
        AssertThat(battle!.RuntimeSurface).IsEqual(BattleRuntimeSurface.DebugArena);
        AssertThat(battle.AiType).IsEqual("none");

        AssertThat(battle.DevPlayerDeck).IsNotNull();
        var playerCardIds = battle.DevPlayerDeck!.Select(entry => entry.CardId).ToHashSet();
        AssertThat(playerCardIds.SetEquals(RealArtTargetCards())).IsTrue();

        AssertThat(battle.EnemyDeck).IsNotNull();
        var enemyCardIds = battle.EnemyDeck!.Select(entry => entry.CardId).ToHashSet();
        AssertThat(enemyCardIds.SetEquals(RealArtTargetCards())).IsTrue();

        foreach (var cardId in playerCardIds.Concat(enemyCardIds))
        {
            var card = CardCatalog.GetCard(cardId);
            AssertThat(card).IsNotNull();
            AssertThat(card!.Type).IsEqual(CardType.Summon);

            var unitDef = UnitDefinitions.Get(card.UnitId);
            AssertThat(unitDef).IsNotNull();
            AssertThat(unitDef!.ScenePath.Contains("placeholder")).IsFalse();
            AssertThat(Godot.ResourceLoader.Exists(unitDef.ScenePath)).IsTrue();
        }
    }

    [TestCase]
    public void TestArenaCampaign_IncludesArenaAllUnitsEvent()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.TestArena);
        AssertThat(campaign).IsNotNull();
        AssertThat(campaign!.EventIds.Contains(EventIds.ArenaAllUnits)).IsTrue();
    }

    [TestCase]
    public void TestArenaCampaign_IncludesArenaAllCardsEvent()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.TestArena);
        AssertThat(campaign).IsNotNull();
        AssertThat(campaign!.EventIds.Contains(EventIds.ArenaAllCards)).IsTrue();
    }

    [TestCase]
    public void TestArenaCampaign_IncludesArenaAllSpellsEvent()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.TestArena);
        AssertThat(campaign).IsNotNull();
        AssertThat(campaign!.EventIds.Contains(EventIds.ArenaAllSpells)).IsTrue();
    }

    [TestCase]
    public void TestArenaCampaign_IncludesArenaSpriteUnitsEvent()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.TestArena);
        AssertThat(campaign).IsNotNull();
        AssertThat(campaign!.EventIds.Contains(EventIds.ArenaSpriteUnits)).IsTrue();
    }

    private static HashSet<CardId> RealArtTargetCards()
    {
        return new HashSet<CardId>
        {
            CardIds.FireWisp,
            CardIds.FireWolf,
            CardIds.WaterFrog,
            CardIds.Pebbloom,
            CardIds.EarthKomodoDragon,
            CardIds.Puff,
        };
    }
}
