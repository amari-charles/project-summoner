using System;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Godot;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Meta;
using Fateforged.UI;

namespace Fateforged.View;

/// <summary>
/// How a summoner's deck is loaded at battle start.
/// </summary>
public enum DeckLoadStrategy
{
    /// <summary>Use the StartingDeck export array as-is.</summary>
    Static = 0,
    /// <summary>Load from BattleContext (enemy decks configured per-battle).</summary>
    BattleContext = 1,
    /// <summary>Load from player profile (selected deck + card instances).</summary>
    Profile = 2,
    /// <summary>Deck will be set manually later (e.g., event sequences).</summary>
    Deferred = 3
}

/// <summary>
/// Registered visual shell for one summoner.
/// Same self-sync model as UnitVisual — reads its own SummonerData from
/// IGameSession.GetState() each frame — but registered at battle init rather
/// than dynamically spawned, since summoners are always present.
///
/// Emits Godot signals for GDScript UI consumers (HandUI, GameUI, etc.).
/// Deck/mana/casting logic lives in Simulation.
/// </summary>
[GlobalClass]
public partial class SummonerVisual : Node3D, IDamageableVisual
{
    // =========================================================================
    // EXPORTS
    // =========================================================================

    [Export] public int Team { get; set; } = 0;
    [Export] public float MaxHpExport { get; set; } = 300.0f;
    [Export] public int MaxHandSize { get; set; } = 4;
    [Export] public DeckLoadStrategy DeckLoadStrategy { get; set; } = DeckLoadStrategy.BattleContext;
    [Export] public Godot.Collections.Array<Resource> StartingDeck { get; set; } = new();

    // =========================================================================
    // SIGNALS (PascalCase for GDScript consumers)
    // =========================================================================

    [Signal] public delegate void CardPlayedEventHandler(GodotObject card);
    [Signal] public delegate void CardDrawnEventHandler(GodotObject card);
    [Signal] public delegate void HandChangedEventHandler(Godot.Collections.Array hand);
    [Signal] public delegate void ManaChangedEventHandler(float current, float max);
    [Signal] public delegate void HpChangedEventHandler(float newHp, float maxHp);
    [Signal] public delegate void CastingStartedEventHandler(GodotObject card, float duration);
    [Signal] public delegate void CastingProgressEventHandler(float remaining, float total);
    [Signal] public delegate void CastingCompletedEventHandler(GodotObject card);
    [Signal] public delegate void SummonerReadyEventHandler(Node3D summoner);
    [Signal] public delegate void SummonerDestroyedEventHandler(Node3D summoner);
    [Signal] public delegate void SummonerDamagedEventHandler(Node3D summoner, float damage);
    [Signal] public delegate void DeckRecycledEventHandler(int cardCount);

    // =========================================================================
    // STATE
    // =========================================================================

    private IGameSession? _session;
    private int _teamIndex;
    private bool _isAlive = true;

    private Sprite3D? _sprite;
    private FloatingHPBar? _hpBar;
    private Color _originalColor = Colors.White;
    private Vector3 _originalVisualPosition;
    private Tween? _activeFeedbackTween;

    // Hit feedback animation constants
    private const float DefaultFlashDuration = 0.3f;
    private const float MinFlashDuration = 0.05f;
    private const float FlashSpeedMultiplier = 0.3f;
    private const float RecentHitsDecayRate = 2.0f;
    private const float FlashToWhiteRatio = 0.4f;
    private const float FlashReturnRatio = 0.6f;
    private const float ShakeOutRatio = 0.35f;
    private const float ShakeReturnRatio = 0.25f;
    private float _recentHits;

    // Tween duration constants
    private const float DamageFlashToWhiteDuration = 0.05f;
    private const float DamageFlashReturnDuration = 0.15f;
    private const float DeathFadeDuration = 0.5f;

    // Collision shape constants
    private const float HurtboxRadius = 2.0f;
    private const float HurtboxHeight = 6.25f;

    // Hand cache — rebuilt only on HandChangedEvent
    private Godot.Collections.Array<Resource> _handCache = new();
    private Card? _castingCard;

    // MP client polling: last-known values for delta detection
    private bool _lastIsCasting;
    private float _lastMana;
    private float _lastMaxMana;
    private float _lastHp;
    private float _lastMaxHp;
    private string[] _lastHandIds = Array.Empty<string>();

