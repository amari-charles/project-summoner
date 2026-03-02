using System;
using System.Collections.Generic;
using Fateforged.Session;
using Fateforged.Simulation;
using Godot;

namespace Fateforged.View;

/// <summary>
/// Central coordinator for all 3D battlefield entities.
/// Three jobs: manage shell lifecycles, dispatch discrete events to the correct shell,
/// and maintain a registry for O(1) lookup.
///
/// Implements ISimEventVisitor for exhaustive event dispatch.
/// </summary>
public partial class EntityManager : Node3D, ISimEventVisitor
{
    private IGameSession? _session;
    private readonly Dictionary<int, UnitVisual> _unitRegistry = new();
    private readonly Dictionary<int, ProjectileVisual> _projectileRegistry = new();
    private readonly Dictionary<int, SummonerVisual> _summonerRegistry = new();

    // --- Initialization ---

    public void Initialize(IGameSession session)
    {
        throw new NotImplementedException();
    }

    public void RegisterSummonerVisual(SummonerVisual shell, int teamIndex)
    {
        throw new NotImplementedException();
    }

    // --- Lifecycle (called each frame) ---

    public override void _PhysicsProcess(double delta)
    {
        // Diff MatchState entity lists against registries.
        // Spawn shells for new IDs, destroy shells for removed IDs.
        throw new NotImplementedException();
    }

    // --- Shell Factory ---

    private UnitVisual SpawnUnitShell(UnitData unitData)
    {
        throw new NotImplementedException();
    }

    private ProjectileVisual SpawnProjectileShell(SimProjectileData projData)
    {
        throw new NotImplementedException();
    }

    private void DestroyShell(int entityId)
    {
        throw new NotImplementedException();
    }

    // --- ISimEventVisitor (event dispatch to shells) ---

    public void Visit(UnitAttackedEvent e)
    {
        // _unitRegistry.TryGetValue(e.AttackerUnitId) -> shell.PlayAttackAnimation()
        throw new NotImplementedException();
    }

    public void Visit(UnitDamagedEvent e)
    {
        // _unitRegistry.TryGetValue(e.TargetUnitId) -> shell.FlashDamage(e.Damage, e.IsCrit)
        throw new NotImplementedException();
    }

    public void Visit(UnitDiedSimEvent e)
    {
        // _unitRegistry.TryGetValue(e.UnitId) -> shell.BeginDeath()
        // + VFXManager death VFX at position
        throw new NotImplementedException();
    }

    public void Visit(ProjectileHitSimEvent e)
    {
        // _projectileRegistry.TryGetValue(e.ProjectileId) -> shell.PlayImpactAndDestroy()
        throw new NotImplementedException();
    }

    public void Visit(SummonerDamagedEvent e)
    {
        // _summonerRegistry.TryGetValue(e.Team) -> shell.FlashDamage()
        throw new NotImplementedException();
    }

    public void Visit(SummonerDestroyedEvent e)
    {
        // _summonerRegistry.TryGetValue(e.Team) -> shell.BeginDeath()
        throw new NotImplementedException();
    }

    public void Visit(SummonerHpChangedEvent e) { } // HUD handles via polling

    public void Visit(AttackEvadedEvent e)
    {
        // _unitRegistry.TryGetValue(e.TargetUnitId) -> shell.ShowEvadeText()
        throw new NotImplementedException();
    }

    public void Visit(BuffAppliedSimEvent e)
    {
        // _unitRegistry.TryGetValue(e.TargetUnitId) -> shell.ShowBuffIcon(e.EffectType)
        throw new NotImplementedException();
    }

    public void Visit(SpellCastEvent e)
    {
        // VFXManager spell VFX at e.Position
        throw new NotImplementedException();
    }

    public void Visit(DelayedEffectFiredSimEvent e)
    {
        // VFXManager AoE VFX at e.Position with e.AoeRadius
        throw new NotImplementedException();
    }

    // --- No-op visitors (HUD handles these, or no visual action needed) ---
    public void Visit(PhaseChangedEvent e) { }
    public void Visit(PrepTimerUpdatedEvent e) { }
    public void Visit(MatchTimeUpdatedEvent e) { }
    public void Visit(SummonerManaChangedEvent e) { }
    public void Visit(CastingStartedEvent e) { }
    public void Visit(CastingCompletedEvent e) { }
    public void Visit(CardDrawnEvent e) { }
    public void Visit(HandChangedEvent e) { }
    public void Visit(DeckRecycledEvent e) { }
    public void Visit(UnitRegisteredEvent e) { }
    public void Visit(UnitRemovedEvent e) { }
    public void Visit(GameOverEvent e) { }
    public void Visit(UnitActivationChangedEvent e) { }
    public void Visit(BuffExpiredSimEvent e) { }

    // --- Global Control ---

    public void Pause() { throw new NotImplementedException(); }
    public void Resume() { throw new NotImplementedException(); }
}
