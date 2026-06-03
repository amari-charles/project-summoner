using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Infrastructure.Debug;
using Fateforged.Infrastructure.Pooling;
using Fateforged.Projectiles;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;
using Fateforged.Units;
using Fateforged.Vfx;
using Fateforged.View.Spells;
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
    private const float ClientInterpolationSpeed = 14.0f;
    private const float ClientSnapThreshold = 3.0f;
    private const float HitscanBeamThickness = 0.22f;
    private const float HitscanBeamMinDurationSeconds = 0.03f;
    private static readonly Color HitscanBeamColor = new(1.0f, 0.35f, 0.25f, 0.95f);

    private IGameSession? _session;
    private readonly Dictionary<int, UnitVisual> _unitRegistry = new();
    private readonly Dictionary<int, ProjectileVisual> _projectileRegistry = new();
    private readonly Dictionary<int, MeshInstance3D> _projectileDebugMarkers = new();
    private readonly Dictionary<
        int,
        (float Radius, ProjectileHitSpace HitSpace)
    > _projectileDebugMarkerMeta = new();
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
        ClearProjectileDebugMarkers();
        _projectilePool?.Clear();
    }

    public void RegisterSummonerVisual(SummonerVisual shell, int teamIndex)
    {
        _summonerRegistry[teamIndex] = shell;
    }

    // --- Lifecycle (called each frame) ---

    public override void _PhysicsProcess(double delta)
    {
        if (_session == null || _isPaused)
            return;

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

            if (_unitRegistry.ContainsKey(unitId))
                continue;
            if (!unitData.IsAlive)
                continue;

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
            if (_projectileRegistry.ContainsKey(projId))
                continue;
            if (projData.IsDead)
                continue;

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

    public override void _Process(double delta)
    {
        bool debugEnabled = BattlefieldDebugService.Instance?.ProjectileHitGeometryEnabled == true;
        if (!debugEnabled)
        {
            ClearProjectileDebugMarkers();
            return;
        }

        if (_session == null || _isPaused)
        {
            ClearProjectileDebugMarkers();
            return;
        }

        var state = _session.GetState();
        foreach (var (projId, projData) in state.Projectiles)
        {
            if (projData.IsDead)
                continue;

            bool hasShell =
                _projectileRegistry.TryGetValue(projId, out var shell) && IsInstanceValid(shell);
            if (hasShell)
            {
                RemoveProjectileDebugMarker(projId);
                continue;
            }

            UpdateProjectileDebugMarker(projId, projData);
        }

        _cleanupBuffer.Clear();
        foreach (var projId in _projectileDebugMarkers.Keys)
        {
            if (!state.Projectiles.ContainsKey(projId))
                _cleanupBuffer.Add(projId);
        }

        foreach (var projId in _cleanupBuffer)
        {
            RemoveProjectileDebugMarker(projId);
        }
    }

    // --- Shell Factory ---

    private UnitVisual? SpawnUnitShell(UnitData unitData)
    {
        var def = UnitDefinitions.Get(unitData.CatalogId.Value);
        if (def == null)
        {
            GD.PrintErr($"[EntityManager] No definition for CatalogId={unitData.CatalogId.Value}");
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

    public void Visit(AbilityActivatedEvent e)
    {
        // No-op in V1: reserved for dedicated ability VFX wiring.
    }

    public void Visit(EffectCueEvent e)
    {
        // Buff/status icons are driven by BuffAppliedEvent and StatusAppliedEvent.
        // Removed cues with authored removal payloads get a pulse at the owner position.
        if (e.Phase != EffectCuePhase.Removed)
            return;

        var card = TryResolveCueCard(e.CueId);
        var removal = card?.SpellEffects.FirstOrDefault(effect => effect.RemovalEffect != null)
            ?.RemovalEffect;
        if (card == null || removal == null || removal.Radius <= 0f)
            return;

        var simNode = SimulationNode.Current;
        var position = SimToLocal(e.Position, simNode);
        var metadata = SpellVisualMetadata.FromCardDefinition(card);
        var customData = new Godot.Collections.Dictionary
        {
            ["card_id"] = (string)card.Id,
            ["element"] = metadata.Element,
            ["shape"] = SpellVisualMetadata.Circle,
            ["radius"] = removal.Radius,
            ["line_width"] = metadata.LineWidth,
            ["duration"] = 0.5f,
            ["mode"] = "pulse",
            ["source_position"] = position,
            ["target_position"] = position,
        };

        _vfxService.PlayEffect((string)VfxIds.SpellAreaPulse, position, customData);
    }

    public void Visit(StatusAppliedEvent e)
    {
        if (_unitRegistry.TryGetValue(e.TargetUnitId, out var shell))
            shell.ShowBuffIcon(EffectType.StatModifier);
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

    public void Visit(HitscanBeamFiredEvent e)
    {
        SpawnTransientHitscanBeam(e);
    }

    public void Visit(SummonerDamagedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
        {
            shell.FlashDamage();
            shell.OnSummonerDamaged(e.Damage, e.AttackerUnitId, e.HitPosition);
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
        var card = CardCatalog.GetCard(e.CatalogId.Value);
        if (card == null || string.IsNullOrEmpty(card.SpellVfx))
            return;

        var simNode = SimulationNode.Current;
        var localPos =
            simNode != null
                ? simNode.SimToLocal(e.Position)
                : new Vector3(e.Position.X, e.Position.Y, e.Position.Z);

        var customData = new Godot.Collections.Dictionary();
        var effectPosition = localPos;
        var metadata = SpellVisualMetadata.FromCardDefinition(card);
        customData["card_id"] = (string)card.Id;
        customData["element"] = metadata.Element;
        customData["shape"] = metadata.Shape;
        customData["line_width"] = metadata.LineWidth;
        if (metadata.Radius > 0f)
            customData["radius"] = metadata.Radius;
        if (card.SpellDuration > 0f)
            customData["duration"] = card.SpellDuration;
        customData["mode"] = card.SpellVfx == VfxIds.SpellAreaBurst ? "burst" : "field";

        if (_session != null)
        {
            var state = _session.GetState();
            if (e.Team >= 0 && e.Team < state.Summoners.Length)
            {
                var sourceLocalPos = ResolveSummonerLocalPosition(e.Team, state, simNode);
                var targetLocalPos = localPos;

                // Prefer explicit sim-resolved target (single-target spell cast).
                if (e.TargetUnitId.HasValue)
                {
                    var targetedUnit = state.GetAliveUnit(e.TargetUnitId.Value);
                    if (targetedUnit != null)
                        targetLocalPos = SimToLocal(targetedUnit.Position, simNode);
                }

                customData["source_position"] = sourceLocalPos;
                customData["target_position"] = targetLocalPos;
                if (
                    card.Id == CardIds.WaterJet
                    || metadata.Shape == SpellVisualMetadata.Line
                    || card.SpellVfx == VfxIds.SpellLine
                )
                {
                    effectPosition = sourceLocalPos;
                }
                else if (
                    metadata.Shape == SpellVisualMetadata.SingleTarget
                    || card.SpellVfx == VfxIds.SpellSingleTarget
                )
                {
                    effectPosition = targetLocalPos;
                }
            }
        }

        foreach (var effect in card.SpellEffects)
        {
            if (effect.DelaySeconds <= 0f && effect.RepeatCount <= 0)
                continue;
            customData["pulse_delay"] = effect.DelaySeconds;
            customData["pulse_repeat_count"] = effect.RepeatCount;
            customData["pulse_interval"] = effect.RepeatIntervalSeconds;
            break;
        }

        _vfxService.PlayEffect((string)card.SpellVfx, effectPosition, customData);
    }

    public void Visit(DelayedEffectFiredEvent e)
    {
        if (string.IsNullOrEmpty(e.CardCatalogId.Value) || e.AoeRadius <= 0f)
            return;

        var card = CardCatalog.GetCard(e.CardCatalogId.Value);
        if (card == null)
            return;

        var simNode = SimulationNode.Current;
        var position = SimToLocal(e.Position, simNode);
        var source = SimToLocal(e.SourcePosition, simNode);
        var metadata = SpellVisualMetadata.FromCardDefinition(card);
        var customData = new Godot.Collections.Dictionary
        {
            ["card_id"] = (string)card.Id,
            ["element"] = metadata.Element,
            ["shape"] = e.AreaShape.ToString().ToLowerInvariant(),
            ["radius"] = e.AoeRadius,
            ["line_width"] = metadata.LineWidth,
            ["duration"] = 0.45f,
            ["mode"] = "pulse",
            ["source_position"] = source,
            ["target_position"] = position,
        };

        _vfxService.PlayEffect((string)VfxIds.SpellAreaPulse, position, customData);
    }

    private Vector3 ResolveSummonerLocalPosition(int team, MatchState state, SimulationNode? simNode)
    {
        if (
            _summonerRegistry.TryGetValue(team, out var summonerShell)
            && IsInstanceValid(summonerShell)
        )
        {
            return summonerShell.GlobalPosition;
        }

        var summonerSimPos = state.Summoners[team].Position;
        return SimToLocal(summonerSimPos, simNode);
    }

    private static Vector3 SimToLocal(SimVector3 position, SimulationNode? simNode)
    {
        return simNode != null
            ? simNode.SimToLocal(position)
            : new Vector3(position.X, position.Y, position.Z);
    }

    private static CardDefinition? TryResolveCueCard(string cueId)
    {
        if (string.IsNullOrWhiteSpace(cueId))
            return null;

        int separator = cueId.IndexOf(':');
        if (separator <= 0)
            return null;

        return CardCatalog.GetCard(cueId[..separator]);
    }

    private void SpawnTransientHitscanBeam(HitscanBeamFiredEvent e)
    {
        var simNode = SimulationNode.Current;
        var start =
            simNode != null
                ? simNode.SimToLocal(e.StartPosition)
                : new Vector3(e.StartPosition.X, e.StartPosition.Y, e.StartPosition.Z);
        var end =
            simNode != null
                ? simNode.SimToLocal(e.EndPosition)
                : new Vector3(e.EndPosition.X, e.EndPosition.Y, e.EndPosition.Z);
        var segment = end - start;
        float length = segment.Length();
        if (length <= 0.01f)
            return;

        var beamMesh = new BoxMesh { Size = new Vector3(HitscanBeamThickness, HitscanBeamThickness, length) };
        beamMesh.Material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = HitscanBeamColor,
            EmissionEnabled = true,
            Emission = HitscanBeamColor,
            EmissionEnergyMultiplier = 1.2f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };

        var beamNode = new MeshInstance3D
        {
            Name = $"HitscanBeam_{e.ProjectileId}",
            Mesh = beamMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(beamNode);
        beamNode.GlobalPosition = start.Lerp(end, 0.5f);

        Vector3 upHint =
            Mathf.Abs(segment.Normalized().Dot(Vector3.Up)) > 0.98f ? Vector3.Forward : Vector3.Up;
        beamNode.LookAt(end, upHint, true);

        var timer = new Timer
        {
            OneShot = true,
            Autostart = true,
            WaitTime = Mathf.Max(HitscanBeamMinDurationSeconds, e.DurationSeconds),
        };
        beamNode.AddChild(timer);
        timer.Timeout += () =>
        {
            if (IsInstanceValid(beamNode))
                beamNode.QueueFree();
        };
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
            shell.OnCastingStarted(e.CardIndex, e.Duration, e.CatalogId.Value);
    }

    public void Visit(CastingCompletedEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnCastingCompleted(e.CardIndex);
    }

    public void Visit(CardDrawnEvent e)
    {
        if (_summonerRegistry.TryGetValue(e.Team, out var shell))
            shell.OnCardDrawn(e.HandIndex, e.CatalogId.Value);
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

    public void Pause()
    {
        _isPaused = true;
    }

    public void Resume()
    {
        _isPaused = false;
    }

    private void UpdateProjectileDebugMarker(int projectileId, SimProjectileData projData)
    {
        float radius = Mathf.Max(0.05f, projData.HitRadius);
        bool needsRebuild =
            !_projectileDebugMarkers.TryGetValue(projectileId, out var marker)
            || !_projectileDebugMarkerMeta.TryGetValue(projectileId, out var markerMeta)
            || !Mathf.IsEqualApprox(markerMeta.Radius, radius)
            || markerMeta.HitSpace != projData.HitSpace;

        if (needsRebuild)
        {
            if (marker != null)
                marker.QueueFree();

            marker = CreateProjectileDebugMarker(
                radius,
                projData.HitSpace,
                new Color(0.1f, 0.95f, 1.0f, 0.62f),
                100
            );
            AddChild(marker);
            _projectileDebugMarkers[projectileId] = marker;
            _projectileDebugMarkerMeta[projectileId] = (radius, projData.HitSpace);
        }

        if (marker == null)
            return;

        var simNode = SimulationNode.Current;
        var projectilePos =
            simNode != null
                ? simNode.SimToLocal(projData.CurrentPosition)
                : new Vector3(
                    projData.CurrentPosition.X,
                    projData.CurrentPosition.Y,
                    projData.CurrentPosition.Z
                );

        if (projData.HitSpace == ProjectileHitSpace.GroundCylinder)
        {
            marker.GlobalPosition = new Vector3(projectilePos.X, 0.06f, projectilePos.Z);
            marker.Rotation = Vector3.Zero;
            return;
        }

        marker.GlobalPosition = projectilePos;
        marker.Rotation = Vector3.Zero;
    }

    private static MeshInstance3D CreateProjectileDebugMarker(
        float radius,
        ProjectileHitSpace hitSpace,
        Color color,
        int renderPriority
    )
    {
        var marker = new MeshInstance3D
        {
            Mesh =
                hitSpace == ProjectileHitSpace.GroundCylinder
                    ? new CylinderMesh
                    {
                        TopRadius = radius,
                        BottomRadius = radius,
                        Height = 0.12f,
                    }
                    : new SphereMesh { Radius = radius, Height = radius * 2f },
            MaterialOverride = CreateProjectileDebugMaterial(color, renderPriority),
        };
        return marker;
    }

    private static StandardMaterial3D CreateProjectileDebugMaterial(Color color, int renderPriority)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 1.5f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            NoDepthTest = true,
            RenderPriority = renderPriority,
        };
    }

    private void RemoveProjectileDebugMarker(int projectileId)
    {
        if (_projectileDebugMarkers.TryGetValue(projectileId, out var marker))
        {
            marker.QueueFree();
            _projectileDebugMarkers.Remove(projectileId);
        }

        _projectileDebugMarkerMeta.Remove(projectileId);
    }

    private void ClearProjectileDebugMarkers()
    {
        foreach (var marker in _projectileDebugMarkers.Values)
            marker.QueueFree();
        _projectileDebugMarkers.Clear();
        _projectileDebugMarkerMeta.Clear();
    }

    public Godot.Collections.Dictionary GetProjectileDebugOverlayStatus()
    {
        var state = _session?.GetState();
        int projectileCount = state?.Projectiles.Count ?? 0;
        bool debugEnabled = BattlefieldDebugService.Instance?.ProjectileHitGeometryEnabled == true;
        return new Godot.Collections.Dictionary
        {
            ["debug_enabled"] = debugEnabled,
            ["session_ready"] = _session != null,
            ["projectiles_in_state"] = projectileCount,
            ["projectile_shells"] = _projectileRegistry.Count,
            ["radius_markers"] = _projectileDebugMarkers.Count,
        };
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetProjectileVisualDiagnostics()
    {
        var rows = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        var state = _session?.GetState();
        if (state == null)
            return rows;

        foreach (var (projectileId, projectileData) in state.Projectiles)
        {
            bool hasShell =
                _projectileRegistry.TryGetValue(projectileId, out var shell)
                && IsInstanceValid(shell);
            bool shellVisible = hasShell && shell!.Visible;
            int shellChildCount = hasShell ? shell!.GetChildCount() : 0;
            string modelName = "";
            bool modelVisible = false;

            if (hasShell && shell != null)
            {
                foreach (var child in shell.GetChildren())
                {
                    if (child is not Node3D node3D)
                        continue;

                    modelName = node3D.Name;
                    modelVisible = node3D.Visible;
                    break;
                }
            }

            rows.Add(
                new Godot.Collections.Dictionary
                {
                    ["projectile_id"] = projectileId,
                    ["catalog_id"] = projectileData.ProjectileCatalogId.Value,
                    ["time_alive"] = projectileData.TimeAlive,
                    ["lifetime"] = projectileData.Lifetime,
                    ["hit_radius"] = projectileData.HitRadius,
                    ["hit_space"] = (int)projectileData.HitSpace,
                    ["has_shell"] = hasShell,
                    ["shell_visible"] = shellVisible,
                    ["shell_children"] = shellChildCount,
                    ["model_name"] = modelName,
                    ["model_visible"] = modelVisible,
                    ["has_debug_marker"] = _projectileDebugMarkers.ContainsKey(projectileId),
                }
            );
        }

        return rows;
    }
}
