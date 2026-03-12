using System;

namespace Fateforged.Multiplayer.Core;

/// <summary>
/// First-class representation of the local player's identity in a match.
/// Provides helpers for team/perspective calculations used throughout the codebase.
/// </summary>
public static class LocalPlayer
{
    /// <summary>
    /// Local player's network index (0 = host, 1 = client).
    /// Set when match starts.
    /// </summary>
    public static int NetworkIndex { get; private set; } = 0;

    /// <summary>
    /// Whether the local player is the host.
    /// </summary>
    public static bool IsHost => NetworkIndex == 0;

    /// <summary>
    /// Whether the local player is a client (not host).
    /// </summary>
    public static bool IsClient => NetworkIndex != 0;

    /// <summary>
    /// Initialize for a match.
    /// </summary>
    public static void Initialize(int networkIndex)
    {
        NetworkIndex = networkIndex;
    }

    /// <summary>
    /// Reset to default state (for match cleanup or single-player).
    /// </summary>
    public static void Reset()
    {
        NetworkIndex = 0;
    }

    /// <summary>
    /// Check if a network team index represents the local player's team.
    /// </summary>
    public static bool IsLocalTeam(int networkTeam) => networkTeam == NetworkIndex;

    /// <summary>
    /// Convert network team index to local team enum.
    /// Local player is always PLAYER (0), opponent is always ENEMY (1).
    /// </summary>
    [Obsolete("Use NetworkToLocal(NetworkTeam) for type safety")]
    public static int NetworkTeamToLocal(int networkTeam)
    {
        return networkTeam == NetworkIndex ? 0 : 1; // 0 = PLAYER, 1 = ENEMY
    }

    /// <summary>
    /// Convert local team enum to network team index.
    /// </summary>
    [Obsolete("Use LocalToNetwork(LocalTeam) for type safety")]
    public static int LocalTeamToNetwork(int localTeam)
    {
        // If localTeam is PLAYER (0), return our network index
        // If localTeam is ENEMY (1), return opponent's network index
        return localTeam == 0 ? NetworkIndex : (1 - NetworkIndex);
    }

    /// <summary>
    /// Type-safe: convert local team to network team.
    /// </summary>
    public static NetworkTeam LocalToNetwork(LocalTeam local) =>
        new(local.Value == 0 ? NetworkIndex : (1 - NetworkIndex));

    /// <summary>
    /// Type-safe: convert network team to local team.
    /// </summary>
    public static LocalTeam NetworkToLocal(NetworkTeam network) =>
        new(network.Value == NetworkIndex ? 0 : 1);
}
