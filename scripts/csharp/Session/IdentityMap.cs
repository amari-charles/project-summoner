namespace Fateforged.Session;

/// <summary>
/// UnitId (sim) ↔ NetworkId (network) O(1) bimap, pure ints, no Godot dependency.
/// Target version of NetworkIdRegistry — maps ints instead of Nodes.
/// </summary>
public class IdentityMap
{
    public int GetNetworkId(int unitId)
    {
        throw new System.NotImplementedException();
    }

    public int GetUnitId(int networkId)
    {
        throw new System.NotImplementedException();
    }

    public void Register(int unitId, int networkId)
    {
        throw new System.NotImplementedException();
    }

    public void Unregister(int unitId)
    {
        throw new System.NotImplementedException();
    }
}
