# Battle Side Spawning Patterns

Battle runtime now resolves each team from a side definition:

```text
battle_config.player_side / enemy_side
    -> BattleSideResolver
    -> ResolvedBattleSide
    -> SimulationNode.RegisterBattleSide()
    -> MatchState.Summoners[team]
```

`MatchState` remains pure runtime state. Authoring/profile/multiplayer data is resolved before it is loaded into the simulation.

## Standard Trainer Battle

Most battles use an authored enemy side with a summoner, deck, and trainer AI controller:

```gdscript
{
    "enemy_side": {
        "team": 1,
        "source": "authored",
        "summoner": {
            "source": "authored",
            "id": "first_trial_enemy",
            "display_name": "First Trial Enemy",
            "hp": 30.0,
            "max_hp": 30.0,
            "mana": 100.0,
            "max_mana": 100.0,
            "cast_speed": 1.0,
            "damage_bonus": 0.0,
            "damage_reduction": 0.0,
            "soul_strength": 0.0
        },
        "deck": {
            "source": "authored",
            "cards": [
                {"catalog_id": "weak_enemy_unit", "count": 2}
            ]
        },
        "controller": {
            "kind": "trainer_ai",
            "ai_type": "simple",
            "ai_difficulty": 1,
            "ai_config": {
                "play_interval_min": 5.0,
                "play_interval_max": 8.0
            }
        }
    }
}
```

Use this for normal opponents that should play legal cards from their authored deck.

## Scripted Encounter Battle

Special encounters use an authored side with a deferred or empty deck and an encounter AI controller:

```gdscript
{
    "enemy_side": {
        "team": 1,
        "source": "authored",
        "summoner": {
            "source": "authored",
            "id": "training_trial",
            "display_name": "Training Trial",
            "hp": 50.0,
            "max_hp": 50.0,
            "mana": 100.0,
            "max_mana": 100.0,
            "cast_speed": 1.0
        },
        "deck": {
            "source": "authored",
            "deferred": true,
            "cards": []
        },
        "controller": {
            "kind": "encounter_ai",
            "ai_type": "none",
            "encounter_ai": {
                "preset": "scripted_encounter",
                "team": 1,
                "use_trainer_ai": false,
                "rules": []
            }
        }
    }
}
```

Use this for training props, waves, rituals, objectives, or battles that do not represent a normal trainer deck.

## Player Loaner Deck

Player summoner stats can still come from profile while the deck is authored:

```gdscript
{
    "player_side": {
        "team": 0,
        "source": "profile",
        "summoner": {"source": "profile"},
        "deck": {
            "source": "authored",
            "cards": [
                {"catalog_id": "neutral_starter_unit", "count": 2}
            ]
        },
        "controller": {"kind": "player"}
    }
}
```

## Related Files

- `scripts/csharp/Battle/Session/BattleSideConfig.cs`
- `scripts/csharp/Battle/Session/BattleSideResolver.cs`
- `scripts/csharp/Battle/Session/BattleSessionConfig.cs`
- `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- `scripts/csharp/Meta/Services/Campaign/Handlers/AcademyProgressHandler.cs`
