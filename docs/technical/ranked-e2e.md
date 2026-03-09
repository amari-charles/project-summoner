# Ranked Multiplayer E2E

This document describes the automated local end-to-end validation flow for ranked multiplayer.

## Prerequisites

- Docker running locally
- Godot .NET binary available
  - default path: `/Applications/Godot_mono.app/Contents/MacOS/Godot`
  - override with `GODOT_PATH=/path/to/Godot`
- .NET SDK installed

## One-command run

```bash
tools/run_ranked_e2e.sh
```

The script always uses a dedicated Docker Compose namespace (`ranked_e2e` by default)
with a fixed E2E Nakama port map:

- `127.0.0.1:8350 -> nakama:7350` (HTTP API)
- `127.0.0.1:8351 -> nakama:7351` (console/API secondary port)
- `127.0.0.1:8349 -> nakama:7349` (gRPC)

Optional overrides:

```bash
GODOT_PATH=/Applications/Godot_mono.app/Contents/MacOS/Godot \
TIMEOUT_SECONDS=300 \
E2E_COMPOSE_PROJECT=ranked_e2e \
E2E_NAKAMA_HOST=127.0.0.1 \
E2E_NAKAMA_PORT=8350 \
E2E_NAKAMA_SERVER_KEY=defaultkey \
tools/run_ranked_e2e.sh
```

## Gate checks

The script enforces the following gates from log checkpoints:

- Gate A: queue -> match found -> battle start for both clients
- Gate B: ranked report path executes with local rating update
- Gate C: player rank refresh log appears after match report (the `ranked_1v1` leaderboard is auto-provisioned at Nakama startup by `nakama/data/modules/ranked_leaderboard.lua`)
- Gate D: reconnect smoke test (Nakama stop/start in dedicated namespace) shows disconnect + reconnect
- Gate E: no uncaught exception markers and no placeholder opponent metadata marker

Gate C fails if the run reports `Cannot submit: not connected to Nakama`; this indicates report/rank refresh did not complete end-to-end.

Logs are written to:

- `.tmp/ranked-e2e/player1.log`
- `.tmp/ranked-e2e/player2.log`

## CLI flags used by automation

- `--nakama-host=<host>`: override Nakama host
- `--nakama-port=<port>`: override Nakama HTTP port
- `--nakama-server-key=<key>`: override Nakama server key
- `--goto-online`: boot straight to online screen
- `--auto-queue`: auto-join ranked queue once online screen is ready
- `--e2e-log-battle-start`: emit battle-start checkpoint marker
- `--e2e-auto-forfeit-seconds=<n>`: auto-submit forfeit after delay (used by player2 automation)
- `--e2e-force-complete-seconds=<n>`: force local battle completion after delay (automation fallback)

## Troubleshooting

- Port collision on `8350/8351/8349`: set `E2E_NAKAMA_PORT`/host as needed, and ensure matching compose override if changing mapped ports.
- Stale compose project resources: run `docker compose -p ranked_e2e -f docker-compose.yml -f docker-compose.e2e.yml down -v`.
- Gate D reconnect failures after abrupt local Docker restarts: re-run the script to rebuild the dedicated namespace from scratch.
