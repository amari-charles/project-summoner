using Godot;
using System.Collections.Generic;
using ProjectSummoner.Systems;
using ProjectSummoner.UI;
using ProjectSummoner.Units;

namespace ProjectSummoner.Services;

/// <summary>
/// Centralized HP bar management with object pooling.
/// Autoload as: /root/HPBarService
/// </summary>
public partial class HPBarService : Node
{
    public static HPBarService? Instance { get; private set; }

    private const int InitialPoolSize = 20;
    private const int MaxPoolSize = 50;
    private const string BarScenePath = "res://scenes/ui/battle/floating_hp_bar.tscn";

    private readonly Dictionary<Node3D, FloatingHPBar> _activeBars = new();
    private readonly Queue<FloatingHPBar> _barPool = new();

    private PackedScene? _barScene;
    private Node3D? _barsContainer;
    private Node3D? _poolContainer;
    private bool _initialized;

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    #region Public API

    /// <summary>
    /// Create an HP bar for a Unit3D. The bar will auto-cleanup when the unit exits the tree.
    /// </summary>
    public FloatingHPBar? CreateBarForUnit(Unit3D unit, HPBarSettings? settings = null)
    {
        if (!EnsureInitialized())
        {
            GD.PushWarning("HPBarService: Cannot create bar, not initialized");
            return null;
        }

        if (_activeBars.ContainsKey(unit))
        {
            GD.PushWarning("HPBarService: Unit already has an HP bar");
            return _activeBars[unit];
        }

        var bar = GetOrCreateBar();
        if (bar == null)
        {
            GD.PushError("HPBarService: Failed to create HP bar");
            return null;
        }

        // Apply settings
        var effectiveSettings = settings ?? HPBarSettings.Default;
        bar.Configure(effectiveSettings);

        // Track the unit (connects signals including TreeExiting for auto-cleanup)
        bar.TrackUnit(unit);

        // Add to scene
        _barsContainer!.AddChild(bar);

        // Track active bar
        _activeBars[unit] = bar;

        return bar;
    }

    /// <summary>
    /// Create an HP bar for a generic Node3D (summoners, bases).
    /// These don't auto-cleanup via TreeExiting, caller must remove manually.
    /// </summary>
    public FloatingHPBar? CreateBarForNode(Node3D node, HPBarSettings? settings = null)
    {
        if (!EnsureInitialized())
        {
            GD.PushWarning("HPBarService: Cannot create bar, not initialized");
            return null;
        }

        if (_activeBars.ContainsKey(node))
        {
            GD.PushWarning("HPBarService: Node already has an HP bar");
            return _activeBars[node];
        }

        var bar = GetOrCreateBar();
        if (bar == null)
        {
            GD.PushError("HPBarService: Failed to create HP bar");
            return null;
        }

        // Apply settings
        var effectiveSettings = settings ?? HPBarSettings.AlwaysVisible;
        bar.Configure(effectiveSettings);

        // Track the node (connects TreeExiting for cleanup)
        bar.TrackNode(node);

        // Add to scene
        _barsContainer!.AddChild(bar);

        // Track active bar
        _activeBars[node] = bar;

        return bar;
    }

    /// <summary>
    /// Remove HP bar from a unit/node. Called automatically via TreeExiting signal for Unit3D.
    /// </summary>
    public void RemoveBar(Node3D node)
    {
        if (!_activeBars.TryGetValue(node, out var bar))
            return;

        _activeBars.Remove(node);

        // Disconnect signals and reset
        bar.Detach();

        // Remove from scene
        bar.GetParent()?.RemoveChild(bar);

        // Return to pool
        ReturnBarToPool(bar);
    }

    /// <summary>
    /// Update HP bar for a node directly (if it exists).
    /// Usually not needed as bars listen to HpChanged signal.
    /// </summary>
    public void UpdateNodeHp(Node3D node, float currentHp, float maxHp)
    {
        if (_activeBars.TryGetValue(node, out var bar))
        {
            bar.UpdateHp(currentHp, maxHp);
        }
    }

    /// <summary>
    /// Remove all bars. Useful for scene transitions.
    /// </summary>
    public void ClearAllBars()
    {
        if (!_initialized)
            return;

        // Collect bars to clear (can't modify dict during iteration)
        var barsToClear = new List<FloatingHPBar>(_activeBars.Values);

        foreach (var bar in barsToClear)
        {
            if (IsInstanceValid(bar))
            {
                bar.Detach();
                bar.GetParent()?.RemoveChild(bar);
                ReturnBarToPool(bar);
            }
        }

        _activeBars.Clear();
    }

    /// <summary>
    /// Debug: Print pool statistics.
    /// </summary>
    public void PrintPoolStats()
    {
        if (!_initialized)
        {
            GD.Print("HPBarService: Not initialized");
            return;
        }

        GD.Print("=== HPBarService Pool Statistics ===");
        GD.Print($"  Bars in pool: {_barPool.Count}");
        GD.Print($"  Active bars: {_activeBars.Count}");
        GD.Print($"  Total bars: {_barPool.Count + _activeBars.Count}");
    }

    /// <summary>
    /// For testing: Get the pool container node.
    /// </summary>
    public Node3D? GetPoolContainer() => _poolContainer;

    /// <summary>
    /// For testing: Get the count of pooled bars.
    /// </summary>
    public int GetPooledBarCount() => _barPool.Count;

