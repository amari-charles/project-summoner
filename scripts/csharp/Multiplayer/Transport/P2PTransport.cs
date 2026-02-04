using System;
using Godot;
using Godot.Collections;

namespace Fateforged.Multiplayer.Transport;

/// <summary>
/// ENet-based P2P transport for multiplayer matches.
/// Can act as host (server) or client.
/// </summary>
public partial class P2PTransport : Node, IMatchTransport
{
    private ENetMultiplayerPeer? _peer;

    #region Godot Signals (for GDScript interop)

    [Signal]
    public delegate void PeerConnectedEventHandler(int peerId);

    [Signal]
    public delegate void PeerDisconnectedEventHandler(int peerId);

    [Signal]
    public delegate void ConnectedEventHandler();

    [Signal]
    public delegate void DisconnectedEventHandler(string reason);

    [Signal]
    public delegate void MessageReceivedEventHandler(int senderId, Dictionary message);

    #endregion

    #region IMatchTransport Properties

    public new bool IsConnected { get; private set; }
    public int LocalPeerId => Multiplayer.GetUniqueId();
    public bool IsHost { get; private set; }

    #endregion

    #region IMatchTransport Events (for C# interop)

    public event Action<int, Dictionary>? OnMessageReceived;
    public event Action<int>? OnPeerConnected;
    public event Action<int>? OnPeerDisconnected;
    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;

    #endregion

    #region Configuration

    /// <summary>
    /// Default port for P2P connections.
    /// </summary>
    public const int DefaultPort = 7777;

    /// <summary>
    /// Maximum number of clients (for 1v1, just need 1).
    /// </summary>
    public const int MaxClients = 1;

    #endregion

    public override void _Ready()
    {
        // Connect to multiplayer signals
        Multiplayer.PeerConnected += HandlePeerConnected;
        Multiplayer.PeerDisconnected += HandlePeerDisconnected;
        Multiplayer.ConnectedToServer += HandleConnectedToServer;
        Multiplayer.ConnectionFailed += HandleConnectionFailed;
        Multiplayer.ServerDisconnected += HandleServerDisconnected;
    }

    public override void _ExitTree()
    {
        Disconnect();

        Multiplayer.PeerConnected -= HandlePeerConnected;
        Multiplayer.PeerDisconnected -= HandlePeerDisconnected;
        Multiplayer.ConnectedToServer -= HandleConnectedToServer;
        Multiplayer.ConnectionFailed -= HandleConnectionFailed;
        Multiplayer.ServerDisconnected -= HandleServerDisconnected;
    }

    #region IMatchTransport Methods

    public void Host(int port)
    {
        if (_peer != null)
        {
            GD.PrintErr("[P2PTransport] Already connected, disconnect first");
            return;
        }

        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(port, MaxClients);

        if (error != Error.Ok)
        {
            GD.PrintErr($"[P2PTransport] Failed to create server: {error}");
            _peer = null;
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        IsHost = true;
        IsConnected = true;

        GD.Print($"[P2PTransport] Hosting on port {port}");
        OnConnected?.Invoke();
        EmitSignal(SignalName.Connected);
    }

    public void Connect(string address, int port)
    {
        if (_peer != null)
        {
            GD.PrintErr("[P2PTransport] Already connected, disconnect first");
            return;
        }

        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(address, port);

        if (error != Error.Ok)
        {
            GD.PrintErr($"[P2PTransport] Failed to create client: {error}");
            _peer = null;
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        IsHost = false;

        GD.Print($"[P2PTransport] Connecting to {address}:{port}...");
    }

    public void Disconnect()
    {
        if (_peer == null) return;

        _peer.Close();
        Multiplayer.MultiplayerPeer = null;
        _peer = null;
        IsConnected = false;
        IsHost = false;

        GD.Print("[P2PTransport] Disconnected");
    }

    public void Send(Dictionary message)
    {
        if (!IsConnected || _peer == null) return;

        // Client sends to host (peer 1)
        RpcId(1, MethodName.ReceiveMessage, message);
    }

    public void Broadcast(Dictionary message)
    {
        if (!IsConnected || !IsHost || _peer == null) return;

        // Host broadcasts to all clients
        Rpc(MethodName.ReceiveMessage, message);
    }

    public void SendTo(int peerId, Dictionary message)
    {
        if (!IsConnected || !IsHost || _peer == null) return;

        RpcId(peerId, MethodName.ReceiveMessage, message);
    }

    #endregion

    #region RPC

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveMessage(Dictionary message)
    {
        var senderId = Multiplayer.GetRemoteSenderId();
        OnMessageReceived?.Invoke(senderId, message);
        EmitSignal(SignalName.MessageReceived, senderId, message);
    }

    #endregion

    #region Multiplayer Signal Handlers

    private void HandlePeerConnected(long id)
    {
        GD.Print($"[P2PTransport] Peer connected: {id}");
        OnPeerConnected?.Invoke((int)id);
        EmitSignal(SignalName.PeerConnected, (int)id);
    }

    private void HandlePeerDisconnected(long id)
    {
        GD.Print($"[P2PTransport] Peer disconnected: {id}");
        OnPeerDisconnected?.Invoke((int)id);
        EmitSignal(SignalName.PeerDisconnected, (int)id);
    }

    private void HandleConnectedToServer()
    {
        IsConnected = true;
        GD.Print("[P2PTransport] Connected to server");
        OnConnected?.Invoke();
        EmitSignal(SignalName.Connected);
    }

    private void HandleConnectionFailed()
    {
        GD.PrintErr("[P2PTransport] Connection failed");
        _peer = null;
        Multiplayer.MultiplayerPeer = null;
        OnDisconnected?.Invoke("Connection failed");
        EmitSignal(SignalName.Disconnected, "Connection failed");
    }

    private void HandleServerDisconnected()
    {
        IsConnected = false;
        GD.Print("[P2PTransport] Server disconnected");
        OnDisconnected?.Invoke("Server disconnected");
        EmitSignal(SignalName.Disconnected, "Server disconnected");
    }

    #endregion
}
