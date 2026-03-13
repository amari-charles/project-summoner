using System;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Infrastructure.Debug;
using Fateforged.Meta;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.UI;
using Godot;

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
    Deferred = 3,
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

    [Export]
    public int Team { get; set; } = 0;

    [Export]
    public float MaxHpExport { get; set; } = 300.0f;

    [Export]
    public NodePath TargetPointNodePath { get; set; } = "TargetPoint";

    [Export]
    public Vector3 TargetPointOffset { get; set; } = new(0f, HurtboxHeight * 0.5f, 0f);

    [Export]
    public int MaxHandSize { get; set; } = 4;

    [Export]
    public DeckLoadStrategy DeckLoadStrategy { get; set; } = DeckLoadStrategy.BattleContext;

    [Export]
    public Godot.Collections.Array<Resource> StartingDeck { get; set; } = new();

    [Export]
    public bool ShowWorldHpBar { get; set; } = false;

    // =========================================================================
    // SIGNALS (PascalCase for GDScript consumers)
    // =========================================================================

    [Signal]
    public delegate void CardPlayedEventHandler(Card card);

    [Signal]
    public delegate void CardDrawnEventHandler(Card card);

    [Signal]
    public delegate void HandChangedEventHandler(Godot.Collections.Array hand);

    [Signal]
    public delegate void ManaChangedEventHandler(float current, float max);

    [Signal]
    public delegate void HpChangedEventHandler(float newHp, float maxHp);

    [Signal]
    public delegate void CastingStartedEventHandler(Card card, float duration);

    [Signal]
    public delegate void CastingProgressEventHandler(float remaining, float total);

    [Signal]
    public delegate void CastingCompletedEventHandler(Card card);

    [Signal]
    public delegate void SummonerReadyEventHandler(Node3D summoner);

    [Signal]
    public delegate void SummonerDestroyedEventHandler(Node3D summoner);

    [Signal]
    public delegate void SummonerDamagedEventHandler(Node3D summoner, float damage);

    [Signal]
    public delegate void DeckRecycledEventHandler(int cardCount);

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
    private const string SummonerShieldRippleShaderPath =
        "res://shaders/vfx/summoner_shield_ripple.gdshader";
    private const float DamageFlashToWhiteDuration = 0.05f;
    private const float DamageFlashReturnDuration = 0.15f;
    private const float SummonerImpactPulseDuration = 0.22f;
    private const float SummonerImpactPulseBandWidth = 0.045f;
    private const float SummonerImpactPulseMaxProgress = 0.07f;
    private const float SummonerImpactPulseMaxAlpha = 0.96f;
    private const float SummonerBubbleCenterYOffset = 0.55f;
    private const float SummonerBubbleVisualRadiusScale = 1.1f;
    private const float SummonerImpactMinYOffset = 0.08f;
    private const float SummonerImpactMaxYOffsetMargin = 0.08f;
    private const float SummonerImpactMeleeAimHeightRatio = 0.25f;
    private const float SummonerImpactMinHorizontalDirLength = 0.000001f;
    private static readonly Color SummonerImpactPulseColor = new(1f, 1f, 1f, 1f);
    private static readonly Vector3 SummonerImpactFallbackDirection = new(0.71f, 0.02f, 0.68f);
    private const float SummonerBubbleRingThicknessScale = 0.04f;
    private static Shader? _summonerShieldRippleShader;

    // Collision shape constants
    private const float HurtboxRadius = 2.0f;
    private const float HurtboxHeight = 6.25f;

    // Hand cache — rebuilt only on HandChangedEvent
    private Godot.Collections.Array<Resource> _handCache = new();
    private Card? _castingCard;

    // MP client polling: last-known values for delta detection
    private bool _castingStartSignaled;
    private string _lastCastingCatalogId = "";
    private float _lastMana;
    private float _lastMaxMana;
    private float _lastHp;
    private float _lastMaxHp;
    private string[] _lastHandIds = Array.Empty<string>();
    private Node3D? _debugSummonerBubbleMarker;
    private float _debugSummonerBubbleRadius = -1f;

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
    public float CastingTimeRemaining =>
        _session?.GetState()?.Summoners[_teamIndex].CastingTimeRemaining ?? 0f;
    public float CastingTimeTotal =>
        _session?.GetState()?.Summoners[_teamIndex].CastingTimeTotal ?? 0f;
    public bool IsEnabled { get; set; } = true;
    public Godot.Collections.Array<Resource> Hand => _handCache;

    public Vector3 GetTargetPointGlobalPosition()
    {
        var targetPoint = GetNodeOrNull<Node3D>(TargetPointNodePath);
        if (targetPoint != null)
            return targetPoint.GlobalPosition;

        return GlobalPosition + TargetPointOffset;
    }

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
        FreeDebugSummonerBubbleMarker();
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

        // Keep the HP bar instance for internal sync (tests/death snap), but hide it
        // from world view by default because summoner HP is presented in HUD.
        if (_hpBar == null)
        {
            var settings = HPBarSettings.AlwaysVisible with { BarWidth = 1.5f, OffsetY = 2.5f };
            _hpBar = new FloatingHPBar();
            AddChild(_hpBar);
            _hpBar.Configure(settings);
            _hpBar.TrackNode(this);
        }

        if (_hpBar != null)
            _hpBar.Visible = ShowWorldHpBar;

        // Initialize last-known values for MP client polling
        var summoner = session.GetState().Summoners[teamIndex];
        _lastMana = summoner.Mana;
        _lastMaxMana = summoner.MaxMana;
        _lastHp = summoner.CurrentHp;
        _lastMaxHp = summoner.MaxHp;
        _castingStartSignaled = false;
        _lastCastingCatalogId = "";

        // Emit initial hand so the UI shows starting cards without waiting for a HandChangedEvent.
        // SetSummonerHand() writes directly to MatchState with no event — host mode never polls,
        // so without this the hand would be invisible until the first card draw.
        if (summoner.Hand.Count > 0)
        {
            _lastHandIds = ToCatalogIdStrings(summoner.Hand);
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
        if (_session == null || !_isAlive)
            return;

        var state = _session.GetState();
        var summoner = state.Summoners[_teamIndex];

        // Update HP bar
        _hpBar?.UpdateHp(summoner.CurrentHp, summoner.MaxHp);
        UpdateDebugSummonerBubble();

        // Decay recent hits counter (for hit feedback animation speed)
        if (_recentHits > 0)
        {
            _recentHits -= RecentHitsDecayRate * (float)delta;
            if (_recentHits < 0)
                _recentHits = 0;
        }

        // Death detection from state
        if (!summoner.IsAlive && _isAlive)
        {
            BeginDeath();
        }
    }

    public override void _Process(double delta)
    {
        if (_session == null || !_isAlive)
            return;

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
        var card = CreateCardResourceRequired(catalogId, "OnCastingStarted");
        _castingCard = card;
        _castingStartSignaled = true;
        _lastCastingCatalogId = card.CatalogId;
        EmitSignal(SignalName.CardPlayed, card);
        EmitSignal(SignalName.CastingStarted, card, duration);
    }

    public void OnCastingCompleted(int cardIndex)
    {
        var completed =
            _castingCard
            ?? CreateCardResourceRequired(
                _lastCastingCatalogId,
                "OnCastingCompleted missing cached casting card"
            );
        _castingCard = null;
        _castingStartSignaled = false;
        _lastCastingCatalogId = "";
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

    public void OnSummonerDamaged(
        float damage,
        int? attackerUnitId = null,
        SimVector3? hitPosition = null
    )
    {
        _recentHits += 1.0f;
        PlayHitFeedback();
        SpawnSummonerImpactPulse(attackerUnitId, hitPosition);
        EmitSignal(SignalName.SummonerDamaged, this, damage);
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
        if (_sprite == null)
            return;

        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", Colors.White, DamageFlashToWhiteDuration);
        tween.TweenProperty(_sprite, "modulate", _originalColor, DamageFlashReturnDuration);
    }

    public void BeginDeath()
    {
        if (!_isAlive)
            return;

        float snappedMaxHp = MaxHp > 0f ? MaxHp : (_lastMaxHp > 0f ? _lastMaxHp : 1f);
        _lastHp = 0f;
        _lastMaxHp = snappedMaxHp;
        _hpBar?.UpdateHpImmediate(0f, snappedMaxHp);

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

        Visible = false;

        EmitSignal(SignalName.SummonerDestroyed, this);
    }

    private void PlayHitFeedback()
    {
        if (_sprite == null || !_isAlive)
            return;

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
        _activeFeedbackTween
            .Chain()
            .TweenProperty(_sprite, "modulate", _originalColor, flashReturn);

        // Shake effect
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        var shakeOffset = new Vector3(
            rng.RandfRange(-0.15f, 0.15f),
            rng.RandfRange(-0.15f, 0.15f),
            0
        );
        _activeFeedbackTween.TweenProperty(
            _sprite,
            "position",
            _originalVisualPosition + shakeOffset,
            shakeOut
        );
        _activeFeedbackTween
            .Chain()
            .TweenProperty(_sprite, "position", _originalVisualPosition, shakeReturn);
    }

    // =========================================================================
    // MP CLIENT POLLING
    // =========================================================================

    private void PollMatchState()
    {
        if (_session == null)
            return;

        var summoner = _session.GetState().Summoners[_teamIndex];

        // Poll casting state. Keep UI state self-healing for reconnect/mid-cast snapshots.
        if (summoner.IsCasting)
        {
            if (!_castingStartSignaled)
            {
                EmitCastingStartedFromSnapshot(summoner);
                _castingStartSignaled = true;
            }
        }
        else if (_castingStartSignaled)
        {
            EmitCastingCompletedFromCache();
            _castingStartSignaled = false;
        }

        if (summoner.IsCasting)
            EmitSignal(
                SignalName.CastingProgress,
                summoner.CastingTimeRemaining,
                summoner.CastingTimeTotal
            );

        // Poll hand — only rebuild Card objects when hand contents change
        if (HasHandChanged(summoner.Hand))
        {
            _lastHandIds = ToCatalogIdStrings(summoner.Hand);
            RebuildHandCache(_lastHandIds);
            EmitSignal(SignalName.HandChanged, _handCache);
        }

        // Poll mana
        if (
            Math.Abs(summoner.Mana - _lastMana) > 0.01f
            || Math.Abs(summoner.MaxMana - _lastMaxMana) > 0.01f
        )
        {
            _lastMana = summoner.Mana;
            _lastMaxMana = summoner.MaxMana;
            EmitSignal(SignalName.ManaChanged, summoner.Mana, summoner.MaxMana);
        }

        // Poll HP
        if (
            Math.Abs(summoner.CurrentHp - _lastHp) > 0.01f
            || Math.Abs(summoner.MaxHp - _lastMaxHp) > 0.01f
        )
        {
            float damage = _lastHp - summoner.CurrentHp;
            bool tookDamage = ApplyHpUpdate(summoner.CurrentHp, summoner.MaxHp);

            if (tookDamage)
                EmitSignal(SignalName.SummonerDamaged, this, damage);
        }
    }

    private bool HasHandChanged(System.Collections.Generic.List<SimCardCatalogId> currentHand)
    {
        if (currentHand.Count != _lastHandIds.Length)
            return true;
        for (int i = 0; i < currentHand.Count; i++)
        {
            if (currentHand[i].Value != _lastHandIds[i])
                return true;
        }
        return false;
    }

    private static string[] ToCatalogIdStrings(
        System.Collections.Generic.List<SimCardCatalogId> ids
    )
    {
        var result = new string[ids.Count];
        for (int i = 0; i < ids.Count; i++)
            result[i] = ids[i].Value;
        return result;
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
        if (def == null)
            return null;
        return Card.FromDefinition(def);
    }

    private static Card CreateCardResourceRequired(string catalogId, string context)
    {
        if (string.IsNullOrWhiteSpace(catalogId))
        {
            string msg = $"[SummonerVisual] {context}: empty casting catalogId is invalid";
            GD.PushError(msg);
            throw new InvalidOperationException(msg);
        }

        var card = CreateCardResource(catalogId);
        if (card != null)
            return card;

        string error = $"[SummonerVisual] {context}: unknown casting catalogId={catalogId}";
        GD.PushError(error);
        throw new InvalidOperationException(error);
    }

    private void EmitCastingStartedFromSnapshot(SummonerData summoner)
    {
        var card = CreateCardResourceRequired(
            summoner.CastingCatalogId,
            "MP snapshot casting start"
        );
        _castingCard = card;
        _lastCastingCatalogId = card.CatalogId;
        EmitSignal(SignalName.CardPlayed, card);
        EmitSignal(SignalName.CastingStarted, card, summoner.CastingTimeTotal);
    }

    private void EmitCastingCompletedFromCache()
    {
        var completed =
            _castingCard
            ?? CreateCardResourceRequired(
                _lastCastingCatalogId,
                "MP snapshot casting complete missing cached casting card"
            );
        _castingCard = null;
        _lastCastingCatalogId = "";
        EmitSignal(SignalName.CastingCompleted, completed);
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

    private void SpawnSummonerImpactPulse(int? attackerUnitId, SimVector3? hitPosition = null)
    {
        _summonerShieldRippleShader ??= GD.Load<Shader>(SummonerShieldRippleShaderPath);
        if (_summonerShieldRippleShader == null)
        {
            GD.PushWarning(
                $"[SummonerVisual] Failed to load shield ripple shader at {SummonerShieldRippleShaderPath}"
            );
            return;
        }

        float baseBubbleRadius = Mathf.Max(0.1f, SummonerMeleeBubble.EffectiveRadius);
        float bubbleRadius = ResolveSummonerVisualBubbleRadius(baseBubbleRadius);
        Vector3 center = ResolveSummonerBubbleCenterWorldPosition();
        bool useFallbackDirection = !HasResolvedImpactSource(attackerUnitId, hitPosition);
        Vector3 impactPoint = ResolveSummonerImpactPointWorldPosition(
            attackerUnitId,
            hitPosition,
            center,
            bubbleRadius
        );
        Vector3 impactDirection = useFallbackDirection
            ? SummonerImpactFallbackDirection.Normalized()
            : ResolveSummonerImpactDirection(impactPoint, center);
        var material = CreateSummonerImpactRippleMaterial(impactDirection);
        if (material == null)
            return;

        var pulse = new MeshInstance3D
        {
            Name = "SummonerImpactPulse",
            Mesh = CreateHemisphereMesh(bubbleRadius),
            MaterialOverride = material,
        };
        AddChild(pulse);
        pulse.GlobalPosition = center;
        pulse.Rotation = Vector3.Zero;

        var tween = CreateTween();
        if (pulse.MaterialOverride is ShaderMaterial rippleMaterial)
        {
            tween.TweenMethod(
                Callable.From<float>(
                    progress =>
                    {
                        if (!GodotObject.IsInstanceValid(rippleMaterial))
                            return;

                        rippleMaterial.SetShaderParameter("progress", progress);
                    }
                ),
                0f,
                SummonerImpactPulseMaxProgress,
                SummonerImpactPulseDuration
            );
            tween
                .Parallel()
                .TweenMethod(
                    Callable.From<float>(
                        alpha =>
                        {
                            if (!GodotObject.IsInstanceValid(rippleMaterial))
                                return;

                            rippleMaterial.SetShaderParameter("alpha", alpha);
                        }
                    ),
                    SummonerImpactPulseMaxAlpha,
                    0f,
                    SummonerImpactPulseDuration
                );
        }

        tween.TweenCallback(
            Callable.From(
                () =>
                {
                    if (GodotObject.IsInstanceValid(pulse))
                        pulse.QueueFree();
                }
            )
        );
    }

    private ShaderMaterial? CreateSummonerImpactRippleMaterial(Vector3 impactDirection)
    {
        if (_summonerShieldRippleShader == null)
            return null;

        var material = new ShaderMaterial { Shader = _summonerShieldRippleShader };
        material.SetShaderParameter("ripple_color", SummonerImpactPulseColor);
        material.SetShaderParameter("impact_dir", impactDirection.Normalized());
        material.SetShaderParameter("progress", 0f);
        material.SetShaderParameter("band_width", SummonerImpactPulseBandWidth);
        material.SetShaderParameter("alpha", SummonerImpactPulseMaxAlpha);
        return material;
    }

    private Vector3 ResolveSummonerBubbleCenterWorldPosition()
    {
        return new Vector3(
            GlobalPosition.X,
            GlobalPosition.Y + SummonerBubbleCenterYOffset,
            GlobalPosition.Z
        );
    }

    private static float ResolveSummonerVisualBubbleRadius(float baseRadius)
    {
        return Mathf.Max(0.1f, baseRadius * SummonerBubbleVisualRadiusScale);
    }

    private Vector3 ResolveSummonerImpactPointWorldPosition(
        int? attackerUnitId,
        SimVector3? explicitHitPosition,
        Vector3 center,
        float bubbleRadius
    )
    {
        if (explicitHitPosition.HasValue)
        {
            Vector3 explicitWorld = ConvertSimToWorld(explicitHitPosition.Value);
            return ResolveBubbleSliceImpactPoint(
                center,
                bubbleRadius,
                explicitWorld,
                explicitWorld.Y
            );
        }

        if (attackerUnitId.HasValue && _session != null)
        {
            var state = _session.GetState();
            if (state.Units.TryGetValue(attackerUnitId.Value, out var attacker))
            {
                Vector3 attackerWorld = ConvertSimToWorld(attacker.Position);
                Vector3 meleeAim = ResolveSummonerMeleeAimPointWorldPosition(center, bubbleRadius);
                return ResolveBubbleSliceImpactPoint(center, bubbleRadius, attackerWorld, meleeAim.Y);
            }
        }

        float fallbackY = ResolveSummonerMeleeAimPointWorldPosition(center, bubbleRadius).Y;
        return ResolveBubbleSliceImpactPoint(
            center,
            bubbleRadius,
            center + SummonerImpactFallbackDirection,
            fallbackY
        );
    }

    private Vector3 ResolveSummonerMeleeAimPointWorldPosition(Vector3 center, float bubbleRadius)
    {
        float aimY = GetTargetPointGlobalPosition().Y;
        float minAimY = center.Y + SummonerImpactMinYOffset;
        float maxAimY = center.Y + (bubbleRadius * SummonerImpactMeleeAimHeightRatio);
        if (aimY <= minAimY)
            aimY = maxAimY;

        aimY = Mathf.Clamp(aimY, minAimY, maxAimY);

        return new Vector3(center.X, aimY, center.Z);
    }

    private bool HasResolvedImpactSource(int? attackerUnitId, SimVector3? explicitHitPosition)
    {
        if (explicitHitPosition.HasValue)
            return true;

        if (!attackerUnitId.HasValue || _session == null)
            return false;

        return _session.GetState().Units.ContainsKey(attackerUnitId.Value);
    }

    private static Vector3 ResolveBubbleSliceImpactPoint(
        Vector3 center,
        float bubbleRadius,
        Vector3 horizontalDirectionSource,
        float desiredY
    )
    {
        float y = Mathf.Clamp(
            desiredY,
            center.Y + SummonerImpactMinYOffset,
            center.Y + Mathf.Max(
                SummonerImpactMinYOffset,
                bubbleRadius - SummonerImpactMaxYOffsetMargin
            )
        );
        float yOffset = y - center.Y;
        float horizontalRadius = Mathf.Sqrt(Mathf.Max(0f, (bubbleRadius * bubbleRadius) - (yOffset * yOffset)));

        var radial = horizontalDirectionSource - center;
        radial.Y = 0f;
        if (radial.LengthSquared() < SummonerImpactMinHorizontalDirLength)
            radial = new Vector3(SummonerImpactFallbackDirection.X, 0f, SummonerImpactFallbackDirection.Z);

        var radialDir = radial.Normalized();
        return new Vector3(
            center.X + (radialDir.X * horizontalRadius),
            y,
            center.Z + (radialDir.Z * horizontalRadius)
        );
    }

    private static Vector3 ResolveSummonerImpactDirection(Vector3 impactPoint, Vector3 center)
    {
        var radial = impactPoint - center;
        if (radial.LengthSquared() < SummonerImpactMinHorizontalDirLength)
            return SummonerImpactFallbackDirection;
        return radial.Normalized();
    }

    private static Vector3 ConvertSimToWorld(SimVector3 position)
    {
        var simNode = SimulationNode.Current;
        if (simNode != null)
            return simNode.SimToLocal(position);

        return new Vector3(position.X, position.Y, position.Z);
    }

    private void UpdateDebugSummonerBubble()
    {
        var debugService = BattlefieldDebugService.Instance;
        if (debugService?.SummonerBubbleEnabled != true)
        {
            FreeDebugSummonerBubbleMarker();
            return;
        }

        float baseRadius = Mathf.Max(0.1f, debugService.GetSummonerMeleeBubbleEffectiveRadius());
        float radius = ResolveSummonerVisualBubbleRadius(baseRadius);
        bool needsRebuild = _debugSummonerBubbleMarker == null || !Mathf.IsEqualApprox(radius, _debugSummonerBubbleRadius);
        if (needsRebuild)
        {
            FreeDebugSummonerBubbleMarker();
            _debugSummonerBubbleMarker = CreateDebugBubbleMarker(radius, 97);
            AddChild(_debugSummonerBubbleMarker);
            _debugSummonerBubbleRadius = radius;
        }

        if (_debugSummonerBubbleMarker == null)
            return;

        _debugSummonerBubbleMarker.GlobalPosition = GlobalPosition;
        _debugSummonerBubbleMarker.Rotation = Vector3.Zero;
    }

    private static Node3D CreateDebugBubbleMarker(float radius, int renderPriority)
    {
        var root = new Node3D();
        var ringThickness = Mathf.Max(0.06f, radius * SummonerBubbleRingThicknessScale);
        var ring = CreateDebugGroundRing(
            radius,
            ringThickness,
            new Color(0.3f, 0.9f, 1.0f, 0.28f),
            renderPriority + 1
        );
        ring.Name = "Ring";
        var cap = CreateDebugHemisphere(
            radius,
            new Color(0.3f, 0.9f, 1.0f, 0.24f),
            renderPriority
        );
        cap.Name = "Dome";
        cap.Position = new Vector3(0f, SummonerBubbleCenterYOffset, 0f);
        root.AddChild(cap);
        root.AddChild(ring);
        return root;
    }

    private static MeshInstance3D CreateDebugHemisphere(float radius, Color color, int renderPriority)
    {
        return new MeshInstance3D
        {
            Mesh = CreateHemisphereMesh(radius),
            MaterialOverride = CreateDebugMaterial(color, renderPriority)
        };
    }

    private static MeshInstance3D CreateDebugGroundRing(
        float radius,
        float thickness,
        Color color,
        int renderPriority
    )
    {
        return new MeshInstance3D
        {
            Mesh = CreateRingMesh(radius, thickness),
            MaterialOverride = CreateDebugMaterial(color, renderPriority),
        };
    }

    private static ArrayMesh CreateRingMesh(float radius, float thickness)
    {
        const int segments = 48;
        float outer = Mathf.Max(0.1f, radius);
        float inner = Mathf.Max(0.05f, outer - Mathf.Max(0.01f, thickness));
        const float y = 0.025f;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < segments; i++)
        {
            float t0 = (i / (float)segments) * Mathf.Tau;
            float t1 = ((i + 1) / (float)segments) * Mathf.Tau;

            var o0 = new Vector3(Mathf.Cos(t0) * outer, y, Mathf.Sin(t0) * outer);
            var o1 = new Vector3(Mathf.Cos(t1) * outer, y, Mathf.Sin(t1) * outer);
            var i0 = new Vector3(Mathf.Cos(t0) * inner, y, Mathf.Sin(t0) * inner);
            var i1 = new Vector3(Mathf.Cos(t1) * inner, y, Mathf.Sin(t1) * inner);

            st.AddVertex(o0);
            st.AddVertex(o1);
            st.AddVertex(i1);

            st.AddVertex(o0);
            st.AddVertex(i1);
            st.AddVertex(i0);
        }

        st.GenerateNormals();
        return st.Commit();
    }

    private static ArrayMesh CreateHemisphereMesh(float radius)
    {
        const int latSegments = 10;
        const int lonSegments = 20;

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        for (int lat = 0; lat < latSegments; lat++)
        {
            float phi0 = (lat / (float)latSegments) * (Mathf.Pi * 0.5f);
            float phi1 = ((lat + 1) / (float)latSegments) * (Mathf.Pi * 0.5f);

            for (int lon = 0; lon < lonSegments; lon++)
            {
                float theta0 = (lon / (float)lonSegments) * Mathf.Tau;
                float theta1 = ((lon + 1) / (float)lonSegments) * Mathf.Tau;

                Vector3 v00 = HemispherePoint(radius, phi0, theta0);
                Vector3 v10 = HemispherePoint(radius, phi1, theta0);
                Vector3 v11 = HemispherePoint(radius, phi1, theta1);
                Vector3 v01 = HemispherePoint(radius, phi0, theta1);

                surfaceTool.AddVertex(v00);
                surfaceTool.AddVertex(v10);
                surfaceTool.AddVertex(v11);

                surfaceTool.AddVertex(v00);
                surfaceTool.AddVertex(v11);
                surfaceTool.AddVertex(v01);
            }
        }

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
    }

    private static Vector3 HemispherePoint(float radius, float phi, float theta)
    {
        float sinPhi = Mathf.Sin(phi);
        float cosPhi = Mathf.Cos(phi);
        float sinTheta = Mathf.Sin(theta);
        float cosTheta = Mathf.Cos(theta);
        return new Vector3(
            radius * sinPhi * cosTheta,
            radius * cosPhi,
            radius * sinPhi * sinTheta
        );
    }

    private static StandardMaterial3D CreateDebugMaterial(Color color, int renderPriority)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            NoDepthTest = true,
            RenderPriority = renderPriority
        };
    }

    private void FreeDebugSummonerBubbleMarker()
    {
        if (_debugSummonerBubbleMarker != null)
            _debugSummonerBubbleMarker.QueueFree();
        _debugSummonerBubbleMarker = null;
        _debugSummonerBubbleRadius = -1f;
    }
}
