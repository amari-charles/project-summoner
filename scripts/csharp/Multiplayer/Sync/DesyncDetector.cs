using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Simulation;
using ProjectSummoner.Units;

namespace Fateforged.Multiplayer.Sync;

/// <summary>
/// Detects state desynchronization between host and client.
/// Tracks hash mismatches and triggers full state resync when needed.
/// </summary>
public class DesyncDetector
{
    private readonly MatchSession _session;

    /// <summary>
    /// Number of consecutive hash mismatches before triggering a full resync.
    /// Single mismatches may be due to timing/ordering, so we wait for confirmation.
    /// </summary>
    private const int DesyncThreshold = 3;

    /// <summary>
    /// Maximum number of desync events to log for debugging.
    /// </summary>
    private const int MaxDesyncLogEntries = 100;

    /// <summary>
    /// Interval between hash reports from client to host (in frames).
    /// Every 60 frames at 60fps = once per second.
    /// </summary>
    public const int HashReportIntervalFrames = 60;

    /// <summary>
    /// Counter for consecutive hash mismatches.
    /// </summary>
    private int _consecutiveMismatches;

    /// <summary>
    /// Last received client hash (host only).
    /// </summary>
    private int _lastClientHash;

    /// <summary>
    /// Frame number of last received client hash (host only).
    /// </summary>
    private long _lastClientHashFrame;

    /// <summary>
    /// Log of desync events for debugging.
    /// </summary>
    private readonly List<DesyncEvent> _desyncLog = new();

    /// <summary>
    /// True if currently in a desync state awaiting resync.
    /// </summary>
    public bool IsDesynced { get; private set; }

    /// <summary>
    /// Total number of desyncs detected this session.
    /// </summary>
    public int TotalDesyncs { get; private set; }

    /// <summary>
    /// Event fired when a desync is detected.
    /// </summary>
    public event Action<DesyncEvent>? OnDesyncDetected;

    /// <summary>
    /// Event fired when a resync is completed.
    /// </summary>
    public event Action? OnResyncCompleted;

    public DesyncDetector(MatchSession session)
    {
        _session = session;
    }

    /// <summary>
    /// Called by host when receiving a StateHashReport from a client.
    /// Compares the client's hash with the authoritative hash.
    /// </summary>
    /// <param name="clientHash">Hash computed by the client</param>
    /// <param name="clientFrame">Frame number the hash was computed for</param>
    /// <returns>True if hashes match, false if desync detected</returns>
    public bool CheckClientHash(int clientHash, long clientFrame)
    {
        _lastClientHash = clientHash;
        _lastClientHashFrame = clientFrame;

        // Skip comparison if client is significantly behind (still syncing).
        // The server state has advanced far past the client's hash frame,
        // so comparing would always produce a false mismatch.
        var serverFrame = SimulationNode.Current?.State.FrameNumber ?? 0;
        if (Math.Abs(serverFrame - clientFrame) > 60) // More than ~1 second behind
            return true;

        // Compute our authoritative hash
        var builder = new StateSnapshotBuilder(_session);
        int authoritativeHash = builder.ComputeHash();

        if (clientHash != authoritativeHash)
        {
            _consecutiveMismatches++;

            var desyncEvent = new DesyncEvent(
                Frame: clientFrame,
                MatchTime: _session.MatchTime,
                ClientHash: clientHash,
                ServerHash: authoritativeHash,
                MismatchCount: _consecutiveMismatches
            );

            LogDesync(desyncEvent);
            GD.PrintErr($"[DesyncDetector] Hash mismatch #{_consecutiveMismatches}: client={clientHash}, server={authoritativeHash} at frame {clientFrame}");

            if (_consecutiveMismatches >= DesyncThreshold)
            {
                TriggerDesync(desyncEvent);
            }

            return false;
        }

        // Hashes match - reset mismatch counter
        if (_consecutiveMismatches > 0)
        {
            GD.Print($"[DesyncDetector] Hash match restored after {_consecutiveMismatches} mismatches");
        }
        _consecutiveMismatches = 0;
        return true;
    }

    /// <summary>
    /// Called by client when receiving a StateSnapshot from the host.
    /// Compares with local state and applies corrections if needed.
    /// </summary>
    /// <param name="snapshot">State snapshot from the host</param>
    /// <returns>True if states matched, false if corrections were applied</returns>
    public bool ApplySnapshot(StateSnapshot snapshot)
    {
        var builder = new StateSnapshotBuilder(_session);
        int localHash = builder.ComputeHash();

        if (localHash != snapshot.StateHash)
        {
            GD.PrintErr($"[DesyncDetector] Client state mismatch: local={localHash}, server={snapshot.StateHash}");

            // Apply corrections from snapshot
            ApplyStateCorrections(snapshot);

            _consecutiveMismatches++;
            TotalDesyncs++;

            if (IsDesynced)
            {
                // We were awaiting resync - check if this snapshot fixes it
                int newHash = builder.ComputeHash();
                if (newHash == snapshot.StateHash)
                {
                    CompleteResync();
                }
            }

            return false;
        }

        // States match
        _consecutiveMismatches = 0;
        if (IsDesynced)
        {
            CompleteResync();
        }
        return true;
    }

