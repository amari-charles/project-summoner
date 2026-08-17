using Fateforged.Cards;
using Fateforged.Domain.Progression;
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
    Encounter = 6,
}

/// <summary>
/// Typed battle configuration. Battle content enters as side definitions; the
/// session layer resolves those into simulation-ready side loadouts.
/// </summary>
public class BattleSessionConfig
{
    public BattleMode Mode { get; set; }
    public long BattleSeed { get; set; }

    public WinConditionType WinCondition { get; set; } = WinConditionType.DestroySummoner;
    public float TimeLimit { get; set; }
    public int KillTarget { get; set; }
    public float PreparationDuration { get; set; } = 15f;

    public BattleSideDefinition PlayerSide { get; set; } = BattleSideDefinition.ProfilePlayer();
    public BattleSideDefinition EnemySide { get; set; } = BattleSideDefinition.AuthoredEnemy();

    /// <summary>Raw battle_config dict — still needed for multiplayer opponent packet data.</summary>
    public Godot.Collections.Dictionary? RawConfig { get; set; }

    public bool IsMultiplayer { get; set; }
    public bool HasAuthority { get; set; } = true;

    public BattleAttemptId BattleAttemptId { get; set; } = BattleAttemptId.None;
    public string OriginScene { get; set; } = "";
    public bool IsRankedMatch { get; set; }
    public Godot.Collections.Dictionary? RankedMatchInfo { get; set; }

    public bool IsMpClient => IsMultiplayer && !HasAuthority;

    public BattleSideDefinition GetSide(int team) => team == 0 ? PlayerSide : EnemySide;

    public static BattleSessionConfig FromBattleContext(Node battleContext)
    {
        var config =
            (Godot.Collections.Dictionary?)battleContext.Get("battle_config")
            ?? new Godot.Collections.Dictionary();

        var cfg = new BattleSessionConfig
        {
            Mode = (BattleMode)(int)battleContext.Get("current_mode"),
            BattleSeed = GetLong(config, "battle_seed", 0),
            RawConfig = config,
            WinCondition = WinConditionFactory.Parse(
                config.GetValueOrDefault("win_condition", "").ToString() ?? ""
            ),
            TimeLimit = GetFloat(config, "time_limit", 0.0f),
            KillTarget = GetInt(config, "kill_target", 0),
            PreparationDuration = GetFloat(config, "prep_duration", 15.0f),
            IsMultiplayer = (bool)battleContext.Call("is_multiplayer_battle"),
            HasAuthority = (bool)battleContext.Call("has_authority"),
            BattleAttemptId = BattleAttemptId.FromString(
                battleContext.Call("get_battle_attempt_id").AsString()
            ),
            OriginScene = battleContext.Get("origin_scene").AsString(),
            IsRankedMatch = (bool)battleContext.Call("is_ranked_match"),
            RankedMatchInfo = battleContext.Call("get_ranked_match_info").AsGodotDictionary(),
        };

        cfg.PlayerSide = ParseSide(
            config.GetValueOrDefault("player_side", default),
            BattleSideDefinition.ProfilePlayer()
        );
        cfg.EnemySide = ParseSide(
            config.GetValueOrDefault("enemy_side", default),
            cfg.IsMultiplayer ? MultiplayerEnemySide() : BattleSideDefinition.AuthoredEnemy()
        );

        return cfg;
    }

    public static BattleSessionConfig ForPractice() =>
        new()
        {
            Mode = BattleMode.Practice,
            WinCondition = WinConditionType.DestroySummoner,
            HasAuthority = true,
            PlayerSide = BattleSideDefinition.ProfilePlayer(),
            EnemySide = new BattleSideDefinition
            {
                Team = 1,
                Source = BattleSideSource.Authored,
                Summoner = new BattleSummonerDefinition
                {
                    Source = BattleSideSource.Authored,
                    Id = "practice_enemy",
                    DisplayName = "Practice Enemy",
                    Hp = 20f,
                    MaxHp = 20f,
                    Mana = 100f,
                    MaxMana = 100f,
                    CastSpeed = 1f,
                },
                Deck = new BattleDeckDefinition
                {
                    Source = BattleDeckSource.Authored,
                    Cards = [new BattleDeckEntryDefinition { CatalogId = "fire_wisp", Count = 1 }],
                },
                Controller = new BattleControllerDefinition
                {
                    Kind = BattleControllerKind.TrainerAi,
                    AiType = AiType.None,
                    AiDifficulty = 0,
                    AiIntervalMin = 999f,
                    AiIntervalMax = 999f,
                },
            },
        };

