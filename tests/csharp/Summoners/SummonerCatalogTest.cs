namespace Fateforged.Tests.Summoners;

using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for the SummonerCatalog static class.
/// These tests run without Godot runtime for speed.
/// </summary>
[TestSuite]
public class SummonerCatalogTest
{
    [TestCase]
    public void GetSummoner_ReturnsSummonerDefinition_WhenSummonerExists()
    {
        var summoner = SummonerCatalog.GetSummoner(SummonerIds.Cole);

        AssertThat(summoner).IsNotNull();
        AssertThat(summoner!.Id).IsEqual(SummonerIds.Cole);
        AssertThat(summoner.ElementalAffinity).IsEqual(Element.Fire);
    }

    [TestCase]
    public void GetSummoner_ReturnsNull_WhenSummonerDoesNotExist()
    {
        var summoner = SummonerCatalog.GetSummoner("nonexistent_summoner");

        AssertThat(summoner).IsNull();
    }

    [TestCase]
    public void HasSummoner_ReturnsTrue_WhenSummonerExists()
    {
        var exists = SummonerCatalog.HasSummoner(SummonerIds.Selene);

        AssertThat(exists).IsTrue();
    }

    [TestCase]
    public void HasSummoner_ReturnsFalse_WhenSummonerDoesNotExist()
    {
        var exists = SummonerCatalog.HasSummoner("nonexistent_summoner");

        AssertThat(exists).IsFalse();
    }

    [TestCase]
    public void GetAllSummonerIds_ReturnsNonEmptyArray()
    {
        var ids = SummonerCatalog.GetAllSummonerIds();

        AssertThat(ids).IsNotNull();
        AssertThat(ids.Length).IsGreater(0);
    }

    [TestCase]
    public void Count_MatchesGetAllSummoners()
    {
        var allSummoners = SummonerCatalog.GetAllSummoners();

        AssertThat(SummonerCatalog.Count).IsEqual(allSummoners.Length);
    }

    [TestCase]
    public void GetStartingSummoners_ReturnsFourCoreSummoners()
    {
        var starters = SummonerCatalog.GetStartingSummoners();

        AssertThat(starters).IsNotNull();
        AssertThat(starters.Length).IsEqual(4);

        foreach (var summoner in starters)
        {
            AssertThat(summoner.UnlockCondition).IsEqual(SummonerUnlockCondition.StartingChoice);
        }
    }

    [TestCase]
    public void GetStartingSummoners_ContainsCoreElements()
    {
        var starters = SummonerCatalog.GetStartingSummoners();
        var elements = starters.Select(s => s.ElementalAffinity).ToArray();

        AssertThat(elements).Contains(Element.Fire);
        AssertThat(elements).Contains(Element.Water);
        AssertThat(elements).Contains(Element.Wind);
        AssertThat(elements).Contains(Element.Earth);
    }

    [TestCase]
    public void GetRandomPoolSummoners_IncludesStartingSummoners()
    {
        var randomPool = SummonerCatalog.GetRandomPoolSummoners();

        AssertThat(randomPool).IsNotNull();
        // Random pool should include at least the 4 starting summoners
        AssertThat(randomPool.Length).IsGreaterEqual(4);

        foreach (var summoner in randomPool)
        {
            var validCondition =
                summoner.UnlockCondition == SummonerUnlockCondition.StartingChoice
                || summoner.UnlockCondition == SummonerUnlockCondition.RandomStarterOnly;
            AssertThat(validCondition).IsTrue();
        }
    }

    [TestCase]
    public void GetSummonersByElement_ReturnsCorrectSummoners()
    {
        var fireSummoners = SummonerCatalog.GetSummonersByElement(Element.Fire);

        AssertThat(fireSummoners).IsNotNull();
        AssertThat(fireSummoners.Length).IsGreater(0);

        foreach (var summoner in fireSummoners)
        {
            AssertThat(summoner.ElementalAffinity).IsEqual(Element.Fire);
        }
    }

    [TestCase]
    public void SummonerDefinition_HasValidInnateTraits()
    {
        var cole = SummonerCatalog.GetSummoner(SummonerIds.Cole);

        AssertThat(cole).IsNotNull();
        AssertThat(cole!.InnateTraitIds).IsNotNull();
        AssertThat(cole.InnateTraitIds.Length).IsGreater(0);
    }

    [TestCase]
    public void SummonerIds_AreAllUnique()
    {
        var ids = SummonerCatalog.GetAllSummonerIds();
        var uniqueIds = ids.Distinct().ToArray();

        AssertThat(ids.Length).IsEqual(uniqueIds.Length);
    }

    [TestCase]
    public void ElementToGdElementId_MapsCorrectly()
    {
        AssertThat(SummonerCatalog.ElementToGdElementId(Element.Neutral)).IsEqual(0);
        AssertThat(SummonerCatalog.ElementToGdElementId(Element.Fire)).IsEqual(1);
        AssertThat(SummonerCatalog.ElementToGdElementId(Element.Water)).IsEqual(2);
        AssertThat(SummonerCatalog.ElementToGdElementId(Element.Wind)).IsEqual(3);
        AssertThat(SummonerCatalog.ElementToGdElementId(Element.Earth)).IsEqual(4);
    }
}
