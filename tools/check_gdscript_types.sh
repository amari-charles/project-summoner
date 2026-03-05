#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

resolve_godot_bin() {
  if [[ -n "${GODOT_BIN:-}" ]] && [[ -x "${GODOT_BIN}" ]]; then
    printf '%s\n' "${GODOT_BIN}"
    return
  fi

  if [[ -n "${GODOT_PATH:-}" ]] && [[ -x "${GODOT_PATH}" ]]; then
    printf '%s\n' "${GODOT_PATH}"
    return
  fi

  local candidates=(
    "/Applications/Godot_mono.app/Contents/MacOS/Godot"
    "/Applications/Godot.app/Contents/MacOS/Godot"
    "$(command -v godot 2>/dev/null || true)"
    "$(command -v godot4 2>/dev/null || true)"
  )

  local bin
  for bin in "${candidates[@]}"; do
    if [[ -n "${bin}" ]] && [[ -x "${bin}" ]]; then
      printf '%s\n' "${bin}"
      return
    fi
  done

  return 1
}

if ! GODOT_BIN_PATH="$(resolve_godot_bin)"; then
  echo "ERROR: Could not find a Godot binary." >&2
  echo "Set GODOT_BIN or GODOT_PATH to /path/to/Godot and re-run." >&2
  exit 2
fi

LOG_FILE="${TMPDIR:-/tmp}/gdscript_type_check.log"

"${GODOT_BIN_PATH}" --headless -e --quit --verbose --path "${ROOT_DIR}" >"${LOG_FILE}" 2>&1 || true

# These are the diagnostics we care about for type/parse safety.
ERROR_PATTERNS='SCRIPT ERROR: Parse Error|SCRIPT ERROR: Compile Error|Warning treated as error|GDScript::reload: The method|inferred type "Node"|inferred type "Variant"'

if rg -n "${ERROR_PATTERNS}" "${LOG_FILE}" >/dev/null; then
  echo "GDScript type/parse check: FAILED"
  echo "Log: ${LOG_FILE}"
  echo
  rg -n "${ERROR_PATTERNS}" "${LOG_FILE}"
  exit 1
fi

echo "GDScript type/parse check: OK"
echo "Log: ${LOG_FILE}"
