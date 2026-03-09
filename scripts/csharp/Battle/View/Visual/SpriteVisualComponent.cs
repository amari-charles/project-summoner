using Godot;
using Fateforged.View;
namespace Fateforged.Visual;

/// <summary>
/// Sprite-based 2.5D Character Rendering Component.
/// Renders 2D sprite animations in 3D space using AnimatedSprite2D + SubViewport.
/// </summary>
[GlobalClass]
public partial class SpriteVisualComponent : Node3D, IVisualComponent
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    // Attack animation constants
    private const float LungeDistancePx = 20.0f;
    private const float LungeForwardDuration = 0.1f;
    private const float LungeReturnDuration = 0.15f;

    // Flash effect timing
    private const float FlashHoldDuration = 0.1f;
    private const float FlashFadeDuration = 0.15f;

    // =========================================================================
    // ENUMS
    // =========================================================================

    public enum AttackStyle
    {
        None,
        Lunge,
        SquashSpring,
        Spin,
        Pulse
    }

    // =========================================================================
    // EXPORTED PROPERTIES
    // =========================================================================

    [ExportGroup("Sprite Configuration")]
    [Export]
    public SpriteFrames? SpriteFramesResource { get; set; }

    [Export]
    public float FeetOffsetPixels { get; set; } = 0.0f;

    [Export]
    public float HeadOffsetPixels { get; set; } = 0.0f;

    /// <summary>
    /// Optional per-animation pivot offsets.
    /// Key: animation name (e.g., "idle", "walk", "attack", "death").
    /// Value: pixel offset relative to texture center.
    /// </summary>
    [Export]
    public Godot.Collections.Dictionary<StringName, Vector2> AnimationPivotOffsets { get; set; } = new();

    /// <summary>
    /// Viewport size in pixels. Controls the rendering area.
    /// Increase for larger sprites to prevent clipping.
    /// </summary>
    [Export]
    public Vector2I ViewportSize { get; set; } = new Vector2I(512, 512);

    /// <summary>
    /// Scale factor applied to the sprite inside the viewport.
    /// Controls how large the unit appears in world space.
    /// </summary>
    [Export]
    public Vector2 ScaleFactor { get; set; } = new Vector2(5.12f, 5.12f);

    [Export]
    public float HpBarOffsetX { get; set; } = 0.0f;

    [ExportGroup("Shadow")]
    [Export]
    public ShadowProfilePreset ShadowPreset { get; set; } = ShadowProfilePreset.Default;

    [Export]
    public ShadowProfileResource? ShadowProfileOverride { get; set; }

    [ExportGroup("Animation Effects")]
    [Export]
    public bool EnableBobbing { get; set; } = false;

    [Export]
    public float BobSpeed { get; set; } = 8.0f;

    [Export]
    public float BobAmplitude { get; set; } = 6.0f;

    [Export]
    public float BobRotationAmplitude { get; set; } = 4.0f;

    [Export]
    public bool EnableBreathing { get; set; } = false;

    [Export]
    public float BreathingSpeed { get; set; } = 1.5f;

    [Export]
    public float BreathingAmplitude { get; set; } = 0.08f;

    [Export]
    public bool EnableProceduralWalk { get; set; } = true;

    [Export]
    public float WalkCycleSpeed { get; set; } = 10.0f;

    [Export]
    public float WalkBobAmplitude { get; set; } = 2.0f;

    [Export]
    public float WalkTiltAmplitude { get; set; } = 3.0f;

    [Export]
    public float WalkScaleAmplitude { get; set; } = 0.025f;

    [Export]
    public AttackStyle AttackStyleSetting { get; set; } = AttackStyle.None;

    /// <summary>
    /// Color to flash when taking damage. Default is bright HDR white (3, 3, 3, 1).
    ///
    /// Most units should use the default white flash, which provides good contrast
    /// against darker sprites. Override this for light-colored or white units where
    /// a white flash wouldn't be visible.
    ///
    /// Example: Puff is a white cloud, so it uses a soft pink flash (1.3, 0.85, 0.85, 1)
    /// to ensure hit feedback is visible against its light-colored sprite.
    /// </summary>
    [Export]
    public Color FlashColor { get; set; } = new Color(3.0f, 3.0f, 3.0f, 1.0f);

    // =========================================================================
    // SHADOW
    // =========================================================================

    private Sprite3D? _shadowSprite3D;

    // =========================================================================
    // NODE REFERENCES
    // =========================================================================

    private Sprite3D? _sprite3D;
    private SubViewport? _viewport;
    private AnimatedSprite2D? _characterSprite;

    // =========================================================================
    // ANIMATION STATE
    // =========================================================================

    private float _bobTime;
    private float _breathingTime;
    private float _walkTime;
    private float _baseSpriteY;
    private Vector2 _baseSpriteScale = Vector2.One;
    private Tween? _attackTween;
    private bool _isAttacking;
    private bool _isWalkCycleActive;
    private bool _isFlipped;
    private ShadowProfile _shadowProfile = ShadowProfiles.FromPreset(ShadowProfilePreset.Default).Sanitize();
    private Color _originalModulate = Colors.White;
    private Tween? _flashTween;

    // Frame size cache (avoid expensive texture lookups on every facing change)
    private Vector2 _cachedFrameSize = Vector2.Zero;
    private string _cachedFrameSizeAnimation = "";
    private int _cachedFrameSizeFrame = -1;
    private string _lastAlignedAnimation = "";
    private int _lastAlignedFrame = -1;
    private Vector2 _lastAlignedScale = Vector2.Zero;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        // Get node references
        _sprite3D = GetNodeOrNull<Sprite3D>("Sprite3D");
        _viewport = GetNodeOrNull<SubViewport>("Sprite3D/SubViewport");
        _characterSprite = GetNodeOrNull<AnimatedSprite2D>("Sprite3D/SubViewport/Model2D/CharacterSprite");
        _shadowProfile = ResolveShadowProfile();

        // Only hide/create runtime-only visuals when under UnitVisual.
        // Walk ancestry so wrapper nodes above Visual don't accidentally disable shadows.
        bool underUnitVisual = IsUnderUnitVisual();

        if (underUnitVisual && _sprite3D != null)
        {
            _sprite3D.Visible = false;
        }

        if (_viewport != null)
        {
            // Force viewport to render every frame
            _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

            // Apply viewport size
            _viewport.Size = ViewportSize;
        }

        if (_characterSprite != null)
        {
            _characterSprite.AnimationFinished += OnCharacterAnimationFinished;

            // Apply SpriteFrames if set via export
            if (SpriteFramesResource != null)
            {
                _characterSprite.SpriteFrames = SpriteFramesResource;
                if (_characterSprite.SpriteFrames.HasAnimation("idle"))
                {
                    _characterSprite.Animation = "idle";
                    _characterSprite.Play();
                }
            }

            // Apply sprite scale
            _characterSprite.Scale = ScaleFactor;

            // Setup alignment
            SetupSpriteAlignment();

            // Store base values for animations
            _baseSpriteScale = ScaleFactor;
            _baseSpriteY = _characterSprite.Position.Y;
        }

        // Setup bobbing animation with random phase
        if (EnableBobbing)
        {
            _bobTime = GD.Randf() * Mathf.Tau;
        }

        // Setup breathing animation with random phase
        if (EnableBreathing)
        {
            _breathingTime = GD.Randf() * Mathf.Tau;
        }

        if (EnableProceduralWalk)
        {
            _walkTime = GD.Randf() * Mathf.Tau;
        }

        // Randomize animation frame
        RandomizeAnimationPhase();

        // Create silhouette shadow (only for real units, not ghost previews)
        if (underUnitVisual)
        {
            CreateShadowSprite();
        }

        // Show sprite after all initialization (only needed if we hid it above)
        // Ghost previews under Node3D don't need this since we didn't hide the sprite
        if (underUnitVisual)
        {
            CallDeferred(MethodName.ShowSpriteDeferred);
        }
    }

    public override void _ExitTree()
    {
        if (_characterSprite != null)
        {
            _characterSprite.AnimationFinished -= OnCharacterAnimationFinished;
        }
    }

    /// <summary>
    /// Show sprite after deferred initialization completes.
    /// </summary>
    private void ShowSpriteDeferred()
    {
        if (_sprite3D != null)
        {
            _sprite3D.Visible = true;
        }
    }

    public override void _Process(double delta)
    {
        // Pin shadow to ground regardless of unit elevation (flying units)
        if (_shadowSprite3D != null)
        {
            ShadowHelper.PinToGround(_shadowSprite3D, GlobalPosition.Y, _shadowProfile);
        }

        if (_characterSprite == null || _isAttacking)
            return;

        RefreshAlignmentIfFrameOrAnimationChanged();

        float deltaF = (float)delta;
        float targetY = _baseSpriteY;
        float targetRotation = 0.0f;
        Vector2 targetScale = _baseSpriteScale;

        // Bobbing animation
        if (EnableBobbing)
        {
            _bobTime = (_bobTime + deltaF * BobSpeed) % (Mathf.Tau * 2.0f);

            float bobOffset = -Mathf.Abs(Mathf.Sin(_bobTime)) * BobAmplitude;
            float rotationOffset = Mathf.Sin(_bobTime) * BobRotationAmplitude;

            targetY += bobOffset;
            targetRotation += rotationOffset;
        }

        // Breathing animation
        if (EnableBreathing)
        {
            _breathingTime = (_breathingTime + deltaF * BreathingSpeed) % Mathf.Tau;

            float scaleFactor = 1.0f + Mathf.Sin(_breathingTime) * BreathingAmplitude;
            targetScale *= scaleFactor;
        }

        // Procedural walk cycle for units using "walk" state (including fallback to idle frames).
        if (EnableProceduralWalk && _isWalkCycleActive)
        {
            _walkTime = (_walkTime + deltaF * WalkCycleSpeed) % (Mathf.Tau * 2.0f);

            float walkWave = Mathf.Sin(_walkTime);
            float walkScale = 1.0f + Mathf.Sin(_walkTime * 2.0f) * WalkScaleAmplitude;

            targetY += -Mathf.Abs(walkWave) * WalkBobAmplitude;
            targetRotation += walkWave * WalkTiltAmplitude;
            targetScale *= walkScale;
        }

        var pos = _characterSprite.Position;
        pos.Y = targetY;
        _characterSprite.Position = pos;
        _characterSprite.RotationDegrees = targetRotation;
        _characterSprite.Scale = targetScale;
    }

    // =========================================================================
    // IVisualComponent IMPLEMENTATION
    // =========================================================================

    /// <summary>
    /// Play animation without auto-play (GDScript compatibility overload).
    /// </summary>
    public void PlayAnimation(string animName)
    {
        PlayAnimation(animName, false);
    }

    public void PlayAnimation(string animName, bool autoPlay)
    {
        if (_characterSprite?.SpriteFrames == null)
            return;

        bool wantsWalkCycle = animName == "walk";
        _isWalkCycleActive = EnableProceduralWalk && wantsWalkCycle;

        if (_characterSprite.SpriteFrames.HasAnimation(animName))
        {
            _characterSprite.Animation = animName;
            _characterSprite.Play();

            // Trigger attack effect for single-frame sprites
            if (animName == "attack" && AttackStyleSetting != AttackStyle.None)
            {
                PlayAttackEffect();
            }
        }
        else
        {
            GD.PushWarning($"Animation '{animName}' not found, falling back to 'idle'");
            if (_characterSprite.SpriteFrames.HasAnimation("idle"))
            {
                _characterSprite.Animation = "idle";
                _characterSprite.Play();
            }
        }
    }

    public void StopAnimation()
    {
        _characterSprite?.Stop();
    }

    public string GetCurrentAnimation()
    {
        return _characterSprite?.Animation ?? "";
    }

    public bool IsPlaying()
    {
        return _characterSprite?.IsPlaying() ?? false;
    }

    public void SetAnimationSpeed(float speed)
    {
        if (_characterSprite != null)
        {
            _characterSprite.SpeedScale = speed;
        }
    }

    public float GetAnimationDuration(string animName)
    {
        if (_characterSprite?.SpriteFrames == null)
            return 1.0f;

        if (_characterSprite.SpriteFrames.HasAnimation(animName))
        {
            int frameCount = _characterSprite.SpriteFrames.GetFrameCount(animName);
            float fps = (float)_characterSprite.SpriteFrames.GetAnimationSpeed(animName);
            if (fps > 0)
            {
                return frameCount / fps;
            }
        }

        return 1.0f;
    }

    public float GetSpriteHeight()
    {
        if (_viewport == null || _sprite3D == null)
            return 1.0f;

        var textureSize = GetCurrentFrameSize();

        if (textureSize.Y > 0)
        {
            return (textureSize.Y - FeetOffsetPixels - HeadOffsetPixels) * _sprite3D.PixelSize;
        }

        return _viewport.Size.Y * _sprite3D.PixelSize;
    }

    public float GetSpriteWidth()
    {
        if (_viewport == null || _sprite3D == null || _characterSprite == null)
            return 1.0f;

        var textureSize = GetCurrentFrameSize();
        if (textureSize.X > 0)
        {
            return textureSize.X * _characterSprite.Scale.X * _sprite3D.PixelSize;
        }

        return _viewport.Size.X * _sprite3D.PixelSize;
    }

    public float GetHpBarOffsetX()
    {
        return HpBarOffsetX;
    }

    public void FlashWhite()
    {
        if (_characterSprite == null)
            return;

        // Kill any existing flash tween to prevent overlapping flashes
        if (_flashTween != null && _flashTween.IsValid())
        {
            _flashTween.Kill();
            _characterSprite.Modulate = _originalModulate;
        }

        // Flash using direct set (tweens had issues with initial flash)
        // Uses configurable FlashColor (default: HDR white, override for light units)
        _characterSprite.Modulate = FlashColor;

        // Hold for a moment, then fade back
        var holdTimer = GetTree().CreateTimer(FlashHoldDuration);
        holdTimer.Timeout += () =>
        {
            if (!IsInstanceValid(this) || _characterSprite == null)
                return;

            // Create fade-back tween
            _flashTween = CreateTween();
            _flashTween.TweenProperty(_characterSprite, "modulate", _originalModulate, FlashFadeDuration);
        };
    }

    public void SetFlipH(bool flip)
    {
        _isFlipped = flip;
        if (_characterSprite != null)
        {
            _characterSprite.FlipH = flip;
            // Re-apply alignment with correct offset direction
            SetupSpriteAlignment();
        }
        // Shadow doesn't need flip updates — the viewport texture already
        // contains the flipped content, so the shadow silhouette updates automatically.
    }

    public void SetRenderPriority(int priority)
    {
        if (_sprite3D != null)
        {
            _sprite3D.RenderPriority = priority;
        }
    }

    public bool IsFullyInitialized()
    {
        return true; // Sprite components are immediately ready
    }

    public Node3D CreateGhostVisual()
    {
        // Create a copy of this component for ghost preview
        var ghost = new Node3D();

        // For now, create a simple placeholder
        // Full implementation would duplicate the sprite structure
        var mesh = new MeshInstance3D();
        var cylinder = new CylinderMesh();
        cylinder.TopRadius = 0.3f;
        cylinder.BottomRadius = 0.3f;
        cylinder.Height = GetSpriteHeight();
        mesh.Mesh = cylinder;
        mesh.Position = new Vector3(0, cylinder.Height / 2, 0);

        ghost.AddChild(mesh);
        return ghost;
    }

    public void ApplyGhostTint(Color tint)
    {
        // Store the tint as original so FlashWhite returns to it
        _originalModulate = tint;

        // Apply tint to the internal sprite for ghost transparency
        if (_characterSprite != null)
        {
            _characterSprite.Modulate = tint;
        }
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Set the SpriteFrames resource for this character.
    /// </summary>
    public void SetSpriteFrames(SpriteFrames frames)
    {
        if (_characterSprite != null)
        {
            _characterSprite.SpriteFrames = frames;
            SetupSpriteAlignment();

            if (EnableBobbing)
            {
                _baseSpriteY = _characterSprite.Position.Y;
            }
        }
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private void CreateShadowSprite()
    {
        if (_sprite3D == null || _viewport == null)
            return;

        _shadowSprite3D = ShadowHelper.CreateShadow(_sprite3D, _viewport, _shadowProfile);
        if (_shadowSprite3D == null)
            return;

        AddChild(_shadowSprite3D);
    }

    private ShadowProfile ResolveShadowProfile()
    {
        if (ShadowProfileOverride != null)
            return ShadowProfileOverride.ToShadowProfile();

        return ShadowProfiles.FromPreset(ShadowPreset).Sanitize();
    }

    private bool IsUnderUnitVisual()
    {
        Node? current = GetParent();
        while (current != null)
        {
            if (current is UnitVisual)
                return true;
            current = current.GetParent();
        }

        return false;
    }

    private void SetupSpriteAlignment()
    {
        if (_sprite3D == null || _viewport == null || _characterSprite == null)
            return;

        // Calculate world height
        float worldHeight = _viewport.Size.Y * _sprite3D.PixelSize;

        // Get texture size for positioning calculations
        var textureSize = GetCurrentFrameSize();

        // Center sprite horizontally in viewport
        var spritePos = _characterSprite.Position;

        // Position Sprite3D and CharacterSprite so:
        // 1. Entire sprite fits in viewport (no clipping)
        // 2. Feet are at world Y=0

        var pos = _sprite3D.Position;

        if (textureSize.Y > 0)
        {
            // Position sprite so its BOTTOM is at viewport bottom (prevents clipping)
            spritePos.Y = _viewport.Size.Y - (textureSize.Y / 2.0f) * _characterSprite.Scale.Y;

            // Feet are FeetOffsetPixels above sprite bottom
            // Shift Sprite3D down so feet align with world Y=0
            float feetWorldOffset = FeetOffsetPixels * _characterSprite.Scale.Y * _sprite3D.PixelSize;
            pos.Y = worldHeight / 2.0f - feetWorldOffset;
        }
        else
        {
            spritePos.Y = _viewport.Size.Y * 0.8f;
            pos.Y = worldHeight / 2.0f;
        }

        // Apply X offset within viewport (keeps world-space unit/shadow center stable).
        // Offsets are mirrored on flip so off-center art pivots in place.
        Vector2 spriteOffset = ResolveSpriteOffset();
        float offsetX = spriteOffset.X;
        float signedOffsetX = _isFlipped ? -offsetX : offsetX;
        spritePos.X = (_viewport.Size.X / 2.0f) + (signedOffsetX * _characterSprite.Scale.X);

        // Keep world-space X anchor at unit origin.
        pos.X = 0.0f;
        _sprite3D.Position = pos;

        // Apply Y offset within viewport (Y offset doesn't need world-space movement)
        spritePos.Y += spriteOffset.Y * _characterSprite.Scale.Y;

        _characterSprite.Position = spritePos;
        _baseSpriteY = spritePos.Y;
        _lastAlignedAnimation = _characterSprite.Animation;
        _lastAlignedFrame = _characterSprite.Frame;
        _lastAlignedScale = _characterSprite.Scale;
    }

    private void RefreshAlignmentIfFrameOrAnimationChanged()
    {
        if (_characterSprite == null)
            return;

        string anim = _characterSprite.Animation;
        int frame = _characterSprite.Frame;
        if (anim == _lastAlignedAnimation
            && frame == _lastAlignedFrame
            && _characterSprite.Scale == _lastAlignedScale)
            return;

        SetupSpriteAlignment();
    }

    private Vector2 ResolveSpriteOffset()
    {
        if (_characterSprite == null)
            return Vector2.Zero;

        StringName anim = _characterSprite.Animation;
        if (!anim.IsEmpty && AnimationPivotOffsets.TryGetValue(anim, out Vector2 animOffset))
            return animOffset;

        return Vector2.Zero;
    }

    private Vector2 GetCurrentFrameSize()
    {
        if (_characterSprite?.SpriteFrames == null)
            return Vector2.Zero;

        string anim = _characterSprite.Animation;
        if (string.IsNullOrEmpty(anim))
            anim = "idle";

        if (!_characterSprite.SpriteFrames.HasAnimation(anim))
            return Vector2.Zero;

        int frameCount = _characterSprite.SpriteFrames.GetFrameCount(anim);
        if (frameCount == 0)
            return Vector2.Zero;

        int frame = Mathf.Clamp(_characterSprite.Frame, 0, frameCount - 1);

        // Return cached size if animation/frame hasn't changed
        if (anim == _cachedFrameSizeAnimation && frame == _cachedFrameSizeFrame && _cachedFrameSize != Vector2.Zero)
            return _cachedFrameSize;

        var frameTexture = _characterSprite.SpriteFrames.GetFrameTexture(anim, frame);
        if (frameTexture == null)
            return Vector2.Zero;

        // Cache the result
        _cachedFrameSize = frameTexture.GetSize();
        _cachedFrameSizeAnimation = anim;
        _cachedFrameSizeFrame = frame;
        return _cachedFrameSize;
    }

    private void RandomizeAnimationPhase()
    {
        if (_characterSprite?.SpriteFrames == null)
            return;

        string anim = _characterSprite.Animation;
        if (string.IsNullOrEmpty(anim))
            anim = "idle";

        if (!_characterSprite.SpriteFrames.HasAnimation(anim))
            return;

        int frameCount = _characterSprite.SpriteFrames.GetFrameCount(anim);
        if (frameCount > 1)
        {
            _characterSprite.Frame = GD.RandRange(0, frameCount - 1);
        }
    }

    private void PlayAttackEffect()
    {
        _attackTween?.Kill();
        _isAttacking = true;
        ResetProceduralTransforms();

        switch (AttackStyleSetting)
        {
            case AttackStyle.Lunge:
                AttackLunge();
                break;
            case AttackStyle.SquashSpring:
            case AttackStyle.Spin:
            case AttackStyle.Pulse:
                // Simplified: use lunge for now, can expand later
                AttackLunge();
                break;
        }
    }

    private void AttackLunge()
    {
        if (_characterSprite == null)
            return;

        float baseX = _characterSprite.Position.X;
        float direction = _characterSprite.FlipH ? -1.0f : 1.0f;

        _attackTween = CreateTween();
        _attackTween.TweenProperty(_characterSprite, "position:x",
            baseX + (LungeDistancePx * direction), LungeForwardDuration)
            .SetEase(Tween.EaseType.Out);
        _attackTween.TweenProperty(_characterSprite, "position:x",
            baseX, LungeReturnDuration)
            .SetEase(Tween.EaseType.In);
        _attackTween.TweenCallback(Callable.From(OnAttackFinished));
    }

    private void OnAttackFinished()
    {
        if (!IsInstanceValid(this))
            return;
        _isAttacking = false;
    }

    private void ResetProceduralTransforms()
    {
        if (_characterSprite == null)
            return;

        var pos = _characterSprite.Position;
        pos.Y = _baseSpriteY;
        _characterSprite.Position = pos;
        _characterSprite.RotationDegrees = 0.0f;
        _characterSprite.Scale = _baseSpriteScale;
    }

    private void OnCharacterAnimationFinished()
    {
        if (_characterSprite?.SpriteFrames == null)
            return;

        if (_characterSprite.Animation == "walk" && _characterSprite.SpriteFrames.HasAnimation("walk_loop"))
        {
            _characterSprite.Animation = "walk_loop";
            _characterSprite.Play();
        }
    }
}
