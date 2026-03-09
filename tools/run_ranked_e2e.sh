#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
LOG_DIR="${LOG_DIR:-$PROJECT_ROOT/.tmp/ranked-e2e}"
GODOT_PATH="${GODOT_PATH:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-240}"
E2E_COMPOSE_PROJECT="${E2E_COMPOSE_PROJECT:-ranked_e2e}"
E2E_NAKAMA_HOST="${E2E_NAKAMA_HOST:-127.0.0.1}"
E2E_NAKAMA_PORT="${E2E_NAKAMA_PORT:-8350}"
E2E_NAKAMA_SERVER_KEY="${E2E_NAKAMA_SERVER_KEY:-defaultkey}"
E2E_DOCKER_COMPOSE_ARGS=(
  -p "$E2E_COMPOSE_PROJECT"
  -f docker-compose.yml
  -f docker-compose.e2e.yml
)

mkdir -p "$LOG_DIR"
PLAYER1_LOG="$LOG_DIR/player1.log"
PLAYER2_LOG="$LOG_DIR/player2.log"
rm -f "$PLAYER1_LOG" "$PLAYER2_LOG"

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

require_cmd docker
require_cmd dotnet
require_cmd curl

if [[ ! -x "$GODOT_PATH" ]]; then
  echo "Godot binary not found: $GODOT_PATH" >&2
  exit 1
fi

cleanup() {
  set +e
  if [[ -n "${PLAYER1_PID:-}" ]]; then
    kill "$PLAYER1_PID" >/dev/null 2>&1 || true
    wait "$PLAYER1_PID" >/dev/null 2>&1 || true
  fi
  if [[ -n "${PLAYER2_PID:-}" ]]; then
    kill "$PLAYER2_PID" >/dev/null 2>&1 || true
    wait "$PLAYER2_PID" >/dev/null 2>&1 || true
  fi
  docker compose "${E2E_DOCKER_COMPOSE_ARGS[@]}" down >/dev/null 2>&1 || true
}
trap cleanup EXIT

cd "$PROJECT_ROOT"

echo "==> Starting dedicated E2E Nakama stack (project: ${E2E_COMPOSE_PROJECT})"
docker compose "${E2E_DOCKER_COMPOSE_ARGS[@]}" up -d cockroachdb nakama
for i in $(seq 1 60); do
  if curl -fsS "http://${E2E_NAKAMA_HOST}:${E2E_NAKAMA_PORT}/healthcheck" >/dev/null 2>&1; then
    break
  fi
  if [[ "$i" -eq 60 ]]; then
    echo "Nakama healthcheck failed (${E2E_NAKAMA_HOST}:${E2E_NAKAMA_PORT})" >&2
    docker compose "${E2E_DOCKER_COMPOSE_ARGS[@]}" logs --tail=100 nakama >&2 || true
    exit 1
  fi
  sleep 1
done

echo "==> Building C# project"
dotnet build --verbosity minimal >/dev/null

echo "==> Launching ranked clients"
"$GODOT_PATH" --path . --headless -- \
  --nakama-host="${E2E_NAKAMA_HOST}" \
  --nakama-port="${E2E_NAKAMA_PORT}" \
  --nakama-server-key="${E2E_NAKAMA_SERVER_KEY}" \
  --goto-online --auto-queue --e2e-log-battle-start --e2e-force-complete-seconds=55 \
  >"$PLAYER1_LOG" 2>&1 &
PLAYER1_PID=$!
"$GODOT_PATH" --path . --headless -- \
  --player2 \
  --nakama-host="${E2E_NAKAMA_HOST}" \
  --nakama-port="${E2E_NAKAMA_PORT}" \
  --nakama-server-key="${E2E_NAKAMA_SERVER_KEY}" \
  --goto-online --auto-queue --e2e-log-battle-start --e2e-auto-forfeit-seconds=40 \
  >"$PLAYER2_LOG" 2>&1 &
PLAYER2_PID=$!

wait_for_log() {
  local file="$1"
  local pattern="$2"
  local label="$3"
  local deadline=$((SECONDS + TIMEOUT_SECONDS))
  while (( SECONDS < deadline )); do
    if grep -Fq "$pattern" "$file"; then
      echo "PASS: $label"
      return 0
    fi
    sleep 1
  done
  echo "FAIL: $label (pattern '$pattern' not found in $file)" >&2
  return 1
}

wait_for_any_log() {
  local pattern="$1"
  local label="$2"
  local deadline=$((SECONDS + TIMEOUT_SECONDS))
  while (( SECONDS < deadline )); do
    if grep -Fq "$pattern" "$PLAYER1_LOG" || grep -Fq "$pattern" "$PLAYER2_LOG"; then
      echo "PASS: $label"
      return 0
    fi
    sleep 1
  done
  echo "FAIL: $label (pattern '$pattern' not found in either player log)" >&2
  return 1
}

