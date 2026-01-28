using Godot;
using System.Collections.Generic;

namespace ProjectSummoner.Visual;

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
    private Color _originalModulate = Colors.White;
    private Tween? _flashTween;
    private List<CanvasItem>? _cachedSprites;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        _sprite3D = GetNodeOrNull<Sprite3D>("Sprite3D");
        _viewport = GetNodeOrNull<SubViewport>("Sprite3D/SubViewport");
        _modelContainer = GetNodeOrNull<Node2D>("Sprite3D/SubViewport/ModelContainer");

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

    private async void InstanceSkeletalSceneDeferred()
    {
        await InstanceSkeletalScene();

        // Guard: component may be freed or removed from tree after await
        if (!IsInstanceValid(this) || !IsInsideTree())
            return;

        SetupSpriteAlignment();
        RandomizeAnimationPhase();
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
            _ => animName
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
            _ => animName
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

    public Vector3 GetShadowOffset()
    {
        // Content is centered at viewport center (FeetLocalPosition.X determines horizontal center)
        // Shadow offset is zero since the visual content is already centered
        return Vector3.Zero;
    }

    public float GetHpBarOffsetX()
    {
        return HpBarOffsetX;
    }

    public void FlashWhite()
    {
        if (_skeletalInstance == null)
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

        // Apply flash to each sprite individually to avoid SubViewport z_index rendering bug
        var flashColor = new Color(2.0f, 2.0f, 2.0f, 1.0f);
        _flashTween = CreateTween();
        _flashTween.SetParallel(true);

        // Phase 1: Flash to white (all sprites in parallel)
        foreach (var sprite in sprites)
        {
            _flashTween.TweenProperty(sprite, "modulate", flashColor, 0.05f);
        }

        // Phase 2: Hold white
        _flashTween.Chain().SetParallel(true);
        foreach (var sprite in sprites)
        {
            _flashTween.TweenProperty(sprite, "modulate", flashColor, 0.1f);
        }

        // Phase 3: Fade back to original
        _flashTween.Chain().SetParallel(true);
        foreach (var sprite in sprites)
        {
            _flashTween.TweenProperty(sprite, "modulate", _originalModulate, 0.15f);
        }
    }

    public void SetFlipH(bool flip)
    {
        _isFlipped = flip;

        if (_skeletalInstance == null || !_initializationComplete || _viewport == null)
            return;

        // Apply scale flip while preserving the display scale
        float displayScale = ScaleFactor.X;
        var scale = _skeletalInstance.Scale;
        scale.X = displayScale * (flip ? -1 : 1);
        _skeletalInstance.Scale = scale;

        // Adjust position to keep content centered after flip
        // When flipped, content that was at +contentCenterX from rig origin
        // is now at -contentCenterX, so we need to adjust rig position
        float contentCenterX = FeetLocalPosition.X * ScaleFactor.X;
        var viewportCenter = new Vector2(_viewport.Size.X / 2.0f, _viewport.Size.Y / 2.0f);
        var pos = _skeletalInstance.Position;
        if (flip)
        {
            // When flipped, content is at -contentCenterX from rig, so move rig right
            pos.X = viewportCenter.X + contentCenterX;
        }
        else
        {
            // When not flipped, content is at +contentCenterX from rig, so move rig left
            pos.X = viewportCenter.X - contentCenterX;
        }
        _skeletalInstance.Position = pos;
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
        if (!IsInstanceValid(this) || !IsInsideTree() || _skeletalInstance == null || !IsInstanceValid(_skeletalInstance))
            return;

        // Apply scale factor to the rig
        _skeletalInstance.Scale = ScaleFactor;

        // Position the rig so content center (at FeetLocalPosition.X) is at viewport center
        // This automatically handles centering without needing a separate ContentOffset parameter
        float contentCenterX = FeetLocalPosition.X * ScaleFactor.X;
        var viewportCenter = new Vector2(_viewport.Size.X / 2.0f, _viewport.Size.Y / 2.0f);
        _skeletalInstance.Position = new Vector2(viewportCenter.X - contentCenterX, viewportCenter.Y);

        // Mark initialization complete and show sprite
        _initializationComplete = true;
        if (_sprite3D != null)
        {
            _sprite3D.Visible = true;
        }

        // Apply deferred flip state (if SetFlipH was called before initialization)
        if (_isFlipped)
        {
            var scale = _skeletalInstance.Scale;
            scale.X = -Mathf.Abs(scale.X);
            _skeletalInstance.Scale = scale;

            // Also adjust position to keep content centered when flipped
            var pos = _skeletalInstance.Position;
            pos.X = viewportCenter.X + contentCenterX;  // Flip reverses the offset
            _skeletalInstance.Position = pos;
        }

        // Start idle animation
        if (_animationPlayer?.HasAnimation("idle") == true)
        {
            _animationPlayer.Play("idle");
        }
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
