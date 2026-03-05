#!/usr/bin/env bash
# Unified local test runner for Project Summoner.
#
# Default flow:
#   1) GDScript static type/parse check
#   2) C# tests (dotnet test)
#   3) GDScript tests (GUT via Godot .NET binary)
#
# Usage:
#   ./tools/run_tests.sh
#   ./tools/run_tests.sh --fast         # typecheck + dotnet
#   ./tools/run_tests.sh --dotnet-only
#   ./tools/run_tests.sh --gut-only
#   ./tools/run_tests.sh --typecheck-only

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GODOT_PATH="${GODOT_PATH:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"

RUN_TYPECHECK=1
RUN_DOTNET=1
RUN_GUT=1

for arg in "$@"; do
    case "$arg" in
        --fast|--dotnet-only)
            RUN_GUT=0
            ;;
        --gut-only)
            RUN_DOTNET=0
            ;;
        --typecheck-only)
            RUN_DOTNET=0
            RUN_GUT=0
            ;;
        --skip-typecheck)
            RUN_TYPECHECK=0
            ;;
        -h|--help)
            cat <<'USAGE'
Usage: ./tools/run_tests.sh [options]

Options:
  --fast, --dotnet-only   Run typecheck + dotnet C# tests
  --gut-only              Run only GUT (GDScript) tests
  --typecheck-only        Run only GDScript type/parse checker
  --skip-typecheck        Skip GDScript type/parse checker
  -h, --help              Show help
USAGE
            exit 0
            ;;
        *)
            echo "Unknown option: $arg"
            echo "Use --help for usage"
            exit 1
            ;;
    esac
done

cd "$PROJECT_ROOT"

TOTAL_PHASES=$((RUN_TYPECHECK + RUN_DOTNET + RUN_GUT))
CURRENT_PHASE=0

if [[ $RUN_TYPECHECK -eq 1 ]]; then
    CURRENT_PHASE=$((CURRENT_PHASE + 1))
    echo ""
    echo "==> Phase ${CURRENT_PHASE}/${TOTAL_PHASES}: GDScript type/parse check"
    "$PROJECT_ROOT/tools/check_gdscript_types.sh"
fi

if [[ $RUN_DOTNET -eq 1 ]]; then
    CURRENT_PHASE=$((CURRENT_PHASE + 1))
    echo ""
    echo "==> Phase ${CURRENT_PHASE}/${TOTAL_PHASES}: dotnet test"
    dotnet test --settings test.runsettings
fi

if [[ $RUN_GUT -eq 1 ]]; then
    CURRENT_PHASE=$((CURRENT_PHASE + 1))
    echo ""
    echo "==> Phase ${CURRENT_PHASE}/${TOTAL_PHASES}: GUT (Godot .NET)"
    if [[ ! -x "$GODOT_PATH" ]]; then
        echo "Error: Godot binary not found at '$GODOT_PATH'"
        echo "Set GODOT_PATH to your Godot .NET binary path."
        exit 1
    fi

    "$GODOT_PATH" --headless --path . -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit
fi

echo ""
echo "All requested test phases passed."
