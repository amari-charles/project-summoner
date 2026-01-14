using Godot;

namespace ProjectSummoner.Visual;

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

    private const int BaseViewportSize = 512;
    private const float DefaultSpriteScale = 5.12f;

    // Attack animation constants
    private const float LungeDistancePx = 20.0f;
    private const float LungeForwardDuration = 0.1f;
    private const float LungeReturnDuration = 0.15f;

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
    /// Pixel offset to shift the sprite rendering position.
    /// Use for sprites where the character isn't centered in the texture.
    /// Example: If particles extend 20px to the right, set X = -10 to center the character body.
    /// Note: This only affects visual rendering. Shadow and collision remain at unit position.
    /// </summary>
    [Export]
    public Vector2 SpriteOffsetPixels { get; set; } = Vector2.Zero;

    [Export]
    public float SpriteScale { get; set; } = DefaultSpriteScale;

    [Export]
    public float ViewportScale { get; set; } = 1.0f;

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
    public AttackStyle AttackStyleSetting { get; set; } = AttackStyle.None;

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
    private float _baseSpriteY;
    private Vector2 _baseSpriteScale = Vector2.One;
    private Tween? _attackTween;
    private bool _isAttacking;
    private bool _isFlipped;

    // Frame size cache (avoid expensive texture lookups on every facing change)
    private Vector2 _cachedFrameSize = Vector2.Zero;
    private string _cachedFrameSizeAnimation = "";

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        // Get node references
        _sprite3D = GetNodeOrNull<Sprite3D>("Sprite3D");
        _viewport = GetNodeOrNull<SubViewport>("Sprite3D/SubViewport");
        _characterSprite = GetNodeOrNull<AnimatedSprite2D>("Sprite3D/SubViewport/Model2D/CharacterSprite");

        // Only hide during initialization if under a Unit3D (CharacterBody3D)
        // This prevents jitter when Unit3D._Ready sets facing after us
        // Ghost previews (under Node3D) need immediate visibility
        bool underUnit3D = GetParent() is CharacterBody3D;

        if (underUnit3D && _sprite3D != null)
        {
            _sprite3D.Visible = false;
        }

        if (_viewport != null)
        {
            // Force viewport to render every frame
            _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

            // Apply viewport scaling for large units
            if (ViewportScale != 1.0f)
            {
                int scaledSize = (int)(BaseViewportSize * ViewportScale);
                _viewport.Size = new Vector2I(scaledSize, scaledSize);
            }
        }

        if (_characterSprite != null)
        {
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
            _characterSprite.Scale = new Vector2(SpriteScale, SpriteScale);

            // Setup alignment
            SetupSpriteAlignment();

            // Store base values for animations
            _baseSpriteScale = _characterSprite.Scale;
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

        // Randomize animation frame
        RandomizeAnimationPhase();

        // Show sprite after all initialization (only needed if we hid it above)
        // Ghost previews under Node3D don't need this since we didn't hide the sprite
        if (underUnit3D)
        {
            CallDeferred(MethodName.ShowSpriteDeferred);
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
        if (_characterSprite == null || _isAttacking)
            return;

        float deltaF = (float)delta;

        // Bobbing animation
        if (EnableBobbing)
        {
            _bobTime = (_bobTime + deltaF * BobSpeed) % (Mathf.Tau * 2.0f);

            float bobOffset = -Mathf.Abs(Mathf.Sin(_bobTime)) * BobAmplitude;
            float rotationOffset = Mathf.Sin(_bobTime) * BobRotationAmplitude;

            var pos = _characterSprite.Position;
            pos.Y = _baseSpriteY + bobOffset;
            _characterSprite.Position = pos;
            _characterSprite.RotationDegrees = rotationOffset;
        }

        // Breathing animation
        if (EnableBreathing)
        {
            _breathingTime = (_breathingTime + deltaF * BreathingSpeed) % Mathf.Tau;

            float scaleFactor = 1.0f + Mathf.Sin(_breathingTime) * BreathingAmplitude;
            _characterSprite.Scale = _baseSpriteScale * scaleFactor;
        }
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

    public Vector3 GetShadowOffset()
    {
        // SpriteOffsetPixels compensates for off-center characters in the texture.
        // After compensation, the character body appears at unit center.
        // Shadow should be at unit center (under the character body).
        return Vector3.Zero;
    }

    public void FlashWhite()
    {
        if (_characterSprite == null)
            return;

        var originalColor = _characterSprite.Modulate;

        var flashTween = CreateTween();
        flashTween.TweenProperty(_characterSprite, "modulate", new Color(2.0f, 2.0f, 2.0f, 1.0f), 0.05f);
        flashTween.TweenProperty(_characterSprite, "modulate", new Color(2.0f, 2.0f, 2.0f, 1.0f), 0.1f);
        flashTween.TweenProperty(_characterSprite, "modulate", originalColor, 0.15f);
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
        spritePos.X = _viewport.Size.X / 2.0f;

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

        _sprite3D.Position = pos;

        // Apply user-defined offset (flip X when sprite is flipped)
        float offsetX = _isFlipped ? -SpriteOffsetPixels.X : SpriteOffsetPixels.X;
        spritePos.X += offsetX * _characterSprite.Scale.X;
        spritePos.Y += SpriteOffsetPixels.Y * _characterSprite.Scale.Y;

        _characterSprite.Position = spritePos;
    }

    private Vector2 GetCurrentFrameSize()
    {
        if (_characterSprite?.SpriteFrames == null)
            return Vector2.Zero;

        string anim = _characterSprite.Animation;
        if (string.IsNullOrEmpty(anim))
            anim = "idle";

        // Return cached size if animation hasn't changed
        if (anim == _cachedFrameSizeAnimation && _cachedFrameSize != Vector2.Zero)
            return _cachedFrameSize;

        if (!_characterSprite.SpriteFrames.HasAnimation(anim))
            return Vector2.Zero;

        int frameCount = _characterSprite.SpriteFrames.GetFrameCount(anim);
        if (frameCount == 0)
            return Vector2.Zero;

        var frameTexture = _characterSprite.SpriteFrames.GetFrameTexture(anim, 0);
        if (frameTexture == null)
            return Vector2.Zero;

        // Cache the result
        _cachedFrameSize = frameTexture.GetSize();
        _cachedFrameSizeAnimation = anim;
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
}
