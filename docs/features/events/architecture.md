# Event Architecture

**Status:** CURRENT

Campaign events are typed domain definitions owned by `EventCatalog`. Their gameplay action is handled by the relevant authoritative screen or service. Narrative attached to an event is a separate typed `MetaMomentStarted`, battle, or activity event consumed by the [Narrative Director](../../design/narrative-dialogue-system.md).

Narrative is not a general event scripting engine. Shops, battle spawning, rewards, choices, and navigation remain with their dedicated owners. The removed EventSequencer format and arbitrary step execution are not supported.
