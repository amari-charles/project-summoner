using Godot;
using Godot.Collections;

namespace ProjectSummoner.Projectiles;

/// <summary>
/// Centralized projectile spawning and pooling system.
/// Uses lazy initialization - resources loaded on first use.
/// Registered as autoload: /root/ProjectileService
/// </summary>
public partial class ProjectileService : Node
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static ProjectileService? Instance { get; private set; }

    // =========================================================================
    // CONSTANTS
    // =========================================================================

    private const int InitialPoolSize = 5;
    private const int MaxPoolSize = 50;

    // =========================================================================
    // STATE
    // =========================================================================

    private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Projectile3D>> _pools = new();
    private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Projectile3D>> _activeProjectiles = new();

    private Node3D? _projectilesContainer;
    private Node3D? _poolContainer;
    private PackedScene? _baseProjectileScene;
    private bool _initialized = false;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        // Lazy initialization - don't load anything here
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================================
    // LAZY INITIALIZATION
    // =========================================================================

    private bool EnsureInitialized()
    {
        if (_initialized)
            return true;

        if (!CanInitialize())
            return false;

        GD.Print("ProjectileService: Initializing (lazy)...");

        // Create container for active projectiles
        _projectilesContainer = new Node3D { Name = "ProjectilesContainer" };
        AddChild(_projectilesContainer);

        // Create container for pooled projectiles
        _poolContainer = new Node3D { Name = "ProjectilePool" };
        AddChild(_poolContainer);

        // Load base projectile scene
        LoadProjectileScene();

        // Pre-instantiate pools for known projectiles
        InitPools();

        _initialized = true;
        GD.Print("ProjectileService: Initialized");
        return true;
    }

    private bool CanInitialize()
    {
        // Check if SpatialGrid is available (indicates C# runtime is ready)
        var spatialGrid = GetNodeOrNull("/root/SpatialGrid");
        if (spatialGrid == null)
            return false;

        return spatialGrid.HasMethod("register_unit");
    }

    // =========================================================================
    // RESOURCE LOADING
    // =========================================================================

    private void LoadProjectileScene()
    {
        const string scenePath = "res://scenes/projectiles/base_projectile_3d.tscn";
        if (ResourceLoader.Exists(scenePath))
        {
            _baseProjectileScene = ResourceLoader.Load<PackedScene>(scenePath);
        }
        else
        {
            GD.PushWarning($"ProjectileService: Base projectile scene not found at '{scenePath}'");
        }
    }

    private void InitPools()
    {
        var contentCatalog = GetNodeOrNull("/root/ContentCatalog");
        if (contentCatalog == null)
            return;

        var projectilesVar = contentCatalog.Get("projectiles");
        if (projectilesVar.VariantType != Variant.Type.Dictionary)
            return;

        var projectiles = projectilesVar.AsGodotDictionary();
        foreach (var key in projectiles.Keys)
        {
            string projectileId = key.AsString();
            CreatePoolFor(projectileId, InitialPoolSize);
        }
    }

    private void CreatePoolFor(string projectileId, int poolSize)
    {
        if (_pools.ContainsKey(projectileId))
            return;

        _pools[projectileId] = new System.Collections.Generic.List<Projectile3D>();
        _activeProjectiles[projectileId] = new System.Collections.Generic.List<Projectile3D>();

        for (int i = 0; i < poolSize; i++)
        {
            var projectile = InstantiateProjectile();
            if (projectile != null)
            {
                projectile.IsPooled = true;
                projectile.Reset();
                projectile.Visible = false;
                _poolContainer?.AddChild(projectile);
                _pools[projectileId].Add(projectile);
            }
        }

        GD.Print($"ProjectileService: Created pool of {poolSize} for '{projectileId}'");
    }

    private Projectile3D? InstantiateProjectile()
    {
        if (_baseProjectileScene != null)
        {
            return _baseProjectileScene.Instantiate<Projectile3D>();
        }
        else
        {
            // Fallback: create from script
            return new Projectile3D();
        }
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Spawn a projectile.
    /// </summary>
    public Projectile3D? SpawnProjectile(
        string projectileId,
        Node3D source,
        Node3D? target,
        float damage,
        string damageType = "physical",
        Dictionary? options = null)
    {
        if (!EnsureInitialized())
        {
            GD.PushWarning($"ProjectileService: Cannot spawn '{projectileId}', not initialized");
            return null;
        }

        // Get projectile data from ContentCatalog
        var data = GetProjectileData(projectileId);
        if (data == null)
        {
            GD.PushError($"ProjectileService: Projectile '{projectileId}' not found in ContentCatalog");
            return null;
        }

        // Get projectile from pool
        var projectile = GetFromPool(projectileId);
        if (projectile == null)
        {
            GD.PushError("ProjectileService: Failed to get projectile from pool");
            return null;
        }

        // Load configuration
        projectile.LoadFromData(data);

        // Build initialization data
        var initData = new Dictionary
        {
            ["source"] = source,
            ["damage"] = damage,
            ["damage_type"] = damageType
        };

        // Add target if provided
        if (target != null)
        {
            initData["target"] = target;
        }

        // Get team from source
        var teamVar = source.Get("Team");
        if (teamVar.VariantType == Variant.Type.Nil)
        {
            teamVar = source.Get("team");
        }
        initData["team"] = teamVar.VariantType != Variant.Type.Nil ? teamVar.AsInt32() : -1;

        // Apply options
        if (options != null)
        {
            if (options.TryGetValue("start_position", out var startPos))
                initData["start_position"] = startPos;
            if (options.TryGetValue("direction", out var dir))
                initData["direction"] = dir;
            if (options.TryGetValue("target_position", out var targetPos))
                initData["target_position"] = targetPos;
        }

        // Add to scene root for proper 3D rendering
        GetTree().Root.AddChild(projectile);

        // Initialize after adding to tree
        projectile.Initialize(initData);

        // Track active projectile
        if (!_activeProjectiles.ContainsKey(projectileId))
        {
            _activeProjectiles[projectileId] = new System.Collections.Generic.List<Projectile3D>();
        }
        _activeProjectiles[projectileId].Add(projectile);

        // Connect expiration signal
        if (!projectile.IsConnected(Projectile3D.SignalName.ProjectileExpired, Callable.From<Projectile3D>(OnProjectileExpired)))
        {
            projectile.ProjectileExpired += OnProjectileExpired;
        }

        return projectile;
    }

    /// <summary>
    /// Clear all active projectiles (for scene transitions).
    /// </summary>
    public void ClearAllProjectiles()
    {
        if (!_initialized)
            return;

        foreach (var kvp in _activeProjectiles)
        {
            string projectileId = kvp.Key;
            var activeList = kvp.Value;

            foreach (var projectile in activeList.ToArray())
            {
                if (projectile.GetParent() != null)
                {
                    projectile.GetParent().RemoveChild(projectile);
                }
                ReturnToPool(projectileId, projectile);
            }
        }

        _activeProjectiles.Clear();
    }

    /// <summary>
    /// Clear all pools.
    /// </summary>
    public void ClearAllPools()
    {
        if (!_initialized)
            return;

        foreach (var pool in _pools.Values)
        {
            foreach (var projectile in pool)
            {
                if (IsInstanceValid(projectile))
                {
                    projectile.QueueFree();
                }
            }
        }

        foreach (var activeList in _activeProjectiles.Values)
        {
            foreach (var projectile in activeList)
            {
                if (IsInstanceValid(projectile))
                {
                    projectile.QueueFree();
                }
            }
        }

        _pools.Clear();
        _activeProjectiles.Clear();
        GD.Print("ProjectileService: Cleared all pools");
    }

    /// <summary>
    /// Refresh pools with new visuals.
    /// </summary>
    public void RefreshPools()
    {
        if (!EnsureInitialized())
            return;

        ClearAllPools();
        InitPools();
        GD.Print("ProjectileService: Pools refreshed");
    }

    /// <summary>
    /// Print pool statistics for debugging.
    /// </summary>
    public void PrintPoolStats()
    {
        if (!_initialized)
        {
            GD.Print("ProjectileService: Not initialized");
            return;
        }

        GD.Print("=== ProjectileService Pool Statistics ===");
        foreach (var kvp in _pools)
        {
            string projectileId = kvp.Key;
            int poolSize = kvp.Value.Count;
            int activeSize = _activeProjectiles.ContainsKey(projectileId) ? _activeProjectiles[projectileId].Count : 0;
            GD.Print($"  {projectileId}: {poolSize} in pool, {activeSize} active");
        }
    }

    // =========================================================================
    // INTERNAL HELPERS
    // =========================================================================

    private Projectile3D? GetFromPool(string projectileId)
    {
        if (!_pools.ContainsKey(projectileId))
        {
            CreatePoolFor(projectileId, InitialPoolSize);
        }

        var pool = _pools[projectileId];

        // Find a valid projectile in the pool (skip disposed ones)
        while (pool.Count > 0)
        {
            var projectile = pool[^1];
            pool.RemoveAt(pool.Count - 1);

            // Skip disposed projectiles
            if (!IsInstanceValid(projectile))
            {
                continue;
            }

            if (projectile.GetParent() != null)
            {
                projectile.GetParent().RemoveChild(projectile);
            }
            projectile.Reset();
            projectile.Visible = true;
            return projectile;
        }

        // Pool exhausted, create new
        var newProjectile = InstantiateProjectile();
        if (newProjectile != null)
        {
            newProjectile.IsPooled = true;
        }
        return newProjectile;
    }

    private void ReturnToPool(string projectileId, Projectile3D projectile)
    {
        // Remove from active
        if (_activeProjectiles.ContainsKey(projectileId))
        {
            _activeProjectiles[projectileId].Remove(projectile);
        }

        // Don't pool disposed projectiles
        if (!IsInstanceValid(projectile))
        {
            return;
        }

        // Return to pool if not full
        if (!_pools.ContainsKey(projectileId))
        {
            _pools[projectileId] = new System.Collections.Generic.List<Projectile3D>();
        }

        var pool = _pools[projectileId];
        if (pool.Count < MaxPoolSize)
        {
            projectile.Reset();
            projectile.Visible = false;
            pool.Add(projectile);
            // Deferred reparent to pool container
            CallDeferred(nameof(DeferredAddToPool), projectile);
        }
        else
        {
            projectile.QueueFree();
        }
    }

    private void DeferredAddToPool(Projectile3D projectile)
    {
        if (!IsInstanceValid(projectile))
            return;

        // Check if already reused
        if (projectile.Visible)
            return;

        if (projectile.GetParent() != null)
        {
            projectile.GetParent().RemoveChild(projectile);
        }
        _poolContainer?.AddChild(projectile);
    }

    private void OnProjectileExpired(Projectile3D projectile)
    {
        // Don't process disposed projectiles
        if (!IsInstanceValid(projectile))
        {
            return;
        }

        if (string.IsNullOrEmpty(projectile.ProjectileId))
        {
            GD.PushError("ProjectileService: Projectile expired with empty ProjectileId");
            projectile.QueueFree();
            return;
        }
        ReturnToPool(projectile.ProjectileId, projectile);
    }

    private ProjectileData? GetProjectileData(string projectileId)
    {
        var contentCatalog = GetNodeOrNull("/root/ContentCatalog");
        if (contentCatalog == null)
            return null;

        if (!contentCatalog.Call("has_projectile", projectileId).AsBool())
            return null;

        var gdProjectileData = contentCatalog.Call("get_projectile", projectileId).AsGodotObject();
        if (gdProjectileData == null)
            return null;

        return ProjectileData.FromGodotResource(gdProjectileData);
    }

    // =========================================================================
    // GDSCRIPT COMPATIBILITY (snake_case aliases)
    // =========================================================================

    public Projectile3D? spawn_projectile(
        string projectileId,
        Node3D source,
        Node3D? target,
        float damage,
        string damageType = "physical",
        Dictionary? options = null)
        => SpawnProjectile(projectileId, source, target, damage, damageType, options);

    public void clear_all_projectiles() => ClearAllProjectiles();
    public void clear_all_pools() => ClearAllPools();
    public void refresh_pools() => RefreshPools();
    public void print_pool_stats() => PrintPoolStats();
}
