using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Godot;

namespace Fateforged.Session;

/// <summary>
/// Battle mode — mirrors GDScript BattleContext.BattleMode enum ordinals.
/// </summary>
public enum BattleMode
{
    Campaign = 0,
    Arena = 1,
    Endless = 2,
    Tutorial = 3,
    Practice = 4,
    Multiplayer = 5,
}

/// <summary>
/// Typed battle configuration. Replaces untyped dict-based config from BattleContext.
/// Built once by BattleScene at init via FromBattleContext(), then used throughout the battle.
/// </summary>
public class BattleSessionConfig
{
    // =========================================================================
    // BATTLE IDENTITY
    // =========================================================================

    public BattleMode Mode { get; set; }
    public long BattleSeed { get; set; }

    // =========================================================================
    // WIN CONDITION
    // =========================================================================

    public WinConditionType WinCondition { get; set; } = WinConditionType.DestroySummoner;
    public float TimeLimit { get; set; }
    public int KillTarget { get; set; }

    // =========================================================================
    // ENEMY CONFIG
    // =========================================================================

    public float EnemyHp { get; set; }
    public bool HasEventSequence { get; set; }

    /// <summary>Raw battle_config dict — still needed by deck loading and summoner init.</summary>
    public Godot.Collections.Dictionary? RawConfig { get; set; }

    // =========================================================================
    // AI CONFIG
    // =========================================================================

    public AiType AiType { get; set; } = AiType.Heuristic;
    public AiPersonality AiPersonality { get; set; } = AiPersonality.Balanced;
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

    /// <summary>XP to grant to each deck card on victory.</summary>
    public int CardXpReward { get; set; }

    /// <summary>XP to grant to the active summoner on victory.</summary>
    public int SummonerXpReward { get; set; }

    /// <summary>Scene to return to after battle (campaign map, arena menu, etc.).</summary>
    public string OriginScene { get; set; } = "";

    /// <summary>Whether this is a ranked multiplayer match.</summary>
    public bool IsRankedMatch { get; set; }

    /// <summary>Ranked match info for match reporting (match_id, opponent data, etc.).</summary>
    public Godot.Collections.Dictionary? RankedMatchInfo { get; set; }

    // =========================================================================
    // FACTORY
    // =========================================================================

    /// <summary>
    /// Build a typed config from the GDScript BattleContext autoload.
    /// Reads all needed values once, eliminating repeated Call() invocations.
    /// </summary>
    public static BattleSessionConfig FromBattleContext(Node battleContext)
    {
        var config =
            (Godot.Collections.Dictionary?)battleContext.Get("battle_config")
            ?? new Godot.Collections.Dictionary();

        var cfg = new BattleSessionConfig
        {
            Mode = (BattleMode)(int)battleContext.Get("current_mode"),
            BattleSeed = (long)config.GetValueOrDefault("battle_seed", 0),
            RawConfig = config,

            // Win condition
            WinCondition = WinConditionFactory.Parse(
                config.GetValueOrDefault("win_condition", "").ToString() ?? ""
            ),
            TimeLimit = (float)config.GetValueOrDefault("time_limit", 0.0f),
            KillTarget = (int)config.GetValueOrDefault("kill_target", 0),

            // Enemy
            EnemyHp = (float)config.GetValueOrDefault("enemy_hp", 0.0f),
            HasEventSequence = config.ContainsKey("event_sequence"),

            // Multiplayer
            IsMultiplayer = (bool)battleContext.Call("is_multiplayer_battle"),
            HasAuthority = (bool)battleContext.Call("has_authority"),

            // Post-battle
            CardXpReward = (int)config.GetValueOrDefault("card_xp_reward", 0),
            SummonerXpReward = (int)config.GetValueOrDefault("summoner_xp_reward", 0),
            OriginScene = battleContext.Get("origin_scene").AsString(),
            IsRankedMatch = (bool)battleContext.Call("is_ranked_match"),
            RankedMatchInfo = battleContext.Call("get_ranked_match_info").AsGodotDictionary(),
        };

        // AI config
        cfg.AiType = ParseAiType(config.GetValueOrDefault("ai_type", "heuristic").ToString()!);
        cfg.AiPersonality = ParseAiPersonality(
            config.GetValueOrDefault("ai_personality", "balanced").ToString()!
        );
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
            Mode = BattleMode.Practice,
            WinCondition = WinConditionType.DestroySummoner,
            AiType = AiType.Scripted,
            HasAuthority = true,
        };
    }

    /// <summary>Whether this is a multiplayer client (not the host).</summary>
    public bool IsMpClient => IsMultiplayer && !HasAuthority;

    /// <summary>
    /// Parse a GDScript AI type string into the enum.
    /// Returns Heuristic for unrecognized values.
    /// </summary>
    public static AiType ParseAiType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "simple" => AiType.Simple,
            "heuristic" => AiType.Heuristic,
            "scripted" => AiType.Scripted,
            "passive" or "none" => AiType.None,
            _ => AiType.Heuristic,
        };
    }

    /// <summary>
    /// Parse a GDScript AI personality string into the enum.
    /// Returns Balanced for unrecognized values.
    /// </summary>
    public static AiPersonality ParseAiPersonality(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "aggressive" => AiPersonality.Aggressive,
            "defensive" => AiPersonality.Defensive,
            "spell_focused" => AiPersonality.SpellFocused,
            _ => AiPersonality.Balanced,
        };
    }
}
