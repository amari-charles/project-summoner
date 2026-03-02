using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Simulation;

namespace Fateforged.Multiplayer.Authority;

/// <summary>
/// Visitor that converts SimEvents into protocol messages and broadcasts them to clients.
/// Replaces the switch statement in HostRunner.HandleTickEvents().
///
/// Adding a new SimEvent without implementing Visit() here causes a compile error.
/// </summary>
public class HostEventBroadcaster : ISimEventVisitor
{
    private readonly IMessageBroadcaster _broadcaster;
    private readonly MatchState _state;

    public HostEventBroadcaster(IMessageBroadcaster broadcaster, MatchState state)
    {
        _broadcaster = broadcaster;
        _state = state;
    }

    // ── Broadcast events (sent as protocol messages to clients) ──

    public void Visit(UnitRegisteredEvent e)
    {
        if (_state.Units.TryGetValue(e.UnitId, out var unit) && unit.NetworkId >= 0)
        {
            var pos = new Vector3(unit.Position.X, unit.Position.Y, unit.Position.Z);
            _broadcaster.Broadcast(new UnitSpawned(
                unit.NetworkId,
                e.CatalogId,
                unit.Team,
                pos,
                _state.FrameNumber,
                null, null,
                unit.SpawnTimer
            ));
        }
    }

    public void Visit(UnitDiedSimEvent e)
    {
        int networkId = -1;
        int? killerNetworkId = null;

        if (_state.Units.TryGetValue(e.UnitId, out var deadUnit))
            networkId = deadUnit.NetworkId;
        if (e.KillerUnitId >= 0 && _state.Units.TryGetValue(e.KillerUnitId, out var killerUnit))
            killerNetworkId = killerUnit.NetworkId;

        if (networkId >= 0)
        {
            _broadcaster.Broadcast(new UnitDied(networkId, killerNetworkId));
        }
    }

    public void Visit(UnitDamagedEvent e)
    {
        int targetNetworkId = -1;
        int? sourceNetworkId = null;

        if (_state.Units.TryGetValue(e.TargetUnitId, out var targetUnit))
            targetNetworkId = targetUnit.NetworkId;
        if (e.AttackerUnitId >= 0 && _state.Units.TryGetValue(e.AttackerUnitId, out var attackerUnit))
            sourceNetworkId = attackerUnit.NetworkId;

        if (targetNetworkId >= 0)
        {
            _broadcaster.Broadcast(new DamageDealt(targetNetworkId, e.Damage, e.IsCrit, sourceNetworkId));
        }
    }

    public void Visit(SummonerHpChangedEvent e)
    {
        var summoner = _state.Summoners[e.Team];
        float amount = summoner.MaxHp - e.Hp;
        _broadcaster.Broadcast(new SummonerDamaged(e.Team, amount, e.Hp));
    }

    public void Visit(GameOverEvent e)
    {
        _broadcaster.Broadcast(new MatchEnded(e.WinnerTeam, e.Reason, _state.MatchTime));
    }

    // ── Snapshot-covered events (state synced via periodic StateSnapshot) ──

    public void Visit(PhaseChangedEvent e) { }
    public void Visit(PrepTimerUpdatedEvent e) { }
    public void Visit(MatchTimeUpdatedEvent e) { }
    public void Visit(SummonerManaChangedEvent e) { }
    public void Visit(CastingStartedEvent e) { }
    public void Visit(CastingCompletedEvent e) { }
    public void Visit(CardDrawnEvent e) { }
    public void Visit(HandChangedEvent e) { }
    public void Visit(DeckRecycledEvent e) { }

    // ── Host-only events (no client equivalent needed yet) ──

    public void Visit(UnitRemovedEvent e) { }
    public void Visit(UnitAttackedEvent e) { }
    public void Visit(SpellCastEvent e) { }
    public void Visit(ProjectileHitSimEvent e) { }
    public void Visit(UnitActivationChangedEvent e) { }
    public void Visit(AttackEvadedEvent e) { }
    public void Visit(BuffAppliedSimEvent e) { }
    public void Visit(BuffExpiredSimEvent e) { }
    public void Visit(DelayedEffectFiredSimEvent e) { }
    public void Visit(SummonerDamagedEvent e)
    {
        _broadcaster.Broadcast(new SummonerDamageFlash(e.Team, e.Damage, e.AttackerUnitId));
    }

    public void Visit(SummonerDestroyedEvent e)
    {
        _broadcaster.Broadcast(new SummonerDestroyed(e.Team, e.KillerUnitId));
    }
}
