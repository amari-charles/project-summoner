using Godot;

namespace ProjectSummoner.Combat.Hitbox;

/// <summary>
/// Hurtbox component - the area of an entity that can BE hit.
/// Attach to units, summoners, or destructibles.
/// </summary>
public partial class HurtboxComponent : Area3D
{
    // =========================================================================
    // COLLISION LAYERS
    // =========================================================================
    // Hurtboxes are on layer 5, detected by hitboxes on layer 6
    private const uint HurtboxLayer = 1 << 4;  // Layer 5
    private const uint HitboxMask = 1 << 5;    // Detect layer 6

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    /// <summary>Team this hurtbox belongs to (for friendly fire checks).</summary>
    [Export] public int Team { get; set; }

    /// <summary>What category of target this is.</summary>
    [Export] public HurtboxCategory Category { get; set; } = HurtboxCategory.Unit;

    /// <summary>Collision radius for the hurtbox capsule.</summary>
    [Export] public float Radius { get; set; } = 0.5f;

    /// <summary>Height of the hurtbox capsule.</summary>
    [Export] public float Height { get; set; } = 2.0f;

    /// <summary>Size for box-shaped hurtbox (Width, Height, Depth).</summary>
    [Export] public Vector3 BoxSize { get; set; } = Vector3.Zero;

    /// <summary>Whether the capsule should be horizontal (rotated 90 degrees).</summary>
    [Export] public bool Horizontal { get; set; } = false;

    /// <summary>The entity this hurtbox belongs to.</summary>
    public Node3D? OwnerEntity { get; private set; }

    // =========================================================================
    // INTERNAL STATE
    // =========================================================================

    private CollisionShape3D? _collisionShape;
    private bool _useBoxShape = false;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        OwnerEntity = GetParent<Node3D>();
        ConfigureCollision();
        // Don't create shape here - let Configure/ConfigureBox create it
        // This ensures properties are set before shape is created
    }

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>
    /// Configure the hurtbox with the given parameters (capsule shape).
    /// Must be called after adding to scene tree.
    /// </summary>
    public void Configure(int team, HurtboxCategory category, float radius, float height, bool horizontal = false)
    {
        Team = team;
        Category = category;
        Radius = radius;
        Height = height;
        Horizontal = horizontal;
        _useBoxShape = false;

        // Create or update the shape
        if (_collisionShape != null)
        {
            _collisionShape.QueueFree();
            _collisionShape = null;
        }
        CreateCapsuleShape();
    }

    /// <summary>
    /// Configure the hurtbox with a box shape (for wide/flat units like Puff).
    /// Must be called after adding to scene tree.
    /// </summary>
    public void ConfigureBox(int team, HurtboxCategory category, Vector3 boxSize)
    {
        Team = team;
        Category = category;
        BoxSize = boxSize;
        Height = boxSize.Y;  // Keep Height for compatibility
        _useBoxShape = true;

        // Create or update the shape
        if (_collisionShape != null)
        {
            _collisionShape.QueueFree();
            _collisionShape = null;
        }
        CreateBoxShape();
    }

    private void ConfigureCollision()
    {
        CollisionLayer = HurtboxLayer;
        CollisionMask = HitboxMask;
        Monitorable = true;   // Can be detected by hitboxes
        Monitoring = false;   // Doesn't need to detect anything
    }

    private void CreateShape()
    {
        if (_useBoxShape && BoxSize != Vector3.Zero)
        {
            CreateBoxShape();
        }
        else
        {
            CreateCapsuleShape();
        }
    }

    private void CreateCapsuleShape()
    {
        var capsule = new CapsuleShape3D
        {
            Radius = Radius,
            Height = Height
        };

        _collisionShape = new CollisionShape3D
        {
            Shape = capsule
        };

        if (Horizontal)
        {
            // Rotate 90 degrees around Z axis to make capsule horizontal (lying along X)
            _collisionShape.Rotation = new Vector3(0, 0, Mathf.DegToRad(90));
            // Position so the center is at the unit's center height
            _collisionShape.Position = new Vector3(0, Radius, 0);
        }
        else
        {
            // Position capsule so base is at Y=0
            _collisionShape.Position = new Vector3(0, Height / 2, 0);
        }

        AddChild(_collisionShape);
    }

    private void CreateBoxShape()
    {
        var box = new BoxShape3D
        {
            Size = BoxSize
        };

        _collisionShape = new CollisionShape3D
        {
            Shape = box,
            // Position box so base is at Y=0
            Position = new Vector3(0, BoxSize.Y / 2, 0)
        };

        AddChild(_collisionShape);
    }

    private void UpdateShape()
    {
        if (_collisionShape?.Shape is CapsuleShape3D capsule)
        {
            capsule.Radius = Radius;
            capsule.Height = Height;
            _collisionShape.Position = new Vector3(0, Height / 2, 0);
        }
    }

    private void UpdateBoxShape()
    {
        if (_collisionShape?.Shape is BoxShape3D box)
        {
            box.Size = BoxSize;
            _collisionShape.Position = new Vector3(0, BoxSize.Y / 2, 0);
        }
        else
        {
            // Need to recreate the shape
            _collisionShape?.QueueFree();
            CreateBoxShape();
        }
    }

    // =========================================================================
    // GDScript COMPATIBILITY (snake_case aliases)
    // =========================================================================

    public void configure(int team, int category, float radius, float height)
        => Configure(team, (HurtboxCategory)category, radius, height);
}
