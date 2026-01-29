using System.Collections.Generic;
using Godot;
using ProjectSummoner.Projectiles;

namespace ProjectSummoner.Data.Projectiles;

/// <summary>
/// Central registry of projectile definitions.
/// Loads projectile data from JSON files at startup.
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

    private readonly Dictionary<string, ProjectileData> _projectiles = new();

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
        LoadProjectiles();
        ValidateContent();
        GD.Print($"ProjectileCatalog: Loaded {_projectiles.Count} projectiles");
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
    /// Load all projectile definitions from JSON files.
    /// </summary>
    private void LoadProjectiles()
    {
        _projectiles.Clear();

        const string projDir = "res://data/projectiles/";
        var dir = DirAccess.Open(projDir);

        if (dir == null)
        {
            GD.PushWarning($"ProjectileCatalog: projectiles directory not found: {projDir}");
            return;
        }

        dir.ListDirBegin();
        var fileName = dir.GetNext();

        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName.EndsWith(".json"))
            {
                var filePath = projDir + fileName;
                var projData = LoadProjectileFromFile(filePath);
                if (projData != null)
                {
                    _projectiles[projData.ProjectileId] = projData;
                }
            }
            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    /// <summary>
    /// Load a single projectile definition from a JSON file.
    /// </summary>
    private ProjectileData? LoadProjectileFromFile(string filePath)
    {
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"ProjectileCatalog: Failed to open file: {filePath}");
            return null;
        }

        var jsonText = file.GetAsText();
        var json = new Json();
        var parseResult = json.Parse(jsonText);

        if (parseResult != Error.Ok)
        {
            GD.PushError($"ProjectileCatalog: JSON parse error in {filePath} at line {json.GetErrorLine()}: {json.GetErrorMessage()}");
            return null;
        }

        var data = json.Data;
        if (data.VariantType != Variant.Type.Dictionary)
        {
            GD.PushError($"ProjectileCatalog: JSON root is not a dictionary: {filePath}");
            return null;
        }

        return ProjectileData.FromDictionary(data.AsGodotDictionary());
    }

    /// <summary>
    /// Validate loaded projectile data for consistency.
    /// </summary>
    private void ValidateContent()
    {
        var errors = new List<string>();

        foreach (var projData in _projectiles.Values)
        {
            if (projData.Speed <= 0)
            {
                errors.Add($"Projectile '{projData.ProjectileId}' has invalid speed: {projData.Speed:F1}");
            }
        }

        if (errors.Count > 0)
        {
            GD.PushError($"ProjectileCatalog: Found {errors.Count} validation errors:");
            foreach (var error in errors)
            {
                GD.PushError($"  - {error}");
            }
        }
        else
        {
            GD.Print("ProjectileCatalog: All projectiles validated successfully");
        }
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Get projectile data by ID.
    /// </summary>
    public ProjectileData? GetProjectile(string projectileId)
    {
        return _projectiles.GetValueOrDefault(projectileId);
    }

    /// <summary>
    /// Check if a projectile exists.
    /// </summary>
    public bool HasProjectile(string projectileId)
    {
        return _projectiles.ContainsKey(projectileId);
    }

    /// <summary>
    /// Get all projectile IDs.
    /// </summary>
    public string[] GetAllProjectileIds()
    {
        return [.. _projectiles.Keys];
    }

    /// <summary>
    /// Get projectile count.
    /// </summary>
    public int Count => _projectiles.Count;

    /// <summary>
    /// Reload all projectiles from disk.
    /// Useful for hot-reloading during development.
    /// </summary>
    public void ReloadProjectiles()
    {
        GD.Print("ProjectileCatalog: Reloading projectiles...");
        LoadProjectiles();
        ValidateContent();
        GD.Print($"ProjectileCatalog: Reloaded {_projectiles.Count} projectiles");
        EmitSignal(SignalName.ContentLoaded);
    }
}
