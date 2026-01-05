using Godot;
using System.Collections.Generic;

namespace ProjectSummoner.Visual;

/// <summary>
/// Skeletal-based 2.5D Character Rendering Component.
/// Renders skeletal 2D animations in 3D space using Node2D pivots/AnimationPlayer + SubViewport.
/// Viewport is automatically sized to fit the character content.
/// </summary>
[GlobalClass]
public partial class SkeletalVisualComponent : Node3D, IVisualComponent
{
    // =========================================================================
    // EXPORTED PROPERTIES
    // =========================================================================

    [Export]
    public PackedScene? SkeletalScene { get; set; }

    [Export]
    public Vector2 ScaleFactor { get; set; } = new Vector2(0.1f, 0.1f);

    [Export]
    public float ViewportPadding { get; set; } = 200.0f;

    /// <summary>
    /// Offset in pixels from viewport bottom to where the feet are positioned.
    /// Used for correct depth sorting. Set to -1 to auto-calculate from ViewportPadding.
    /// </summary>
    [ExportGroup("Sprite Configuration")]
    [Export]
    public float FeetOffsetPixels { get; set; } = -1.0f;

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

    private Rect2 _cachedBounds;
    private bool _isFlipped;
    private bool _initializationComplete;

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
            _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        }

        // Instance skeletal scene if provided
        if (SkeletalScene != null)
        {
            CallDeferred(MethodName.InstanceSkeletalSceneDeferred);
        }
    }

    private async void InstanceSkeletalSceneDeferred()
    {
        await InstanceSkeletalScene();
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

        if (_cachedBounds.Size.Y > 0)
        {
            return _cachedBounds.Size.Y * _sprite3D.PixelSize;
        }

        return _viewport.Size.Y * _sprite3D.PixelSize;
    }

    public float GetSpriteWidth()
    {
        if (_viewport == null || _sprite3D == null)
            return 1.0f;

        if (_cachedBounds.Size.X > 0)
        {
            return _cachedBounds.Size.X * ScaleFactor.X * _sprite3D.PixelSize;
        }

        return _viewport.Size.X * ScaleFactor.X * _sprite3D.PixelSize;
    }

    public Vector3 GetShadowOffset()
    {
        // Skeletal units are centered - no offset needed
        return Vector3.Zero;
    }

    public void FlashWhite()
    {
        if (_skeletalInstance == null)
            return;

        var originalColor = _skeletalInstance.Modulate;

        var flashTween = CreateTween();
        flashTween.TweenProperty(_skeletalInstance, "modulate", new Color(2.0f, 2.0f, 2.0f, 1.0f), 0.05f);
        flashTween.TweenProperty(_skeletalInstance, "modulate", new Color(2.0f, 2.0f, 2.0f, 1.0f), 0.1f);
        flashTween.TweenProperty(_skeletalInstance, "modulate", originalColor, 0.15f);
    }

    public void SetFlipH(bool flip)
    {
        _isFlipped = flip;

        if (_skeletalInstance == null || !_initializationComplete)
            return;

        // Apply scale flip
        var scale = _skeletalInstance.Scale;
        scale.X = Mathf.Abs(scale.X) * (flip ? -1 : 1);
        _skeletalInstance.Scale = scale;

        ApplyFlipPosition(flip);
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
        // Apply tint to the internal skeletal instance for ghost transparency
        if (_skeletalInstance != null)
        {
            _skeletalInstance.Modulate = tint;
        }
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private async System.Threading.Tasks.Task InstanceSkeletalScene()
    {
        if (SkeletalScene == null || _modelContainer == null)
            return;

        _skeletalInstance = SkeletalScene.Instantiate<Node2D>();
        if (_skeletalInstance == null)
        {
            GD.PushError("SkeletalVisual: Failed to instance skeletal scene");
            return;
        }

        // Add at origin with no scale to calculate bounds
        _skeletalInstance.Position = Vector2.Zero;
        _skeletalInstance.Scale = Vector2.One;
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

        // Wait for tree update to calculate bounds
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Calculate bounds and resize viewport
        _cachedBounds = GetSkeletalBounds();

        if (_viewport != null && _cachedBounds.Size.X > 0 && _cachedBounds.Size.Y > 0)
        {
            int newWidth = (int)(_cachedBounds.Size.X + ViewportPadding * 2);
            int newHeight = (int)(_cachedBounds.Size.Y + ViewportPadding * 2);
            _viewport.Size = new Vector2I(newWidth, newHeight);

            // Position content: center horizontally, bottom-align vertically
            var pos = _skeletalInstance.Position;
            pos.X = (newWidth / 2.0f) - _cachedBounds.GetCenter().X;
            pos.Y = newHeight - _cachedBounds.End.Y - ViewportPadding;
            _skeletalInstance.Position = pos;
        }

        // Mark initialization complete
        _initializationComplete = true;

        // Apply deferred flip state
        if (_isFlipped)
        {
            var scale = _skeletalInstance.Scale;
            scale.X = -1;
            _skeletalInstance.Scale = scale;
            ApplyFlipPosition(true);
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

        // Calculate world height
        float worldHeight = _viewport.Size.Y * ScaleFactor.Y * _sprite3D.PixelSize;

        // Calculate feet offset from viewport bottom
        // If FeetOffsetPixels is set (>= 0), use it; otherwise auto-calculate from ViewportPadding
        float feetOffsetPx = FeetOffsetPixels >= 0 ? FeetOffsetPixels : ViewportPadding;
        float feetOffsetWorld = feetOffsetPx * ScaleFactor.Y * _sprite3D.PixelSize;

        // Position Sprite3D so feet (not viewport bottom) are at Y=0
        var pos = _sprite3D.Position;
        pos.Y = (worldHeight / 2.0f) - feetOffsetWorld;
        _sprite3D.Position = pos;

        // Apply scale
        _sprite3D.PixelSize = 0.01f * ScaleFactor.Y;
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

    private void ApplyFlipPosition(bool flip)
    {
        if (_skeletalInstance == null || _viewport == null || _cachedBounds.Size.X <= 0)
            return;

        float centerX = _viewport.Size.X / 2.0f;
        var pos = _skeletalInstance.Position;

        if (flip)
        {
            pos.X = centerX + _cachedBounds.GetCenter().X;
        }
        else
        {
            pos.X = centerX - _cachedBounds.GetCenter().X;
        }

        _skeletalInstance.Position = pos;
    }

    private Rect2 GetSkeletalBounds()
    {
        if (_skeletalInstance == null || !_skeletalInstance.IsInsideTree())
            return new Rect2();

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        bool foundSprites = false;

        var sprites = FindAllSprites(_skeletalInstance);

        foreach (var sprite in sprites)
        {
            var texture = sprite.Texture;
            if (texture == null)
                continue;

            var texSize = texture.GetSize();
            var spriteCenter = sprite.GlobalPosition - _skeletalInstance.GlobalPosition;

            Vector2 spriteMin, spriteMax;
            if (sprite.Centered)
            {
                var halfSize = texSize / 2.0f;
                spriteMin = spriteCenter - halfSize;
                spriteMax = spriteCenter + halfSize;
            }
            else
            {
                spriteMin = spriteCenter;
                spriteMax = spriteCenter + texSize;
            }

            minX = Mathf.Min(minX, spriteMin.X);
            maxX = Mathf.Max(maxX, spriteMax.X);
            minY = Mathf.Min(minY, spriteMin.Y);
            maxY = Mathf.Max(maxY, spriteMax.Y);
            foundSprites = true;
        }

        if (foundSprites)
        {
            return new Rect2(new Vector2(minX, minY), new Vector2(maxX - minX, maxY - minY));
        }

        return new Rect2();
    }

    private List<Sprite2D> FindAllSprites(Node node)
    {
        var sprites = new List<Sprite2D>();

        if (node is Sprite2D sprite)
        {
            sprites.Add(sprite);
        }

        foreach (var child in node.GetChildren())
        {
            sprites.AddRange(FindAllSprites(child));
        }

        return sprites;
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
