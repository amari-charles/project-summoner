namespace Fateforged.Tests.View;

using System.IO;
using Fateforged.View.Debug.DeckSources;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class DebugArenaDeckProviderTest
{
    [TestCase]
    public void Resolve_FileBackedMode_ReturnsNonEmptyDecks()
    {
        var provider = new DebugArenaDeckProvider();
        var result = provider.Resolve(new DebugArenaDeckResolveRequest());

        AssertThat(result.PlayerDeck.Count).IsGreater(0);
        AssertThat(result.EnemyDeck.Count).IsEqual(result.PlayerDeck.Count);
        AssertThat(result.SourceTag == "debug_file" || result.SourceTag == "curated_fallback")
            .IsTrue();
    }

    [TestCase]
    public void Resolve_FileBackedMode_MissingDeckFile_FallsBackToCuratedDeck()
    {
        var provider = new DebugArenaDeckProvider("user://missing_debug_deck_for_test.json");
        var result = provider.Resolve(
            new DebugArenaDeckResolveRequest { SourceMode = DebugArenaDeckSourceMode.FileBacked }
        );

        AssertThat(result.SourceTag).IsEqual("curated_fallback");
        AssertThat(result.PlayerDeck.Count).IsGreater(0);
        AssertThat(result.EnemyDeck.Count).IsEqual(result.PlayerDeck.Count);
    }

    [TestCase]
    public void Resolve_FileBackedMode_InvalidDeckFile_FallsBackToCuratedDeck()
    {
        string filePath = "user://invalid_debug_deck_for_test.json";
        string globalPath = ProjectSettings.GlobalizePath(filePath);
        File.WriteAllText(globalPath, "{\"not\":\"an array\"}");

        try
        {
            var provider = new DebugArenaDeckProvider(filePath);
            var result = provider.Resolve(
                new DebugArenaDeckResolveRequest
                {
                    SourceMode = DebugArenaDeckSourceMode.FileBacked,
                }
            );

            AssertThat(result.SourceTag).IsEqual("curated_fallback");
            AssertThat(result.PlayerDeck.Count).IsGreater(0);
            AssertThat(result.EnemyDeck.Count).IsEqual(result.PlayerDeck.Count);
        }
        finally
        {
            if (File.Exists(globalPath))
                File.Delete(globalPath);
        }
    }

    [TestCase]
    public void Resolve_ContextMode_UsesContextDeckEntries()
    {
        var provider = new DebugArenaDeckProvider();
        var contextConfig = new Godot.Collections.Dictionary
        {
            { "dev_player_deck", BuildDeck("fire_wisp", 2) },
            { "enemy_deck", BuildDeck("water_jet", 3) },
        };

        var result = provider.Resolve(
            new DebugArenaDeckResolveRequest
            {
                SourceMode = DebugArenaDeckSourceMode.ContextThenFileThenFallback,
                ContextConfig = contextConfig,
            }
        );

        AssertThat(result.SourceTag).IsEqual("context_config");
        AssertThat(GetDeckSignature(result.PlayerDeck)).IsEqual("fire_wisp:2");
        AssertThat(GetDeckSignature(result.EnemyDeck)).IsEqual("water_jet:3");
    }

    [TestCase]
    public void Resolve_OverrideMode_PrefersOverrideDeckOverContextDeck()
    {
        var provider = new DebugArenaDeckProvider();
        var contextConfig = new Godot.Collections.Dictionary
        {
            { "dev_player_deck", BuildDeck("fire_wisp", 1) },
        };
        var overrideConfig = new Godot.Collections.Dictionary
        {
            { "dev_player_deck", BuildDeck("earth_bullet_unit", 4) },
        };

        var result = provider.Resolve(
            new DebugArenaDeckResolveRequest
            {
                SourceMode = DebugArenaDeckSourceMode.OverrideThenContextThenFileThenFallback,
                ContextConfig = contextConfig,
                OverrideConfig = overrideConfig,
            }
        );

        AssertThat(result.SourceTag).IsEqual("override_config");
        AssertThat(GetDeckSignature(result.PlayerDeck)).IsEqual("earth_bullet_unit:4");
        AssertThat(GetDeckSignature(result.EnemyDeck)).IsEqual("earth_bullet_unit:4");
    }

    [TestCase]
    public void ResolveModeFromConfig_ParsesKnownModes()
    {
        var emptyConfig = new Godot.Collections.Dictionary();
        AssertThat(DebugArenaDeckSourceModeResolver.ResolveFromConfig(emptyConfig))
            .IsEqual(DebugArenaDeckSourceMode.ContextThenFileThenFallback);

        var contextConfig = new Godot.Collections.Dictionary
        {
            { DebugArenaDeckSourceModeResolver.ConfigKey, "context" },
        };
        AssertThat(DebugArenaDeckSourceModeResolver.ResolveFromConfig(contextConfig))
            .IsEqual(DebugArenaDeckSourceMode.ContextThenFileThenFallback);

        var overrideConfig = new Godot.Collections.Dictionary
        {
            { DebugArenaDeckSourceModeResolver.ConfigKey, "override" },
        };
        AssertThat(DebugArenaDeckSourceModeResolver.ResolveFromConfig(overrideConfig))
            .IsEqual(DebugArenaDeckSourceMode.OverrideThenContextThenFileThenFallback);
    }

    private static Godot.Collections.Array BuildDeck(string catalogId, int count)
    {
        return new Godot.Collections.Array
        {
            new Godot.Collections.Dictionary
            {
                { "catalog_id", catalogId },
                { "count", count },
            },
        };
    }

    private static string GetDeckSignature(Godot.Collections.Array deck)
    {
        var entry = deck[0].AsGodotDictionary();
        string catalogId = entry.ContainsKey("catalog_id") ? entry["catalog_id"].AsString() : "";
        int count = entry.ContainsKey("count") ? entry["count"].AsInt32() : 0;
        return $"{catalogId}:{count}";
    }
}