    private static BattleSideDefinition MultiplayerEnemySide() =>
        new()
        {
            Team = 1,
            Source = BattleSideSource.MultiplayerOpponent,
            Summoner = new BattleSummonerDefinition
            {
                Source = BattleSideSource.MultiplayerOpponent,
            },
            Deck = new BattleDeckDefinition { Source = BattleDeckSource.Authored },
            Controller = new BattleControllerDefinition { Kind = BattleControllerKind.Network },
        };

    private static BattleSideDefinition ParseSide(Variant value, BattleSideDefinition fallback)
    {
        if (value.VariantType != Variant.Type.Dictionary)
            return fallback;

        var dict = value.AsGodotDictionary();
        var side = new BattleSideDefinition
        {
            Team = GetInt(dict, "team", fallback.Team),
            Source = ParseSideSource(
                dict.GetValueOrDefault("source", fallback.Source.ToString()).ToString()!
            ),
            Summoner = fallback.Summoner,
            Deck = fallback.Deck,
            Controller = fallback.Controller,
        };

        var summonerVar = dict.GetValueOrDefault("summoner", default);
        if (summonerVar.VariantType == Variant.Type.Dictionary)
            side.Summoner = ParseSummoner(summonerVar.AsGodotDictionary(), fallback.Summoner);

        var deckVar = dict.GetValueOrDefault("deck", default);
        if (deckVar.VariantType == Variant.Type.Dictionary)
            side.Deck = ParseDeck(deckVar.AsGodotDictionary(), fallback.Deck);

        var controllerVar = dict.GetValueOrDefault("controller", default);
        if (controllerVar.VariantType == Variant.Type.Dictionary)
            side.Controller = ParseController(
                controllerVar.AsGodotDictionary(),
                fallback.Controller
            );

        return side;
    }

    private static BattleSummonerDefinition ParseSummoner(
        Godot.Collections.Dictionary dict,
        BattleSummonerDefinition fallback
    )
    {
        var summoner = new BattleSummonerDefinition
        {
            Source = ParseSideSource(
                dict.GetValueOrDefault("source", fallback.Source.ToString()).ToString()!
            ),
            Id = dict.GetValueOrDefault("id", fallback.Id).ToString() ?? "",
            DisplayName =
                dict.GetValueOrDefault("display_name", fallback.DisplayName).ToString() ?? "",
            Hp = GetOptionalFloat(dict, "hp", fallback.Hp),
            MaxHp = GetOptionalFloat(dict, "max_hp", fallback.MaxHp),
            Mana = GetOptionalFloat(dict, "mana", fallback.Mana),
            MaxMana = GetOptionalFloat(dict, "max_mana", fallback.MaxMana),
            CastSpeed = GetOptionalFloat(dict, "cast_speed", fallback.CastSpeed),
            DamageBonus = GetOptionalFloat(dict, "damage_bonus", fallback.DamageBonus),
            DamageReduction = GetOptionalFloat(dict, "damage_reduction", fallback.DamageReduction),
            SoulStrength = GetOptionalFloat(dict, "soul_strength", fallback.SoulStrength),
        };

        foreach (var kvp in fallback.ElementalDamageBonuses)
            summoner.ElementalDamageBonuses[kvp.Key] = kvp.Value;

        SetElementalBonus(dict, summoner, "fire_damage_bonus", Element.Fire);
        SetElementalBonus(dict, summoner, "water_damage_bonus", Element.Water);
        SetElementalBonus(dict, summoner, "wind_damage_bonus", Element.Wind);
        SetElementalBonus(dict, summoner, "earth_damage_bonus", Element.Earth);
        SetElementalBonus(dict, summoner, "lightning_damage_bonus", Element.Lightning);
        SetElementalBonus(dict, summoner, "life_damage_bonus", Element.Life);
        SetElementalBonus(dict, summoner, "death_damage_bonus", Element.Death);
        SetElementalBonus(dict, summoner, "shadow_damage_bonus", Element.Shadow);

        return summoner;
    }

