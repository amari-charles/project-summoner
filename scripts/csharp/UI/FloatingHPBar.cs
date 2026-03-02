using Godot;
using ProjectSummoner.Services;

namespace ProjectSummoner.UI;

/// <summary>
/// 3D floating health bar that follows a node.
/// Uses GPU shader for efficient rendering.
/// Created and owned by the visual that needs it (UnitVisual, SummonerVisual).
/// </summary>
public partial class FloatingHPBar : Node3D
{
    private const string ShaderPath = "res://shaders/ui/hp_bar.gdshader";

    // Padding above sprite when calculating dynamic offset (in world units)
    private const float OffsetPadding = 0.8f;

    // Damage flash duration
    private const float DamageFlashDuration = 0.15f;

    #region Configuration (from HPBarSettings)

    private float _barWidth = HPBarSettings.DefaultBarWidth;
    private float _barHeight = HPBarSettings.DefaultBarHeight;
    private float _offsetY = HPBarSettings.DefaultOffsetY;
    private float _offsetZ = HPBarSettings.DefaultOffsetZ;
    private bool _showOnDamageOnly = true;
    private float _fadeDelay = HPBarSettings.DefaultFadeDelay;
    private float _fadeDuration = HPBarSettings.DefaultFadeDuration;
    private float _thresholdMid = HPBarSettings.DefaultThresholdMid;
    private float _thresholdLow = HPBarSettings.DefaultThresholdLow;
    private float _animationSpeed = HPBarSettings.DefaultAnimationSpeed;

    #endregion

    #region State

    private Node3D? _trackedNode;
    private float _targetHpPercent = 1f;
    private float _displayHpPercent = 1f;
    private float _shieldPercent;
    private float _maxHp = 100f;
    private float _fadeTimer;
    private bool _barIsVisible = true;
    private float _cachedOffsetX;
    private float _damageFlashTimer;

    #endregion

    #region Visuals

    private MeshInstance3D? _meshInstance;
    private ShaderMaterial? _shaderMaterial;
    private static Shader? _cachedShader;
    private Tween? _fadeTween;

    #endregion

    [Signal]
    public delegate void BarHiddenEventHandler();

    public override void _Ready()
    {
        CreateShaderVisuals();
    }

    public override void _ExitTree()
    {
        // Kill any active tweens to prevent lambda capture errors
        _fadeTween?.Kill();
    }

