namespace Fateforged.Tests.Cards;

using GdUnit4;
using Fateforged.Cards;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for the CardCatalog static class.
/// These tests run without Godot runtime for speed.
/// </summary>
[TestSuite]
public class CardCatalogTest
{
    [TestCase]
    public void GetCard_ReturnsCardDefinition_WhenCardExists()
    {
        var card = CardCatalog.GetCard("fire_wisp");

        AssertThat(card).IsNotNull();
        AssertThat((string)card!.Id).IsEqual("fire_wisp");
        AssertThat(card.Name).IsEqual("Fire Wisp");
        AssertThat(card.Type).IsEqual(CardType.Summon);
    }

    [TestCase]
    public void GetCard_ReturnsNull_WhenCardDoesNotExist()
    {
        var card = CardCatalog.GetCard("nonexistent_card");

        AssertThat(card).IsNull();
    }

    [TestCase]
    public void HasCard_ReturnsTrue_WhenCardExists()
    {
        var exists = CardCatalog.HasCard("fireball");

        AssertThat(exists).IsTrue();
    }

    [TestCase]
    public void HasCard_ReturnsFalse_WhenCardDoesNotExist()
    {
        var exists = CardCatalog.HasCard("nonexistent_card");

        AssertThat(exists).IsFalse();
    }

    [TestCase]
    public void GetAllCardIds_ReturnsNonEmptyArray()
    {
        var ids = CardCatalog.GetAllCardIds();

        AssertThat(ids).IsNotNull();
        AssertThat(ids.Length).IsGreater(0);
    }

    [TestCase]
    public void GetCardsByRarity_ReturnsCorrectCards()
    {
        var commonCards = CardCatalog.GetCardsByRarity(Rarity.Common);

        AssertThat(commonCards).IsNotNull();
        AssertThat(commonCards.Length).IsGreater(0);

        foreach (var card in commonCards)
        {
            AssertThat(card.Rarity).IsEqual(Rarity.Common);
        }
    }

    [TestCase]
    public void GetCardsByType_ReturnsOnlySummonCards()
    {
        var summonCards = CardCatalog.GetCardsByType(CardType.Summon);

        AssertThat(summonCards).IsNotNull();
        AssertThat(summonCards.Length).IsGreater(0);

        foreach (var card in summonCards)
        {
            AssertThat(card.Type).IsEqual(CardType.Summon);
        }
    }

    [TestCase]
    public void GetCardsByType_ReturnsOnlySpellCards()
    {
        var spellCards = CardCatalog.GetCardsByType(CardType.Spell);

        AssertThat(spellCards).IsNotNull();
        AssertThat(spellCards.Length).IsGreater(0);

        foreach (var card in spellCards)
        {
            AssertThat(card.Type).IsEqual(CardType.Spell);
        }
    }

    [TestCase]
    public void GetCardsByElement_ReturnsFireCards()
    {
        var fireCards = CardCatalog.GetCardsByElement(Element.Fire);

        AssertThat(fireCards).IsNotNull();
        AssertThat(fireCards.Length).IsGreater(0);

        foreach (var card in fireCards)
        {
            AssertThat(card.ElementalAffinity).IsEqual(Element.Fire);
        }
    }

    [TestCase]
    public void GetStarterCards_ReturnsOnlyDefaultUnlockCards()
    {
        var starterCards = CardCatalog.GetStarterCards();

        AssertThat(starterCards).IsNotNull();
        AssertThat(starterCards.Length).IsGreater(0);

        foreach (var card in starterCards)
        {
            AssertThat(card.UnlockCondition).IsEqual(UnlockCondition.Default);
        }
    }

    [TestCase]
    public void CardDefinition_HasValidFormation()
    {
        var card = CardCatalog.GetCard("fire_wisp_swarm");

        AssertThat(card).IsNotNull();
        AssertThat(card!.Formation).IsNotNull();
        AssertThat(card.SpawnCount).IsEqual(12);
    }

    [TestCase]
    public void Count_MatchesGetAllCards()
    {
        var allCards = CardCatalog.GetAllCards();

        AssertThat(CardCatalog.Count).IsEqual(allCards.Length);
    }
}
