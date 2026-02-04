using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Nakama;
using Fateforged.Multiplayer.Backend;
using Fateforged.Multiplayer.Ranking;

namespace Fateforged.Multiplayer.Matchmaking;

/// <summary>
/// Service for managing ranked matchmaking via Nakama.
/// Handles queue joining, cancellation, and match found events.
/// </summary>
public partial class MatchmakingService : Node
{
    #region Configuration

    /// <summary>
    /// Minimum number of players for a match.
    /// </summary>
    private const int MinPlayers = 2;

    /// <summary>
    /// Maximum number of players for a match.
    /// </summary>
    private const int MaxPlayers = 2;

    /// <summary>
    /// Default rating range for matchmaking (+/- from player rating).
    /// </summary>
    private const int DefaultRatingRange = 200;

    // Note: Rating range expansion is handled server-side by Nakama matchmaker

    #endregion

    #region State

    private IMatchmakerTicket? _currentTicket;
    private float _queueStartTime;
    private MatchmakingOptions? _currentOptions;

    /// <summary>
    /// Whether currently in the matchmaking queue.
    /// </summary>
    public bool IsInQueue => _currentTicket != null;

    /// <summary>
    /// Time spent in queue (in seconds).
    /// </summary>
    public float QueueTime => IsInQueue ? (float)(Time.GetTicksMsec() / 1000.0) - _queueStartTime : 0;

    /// <summary>
    /// The current matchmaking ticket.
    /// </summary>
    public IMatchmakerTicket? CurrentTicket => _currentTicket;

    #endregion

    #region Signals

    /// <summary>
    /// Emitted when a match is found.
    /// </summary>
    [Signal]
    public delegate void MatchFoundEventHandler(string matchId, string opponentUserId, string opponentUsername, int opponentRating);

    /// <summary>
    /// Emitted when matchmaking fails or is cancelled.
    /// </summary>
    [Signal]
    public delegate void MatchmakingCancelledEventHandler(string reason);

    /// <summary>
    /// Emitted when queue status changes.
    /// </summary>
    [Signal]
    public delegate void QueueStatusChangedEventHandler(bool isInQueue, float queueTime);

    /// <summary>
    /// Emitted when an error occurs during matchmaking.
    /// </summary>
    [Signal]
    public delegate void MatchmakingErrorEventHandler(string error);

    #endregion

    #region Singleton

    /// <summary>
    /// The global MatchmakingService instance.
    /// </summary>
    public static MatchmakingService? Instance { get; private set; }

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

