using System.Collections.Generic;
using System.Linq;
using Fateforged.Projectiles;
using Godot;

namespace Fateforged.Data.Projectiles;

/// <summary>
/// Central registry of projectile definitions.
/// Uses ProjectileDefinitions as the source of truth.
/// Registered as autoload: /root/ProjectileCatalog
/// </summary>
public partial class ProjectileCatalog : Node
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static ProjectileCatalog? Instance { get; private set; }

    // =========================================================================
    // STATE
    // =========================================================================

    private readonly Dictionary<string, ProjectileData> _loadedScenes = new();

    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void ContentLoadedEventHandler();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        GD.Print("ProjectileCatalog: Initializing...");
        LoadVisualScenes();
        GD.Print($"ProjectileCatalog: Ready with {ProjectileDefinitions.Count} projectiles");
        EmitSignal(SignalName.ContentLoaded);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================================
    // LOADING
    // =========================================================================

    /// <summary>
    /// Pre-load visual scenes for all projectiles.
    /// Scenes are loaded lazily and cached.
    /// </summary>
    private void LoadVisualScenes()
    {
        _loadedScenes.Clear();
        var errors = new List<string>();

        foreach (var projData in ProjectileDefinitions.All)
        {
            if (string.IsNullOrEmpty(projData.ModelScenePath))
                continue;

            if (ResourceLoader.Exists(projData.ModelScenePath))
            {
                var scene = ResourceLoader.Load<PackedScene>(projData.ModelScenePath);
                if (scene != null)
                {
                    projData.VisualScene = scene;
                    _loadedScenes[projData.ProjectileId] = projData;
                }
                else
                {
                    errors.Add(
                        $"Failed to load scene for '{projData.ProjectileId}': {projData.ModelScenePath}"
                    );
                }
            }
            else
            {
                errors.Add(
                    $"Scene path does not exist for '{projData.ProjectileId}': {projData.ModelScenePath}"
                );
            }
        }

        if (errors.Count > 0)
        {
            GD.PushWarning($"ProjectileCatalog: {errors.Count} visual scene errors:");
            foreach (var error in errors)
            {
                GD.PushWarning($"  - {error}");
            }
        }
        else
        {
            GD.Print("ProjectileCatalog: All visual scenes loaded successfully");
        }
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Get projectile data by ID.
    /// </summary>
    public ProjectileData? GetProjectile(ProjectileId projectileId) =>
        ProjectileDefinitions.Get(projectileId);

    /// <summary>
    /// Get projectile data by string ID.
    /// </summary>
    public ProjectileData? GetProjectile(string projectileId) =>
        ProjectileDefinitions.Get(projectileId);

    /// <summary>
    /// Check if a projectile exists.
    /// </summary>
    public bool HasProjectile(ProjectileId projectileId) => ProjectileDefinitions.Has(projectileId);

    /// <summary>
    /// Check if a projectile exists by string ID.
    /// </summary>
    public bool HasProjectile(string projectileId) => ProjectileDefinitions.Has(projectileId);

    /// <summary>
    /// Get all projectile IDs.
    /// </summary>
    public string[] GetAllProjectileIds() => ProjectileDefinitions.AllIds.ToArray();

    /// <summary>
    /// Get projectile count.
    /// </summary>
    public int Count => ProjectileDefinitions.Count;

    /// <summary>
    /// Reload visual scenes.
    /// Useful for hot-reloading during development.
    /// </summary>
    public void ReloadProjectiles()
    {
        GD.Print("ProjectileCatalog: Reloading visual scenes...");
        LoadVisualScenes();
        GD.Print($"ProjectileCatalog: Reloaded {ProjectileDefinitions.Count} projectiles");
        EmitSignal(SignalName.ContentLoaded);
    }

    // =========================================================================
    // GDSCRIPT COMPATIBILITY
    // =========================================================================

    // snake_case alias for GDScript interop
    public void reload_projectiles() => ReloadProjectiles();
}
