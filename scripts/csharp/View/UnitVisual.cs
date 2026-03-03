using Fateforged.Session;
using Fateforged.Simulation;
using Godot;
using Fateforged.Meta;
using Fateforged.UI;
using Fateforged.Visual;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.View;

/// <summary>
/// Self-syncing visual shell for one unit.
/// Reads its own UnitData from IGameSession.GetState() each frame.
/// Exposes reaction methods called by EntityManager on discrete events.
///
/// Replaces Unit3D — game logic moves to Simulation subsystems.
/// </summary>
public partial class UnitVisual : Node3D, IDamageableVisual
{
    private IGameSession? _session;
    private int _unitId;
    private bool _isAlive = true;
    private bool _loggedMissing;

    private IVisualComponent? _visual;
    private ShadowComponent? _shadow;
    private SpawnRevealComponent? _spawnReveal;
    private float _attackAnimTimer;
    private bool _isFacingRight;

    // --- IDamageableVisual ---

    public bool IsAlive => _isAlive;

    // --- Initialization (called by EntityManager at spawn) ---

    public void Initialize(IGameSession session, int unitId)
    {
        _session = session;
        _unitId = unitId;

        // Find IVisualComponent child (may be named "Visual" or implement the interface)
        _visual = GetNodeOrNull<Node3D>("Visual") as IVisualComponent;
        if (_visual == null)
        {
            // Search children for any IVisualComponent
            foreach (var child in GetChildren())
            {
                if (child is IVisualComponent vc)
                {
                    _visual = vc;
                    break;
                }
            }
        }

        // Find optional shadow
        foreach (var child in GetChildren())
        {
            if (child is ShadowComponent sc)
            {
                _shadow = sc;
                break;
            }
        }

        // Set initial position from state so shell is never at origin
        var state = session.GetState();
        if (state.Units.TryGetValue(unitId, out var unitData))
        {
            GlobalPosition = SimulationNode.Current.SimToLocal(unitData.Position);

            // Start spawn reveal if unit has a spawn timer
            if (unitData.SpawnTimer > 0)
            {
                _spawnReveal = new SpawnRevealComponent(
                    this,
                    () => _visual,
                    () => _shadow as Node3D,
                    () => unitData.Team
                );
                Visible = true; // Must be visible for reveal shader to render
                _spawnReveal.StartReveal(unitData.SpawnTimer);
            }
            else
            {
                Visible = true;
            }

            // Set initial facing
            _isFacingRight = unitData.IsFacingRight;
            if (!SimulationNode.Current.IsHost)
                _isFacingRight = !_isFacingRight;
            _visual?.SetFlipH(_isFacingRight);
        }
        else
        {
            Visible = false;
        }

        // Create floating HP bar as a child
        var hpBar = new FloatingHPBar();
        AddChild(hpBar);
        hpBar.Configure(HPBarSettings.Default);
        hpBar.TrackNode(this);
    }

    // --- Self-Sync (continuous, every frame) ---

    public override void _PhysicsProcess(double delta)
    {
        if (_session == null || !_isAlive) return;

        var state = _session.GetState();
        if (!state.Units.TryGetValue(_unitId, out var unitData))
        {
            if (!_loggedMissing)
            {
                GD.PrintErr($"[UnitVisual] UnitData not found for unitId={_unitId}");
                _loggedMissing = true;
            }
            return;
        }

        // Death fallback from snapshot
        if (!unitData.IsAlive && _isAlive)
        {
            BeginDeath();
            return;
        }

        // Reveal on first active frame
        if (!Visible) Visible = true;

        // Sync position
        GlobalPosition = SimulationNode.Current.SimToLocal(unitData.Position);

        // Sync facing (flip for client since X axis is mirrored)
        bool localFacing = unitData.IsFacingRight;
        if (!SimulationNode.Current.IsHost)
            localFacing = !localFacing;

        if (_isFacingRight != localFacing)
        {
            _isFacingRight = localFacing;
            _visual?.SetFlipH(_isFacingRight);
        }

        // Animation from BehaviorState (attack anim timer has priority)
        if (_attackAnimTimer > 0)
        {
            _attackAnimTimer -= (float)delta;
        }
        else if (_visual != null)
        {
            string anim = unitData.BehaviorState switch
            {
                BehaviorState.Attacking => "idle",
                BehaviorState.InRange => "idle",
                _ => "walk"
            };
            _visual.PlayAnimation(anim);
        }

        // Update render priority for correct sprite layering
        float rawPriority = (-GlobalPosition.Z + GlobalPosition.Y) * 3.0f;
        int priority = (int)Mathf.Clamp(rawPriority, -128, 127);
        _visual?.SetRenderPriority(priority);
    }

    // --- Event Reactions (called by EntityManager) ---

    public void PlayAttackAnimation()
    {
        if (_visual == null) return;
        _visual.PlayAnimation("attack");
        _attackAnimTimer = _visual.GetAnimationDuration("attack");
    }

    public void FlashDamage()
    {
        _visual?.FlashWhite();
    }

    public void BeginDeath()
    {
        if (!_isAlive) return;
        _isAlive = false;

        _spawnReveal?.Cancel();

        if (_visual != null)
        {
            _visual.PlayAnimation("death");
        }

        // Queue free after death animation completes
        GetTree().CreateTimer(1.0).Timeout += QueueFree;
    }

    public void ShowBuffIcon(EffectType effectType)
    {
        // Stub — full implementation in Milestone 2
        GD.Print($"[UnitVisual] ShowBuffIcon: {effectType} on unit {_unitId}");
    }

    public void ShowEvadeText()
    {
        // Stub — full implementation in Milestone 2
        GD.Print($"[UnitVisual] ShowEvadeText on unit {_unitId}");
    }
}
