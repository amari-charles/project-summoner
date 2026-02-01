using Godot;
using System;
using System.Collections.Generic;

namespace ProjectSummoner.Combat.Hitbox;

/// <summary>
/// Hitbox component - the area that DEALS damage when overlapping a hurtbox.
/// Attach to projectiles, melee attacks, or spell effects.
/// </summary>
public partial class HitboxComponent : Area3D
{
    // =========================================================================
    // COLLISION LAYERS
    // =========================================================================
    // Hitboxes are on layer 6, detect hurtboxes on layer 5
    private const uint HitboxLayer = 1 << 5;   // Layer 6
    private const uint HurtboxMask = 1 << 4;   // Detect layer 5

    // =========================================================================
    // DAMAGE PROPERTIES
    // =========================================================================

    /// <summary>Base damage dealt by this hitbox.</summary>
    [Export] public float Damage { get; set; }

    /// <summary>Type of damage (physical, magical, fire, etc.).</summary>
    [Export] public string DamageType { get; set; } = DamageTypes.Physical;

    /// <summary>Team that created this hitbox (for friendly fire checks).</summary>
    [Export] public int SourceTeam { get; set; }

    /// <summary>The entity that created this hitbox.</summary>
    public Node3D? Source { get; set; }

    // =========================================================================
    // TARGETING
    // =========================================================================

    /// <summary>Categories of hurtboxes this hitbox can hit.</summary>
    [Export] public HurtboxCategory TargetCategories { get; set; } = HurtboxCategory.Unit | HurtboxCategory.Summoner;

    /// <summary>Whether this hitbox can hit allies.</summary>
    [Export] public bool CanHitAllies { get; set; } = false;

    // =========================================================================
    // HIT BEHAVIOR
    // =========================================================================

    /// <summary>If true, each target can only be hit once by this hitbox.</summary>
    [Export] public bool SingleHitPerTarget { get; set; } = true;

    /// <summary>How this hitbox determines its lifetime.</summary>
    [Export] public HitboxLifetime Lifetime { get; set; } = HitboxLifetime.UntilDestroyed;

    /// <summary>Duration in seconds (only used if Lifetime is Timed).</summary>
    [Export] public float Duration { get; set; } = 0.0f;

    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>Fired when this hitbox successfully hits a target.</summary>
    public event Action<HurtboxComponent, HitResult>? OnHit;

    // =========================================================================
    // INTERNAL STATE
    // =========================================================================

    private readonly HashSet<ulong> _hitTargets = new();
    private float _elapsed = 0.0f;
    private CollisionShape3D? _collisionShape;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        ConfigureCollision();
        AreaEntered += OnAreaEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Lifetime == HitboxLifetime.Timed)
        {
            _elapsed += (float)delta;
            if (_elapsed >= Duration)
            {
                QueueFree();
            }
        }
    }

    // =========================================================================
    // COLLISION HANDLING
    // =========================================================================

    private void OnAreaEntered(Area3D area)
    {
        if (area is not HurtboxComponent hurtbox)
            return;

        // Guard: entities may be disposed but physics callbacks still fire
        if (!IsInstanceValid(hurtbox) || !IsInstanceValid(hurtbox.OwnerEntity))
            return;

        if (Source != null && !IsInstanceValid(Source))
            return;

        // Already hit this target?
        if (SingleHitPerTarget && hurtbox.OwnerEntity != null &&
            _hitTargets.Contains(hurtbox.OwnerEntity.GetInstanceId()))
            return;

        // Team check (no friendly fire unless explicitly allowed)
        if (!CanHitAllies && hurtbox.Team == SourceTeam)
            return;

        // Category check
        if ((TargetCategories & hurtbox.Category) == 0)
            return;

        // Valid hit - resolve it through HitResolver
        if (HitResolver.Instance == null)
        {
            GD.PushWarning("HitboxComponent: HitResolver not found, hit ignored");
            return;
        }

        var result = HitResolver.Instance.ResolveHit(this, hurtbox);

        // Track this target
        if (hurtbox.OwnerEntity != null)
        {
            _hitTargets.Add(hurtbox.OwnerEntity.GetInstanceId());
        }

        // Fire event
        OnHit?.Invoke(hurtbox, result);

        // Self-destruct on first hit if configured
        if (Lifetime == HitboxLifetime.UntilFirstHit)
        {
            QueueFree();
        }
    }

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    private void ConfigureCollision()
    {
        CollisionLayer = HitboxLayer;
        CollisionMask = HurtboxMask;
        Monitorable = false;  // Hitboxes don't need to be detected by others
        Monitoring = true;    // Actively detects hurtboxes
    }

    /// <summary>
    /// Create a sphere-shaped hitbox.
    /// </summary>
    public void CreateSphereShape(float radius)
    {
        ClearExistingShape();

        var sphere = new SphereShape3D { Radius = radius };
        _collisionShape = new CollisionShape3D { Shape = sphere };
        AddChild(_collisionShape);
    }

    /// <summary>
    /// Create a capsule-shaped hitbox.
    /// </summary>
    public void CreateCapsuleShape(float radius, float height)
    {
        ClearExistingShape();

        var capsule = new CapsuleShape3D
        {
            Radius = radius,
            Height = height
        };
        _collisionShape = new CollisionShape3D
        {
            Shape = capsule,
            Position = new Vector3(0, height / 2, 0)
        };
        AddChild(_collisionShape);
    }

    /// <summary>
    /// Create a cylinder-shaped hitbox (for AoE effects).
    /// </summary>
    public void CreateCylinderShape(float radius, float height)
    {
        ClearExistingShape();

        var cylinder = new CylinderShape3D
        {
            Radius = radius,
            Height = height
        };
        _collisionShape = new CollisionShape3D
        {
            Shape = cylinder,
            Position = new Vector3(0, height / 2, 0)
        };
        AddChild(_collisionShape);
    }

    /// <summary>
    /// Create a box-shaped hitbox.
    /// </summary>
    public void CreateBoxShape(Vector3 size)
    {
        ClearExistingShape();

        var box = new BoxShape3D { Size = size };
        _collisionShape = new CollisionShape3D
        {
            Shape = box,
            Position = new Vector3(0, size.Y / 2, 0)
        };
        AddChild(_collisionShape);
    }

    private void ClearExistingShape()
    {
        if (_collisionShape != null)
        {
            _collisionShape.QueueFree();
            _collisionShape = null;
        }
    }

    /// <summary>
    /// Reset hit tracking (allows hitting the same targets again).
    /// Useful for persistent hitboxes that need periodic damage.
    /// </summary>
    public void ResetHitTracking()
    {
        _hitTargets.Clear();
    }

    // =========================================================================
    // GDScript COMPATIBILITY (snake_case aliases)
    // =========================================================================

    public void create_sphere_shape(float radius) => CreateSphereShape(radius);
    public void create_capsule_shape(float radius, float height) => CreateCapsuleShape(radius, height);
    public void create_cylinder_shape(float radius, float height) => CreateCylinderShape(radius, height);
    public void create_box_shape(Vector3 size) => CreateBoxShape(size);
    public void reset_hit_tracking() => ResetHitTracking();
}