    /// <summary>
    /// For testing: Get the count of active bars.
    /// </summary>
    public int GetActiveBarCount() => _activeBars.Count;

    /// <summary>
    /// For testing: Force initialization (normally lazy).
    /// </summary>
    public bool ForceInitialize() => EnsureInitialized();

    #endregion

    #region GDScript Interop

    // These methods allow GDScript (summoner.gd) to call the service

    public FloatingHPBar? create_bar_for_unit(Node3D unit, Godot.Collections.Dictionary? settings = null)
    {
        if (unit is Unit3D unit3d)
        {
            return CreateBarForUnit(unit3d, ParseSettings(settings));
        }
        return CreateBarForNode(unit, ParseSettings(settings));
    }

    public void remove_bar_from_unit(Node3D unit)
    {
        RemoveBar(unit);
    }

    public void update_unit_hp(Node3D unit, float currentHp, float maxHp)
    {
        UpdateNodeHp(unit, currentHp, maxHp);
    }

    public void clear_all_bars()
    {
        ClearAllBars();
    }

    public void print_pool_stats()
    {
        PrintPoolStats();
    }

    private static HPBarSettings ParseSettings(Godot.Collections.Dictionary? dict)
    {
        if (dict == null)
            return HPBarSettings.Default;

        var settings = HPBarSettings.Default;

        if (dict.TryGetValue("bar_width", out var bw) || dict.TryGetValue("BarWidth", out bw))
            settings.BarWidth = (float)bw;
        if (dict.TryGetValue("bar_height", out var bh) || dict.TryGetValue("BarHeight", out bh))
            settings.BarHeight = (float)bh;
        if (dict.TryGetValue("offset_y", out var oy) || dict.TryGetValue("OffsetY", out oy))
            settings.OffsetY = (float)oy;
        if (dict.TryGetValue("show_on_damage_only", out var sod) || dict.TryGetValue("ShowOnDamageOnly", out sod))
            settings.ShowOnDamageOnly = (bool)sod;
        if (dict.TryGetValue("fade_delay", out var fd) || dict.TryGetValue("FadeDelay", out fd))
            settings.FadeDelay = (float)fd;

        return settings;
    }

    #endregion

    #region Initialization

    private bool EnsureInitialized()
    {
        if (_initialized)
            return true;

        if (!CanInitialize())
            return false;

        GD.Print("HPBarService: Initializing...");

        // Create container for active bars
        _barsContainer = new Node3D { Name = "HPBarsContainer" };
        AddChild(_barsContainer);

        // Create container for pooled bars (keeps them in scene tree, avoids orphans)
        _poolContainer = new Node3D { Name = "HPBarPool" };
        AddChild(_poolContainer);

        // Load HP bar scene
        LoadBarScene();

        // Pre-instantiate pool
        InitializePool();

        _initialized = true;
        GD.Print($"HPBarService: Initialized with pool of {InitialPoolSize} bars");
        return true;
    }

    private bool CanInitialize()
    {
        // Check if SpatialGrid is available (indicates C# runtime is ready)
        return SpatialGrid.Instance != null;
    }

    private void LoadBarScene()
    {
        if (ResourceLoader.Exists(BarScenePath))
        {
            _barScene = GD.Load<PackedScene>(BarScenePath);
        }
        else
        {
            GD.PushWarning("HPBarService: HP bar scene not found, will instantiate directly");
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < InitialPoolSize; i++)
        {
            var bar = InstantiateBar();
            if (bar != null)
            {
                bar.Reset();
                bar.Visible = false;
                _poolContainer!.AddChild(bar);
                _barPool.Enqueue(bar);
            }
        }
    }

    #endregion

    #region Pooling

    private FloatingHPBar? InstantiateBar()
    {
        if (_barScene != null)
        {
            return _barScene.Instantiate<FloatingHPBar>();
        }
        else
        {
            // Fallback: create from code
            return new FloatingHPBar();
        }
    }

    private FloatingHPBar? GetOrCreateBar()
    {
        if (_barPool.TryDequeue(out var bar))
        {
            bar.GetParent()?.RemoveChild(bar);
            bar.Reset();
            return bar;
        }

        return InstantiateBar();
    }

    private void ReturnBarToPool(FloatingHPBar bar)
    {
        bar.Reset();

        if (_barPool.Count < MaxPoolSize)
        {
            _poolContainer!.AddChild(bar);
            _barPool.Enqueue(bar);
        }
        else
        {
            // Pool full, destroy bar
            bar.QueueFree();
        }
    }

    #endregion
}

/// <summary>
/// Settings for HP bar appearance and behavior.
/// </summary>
public struct HPBarSettings
{
    public float BarWidth;
    public float BarHeight;
    public float OffsetY;
    public bool ShowOnDamageOnly;
    public float FadeDelay;
    public float FadeDuration;

    public static HPBarSettings Default => new()
    {
        BarWidth = 0.8f,
        BarHeight = 0.08f,
        OffsetY = 3.2f,
        ShowOnDamageOnly = true,
        FadeDelay = 3.0f,
        FadeDuration = 0.5f
    };

    public static HPBarSettings AlwaysVisible => new()
    {
        BarWidth = 0.8f,
        BarHeight = 0.08f,
        OffsetY = 3.2f,
        ShowOnDamageOnly = false,
        FadeDelay = 3.0f,
        FadeDuration = 0.5f
    };
}
