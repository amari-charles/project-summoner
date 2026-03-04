using System;
using Godot;
using Godot.Collections;
using Fateforged.Multiplayer.Backend;

namespace Fateforged.Multiplayer.Transport;

/// <summary>
/// IMatchTransport implementation that routes game messages through Nakama relay.
/// Uses opCode 200 for game messages (opCode 100 is reserved for pre-battle deck exchange).
/// </summary>
public partial class NakamaMatchTransport : Node, IMatchTransport
{
    private const long GameMessageOpCode = 200;

    private string _matchId = "";
    private bool _isHost;
    private bool _isConnected;
    private int _localPeerId;

    #region IMatchTransport Properties

    public new bool IsConnected => _isConnected;
    public int LocalPeerId => _localPeerId;
    public bool IsHost => _isHost;

    #endregion

    #region IMatchTransport Events

    public event Action<int, Dictionary>? OnMessageReceived;
    public event Action<int>? OnPeerConnected;
    public event Action<int>? OnPeerDisconnected;
    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;

    #endregion

    /// <summary>
    /// Initialize the transport with match details.
    /// Called from GDScript after match is joined and battle scene loads.
    /// </summary>
    public void Initialize(string matchId, bool isHost, int localPeerId)
    {
        _matchId = matchId;
        _isHost = isHost;
        _localPeerId = localPeerId;
        _isConnected = true;

        // Listen for match data and presence from NakamaGameClient
        var nakama = NakamaGameClient.Instance;
        if (nakama != null)
        {
            nakama.MatchDataReceived += OnNakamaMatchData;
            nakama.MatchPresenceLeft += OnNakamaPresenceLeft;
        }

        // Fire connected events
        OnConnected?.Invoke();

        // Simulate peer connected for the remote player
        int remotePeerId = isHost ? 2 : 1;
        OnPeerConnected?.Invoke(remotePeerId);

        GD.Print($"[NakamaMatchTransport] Initialized (match: {matchId}, host: {isHost}, peerId: {localPeerId})");
    }

    #region IMatchTransport Methods

    public void Send(Dictionary message)
    {
        SendJsonMessage(message);
    }

    public void Broadcast(Dictionary message)
    {
        SendJsonMessage(message);
    }

    public void SendTo(int peerId, Dictionary message)
    {
        // Nakama relay doesn't support targeted sends within a match,
        // so we send to all and the receiver filters by peer ID if needed.
        SendJsonMessage(message);
    }

    public void Host(int port)
    {
        // No-op: Nakama relay doesn't use ports
    }

    public void Connect(string address, int port)
    {
        // No-op: Connection is established via NakamaGameClient.JoinMatchAsync
    }

    public void Disconnect()
    {
        if (!_isConnected) return;
        _isConnected = false;

        var nakama = NakamaGameClient.Instance;
        if (nakama != null)
        {
            nakama.MatchDataReceived -= OnNakamaMatchData;
            nakama.MatchPresenceLeft -= OnNakamaPresenceLeft;
        }

        OnDisconnected?.Invoke("Disconnected");
        GD.Print("[NakamaMatchTransport] Disconnected");
    }

    #endregion

    #region Internal

    private void SendJsonMessage(Dictionary message)
    {
        if (!_isConnected) return;

        var nakama = NakamaGameClient.Instance;
        if (nakama == null) return;

        var jsonStr = Json.Stringify(message);
        nakama.SendMatchData(GameMessageOpCode, jsonStr);
    }

    private void OnNakamaMatchData(string matchId, long opCode, string data, string senderId)
    {
        if (opCode != GameMessageOpCode) return;
        if (matchId != _matchId) return;

        // Don't process our own messages
        var nakama = NakamaGameClient.Instance;
        if (nakama != null && senderId == nakama.UserId) return;

        // Deserialize JSON string back to Dictionary
        var parsed = Json.ParseString(data);
        if (parsed.VariantType == Variant.Type.Dictionary)
        {
            var dict = parsed.AsGodotDictionary();
            // Use remote peer ID (opposite of local)
            int remotePeerId = _isHost ? 2 : 1;
            OnMessageReceived?.Invoke(remotePeerId, dict);
        }
        else
        {
            GD.PrintErr($"[NakamaMatchTransport] Failed to parse game message: {data[..Math.Min(100, data.Length)]}");
        }
    }

    private void OnNakamaPresenceLeft(string matchId, string userId)
    {
        if (matchId != _matchId) return;

        int remotePeerId = _isHost ? 2 : 1;
        GD.Print($"[NakamaMatchTransport] Peer left: {userId}");
        OnPeerDisconnected?.Invoke(remotePeerId);
    }

    #endregion

    public override void _ExitTree()
    {
        Disconnect();
    }
}
