using Godot;

namespace Fateforged.Multiplayer.Core;

/// <summary>
/// Transforms between canonical (network) and local (view) coordinate spaces.
///
/// Canonical Space (Network/Host perspective):
/// - X &lt; 0 = Host's spawn zone
/// - X &gt; 0 = Client's spawn zone
/// - All network messages use canonical coordinates
///
/// Local Space (Each player's view):
/// - X &lt; 0 = Local player's spawn zone (always "their side")
/// - X &gt; 0 = Opponent's spawn zone (always "enemy side")
/// </summary>
public static class CoordinateTransform
{
    /// <summary>
    /// Transform position from local to canonical space.
    /// </summary>
    public static Vector3 LocalToCanonical(Vector3 localPos)
    {
        if (LocalPlayer.IsHost)
            return localPos;
        return MirrorX(localPos);
    }

    /// <summary>
    /// Transform position from canonical to local space.
    /// </summary>
    public static Vector3 CanonicalToLocal(Vector3 canonicalPos)
    {
        if (LocalPlayer.IsHost)
            return canonicalPos;
        return MirrorX(canonicalPos);
    }

    /// <summary>
    /// Transform rotation from local to canonical space.
    /// For units that face a direction (yaw 180° flip for client).
    /// </summary>
    public static Vector3 LocalRotationToCanonical(Vector3 localRotation)
    {
        if (LocalPlayer.IsHost)
            return localRotation;
        return new Vector3(localRotation.X, localRotation.Y + Mathf.Pi, localRotation.Z);
    }

    /// <summary>
    /// Transform rotation from canonical to local space.
    /// </summary>
    public static Vector3 CanonicalRotationToLocal(Vector3 canonicalRotation)
    {
        if (LocalPlayer.IsHost)
            return canonicalRotation;
        return new Vector3(
            canonicalRotation.X,
            canonicalRotation.Y + Mathf.Pi,
            canonicalRotation.Z
        );
    }

    /// <summary>
    /// Transform a full pose (position + rotation) from local to canonical.
    /// </summary>
    public static (Vector3 position, Vector3 rotation) LocalPoseToCanonical(
        Vector3 pos,
        Vector3 rot
    )
    {
        return (LocalToCanonical(pos), LocalRotationToCanonical(rot));
    }

    /// <summary>
    /// Transform a full pose from canonical to local.
    /// </summary>
    public static (Vector3 position, Vector3 rotation) CanonicalPoseToLocal(
        Vector3 pos,
        Vector3 rot
    )
    {
        return (CanonicalToLocal(pos), CanonicalRotationToLocal(rot));
    }

    private static Vector3 MirrorX(Vector3 v) => new(-v.X, v.Y, v.Z);
}
