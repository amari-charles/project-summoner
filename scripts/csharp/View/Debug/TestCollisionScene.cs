using Godot;
using Fateforged.Constants;
using Fateforged.View;

namespace Fateforged.View.Debug;

/// <summary>
/// Collision test scene — spawns player/enemy units via buttons to test collision &amp; separation.
/// Replaces test_collision_controller.gd. Used by scenes/battlefield/dev/test_collision.tscn.
/// </summary>
[GlobalClass]
public partial class TestCollisionScene : TestBattleScene
{
    private float _spawnPositionOffset;

    public override async void _Ready()
    {
        GD.Print("TestCollisionScene: Initializing collision test mode...");

        // Configure BattleContext with collision-test deck before parent init
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null)
        {
            var config = new Godot.Collections.Dictionary
            {
                { "enemy_deck", new Godot.Collections.Array
                    {
                        new Godot.Collections.Dictionary { { "catalog_id", "puff" }, { "count", 6 } },
                        new Godot.Collections.Dictionary { { "catalog_id", "fire_wisp" }, { "count", 6 } },
                        new Godot.Collections.Dictionary { { "catalog_id", "pebbloom" }, { "count", 3 } },
                        new Godot.Collections.Dictionary { { "catalog_id", "water_frog" }, { "count", 3 } },
                        new Godot.Collections.Dictionary { { "catalog_id", "mana_bolt" }, { "count", 4 } }
                    }
                },
                { "enemy_hp", 999999.0 }
            };
            battleContext.Call("configure_practice_battle", config);
        }

        base._Ready();

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        GD.Print("TestCollisionScene: Collision test mode ready!");
    }

    /// <summary>
    /// Spawn an enemy unit at a varied position. Called by button signals in test_collision.tscn.
    /// </summary>
    public void SpawnEnemyAtPosition(string catalogId)
    {
        GD.Print($"TestCollisionScene: Spawning enemy: {catalogId}");

        var card = CreateCard(catalogId);
        if (card.VariantType == Variant.Type.Nil) return;

        if (EnemySummoner == null)
        {
            GD.PushError("TestCollisionScene: No enemy summoner available");
            return;
        }

        var hand = (Godot.Collections.Array)EnemySummoner.Get("hand");
        hand.Add(card);
        int cardIndex = hand.Count - 1;
        var spawnPos = new Vector3(5.0f + _spawnPositionOffset, 1.0f, (float)GD.RandRange(-3.0, 3.0));
        _spawnPositionOffset = (_spawnPositionOffset + 1.5f) % 10.0f;

        EnemySummoner.Call("play_card_3d", cardIndex, spawnPos);
    }

    /// <summary>
    /// Spawn a cluster of enemy units at the same position. Called by button signals.
    /// </summary>
    public async void SpawnEnemyCluster(string catalogId, int count = 5)
    {
        GD.Print($"TestCollisionScene: Spawning cluster of {count} {catalogId}");
        var spawnPos = new Vector3(8.0f, 1.0f, 0.0f);

        for (int i = 0; i < count; i++)
        {
            var card = CreateCard(catalogId);
            if (card.VariantType == Variant.Type.Nil) continue;

            var hand = (Godot.Collections.Array)EnemySummoner!.Get("hand");
            hand.Add(card);
            int cardIndex = hand.Count - 1;

            EnemySummoner.Call("play_card_3d", cardIndex, spawnPos);
            GD.Print($"  - Spawned #{i + 1}");

            await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        }
    }

    /// <summary>
    /// Spawn a player unit at a varied position. Called by button signals.
    /// </summary>
    public void SpawnPlayerAtPosition(string catalogId)
    {
        GD.Print($"TestCollisionScene: Spawning player unit: {catalogId}");

        var card = CreateCard(catalogId);
        if (card.VariantType == Variant.Type.Nil) return;

        if (PlayerSummoner == null)
        {
            GD.PushError("TestCollisionScene: No player summoner available");
            return;
        }

        var hand = (Godot.Collections.Array)PlayerSummoner.Get("hand");
        hand.Add(card);
        int cardIndex = hand.Count - 1;
        var spawnPos = new Vector3(-5.0f - _spawnPositionOffset, 1.0f, (float)GD.RandRange(-3.0, 3.0));
        _spawnPositionOffset = (_spawnPositionOffset + 1.5f) % 10.0f;

        PlayerSummoner.Call("play_card_3d", cardIndex, spawnPos);
    }

    /// <summary>
    /// Spawn a cluster of player units at the same position. Called by button signals.
    /// </summary>
    public async void SpawnPlayerCluster(string catalogId, int count = 5)
    {
        GD.Print($"TestCollisionScene: Spawning player cluster of {count} {catalogId}");
        var spawnPos = new Vector3(-8.0f, 1.0f, 0.0f);

        for (int i = 0; i < count; i++)
        {
            var card = CreateCard(catalogId);
            if (card.VariantType == Variant.Type.Nil) continue;

            var hand = (Godot.Collections.Array)PlayerSummoner!.Get("hand");
            hand.Add(card);
            int cardIndex = hand.Count - 1;

            PlayerSummoner.Call("play_card_3d", cardIndex, spawnPos);
            GD.Print($"  - Spawned #{i + 1}");

            await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        }
    }

    /// <summary>
    /// Clear all units from the battlefield. Called by ClearButton signal.
    /// </summary>
    public void ClearAllUnits()
    {
        GD.Print("TestCollisionScene: Clearing all units...");
        var units = GetTree().GetNodesInGroup(GroupIDs.Units);
        int count = units.Count;

        foreach (var unit in units)
            unit.QueueFree();

        _spawnPositionOffset = 0.0f;
        GD.Print($"TestCollisionScene: Cleared {count} units");
    }

    private Variant CreateCard(string catalogId)
    {
        var cardCatalog = GetNodeOrNull("/root/CardCatalog");
        if (cardCatalog == null) return default;

        var card = cardCatalog.Call("create_card_resource", catalogId);
        if (card.VariantType == Variant.Type.Nil)
        {
            GD.PushError($"TestCollisionScene: Failed to load card: {catalogId}");
            return default;
        }
        return card;
    }
}
