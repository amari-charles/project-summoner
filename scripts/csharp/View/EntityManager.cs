using System.Collections.Generic;
using Fateforged.Session;
using Fateforged.Simulation;
using Godot;
using Fateforged.Units;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;

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

    private bool _isPaused;

    // --- Initialization ---

    public void Initialize(IGameSession session)
    {
        _session = session;
        _session.SimEventsEmitted += OnSimEvents;
    }

    /// <summary>
    /// GDScript-callable overload. IGameSession is not visible to GDScript,
    /// so this accepts a Node and casts internally.
    /// </summary>
    public void Initialize(Node sessionNode)
    {
        if (sessionNode is IGameSession session)
            Initialize(session);
        else
            GD.PrintErr($"[EntityManager] Node {sessionNode.Name} does not implement IGameSession");
    }

    public void RegisterSummonerVisual(SummonerVisual shell, int teamIndex)
    {
        _summonerRegistry[teamIndex] = shell;
    }

    // --- Lifecycle (called each frame) ---

    public override void _PhysicsProcess(double delta)
    {
        if (_session == null || _isPaused) return;

        var state = _session.GetState();

        // Diff units: spawn shells for new IDs
        foreach (var (unitId, unitData) in state.Units)
        {
            if (_unitRegistry.ContainsKey(unitId)) continue;
            if (!unitData.IsAlive) continue;

            var shell = SpawnUnitShell(unitData);
            if (shell != null)
            {
                _unitRegistry[unitId] = shell;
            }
        }

        // Diff projectiles: spawn shells for new IDs
        foreach (var (projId, projData) in state.Projectiles)
        {
            if (_projectileRegistry.ContainsKey(projId)) continue;
            if (projData.IsDead) continue;

            var shell = SpawnProjectileShell(projData);
            _projectileRegistry[projId] = shell;
        }

        // Clean up freed unit nodes
        var toRemove = new List<int>();
        foreach (var (unitId, shell) in _unitRegistry)
        {
            if (!IsInstanceValid(shell))
            {
                toRemove.Add(unitId);
            }
        }
        foreach (var id in toRemove)
        {
            _unitRegistry.Remove(id);
        }

        // Clean up freed or dead projectile nodes
        toRemove.Clear();
        foreach (var (projId, shell) in _projectileRegistry)
        {
            if (!IsInstanceValid(shell))
            {
                toRemove.Add(projId);
            }
            else if (!state.Projectiles.ContainsKey(projId))
            {
                // Projectile removed from state — destroy shell
                shell.PlayImpactAndDestroy();
                toRemove.Add(projId);
            }
        }
        foreach (var id in toRemove)
        {
            _projectileRegistry.Remove(id);
        }
    }

    // --- Shell Factory ---

    private UnitVisual? SpawnUnitShell(UnitData unitData)
    {
        var def = UnitDefinitions.Get(unitData.CatalogId);
        if (def == null)
        {
            GD.PrintErr($"[EntityManager] No definition for CatalogId={unitData.CatalogId}");
            return null;
        }

        var packedScene = GD.Load<PackedScene>(def.ScenePath);
        if (packedScene == null)
        {
            GD.PrintErr($"[EntityManager] Failed to load scene: {def.ScenePath}");
            return null;
        }

        var shell = packedScene.Instantiate<UnitVisual>();
        shell.Name = $"UnitVisual_{unitData.UnitId}";
        AddChild(shell);
        shell.Initialize(_session!, unitData.UnitId);
        return shell;
    }

    private ProjectileVisual SpawnProjectileShell(SimProjectileData projData)
    {
        var shell = new ProjectileVisual();
        shell.Name = $"ProjectileVisual_{projData.ProjectileId}";
        AddChild(shell);
        shell.Initialize(_session!, projData.ProjectileId);
        return shell;
    }

    private void DestroyShell(int entityId)
    {
        if (_unitRegistry.TryGetValue(entityId, out var unitShell))
        {
            if (IsInstanceValid(unitShell))
                unitShell.QueueFree();
            _unitRegistry.Remove(entityId);
        }
        if (_projectileRegistry.TryGetValue(entityId, out var projShell))
        {
            if (IsInstanceValid(projShell))
                projShell.QueueFree();
            _projectileRegistry.Remove(entityId);
        }
    }

    // --- Event Dispatch ---

    private void OnSimEvents(IReadOnlyList<SimEvent> events)
    {
        foreach (var e in events)
        {
            e.Accept(this);
        }
    }

    // --- ISimEventVisitor (unit event dispatch to shells) ---

    public void Visit(UnitAttackedEvent e)
    {
        if (_unitRegistry.TryGetValue(e.AttackerUnitId, out var shell))
            shell.PlayAttackAnimation();
    }

    public void Visit(UnitDamagedEvent e)
    {
        if (_unitRegistry.TryGetValue(e.TargetUnitId, out var shell))
            shell.FlashDamage();
    }

    public void Visit(UnitDiedSimEvent e)
    {
        if (_unitRegistry.TryGetValue(e.UnitId, out var shell))
            shell.BeginDeath();
    }

    public void Visit(AttackEvadedEvent e)
    {
        if (_unitRegistry.TryGetValue(e.TargetUnitId, out var shell))
            shell.ShowEvadeText();
    }

    public void Visit(BuffAppliedSimEvent e)
    {
        if (_unitRegistry.TryGetValue(e.TargetUnitId, out var shell))
            shell.ShowBuffIcon(e.EffectType);
    }

    // --- Projectile/Summoner/Spell visitors ---

    public void Visit(ProjectileHitSimEvent e)
    {
        if (_projectileRegistry.TryGetValue(e.ProjectileId, out var shell))
            shell.PlayImpactAndDestroy();
    }

    public void Visit(SummonerDamagedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
        {
            shell.FlashDamage();
            shell.OnSummonerDamaged(e.Damage);
        }
    }

    public void Visit(SummonerDestroyedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
        {
            shell.BeginDeath();
            shell.OnSummonerDestroyed();
        }
    }

    public void Visit(SpellCastEvent e)
    {
        GD.Print($"[EntityManager] SpellCastEvent: team={e.Team}, catalogId={e.CatalogId}");
    }

    public void Visit(DelayedEffectFiredSimEvent e)
    {
        GD.Print($"[EntityManager] DelayedEffectFiredSimEvent: type={e.EffectType}, radius={e.AoeRadius}");
    }

    // --- Summoner event dispatch (forwarded to SummonerVisual for signal emission) ---

    public void Visit(SummonerHpChangedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnHpChanged(e.Hp, e.MaxHp);
    }

    public void Visit(SummonerManaChangedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnManaChanged(e.Mana, e.MaxMana);
    }

    public void Visit(CastingStartedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnCastingStarted(e.CardIndex, e.Duration, e.CatalogId);
    }

    public void Visit(CastingCompletedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnCastingCompleted(e.CardIndex);
    }

    public void Visit(CardDrawnEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnCardDrawn(e.HandIndex, e.CatalogId);
    }

    public void Visit(HandChangedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnHandChanged(e.Hand);
    }

    public void Visit(DeckRecycledEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnDeckRecycled();
    }

    // --- No-op visitors (HUD handles these, or no visual action needed) ---
    public void Visit(PhaseChangedEvent e) { }
    public void Visit(PrepTimerUpdatedEvent e) { }
    public void Visit(MatchTimeUpdatedEvent e) { }
    public void Visit(UnitRegisteredEvent e) { }
    public void Visit(UnitRemovedEvent e) { }
    public void Visit(GameOverEvent e) { }
    public void Visit(UnitActivationChangedEvent e) { }
    public void Visit(BuffExpiredSimEvent e) { }

    // --- Global Control ---

    public void Pause() { _isPaused = true; }
    public void Resume() { _isPaused = false; }
}