    // =========================================================================
    // IDamageableVisual
    // =========================================================================

    public bool IsAlive => _isAlive;

    // =========================================================================
    // READ-THROUGH PROPERTIES (computed from MatchState on demand)
    // =========================================================================

    public float Mana => _session?.GetState()?.Summoners[_teamIndex].Mana ?? 0f;
    public float MaxMana => _session?.GetState()?.Summoners[_teamIndex].MaxMana ?? 0f;
    public float CurrentHp => _session?.GetState()?.Summoners[_teamIndex].CurrentHp ?? 0f;
    public float MaxHp => _session?.GetState()?.Summoners[_teamIndex].MaxHp ?? 0f;
    public bool IsCasting => _session?.GetState()?.Summoners[_teamIndex].IsCasting ?? false;
    public float CastingTimeRemaining => _session?.GetState()?.Summoners[_teamIndex].CastingTimeRemaining ?? 0f;
    public float CastingTimeTotal => _session?.GetState()?.Summoners[_teamIndex].CastingTimeTotal ?? 0f;
    public bool IsEnabled { get; set; } = true;
    public Godot.Collections.Array<Resource> Hand => _handCache;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        // Add to groups for discovery
        AddToGroup(GroupIDs.Summoners);
        AddToGroup(GroupIDs.Bases);
        if (Team == 0)
        {
            AddToGroup(GroupIDs.PlayerSummoners);
            AddToGroup(GroupIDs.PlayerBases);
        }
        else
        {
            AddToGroup(GroupIDs.EnemySummoners);
            AddToGroup(GroupIDs.EnemyBases);
        }

        // Initialize visual reference for hit feedback
        _sprite = GetNodeOrNull<Sprite3D>("Visual");
        if (_sprite != null)
        {
            _originalColor = _sprite.Modulate;
            _originalVisualPosition = _sprite.Position;
        }

