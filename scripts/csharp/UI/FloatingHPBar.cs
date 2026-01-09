using Godot;
using ProjectSummoner.Services;
using ProjectSummoner.Units;

namespace ProjectSummoner.UI;

/// <summary>
/// 3D floating health bar that follows a unit.
/// Managed by HPBarService for pooling.
/// </summary>
public partial class FloatingHPBar : Node3D
{
    private const float BaseHpBarHeight = 3.2f;
    private const int TextureWidth = 100;
    private const int TextureHeight = 12;

    #region Configuration

    public float BarWidth { get; set; } = 0.8f;
    public float BarHeight { get; set; } = 0.08f;
    public float OffsetY { get; set; } = 3.2f;
    public bool ShowOnDamageOnly { get; set; } = true;
    public float FadeDelay { get; set; } = 3.0f;
    public float FadeDuration { get; set; } = 0.5f;

    // Colors
    public Color ColorFull { get; set; } = Colors.Green;
    public Color ColorMid { get; set; } = Colors.Yellow;
    public Color ColorLow { get; set; } = Colors.Red;
    public Color BackgroundColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    #endregion

    #region State

    private Node3D? _trackedNode;
    private Unit3D? _trackedUnit;
    private float _currentHp = 100f;
    private float _maxHp = 100f;
    private float _fadeTimer;
    private bool _barIsVisible = true;
    private float _cachedOffsetX;

    #endregion

    #region Visuals

    private Sprite3D? _sprite;
    private Image? _barImage;
    private ImageTexture? _barTexture;
    private Tween? _fadeTween;

    #endregion

    [Signal]
    public delegate void BarHiddenEventHandler();

    public override void _Ready()
    {
        CreateSpriteVisuals();
    }

    public override void _ExitTree()
    {
        // Kill any active tweens to prevent lambda capture errors
        _fadeTween?.Kill();
    }

    public override void _Process(double delta)
    {
        if (_trackedNode == null || !IsInstanceValid(_trackedNode))
            return;

        // Safety check: ensure both we and the target are in the scene tree
        if (!IsInsideTree() || !_trackedNode.IsInsideTree())
            return;

        // Follow target with cached offsets
        var targetPos = _trackedNode.GlobalPosition + new Vector3(_cachedOffsetX, OffsetY, 0);
        GlobalPosition = targetPos;

        // Handle fade timer
        if (ShowOnDamageOnly && _fadeTimer > 0)
        {
            _fadeTimer -= (float)delta;
            if (_fadeTimer <= 0)
            {
                FadeOut();
            }
        }
    }

    #region Public API

    /// <summary>
    /// Configure the bar with settings.
    /// </summary>
    public void Configure(HPBarSettings settings)
    {
        BarWidth = settings.BarWidth;
        BarHeight = settings.BarHeight;
        OffsetY = settings.OffsetY;
        ShowOnDamageOnly = settings.ShowOnDamageOnly;
        FadeDelay = settings.FadeDelay;
        FadeDuration = settings.FadeDuration;
    }

    /// <summary>
    /// Track a Unit3D. Connects to HpChanged and TreeExiting signals.
    /// THE FIX: TreeExiting ensures cleanup even if unit is freed unexpectedly.
    /// </summary>
    public void TrackUnit(Unit3D unit)
    {
        _trackedUnit = unit;
        _trackedNode = unit;

        // THE FIX: Connect to TreeExiting for guaranteed cleanup
        unit.TreeExiting += OnTrackedNodeExiting;

        // Connect to HP changes
        unit.HpChanged += OnHpChanged;

        // Calculate offset after visual is ready
        CallDeferred(MethodName.DeferredCalculateOffset);

        // Update HP immediately
        UpdateHp(unit.CurrentHp, unit.MaxHp);
    }

