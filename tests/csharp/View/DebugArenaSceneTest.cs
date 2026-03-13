namespace Fateforged.Tests.View;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using Fateforged.View;
using Fateforged.View.Debug;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class DebugArenaSceneTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    [TestCase]
    public void OnEnemyAiToggled_ConfiguresAndDisablesEnemyAi()
    {
        var simNode = CreateSimulationNode();
        var arena = CreateArenaNode();

        arena.OnEnemyAiToggled(true);

        var enemySummoner = simNode.State.Summoners[1];
        AssertThat(enemySummoner.Ai).IsNotNull();
        AssertThat(enemySummoner.Ai!.Type).IsEqual(AiType.Heuristic);
        AssertThat(enemySummoner.Ai.Personality).IsEqual(AiPersonality.Balanced);

        arena.OnEnemyAiToggled(false);
        AssertThat(enemySummoner.Ai).IsNull();
    }

    [TestCase]
    public void OnPlayerAiToggled_ConfiguresAndDisablesPlayerAi()
    {
        var simNode = CreateSimulationNode();
        var arena = CreateArenaNode();

        arena.OnPlayerAiToggled(true);

        var playerSummoner = simNode.State.Summoners[0];
        AssertThat(playerSummoner.Ai).IsNotNull();
        AssertThat(playerSummoner.Ai!.Type).IsEqual(AiType.Heuristic);
        AssertThat(playerSummoner.Ai.Personality).IsEqual(AiPersonality.Balanced);

        arena.OnPlayerAiToggled(false);
        AssertThat(playerSummoner.Ai).IsNull();
    }

    [TestCase]
    public void ClearAllUnits_ClearsSimulationStateAndQueuesVisualsForDeletion()
    {
        var simNode = CreateSimulationNode();
        SeedState(simNode.State);

        var arena = CreateArenaNode();
        var entityManager = new EntityManager { Name = "EntityManager" };
        var unitVisual = new UnitVisual();
        var projectileVisual = new ProjectileVisual();
        entityManager.AddChild(unitVisual);
        entityManager.AddChild(projectileVisual);
        arena.AddChild(entityManager);

        arena.ClearAllUnits();

        AssertThat(simNode.State.Units.Count).IsEqual(0);
        AssertThat(simNode.State.Projectiles.Count).IsEqual(0);
        AssertThat(simNode.State.DelayedEffects.Count).IsEqual(1);
        AssertThat(simNode.State.PendingCommandBuffer.Count).IsEqual(1);

        foreach (var summoner in simNode.State.Summoners)
        {
            AssertThat(summoner.IsCasting).IsTrue();
            AssertThat(summoner.CastingTimeRemaining).IsEqual(1.0f);
            AssertThat(summoner.CastingCardIndex).IsEqual(2);
            AssertThat(summoner.CastingCatalogId.Value).IsEqual("fire_wisp");
            AssertThat(summoner.CastingNetworkId).IsEqual(123);
        }

        bool unitFreedOrQueued =
            !GodotObject.IsInstanceValid(unitVisual) || unitVisual.IsQueuedForDeletion();
        bool projectileFreedOrQueued =
            !GodotObject.IsInstanceValid(projectileVisual)
            || projectileVisual.IsQueuedForDeletion();
        AssertThat(unitFreedOrQueued).IsTrue();
        AssertThat(projectileFreedOrQueued).IsTrue();
    }

    [TestCase]
    public void BuildPracticeConfig_UsesDebugDeckForPlayerAndEnemy()
    {
        var arena = CreateArenaNode();
        var config = arena.BuildPracticeConfigPublic();

        var playerDeck = config["dev_player_deck"].AsGodotArray();
        var enemyDeck = config["enemy_deck"].AsGodotArray();
        var expectedDeck = LoadDebugDeckEntries();

        AssertThat(playerDeck.Count).IsEqual(expectedDeck.Count);
        AssertThat(enemyDeck.Count).IsEqual(expectedDeck.Count);

        AssertThat(DeckSignatures(playerDeck))
            .ContainsExactly(DeckSignatures(expectedDeck).ToArray());
        AssertThat(DeckSignatures(enemyDeck))
            .ContainsExactly(DeckSignatures(expectedDeck).ToArray());
    }

    private TestDebugArenaScene CreateArenaNode()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var arena = new TestDebugArenaScene { Name = $"DebugArenaSceneTest_{_createdNodes.Count}" };
        root.AddChild(arena);
        _createdNodes.Add(arena);
        return arena;
    }

    private SimulationNode CreateSimulationNode()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var simNode = new SimulationNode { Name = $"SimulationNodeTest_{_createdNodes.Count}" };
        root.AddChild(simNode);
        _createdNodes.Add(simNode);
        return simNode;
    }

    private static void SeedState(MatchState state)
    {
        state.Units[1] = new UnitData
        {
            UnitId = 1,
            Team = Team.Player,
            IsAlive = true,
            ActivationState = ActivationState.Active,
            Position = new SimVector3(0f, 0f, 0f),
        };

        state.Projectiles[1] = new SimProjectileData { ProjectileId = 1 };
        state.DelayedEffects.Add(
            new DelayedEffect
            {
                Timer = 0.5f,
                EffectType = EffectType.Damage,
                Value = 1f,
                DamageType = DamageType.Physical,
                AoeRadius = 0f,
                Position = SimVector3.Zero,
                SourceUnitId = 1,
                SourceTeam = Team.Player,
            }
        );
        state.PendingCommandBuffer.Add(new SpawnUnitCommand("fire_wisp", 0, SimVector3.Zero));

        foreach (var summoner in state.Summoners)
        {
            summoner.IsCasting = true;
            summoner.CastingTimeRemaining = 1.0f;
            summoner.CastingTimeTotal = 1.0f;
            summoner.CastingCardIndex = 2;
            summoner.CastingCatalogId = "fire_wisp";
            summoner.CastingSpawnPosition = new SimVector3(1f, 0f, 0f);
            summoner.CastingNetworkId = 123;
        }
    }

    private static Godot.Collections.Array LoadDebugDeckEntries()
    {
        using var file = FileAccess.Open(
            "res://data/debug/debug_deck.json",
            FileAccess.ModeFlags.Read
        );
        AssertThat(file).IsNotNull();

        var parsed = Json.ParseString(file!.GetAsText());
        AssertThat(parsed.VariantType).IsEqual(Variant.Type.Array);
        return parsed.AsGodotArray();
    }

    private static IEnumerable<string> DeckSignatures(Godot.Collections.Array deck)
    {
        foreach (var item in deck)
        {
            var entry = item.AsGodotDictionary();
            string catalogId = entry.GetValueOrDefault("catalog_id", "").AsString();
            int count = entry.GetValueOrDefault("count", 0).AsInt32();
            yield return $"{catalogId}:{count}";
        }
    }

    private sealed partial class TestDebugArenaScene : DebugArenaScene
    {
        public override async void _Ready()
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }

        public Godot.Collections.Dictionary BuildPracticeConfigPublic() => BuildPracticeConfig();
    }
}
