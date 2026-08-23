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
    public void StandardBattle_SerializesTypedStandardSurfaceWithoutScenePath()
    {
        var battle = EventCatalog.GetEvent<BattleEventDefinition>(EventIds.FirstTrial);

        AssertThat(battle).IsNotNull();
        AssertThat(battle!.RuntimeSurface).IsEqual(BattleRuntimeSurface.Standard);

        var config = EventCatalog.ToDictionary(battle);
        AssertThat(config["runtime_surface"].AsString()).IsEqual("standard");
        AssertThat(config.ContainsKey("scene_path")).IsFalse();
    }

    [TestCase]
    public void DebugArenaBattles_SerializeTypedDebugArenaSurfaceWithoutScenePath()
    {
        var arenaBattles = EventCatalog
            .GetAllBattles()
            .Where(battle => battle.RuntimeSurface == BattleRuntimeSurface.DebugArena)
            .ToArray();
        var expectedArenaIds = new HashSet<EventId>
        {
            EventIds.ArenaWindEarthNewCards,
            EventIds.ArenaAllUnits,
            EventIds.ArenaAllCards,
            EventIds.ArenaAllSpells,
            EventIds.ArenaSpriteUnits,
            EventIds.DebugArena,
        };

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
