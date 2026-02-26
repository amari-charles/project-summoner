using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using ProjectSummoner.Units;

namespace Fateforged.Multiplayer.Sync;

/// <summary>
/// Builds state snapshots from actual game state for synchronization.
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
    /// Build a complete state snapshot from the current game state.
    /// </summary>
    public StateSnapshot Build()
    {
        var summoners = BuildSummonerStates();
        var units = BuildUnitStates();
        var hash = ComputeStateHash(summoners, units);

        return new StateSnapshot(
            _session.CurrentFrame,
            _session.MatchTime,
            Phase: 0,
            PrepTimeRemaining: 0f,
            summoners,
            units,
            hash,
            IsOvertime: false
        );
    }

    /// <summary>
    /// Compute a deterministic hash from the current game state.
    /// Used for quick desync detection without transmitting full state.
    /// </summary>
    public int ComputeHash()
    {
        var summoners = BuildSummonerStates();
        var units = BuildUnitStates();
        return ComputeStateHash(summoners, units);
    }

    /// <summary>
    /// Build summoner state array from the scene tree.
    /// </summary>
    private SummonerState[] BuildSummonerStates()
    {
        var summoners = new List<SummonerState>();
        var sceneTree = _session.GetTree();
        if (sceneTree == null) return Array.Empty<SummonerState>();

        // Get summoners from the "summoners" group
        var summonerNodes = sceneTree.GetNodesInGroup("summoners");

        foreach (var node in summonerNodes)
        {
            if (node is not Node3D summoner) continue;

            // Get team from summoner
            var teamVar = summoner.Get("team");
            if (teamVar.VariantType == Variant.Type.Nil) continue;
            int team = teamVar.AsInt32();

            // Get HP values
            var currentHpVar = summoner.Get("current_hp");
            var maxHpVar = summoner.Get("max_hp");
            float currentHp = currentHpVar.VariantType != Variant.Type.Nil ? currentHpVar.AsSingle() : 0;
            float maxHp = maxHpVar.VariantType != Variant.Type.Nil ? maxHpVar.AsSingle() : 100;

            // Get mana values
            var manaVar = summoner.Get("mana");
            var maxManaVar = summoner.Get("max_mana");
            float mana = manaVar.VariantType != Variant.Type.Nil ? manaVar.AsSingle() : 0;
            float maxMana = maxManaVar.VariantType != Variant.Type.Nil ? maxManaVar.AsSingle() : 10;

            summoners.Add(new SummonerState(team, currentHp, maxHp, mana, maxMana,
                IsCasting: false, CastingTimeRemaining: 0f, CastingTimeTotal: 0f,
                CastingCardIndex: -1, CastingSpawnPosition: Vector3.Zero, CastingNetworkId: -1,
                CardStateHash: 0));
        }

        // Sort by team for deterministic order
        summoners.Sort((a, b) => a.Team.CompareTo(b.Team));
        return summoners.ToArray();
    }

    /// <summary>
    /// Build unit state array from the NetworkIdRegistry.
    /// </summary>
    private UnitState[] BuildUnitStates()
    {
        var units = new List<UnitState>();

        foreach (var networkId in _session.NetworkIds.GetAllIds())
        {
            var node = _session.NetworkIds.GetNode(networkId);
            if (node is not Unit3D unit) continue;
            if (!unit.IsAlive) continue;

            // Get target's network ID if it has one
            int? targetNetworkId = null;
            if (unit.CurrentTarget is Unit3D targetUnit && targetUnit.NetworkId >= 0)
            {
                targetNetworkId = targetUnit.NetworkId;
            }

            units.Add(new UnitState(
                networkId,
                unit.GlobalPosition,
                unit.CurrentHp,
                targetNetworkId,
                unit.IsAlive,
                ActivationState: 1
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
    private int ComputeStateHash(SummonerState[] summoners, UnitState[] units)
    {
        unchecked
        {
            int hash = 17;

            // Include frame number for temporal uniqueness
            hash = hash * 31 + (int)_session.CurrentFrame;

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
