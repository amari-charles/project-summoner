namespace Fateforged.Tests.View;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Movement;
using Fateforged.Units;
using Fateforged.View;
using Fateforged.View.Debug.DeckSources;
using Fateforged.View.Debug;
using Fateforged.View.Debug.SpawnerPanel;
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
        SimMovement.DebugHoldPlayerAdvanceEnabled = false;
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
    public void OnPlayerHoldAdvanceToggled_ConfiguresDebugMovementHold()
    {
        var arena = CreateArenaNode();

        arena.OnPlayerHoldAdvanceToggled(true);
        AssertThat(SimMovement.DebugHoldPlayerAdvanceEnabled).IsTrue();

        arena.OnPlayerHoldAdvanceToggled(false);
        AssertThat(SimMovement.DebugHoldPlayerAdvanceEnabled).IsFalse();
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
    public void UndoLastSpawnBatch_RemovesTrackedUnitsInsteadOfLatestTeamUnits()
    {
        var simNode = CreateSimulationNode();
        var arena = CreateArenaNode();

        simNode.State.Units.Clear();
        simNode.State.Units[10] = CreateUnitData(10, Team.Enemy);
        simNode.State.Units[11] = CreateUnitData(11, Team.Enemy);
        simNode.State.Units[12] = CreateUnitData(12, Team.Enemy);

        arena.RegisterDebugSpawnBatch(1, 2, "Enemy Batch", [10, 11]);

        // Simulate enemy AI spawning after the manual batch.
        simNode.State.Units[99] = CreateUnitData(99, Team.Enemy);

        arena.UndoLastSpawnBatch();

        AssertThat(simNode.State.Units.ContainsKey(10)).IsFalse();
        AssertThat(simNode.State.Units.ContainsKey(11)).IsFalse();
        AssertThat(simNode.State.Units.ContainsKey(12)).IsTrue();
        AssertThat(simNode.State.Units.ContainsKey(99)).IsTrue();
    }

    [TestCase]
    public void ClearTeamUnits_RemovesOnlyRequestedTeamUnitsAndProjectiles()
    {
        var simNode = CreateSimulationNode();
        var arena = CreateArenaNode();

        simNode.State.Units.Clear();
        simNode.State.Projectiles.Clear();
        simNode.State.Units[1] = CreateUnitData(1, Team.Player);
        simNode.State.Units[2] = CreateUnitData(2, Team.Enemy);
        simNode.State.Projectiles[101] = new SimProjectileData
        {
            ProjectileId = 101,
            Team = Team.Player,
        };
        simNode.State.Projectiles[202] = new SimProjectileData
        {
            ProjectileId = 202,
            Team = Team.Enemy,
        };

        arena.ClearTeamUnits(1);

        AssertThat(simNode.State.Units.ContainsKey(1)).IsTrue();
        AssertThat(simNode.State.Units.ContainsKey(2)).IsFalse();
        AssertThat(simNode.State.Projectiles.ContainsKey(101)).IsTrue();
        AssertThat(simNode.State.Projectiles.ContainsKey(202)).IsFalse();
    }

    [TestCase]
    public void BuildPracticeConfig_FileMode_UsesDebugDeckForPlayerAndEnemy()
    {
        var arena = CreateArenaNode();
        arena.ContextConfigOverride = new Godot.Collections.Dictionary
        {
            { "debug_arena_deck_source", "file" },
        };
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

    [TestCase]
    public void BuildPracticeConfig_UsesDeckProviderResolution()
    {
        var provider = new StubDeckProvider(
            new DebugArenaDeckResolution(
                BuildDeck("wind_evasion_tank", 5),
                BuildDeck("earth_bullet_unit", 6),
                "stub"
            )
        );
        var arena = CreateArenaNode();
        arena.DeckProviderOverride = provider;

        var config = arena.BuildPracticeConfigPublic();
        var playerDeck = config["dev_player_deck"].AsGodotArray();
        var enemyDeck = config["enemy_deck"].AsGodotArray();

        AssertThat(provider.Calls).IsEqual(1);
        AssertThat(GetDeckSignature(playerDeck)).IsEqual("wind_evasion_tank:5");
        AssertThat(GetDeckSignature(enemyDeck)).IsEqual("earth_bullet_unit:6");
    }

    [TestCase]
    public void BuildPracticeConfig_DefaultMode_UsesContextSourceModeInDeckRequest()
    {
        var provider = new StubDeckProvider(
            new DebugArenaDeckResolution(
                BuildDeck("fire_wisp", 1),
                BuildDeck("fire_wisp", 1),
                "stub"
            )
        );
        var arena = CreateArenaNode();
        arena.DeckProviderOverride = provider;
        arena.ContextConfigOverride = new Godot.Collections.Dictionary
        {
            { "dev_player_deck", BuildDeck("wind_evasion_tank", 2) },
        };

        _ = arena.BuildPracticeConfigPublic();

        AssertThat(provider.Calls).IsEqual(1);
        AssertThat(provider.LastRequest.SourceMode)
            .IsEqual(DebugArenaDeckSourceMode.ContextThenFileThenFallback);
    }

    [TestCase]
    public void BuildPracticeConfig_ContextMode_UsesContextSourceModeInDeckRequest()
    {
        var provider = new StubDeckProvider(
            new DebugArenaDeckResolution(
                BuildDeck("fire_wisp", 1),
                BuildDeck("fire_wisp", 1),
                "stub"
            )
        );
        var arena = CreateArenaNode();
        arena.DeckProviderOverride = provider;
        arena.ContextConfigOverride = new Godot.Collections.Dictionary
        {
            { "debug_arena_deck_source", "context" },
            { "dev_player_deck", BuildDeck("wind_evasion_tank", 2) },
        };

        _ = arena.BuildPracticeConfigPublic();

        AssertThat(provider.Calls).IsEqual(1);
        AssertThat(provider.LastRequest.SourceMode)
            .IsEqual(DebugArenaDeckSourceMode.ContextThenFileThenFallback);
    }

    [TestCase]
    public void ConnectSpawnerPanel_SyncsResolvedDeckEntriesToPanelBridge()
    {
        var provider = new StubDeckProvider(
            new DebugArenaDeckResolution(
                BuildDeck("wind_evasion_tank", 3),
                BuildDeck("earth_bullet_unit", 2),
                "stub"
            )
        );
        var bridge = new StubSpawnerPanelBridge();
        var arena = CreateArenaNode();
        arena.DeckProviderOverride = provider;
        arena.BridgeOverride = bridge;

        var config = arena.BuildPracticeConfigPublic();
        arena.ConnectSpawnerPanelPublic();

        var playerDeck = config["dev_player_deck"].AsGodotArray();
        AssertThat(bridge.SetDeckEntriesCalls).IsEqual(1);
        AssertThat(DeckSignatures(bridge.LastDeckEntries))
            .ContainsExactly(DeckSignatures(playerDeck).ToArray());
    }

    [TestCase]
    public void ConnectSpawnerPanel_ContextMode_SyncsContextDeckEntriesToPanelBridge()
    {
        var bridge = new StubSpawnerPanelBridge();
        var arena = CreateArenaNode();
        arena.BridgeOverride = bridge;
        arena.ContextConfigOverride = new Godot.Collections.Dictionary
        {
            { "debug_arena_deck_source", "context" },
            { "dev_player_deck", BuildDeck("wind_cleave_unit", 4) },
            { "enemy_deck", BuildDeck("fire_wisp", 1) },
        };

        var config = arena.BuildPracticeConfigPublic();
        arena.ConnectSpawnerPanelPublic();

        var playerDeck = config["dev_player_deck"].AsGodotArray();
        AssertThat(GetDeckSignature(playerDeck)).IsEqual("wind_cleave_unit:4");
        AssertThat(bridge.SetDeckEntriesCalls).IsEqual(1);
        AssertThat(GetDeckSignature(bridge.LastDeckEntries)).IsEqual("wind_cleave_unit:4");
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

    private static UnitData CreateUnitData(int unitId, Team team)
    {
        return new UnitData
        {
            UnitId = unitId,
            Team = team,
            IsAlive = true,
            ActivationState = ActivationState.Active,
            Position = new SimVector3(0f, 0f, 0f),
        };
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

    private static Godot.Collections.Array BuildDeck(string catalogId, int count)
    {
        return new Godot.Collections.Array
        {
            new Godot.Collections.Dictionary { { "catalog_id", catalogId }, { "count", count } },
        };
    }

    private static string GetDeckSignature(Godot.Collections.Array deck)
    {
        var entry = deck[0].AsGodotDictionary();
        string catalogId = entry.GetValueOrDefault("catalog_id", "").AsString();
        int count = entry.GetValueOrDefault("count", 0).AsInt32();
        return $"{catalogId}:{count}";
    }

    private sealed partial class TestDebugArenaScene : DebugArenaScene
    {
        public IDebugArenaDeckProvider? DeckProviderOverride { get; set; }
        public Godot.Collections.Dictionary? ContextConfigOverride { get; set; }
        public IDebugArenaSpawnerPanelBridge? BridgeOverride { get; set; }

        public override async void _Ready()
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }

        protected override IDebugArenaDeckProvider CreateDeckProvider()
        {
            return DeckProviderOverride ?? base.CreateDeckProvider();
        }

        protected override Godot.Collections.Dictionary ReadBattleContextConfig()
        {
            return ContextConfigOverride ?? base.ReadBattleContextConfig();
        }

        protected override IDebugArenaSpawnerPanelBridge? ResolveSpawnerPanelBridge()
        {
            return BridgeOverride ?? base.ResolveSpawnerPanelBridge();
        }

        public Godot.Collections.Dictionary BuildPracticeConfigPublic() => BuildPracticeConfig();

        public void ConnectSpawnerPanelPublic() => ConnectSpawnerPanel();
    }

    private sealed class StubDeckProvider : IDebugArenaDeckProvider
    {
        private readonly DebugArenaDeckResolution _resolution;
        public int Calls { get; private set; }
        public DebugArenaDeckResolveRequest LastRequest { get; private set; } =
            new DebugArenaDeckResolveRequest();

        public StubDeckProvider(DebugArenaDeckResolution resolution)
        {
            _resolution = resolution;
        }

        public DebugArenaDeckResolution Resolve(DebugArenaDeckResolveRequest request)
        {
            Calls++;
            LastRequest = request;
            return _resolution;
        }
    }

    private sealed class StubSpawnerPanelBridge : IDebugArenaSpawnerPanelBridge
    {
        public Node PanelNode { get; } = new Node();

        public int SetDeckEntriesCalls { get; private set; }
        public Godot.Collections.Array LastDeckEntries { get; private set; } = new();

        public bool ConnectClearRequested(Callable handler) => true;

        public bool ConnectSkipPrepToggled(Callable handler) => true;

        public bool ConnectEnemyAiToggled(Callable handler) => true;

        public bool ConnectPlayerAiToggled(Callable handler) => true;

        public bool ConnectPlayerHoldAdvanceToggled(Callable handler) => true;

        public bool ConnectClearTeamRequested(Callable handler) => true;

        public bool ConnectUndoRequested(Callable handler) => true;

        public bool GetSkipPrepPhase() => false;

        public bool GetEnemyAiEnabled() => false;

        public bool GetPlayerAiEnabled() => false;

        public bool GetPlayerHoldAdvanceEnabled() => false;

        public void AppendSpawnLog(string message) { }

        public void SetDebugDeckEntries(Godot.Collections.Array deckEntries)
        {
            SetDeckEntriesCalls++;
            LastDeckEntries = (Godot.Collections.Array)deckEntries.Duplicate(true);
        }
    }
}
