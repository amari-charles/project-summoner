using Fateforged.Session;
using Fateforged.Simulation;
using Godot;
using ProjectSummoner.Services;
using ProjectSummoner.UI;


namespace Fateforged.View;

/// <summary>
/// Registered visual shell for one summoner.
/// Same self-sync model as UnitVisual — reads its own SummonerData from
/// IGameSession.GetState() each frame — but registered at battle init rather
/// than dynamically spawned, since summoners are always present.
///
/// Replaces visual code in summoner.gd — deck/mana/casting logic moves to Simulation.
/// </summary>
public partial class SummonerVisual : Node3D, IDamageableVisual
{
    private IGameSession? _session;
    private int _teamIndex;
    private bool _isAlive = true;

    private Sprite3D? _sprite;
    private FloatingHPBar? _hpBar;

    // --- IDamageableVisual ---

    public bool IsAlive => _isAlive;

    // --- Initialization (called by EntityManager at battle init, NOT spawned dynamically) ---

    public void Initialize(IGameSession session, int teamIndex)
    {
        _session = session;
        _teamIndex = teamIndex;

        _sprite = GetNodeOrNull<Sprite3D>("Visual");

        // Create HP bar as a child (always visible, summoner-sized)
        var settings = HPBarSettings.AlwaysVisible with { BarWidth = 1.5f, OffsetY = 2.5f };
        _hpBar = new FloatingHPBar();
        AddChild(_hpBar);
        _hpBar.Configure(settings);
        _hpBar.TrackNode(this);
    }

    // --- Self-Sync (continuous, every frame) ---

    public override void _PhysicsProcess(double delta)
    {
        if (_session == null || !_isAlive) return;

        var state = _session.GetState();
        var summoner = state.Summoners[_teamIndex];

        // Update HP bar
        _hpBar?.UpdateHp(summoner.CurrentHp, summoner.MaxHp);

        // Death detection from state
        if (!summoner.IsAlive && _isAlive)
        {
            BeginDeath();
        }
    }

    // --- Event Reactions (called by EntityManager) ---

    public void FlashDamage()
    {
        if (_sprite == null) return;

        var originalModulate = _sprite.Modulate;
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", Colors.White, 0.05);
        tween.TweenProperty(_sprite, "modulate", originalModulate, 0.15);
    }

    public void BeginDeath()
    {
        if (!_isAlive) return;
        _isAlive = false;

        if (_sprite != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0f, 0.5);
        }
    }
}
