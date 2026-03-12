using System;
using System.Threading.Tasks;
using Fateforged.Multiplayer.Backend;
using Godot;
using Godot.Collections;

namespace Fateforged.Multiplayer.Transport;

/// <summary>
/// IMatchTransport implementation that routes game messages through Nakama relay.
/// Uses opCode 200 for game messages (opCode 100 is reserved for pre-battle deck exchange).
/// </summary>
public partial class NakamaMatchTransport : Node, IMatchTransport
{
    private const long GameMessageOpCode = 200;
    private const int ReconnectMaxAttempts = 30;
    private const int ReconnectAttemptDelayMs = 1000;

    private string _matchId = "";
    private bool _isHost;
    private bool _isConnected;
    private int _localPeerId;
    private bool _remotePeerConnected;
    private bool _reconnectLoopActive;
    private bool _disposed;

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
        _remotePeerConnected = true;
        _disposed = false;

        // Listen for match data and presence from NakamaGameClient
        var nakama = NakamaGameClient.Instance;
        if (nakama != null)
        {
            nakama.MatchDataReceived += OnNakamaMatchData;
            nakama.MatchPresenceJoined += OnNakamaPresenceJoined;
            nakama.MatchPresenceLeft += OnNakamaPresenceLeft;
            nakama.SocketDisconnected += OnNakamaSocketDisconnected;
        }

        // Fire connected events
        OnConnected?.Invoke();

        // Simulate peer connected for the remote player
        int remotePeerId = isHost ? 2 : 1;
        OnPeerConnected?.Invoke(remotePeerId);

        GD.Print(
            $"[RANKED][RECONNECT] Transport initialized (match: {matchId}, host: {isHost}, peerId: {localPeerId})"
        );
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
        bool wasConnected = _isConnected;
        _isConnected = false;
        _remotePeerConnected = false;
        _reconnectLoopActive = false;

        var nakama = NakamaGameClient.Instance;
        if (nakama != null)
        {
            nakama.MatchDataReceived -= OnNakamaMatchData;
            nakama.MatchPresenceJoined -= OnNakamaPresenceJoined;
            nakama.MatchPresenceLeft -= OnNakamaPresenceLeft;
            nakama.SocketDisconnected -= OnNakamaSocketDisconnected;
        }

        if (wasConnected)
        {
            OnDisconnected?.Invoke("Disconnected");
            GD.Print("[RANKED][RECONNECT] Transport disconnected");
        }
    }

    #endregion

    #region Internal

    private void SendJsonMessage(Dictionary message)
    {
        if (!_isConnected)
            return;

        var nakama = NakamaGameClient.Instance;
        if (nakama == null)
            return;

        var jsonStr = Json.Stringify(message);
        nakama.SendMatchData(GameMessageOpCode, jsonStr);
    }

    private void OnNakamaMatchData(string matchId, long opCode, string data, string senderId)
    {
        if (opCode != GameMessageOpCode)
            return;
        if (matchId != _matchId)
            return;

        // Don't process our own messages
        var nakama = NakamaGameClient.Instance;
        if (nakama != null && senderId == nakama.UserId)
            return;

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
            GD.PrintErr(
                $"[NakamaMatchTransport] Failed to parse game message: {data[..Math.Min(100, data.Length)]}"
            );
        }
    }

    private void OnNakamaPresenceLeft(string matchId, string userId)
    {
        if (matchId != _matchId)
            return;
        if (!_remotePeerConnected)
            return;

        int remotePeerId = _isHost ? 2 : 1;
        _remotePeerConnected = false;
        GD.Print($"[RANKED][RECONNECT] Peer left: {userId}");
        OnPeerDisconnected?.Invoke(remotePeerId);
    }

    private void OnNakamaPresenceJoined(string matchId, string userId, string username)
    {
        if (matchId != _matchId)
            return;

        var nakama = NakamaGameClient.Instance;
        if (nakama != null && userId == nakama.UserId)
            return;
        if (_remotePeerConnected)
            return;

        _remotePeerConnected = true;
        int remotePeerId = _isHost ? 2 : 1;
        GD.Print($"[RANKED][RECONNECT] Peer rejoined: {username} ({userId})");
        OnPeerConnected?.Invoke(remotePeerId);
    }

    private void OnNakamaSocketDisconnected()
    {
        if (_disposed || _reconnectLoopActive)
            return;

        _isConnected = false;
        OnDisconnected?.Invoke("Socket disconnected");
        _ = StartReconnectLoopAsync();
    }

    private async Task StartReconnectLoopAsync()
    {
        if (_disposed || _reconnectLoopActive)
            return;

        _reconnectLoopActive = true;

        try
        {
            var nakama = NakamaGameClient.Instance;
            if (nakama == null)
                return;

            for (int attempt = 1; attempt <= ReconnectMaxAttempts && !_disposed; attempt++)
            {
                GD.Print(
                    $"[RANKED][RECONNECT] Reconnect attempt {attempt}/{ReconnectMaxAttempts}..."
                );
                bool rejoined = await nakama.ReconnectToMatchAsync(_matchId);
                if (rejoined)
                {
                    _isConnected = true;
                    OnConnected?.Invoke();

                    if (_remotePeerConnected)
                    {
                        int remotePeerId = _isHost ? 2 : 1;
                        OnPeerConnected?.Invoke(remotePeerId);
                    }

                    GD.Print("[RANKED][RECONNECT] Reconnected to match");
                    return;
                }

                await Task.Delay(ReconnectAttemptDelayMs);
            }

            OnDisconnected?.Invoke("Reconnect timeout");
            GD.PrintErr("[RANKED][RECONNECT] Reconnect timeout");
        }
        finally
        {
            _reconnectLoopActive = false;
        }
    }

    #endregion

    public override void _ExitTree()
    {
        _disposed = true;
        Disconnect();
    }
}
