using Godot;
using GdArray = Godot.Collections.Array;
using GdDict = Godot.Collections.Dictionary;

namespace Fateforged.Infrastructure.Persistence;

/// <summary>
/// Migrates raw profile dictionaries from older versions to the current version.
/// Operates on Godot.Collections.Dictionary before conversion to typed objects.
/// Direct port of json_profile_repository.gd migration chain.
/// </summary>
public static class ProfileMigrator
{
    public const int CurrentVersion = 6;

    /// <summary>
    /// Safe dictionary access — Godot.Collections.Dictionary lacks GetValueOrDefault.
    /// </summary>
    private static Variant DictGet(GdDict dict, string key, Variant defaultValue)
    {
        return dict.TryGetValue(key, out var val) ? val : defaultValue;
    }

    /// <summary>
    /// Run all necessary migrations on raw profile data.
    /// Modifies the dictionary in place.
    /// </summary>
    public static void MigrateIfNeeded(GdDict data)
    {
        var versionVar = DictGet(data, "version", 0);
        var version = versionVar.AsInt32();

        if (version >= CurrentVersion)
            return;

        GD.Print($"ProfileMigrator: Migrating save from version {version} to {CurrentVersion}");

        // Run sequential migrations
        if (version < 2)
            MigrateV1ToV2(data);
        if (version < 3)
            MigrateV2ToV3(data);
        if (version < 4)
            MigrateV3ToV4(data);
        if (version < 5)
            MigrateV4ToV5(data);
        // v5 → v6: no data changes (version bump only)

        data["version"] = CurrentVersion;
    }

    /// <summary>V1→V2: Add card progression fields (level, xp, upgrades) to existing cards.</summary>
    private static void MigrateV1ToV2(GdDict data)
    {
        GD.Print("ProfileMigrator: Adding card progression fields...");
        var collectionVar = DictGet(data, "collection", new GdArray());
        if (collectionVar.VariantType != Variant.Type.Array)
            return;
        var collection = collectionVar.AsGodotArray();

        for (int i = 0; i < collection.Count; i++)
        {
            if (collection[i].VariantType != Variant.Type.Dictionary)
                continue;
            var card = collection[i].AsGodotDictionary();
            if (!card.ContainsKey("level"))
                card["level"] = 1;
            if (!card.ContainsKey("xp"))
                card["xp"] = 0;
            if (!card.ContainsKey("upgrades"))
                card["upgrades"] = new GdArray();
        }
    }

    /// <summary>V2→V3: Move onboarding battles from per-summoner to shared progress.</summary>
    private static void MigrateV2ToV3(GdDict data)
    {
        GD.Print("ProfileMigrator: Moving onboarding battles to shared progress...");

        string[] onboardingBattleIds =
        [
            "event_affinity",
            "event_first_summon",
            "first_trial",
            "charge_tutorial",
            "event_caravan_tutorial",
        ];

        // Initialize shared progress if needed
        if (!data.ContainsKey("shared_campaign_progress"))
            data["shared_campaign_progress"] = new GdDict { ["completed_battles"] = new GdArray() };

        var sharedProgress = data["shared_campaign_progress"].AsGodotDictionary();
        var sharedCompletedVar = DictGet(sharedProgress, "completed_battles", new GdArray());
        var sharedCompleted =
            sharedCompletedVar.VariantType == Variant.Type.Array
                ? sharedCompletedVar.AsGodotArray()
                : new GdArray();

        var campaignProgressVar = DictGet(data, "campaign_progress", new GdDict());
        if (campaignProgressVar.VariantType != Variant.Type.Dictionary)
            return;
        var progressDict = campaignProgressVar.AsGodotDictionary();

        foreach (var summonerIdVar in progressDict.Keys)
        {
            var summonerProgressVar = progressDict[summonerIdVar];
            if (summonerProgressVar.VariantType != Variant.Type.Dictionary)
                continue;
            var summonerDict = summonerProgressVar.AsGodotDictionary();

            var completedVar = DictGet(summonerDict, "completed_battles", new GdArray());
            if (completedVar.VariantType != Variant.Type.Array)
                continue;
            var completed = completedVar.AsGodotArray();

            foreach (var battleId in onboardingBattleIds)
            {
                if (completed.Contains(battleId))
                {
                    if (!sharedCompleted.Contains(battleId))
                        sharedCompleted.Add(battleId);
                    completed.Remove(battleId);
                }
            }

            summonerDict["completed_battles"] = completed;
        }

        sharedProgress["completed_battles"] = sharedCompleted;
        data["shared_campaign_progress"] = sharedProgress;
    }

    /// <summary>V3→V4: Add cosmetics and emotes structures.</summary>
    private static void MigrateV3ToV4(GdDict data)
    {
        GD.Print("ProfileMigrator: Adding cosmetics and emotes structures...");

        if (!data.ContainsKey("cosmetics"))
        {
            data["cosmetics"] = new GdDict
            {
                ["owned"] = new GdArray(),
                ["equipped"] = new GdDict { ["card_back"] = "", ["ui_theme"] = "" },
                ["summoner_skins"] = new GdDict(),
            };
        }

        if (!data.ContainsKey("emotes"))
        {
            data["emotes"] = new GdDict
            {
                ["owned"] = new GdArray(),
                ["equipped"] = new GdArray { "", "", "", "" },
            };
        }
    }

    /// <summary>V4→V5: Move account gold to campaign progress (campaign-scoped gold).</summary>
    private static void MigrateV4ToV5(GdDict data)
    {
        GD.Print("ProfileMigrator: Migrating to campaign-scoped gold...");

        var resourcesVar = DictGet(data, "resources", new GdDict());
        if (resourcesVar.VariantType != Variant.Type.Dictionary)
            return;
        var resources = resourcesVar.AsGodotDictionary();
        var accountGold = DictGet(resources, "gold", 0).AsInt32();

        if (accountGold <= 0)
        {
            GD.Print("ProfileMigrator: No account gold to migrate");
            return;
        }

        var campaignProgressVar = DictGet(data, "campaign_progress", new GdDict());
        if (campaignProgressVar.VariantType != Variant.Type.Dictionary)
        {
            GD.Print("ProfileMigrator: No campaign progress to migrate gold into");
            resources["gold"] = 0;
            data["resources"] = resources;
            return;
        }

        var progressDict = campaignProgressVar.AsGodotDictionary();
        var migrated = false;

        foreach (var summonerIdVar in progressDict.Keys)
        {
            var summonerProgressVar = progressDict[summonerIdVar];
            if (summonerProgressVar.VariantType != Variant.Type.Dictionary)
                continue;
            var summonerDict = summonerProgressVar.AsGodotDictionary();

            var completedVar = DictGet(summonerDict, "completed_battles", new GdArray());
            var completed =
                completedVar.VariantType == Variant.Type.Array
                    ? completedVar.AsGodotArray()
                    : new GdArray();
            if (completed.Count > 0)
            {
                summonerDict["gold"] = accountGold;
                progressDict[summonerIdVar] = summonerDict;
                migrated = true;
                GD.Print(
                    $"ProfileMigrator: Migrated {accountGold} gold to summoner '{summonerIdVar}' campaign"
                );
                break;
            }
        }

        if (!migrated)
            GD.Print(
                $"ProfileMigrator: No active campaign found - account gold ({accountGold}) will be lost"
            );

        resources["gold"] = 0;
        data["resources"] = resources;
        data["campaign_progress"] = progressDict;
    }
}
