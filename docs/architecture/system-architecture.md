# System Architecture

*Last Updated: 2026-01-06*

## Guiding Principle

**C# = Systems & Mechanics** | **GDScript = Orchestration & UI**

---

## Core Data Flow (DAG)

```mermaid
flowchart LR
    Input["User Input"] --> UI["UI Layer<br/>(GDScript)"]
    UI --> CardFactory["CardFactory"]
    CardFactory --> PlayerCardService["PlayerCardService"]
    CardFactory --> ModifierService["ModifierService"]
    CardFactory --> SpawnOrchestrator["SpawnOrchestrator"]
    PlayerCardService --> Unit["Unit3D"]
    ModifierService --> Unit
    SpawnOrchestrator --> Unit
    Unit --> Battle["Battle<br/>(GDScript)"]

    style UI fill:#f0ad4e,color:#000
    style Battle fill:#f0ad4e,color:#000
    style CardFactory fill:#2d8a2d,color:#fff
    style PlayerCardService fill:#2d8a2d,color:#fff
    style ModifierService fill:#2d8a2d,color:#fff
    style SpawnOrchestrator fill:#2d8a2d,color:#fff
    style Unit fill:#4a90d9,color:#fff
```

**Legend:** Orange = GDScript | Green = C# Services | Blue = C# Runtime

---

## Card Play Flow

```mermaid
flowchart LR
    A["card.gd"] --> B["CardFactory"]
    B --> C["PlayerCardService<br/>effective stats"]
    B --> D["ModifierService<br/>trait/upgrade mods"]
    B --> E["SpawnOrchestrator<br/>positions"]
    C --> F["Unit3D"]
    D --> F
    E --> F

    style A fill:#f0ad4e,color:#000
    style B fill:#2d8a2d,color:#fff
    style C fill:#2d8a2d,color:#fff
    style D fill:#2d8a2d,color:#fff
    style E fill:#2d8a2d,color:#fff
    style F fill:#4a90d9,color:#fff
```

---

## Modifier Provider Pattern

```mermaid
flowchart LR
    SummonerInstance --> SummonerProvider["SummonerModifierProvider"]
    CardUpgrades --> CardProvider["CardModifierProvider"]
    SummonerProvider --> ModifierService
    CardProvider --> ModifierService
    ModifierService --> Unit3D

    style ModifierService fill:#2d8a2d,color:#fff
    style SummonerProvider fill:#4a90d9,color:#fff
    style CardProvider fill:#4a90d9,color:#fff
    style Unit3D fill:#4a90d9,color:#fff
```

---

## C# Services (Autoloads)

| Service | Path | Purpose |
|---------|------|---------|
| `CardFactory` | `/root/CardFactory` | Spawns units, executes spells |
| `PlayerCardService` | `/root/PlayerCardService` | Card stats, progression |
| `ModifierService` | `/root/ModifierService` | Stat modifiers from traits/upgrades |
| `SpawnOrchestrator` | `/root/SpawnOrchestrator` | Formation positions |
| `DamageSystem` | `/root/DamageSystem` | Damage/healing application |

All services implement interfaces (`ICardFactory`, etc.) in `scripts/csharp/Services/Interfaces/`.

---

## Unit Components

| Component | Purpose |
|-----------|---------|
| `Unit3D.cs` | Coordinator |
| `UnitHealth.cs` | HP, damage, healing |
| `UnitMovement.cs` | Steering, pathfinding |
| `SpawnRevealComponent.cs` | Spawn animation |

---

## GDScript → C# Interop

```gdscript
# Use factory methods (GDScript can't instantiate C# classes)
var service: Node = get_node_or_null("/root/ModifierService")
service.call("register_summoner_provider", summoner_instance, summoner_id)
```

---

## Key Decisions

1. **C# for mechanics, GDScript for orchestration** - Clear boundary reduces cross-language complexity

2. **Provider pattern for modifiers** - Extensible without modifying core service

3. **Service interfaces** - Enable testing and future dependency injection

4. **Factory methods for interop** - Work around GDScript's inability to instantiate C# classes

5. **Type-safe calls over duck typing** - Use `if unit is Unit3D` instead of `has_method()`