    private static void SetElementalBonus(
        Godot.Collections.Dictionary dict,
        BattleSummonerDefinition summoner,
        string key,
        Element element
    )
    {
        var value = GetOptionalFloat(dict, key, null);
        if (value.HasValue)
            summoner.ElementalDamageBonuses[element] = value.Value;
    }

    private static BattleDeckDefinition ParseDeck(
        Godot.Collections.Dictionary dict,
        BattleDeckDefinition fallback
    )
    {
        var deck = new BattleDeckDefinition
        {
            Source = ParseDeckSource(
                dict.GetValueOrDefault("source", fallback.Source.ToString()).ToString()!
            ),
            Deferred = GetBool(dict, "deferred", fallback.Deferred),
        };

        var cardsVar = dict.GetValueOrDefault("cards", default);
        if (cardsVar.VariantType == Variant.Type.Array)
        {
            foreach (var entryVar in cardsVar.AsGodotArray())
            {
                if (entryVar.VariantType != Variant.Type.Dictionary)
                    continue;
                var entry = entryVar.AsGodotDictionary();
                deck.Cards.Add(
                    new BattleDeckEntryDefinition
                    {
                        CatalogId = entry.GetValueOrDefault("catalog_id", "").ToString() ?? "",
                        Count = GetInt(entry, "count", 1),
                    }
                );
            }
        }
        else
        {
            deck.Cards.AddRange(fallback.Cards);
        }

        return deck;
    }

    private static BattleControllerDefinition ParseController(
        Godot.Collections.Dictionary dict,
        BattleControllerDefinition fallback
    )
    {
        var controller = new BattleControllerDefinition
        {
            Kind = ParseControllerKind(
                dict.GetValueOrDefault("kind", fallback.Kind.ToString()).ToString()!
            ),
            AiType = ParseAiType(
                dict.GetValueOrDefault("ai_type", fallback.AiType.ToString()).ToString()!
            ),
            AiPersonality = ParseAiPersonality(
                dict.GetValueOrDefault("ai_personality", fallback.AiPersonality.ToString())
                    .ToString()!
            ),
            AiDifficulty = GetInt(dict, "ai_difficulty", fallback.AiDifficulty),
            AiIntervalMin = fallback.AiIntervalMin,
            AiIntervalMax = fallback.AiIntervalMax,
            AiScript = fallback.AiScript,
            EncounterAi = fallback.EncounterAi,
        };

        var aiConfigVar = dict.GetValueOrDefault("ai_config", default);
        if (aiConfigVar.VariantType == Variant.Type.Dictionary)
        {
            var aiConfig = aiConfigVar.AsGodotDictionary();
            controller.AiIntervalMin = GetFloat(
                aiConfig,
                "play_interval_min",
                fallback.AiIntervalMin
            );
            controller.AiIntervalMax = GetFloat(
                aiConfig,
                "play_interval_max",
                fallback.AiIntervalMax
            );
        }

        var scriptVar = dict.GetValueOrDefault("ai_script", default);
        if (scriptVar.VariantType == Variant.Type.Array)
            controller.AiScript = scriptVar.AsGodotArray();

        var encounterVar = dict.GetValueOrDefault("encounter_ai", default);
        if (encounterVar.VariantType == Variant.Type.Dictionary)
            controller.EncounterAi = ParseEncounterAi(encounterVar.AsGodotDictionary());

        return controller;
    }

    private static BattleSideSource ParseSideSource(string value) =>
        Normalize(value) switch
        {
            "profile" => BattleSideSource.Profile,
            "authored" => BattleSideSource.Authored,
            "multiplayeropponent" => BattleSideSource.MultiplayerOpponent,
            "multiplayer_opponent" => BattleSideSource.MultiplayerOpponent,
            "clientplaceholder" => BattleSideSource.ClientPlaceholder,
            "client_placeholder" => BattleSideSource.ClientPlaceholder,
            _ => BattleSideSource.SceneDefault,
        };

    private static BattleDeckSource ParseDeckSource(string value) =>
        Normalize(value) switch
        {
            "profile" => BattleDeckSource.Profile,
            "authored" => BattleDeckSource.Authored,
            _ => BattleDeckSource.None,
        };

    private static BattleControllerKind ParseControllerKind(string value) =>
        Normalize(value) switch
        {
            "player" => BattleControllerKind.Player,
            "trainerai" => BattleControllerKind.TrainerAi,
            "trainer_ai" => BattleControllerKind.TrainerAi,
            "encounterai" => BattleControllerKind.EncounterAi,
            "encounter_ai" => BattleControllerKind.EncounterAi,
            "network" => BattleControllerKind.Network,
            _ => BattleControllerKind.None,
        };