        // Configure collision shape
        ConfigureCollisionShape();
    }

    public override void _ExitTree()
    {
        if (_activeFeedbackTween != null && _activeFeedbackTween.IsValid())
            _activeFeedbackTween.Kill();
    }

    // =========================================================================
    // INITIALIZATION (called by BattleScene after sim registration)
    // =========================================================================

    public void Initialize(IGameSession session, int teamIndex)
    {
        _session = session;
        _teamIndex = teamIndex;

        // Ensure sprite reference is set (may be called before _Ready in some paths)
        _sprite ??= GetNodeOrNull<Sprite3D>("Visual");

        // Create HP bar as a child (always visible, summoner-sized)
        if (_hpBar == null)
        {
            var settings = HPBarSettings.AlwaysVisible with { BarWidth = 1.5f, OffsetY = 2.5f };
            _hpBar = new FloatingHPBar();
            AddChild(_hpBar);
            _hpBar.Configure(settings);
            _hpBar.TrackNode(this);
        }

        // Initialize last-known values for MP client polling
        var summoner = session.GetState().Summoners[teamIndex];
        _lastMana = summoner.Mana;
        _lastMaxMana = summoner.MaxMana;
        _lastHp = summoner.CurrentHp;
        _lastMaxHp = summoner.MaxHp;
        _lastIsCasting = summoner.IsCasting;

        // Emit initial hand so the UI shows starting cards without waiting for a HandChangedEvent.
        // SetSummonerHand() writes directly to MatchState with no event — host mode never polls,
        // so without this the hand would be invisible until the first card draw.
        if (summoner.Hand.Count > 0)
        {
            _lastHandIds = summoner.Hand.ToArray();
            RebuildHandCache(_lastHandIds);
            EmitSignal(SignalName.HandChanged, _handCache);
        }

        EmitSignal(SignalName.SummonerReady, this);
    }

    // =========================================================================
    // SELF-SYNC (continuous, every frame)
    // =========================================================================

    public override void _PhysicsProcess(double delta)
    {
        if (_session == null || !_isAlive) return;

        var state = _session.GetState();
        var summoner = state.Summoners[_teamIndex];

        // Update HP bar
        _hpBar?.UpdateHp(summoner.CurrentHp, summoner.MaxHp);

        // Decay recent hits counter (for hit feedback animation speed)
        if (_recentHits > 0)
        {
            _recentHits -= RecentHitsDecayRate * (float)delta;
            if (_recentHits < 0) _recentHits = 0;
        }

        // Death detection from state
        if (!summoner.IsAlive && _isAlive)
        {
            BeginDeath();
        }
    }

    public override void _Process(double delta)
    {
        if (_session == null || !_isAlive) return;

        // MP client: poll MatchState for casting/hand/mana/HP changes
        var simNode = SimulationNode.Current;
        if (simNode != null && !simNode.IsHost)
        {
            PollMatchState();
        }
        else if (IsCasting)
        {
            // Host/single-player: emit casting progress
            EmitSignal(SignalName.CastingProgress, CastingTimeRemaining, CastingTimeTotal);
        }
    }

    // =========================================================================
    // EVENT HANDLERS (called by EntityManager on SimEvents)
    // =========================================================================

    public void OnCastingStarted(int cardIndex, float duration, string catalogId)
    {
        var card = CreateCardResource(catalogId);
        if (card == null)
        {
            GD.PrintErr($"[SummonerVisual] Failed to create card resource for catalogId={catalogId}");
            return;
        }
        _castingCard = card;
        EmitSignal(SignalName.CardPlayed, card);
        EmitSignal(SignalName.CastingStarted, card, duration);
    }

    public void OnCastingCompleted(int cardIndex)
    {
        var completed = _castingCard;
        _castingCard = null;
        if (completed == null)
        {
            GD.PrintErr("[SummonerVisual] CastingCompleted but no casting card was set");
            return;
        }
        EmitSignal(SignalName.CastingCompleted, completed);
    }

    public void OnHandChanged(string[] catalogIds)
    {
        RebuildHandCache(catalogIds);
        EmitSignal(SignalName.HandChanged, _handCache);
    }

    public void OnCardDrawn(int handIndex, string catalogId)
    {
        var card = CreateCardResource(catalogId);
        if (card != null)
            EmitSignal(SignalName.CardDrawn, card);
    }

    public void OnManaChanged(float mana, float maxMana)
    {
        EmitSignal(SignalName.ManaChanged, mana, maxMana);
    }

    public void OnHpChanged(float hp, float maxHp)
    {
        ApplyHpUpdate(hp, maxHp);
    }

    public void OnSummonerDamaged(float damage)
    {
        _recentHits += 1.0f;
        PlayHitFeedback();
        EmitSignal(SignalName.SummonerDamaged, this, damage);
    }

    public void OnSummonerDestroyed()
    {
        EmitSignal(SignalName.SummonerDestroyed, this);
    }

    public void OnDeckRecycled()
    {
        // Card count not available from DeckRecycledEvent (only carries Team) — pass 0 as stub
        EmitSignal(SignalName.DeckRecycled, 0);
    }

    // =========================================================================
    // VISUAL FEEDBACK
    // =========================================================================

    public void FlashDamage()
    {
        if (_sprite == null) return;

        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", Colors.White, DamageFlashToWhiteDuration);
        tween.TweenProperty(_sprite, "modulate", _originalColor, DamageFlashReturnDuration);
    }

    public void BeginDeath()
    {
        if (!_isAlive) return;
        _isAlive = false;

        // Kill any active feedback animations
        if (_activeFeedbackTween != null && _activeFeedbackTween.IsValid())
            _activeFeedbackTween.Kill();
        _activeFeedbackTween = null;

        // Restore visual to original state
        if (_sprite != null && IsInstanceValid(_sprite))
        {
            _sprite.Modulate = _originalColor;
            _sprite.Position = _originalVisualPosition;
        }

        EmitSignal(SignalName.SummonerDestroyed, this);

        if (_sprite != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0f, DeathFadeDuration);
        }
    }

    private void PlayHitFeedback()
    {
        if (_sprite == null || !_isAlive) return;

        // Kill previous tween if still running
        if (_activeFeedbackTween != null && _activeFeedbackTween.IsValid())
            _activeFeedbackTween.Kill();

        // Calculate duration based on attack intensity
        float intensityFactor = 1.0f + (_recentHits * FlashSpeedMultiplier);
        float flashDuration = Math.Max(MinFlashDuration, DefaultFlashDuration / intensityFactor);

        float flashToWhite = flashDuration * FlashToWhiteRatio;
        float flashReturn = flashDuration * FlashReturnRatio;
        float shakeOut = flashDuration * ShakeOutRatio;
        float shakeReturn = flashDuration * ShakeReturnRatio;

        _activeFeedbackTween = CreateTween();
        _activeFeedbackTween.SetParallel(true);

        // Flash effect
        _activeFeedbackTween.TweenProperty(_sprite, "modulate", Colors.White, flashToWhite);
        _activeFeedbackTween.Chain().TweenProperty(_sprite, "modulate", _originalColor, flashReturn);

        // Shake effect
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        var shakeOffset = new Vector3(rng.RandfRange(-0.15f, 0.15f), rng.RandfRange(-0.15f, 0.15f), 0);
        _activeFeedbackTween.TweenProperty(_sprite, "position", _originalVisualPosition + shakeOffset, shakeOut);
        _activeFeedbackTween.Chain().TweenProperty(_sprite, "position", _originalVisualPosition, shakeReturn);
    }

    // =========================================================================
    // MP CLIENT POLLING
    // =========================================================================

    private void PollMatchState()
    {
        if (_session == null) return;

        var summoner = _session.GetState().Summoners[_teamIndex];

        // Poll casting state
        if (summoner.IsCasting != _lastIsCasting)
        {
            _lastIsCasting = summoner.IsCasting;
            if (summoner.IsCasting)
            {
                // MP client polling path has no card reference — null card emitted. See todos.md.
                EmitSignal(SignalName.CastingStarted, (GodotObject)null!, summoner.CastingTimeTotal);
            }
            else
            {
                EmitSignal(SignalName.CastingCompleted, (GodotObject)null!);
            }
        }

        if (summoner.IsCasting)
            EmitSignal(SignalName.CastingProgress, summoner.CastingTimeRemaining, summoner.CastingTimeTotal);

        // Poll hand — only rebuild Card objects when hand contents change
        if (HasHandChanged(summoner.Hand))
        {
            _lastHandIds = summoner.Hand.ToArray();
            RebuildHandCache(_lastHandIds);
            EmitSignal(SignalName.HandChanged, _handCache);
        }

        // Poll mana
        if (Math.Abs(summoner.Mana - _lastMana) > 0.01f || Math.Abs(summoner.MaxMana - _lastMaxMana) > 0.01f)
        {
            _lastMana = summoner.Mana;
            _lastMaxMana = summoner.MaxMana;
            EmitSignal(SignalName.ManaChanged, summoner.Mana, summoner.MaxMana);
        }

        // Poll HP
        if (Math.Abs(summoner.CurrentHp - _lastHp) > 0.01f || Math.Abs(summoner.MaxHp - _lastMaxHp) > 0.01f)
        {
            float damage = _lastHp - summoner.CurrentHp;
            bool tookDamage = ApplyHpUpdate(summoner.CurrentHp, summoner.MaxHp);

            if (tookDamage)
                EmitSignal(SignalName.SummonerDamaged, this, damage);
        }
    }

    private bool HasHandChanged(System.Collections.Generic.List<string> currentHand)
    {
        if (currentHand.Count != _lastHandIds.Length) return true;
        for (int i = 0; i < currentHand.Count; i++)
        {
            if (currentHand[i] != _lastHandIds[i]) return true;
        }
        return false;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Shared HP update logic. Updates last-known values, emits HpChanged,
    /// and triggers hit feedback on damage. Returns true if damage was taken.
    /// </summary>
    private bool ApplyHpUpdate(float hp, float maxHp)
    {
        bool tookDamage = hp < _lastHp;
        _lastHp = hp;
        _lastMaxHp = maxHp;

        EmitSignal(SignalName.HpChanged, hp, maxHp);

        if (tookDamage && _isAlive)
        {
            _recentHits += 1.0f;
            PlayHitFeedback();
        }

        return tookDamage;
    }

    private void RebuildHandCache(string[] catalogIds)
    {
        _handCache.Clear();
        foreach (var id in catalogIds)
        {
            var card = CreateCardResource(id);
            if (card != null)
                _handCache.Add(card);
        }
    }

    private static Card? CreateCardResource(string catalogId)
    {
        var def = CardCatalog.GetCard(catalogId);
        if (def == null) return null;
        return Card.FromDefinition(def);
    }

    private void ConfigureCollisionShape()
    {
        var collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionBody/CollisionShape3D");
        if (collisionShape?.Shape is CylinderShape3D cylinder)
        {
            cylinder.Radius = HurtboxRadius;
            cylinder.Height = HurtboxHeight;
        }
    }
}