    public override void _Process(double delta)
    {
        float deltaF = (float)delta;

        // Animate display percent toward target (smooth HP drain)
        if (!Mathf.IsEqualApprox(_displayHpPercent, _targetHpPercent))
        {
            _displayHpPercent = Mathf.MoveToward(_displayHpPercent, _targetHpPercent, _animationSpeed * deltaF);
            UpdateShaderDisplayPercent();
        }

        // Update damage flash
        if (_damageFlashTimer > 0)
        {
            _damageFlashTimer -= deltaF;
            float flashIntensity = Mathf.Clamp(_damageFlashTimer / DamageFlashDuration, 0f, 1f);
            _shaderMaterial?.SetShaderParameter("damage_flash", flashIntensity);
        }

        // Follow tracked node
        if (_trackedNode == null || !IsInstanceValid(_trackedNode))
            return;

        if (!IsInsideTree() || !_trackedNode.IsInsideTree())
            return;

        var targetPos = _trackedNode.GlobalPosition + new Vector3(_cachedOffsetX, _offsetY, _offsetZ);
        GlobalPosition = targetPos;

        // Handle fade timer
        if (_showOnDamageOnly && _fadeTimer > 0)
        {
            _fadeTimer -= deltaF;
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
        _barWidth = settings.BarWidth;
        _barHeight = settings.BarHeight;
        _offsetY = settings.OffsetY;
        _offsetZ = settings.OffsetZ;
        _showOnDamageOnly = settings.ShowOnDamageOnly;
        _fadeDelay = settings.FadeDelay;
        _fadeDuration = settings.FadeDuration;
        _thresholdMid = settings.ThresholdMid;
        _thresholdLow = settings.ThresholdLow;
        _animationSpeed = settings.AnimationSpeed;

        // Apply size to mesh
        UpdateMeshSize();

        // Apply shader parameters
        ApplyShaderColors(settings);
        _shaderMaterial?.SetShaderParameter("threshold_mid", _thresholdMid);
        _shaderMaterial?.SetShaderParameter("threshold_low", _thresholdLow);
    }

    /// <summary>
    /// Track a Node3D. Connects to TreeExiting for cleanup and hp_changed if available.
    /// </summary>
    public void TrackNode(Node3D node)
    {
        _trackedNode = node;

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
        if (_trackedNode != null && IsInstanceValid(_trackedNode))
        {
            _trackedNode.TreeExiting -= OnTrackedNodeExiting;

            if (_trackedNode.HasSignal("hp_changed") && _trackedNode.IsConnected("hp_changed", Callable.From<float, float>(OnHpChanged)))
            {
                _trackedNode.Disconnect("hp_changed", Callable.From<float, float>(OnHpChanged));
            }
        }

        _trackedNode = null;
    }

    /// <summary>
    /// Update the HP bar display.
    /// </summary>
    public void UpdateHp(float current, float max)
    {
        bool wasDamaged = current < (_targetHpPercent * _maxHp);
        _maxHp = max;
        _targetHpPercent = max > 0 ? Mathf.Clamp(current / max, 0f, 1f) : 0f;

        // Update the actual HP percent in shader (color calculation uses this)
        _shaderMaterial?.SetShaderParameter("hp_percent", _targetHpPercent);

        // Trigger damage flash on HP decrease
        if (wasDamaged && _damageFlashTimer <= 0)
        {
            _damageFlashTimer = DamageFlashDuration;
        }

        // Handle visibility based on settings
        if (_showOnDamageOnly)
        {
            // Only show when damaged
            if (_targetHpPercent < 1f)
            {
                ShowBar();
                _fadeTimer = _fadeDelay;
            }
            else
            {
                HideImmediate();
            }
        }
        else
        {
            // Always visible mode - ensure bar is shown
            ShowBar();
        }
    }

    /// <summary>
    /// Update shield display (0-1 percentage, renders as overlay).
    /// </summary>
    public void UpdateShield(float shieldPercent)
    {
        _shieldPercent = Mathf.Clamp(shieldPercent, 0f, 1f);
        _shaderMaterial?.SetShaderParameter("shield_percent", _shieldPercent);
    }

    /// <summary>
    /// Reset for pooling reuse.
    /// </summary>
    public void Reset()
    {
        // Detach from any tracked node
        Detach();

        // Reset state
        _targetHpPercent = 1f;
        _displayHpPercent = 1f;
        _shieldPercent = 0f;
        _maxHp = 100f;
        _fadeTimer = 0f;
        _barIsVisible = false;
        Visible = false;
        _cachedOffsetX = 0f;
        _damageFlashTimer = 0f;

        // Reset configuration to defaults
        _barWidth = HPBarSettings.DefaultBarWidth;
        _barHeight = HPBarSettings.DefaultBarHeight;
        _offsetY = HPBarSettings.DefaultOffsetY;
        _offsetZ = HPBarSettings.DefaultOffsetZ;
        _showOnDamageOnly = true;
        _fadeDelay = HPBarSettings.DefaultFadeDelay;
        _fadeDuration = HPBarSettings.DefaultFadeDuration;
        _thresholdMid = HPBarSettings.DefaultThresholdMid;
        _thresholdLow = HPBarSettings.DefaultThresholdLow;
        _animationSpeed = HPBarSettings.DefaultAnimationSpeed;

        // Reset mesh size
        UpdateMeshSize();

        // Reset shader parameters
        if (_shaderMaterial != null)
        {
            _shaderMaterial.SetShaderParameter("hp_percent", 1f);
            _shaderMaterial.SetShaderParameter("display_percent", 1f);
            _shaderMaterial.SetShaderParameter("shield_percent", 0f);
            _shaderMaterial.SetShaderParameter("damage_flash", 0f);
            _shaderMaterial.SetShaderParameter("threshold_mid", _thresholdMid);
            _shaderMaterial.SetShaderParameter("threshold_low", _thresholdLow);
        }

        // Reset mesh opacity
        if (_meshInstance != null)
        {
            _meshInstance.Transparency = 0f;
        }
    }

    #endregion

    #region Signal Handlers

    /// <summary>
    /// Called when tracked node is about to exit the tree.
    /// Detach signals and queue self for deletion.
    /// </summary>
    private void OnTrackedNodeExiting()
    {
        Detach();
        QueueFree();
    }

    private void OnHpChanged(float newHp, float maxHp)
    {
        UpdateHp(newHp, maxHp);
    }

    #endregion

    #region Visual Rendering

    private void CreateShaderVisuals()
    {
        // Load shader (cached across instances)
        if (_cachedShader == null && ResourceLoader.Exists(ShaderPath))
        {
            _cachedShader = GD.Load<Shader>(ShaderPath);
        }

        // Create quad mesh
        var quadMesh = new QuadMesh();
        quadMesh.Size = new Vector2(_barWidth, _barHeight);

        // Create mesh instance
        _meshInstance = new MeshInstance3D
        {
            Mesh = quadMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        // Try to use shader material, fallback to standard material
        if (_cachedShader != null)
        {
            _shaderMaterial = new ShaderMaterial { Shader = _cachedShader };
            _meshInstance.MaterialOverride = _shaderMaterial;

            // Set initial shader parameters
            _shaderMaterial.SetShaderParameter("hp_percent", 1f);
            _shaderMaterial.SetShaderParameter("display_percent", 1f);
            _shaderMaterial.SetShaderParameter("shield_percent", 0f);
            _shaderMaterial.SetShaderParameter("damage_flash", 0f);
            _shaderMaterial.SetShaderParameter("threshold_mid", _thresholdMid);
            _shaderMaterial.SetShaderParameter("threshold_low", _thresholdLow);
        }
        else
        {
            // Fallback: use obvious error color so missing shader is visible
            GD.PushWarning("FloatingHPBar: Shader not found at " + ShaderPath + ", using fallback material");
            var fallbackMaterial = new StandardMaterial3D
            {
                AlbedoColor = Colors.Magenta,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
                NoDepthTest = true,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled
            };
            _meshInstance.MaterialOverride = fallbackMaterial;
        }

        AddChild(_meshInstance);
    }

    private void UpdateMeshSize()
    {
        if (_meshInstance?.Mesh is QuadMesh quadMesh)
        {
            quadMesh.Size = new Vector2(_barWidth, _barHeight);
        }
    }

    private void UpdateShaderDisplayPercent()
    {
        _shaderMaterial?.SetShaderParameter("display_percent", _displayHpPercent);
    }

    private void ApplyShaderColors(HPBarSettings settings)
    {
        if (_shaderMaterial == null)
            return;

        if (settings.ColorFull.HasValue)
            _shaderMaterial.SetShaderParameter("color_full", settings.ColorFull.Value);
        if (settings.ColorMid.HasValue)
            _shaderMaterial.SetShaderParameter("color_mid", settings.ColorMid.Value);
        if (settings.ColorLow.HasValue)
            _shaderMaterial.SetShaderParameter("color_low", settings.ColorLow.Value);
        if (settings.ColorBackground.HasValue)
            _shaderMaterial.SetShaderParameter("color_background", settings.ColorBackground.Value);
    }

    #endregion

    #region Visibility

    /// <summary>
    /// Show the HP bar (renamed from Show to avoid hiding Node.Show).
    /// </summary>
    private void ShowBar()
    {
        if (_barIsVisible)
            return;

        _barIsVisible = true;
        Visible = true;

        // Cancel any fade animation
        _fadeTween?.Kill();

        if (_meshInstance != null)
        {
            _meshInstance.Transparency = 0f;
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

        if (_meshInstance != null)
        {
            _fadeTween.TweenProperty(_meshInstance, "transparency", 1f, _fadeDuration)
                .From(_meshInstance.Transparency);
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

        // Try to calculate vertical offset from visual, keep configured value as fallback
        var calculatedOffset = TryCalculateBarOffset();
        if (calculatedOffset.HasValue)
        {
            _offsetY = calculatedOffset.Value;
        }
        // If no visual calculation possible, keep the configured _offsetY

        // Cache horizontal offset
        _cachedOffsetX = 0f;
        var visual = _trackedNode.GetNodeOrNull("Visual");
        if (visual != null && visual.HasMethod("get_hp_bar_offset_x"))
        {
            _cachedOffsetX = (float)visual.Call("get_hp_bar_offset_x");
        }
    }

    /// <summary>
    /// Try to calculate bar offset from visual component.
    /// Returns null if no visual or no sprite height method (uses configured offset as fallback).
    /// </summary>
    private float? TryCalculateBarOffset()
    {
        if (_trackedNode == null)
            return null;

        var visual = _trackedNode.GetNodeOrNull("Visual");
        if (visual == null)
        {
            // Bases/summoners don't have Visual components - use configured offset
            return null;
        }

        // Query sprite height from visual component
        if (visual.HasMethod("get_sprite_height"))
        {
            var spriteHeight = (float)visual.Call("get_sprite_height");

            // Scale factor from parent
            var scaleFactor = _trackedNode.Scale.Y;
            var scaledSpriteHeight = spriteHeight * scaleFactor;

            return scaledSpriteHeight + OffsetPadding;
        }

        return null;
    }

    #endregion
}
