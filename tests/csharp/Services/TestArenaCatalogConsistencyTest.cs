namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fateforged.Data.Events;
using Fateforged.Meta.Campaign;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class TestArenaCatalogConsistencyTest
{
    private const string PresetCatalogPath = "res://data/debug/debug_arena_menu_presets.json";

    [TestCase]
    public void TestArenaCampaign_AllTestArenaPreset_MatchesCampaignEventIds()
    {
        var campaign = CampaignCatalog.GetCampaign(CampaignIds.TestArena);
        AssertThat(campaign).IsNotNull();

        var campaignEventIds = campaign!.EventIds.Select(id => id.Value).ToHashSet();
        var presetEntries = LoadPresetEntries("all_test_arena");
        var presetBattleIds = presetEntries.Select(entry => entry.BattleId).ToHashSet();

        AssertThat(presetBattleIds.Count).IsEqual(campaignEventIds.Count);
        AssertThat(presetBattleIds.SetEquals(campaignEventIds)).IsTrue();
    }

    [TestCase]
    public void TestArenaCampaign_AllPresetBattleIds_ResolveEventCatalog()
    {
        var entries = LoadPresetEntries("all_test_arena");
        AssertThat(entries.Count).IsGreater(0);

        foreach (var entry in entries)
        {
            var eventId = EventId.FromString(entry.BattleId);
            AssertThat(EventCatalog.HasEvent(eventId)).IsTrue();
        }
    }

    [TestCase]
    public void TestArenaCampaign_NewCardsOnlyPreset_ContainsExpectedStableSubset()
    {
        var entries = LoadPresetEntries("new_cards_only");
        var ids = entries.Select(entry => entry.BattleId).ToHashSet();

        AssertThat(ids.Count).IsEqual(2);
        AssertThat(ids.Contains(EventIds.ArenaWindEarthNewCards.Value)).IsTrue();
        AssertThat(ids.Contains(EventIds.ArenaFireWisp.Value)).IsTrue();
    }

    private static List<PresetEntry> LoadPresetEntries(string presetId)
    {
        var catalog = JsonDocument.Parse(File.ReadAllText(ProjectSettings.GlobalizePath(PresetCatalogPath)));
        var root = catalog.RootElement;
        if (!root.TryGetProperty("presets", out var presets) || presets.ValueKind != JsonValueKind.Array)
            return new List<PresetEntry>();

        foreach (var preset in presets.EnumerateArray())
        {
            if (!preset.TryGetProperty("id", out var idElement))
                continue;
            if (idElement.GetString() != presetId)
                continue;

            var entries = new List<PresetEntry>();
            if (
                !preset.TryGetProperty("entries", out var entriesElement)
                || entriesElement.ValueKind != JsonValueKind.Array
            )
                return entries;

            foreach (var entry in entriesElement.EnumerateArray())
            {
                string battleId = entry.TryGetProperty("battle_id", out var battleIdElement)
                    ? battleIdElement.GetString() ?? ""
                    : "";
                string label = entry.TryGetProperty("label", out var labelElement)
                    ? labelElement.GetString() ?? battleId
                    : battleId;

                if (string.IsNullOrWhiteSpace(battleId))
                    continue;

                entries.Add(new PresetEntry(battleId, label));
            }

            return entries;
        }

        return new List<PresetEntry>();
    }

    private readonly record struct PresetEntry(string BattleId, string Label);
}