    /// <summary>
    /// Track a generic Node3D (summoners, bases).
    /// </summary>
    public void TrackNode(Node3D node)
    {
        _trackedNode = node;
        _trackedUnit = null;

        // Connect to TreeExiting for cleanup
        node.TreeExiting += OnTrackedNodeExiting;

        // Check for hp_changed signal (GDScript summoners)
        if (node.HasSignal("hp_changed"))
        {
            node.Connect("hp_changed", Callable.From<float, float>(OnHpChanged));
        }

        // Calculate offset after visual is ready
        CallDeferred(MethodName.DeferredCalculateOffset);

        // Try to get initial HP values
        if (node.HasMethod("get_current_hp") && node.HasMethod("get_max_hp"))
        {
            var currentHp = (float)node.Call("get_current_hp");
            var maxHp = (float)node.Call("get_max_hp");
            UpdateHp(currentHp, maxHp);
        }
        else if (node.Get("current_hp").VariantType != Variant.Type.Nil)
        {
            var currentHp = (float)node.Get("current_hp");
            var maxHp = (float)node.Get("max_hp");
            UpdateHp(currentHp, maxHp);
        }
    }

    /// <summary>
    /// Detach from tracked node (disconnect signals).
    /// </summary>
    public void Detach()
    {
        if (_trackedUnit != null && IsInstanceValid(_trackedUnit))
        {
            _trackedUnit.TreeExiting -= OnTrackedNodeExiting;
            _trackedUnit.HpChanged -= OnHpChanged;
        }
        else if (_trackedNode != null && IsInstanceValid(_trackedNode))
        {
            _trackedNode.TreeExiting -= OnTrackedNodeExiting;

            if (_trackedNode.HasSignal("hp_changed") && _trackedNode.IsConnected("hp_changed", Callable.From<float, float>(OnHpChanged)))
            {
                _trackedNode.Disconnect("hp_changed", Callable.From<float, float>(OnHpChanged));
            }
        }

        _trackedNode = null;
        _trackedUnit = null;
    }

    /// <summary>
    /// Update the HP bar display.
    /// </summary>
    public void UpdateHp(float current, float max)
    {
        _currentHp = current;
        _maxHp = max;

        var hpPercent = _maxHp > 0 ? Mathf.Clamp(_currentHp / _maxHp, 0f, 1f) : 0f;

        // Redraw the bar texture
        RedrawBarTexture(hpPercent);

        // Handle show_on_damage_only behavior
        if (ShowOnDamageOnly)
        {
            if (hpPercent < 1f)
            {
                Show();
                _fadeTimer = FadeDelay;
            }
            else
            {
                HideImmediate();
            }
        }
    }

    /// <summary>
    /// Reset for pooling reuse.
    /// </summary>
    public void Reset()
    {
        // Detach from any tracked node
        Detach();

        // Reset state
        _currentHp = 100f;
        _maxHp = 100f;
        _fadeTimer = 0f;
        _barIsVisible = false;
        Visible = false;
        _cachedOffsetX = 0f;

        // Reset configuration to defaults
        BarWidth = 0.8f;
        BarHeight = 0.08f;
        OffsetY = 3.2f;
        ShowOnDamageOnly = true;
        FadeDelay = 3.0f;
        FadeDuration = 0.5f;

        // Reset sprite properties
        if (_sprite != null)
        {
            _sprite.Modulate = Colors.White;
        }

        // Redraw at full HP
        RedrawBarTexture(1f);
    }

    #endregion

    #region Signal Handlers

    /// <summary>
    /// THE FIX: Called when tracked node is about to exit the tree.
    /// This fires BEFORE the node is freed, guaranteeing cleanup.
    /// </summary>
    private void OnTrackedNodeExiting()
    {
        // Remove ourselves from the service
        if (_trackedNode != null)
        {
            HPBarService.Instance?.RemoveBar(_trackedNode);
        }
    }

    private void OnHpChanged(float newHp, float maxHp)
    {
        UpdateHp(newHp, maxHp);
    }

    #endregion

    #region Visual Rendering

