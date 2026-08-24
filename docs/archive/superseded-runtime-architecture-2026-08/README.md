# Superseded Runtime Architecture

These documents describe runtime implementations that have been fully replaced.
They are retained as historical context and are not current implementation
guidance.

Current unit abilities are authored as typed primitive configurations and
executed by `SimAbilityOrchestrator` inside deterministic simulation. The former
Godot-node component ability architecture is retired.

Included documents:

- `ability-system.md` — former node-based ability runtime.
- `view-input-decomposition-specs.md` — completed historical decomposition plan.
- `rally-guard-charge-spells.md` — retired command-spell behavior and deleted
  GDScript wiring.
- `summon-spec-proposal.md` — completed proposal whose `SummonSpec` model is now
  part of the card data architecture.
