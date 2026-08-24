namespace Fateforged.Tests.Services;

using System.IO;
using Fateforged.Data.Academy;
using Fateforged.Data.Encounters;
using Fateforged.Data.Quests;
using Fateforged.Infrastructure.Persistence;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class QuestEncounterCatalogIntegrationTest
{
    [TestCase]
    public void CurrentProgressionCatalogs_LoadTogether()
    {
        AssertThat(EncounterCatalog.All.Count).IsGreater(0);
        AssertThat(QuestCatalog.All.Count).IsGreater(0);
        AssertThat(AcademyProfessorCatalog.All.Count).IsEqual(5);
        AssertThat(AcademyProfessorCatalog.Find(ProfessorIds.GeneralMagic)).IsNotNull();
    }

    [TestCase]
    public void RetiredProgressionRuntimeFiles_AreAbsent()
    {
        AssertThat(Exists("res://data/academy/courses.json")).IsFalse();
        AssertThat(Exists("res://scripts/infrastructure/services/campaign_api.gd")).IsFalse();
        AssertThat(Exists("res://scripts/csharp/Meta/Services/Campaign/CampaignService.cs"))
            .IsFalse();
    }

    [TestCase]
    public void PreQuestSave_DiscardsRetiredProgressionKeys()
    {
        var meta = new Godot.Collections.Dictionary { ["selected_campaign"] = "academy" };
        var raw = new Godot.Collections.Dictionary
        {
            ["version"] = 7,
            ["campaign_progress"] = new Godot.Collections.Dictionary(),
            ["shared_campaign_progress"] = new Godot.Collections.Dictionary(),
            ["meta"] = meta,
        };

        ProfileMigrator.MigrateIfNeeded(raw);

        AssertThat(raw.ContainsKey("campaign_progress")).IsFalse();
        AssertThat(raw.ContainsKey("shared_campaign_progress")).IsFalse();
        AssertThat(meta.ContainsKey("selected_campaign")).IsFalse();
        AssertThat(raw.ContainsKey("summoner_progress")).IsTrue();
        AssertThat(raw["version"].AsInt32()).IsEqual(ProfileMigrator.CurrentVersion);
    }

    private static bool Exists(string resourcePath) =>
        File.Exists(ProjectSettings.GlobalizePath(resourcePath));
}
