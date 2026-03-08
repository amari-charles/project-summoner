using System.Collections.Generic;
using Fateforged.Infrastructure.Pooling;
using Fateforged.Session;
using Fateforged.Simulation;
using Godot;
using Fateforged.Units;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;
using Fateforged.Cards;

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
    private const float ClientInterpolationSpeed = 14.0f;
    private const float ClientSnapThreshold = 3.0f;

    private IGameSession? _session;
    private readonly Dictionary<int, UnitVisual> _unitRegistry = new();
    private readonly Dictionary<int, ProjectileVisual> _projectileRegistry = new();
    private readonly Dictionary<int, SummonerVisual> _summonerRegistry = new();
    private readonly StateInterpolator _unitInterpolator = new();
    private IBattleVfxService _vfxService = NullBattleVfxService.Instance;
    private NodePool<ProjectileVisual>? _projectilePool;

    private bool _isPaused;
    private readonly List<int> _cleanupBuffer = new();

    // --- Initialization ---

    public void Initialize(IGameSession session)
    {
        Initialize(session, NullBattleVfxService.Instance);
    }

    public void Initialize(IGameSession session, IBattleVfxService? vfxService)
    {
        _session = session;
        _session.SimEventsEmitted += OnSimEvents;
        _unitInterpolator.InterpolationSpeed = ClientInterpolationSpeed;
        _unitInterpolator.SnapThreshold = ClientSnapThreshold;
        _vfxService = vfxService ?? NullBattleVfxService.Instance;
        _projectilePool = new NodePool<ProjectileVisual>(this);
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

    public override void _ExitTree()
    {
        if (_session != null)
            _session.SimEventsEmitted -= OnSimEvents;
        _unitInterpolator.Clear();
        _projectilePool?.Clear();
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
        var simNode = SimulationNode.Current;
        bool useClientInterpolation = simNode != null && !simNode.IsHost;

        // Diff units: spawn shells for new IDs
        foreach (var (unitId, unitData) in state.Units)
        {
            if (useClientInterpolation)
            {
                _unitInterpolator.SetTarget(unitId, simNode!.SimToLocal(unitData.Position));
            }

            if (_unitRegistry.ContainsKey(unitId)) continue;
            if (!unitData.IsAlive) continue;

            var shell = SpawnUnitShell(unitData);
            if (shell != null)
            {
                _unitRegistry[unitId] = shell;
            }
        }

        if (useClientInterpolation)
            _unitInterpolator.Update(delta);

        // Diff projectiles: spawn shells for new IDs
        foreach (var (projId, projData) in state.Projectiles)
        {
            if (_projectileRegistry.ContainsKey(projId)) continue;
            if (projData.IsDead) continue;

            var shell = SpawnProjectileShell(projData);
            _projectileRegistry[projId] = shell;
        }

        // Clean up freed unit nodes
        _cleanupBuffer.Clear();
        foreach (var (unitId, shell) in _unitRegistry)
        {
            if (!IsInstanceValid(shell))
            {
                _unitInterpolator.Remove(unitId);
                _cleanupBuffer.Add(unitId);
            }
            else if (!state.Units.ContainsKey(unitId))
            {
                _unitInterpolator.Remove(unitId);
                if (shell.IsAlive)
                    shell.BeginDeath();
                _cleanupBuffer.Add(unitId);
            }
        }
        foreach (var id in _cleanupBuffer)
        {
            _unitRegistry.Remove(id);
        }

        // Clean up freed or dead projectile nodes
        _cleanupBuffer.Clear();
        foreach (var (projId, shell) in _projectileRegistry)
        {
            if (!IsInstanceValid(shell))
            {
                _cleanupBuffer.Add(projId);
            }
            else if (!state.Projectiles.ContainsKey(projId))
            {
                // Projectile removed from state — deactivate and return to pool
                ReleaseProjectile(shell);
                _cleanupBuffer.Add(projId);
            }
        }
        foreach (var id in _cleanupBuffer)
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
        var shell = _projectilePool!.Acquire();
        shell.Name = $"ProjectileVisual_{projData.ProjectileId}";
        AddChild(shell);
        shell.Initialize(_session!, projData.ProjectileId);
        return shell;
    }

    private void ReleaseProjectile(ProjectileVisual shell)
    {
        shell.Deactivate();
        _projectilePool?.Release(shell);
    }

    // --- Event Dispatch ---

    public Vector3 ResolveUnitRenderPosition(int unitId, Vector3 authoritativePosition)
    {
        var simNode = SimulationNode.Current;
        if (simNode == null || simNode.IsHost)
            return authoritativePosition;

        return _unitInterpolator.GetPosition(unitId) ?? authoritativePosition;
    }

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

    public void Visit(UnitDiedEvent e)
    {
        if (_unitRegistry.TryGetValue(e.UnitId, out var shell))
            shell.BeginDeath();
    }

    public void Visit(AttackEvadedEvent e)
    {
        if (_unitRegistry.TryGetValue(e.TargetUnitId, out var shell))
            shell.ShowEvadeText();
    }

    public void Visit(BuffAppliedEvent e)
    {
        if (_unitRegistry.TryGetValue(e.TargetUnitId, out var shell))
            shell.ShowBuffIcon(e.EffectType);
    }

    // --- Projectile/Summoner/Spell visitors ---

    public void Visit(ProjectileHitEvent e)
    {
        if (_projectileRegistry.TryGetValue(e.ProjectileId, out var shell))
        {
            ReleaseProjectile(shell);
            _projectileRegistry.Remove(e.ProjectileId);
        }
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
        }
    }

    public void Visit(SpellCastEvent e)
    {
        var card = CardCatalog.GetCard(e.CatalogId);
        if (card == null || string.IsNullOrEmpty(card.SpellVfx))
            return;

        var simNode = SimulationNode.Current;
        var localPos = simNode != null
            ? simNode.SimToLocal(e.Position)
            : new Vector3(e.Position.X, e.Position.Y, e.Position.Z);

        var customData = new Godot.Collections.Dictionary();
        if (card.SpellRadius > 0f)
            customData["radius"] = card.SpellRadius;

        _vfxService.PlayEffect((string)card.SpellVfx, localPos, customData);
    }

    public void Visit(DelayedEffectFiredEvent e)
    {
        GD.Print($"[EntityManager] DelayedEffectFiredEvent: type={e.EffectType}, radius={e.AoeRadius}");
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
    public void Visit(BuffExpiredEvent e) { }

    // --- Global Control ---

    public void Pause() { _isPaused = true; }
    public void Resume() { _isPaused = false; }
}