        // Leave queue if in one
        _ = LeaveQueueAsync();
    }

    public override void _Ready()
    {
        // Subscribe to NakamaGameClient match found events
        var nakama = NakamaGameClient.Instance;
        if (nakama != null)
        {
            nakama.MatchFound += OnMatchFound;
        }

        GD.Print("[MatchmakingService] Initialized");
    }

    public override void _Process(double delta)
    {
        // Periodically emit queue status while in queue
        if (IsInQueue && Engine.GetProcessFrames() % 30 == 0) // Every 0.5 seconds at 60fps
        {
            EmitSignal(SignalName.QueueStatusChanged, true, QueueTime);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Join the ranked matchmaking queue.
    /// </summary>
    /// <param name="options">Matchmaking options (mode, rating, etc.)</param>
    /// <returns>True if successfully joined queue</returns>
    public async Task<bool> JoinQueueAsync(MatchmakingOptions? options = null)
    {
        var nakama = NakamaGameClient.Instance;
        if (nakama == null || !nakama.IsAuthenticated)
        {
            GD.PrintErr("[MatchmakingService] Cannot join queue: not authenticated");
            EmitSignal(SignalName.MatchmakingError, "Not authenticated");
            return false;
        }

        if (!nakama.IsSocketConnected)
        {
            GD.PrintErr("[MatchmakingService] Cannot join queue: socket not connected");
            EmitSignal(SignalName.MatchmakingError, "Socket not connected");
            return false;
        }

        if (IsInQueue)
        {
            GD.Print("[MatchmakingService] Already in queue");
            return true;
        }

        // Use provided options or create defaults
        options ??= CreateDefaultOptions();
        _currentOptions = options;

        try
        {
            // Build matchmaker query
            var query = BuildMatchmakerQuery(options);
            var stringProperties = new Dictionary<string, string>
            {
                ["mode"] = options.Mode,
                ["summoner_id"] = options.SummonerId ?? ""
            };
            var numericProperties = new Dictionary<string, double>
            {
                ["rating"] = options.Rating
            };

            GD.Print($"[MatchmakingService] Joining queue: rating={options.Rating}, range=±{options.RatingRange}");
            GD.Print($"[MatchmakingService] Query: {query}");

            _currentTicket = await nakama.Socket!.AddMatchmakerAsync(
                query: query,
                minCount: MinPlayers,
                maxCount: MaxPlayers,
                stringProperties: stringProperties,
                numericProperties: numericProperties
            );

            _queueStartTime = (float)(Time.GetTicksMsec() / 1000.0);

            GD.Print($"[MatchmakingService] Joined queue with ticket: {_currentTicket.Ticket}");
            EmitSignal(SignalName.QueueStatusChanged, true, 0f);

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchmakingService] Failed to join queue: {ex.Message}");
            EmitSignal(SignalName.MatchmakingError, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Leave the matchmaking queue.
    /// </summary>
    public async Task LeaveQueueAsync()
    {
        if (!IsInQueue || _currentTicket == null)
        {
            return;
        }

        var nakama = NakamaGameClient.Instance;
        if (nakama?.Socket == null)
        {
            _currentTicket = null;
            _currentOptions = null;
            return;
        }

        try
        {
            await nakama.Socket.RemoveMatchmakerAsync(_currentTicket);
            GD.Print("[MatchmakingService] Left queue");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchmakingService] Error leaving queue: {ex.Message}");
        }

        _currentTicket = null;
        _currentOptions = null;
        EmitSignal(SignalName.QueueStatusChanged, false, 0f);
        EmitSignal(SignalName.MatchmakingCancelled, "Player cancelled");
    }

    /// <summary>
    /// Get the estimated wait time based on current queue time.
    /// Returns a human-readable string.
    /// </summary>
    public string GetEstimatedWaitTime()
    {
        if (!IsInQueue) return "Not in queue";

        var queueTime = QueueTime;
        if (queueTime < 10) return "Finding match...";
        if (queueTime < 30) return "Searching...";
        if (queueTime < 60) return "Expanding search...";
        return "Extended search...";
    }

    #endregion

    #region Private Methods

    private MatchmakingOptions CreateDefaultOptions()
    {
        // Get player's rating from RankingService (autoload)
        int rating = EloCalculator.StartingElo;
        var rankingService = GetNodeOrNull<RankingService>("RankingService");
        if (rankingService != null)
        {
            rating = rankingService.GetRating();
        }

        return new MatchmakingOptions
        {
            Mode = "ranked_1v1",
            Rating = rating,
            RatingRange = DefaultRatingRange
        };
    }

    private string BuildMatchmakerQuery(MatchmakingOptions options)
    {
        // Nakama matchmaker query syntax
        // Find players with same mode and compatible rating
        int minRating = Math.Max(0, options.Rating - options.RatingRange);
        int maxRating = options.Rating + options.RatingRange;

        return $"+properties.mode:{options.Mode} +properties.rating:>={minRating} +properties.rating:<={maxRating}";
    }

    private void OnMatchFound(string matchId, string[] userIds)
    {
        GD.Print($"[MatchmakingService] Match found: {matchId}");

        // Clear queue state
        _currentTicket = null;
        _currentOptions = null;

        // Find opponent info
        var nakama = NakamaGameClient.Instance;
        string opponentUserId = "";
        string opponentUsername = "Opponent";
        int opponentRating = EloCalculator.StartingElo;

        if (nakama != null)
        {
            foreach (var userId in userIds)
            {
                if (userId != nakama.UserId)
                {
                    opponentUserId = userId;
                    break;
                }
            }
        }

        EmitSignal(SignalName.QueueStatusChanged, false, QueueTime);
        EmitSignal(SignalName.MatchFound, matchId, opponentUserId, opponentUsername, opponentRating);
    }

    #endregion
}

/// <summary>
/// Options for joining the matchmaking queue.
/// </summary>
public class MatchmakingOptions
{
    /// <summary>
    /// Game mode to match for.
    /// </summary>
    public string Mode { get; set; } = "ranked_1v1";

    /// <summary>
    /// Player's ELO rating.
    /// </summary>
    public int Rating { get; set; } = EloCalculator.StartingElo;

    /// <summary>
    /// Rating range for matching (+/- from rating).
    /// </summary>
    public int RatingRange { get; set; } = 200;

    /// <summary>
    /// Optional region for regional matching.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Player's selected summoner ID.
    /// </summary>
    public string? SummonerId { get; set; }
}