wait_for_any_log_after_report() {
  local pattern="$1"
  local label="$2"
  local deadline=$((SECONDS + TIMEOUT_SECONDS))
  while (( SECONDS < deadline )); do
    local p1_report_line p2_report_line
    p1_report_line="$(grep -nF "[RANKED][REPORT] Match recorded:" "$PLAYER1_LOG" | tail -n1 | cut -d: -f1 || true)"
    p2_report_line="$(grep -nF "[RANKED][REPORT] Match recorded:" "$PLAYER2_LOG" | tail -n1 | cut -d: -f1 || true)"

    if [[ -n "${p1_report_line}" ]] && tail -n +"${p1_report_line}" "$PLAYER1_LOG" | grep -Fq "$pattern"; then
      echo "PASS: $label"
      return 0
    fi
    if [[ -n "${p2_report_line}" ]] && tail -n +"${p2_report_line}" "$PLAYER2_LOG" | grep -Fq "$pattern"; then
      echo "PASS: $label"
      return 0
    fi

    sleep 1
  done
  echo "FAIL: $label (pattern '$pattern' not found after report marker)" >&2
  return 1
}

echo "==> Gate A: queue -> match -> battle start"
wait_for_log "$PLAYER1_LOG" "[RANKED][MATCH_JOIN] Match found:" "Player1 matched"
wait_for_log "$PLAYER2_LOG" "[RANKED][MATCH_JOIN] Match found:" "Player2 matched"
wait_for_log "$PLAYER1_LOG" "[RANKED][E2E] Battle scene initialized" "Player1 battle started"
wait_for_log "$PLAYER2_LOG" "[RANKED][E2E] Battle scene initialized" "Player2 battle started"

echo "==> Gate B: ranked report path"
wait_for_any_log "[RANKED][REPORT] Reporting ranked match" "Report triggered"
wait_for_any_log "[RANKED][REPORT] Match recorded:" "Local rating update recorded"

echo "==> Gate C: rank refresh path"
if grep -Fq "Leaderboard not found." "$PLAYER1_LOG" || grep -Fq "Leaderboard not found." "$PLAYER2_LOG"; then
  echo "SKIP: rank refresh gate (leaderboard backend not provisioned)"
elif grep -Fq "[RANKED][REPORT] Cannot submit: not connected to Nakama" "$PLAYER1_LOG" || \
     grep -Fq "[RANKED][REPORT] Cannot submit: not connected to Nakama" "$PLAYER2_LOG"; then
  echo "FAIL: rank refresh gate (report submission deferred due disconnected client)" >&2
  exit 1
else
  wait_for_any_log_after_report "[RANKED][REPORT] Player rank refreshed:" "Player rank refreshed"
fi

echo "==> Gate D: reconnect smoke (controlled socket drop)"
docker compose "${E2E_DOCKER_COMPOSE_ARGS[@]}" stop nakama >/dev/null
wait_for_log "$PLAYER1_LOG" "[RANKED][RECONNECT] Socket disconnected" "Reconnect disconnect observed"
docker compose "${E2E_DOCKER_COMPOSE_ARGS[@]}" up -d nakama >/dev/null
for i in $(seq 1 30); do
  if curl -fsS "http://${E2E_NAKAMA_HOST}:${E2E_NAKAMA_PORT}/healthcheck" >/dev/null 2>&1; then
    break
  fi
  if [[ "$i" -eq 30 ]]; then
    echo "FAIL: Nakama did not recover during Gate D" >&2
    exit 1
  fi
  sleep 1
done
wait_for_log "$PLAYER1_LOG" "[RANKED][RECONNECT] Socket connected" "Reconnect recovery observed"

echo "==> Gate E: no uncaught exceptions"
if grep -Eq "Unhandled exception|CRASH|ERROR:.*Unhandled" "$PLAYER1_LOG" "$PLAYER2_LOG"; then
  echo "FAIL: Found uncaught exception markers in logs" >&2
  exit 1
fi

if grep -Fq "[RANKED][E2E] Forcing ranked report checkpoint" "$PLAYER1_LOG" "$PLAYER2_LOG"; then
  echo "FAIL: Forced report fallback was used during E2E run" >&2
  exit 1
fi

if grep -Fq "opponent=Opponent(" "$PLAYER1_LOG" "$PLAYER2_LOG"; then
  echo "FAIL: Placeholder opponent metadata detected in match log" >&2
  exit 1
fi

echo "Ranked E2E passed."