    private void CreateSpriteVisuals()
    {
        // Create image for drawing
        _barImage = Image.CreateEmpty(TextureWidth, TextureHeight, false, Image.Format.Rgba8);

        // Draw initial full HP bar
        RedrawBarTexture(1f);

        // Calculate pixel size to achieve desired world size
        var pixelsPerUnit = TextureWidth / BarWidth;
        var pixelSize = 1f / pixelsPerUnit;

        // Create sprite
        _sprite = new Sprite3D
        {
            Texture = _barTexture,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            PixelSize = pixelSize,
            Centered = true
        };
        AddChild(_sprite);
    }

    private void RedrawBarTexture(float hpPercent)
    {
        if (_barImage == null)
            return;

        var width = _barImage.GetWidth();
        var height = _barImage.GetHeight();

        // Calculate bar width in pixels
        var barPixelWidth = (int)(width * hpPercent);

        // Fill background
        _barImage.Fill(BackgroundColor);

        // Draw foreground bar
        if (barPixelWidth > 0)
        {
            var barColor = GetHpColor(hpPercent);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < barPixelWidth; x++)
                {
                    _barImage.SetPixel(x, y, barColor);
                }
            }
        }

        // Update texture
        if (_barTexture != null)
        {
            _barTexture.Update(_barImage);
        }
        else
        {
            _barTexture = ImageTexture.CreateFromImage(_barImage);
            if (_sprite != null)
            {
                _sprite.Texture = _barTexture;
            }
        }
    }

    private Color GetHpColor(float hpPercent)
    {
        if (hpPercent > 0.5f)
        {
            // Interpolate between mid and full
            var t = (hpPercent - 0.5f) / 0.5f;
            return ColorMid.Lerp(ColorFull, t);
        }
        else
        {
            // Interpolate between low and mid
            var t = hpPercent / 0.5f;
            return ColorLow.Lerp(ColorMid, t);
        }
    }

    #endregion

    #region Visibility

    private new void Show()
    {
        if (_barIsVisible)
            return;

        _barIsVisible = true;
        Visible = true;

        if (_sprite != null)
        {
            _sprite.Modulate = new Color(_sprite.Modulate, 1f);
        }
    }

    private void HideImmediate()
    {
        _barIsVisible = false;
        Visible = false;
    }

    private void FadeOut()
    {
        if (!_barIsVisible)
            return;

        _fadeTween?.Kill();
        _fadeTween = CreateTween();

        if (_sprite != null)
        {
            _fadeTween.TweenProperty(_sprite, "modulate:a", 0f, FadeDuration)
                .From(_sprite.Modulate.A);
        }

        _fadeTween.Finished += () =>
        {
            HideImmediate();
            EmitSignal(SignalName.BarHidden);
        };
    }

    #endregion

    #region Offset Calculation

    private void DeferredCalculateOffset()
    {
        if (_trackedNode == null || !IsInstanceValid(_trackedNode))
            return;

        // Calculate vertical offset
        OffsetY = CalculateBarOffset();

        // Cache horizontal offset
        _cachedOffsetX = 0f;
        var visual = _trackedNode.GetNodeOrNull("Visual");
        if (visual != null && visual.HasMethod("get_hp_bar_offset_x"))
        {
            _cachedOffsetX = (float)visual.Call("get_hp_bar_offset_x");
        }
    }

    private float CalculateBarOffset()
    {
        if (_trackedNode == null)
            return BaseHpBarHeight;

        var visual = _trackedNode.GetNodeOrNull("Visual");
        if (visual == null)
        {
            // Bases don't have Visual components
            return BaseHpBarHeight;
        }

        // Query sprite height from visual component
        if (visual.HasMethod("get_sprite_height"))
        {
            var spriteHeight = (float)visual.Call("get_sprite_height");

            // Scale factor from parent
            var scaleFactor = _trackedNode.Scale.Y;
            var scaledSpriteHeight = spriteHeight * scaleFactor;
            var padding = 0.8f;

            return scaledSpriteHeight + padding;
        }

        return BaseHpBarHeight;
    }

    #endregion
}
