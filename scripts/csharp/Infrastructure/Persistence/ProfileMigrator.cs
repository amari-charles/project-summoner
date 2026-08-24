using Godot;
using GdDict = Godot.Collections.Dictionary;

namespace Fateforged.Infrastructure.Persistence;

/// <summary>
/// Marks persisted data with the current schema version. Pre-quest progression
/// schemas are intentionally unsupported and start with empty summoner progress.
/// </summary>
public static class ProfileMigrator
{
    public const int CurrentVersion = 8;

    public static void MigrateIfNeeded(GdDict data)
    {
        var version = data.TryGetValue("version", out var value) ? value.AsInt32() : 0;
        if (version >= CurrentVersion)
            return;

        GD.Print(
            $"ProfileMigrator: Discarding unsupported progression fields from schema {version}."
        );
        data.Remove("campaign_progress");
        data.Remove("shared_campaign_progress");
        if (
            data.TryGetValue("meta", out var metaValue)
            && metaValue.VariantType == Variant.Type.Dictionary
        )
            metaValue.AsGodotDictionary().Remove("selected_campaign");
        data["summoner_progress"] = new GdDict();
        data["version"] = CurrentVersion;
    }
}
