#!/usr/bin/env bash
# Unified local test runner for Project Summoner.
#
# Default flow:
#   1) C# tests (dotnet test)
#   2) GDScript tests (GUT via Godot .NET binary)
#
# Usage:
#   ./tools/run_tests.sh
#   ./tools/run_tests.sh --fast         # dotnet only
#   ./tools/run_tests.sh --dotnet-only
#   ./tools/run_tests.sh --gut-only

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GODOT_PATH="${GODOT_PATH:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"

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
        -h|--help)
            cat <<'USAGE'
Usage: ./tools/run_tests.sh [options]

Options:
  --fast, --dotnet-only   Run only dotnet C# tests
  --gut-only              Run only GUT (GDScript) tests
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

if [[ $RUN_DOTNET -eq 1 ]]; then
    echo ""
    echo "==> Phase 1/2: dotnet test"
    dotnet test --settings test.runsettings
fi

if [[ $RUN_GUT -eq 1 ]]; then
    echo ""
    echo "==> Phase 2/2: GUT (Godot .NET)"
    if [[ ! -x "$GODOT_PATH" ]]; then
        echo "Error: Godot binary not found at '$GODOT_PATH'"
        echo "Set GODOT_PATH to your Godot .NET binary path."
        exit 1
    fi

    "$GODOT_PATH" --headless --path . -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit
fi

echo ""
echo "All requested test phases passed."
