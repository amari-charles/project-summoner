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
    void Visit(UnitDiedEvent e);
    void Visit(ProjectileHitEvent e);
    void Visit(HitscanBeamFiredEvent e);
    void Visit(UnitActivationChangedEvent e);
    void Visit(AttackEvadedEvent e);
    void Visit(BuffAppliedEvent e);
    void Visit(BuffExpiredEvent e);
    void Visit(AbilityActivatedEvent e);
    void Visit(EffectCueEvent e);
    void Visit(StatusAppliedEvent e);
    void Visit(DelayedEffectFiredEvent e);
    void Visit(SummonerDamagedEvent e);
    void Visit(SummonerDestroyedEvent e);
}
