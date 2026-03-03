namespace Fateforged.Tests.Session;

using System.Collections.Generic;
using Fateforged.Session;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class IdentityMapTest
{
    private IdentityMap _map = null!;

    [BeforeTest]
    public void Setup()
    {
        _map = new IdentityMap();
    }

    [TestCase]
    public void Register_CanLookupBothDirections()
    {
        _map.Register(unitId: 1, networkId: 100);

        AssertThat(_map.GetNetworkId(1)).IsEqual(100);
        AssertThat(_map.GetUnitId(100)).IsEqual(1);
    }

    [TestCase]
    public void Unregister_RemovesBothDirections()
    {
        _map.Register(1, 100);
        _map.Unregister(1);

        AssertThat(_map.TryGetNetworkId(1, out _)).IsFalse();
        AssertThat(_map.TryGetUnitId(100, out _)).IsFalse();
    }

    [TestCase]
    public void Count_TracksRegistrations()
    {
        AssertThat(_map.Count).IsEqual(0);
        _map.Register(1, 100);
        _map.Register(2, 200);
        AssertThat(_map.Count).IsEqual(2);
        _map.Unregister(1);
        AssertThat(_map.Count).IsEqual(1);
    }

    [TestCase]
    public void Clear_RemovesAll()
    {
        _map.Register(1, 100);
        _map.Register(2, 200);
        _map.Clear();
        AssertThat(_map.Count).IsEqual(0);
    }

    [TestCase]
    public void TryGet_ReturnsFalseForMissing()
    {
        AssertThat(_map.TryGetNetworkId(999, out _)).IsFalse();
        AssertThat(_map.TryGetUnitId(999, out _)).IsFalse();
    }

    [TestCase]
    public void GetMissing_ThrowsKeyNotFound()
    {
        AssertThrown(() => _map.GetNetworkId(999))
            .IsInstanceOf<KeyNotFoundException>();
        AssertThrown(() => _map.GetUnitId(999))
            .IsInstanceOf<KeyNotFoundException>();
    }
}
