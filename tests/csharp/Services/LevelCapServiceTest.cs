namespace ProjectSummoner.Tests.Services;

using System.Collections.Generic;
using GdUnit4;
using ProjectSummoner.Services;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for LevelCapService class.
/// Tests the static methods directly without needing the Node instance.
/// </summary>
// [TestSuite] — Disabled: Godot.Collections.Dictionary crashes test host in headless mode.
// Run in Godot editor instead. See MessageSerializerTest.cs for details.
public class LevelCapServiceTest
{
    private LevelCapService _service = null!;

    [Before]
    public void Setup()
    {
        _service = new LevelCapService();
    }

    // =============================================================================
    // GetEffectiveLevel Tests
    // =============================================================================

    [TestCase]
    public void GetEffectiveLevel_NoCap_ReturnsCardLevel()
    {
        // No cap (0) should return the card's actual level
        AssertThat(_service.GetEffectiveLevel(5, LevelCapService.NoCap)).IsEqual(5);
        AssertThat(_service.GetEffectiveLevel(10, 0)).IsEqual(10);
        AssertThat(_service.GetEffectiveLevel(1, -1)).IsEqual(1);
    }

    [TestCase]
    public void GetEffectiveLevel_CardBelowCap_ReturnsCardLevel()
    {
        // Card level 3, cap 5 -> returns 3
        AssertThat(_service.GetEffectiveLevel(3, 5)).IsEqual(3);
        AssertThat(_service.GetEffectiveLevel(1, 10)).IsEqual(1);
    }

    [TestCase]
    public void GetEffectiveLevel_CardAtCap_ReturnsCardLevel()
    {
        // Card level equals cap -> returns card level (which equals cap)
        AssertThat(_service.GetEffectiveLevel(5, 5)).IsEqual(5);
        AssertThat(_service.GetEffectiveLevel(3, 3)).IsEqual(3);
    }

    [TestCase]
    public void GetEffectiveLevel_CardAboveCap_ReturnsCap()
    {
        // Card level 7, cap 5 -> returns 5
        AssertThat(_service.GetEffectiveLevel(7, 5)).IsEqual(5);
        AssertThat(_service.GetEffectiveLevel(10, 3)).IsEqual(3);
    }

    // =============================================================================
    // GetEffectiveTraits Tests
    // =============================================================================

    [TestCase]
    public void GetEffectiveTraits_NoCap_ReturnsAllTraits()
    {
        var traits = new List<string> { "hp_1", "atk_1", "hp_2", "atk_2" };

        var result = _service.GetEffectiveTraits(traits, LevelCapService.NoCap);

        AssertThat(result.Count).IsEqual(4);
        AssertThat(result).ContainsExactly(new string[] { "hp_1", "atk_1", "hp_2", "atk_2" });
    }

    [TestCase]
    public void GetEffectiveTraits_NullTraits_ReturnsEmptyList()
    {
        var result = _service.GetEffectiveTraits((List<string>)null!, LevelCapService.NoCap);

        AssertThat(result).IsNotNull();
        AssertThat(result.Count).IsEqual(0);
    }

    [TestCase]
    public void GetEffectiveTraits_CapLevel1_ReturnsNoTraits()
    {
        // Level 1 cards have 0 traits, so cap 1 means 0 effective traits
        var traits = new List<string> { "hp_1", "atk_1", "hp_2" };

        var result = _service.GetEffectiveTraits(traits, 1);

        AssertThat(result.Count).IsEqual(0);
    }

    [TestCase]
    public void GetEffectiveTraits_CapLevel2_ReturnsFirstTrait()
    {
        // Level 2 cards have 1 trait
        var traits = new List<string> { "hp_1", "atk_1", "hp_2", "atk_2" };

        var result = _service.GetEffectiveTraits(traits, 2);

        AssertThat(result.Count).IsEqual(1);
        AssertThat(result[0]).IsEqual("hp_1");
    }

    [TestCase]
    public void GetEffectiveTraits_CapLevel3_ReturnsFirstTwoTraits()
    {
        // Level 3 cards have 2 traits
        var traits = new List<string> { "hp_1", "atk_1", "hp_2", "atk_2" };

        var result = _service.GetEffectiveTraits(traits, 3);

        AssertThat(result.Count).IsEqual(2);
        AssertThat(result).ContainsExactly(new string[] { "hp_1", "atk_1" });
    }

