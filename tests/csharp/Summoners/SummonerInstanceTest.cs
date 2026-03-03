namespace Fateforged.Tests.Summoners;

using System.Linq;
using GdUnit4;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile.Summoners;
using static GdUnit4.Assertions;

[TestSuite]
public class SummonerInstanceTest
{
    [TestCase]
    public void GetComputedStats_ReturnsBaseStats_ForLevel1NoTraits()
    {
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(SummonerIds.Cole)
        };

        var stats = instance.GetComputedStats();

        AssertThat(stats).IsNotNull();
        AssertThat(stats.ContainsKey("health")).IsTrue();
        AssertThat(stats.ContainsKey("max_mana")).IsTrue();

        var def = SummonerCatalog.GetSummoner(SummonerIds.Cole)!;
        // Level 1: no level bonus, but Cole has innate traits that may modify stats
        // Base health before trait modifiers
        AssertThat(stats["health"]).IsGreaterEqual(def.BaseHealth);
    }

    [TestCase]
    public void GetComputedStats_AppliesLevelBonus()
    {
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(SummonerIds.ManaTest),
            Level = 3
        };

        var stats = instance.GetComputedStats();
        var def = SummonerCatalog.GetSummoner(SummonerIds.ManaTest)!;

        // Level 3: 1.0 + (3-1) * 0.05 = 1.10 multiplier
        var expectedHealth = def.BaseHealth * 1.10f;
        var expectedMana = def.MaxMana * 1.10f;

        // ManaTest has no innate traits, so stats should be exactly the level-multiplied base
        AssertThat(stats["health"]).IsEqual(expectedHealth);
        AssertThat(stats["max_mana"]).IsEqual(expectedMana);
    }

    [TestCase]
    public void GetComputedStats_AppliesTraitModifiers()
    {
        // Cole has innate traits: FireAffinity, BurningSpirit
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(SummonerIds.Cole)
        };

        var stats = instance.GetComputedStats();

        // FireAffinity should set fire_damage_bonus > 0
        AssertThat(stats.ContainsKey("fire_damage_bonus")).IsTrue();
        AssertThat(stats["fire_damage_bonus"]).IsGreater(0f);
    }

    [TestCase]
    public void GetComputedStats_ReturnsEmpty_WhenSummonerNotFound()
    {
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId("nonexistent_summoner")
        };

        var stats = instance.GetComputedStats();

        AssertThat(stats.Count).IsEqual(0);
    }

    [TestCase]
    public void GetAllTraitIds_ReturnsInnateAndAcquired()
    {
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(SummonerIds.Cole),
            AcquiredTraitIds = [new TraitId("trait_fortune_favors_the_bold")]
        };

        var allTraits = instance.GetAllTraitIds();

        // Cole has 2 innate traits + 1 acquired
        var def = SummonerCatalog.GetSummoner(SummonerIds.Cole)!;
        AssertThat(allTraits.Count).IsEqual(def.InnateTraitIds.Length + 1);
        AssertThat(allTraits).Contains("trait_fortune_favors_the_bold");
    }

    [TestCase]
    public void GetAllTraitIds_ReturnsInnateOnly_WhenNoAcquired()
    {
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(SummonerIds.Celine)
        };

        var allTraits = instance.GetAllTraitIds();

        var def = SummonerCatalog.GetSummoner(SummonerIds.Celine)!;
        AssertThat(allTraits.Count).IsEqual(def.InnateTraitIds.Length);

        foreach (var innateId in def.InnateTraitIds)
        {
            AssertThat(allTraits).Contains((string)innateId);
        }
    }

    [TestCase]
    public void GetStat_ReturnsSingleStatValue()
    {
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(SummonerIds.ManaTest)
        };

        var health = instance.GetStat("health");
        var def = SummonerCatalog.GetSummoner(SummonerIds.ManaTest)!;

        AssertThat(health).IsEqual(def.BaseHealth);
    }

    [TestCase]
    public void GetStat_ReturnsZero_ForUnknownStat()
    {
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(SummonerIds.ManaTest)
        };

        var unknown = instance.GetStat("nonexistent_stat");

        AssertThat(unknown).IsEqual(0f);
    }

    [TestCase]
    public void LevelStatBonusPercent_IsFivePercent()
    {
        AssertThat(SummonerInstance.LevelStatBonusPercent).IsEqual(0.05f);
    }
}
