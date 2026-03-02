# Gameplay Subsystem

The gameplay subsystem covers everything that happens during a battle: simulation logic, session orchestration, visual rendering, and player input.

## Layers

Each layer has its own subtree with detailed documentation. The high-level overview lives in [target-architecture.md](../target-architecture.md).

| Layer | Role | Docs |
|-------|------|------|
| [Simulation](simulation/) | Pure game logic — movement, combat, abilities, projectiles | [target-architecture.md &sect;2](../target-architecture.md#2-simulation-layer) |
| [Session](session/) | Orchestration — how the simulation gets run (local or networked) | [target-architecture.md &sect;3-4](../target-architecture.md#3-session-layer) |
| [View](view/) | Visual rendering — 3D battlefield + 2D HUD | [View layer docs](view/) |
| [Input](input/) | Player intent capture — gestures to Commands | [target-architecture.md &sect;5](../target-architecture.md#5-input) |

## Dependency Flow

```
Input ──pushes commands──▶ Session ◀──reads state── View
                             │
                          ticks ▼
                         Simulation
```

Input and View are independent peers. Both depend on Session, not on each other.

## Future Subtrees

Sibling subsystems will slot in alongside `gameplay/` as they're designed:

- `meta/` — progression, unlocks, player profile
- `ui-shell/` — menus, lobby, deck builder
