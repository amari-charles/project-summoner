using Fateforged.Simulation;
using Godot;

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
                { "dev_player_deck", new Godot.Collections.Array
                    {
                        new Godot.Collections.Dictionary { { "catalog_id", "fire_wisp" }, { "count", 30 } }
                    }
                },
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

        // Skip 30-second prep phase so AI starts immediately in test scenes
        PreparationDuration = 0f;

        // Call parent _Ready (runs full init sequence)
        base._Ready();

        // Wait one frame for init to complete, then override HP in MatchState
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var simNode = SimulationNode.Current;
        if (simNode != null)
        {
            // Set infinite HP directly in MatchState
            foreach (var summoner in simNode.State.Summoners)
            {
                summoner.MaxHp = 999999.0f;
                summoner.CurrentHp = 999999.0f;
            }
            GD.Print("[TestBattleScene] All summoners set to infinite HP");
        }

        GD.Print("[TestBattleScene] Test mode ready!");
    }

    public override void _Process(double delta)
    {
        // Grant infinite mana every frame by writing directly to MatchState
        var simNode = SimulationNode.Current;
        if (simNode != null)
        {
            foreach (var summoner in simNode.State.Summoners)
            {
                if (summoner.Mana < 900)
                    summoner.Mana = 999.0f;
            }
        }

        // Skip parent _Process to disable time limit polling
    }

    /// <summary>
    /// Override EndGame to pause without triggering completion callback (no scene transition).
    /// </summary>
    public new void EndGame(int winnerTeam)
    {
        if (CurrentState == GameState.GameOver)
            return;

        CurrentState = GameState.GameOver;
        EmitSignal(SignalName.StateChanged, (int)CurrentState);
        EmitSignal(SignalName.GameEnded, winnerTeam);
        GetTree().Paused = true;

        string winner = winnerTeam == 0 ? "Player" : "Enemy";
        GD.Print($"[TestBattleScene] Game ended - Winner: {winner}");
        GD.Print("[TestBattleScene] Restart scene (F5) to test again");
    }

    /// <summary>
    /// Spawn a test enemy unit via SimulationNode.QueueSpawnUnit.
    /// </summary>
    public void SpawnTestEnemy(string catalogId)
    {
        GD.Print($"[TestBattleScene] Spawning test enemy: {catalogId}");

        var simNode = SimulationNode.Current;
        if (simNode == null)
        {
            GD.PushError("[TestBattleScene] No SimulationNode available");
            return;
        }

        var spawnPos = new Vector3(5.0f, 0.0f, 0.0f);
        simNode.QueueSpawnUnit(catalogId, 1, spawnPos, true, null);
    }
}
