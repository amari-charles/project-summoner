# Authored Battle and Encounter Architecture

Quest content launches reusable encounter definitions from `data/encounters/`.
The encounter owns preparation, loadout mode, battle configuration, and its
completion summary; the quest owns sequencing and reward intent.

Developer-only battles are selected directly from the Debug Arena authored
battle catalog. They use `ProgressionAuthority` for attempts, XP, first-clear
rewards, and idempotent completion without participating in quest sequencing.

Both routes enter the same battle runtime and shared Results screen.
