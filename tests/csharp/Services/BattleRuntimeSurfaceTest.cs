namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Events;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class BattleRuntimeSurfaceTest
{
    [TestCase]
    public void AuthoredBattle_SerializesTypedSurfaceWithoutScenePath()
    {
        var battle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.ArenaEarthSprite);

        AssertThat(battle).IsNotNull();
        AssertThat(battle!.RuntimeSurface).IsEqual(BattleRuntimeSurface.DebugArena);

        var config = EventCatalog.ToDictionary(battle);
        AssertThat(config["runtime_surface"].AsString()).IsEqual("debug_arena");
        AssertThat(config.ContainsKey("scene_path")).IsFalse();
    }

    [TestCase]
    public void DebugArenaBattles_SerializeTypedDebugArenaSurfaceWithoutScenePath()
    {
        var arenaBattles = EventCatalog
            .GetAllBattles()
            .Where(battle => battle.RuntimeSurface == BattleRuntimeSurface.DebugArena)
            .ToArray();
        var expectedArenaIds = EventCatalog.GetAllEventIds().ToHashSet();

        AssertThat(arenaBattles.Select(battle => battle.Id).ToHashSet().SetEquals(expectedArenaIds))
            .IsTrue();
        foreach (var battle in arenaBattles)
        {
            var config = EventCatalog.ToDictionary(battle);
            AssertThat(config["runtime_surface"].AsString()).IsEqual("debug_arena");
            AssertThat(config.ContainsKey("scene_path")).IsFalse();
        }
    }
}
