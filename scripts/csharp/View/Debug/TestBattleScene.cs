using Godot;
using Fateforged.View;

namespace Fateforged.View.Debug;

/// <summary>
/// Test override for BattleScene — infinite mana, infinite HP, hardcoded test deck.
/// Replaces TestGameController (GDScript) for dev test scenes.
/// </summary>
[GlobalClass]
public partial class TestBattleScene : BattleScene
{
    public override async void _Ready()
    {
        // Configure BattleContext for practice mode before parent _Ready
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null)
        {
            var config = new Godot.Collections.Dictionary
            {
                { "enemy_deck", new Godot.Collections.Array
                    {
                        new Godot.Collections.Dictionary { { "catalog_id", "fire_wisp" }, { "count", 30 } }
                    }
                },
                { "enemy_hp", 999999.0 }
            };
            battleContext.Call("configure_practice_battle", config);
        }

        // Force reload ProjectileCatalog
        var projectileCatalog = GetNodeOrNull("/root/ProjectileCatalog");
        if (projectileCatalog != null && projectileCatalog.HasMethod("reload_projectiles"))
        {
            projectileCatalog.Call("reload_projectiles");
            GD.Print("[TestBattleScene] Reloaded projectile data from disk");
        }

        // Call parent _Ready (runs full init sequence)
        base._Ready();

        // Wait one frame for init to complete, then override HP
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (PlayerSummoner != null)
        {
            PlayerSummoner.Set("max_hp", 999999.0f);
            PlayerSummoner.Set("current_hp", 999999.0f);
            GD.Print("[TestBattleScene] Player summoner set to infinite HP");
        }

        if (EnemySummoner != null)
        {
            EnemySummoner.Set("max_hp", 999999.0f);
            EnemySummoner.Set("current_hp", 999999.0f);
            GD.Print("[TestBattleScene] Enemy summoner set to infinite HP");
        }

        GD.Print("[TestBattleScene] Test mode ready!");
    }

    public override void _Process(double delta)
    {
        // Grant infinite mana every frame
        if (PlayerSummoner != null)
        {
            var mana = (float)PlayerSummoner.Get("mana");
            if (mana < 900)
                PlayerSummoner.Set("mana", 999.0f);
        }

        if (EnemySummoner != null)
        {
            var mana = (float)EnemySummoner.Get("mana");
            if (mana < 900)
                EnemySummoner.Set("mana", 999.0f);
        }

        // Skip parent _Process to disable time limit polling
    }

    /// <summary>
    /// Override EndGame to pause without triggering completion callback (no scene transition).
    /// </summary>
    public new void EndGame(int winnerTeam)
    {
        if (CurrentState == (int)GameState.GameOver)
            return;

        CurrentState = (int)GameState.GameOver;
        EmitSignal(SignalName.StateChanged, CurrentState);
        EmitSignal(SignalName.GameEnded, winnerTeam);
        GetTree().Paused = true;

        string winner = winnerTeam == 0 ? "Player" : "Enemy";
        GD.Print($"[TestBattleScene] Game ended - Winner: {winner}");
        GD.Print("[TestBattleScene] Restart scene (F5) to test again");
    }

    /// <summary>
    /// Spawn a test enemy unit. Called by spawn buttons in test_battle_abilities.tscn.
    /// </summary>
    public void SpawnTestEnemy(string catalogId)
    {
        GD.Print($"[TestBattleScene] Spawning test enemy: {catalogId}");

        var cardCatalog = GetNodeOrNull("/root/CardCatalog");
        if (cardCatalog == null) return;

        var card = cardCatalog.Call("create_card_resource", catalogId);
        if (card.VariantType == Variant.Type.Nil)
        {
            GD.PushError($"[TestBattleScene] Failed to load card: {catalogId}");
            return;
        }

        if (EnemySummoner == null)
        {
            GD.PushError("[TestBattleScene] No enemy summoner available");
            return;
        }

        // Add card to enemy hand and play it
        var hand = (Godot.Collections.Array)EnemySummoner.Get("hand");
        hand.Add(card);
        int cardIndex = hand.Count - 1;
        var spawnPos = new Vector3(5.0f, 1.0f, 0.0f);

        EnemySummoner.Call("play_card_3d", cardIndex, spawnPos);
    }
}