    [TestCase]
    public void GetEffectiveTraits_CapHigherThanTraitCount_ReturnsAllTraits()
    {
        // Card only has 2 traits but cap is 5 (which allows 4 traits)
        var traits = new List<string> { "hp_1", "atk_1" };

        var result = _service.GetEffectiveTraits(traits, 5);

        AssertThat(result.Count).IsEqual(2);
        AssertThat(result).ContainsExactly(new string[] { "hp_1", "atk_1" });
    }

    [TestCase]
    public void GetEffectiveTraits_EmptyTraits_ReturnsEmptyList()
    {
        var result = _service.GetEffectiveTraits(new List<string>(), 5);

        AssertThat(result.Count).IsEqual(0);
    }

    // =============================================================================
    // GetLevelCap Tests (from Dictionary)
    // =============================================================================

    [TestCase]
    public void GetLevelCap_NullConfig_ReturnsNoCap()
    {
        var result = _service.GetLevelCap(null!);

        AssertThat(result).IsEqual(LevelCapService.NoCap);
    }

    [TestCase]
    public void GetLevelCap_EmptyConfig_ReturnsNoCap()
    {
        var config = new Godot.Collections.Dictionary();

        var result = _service.GetLevelCap(config);

        AssertThat(result).IsEqual(LevelCapService.NoCap);
    }

    [TestCase]
    public void GetLevelCap_ConfigWithCap_ReturnsCap()
    {
        var config = new Godot.Collections.Dictionary
        {
            ["level_cap"] = 5
        };

        var result = _service.GetLevelCap(config);

        AssertThat(result).IsEqual(5);
    }

    [TestCase]
    public void GetLevelCap_ConfigWithZeroCap_ReturnsNoCap()
    {
        var config = new Godot.Collections.Dictionary
        {
            ["level_cap"] = 0
        };

        var result = _service.GetLevelCap(config);

        AssertThat(result).IsEqual(0);
    }

    // =============================================================================
    // HasLevelCap Tests
    // =============================================================================

    [TestCase]
    public void HasLevelCap_NullConfig_ReturnsFalse()
    {
        var result = _service.HasLevelCap(null!);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void HasLevelCap_ConfigWithoutCap_ReturnsFalse()
    {
        var config = new Godot.Collections.Dictionary();

        var result = _service.HasLevelCap(config);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void HasLevelCap_ConfigWithCap_ReturnsTrue()
    {
        var config = new Godot.Collections.Dictionary
        {
            ["level_cap"] = 3
        };

        var result = _service.HasLevelCap(config);

        AssertThat(result).IsTrue();
    }

    // =============================================================================
    // GetPathType Tests
    // =============================================================================

    [TestCase]
    public void GetPathType_NullConfig_ReturnsStandard()
    {
        var result = _service.GetPathType(null!);

        AssertThat(result).IsEqual("standard");
    }

    [TestCase]
    public void GetPathType_ConfigWithoutPathType_ReturnsStandard()
    {
        var config = new Godot.Collections.Dictionary();

        var result = _service.GetPathType(config);

        AssertThat(result).IsEqual("standard");
    }

    [TestCase]
    public void GetPathType_ConfigWithPathType_ReturnsPathType()
    {
        var config = new Godot.Collections.Dictionary
        {
            ["path_type"] = "elite"
        };

        var result = _service.GetPathType(config);

        AssertThat(result).IsEqual("elite");
    }

    // =============================================================================
    // GetRecommendedLevel Tests
    // =============================================================================

    [TestCase]
    public void GetRecommendedLevel_NullConfig_ReturnsZero()
    {
        var result = _service.GetRecommendedLevel(null!);

        AssertThat(result).IsEqual(0);
    }

    [TestCase]
    public void GetRecommendedLevel_ConfigWithoutRecommendedLevel_ReturnsZero()
    {
        var config = new Godot.Collections.Dictionary();

        var result = _service.GetRecommendedLevel(config);

        AssertThat(result).IsEqual(0);
    }

    [TestCase]
    public void GetRecommendedLevel_ConfigWithRecommendedLevel_ReturnsLevel()
    {
        var config = new Godot.Collections.Dictionary
        {
            ["recommended_level"] = 5
        };

        var result = _service.GetRecommendedLevel(config);

        AssertThat(result).IsEqual(5);
    }
}
