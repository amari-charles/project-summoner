namespace Fateforged.Tests.Serialization;

using Fateforged.Infrastructure.Persistence;
using GdUnit4;
using Godot.Collections;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class ItemOwnershipMigrationTest
{
    [TestCase]
    public void V6EquippedNormalItemRecoversOwnerFromEquipmentProvenance()
    {
        var item = new Dictionary
        {
            ["id"] = "legacy_item",
            ["catalog_id"] = "item_training_blade",
            ["equipped_by"] = "summoner_cole",
            ["bound_to"] = "",
            ["slot"] = "wand",
        };
        var profile = new Dictionary { ["version"] = 6, ["items"] = new Array { item } };

        ProfileMigrator.MigrateIfNeeded(profile);

        AssertThat(profile["version"].AsInt32()).IsEqual(7);
        AssertThat(item["bound_to"].AsString()).IsEqual("summoner_cole");
    }

    [TestCase]
    public void V6AmbiguousNormalItemIsPreservedUnassigned()
    {
        var item = new Dictionary
        {
            ["id"] = "legacy_item",
            ["catalog_id"] = "item_training_blade",
            ["equipped_by"] = "",
            ["bound_to"] = "",
        };
        var profile = new Dictionary { ["version"] = 6, ["items"] = new Array { item } };

        ProfileMigrator.MigrateIfNeeded(profile);

        AssertThat(item["bound_to"].AsString()).IsEmpty();
        AssertThat(profile["items"].AsGodotArray()).HasSize(1);
    }

    [TestCase]
    public void V6ExplicitSharedEventItemRemainsAccountWide()
    {
        var item = new Dictionary
        {
            ["id"] = "legacy_shared",
            ["catalog_id"] = "item_test_shared_event",
            ["equipped_by"] = "",
            ["bound_to"] = "",
        };
        var profile = new Dictionary { ["version"] = 6, ["items"] = new Array { item } };

        ProfileMigrator.MigrateIfNeeded(profile);

        AssertThat(item["bound_to"].AsString()).IsEmpty();
    }
}
