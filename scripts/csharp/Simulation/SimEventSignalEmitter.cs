namespace Fateforged.Simulation;

/// <summary>
/// Visitor that converts SimEvents into Godot signal emissions on SimulationNode.
/// Replaces the switch statement in SimulationNode.EmitEvents().
///
/// Adding a new SimEvent without implementing Visit() here causes a compile error.
/// </summary>
public class SimEventSignalEmitter : ISimEventVisitor
{
    private readonly SimulationNode _node;

    public SimEventSignalEmitter(SimulationNode node)
    {
        _node = node;
    }

    public void Visit(PhaseChangedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.PhaseChanged, (int)e.NewPhase);
    }

    public void Visit(PrepTimerUpdatedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.PrepTimerUpdated, e.Remaining);
    }

    public void Visit(MatchTimeUpdatedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.MatchTimeUpdated, e.MatchTime);
    }

    public void Visit(SummonerHpChangedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.SummonerHpChanged, _node.RemapTeam(e.Team), e.Hp, e.MaxHp);
    }

    public void Visit(SummonerManaChangedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.SummonerManaChanged, _node.RemapTeam(e.Team), e.Mana, e.MaxMana);
    }

    public void Visit(CastingStartedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.CastingStarted, _node.RemapTeam(e.Team), e.CardIndex, e.Duration, _node.SimToLocal(e.SpawnPosition), e.CatalogId);
    }

    public void Visit(CastingCompletedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.CastingCompleted, _node.RemapTeam(e.Team), e.CardIndex, _node.SimToLocal(e.SpawnPosition), e.NetworkId);
    }

    public void Visit(CardDrawnEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.CardDrawn, _node.RemapTeam(e.Team), e.HandIndex, e.CatalogId);
    }

    public void Visit(HandChangedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.HandChanged, _node.RemapTeam(e.Team), e.Hand);
    }

    public void Visit(DeckRecycledEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.DeckRecycled, _node.RemapTeam(e.Team));
    }

    public void Visit(UnitRegisteredEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.UnitStateRegistered, e.UnitId, e.NetworkId, _node.RemapTeam(e.Team));
    }

    public void Visit(UnitRemovedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.UnitStateRemoved, e.UnitId);
    }

    public void Visit(GameOverEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.GameOver, _node.RemapTeam(e.WinnerTeam), e.Reason);
    }

    public void Visit(UnitAttackedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.UnitAttacked, e.AttackerUnitId, e.TargetUnitId);
    }

    public void Visit(UnitDamagedEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.UnitDamaged, e.TargetUnitId, e.AttackerUnitId, e.Damage, e.IsCrit);
    }

    public void Visit(UnitDiedSimEvent e)
    {
        _node.EmitSignal(SimulationNode.SignalName.UnitDiedSim, e.UnitId, e.KillerUnitId);
    }

    // ── Events without signals yet (no-ops) ──

    public void Visit(UnitActivationChangedEvent e) { }
    public void Visit(SpellCastEvent e) { }
    public void Visit(ProjectileHitSimEvent e) { }
    public void Visit(AttackEvadedEvent e) { }
    public void Visit(BuffAppliedSimEvent e) { }
    public void Visit(BuffExpiredSimEvent e) { }
    public void Visit(DelayedEffectFiredSimEvent e) { }
}
