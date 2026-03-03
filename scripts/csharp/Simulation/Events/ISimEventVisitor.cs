namespace Fateforged.Simulation.Events;

/// <summary>
/// Visitor interface for exhaustive SimEvent dispatch.
/// Adding a new SimEvent subclass without implementing Visit() here
/// causes a compile error in every ISimEventVisitor implementation.
/// </summary>
public interface ISimEventVisitor
{
    void Visit(PhaseChangedEvent e);
    void Visit(PrepTimerUpdatedEvent e);
    void Visit(MatchTimeUpdatedEvent e);
    void Visit(SummonerHpChangedEvent e);
    void Visit(SummonerManaChangedEvent e);
    void Visit(CastingStartedEvent e);
    void Visit(CastingCompletedEvent e);
    void Visit(CardDrawnEvent e);
    void Visit(HandChangedEvent e);
    void Visit(DeckRecycledEvent e);
    void Visit(UnitRegisteredEvent e);
    void Visit(UnitRemovedEvent e);
    void Visit(GameOverEvent e);
    void Visit(SpellCastEvent e);
    void Visit(UnitAttackedEvent e);
    void Visit(UnitDamagedEvent e);
    void Visit(UnitDiedSimEvent e);
    void Visit(ProjectileHitSimEvent e);
    void Visit(UnitActivationChangedEvent e);
    void Visit(AttackEvadedEvent e);
    void Visit(BuffAppliedSimEvent e);
    void Visit(BuffExpiredSimEvent e);
    void Visit(DelayedEffectFiredSimEvent e);
    void Visit(SummonerDamagedEvent e);
    void Visit(SummonerDestroyedEvent e);
}
