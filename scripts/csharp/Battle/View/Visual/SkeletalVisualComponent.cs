using System.Collections.Generic;
using Fateforged.View;
using Godot;

namespace Fateforged.Visual;

/// <summary>
/// Skeletal-based 2.5D Character Rendering Component.
/// Renders skeletal 2D animations in 3D space using Node2D pivots/AnimationPlayer + SubViewport.
/// Viewport size is manually specified per-unit for explicit control.
/// </summary>
[GlobalClass]
public partial class SkeletalVisualComponent : Node3D, IVisualComponent
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>World units per viewport pixel for Sprite3D rendering.</summary>
    private const float SpritePixelSize = 0.01f;

    // Flash effect timing
    private const float FlashHoldDuration = 0.1f;
    private const float FlashFadeDuration = 0.15f;

    // =========================================================================
    // EXPORTED PROPERTIES
    // =========================================================================

    [Export]
    public PackedScene? SkeletalScene { get; set; }

    /// <summary>
    /// Scale factor applied to the rig inside the viewport.
    /// Controls how large the unit appears in world space.
    /// </summary>
    [Export]
    public Vector2 ScaleFactor { get; set; } = new Vector2(0.1f, 0.1f);

    public void SetScaleFactor(Vector2 scaleFactor)
    {
        ScaleFactor = scaleFactor;
        ApplyScaleFactorToRig();
    }

    /// <summary>
    /// Viewport size in pixels. Set this to fit your unit's content.
    /// If zero, uses the default size from the component scene (1200x1200).
    /// </summary>
    [Export]
    public Vector2I ViewportSize { get; set; } = Vector2I.Zero;

    /// <summary>
    /// Approximate size of the rig content in pixels (before scaling).
    /// Used for shadow auto-sizing and other dimension calculations.
    /// If zero, falls back to viewport size (less accurate).
    /// </summary>
    [Export]
    public Vector2 ContentSize { get; set; } = Vector2.Zero;

    /// <summary>
    /// Position of the feet in rig local space (before scaling).
    /// This defines where the unit's ground contact point is relative to the rig's origin.
    ///
    /// The X component determines horizontal centering - the system positions the rig so that
    /// FeetLocalPosition.X is at the viewport center. This ensures the visual content is
    /// centered and flipping works correctly in both directions.
    ///
    /// The Y component determines vertical grounding - the system calculates sprite positioning
    /// to place feet at world Y=0.
    ///
    /// Example: If your rig has its origin at top-left and feet are at (300, 800),
    /// set FeetLocalPosition = (300, 800).
    /// </summary>
    [ExportGroup("Sprite Configuration")]
    [Export]
    public Vector2 FeetLocalPosition { get; set; } = Vector2.Zero;

    [ExportGroup("")]
    [Export]
    public float HpBarOffsetX { get; set; } = 0.0f;

    [ExportGroup("Shadow")]
    [Export]
    public ShadowProfilePreset ShadowPreset { get; set; } = ShadowProfilePreset.Default;

    [Export]
    public ShadowProfileResource? ShadowProfileOverride { get; set; }

    [ExportGroup("")]
    /// <summary>
    /// Color to flash when taking damage. Default is bright HDR white (3, 3, 3, 1).
    ///
    /// Most units should use the default white flash, which provides good contrast
    /// against darker sprites. Override this for light-colored or white units where
    /// a white flash wouldn't be visible.
    /// </summary>
    [Export]
    public Color FlashColor { get; set; } = new Color(3.0f, 3.0f, 3.0f, 1.0f);

    [ExportGroup("Damage Feedback")]
    [Export]
    public float FlashMinIntervalSeconds { get; set; } = 0.08f;

    [Export]
    public float FlashMinIntervalLargeUnitSeconds { get; set; } = 0.18f;

    [Export]
    public float FlashLargeUnitWidthThreshold { get; set; } = 2.4f;

    // =========================================================================
    // SHADOW
    // =========================================================================

    private Sprite3D? _shadowSprite3D;

    // =========================================================================
    // NODE REFERENCES
    // =========================================================================

    private Sprite3D? _sprite3D;
    private SubViewport? _viewport;
    private Node2D? _modelContainer;
    private AnimationPlayer? _animationPlayer;
    private Node2D? _skeletalInstance;

    // =========================================================================
    // STATE
    // =========================================================================

    private bool _isFlipped;
    private bool _initializationComplete;
    private bool _underUnitVisual;
    private ShadowProfile _shadowProfile = ShadowProfiles
        .FromPreset(ShadowProfilePreset.Default)
        .Sanitize();
    private Color _originalModulate = Colors.White;
    private Tween? _flashTween;
    private List<CanvasItem>? _cachedSprites;
    private ulong _lastFlashTimestampMs;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        _sprite3D = GetNodeOrNull<Sprite3D>("Sprite3D");
        _viewport = GetNodeOrNull<SubViewport>("Sprite3D/SubViewport");
        _modelContainer = GetNodeOrNull<Node2D>("Sprite3D/SubViewport/ModelContainer");
        _underUnitVisual = IsUnderUnitVisual();
        _shadowProfile = ResolveShadowProfile();

        if (_viewport != null)
        {
            // WhenVisible: only render when viewport is visible AND content changed
            _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible;

            // Apply custom viewport size if specified
            if (ViewportSize != Vector2I.Zero)
            {
                _viewport.Size = ViewportSize;
            }
        }

        // Instance skeletal scene if provided
        if (SkeletalScene != null)
        {
            // Hide during initialization to prevent jitter
            if (_sprite3D != null)
            {
                _sprite3D.Visible = false;
            }
            CallDeferred(MethodName.InstanceSkeletalSceneDeferred);
        }
    }

    public override void _Process(double delta)
    {
        // Pin shadow to ground regardless of unit elevation (flying units)
        if (_shadowSprite3D != null)
        {
            ShadowHelper.PinToGround(_shadowSprite3D, GlobalPosition.Y, _shadowProfile);
        }
    }

    private async void InstanceSkeletalSceneDeferred()
    {
        await InstanceSkeletalScene();

        // Guard: component may be freed or removed from tree after await
        if (!IsInstanceValid(this) || !IsInsideTree())
            return;

        SetupSpriteAlignment();
        RandomizeAnimationPhase();
        if (_underUnitVisual)
        {
            CreateShadowSprite();
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
        if (_animationPlayer == null)
            return;

        // Map animation names for missing animations
        string mappedName = animName switch
        {
            "hurt" => "idle",
            "death" => "idle",
            _ => animName,
        };

        if (_animationPlayer.HasAnimation(mappedName))
        {
            _animationPlayer.Play(mappedName);
        }
        else
        {
            GD.PushWarning($"SkeletalVisual: Animation '{mappedName}' not found");
        }
    }

    public void StopAnimation()
    {
        _animationPlayer?.Stop();
    }

    public string GetCurrentAnimation()
    {
        return _animationPlayer?.CurrentAnimation ?? "";
    }

    public bool IsPlaying()
    {
        return _animationPlayer?.IsPlaying() ?? false;
    }

    public void SetAnimationSpeed(float speed)
    {
        if (_animationPlayer != null)
        {
            _animationPlayer.SpeedScale = speed;
        }
    }

    public float GetAnimationDuration(string animName)
    {
        if (_animationPlayer == null)
            return 1.0f;

        string mappedName = animName switch
        {
            "walk" => "idle",
            "hurt" => "idle",
            "death" => "idle",
            _ => animName,
        };

        if (_animationPlayer.HasAnimation(mappedName))
        {
            var animation = _animationPlayer.GetAnimation(mappedName);
            return (float)animation.Length;
        }

        return 1.0f;
    }

    public float GetSpriteHeight()
    {
        if (_viewport == null || _sprite3D == null)
            return 1.0f;

        // Use ContentSize if specified, otherwise viewport size
        float contentHeight = ContentSize.Y > 0 ? ContentSize.Y : _viewport.Size.Y;
        return contentHeight * ScaleFactor.Y * _sprite3D.PixelSize;
    }

    public float GetSpriteWidth()
    {
        if (_viewport == null || _sprite3D == null)
            return 1.0f;

        // Use ContentSize if specified, otherwise viewport size
        float contentWidth = ContentSize.X > 0 ? ContentSize.X : _viewport.Size.X;
        return contentWidth * ScaleFactor.X * _sprite3D.PixelSize;
    }

    public float GetHpBarOffsetX()
    {
        return HpBarOffsetX;
    }

    public void FlashWhite()
    {
        if (_skeletalInstance == null)
            return;
        if (IsFlashRateLimited())
            return;

        var sprites = GetAllSprites();
        if (sprites.Count == 0)
            return;

        // Kill any existing flash tween to prevent overlapping flashes
        if (_flashTween != null && _flashTween.IsValid())
        {
            _flashTween.Kill();
            // Reset all sprites to original color
            foreach (var sprite in sprites)
            {
                sprite.Modulate = _originalModulate;
            }
        }

        // Flash to configured color then fade back
        // Using direct set + SceneTreeTimer since tweens had issues
        foreach (var sprite in sprites)
        {
            sprite.Modulate = FlashColor;
        }

        // Hold for a moment, then fade back
        var holdTimer = GetTree().CreateTimer(FlashHoldDuration);
        holdTimer.Timeout += () =>
        {
            if (!IsInstanceValid(this))
                return;

            // Re-fetch sprites in case any were freed during hold
            var currentSprites = GetAllSprites();

            // Create fade-back tween
            _flashTween = CreateTween();
            _flashTween.SetParallel(true);
            foreach (var sprite in currentSprites)
            {
                if (IsInstanceValid(sprite))
                {
                    _flashTween.TweenProperty(
                        sprite,
                        "modulate",
                        _originalModulate,
                        FlashFadeDuration
                    );
                }
            }
        };
    }

    private bool IsFlashRateLimited()
    {
        ulong nowMs = Time.GetTicksMsec();
        float minInterval = ResolveFlashIntervalSeconds();

        if (_lastFlashTimestampMs != 0)
        {
            ulong elapsedMs = nowMs - _lastFlashTimestampMs;
            if (elapsedMs < (ulong)(Mathf.Max(0f, minInterval) * 1000f))
                return true;
        }

        _lastFlashTimestampMs = nowMs;
        return false;
    }

    private float ResolveFlashIntervalSeconds()
    {
        float spriteWidth = GetSpriteWidth();
        if (spriteWidth >= FlashLargeUnitWidthThreshold)
            return FlashMinIntervalLargeUnitSeconds;
        return FlashMinIntervalSeconds;
    }

    public void SetFlipH(bool flip)
    {
        _isFlipped = flip;

        // Shadow doesn't need flip updates — the viewport texture already
        // contains the flipped content, so the shadow silhouette updates automatically.

        if (_skeletalInstance == null || !_initializationComplete || _viewport == null)
            return;

        ApplyScaleFactorToRig();
    }

    public void SetRenderPriority(int priority)
    {
        if (_sprite3D != null)
        {
            _sprite3D.RenderPriority = priority;
        }

        if (_shadowSprite3D != null)
        {
            _shadowSprite3D.RenderPriority = ResolveShadowRenderPriority(priority);
        }
    }

    public bool IsFullyInitialized()
    {
        return _initializationComplete;
    }

    public Node3D CreateGhostVisual()
    {
        var ghost = new Node3D();

        // Create a simple placeholder for ghost preview
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

        // Apply tint to each sprite individually to avoid SubViewport z_index rendering bug
        foreach (var sprite in GetAllSprites())
        {
            sprite.Modulate = tint;
        }
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Get all CanvasItem children (Sprite2D, etc.) from the skeletal instance.
    /// Results are cached after first call for performance.
    /// </summary>
    private List<CanvasItem> GetAllSprites()
    {
        if (_cachedSprites != null)
            return _cachedSprites;

        _cachedSprites = new List<CanvasItem>();
        if (_skeletalInstance != null)
        {
            CollectCanvasItems(_skeletalInstance, _cachedSprites);
        }
        return _cachedSprites;
    }

    private void CollectCanvasItems(Node node, List<CanvasItem> items)
    {
        // Collect Sprite2D and other CanvasItem types that can be modulated
        if (node is Sprite2D sprite)
        {
            items.Add(sprite);
        }

        foreach (var child in node.GetChildren())
        {
            CollectCanvasItems(child, items);
        }
    }

    private async System.Threading.Tasks.Task InstanceSkeletalScene()
    {
        if (SkeletalScene == null || _modelContainer == null || _viewport == null)
            return;

        _skeletalInstance = SkeletalScene.Instantiate<Node2D>();
        if (_skeletalInstance == null)
        {
            GD.PushError("SkeletalVisual: Failed to instance skeletal scene");
            return;
        }

        _modelContainer.AddChild(_skeletalInstance);

        // Find AnimationPlayer
        _animationPlayer = FindAnimationPlayer(_skeletalInstance);
        if (_animationPlayer == null)
        {
            GD.PushWarning("SkeletalVisual: No AnimationPlayer found");
        }

        // Connect attack_impact signal if present
        if (_skeletalInstance.HasSignal("attack_impact"))
        {
            _skeletalInstance.Connect("attack_impact", Callable.From(OnAttackImpact));
        }

        // Wait for tree update
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Guard: component or skeletal instance may be freed after await
        if (
            !IsInstanceValid(this)
            || !IsInsideTree()
            || _skeletalInstance == null
            || !IsInstanceValid(_skeletalInstance)
        )
            return;

        ApplyScaleFactorToRig();

        // Mark initialization complete and show sprite
        _initializationComplete = true;
        if (_sprite3D != null)
        {
            _sprite3D.Visible = true;
        }

        // Start idle animation
        if (_animationPlayer?.HasAnimation("idle") == true)
        {
            _animationPlayer.Play("idle");
        }
    }

    private void ApplyScaleFactorToRig()
    {
        if (_skeletalInstance == null || !IsInstanceValid(_skeletalInstance) || _viewport == null)
            return;

        _skeletalInstance.Scale = new Vector2(
            ScaleFactor.X * (_isFlipped ? -1f : 1f),
            ScaleFactor.Y
        );

        // Position the rig so content center (at FeetLocalPosition.X) is at viewport center.
        float contentCenterX = FeetLocalPosition.X * ScaleFactor.X;
        var viewportCenter = new Vector2(_viewport.Size.X / 2.0f, _viewport.Size.Y / 2.0f);
        float x = _isFlipped ? viewportCenter.X + contentCenterX : viewportCenter.X - contentCenterX;
        _skeletalInstance.Position = new Vector2(x, viewportCenter.Y);
    }

    private AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player)
            return player;

        foreach (var child in node.GetChildren())
        {
            var result = FindAnimationPlayer(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private void CreateShadowSprite()
    {
        if (_sprite3D == null || _viewport == null)
            return;

        _shadowSprite3D = ShadowHelper.CreateShadow(_sprite3D, _viewport, _shadowProfile);
        if (_shadowSprite3D == null)
            return;

        AddChild(_shadowSprite3D);
        _shadowSprite3D.RenderPriority = ResolveShadowRenderPriority(_sprite3D.RenderPriority);
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

    private int ResolveShadowRenderPriority(int bodyRenderPriority)
    {
        int behindBody = bodyRenderPriority - 1;
        int baseline = _shadowProfile.RenderPriority;
        return Mathf.Clamp(Mathf.Min(behindBody, baseline), -128, 127);
    }

    private void SetupSpriteAlignment()
    {
        if (_sprite3D == null || _viewport == null)
            return;

        // Set consistent pixel size FIRST (fixes bug where calculations used old value)
        _sprite3D.PixelSize = SpritePixelSize;

        // Calculate where the feet are in viewport space
        // Rig is positioned so that content center (FeetLocalPosition.X) is at viewport center X
        // Rig Y is at viewport center Y
        float contentCenterX = FeetLocalPosition.X * ScaleFactor.X;
        var viewportCenter = new Vector2(_viewport.Size.X / 2.0f, _viewport.Size.Y / 2.0f);
        var rigPosition = new Vector2(viewportCenter.X - contentCenterX, viewportCenter.Y);

        // Feet are at rig position + FeetLocalPosition * ScaleFactor
        var feetViewportPosition = rigPosition + FeetLocalPosition * ScaleFactor;

        // Calculate feet offset from viewport bottom
        float feetOffsetPixels = _viewport.Size.Y - feetViewportPosition.Y;

        // Convert to world units
        float worldHeight = _viewport.Size.Y * SpritePixelSize;
        float feetOffsetWorld = feetOffsetPixels * SpritePixelSize;

        // Position Sprite3D so feet are at world Y=0
        // Sprite3D is centered on its texture, so we offset it vertically
        var pos = _sprite3D.Position;
        pos.Y = (worldHeight / 2.0f) - feetOffsetWorld;
        _sprite3D.Position = pos;
    }

    private void RandomizeAnimationPhase()
    {
        if (_animationPlayer == null)
            return;

        string currentAnim = _animationPlayer.CurrentAnimation;
        if (string.IsNullOrEmpty(currentAnim))
            return;

        var anim = _animationPlayer.GetAnimation(currentAnim);
        if (anim == null)
            return;

        float randomOffset = GD.Randf() * (float)anim.Length;
        _animationPlayer.Seek(randomOffset, true);
    }

    private void OnAttackImpact()
    {
        // Forward to parent Unit3D
        var unit = GetParent();
        if (unit?.HasMethod("OnAttackImpact") == true)
        {
            unit.Call("OnAttackImpact");
        }
    }
}
