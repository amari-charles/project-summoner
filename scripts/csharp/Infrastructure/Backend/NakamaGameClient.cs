using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Nakama;
using NakamaClient = Nakama.Client;
using NakamaSocket = Nakama.Socket;
using NakamaSession = Nakama.Session;

namespace Fateforged.Multiplayer.Backend;

/// <summary>
/// Wrapper for the Nakama client, providing authentication, socket connection,
/// and matchmaking functionality for the game.
/// </summary>
public partial class NakamaGameClient : Node
{
    #region Configuration

    /// <summary>
    /// Server key for Nakama authentication.
    /// "defaultkey" is the default for local development.
    /// </summary>
    [Export]
    public string ServerKey { get; set; } = "defaultkey";

    /// <summary>
    /// Nakama server host address.
    /// </summary>
    [Export]
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// Nakama server HTTP port.
    /// </summary>
    [Export]
    public int Port { get; set; } = 7350;

    /// <summary>
    /// Whether to use HTTPS/WSS.
    /// </summary>
    [Export]
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Whether this runtime should use player2-local persisted identity/session files.
    /// </summary>
    private static bool IsPlayer2Instance => OS.GetCmdlineUserArgs().Contains("--player2");

    /// <summary>
    /// Path to session token storage.
    /// Player2 test instance uses a dedicated file to avoid restoring player1's session.
    /// </summary>
    private static string SessionTokenPath => IsPlayer2Instance
        ? "user://nakama_session_p2.dat"
        : "user://nakama_session.dat";

    /// <summary>
    /// Path to persisted device ID.
    /// Player2 test instance uses a dedicated file to keep test identities isolated.
    /// </summary>
    private static string DeviceIdPath => IsPlayer2Instance
        ? "user://device_id_p2.dat"
        : "user://device_id.dat";

    #endregion

    #region State

    private NakamaClient? _client;
    private ISession? _session;
    private ISocket? _socket;
    private IMatch? _activeMatch;
    private string? _activeMatchId;

    /// <summary>
    /// The underlying Nakama client instance.
    /// </summary>
    public NakamaClient? Client => _client;

    /// <summary>
    /// The current authenticated session.
    /// </summary>
    public ISession? Session => _session;

    /// <summary>
    /// The WebSocket connection for real-time features.
    /// </summary>
    public ISocket? Socket => _socket;

    /// <summary>
    /// Whether the client is authenticated.
    /// </summary>
    public bool IsAuthenticated => _session != null && !_session.IsExpired;

    /// <summary>
    /// Whether the socket is connected.
    /// </summary>
    public bool IsSocketConnected => _socket != null && _socket.IsConnected;

    /// <summary>
    /// The authenticated user's ID.
    /// </summary>
    public string? UserId => _session?.UserId;

    /// <summary>
    /// The authenticated user's username.
    /// </summary>
    public string? Username => _session?.Username;

    /// <summary>
    /// The active match ID (set after joining a match).
    /// </summary>
    public string? ActiveMatchId => _activeMatchId;

    #endregion

    #region Signals

    /// <summary>
    /// Emitted when authentication succeeds.
    /// </summary>
    [Signal]
    public delegate void AuthenticatedEventHandler(string userId, string username);

    /// <summary>
    /// Emitted when authentication fails.
    /// </summary>
    [Signal]
    public delegate void AuthenticationFailedEventHandler(string error);

    /// <summary>
    /// Emitted when the socket connects.
    /// </summary>
    [Signal]
    public delegate void SocketConnectedEventHandler();

    /// <summary>
    /// Emitted when the socket disconnects.
    /// </summary>
    [Signal]
    public delegate void SocketDisconnectedEventHandler();

    /// <summary>
    /// Emitted after joining a match found through matchmaking.
    /// Includes user IDs and summoner IDs extracted from matchmaker properties.
    /// </summary>
    [Signal]
    public delegate void MatchJoinedEventHandler(string matchId, string[] userIds, string[] summonerIds);

    /// <summary>
    /// Emitted when matchmaking is cancelled or times out.
    /// </summary>
    [Signal]
    public delegate void MatchmakingCancelledEventHandler(string reason);

    /// <summary>
    /// Emitted when a match data message is received.
    /// Data is decoded from UTF-8 bytes to a string for GDScript compatibility.
    /// </summary>
    [Signal]
    public delegate void MatchDataReceivedEventHandler(string matchId, long opCode, string data, string senderId);

