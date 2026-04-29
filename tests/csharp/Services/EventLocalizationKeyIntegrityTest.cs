namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Fateforged.Data.Events;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class EventLocalizationKeyIntegrityTest
{
    [TestCase]
    public void AllEventNameAndDescriptionKeys_ResolveLocalizationEntries()
    {
        // PASS 2 skeleton for C19.
        // PASS 3 can expand this to campaign-level key validation too.
        var flattenedKeys = LoadFlattenedLocalizationKeys();
        var allEvents = EventCatalog.GetAllEvents();

        var missing = new List<string>();
        foreach (var evt in allEvents)
        {
            if (!string.IsNullOrWhiteSpace(evt.NameKey) && !flattenedKeys.Contains(evt.NameKey))
                missing.Add(evt.NameKey);
            if (
                !string.IsNullOrWhiteSpace(evt.DescriptionKey)
                && !flattenedKeys.Contains(evt.DescriptionKey)
            )
                missing.Add(evt.DescriptionKey);
        }

        AssertThat(missing.Count).IsEqual(0);
    }

    private static HashSet<string> LoadFlattenedLocalizationKeys()
    {
        var path = ProjectSettings.GlobalizePath("res://localization/data/en.json");
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);

        var keys = new HashSet<string>();
        Flatten(doc.RootElement, "", keys);
        return keys;
    }

    private static void Flatten(JsonElement element, string prefix, HashSet<string> keys)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in element.EnumerateObject())
        {
            string key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                Flatten(prop.Value, key, keys);
                continue;
            }

            keys.Add(key);
        }
    }
}
