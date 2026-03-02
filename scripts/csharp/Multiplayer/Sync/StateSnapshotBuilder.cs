using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Simulation;

namespace Fateforged.Multiplayer.Sync;

/// <summary>
/// Builds state snapshots from MatchState for synchronization.
/// Reads directly from SimulationNode.Current.State instead of the scene tree.
/// Captures unit positions, HP, summoner state, and computes deterministic hashes.
/// </summary>
public class StateSnapshotBuilder
{
    private readonly MatchSession _session;

    /// <summary>
    /// Precision for position quantization (millimeters).
    /// Positions are multiplied by this before converting to int to avoid float drift.
    /// </summary>
    private const float PositionQuantizationScale = 1000f;

    /// <summary>
    /// Precision for HP quantization (tenths).
    /// HP values are multiplied by this before converting to int.
    /// </summary>
    private const float HpQuantizationScale = 10f;

    public StateSnapshotBuilder(MatchSession session)
    {
        _session = session;
    }

    /// <summary>
    /// Build a complete state snapshot from MatchState.
    /// </summary>
    public StateSnapshot Build()
    {
        var sim = SimulationNode.Current;
        if (sim == null)
        {
            return new StateSnapshot(
                _session.CurrentFrame, _session.MatchTime,
                Phase: 0, PrepTimeRemaining: 0f,
                Array.Empty<SummonerState>(), Array.Empty<UnitState>(),
                0, IsOvertime: false
            );
        }

        var state = sim.State;
        var summoners = BuildSummonerStates(state);
        var units = BuildUnitStates(state);
        var hash = ComputeStateHash(state, summoners, units);

        return new StateSnapshot(
            state.FrameNumber,
            state.MatchTime,
            (int)state.Phase,
            state.PrepTimeRemaining,
            summoners,
            units,
            hash,
            state.IsOvertime
        );
    }

    /// <summary>
    /// Compute a deterministic hash from MatchState.
    /// Used for quick desync detection without transmitting full state.
    /// </summary>
    public int ComputeHash()
    {
        var sim = SimulationNode.Current;
        if (sim == null) return 0;

        var state = sim.State;
        var summoners = BuildSummonerStates(state);
        var units = BuildUnitStates(state);
        return ComputeStateHash(state, summoners, units);
    }

    /// <summary>
    /// Build summoner state array from MatchState.
    /// </summary>
    private SummonerState[] BuildSummonerStates(MatchState state)
    {
        var summoners = new SummonerState[2];

        for (int i = 0; i < 2; i++)
        {
            var s = state.Summoners[i];
            var castPos = new Vector3(s.CastingSpawnPosition.X, s.CastingSpawnPosition.Y, s.CastingSpawnPosition.Z);

            summoners[i] = new SummonerState(
                s.Team,
                s.CurrentHp,
                s.MaxHp,
                s.Mana,
                s.MaxMana,
                s.IsCasting,
                s.CastingTimeRemaining,
                s.CastingTimeTotal,
                s.CastingCardIndex,
                castPos,
                s.CastingNetworkId,
                s.ComputeCardHash(),
                s.Hand.ToArray(),
                s.Deck.ToArray(),
                s.DiscardPile.ToArray()
            );
        }

        return summoners;
    }

    /// <summary>
    /// Build unit state array from MatchState.Units.
    /// Only includes alive units. Positions are converted from SimVector3 to Vector3.
    /// </summary>
    private UnitState[] BuildUnitStates(MatchState state)
    {
        var units = new List<UnitState>();

        foreach (var kvp in state.Units)
        {
            var unit = kvp.Value;
            if (!unit.IsAlive) continue;

            // Map TargetUnitId to TargetNetworkId
            int? targetNetworkId = null;
            if (unit.TargetUnitId.HasValue && state.Units.TryGetValue(unit.TargetUnitId.Value, out var targetUnit))
            {
                targetNetworkId = targetUnit.NetworkId;
            }
            // Also check the pre-computed TargetNetworkId if it was set by snapshot application
            if (!targetNetworkId.HasValue && unit.TargetNetworkId.HasValue)
            {
                targetNetworkId = unit.TargetNetworkId;
            }

            units.Add(new UnitState(
                unit.NetworkId,
                unit.Team,
                new Vector3(unit.Position.X, unit.Position.Y, unit.Position.Z),
                unit.CurrentHp,
                unit.MaxHp,
                targetNetworkId,
                unit.IsAlive,
                unit.ActivationState,
                (int)unit.BehaviorState,
                unit.IsFacingRight
            ));
        }

        // Sort by network ID for deterministic order
        units.Sort((a, b) => a.NetworkId.CompareTo(b.NetworkId));
        return units.ToArray();
    }

    /// <summary>
    /// Compute a deterministic hash from summoner and unit states.
    /// Uses quantized values to prevent float precision issues.
    /// </summary>
    private int ComputeStateHash(MatchState state, SummonerState[] summoners, UnitState[] units)
    {
        unchecked
        {
            int hash = 17;

            // Include frame number for temporal uniqueness
            hash = hash * 31 + (int)state.FrameNumber;

            // Hash summoner states
            foreach (var s in summoners)
            {
                hash = hash * 31 + s.Team;
                hash = hash * 31 + QuantizeHp(s.Hp);
                hash = hash * 31 + QuantizeHp(s.Mana);
            }

            // Hash unit states
            foreach (var u in units)
            {
                hash = hash * 31 + u.NetworkId;
                hash = hash * 31 + QuantizePosition(u.Position.X);
                hash = hash * 31 + QuantizePosition(u.Position.Y);
                hash = hash * 31 + QuantizePosition(u.Position.Z);
                hash = hash * 31 + QuantizeHp(u.Hp);
                hash = hash * 31 + (u.TargetNetworkId ?? -1);
                hash = hash * 31 + (u.IsAlive ? 1 : 0);
            }

            return hash;
        }
    }

    /// <summary>
    /// Quantize a position component to an integer (millimeter precision).
    /// </summary>
    private static int QuantizePosition(float value)
    {
        return (int)(value * PositionQuantizationScale);
    }

    /// <summary>
    /// Quantize an HP/mana value to an integer (tenths precision).
    /// </summary>
    private static int QuantizeHp(float value)
    {
        return (int)(value * HpQuantizationScale);
    }
}