    /// <summary>
    /// Emitted when a player joins the current match.
    /// </summary>
    [Signal]
    public delegate void MatchPresenceJoinedEventHandler(string matchId, string userId, string username);

    /// <summary>
    /// Emitted when a player leaves the current match.
    /// </summary>
    [Signal]
    public delegate void MatchPresenceLeftEventHandler(string matchId, string userId);

    #endregion

    #region Singleton

    /// <summary>
    /// The global NakamaGameClient instance.
    /// </summary>
    public static NakamaGameClient? Instance { get; private set; }

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DisconnectSocket();
    }

    public override void _Ready()
    {
        // Create the Nakama client
        _client = new NakamaClient(
            scheme: UseSSL ? "https" : "http",
            host: Host,
            port: Port,
            serverKey: ServerKey
        );

        GD.Print($"[NakamaGameClient] Initialized (Host: {Host}:{Port}, SSL: {UseSSL})");

        // Try to restore session from storage
        TryRestoreSession();
    }

    #endregion

    #region Authentication

    /// <summary>
    /// GDScript-callable wrapper: fire-and-forget authentication.
    /// Results are communicated via Authenticated/AuthenticationFailed signals.
    /// </summary>
    public void Authenticate()
    {
        _ = AuthenticateDeviceAsync();
    }

    /// <summary>
    /// Authenticate with a device ID (anonymous authentication).
    /// Creates a new account if the device ID doesn't exist.
    /// </summary>
    public async Task<bool> AuthenticateDeviceAsync(string deviceId = "")
    {
        if (_client == null)
        {
            EmitSignal(SignalName.AuthenticationFailed, "Client not initialized");
            return false;
        }

        try
        {
            // Use provided device ID or generate one
            if (string.IsNullOrEmpty(deviceId))
                deviceId = GetOrCreateDeviceId();

            GD.Print($"[NakamaGameClient] Authenticating with device ID: {deviceId[..8]}...");

            _session = await _client.AuthenticateDeviceAsync(deviceId, create: true);

            // Save session for later restoration
            SaveSession();

            GD.Print($"[NakamaGameClient] Authenticated as {_session.Username} ({_session.UserId})");

            // Connect WebSocket for real-time features (matchmaking, match data)
            await ConnectSocketAsync();

            EmitSignal(SignalName.Authenticated, _session.UserId, _session.Username);

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Authentication failed: {ex.Message}");
            EmitSignal(SignalName.AuthenticationFailed, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Authenticate with email and password.
    /// </summary>
    public async Task<bool> AuthenticateEmailAsync(string email, string password, bool create = false)
    {
        if (_client == null)
        {
            EmitSignal(SignalName.AuthenticationFailed, "Client not initialized");
            return false;
        }

        try
        {
            GD.Print($"[NakamaGameClient] Authenticating with email: {email}");

            _session = await _client.AuthenticateEmailAsync(email, password, create: create);

            SaveSession();

            GD.Print($"[NakamaGameClient] Authenticated as {_session.Username} ({_session.UserId})");
            EmitSignal(SignalName.Authenticated, _session.UserId, _session.Username);

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Authentication failed: {ex.Message}");
            EmitSignal(SignalName.AuthenticationFailed, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Refresh the current session if it's about to expire.
    /// </summary>
    public async Task<bool> RefreshSessionAsync()
    {
        if (_client == null || _session == null)
        {
            return false;
        }

        try
        {
            // Check if session needs refresh (within 5 minutes of expiry)
            if (!_session.IsExpired && !_session.HasExpired(DateTime.UtcNow.AddMinutes(5)))
            {
                return true; // Session is still valid
            }

            _session = await _client.SessionRefreshAsync(_session);
            SaveSession();

            GD.Print("[NakamaGameClient] Session refreshed");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Session refresh failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Log out and clear the session.
    /// </summary>
    public void Logout()
    {
        DisconnectSocket();
        _session = null;
        ClearSavedSession();
        GD.Print("[NakamaGameClient] Logged out");
    }

    #endregion

    #region Socket Connection

    /// <summary>
    /// Connect the WebSocket for real-time features (matchmaking, match data, etc.).
    /// </summary>
    public async Task<bool> ConnectSocketAsync()
    {
        if (_client == null || _session == null)
        {
            GD.PrintErr("[NakamaGameClient] Cannot connect socket: not authenticated");
            return false;
        }

        if (_socket != null && _socket.IsConnected)
        {
            return true; // Already connected
        }

        try
        {
            _socket = NakamaSocket.From(_client);

            // Set up event handlers
            _socket.Closed += OnSocketClosed;
            _socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
            _socket.ReceivedMatchState += OnMatchState;
            _socket.ReceivedMatchPresence += OnMatchPresence;

            await _socket.ConnectAsync(_session);

            GD.Print("[NakamaGameClient] Socket connected");
            EmitSignal(SignalName.SocketConnected);

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Socket connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disconnect the WebSocket.
    /// </summary>
    public void DisconnectSocket()
    {
        if (_socket == null) return;

        try
        {
            _socket.Closed -= OnSocketClosed;
            _socket.ReceivedMatchmakerMatched -= OnMatchmakerMatched;
            _socket.ReceivedMatchState -= OnMatchState;
            _socket.ReceivedMatchPresence -= OnMatchPresence;

            _socket.CloseAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Error disconnecting socket: {ex.Message}");
        }

        _socket = null;
    }

    #endregion

    #region Socket Event Handlers

    private void OnSocketClosed(string reason)
    {
        GD.Print($"[NakamaGameClient] Socket disconnected: {reason}");
        CallDeferred(MethodName.EmitSocketDisconnected);
    }

    private void EmitSocketDisconnected()
    {
        EmitSignal(SignalName.SocketDisconnected);
    }

    private async void OnMatchmakerMatched(IMatchmakerMatched matched)
    {
        GD.Print($"[NakamaGameClient] Matchmaker matched, joining match...");

        try
        {
            // Join the Nakama match (opens data channel)
            _activeMatch = await _socket!.JoinMatchAsync(matched);
            _activeMatchId = _activeMatch.Id;

            GD.Print($"[NakamaGameClient] Match joined: {_activeMatchId}");

            // Extract user IDs and summoner IDs from matchmaker properties
            var users = matched.Users.ToList();
            var userIds = new string[users.Count];
            var summonerIds = new string[users.Count];
            for (int i = 0; i < users.Count; i++)
            {
                userIds[i] = users[i].Presence.UserId;
                summonerIds[i] = users[i].StringProperties.TryGetValue("summoner_id", out var sid) ? sid : "ignis";
            }

            CallDeferred(MethodName.EmitMatchJoined, _activeMatchId, userIds, summonerIds);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Failed to join match: {ex.Message}");
        }
    }

    private void EmitMatchJoined(string matchId, string[] userIds, string[] summonerIds)
    {
        EmitSignal(SignalName.MatchJoined, matchId, userIds, summonerIds);
    }

    private void OnMatchState(IMatchState state)
    {
        var dataStr = System.Text.Encoding.UTF8.GetString(state.State);
        CallDeferred(MethodName.EmitMatchData, state.MatchId, state.OpCode, dataStr, state.UserPresence.UserId);
    }

    private void EmitMatchData(string matchId, long opCode, string data, string senderId)
    {
        EmitSignal(SignalName.MatchDataReceived, matchId, opCode, data, senderId);
    }

    private void OnMatchPresence(IMatchPresenceEvent presence)
    {
        foreach (var joined in presence.Joins)
        {
            CallDeferred(MethodName.EmitPresenceJoined, presence.MatchId, joined.UserId, joined.Username);
        }

        foreach (var left in presence.Leaves)
        {
            CallDeferred(MethodName.EmitPresenceLeft, presence.MatchId, left.UserId);
        }
    }

    private void EmitPresenceJoined(string matchId, string userId, string username)
    {
        EmitSignal(SignalName.MatchPresenceJoined, matchId, userId, username);
    }

    private void EmitPresenceLeft(string matchId, string userId)
    {
        EmitSignal(SignalName.MatchPresenceLeft, matchId, userId);
    }

    #endregion

    #region Match Data

    /// <summary>
    /// Send string data on the active match (GDScript-callable).
    /// Data is encoded as UTF-8 bytes for Nakama transport.
    /// </summary>
    public void SendMatchData(long opCode, string jsonData)
    {
        if (_socket == null || _activeMatchId == null)
        {
            GD.PrintErr("[NakamaGameClient] Cannot send match data: no active match");
            return;
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
        _ = _socket.SendMatchStateAsync(_activeMatchId, opCode, bytes);
    }

    /// <summary>
    /// Leave the current active match.
    /// </summary>
    public void LeaveMatch()
    {
        if (_socket == null || _activeMatchId == null) return;

        GD.Print($"[NakamaGameClient] Leaving match: {_activeMatchId}");
        _ = _socket.LeaveMatchAsync(_activeMatchId);
        _activeMatch = null;
        _activeMatchId = null;
    }

    /// <summary>
    /// Reconnect the realtime socket and rejoin the currently active match.
    /// </summary>
    public async Task<bool> ReconnectToActiveMatchAsync()
    {
        if (string.IsNullOrEmpty(_activeMatchId))
        {
            GD.PrintErr("[NakamaGameClient] Cannot reconnect: no active match ID");
            return false;
        }

        return await ReconnectToMatchAsync(_activeMatchId);
    }

    /// <summary>
    /// Reconnect the realtime socket and join a specific match by ID.
    /// </summary>
    public async Task<bool> ReconnectToMatchAsync(string matchId)
    {
        if (_client == null || _session == null)
        {
            GD.PrintErr("[NakamaGameClient] Cannot reconnect: client/session unavailable");
            return false;
        }

        try
        {
            if (_session.IsExpired)
            {
                var refreshed = await RefreshSessionAsync();
                if (!refreshed)
                {
                    GD.PrintErr("[NakamaGameClient] Cannot reconnect: session refresh failed");
                    return false;
                }
            }

            DisconnectSocket();

            var socketConnected = await ConnectSocketAsync();
            if (!socketConnected || _socket == null)
            {
                GD.PrintErr("[NakamaGameClient] Reconnect failed: socket did not connect");
                return false;
            }

            _activeMatch = await _socket.JoinMatchAsync(matchId);
            _activeMatchId = _activeMatch.Id;

            GD.Print($"[NakamaGameClient] Rejoined match: {_activeMatchId}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Failed to reconnect match: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Session Persistence

    private string GetOrCreateDeviceId()
    {
        string deviceId;

        if (FileAccess.FileExists(DeviceIdPath))
        {
            using var file = FileAccess.Open(DeviceIdPath, FileAccess.ModeFlags.Read);
            deviceId = file?.GetAsText().Trim() ?? Guid.NewGuid().ToString();
        }
        else
        {
            deviceId = Guid.NewGuid().ToString();

            using var writeFile = FileAccess.Open(DeviceIdPath, FileAccess.ModeFlags.Write);
            writeFile?.StoreString(deviceId);

            GD.Print($"[NakamaGameClient] Generated new device ID ({(IsPlayer2Instance ? "player2" : "player1")})");
        }

        // Use alternate identity for second test instance
        if (IsPlayer2Instance)
        {
            GD.Print("[NakamaGameClient] --player2 flag detected, using alternate device ID");
            deviceId += "-p2";
        }

        return deviceId;
    }

    private void SaveSession()
    {
        if (_session == null) return;

        try
        {
            using var file = FileAccess.Open(SessionTokenPath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(_session.AuthToken);
                file.StoreString("\n");
                file.StoreString(_session.RefreshToken);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Failed to save session: {ex.Message}");
        }
    }

    private void TryRestoreSession()
    {
        if (!FileAccess.FileExists(SessionTokenPath)) return;

        try
        {
            using var file = FileAccess.Open(SessionTokenPath, FileAccess.ModeFlags.Read);
            if (file == null) return;

            var authToken = file.GetLine().Trim();
            var refreshToken = file.GetLine().Trim();

            if (string.IsNullOrEmpty(authToken)) return;

            _session = NakamaSession.Restore(authToken, refreshToken);

            if (_session.IsExpired)
            {
                GD.Print("[NakamaGameClient] Restored session is expired, will need to re-authenticate");
                _session = null;
                ClearSavedSession();
            }
            else
            {
                GD.Print($"[NakamaGameClient] Restored session for {_session.Username}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NakamaGameClient] Failed to restore session: {ex.Message}");
            ClearSavedSession();
        }
    }

    private void ClearSavedSession()
    {
        if (FileAccess.FileExists(SessionTokenPath))
        {
            DirAccess.RemoveAbsolute(SessionTokenPath);
        }
    }

    #endregion
}