    /// <summary>
    /// Apply state corrections from a host snapshot.
    /// Updates unit positions, HP, and summoner state to match the host.
    /// </summary>
    private void ApplyStateCorrections(StateSnapshot snapshot)
    {
        GD.Print($"[DesyncDetector] Applying state corrections from frame {snapshot.Frame}");

        // Correct unit states
        foreach (var unitState in snapshot.Units)
        {
            var node = _session.NetworkIds.GetNode(unitState.NetworkId);
            if (node is Unit3D unit)
            {
                // Correct position (with some tolerance for interpolation)
                float positionDiff = unit.GlobalPosition.DistanceTo(unitState.Position);
                if (positionDiff > 0.5f) // Only correct significant deviations
                {
                    GD.Print($"[DesyncDetector] Correcting unit {unitState.NetworkId} position: {unit.GlobalPosition} -> {unitState.Position}");
                    unit.GlobalPosition = unitState.Position;
                }

                // Note: HP corrections would require UnitHealth to expose SetHp method
                // For now, we log the discrepancy
                if (Mathf.Abs(unit.CurrentHp - unitState.Hp) > 1f)
                {
                    GD.PrintErr($"[DesyncDetector] Unit {unitState.NetworkId} HP mismatch: {unit.CurrentHp} vs {unitState.Hp}");
                }
            }
        }

        // Correct summoner states
        var sceneTree = _session.GetTree();
        if (sceneTree != null)
        {
            var summonerNodes = sceneTree.GetNodesInGroup("summoners");
            foreach (var summonerState in snapshot.Summoners)
            {
                foreach (var node in summonerNodes)
                {
                    if (node is not Node3D summoner) continue;

                    var teamVar = summoner.Get("team");
                    if (teamVar.VariantType == Variant.Type.Nil) continue;

                    // GDScript "team" is in local perspective; snapshot Team is in network perspective.
                    // Convert local → network before comparing so client-side doesn't false-match.
                    var localTeam = new LocalTeam(teamVar.AsInt32());
                    var networkTeam = SimulationNode.Current?.ToNetworkTeam(localTeam)
                        ?? new NetworkTeam(localTeam.Value);

                    if (networkTeam.Value == summonerState.Team)
                    {
                        // Log HP discrepancy (would need to call take_damage or heal to correct)
                        var currentHpVar = summoner.Get("current_hp");
                        if (currentHpVar.VariantType != Variant.Type.Nil)
                        {
                            float currentHp = currentHpVar.AsSingle();
                            if (Mathf.Abs(currentHp - summonerState.Hp) > 1f)
                            {
                                GD.PrintErr($"[DesyncDetector] Summoner {networkTeam} HP mismatch: {currentHp} vs {summonerState.Hp}");
                            }
                        }
                        break;
                    }
                }
            }
        }

        // Update match time to server time
        _session.MatchTime = snapshot.MatchTime;
    }

    /// <summary>
    /// Trigger a full desync state requiring resync.
    /// </summary>
    private void TriggerDesync(DesyncEvent desyncEvent)
    {
        if (IsDesynced) return; // Already in desync state

        IsDesynced = true;
        TotalDesyncs++;
        GD.PrintErr($"[DesyncDetector] DESYNC TRIGGERED after {DesyncThreshold} consecutive mismatches");

        OnDesyncDetected?.Invoke(desyncEvent);
    }

    /// <summary>
    /// Mark resync as complete.
    /// </summary>
    private void CompleteResync()
    {
        IsDesynced = false;
        _consecutiveMismatches = 0;
        GD.Print("[DesyncDetector] Resync completed");
        OnResyncCompleted?.Invoke();
    }

    /// <summary>
    /// Log a desync event for debugging.
    /// </summary>
    private void LogDesync(DesyncEvent desyncEvent)
    {
        if (_desyncLog.Count >= MaxDesyncLogEntries)
        {
            _desyncLog.RemoveAt(0);
        }
        _desyncLog.Add(desyncEvent);
    }

    /// <summary>
    /// Get the desync log for debugging.
    /// </summary>
    public IReadOnlyList<DesyncEvent> GetDesyncLog() => _desyncLog.AsReadOnly();

    /// <summary>
    /// Clear the desync log.
    /// </summary>
    public void ClearDesyncLog()
    {
        _desyncLog.Clear();
    }

    /// <summary>
    /// Reset the detector state (e.g., for new match).
    /// </summary>
    public void Reset()
    {
        _consecutiveMismatches = 0;
        _lastClientHash = 0;
        _lastClientHashFrame = 0;
        IsDesynced = false;
        TotalDesyncs = 0;
        _desyncLog.Clear();
    }
}

/// <summary>
/// Record of a desync event for debugging.
/// </summary>
public readonly record struct DesyncEvent(
    long Frame,
    float MatchTime,
    int ClientHash,
    int ServerHash,
    int MismatchCount
);
