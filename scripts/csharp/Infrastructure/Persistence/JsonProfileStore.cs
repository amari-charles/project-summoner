using Godot;

namespace Fateforged.Infrastructure.Persistence;

/// <summary>
/// Pure file I/O for profile JSON persistence.
/// Handles atomic writes, backup rotation, and fallback loading.
/// No business logic or signals.
/// </summary>
public class JsonProfileStore
{
    /// <summary>
    /// Load profile data from disk, trying main file then backups.
    /// Returns null if all files are missing or corrupted.
    /// </summary>
    public Godot.Collections.Dictionary? LoadProfile(string profileId)
    {
        var profileDir = GetProfileDir(profileId);
        var mainPath = profileDir + "/profile.json";
        var bak1Path = profileDir + "/profile.bak1";
        var bak2Path = profileDir + "/profile.bak2";

        // Try main save first
        if (FileAccess.FileExists(mainPath))
        {
            var data = LoadFromFile(mainPath);
            if (data != null)
            {
                GD.Print("JsonProfileStore: Loaded from main save");
                return data;
            }
            GD.PushWarning("JsonProfileStore: Main save corrupted, trying backup1...");
        }

        // Try backup1
        if (FileAccess.FileExists(bak1Path))
        {
            var data = LoadFromFile(bak1Path);
            if (data != null)
            {
                GD.Print("JsonProfileStore: Loaded from backup1");
                return data;
            }
            GD.PushWarning("JsonProfileStore: Backup1 corrupted, trying backup2...");
        }

        // Try backup2
        if (FileAccess.FileExists(bak2Path))
        {
            var data = LoadFromFile(bak2Path);
            if (data != null)
            {
                GD.Print("JsonProfileStore: Loaded from backup2");
                return data;
            }
            GD.PushError("JsonProfileStore: All save files corrupted!");
        }

        return null;
    }

    /// <summary>
    /// Save profile data to disk with atomic write (temp→rename) and backup rotation.
    /// </summary>
    public bool SaveProfile(string profileId, Godot.Collections.Dictionary data)
    {
        var profileDir = GetProfileDir(profileId);
        var mainPath = profileDir + "/profile.json";
        var bak1Path = profileDir + "/profile.bak1";
        var bak2Path = profileDir + "/profile.bak2";
        var tempPath = profileDir + "/profile.tmp";

        // Rotate backups BEFORE writing (preserves old data in case of crash)
        RotateBackups(profileDir, mainPath, bak1Path, bak2Path);

        // Atomic write: write to temp, then rename
        return AtomicWrite(data, profileDir, tempPath, mainPath);
    }

    /// <summary>
    /// Ensure the profile directory exists.
    /// </summary>
    public void EnsureProfileDir(string profileId)
    {
        using var dir = DirAccess.Open("user://");
        if (dir == null)
            return;

        if (!dir.DirExists("profiles"))
            dir.MakeDir("profiles");

        var profileDir = "profiles/" + profileId;
        if (!dir.DirExists(profileDir))
            dir.MakeDir(profileDir);
    }

    private static string GetProfileDir(string profileId)
    {
        return "user://profiles/" + profileId;
    }

    private static Godot.Collections.Dictionary? LoadFromFile(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"JsonProfileStore: Failed to open file: {path}");
            return null;
        }

        var jsonString = file.GetAsText();

        using var json = new Json();
        var error = json.Parse(jsonString);
        if (error != Error.Ok)
        {
            GD.PushError($"JsonProfileStore: JSON parse error in {path}: {json.GetErrorMessage()}");
            return null;
        }

        if (json.Data.VariantType == Variant.Type.Dictionary)
            return json.Data.AsGodotDictionary();

        GD.PushError($"JsonProfileStore: JSON root is not a dictionary in {path}");
        return null;
    }

    private static bool AtomicWrite(
        Godot.Collections.Dictionary data,
        string profileDir,
        string tempPath,
        string mainPath
    )
    {
        // Write to temp file first
        using (var file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write))
        {
            if (file == null)
            {
                GD.PushError("JsonProfileStore: Failed to create temp file");
                return false;
            }

            var jsonString = Json.Stringify(data, "\t");
            file.StoreString(jsonString);
        }

        // Verify temp file was written
        if (!FileAccess.FileExists(tempPath))
        {
            GD.PushError("JsonProfileStore: Temp file disappeared!");
            return false;
        }

        // Rename temp to main save (atomic operation)
        using var dir = DirAccess.Open(profileDir);
        if (dir == null)
        {
            GD.PushError("JsonProfileStore: Failed to open profile directory");
            return false;
        }

        // Remove old main save if it exists
        if (FileAccess.FileExists(mainPath))
        {
            var err = dir.Remove("profile.json");
            if (err != Error.Ok)
            {
                GD.PushError("JsonProfileStore: Failed to remove old save");
                return false;
            }
        }

        // Rename temp to main
        var renameErr = dir.Rename("profile.tmp", "profile.json");
        if (renameErr != Error.Ok)
        {
            GD.PushError("JsonProfileStore: Failed to rename temp to main");
            return false;
        }

        return true;
    }

    private static void RotateBackups(
        string profileDir,
        string mainPath,
        string bak1Path,
        string bak2Path
    )
    {
        using var dir = DirAccess.Open(profileDir);
        if (dir == null)
        {
            GD.PushWarning(
                "JsonProfileStore: Failed to open profile directory for backup rotation"
            );
            return;
        }

        // Rotate: bak1 → bak2
        if (FileAccess.FileExists(bak1Path))
        {
            if (FileAccess.FileExists(bak2Path))
                dir.Remove("profile.bak2");
            dir.Rename("profile.bak1", "profile.bak2");
        }

        // Copy main → bak1
        if (FileAccess.FileExists(mainPath))
            dir.Copy(mainPath, bak1Path);
    }
}
