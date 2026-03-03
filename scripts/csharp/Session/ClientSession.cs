using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Session;

/// <summary>
/// Multiplayer client session. Does NOT tick the simulation — sends local
/// commands to the host over the network and applies snapshots received
/// from the host to a local copy of MatchState.
/// </summary>
public class ClientSession : NetworkSession
{
    private readonly MatchState _localState = new();
    private readonly List<SimEvent> _pendingEvents = new();

    public override event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public override MatchState GetState() => _localState;

    public override void SubmitCommand(ICommand command)
    {
        // TODO: send command to host over network (no transport wired yet)
        throw new NotImplementedException();
    }

    public override void Tick(float delta)
    {
        if (_pendingEvents.Count > 0)
        {
            SimEventsEmitted?.Invoke(_pendingEvents);
            _pendingEvents.Clear();
        }
    }

    /// <summary>
    /// Apply an authoritative state snapshot received from the host.
    /// </summary>
    public void ApplySnapshot(MatchState snapshot)
    {
        _localState.FrameNumber = snapshot.FrameNumber;
        _localState.MatchTime = snapshot.MatchTime;
        _localState.Phase = snapshot.Phase;
        _localState.PrepTimeRemaining = snapshot.PrepTimeRemaining;
        _localState.IsOvertime = snapshot.IsOvertime;
        _localState.WinnerTeam = snapshot.WinnerTeam;
        _localState.WinCondition = snapshot.WinCondition;

        // Copy summoner data
        for (int i = 0; i < snapshot.Summoners.Length && i < _localState.Summoners.Length; i++)
        {
            var src = snapshot.Summoners[i];
            var dst = _localState.Summoners[i];
            dst.CurrentHp = src.CurrentHp;
            dst.MaxHp = src.MaxHp;
            dst.IsAlive = src.IsAlive;
            dst.Mana = src.Mana;
            dst.MaxMana = src.MaxMana;
            dst.IsCasting = src.IsCasting;
            dst.CastingTimeRemaining = src.CastingTimeRemaining;
            dst.CastingTimeTotal = src.CastingTimeTotal;
            dst.CastingCardIndex = src.CastingCardIndex;
            dst.CastingCatalogId = src.CastingCatalogId;
            dst.CastingSpawnPosition = src.CastingSpawnPosition;
            dst.CastingNetworkId = src.CastingNetworkId;
            dst.Hand.Clear();
            dst.Hand.AddRange(src.Hand);
            dst.Deck.Clear();
            dst.Deck.AddRange(src.Deck);
            dst.DiscardPile.Clear();
            dst.DiscardPile.AddRange(src.DiscardPile);
        }

        // Copy units
        _localState.Units.Clear();
        foreach (var (unitId, unitData) in snapshot.Units)
        {
            _localState.Units[unitId] = unitData;
        }

        // Copy projectiles
        _localState.Projectiles.Clear();
        foreach (var (projId, projData) in snapshot.Projectiles)
        {
            _localState.Projectiles[projId] = projData;
        }
    }
}
