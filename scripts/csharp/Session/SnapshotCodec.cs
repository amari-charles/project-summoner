using Fateforged.Simulation;

namespace Fateforged.Session;

/// <summary>
/// Serialize/deserialize MatchState for network transmission.
/// Thin wrapper around the existing StateSnapshotBuilder pattern.
/// </summary>
public class SnapshotCodec
{
    public byte[] Encode(MatchState state)
    {
        throw new System.NotImplementedException();
    }

    public MatchState Decode(byte[] data)
    {
        throw new System.NotImplementedException();
    }
}