    public static AiType ParseAiType(string value) =>
        Normalize(value) switch
        {
            "simple" => AiType.Simple,
            "heuristic" => AiType.Heuristic,
            "scripted" => AiType.Scripted,
            "passive" or "none" => AiType.None,
            _ => AiType.Heuristic,
        };

    public static AiPersonality ParseAiPersonality(string value) =>
        Normalize(value) switch
        {
            "aggressive" => AiPersonality.Aggressive,
            "defensive" => AiPersonality.Defensive,
            "spellfocused" => AiPersonality.SpellFocused,
            "spell_focused" => AiPersonality.SpellFocused,
            _ => AiPersonality.Balanced,
        };

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static float GetFloat(Godot.Collections.Dictionary dict, string key, float defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;

        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt32(),
            _ => defaultValue,
        };
    }

    private static float? GetOptionalFloat(
        Godot.Collections.Dictionary dict,
        string key,
        float? defaultValue
    )
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;

        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt32(),
            _ => defaultValue,
        };
    }

    private static int GetInt(Godot.Collections.Dictionary dict, string key, int defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;

        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => (int)value.AsDouble(),
            _ => defaultValue,
        };
    }

    private static long GetLong(Godot.Collections.Dictionary dict, string key, long defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;

        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt64(),
            Variant.Type.Float => (long)value.AsDouble(),
            _ => defaultValue,
        };
    }

    private static bool GetBool(Godot.Collections.Dictionary dict, string key, bool defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        return value.VariantType == Variant.Type.Bool ? value.AsBool() : defaultValue;
    }

    private static EncounterAiConfig ParseEncounterAi(Godot.Collections.Dictionary dict)
    {
        var preset = ParseEncounterAiPreset(
            dict.GetValueOrDefault("preset", "default_trainer").ToString()!
        );
        var config =
            preset == EncounterAiPreset.ScriptedEncounter
                ? EncounterAiConfig.ScriptedEncounter()
                : EncounterAiConfig.DefaultTrainer();

        config.Team = GetInt(dict, "team", 1);
        if (dict.ContainsKey("use_trainer_ai"))
            config.UseTrainerAi = (bool)
                dict.GetValueOrDefault("use_trainer_ai", config.UseTrainerAi);

        var rulesVar = dict.GetValueOrDefault("rules", default);
        if (rulesVar.VariantType == Variant.Type.Array)
        {
            foreach (var ruleVar in rulesVar.AsGodotArray())
            {
                if (ruleVar.VariantType == Variant.Type.Dictionary)
                    config.Rules.Add(ParseEncounterRule(ruleVar.AsGodotDictionary()));
            }
        }

        return config;
    }

    private static EncounterRule ParseEncounterRule(Godot.Collections.Dictionary dict)
    {
        var rule = new EncounterRule
        {
            Id = dict.GetValueOrDefault("id", "").ToString() ?? "",
            Kind = ParseEncounterRuleKind(dict.GetValueOrDefault("kind", "event").ToString()!),
            Enabled = GetBool(dict, "enabled", true),
            StartTime = GetFloat(dict, "start_time", 0.0f),
            Rhythm = ParseEncounterRhythm(dict.GetValueOrDefault("rhythm", "steady").ToString()!),
            Source = ParseEncounterActionSource(
                dict.GetValueOrDefault("source", "encounter").ToString()!
            ),
            Placement = ParseEncounterPlacement(
                dict.GetValueOrDefault("placement", "neutral").ToString()!
            ),
        };

        if (dict.ContainsKey("ai_type"))
            rule.AiType = ParseAiType(dict.GetValueOrDefault("ai_type", "").ToString()!);
        if (dict.ContainsKey("ai_personality"))
            rule.Personality = ParseAiPersonality(
                dict.GetValueOrDefault("ai_personality", "").ToString()!
            );

        if (dict.ContainsKey("end_time"))
            rule.EndTime = GetFloat(dict, "end_time", 0.0f);
        if (dict.ContainsKey("interval_seconds"))
            rule.IntervalSeconds = GetFloat(dict, "interval_seconds", 0.0f);
        if (dict.ContainsKey("max_executions"))
            rule.MaxExecutions = GetInt(dict, "max_executions", 0);
        if (dict.ContainsKey("max_alive"))
            rule.MaxAlive = GetInt(dict, "max_alive", 0);

        var ruleAiConfigVar = dict.GetValueOrDefault("ai_config", default);
        if (ruleAiConfigVar.VariantType == Variant.Type.Dictionary)
        {
            var aiCfg = ruleAiConfigVar.AsGodotDictionary();
            if (aiCfg.ContainsKey("play_interval_min"))
                rule.PlayIntervalMin = GetFloat(aiCfg, "play_interval_min", 0.0f);
            if (aiCfg.ContainsKey("play_interval_max"))
                rule.PlayIntervalMax = GetFloat(aiCfg, "play_interval_max", 0.0f);
        }

        var poolVar = dict.GetValueOrDefault("card_pool", default);
        if (poolVar.VariantType == Variant.Type.Array)
        {
            foreach (var entry in poolVar.AsGodotArray())
                rule.CardPool.Add(entry.ToString());
        }

        var actionsVar = dict.GetValueOrDefault("actions", default);
        if (actionsVar.VariantType == Variant.Type.Array)
        {
            foreach (var actionVar in actionsVar.AsGodotArray())
            {
                if (actionVar.VariantType == Variant.Type.Dictionary)
                    rule.Actions.Add(ParseEncounterAction(actionVar.AsGodotDictionary()));
            }
        }

        return rule;
    }

    private static EncounterAction ParseEncounterAction(Godot.Collections.Dictionary dict)
    {
        var action = new EncounterAction
        {
            Kind = ParseEncounterActionKind(
                dict.GetValueOrDefault("kind", "spawn_units").ToString()!
            ),
            Source = ParseEncounterActionSource(
                dict.GetValueOrDefault("source", "encounter").ToString()!
            ),
            Team = GetInt(dict, "team", 1),
            CardId = dict.GetValueOrDefault("card_id", "").ToString() ?? "",
            Placement = ParseEncounterPlacement(
                dict.GetValueOrDefault("placement", "neutral").ToString()!
            ),
            ActivateImmediately = GetBool(dict, "activate_immediately", true),
            AllowWhenOverwhelmed = GetBool(dict, "allow_when_overwhelmed", false),
            IgnoreCaps = GetBool(dict, "ignore_caps", false),
            RuleId = dict.GetValueOrDefault("rule_id", "").ToString() ?? "",
            Enabled = GetBool(dict, "enabled", true),
        };

        if (dict.ContainsKey("ai_type"))
            action.AiType = ParseAiType(dict.GetValueOrDefault("ai_type", "").ToString()!);
        if (dict.ContainsKey("ai_personality"))
            action.Personality = ParseAiPersonality(
                dict.GetValueOrDefault("ai_personality", "").ToString()!
            );

        var actionAiConfigVar = dict.GetValueOrDefault("ai_config", default);
        if (actionAiConfigVar.VariantType == Variant.Type.Dictionary)
        {
            var aiCfg = actionAiConfigVar.AsGodotDictionary();
            if (aiCfg.ContainsKey("play_interval_min"))
                action.PlayIntervalMin = GetFloat(aiCfg, "play_interval_min", 0.0f);
            if (aiCfg.ContainsKey("play_interval_max"))
                action.PlayIntervalMax = GetFloat(aiCfg, "play_interval_max", 0.0f);
        }

        var cardIdsVar = dict.GetValueOrDefault("card_ids", default);
        if (cardIdsVar.VariantType == Variant.Type.Array)
        {
            foreach (var entry in cardIdsVar.AsGodotArray())
                action.CardIds.Add(entry.ToString());
        }

        var positionVar = dict.GetValueOrDefault("position", default);
        if (TryParseEncounterPosition(positionVar, out var position))
            action.Position = position;

        var positionsVar = dict.GetValueOrDefault("positions", default);
        if (positionsVar.VariantType == Variant.Type.Array)
        {
            foreach (var entry in positionsVar.AsGodotArray())
            {
                if (TryParseEncounterPosition(entry, out var parsed))
                    action.Positions.Add(parsed);
            }
        }

        return action;
    }

    private static bool TryParseEncounterPosition(Variant value, out SimVector3 position)
    {
        position = SimVector3.Zero;
        if (value.VariantType == Variant.Type.Dictionary)
        {
            var dict = value.AsGodotDictionary();
            float z = GetFloat(dict, "z", 0.0f);
            if (!dict.ContainsKey("z"))
                z = GetFloat(dict, "y", 0.0f);
            position = new SimVector3(GetFloat(dict, "x", 0.0f), 0f, z);
            return true;
        }
        if (value.VariantType == Variant.Type.Vector2)
        {
            var v2 = value.AsVector2();
            position = new SimVector3(v2.X, 0f, v2.Y);
            return true;
        }
        return false;
    }

    private static EncounterAiPreset ParseEncounterAiPreset(string value) =>
        Normalize(value) switch
        {
            "scripted_encounter" or "scriptedencounter" => EncounterAiPreset.ScriptedEncounter,
            _ => EncounterAiPreset.DefaultTrainer,
        };

    private static EncounterRuleKind ParseEncounterRuleKind(string value) =>
        Normalize(value) switch
        {
            "rhythm" or "rhythm_rule" or "rhythmrule" => EncounterRuleKind.RhythmRule,
            "pool" or "pool_rule" or "poolrule" => EncounterRuleKind.PoolRule,
            "cap" or "cap_rule" or "caprule" => EncounterRuleKind.CapRule,
            "placement" or "placement_rule" or "placementrule" => EncounterRuleKind.PlacementRule,
            "behavior" or "behavior_rule" or "behaviorrule" => EncounterRuleKind.BehaviorRule,
            "hazard" or "hazard_rule" or "hazardrule" => EncounterRuleKind.HazardRule,
            "objective" or "objective_rule" or "objectiverule" => EncounterRuleKind.ObjectiveRule,
            "dialogue" or "dialogue_rule" or "dialoguerule" => EncounterRuleKind.DialogueRule,
            "arena_modifier" or "arena_modifier_rule" or "arenamodifierrule" =>
                EncounterRuleKind.ArenaModifierRule,
            "reward_preview" or "reward_preview_rule" or "rewardpreviewrule" =>
                EncounterRuleKind.RewardPreviewRule,
            _ => EncounterRuleKind.EventRule,
        };

    private static EncounterActionKind ParseEncounterActionKind(string value) =>
        Normalize(value) switch
        {
            "play_card" or "playcard" => EncounterActionKind.PlayCard,
            "set_behavior" or "setbehavior" => EncounterActionKind.SetBehavior,
            "set_rule_enabled" or "setruleenabled" => EncounterActionKind.SetRuleEnabled,
            "spawn_hazard" or "spawnhazard" => EncounterActionKind.SpawnHazard,
            "apply_arena_modifier" or "applyarenamodifier" =>
                EncounterActionKind.ApplyArenaModifier,
            "set_objective_state" or "setobjectivestate" => EncounterActionKind.SetObjectiveState,
            "grant_temporary_card" or "granttemporarycard" =>
                EncounterActionKind.GrantTemporaryCard,
            "modify_mana_rule" or "modifymanarule" => EncounterActionKind.ModifyManaRule,
            "trigger_dialogue_beat" or "triggerdialoguebeat" =>
                EncounterActionKind.TriggerDialogueBeat,
            "set_win_condition_progress" or "setwinconditionprogress" =>
                EncounterActionKind.SetWinConditionProgress,
            _ => EncounterActionKind.SpawnUnits,
        };

    private static EncounterActionSource ParseEncounterActionSource(string value) =>
        Normalize(value) switch
        {
            "trainer" => EncounterActionSource.Trainer,
            "hazard" => EncounterActionSource.Hazard,
            "objective" => EncounterActionSource.Objective,
            _ => EncounterActionSource.Encounter,
        };

    private static EncounterRhythm ParseEncounterRhythm(string value) =>
        Normalize(value) switch
        {
            "sparse" => EncounterRhythm.Sparse,
            "frequent" => EncounterRhythm.Frequent,
            "relentless" => EncounterRhythm.Relentless,
            _ => EncounterRhythm.Steady,
        };

    private static EncounterPlacement ParseEncounterPlacement(string value) =>
        Normalize(value) switch
        {
            "defensive" => EncounterPlacement.Defensive,
            "aggressive" => EncounterPlacement.Aggressive,
            _ => EncounterPlacement.Neutral,
        };
}
