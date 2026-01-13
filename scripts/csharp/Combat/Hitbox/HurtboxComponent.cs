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

    /// <summary>The entity this hurtbox belongs to.</summary>
    public Node3D? OwnerEntity { get; private set; }

    // =========================================================================
    // INTERNAL STATE
    // =========================================================================

    private CollisionShape3D? _collisionShape;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        OwnerEntity = GetParent<Node3D>();
        ConfigureCollision();
        CreateShape();
    }

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>
    /// Configure the hurtbox with the given parameters.
    /// Call after adding to scene tree if not using exports.
    /// </summary>
    public void Configure(int team, HurtboxCategory category, float radius, float height)
    {
        Team = team;
        Category = category;
        Radius = radius;
        Height = height;

        if (IsInsideTree())
        {
            UpdateShape();
        }
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
        var capsule = new CapsuleShape3D
        {
            Radius = Radius,
            Height = Height
        };

        _collisionShape = new CollisionShape3D
        {
            Shape = capsule,
            // Position capsule so base is at Y=0
            Position = new Vector3(0, Height / 2, 0)
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

    // =========================================================================
    // GDScript COMPATIBILITY (snake_case aliases)
    // =========================================================================

    public void configure(int team, int category, float radius, float height)
        => Configure(team, (HurtboxCategory)category, radius, height);
}
