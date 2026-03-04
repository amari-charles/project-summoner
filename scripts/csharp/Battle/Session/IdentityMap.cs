using System.Collections.Generic;

namespace Fateforged.Session;

/// <summary>
/// UnitId (sim) ↔ NetworkId (network) O(1) bimap, pure ints, no Godot dependency.
/// Target version of NetworkIdRegistry — maps ints instead of Nodes.
/// </summary>
public class IdentityMap
{
    private readonly Dictionary<int, int> _unitToNetwork = new();
    private readonly Dictionary<int, int> _networkToUnit = new();

    public int Count => _unitToNetwork.Count;

    public void Register(int unitId, int networkId)
    {
        _unitToNetwork[unitId] = networkId;
        _networkToUnit[networkId] = unitId;
    }

    public void Unregister(int unitId)
    {
        if (_unitToNetwork.TryGetValue(unitId, out var networkId))
        {
            _unitToNetwork.Remove(unitId);
            _networkToUnit.Remove(networkId);
        }
    }

    public int GetNetworkId(int unitId)
    {
        return _unitToNetwork[unitId];
    }

    public int GetUnitId(int networkId)
    {
        return _networkToUnit[networkId];
    }

    public bool TryGetNetworkId(int unitId, out int networkId)
    {
        return _unitToNetwork.TryGetValue(unitId, out networkId);
    }

    public bool TryGetUnitId(int networkId, out int unitId)
    {
        return _networkToUnit.TryGetValue(networkId, out unitId);
    }

    public void Clear()
    {
        _unitToNetwork.Clear();
        _networkToUnit.Clear();
    }
}
