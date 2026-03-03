using System.Collections.Generic;
using Godot;

namespace Fateforged.Session;

/// <summary>
/// Typed battle configuration. Replaces untyped dict-based config from BattleContext.
/// Built once by BattleScene at init via FromBattleContext(), then used throughout the battle.
/// </summary>
public class BattleSessionConfig
{
    // =========================================================================
    // BATTLE IDENTITY
    // =========================================================================

    public int Mode { get; set; } // BattleMode enum value from BattleContext
    public long BattleSeed { get; set; }

    // =========================================================================
    // WIN CONDITION
    // =========================================================================

    public string WinCondition { get; set; } = "destroy_base";
    public float TimeLimit { get; set; }
    public int KillTarget { get; set; }

    // =========================================================================
    // ENEMY CONFIG
    // =========================================================================

    public float EnemyHp { get; set; }
    public bool HasEventSequence { get; set; }

    /// <summary>Raw battle_config dict — still needed by deck loading and summoner init.</summary>
    public Godot.Collections.Dictionary RawConfig { get; set; } = new();

    // =========================================================================
    // AI CONFIG
    // =========================================================================

    public string AiType { get; set; } = "heuristic";
    public string AiPersonality { get; set; } = "balanced";
    public int AiDifficulty { get; set; } = 3;
    public float AiIntervalMin { get; set; } = 3.0f;
    public float AiIntervalMax { get; set; } = 6.0f;
    public Godot.Collections.Array? AiScript { get; set; }

    // =========================================================================
    // MULTIPLAYER
    // =========================================================================

    public bool IsMultiplayer { get; set; }
    public bool HasAuthority { get; set; } = true;

    // =========================================================================
    // POST-BATTLE
    // =========================================================================

    /// <summary>Callable from BattleContext to invoke on game end.</summary>
    public Callable CompletionCallback { get; set; }

    /// <summary>Reference to BattleContext node for lifecycle calls (start/end battle).</summary>
    public Node? BattleContextNode { get; set; }

    // =========================================================================
    // FACTORY
    // =========================================================================

    /// <summary>
    /// Build a typed config from the GDScript BattleContext autoload.
    /// Reads all needed values once, eliminating repeated Call() invocations.
    /// </summary>
    public static BattleSessionConfig FromBattleContext(Node battleContext)
    {
        var config = (Godot.Collections.Dictionary?)battleContext.Get("battle_config")
                     ?? new Godot.Collections.Dictionary();

        var cfg = new BattleSessionConfig
        {
            Mode = (int)battleContext.Get("current_mode"),
            BattleSeed = (long)config.GetValueOrDefault("battle_seed", 0),
            BattleContextNode = battleContext,
            RawConfig = config,

            // Win condition
            WinCondition = config.GetValueOrDefault("win_condition", "").ToString() is { Length: > 0 } wc
                ? wc : "destroy_base",
            TimeLimit = (float)config.GetValueOrDefault("time_limit", 0.0f),
            KillTarget = (int)config.GetValueOrDefault("kill_target", 0),

            // Enemy
            EnemyHp = (float)config.GetValueOrDefault("enemy_hp", 0.0f),
            HasEventSequence = config.ContainsKey("event_sequence"),

            // Multiplayer
            IsMultiplayer = (bool)battleContext.Call("is_multiplayer_battle"),
            HasAuthority = (bool)battleContext.Call("has_authority"),

            // Completion callback
            CompletionCallback = battleContext.Get("completion_callback").AsCallable(),
        };

        // AI config
        cfg.AiType = config.GetValueOrDefault("ai_type", "heuristic").ToString()!;
        cfg.AiPersonality = config.GetValueOrDefault("ai_personality", "balanced").ToString()!;
        cfg.AiDifficulty = (int)config.GetValueOrDefault("ai_difficulty", 3);

        var aiConfigVar = config.GetValueOrDefault("ai_config", default);
        if (aiConfigVar.VariantType == Variant.Type.Dictionary)
        {
            var aiCfg = aiConfigVar.AsGodotDictionary();
            cfg.AiIntervalMin = (float)aiCfg.GetValueOrDefault("play_interval_min", 3.0f);
            cfg.AiIntervalMax = (float)aiCfg.GetValueOrDefault("play_interval_max", 6.0f);
        }

        var scriptVar = config.GetValueOrDefault("ai_script", default);
        if (scriptVar.VariantType == Variant.Type.Array)
            cfg.AiScript = scriptVar.AsGodotArray();

        return cfg;
    }

    /// <summary>
    /// Build a minimal config for practice/test mode when BattleContext is unconfigured.
    /// </summary>
    public static BattleSessionConfig ForPractice()
    {
        return new BattleSessionConfig
        {
            Mode = 4, // PRACTICE
            WinCondition = "destroy_base",
            AiType = "scripted",
            HasAuthority = true,
        };
    }

    /// <summary>Whether this is a multiplayer client (not the host).</summary>
    public bool IsMpClient => IsMultiplayer && !HasAuthority;
}
