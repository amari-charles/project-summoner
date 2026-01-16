namespace ProjectSummoner.Tests.Economy;

using System.Collections.Generic;
using GdUnit4;
using ProjectSummoner.Data.Profile;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for ResourceType and ResourceData classes.
/// EconomyService itself extends Node and requires Godot runtime.
/// These tests verify the data types and extensions used by EconomyService.
/// </summary>
[TestSuite]
public class EconomyServiceTest
{
    [TestCase]
    public void ResourceData_DefaultsToZero()
    {
        var resources = new ResourceData();

        AssertThat(resources.Gold).IsEqual(0);
        AssertThat(resources.Gems).IsEqual(0);
        AssertThat(resources.Essence).IsEqual(0);
        AssertThat(resources.Fragments).IsEqual(0);
    }

    [TestCase]
    public void ResourceData_CanSetValues()
    {
        var resources = new ResourceData
        {
            Gold = 100,
            Gems = 50,
            Essence = 25,
            Fragments = 10
        };

        AssertThat(resources.Gold).IsEqual(100);
        AssertThat(resources.Gems).IsEqual(50);
        AssertThat(resources.Essence).IsEqual(25);
        AssertThat(resources.Fragments).IsEqual(10);
    }

    [TestCase]
    public void ResourceType_ToKey_ReturnsCorrectStrings()
    {
        AssertThat(ResourceType.Gold.ToKey()).IsEqual("gold");
        AssertThat(ResourceType.Gems.ToKey()).IsEqual("gems");
        AssertThat(ResourceType.Essence.ToKey()).IsEqual("essence");
        AssertThat(ResourceType.Fragments.ToKey()).IsEqual("fragments");
    }

    [TestCase]
    public void ResourceTypeExtensions_FromKey_ParsesCorrectly()
    {
        AssertThat(ResourceTypeExtensions.FromKey("gold")).IsEqual(ResourceType.Gold);
        AssertThat(ResourceTypeExtensions.FromKey("gems")).IsEqual(ResourceType.Gems);
        AssertThat(ResourceTypeExtensions.FromKey("essence")).IsEqual(ResourceType.Essence);
        AssertThat(ResourceTypeExtensions.FromKey("fragments")).IsEqual(ResourceType.Fragments);
    }

    [TestCase]
    public void ResourceTypeExtensions_FromKey_ReturnsNull_ForInvalidKey()
    {
        AssertThat(ResourceTypeExtensions.FromKey("invalid")).IsNull();
        AssertThat(ResourceTypeExtensions.FromKey("")).IsNull();
    }

    [TestCase]
    public void ResourceCost_CanBeRepresentedAsDictionary()
    {
        var cost = new Dictionary<ResourceType, int>
        {
            { ResourceType.Gold, 100 },
            { ResourceType.Gems, 50 }
        };

        AssertThat(cost.ContainsKey(ResourceType.Gold)).IsTrue();
        AssertThat(cost[ResourceType.Gold]).IsEqual(100);
        AssertThat(cost.ContainsKey(ResourceType.Gems)).IsTrue();
        AssertThat(cost[ResourceType.Gems]).IsEqual(50);
    }

    [TestCase]
    public void MockRepository_CanTrackResourceOperations()
    {
        var mock = new TestableResourceTracker();
        mock.SetResources(100, 50, 25, 10);

        AssertThat(mock.GetResources().Gold).IsEqual(100);
        AssertThat(mock.GetResources().Gems).IsEqual(50);

        // Test update
        mock.UpdateResources(new Dictionary<ResourceType, int>
        {
            { ResourceType.Gold, -30 },
            { ResourceType.Gems, 20 }
        });

        AssertThat(mock.GetResources().Gold).IsEqual(70);
        AssertThat(mock.GetResources().Gems).IsEqual(70);
    }

    [TestCase]
    public void CanAfford_Logic_WorksCorrectly()
    {
        var resources = new ResourceData { Gold = 100, Gems = 50 };
        var cost = new Dictionary<ResourceType, int>
        {
            { ResourceType.Gold, 50 },
            { ResourceType.Gems, 25 }
        };

        // Implement the can afford check logic locally for testing
        var canAfford = true;
        foreach (var kvp in cost)
        {
            var available = kvp.Key switch
            {
                ResourceType.Gold => resources.Gold,
                ResourceType.Gems => resources.Gems,
                ResourceType.Essence => resources.Essence,
                ResourceType.Fragments => resources.Fragments,
                _ => 0
            };
            if (available < kvp.Value)
            {
                canAfford = false;
                break;
            }
        }

        AssertThat(canAfford).IsTrue();
    }

    [TestCase]
    public void CanAfford_Logic_ReturnsFalse_WhenInsufficient()
    {
        var resources = new ResourceData { Gold = 30, Gems = 50 };
        var cost = new Dictionary<ResourceType, int>
        {
            { ResourceType.Gold, 50 },  // Not enough
            { ResourceType.Gems, 25 }
        };

        var canAfford = true;
        foreach (var kvp in cost)
        {
            var available = kvp.Key switch
            {
                ResourceType.Gold => resources.Gold,
                ResourceType.Gems => resources.Gems,
                ResourceType.Essence => resources.Essence,
                ResourceType.Fragments => resources.Fragments,
                _ => 0
            };
            if (available < kvp.Value)
            {
                canAfford = false;
                break;
            }
        }

        AssertThat(canAfford).IsFalse();
    }

    /// <summary>
    /// Simple resource tracker for testing resource update logic.
    /// </summary>
    private class TestableResourceTracker
    {
        private ResourceData _resources = new();

        public void SetResources(int gold = 0, int gems = 0, int essence = 0, int fragments = 0)
        {
            _resources = new ResourceData
            {
                Gold = gold,
                Gems = gems,
                Essence = essence,
                Fragments = fragments
            };
        }

        public ResourceData GetResources() => _resources;

        public void UpdateResources(Dictionary<ResourceType, int> delta)
        {
            foreach (var kvp in delta)
            {
                switch (kvp.Key)
                {
                    case ResourceType.Gold:
                        _resources.Gold += kvp.Value;
                        break;
                    case ResourceType.Gems:
                        _resources.Gems += kvp.Value;
                        break;
                    case ResourceType.Essence:
                        _resources.Essence += kvp.Value;
                        break;
                    case ResourceType.Fragments:
                        _resources.Fragments += kvp.Value;
                        break;
                }
            }
        }
    }
}
