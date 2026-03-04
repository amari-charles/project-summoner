#!/bin/bash
# Launch two game instances for multiplayer testing
#
# This script:
# 1. Ensures Nakama server is running (via docker-compose)
# 2. Launches Player 1 instance (goes to online screen)
# 3. Launches Player 2 instance (goes to online screen with --player2 flag)
#
# Both instances will auto-navigate to the online screen and you can
# click "Find Match" on both to test matchmaking.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GODOT_PATH="${GODOT_PATH:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Multiplayer Test Launcher ===${NC}"

# Check if Godot exists
if [ ! -f "$GODOT_PATH" ]; then
    echo -e "${RED}Error: Godot not found at $GODOT_PATH${NC}"
    echo "Set GODOT_PATH environment variable to your Godot_mono.app path"
    exit 1
fi

# Navigate to project root
cd "$PROJECT_ROOT"

# Check if docker-compose.yml exists
if [ -f "docker-compose.yml" ]; then
    echo -e "${YELLOW}Checking Nakama server...${NC}"

    # Check if Nakama is already running
    if docker compose ps 2>/dev/null | grep -q "nakama.*Up"; then
        echo -e "${GREEN}Nakama is already running${NC}"
    else
        echo -e "${YELLOW}Starting Nakama server...${NC}"
        docker compose up -d

        # Wait for Nakama to be ready via healthcheck
        echo "Waiting for Nakama to be ready..."
        for i in $(seq 1 30); do
            if curl -s http://127.0.0.1:7350/healthcheck > /dev/null 2>&1; then
                echo -e "${GREEN}Nakama is ready${NC}"
                break
            fi
            if [ "$i" -eq 30 ]; then
                echo -e "${RED}Warning: Nakama may not have started properly${NC}"
                echo "Check with: docker compose logs nakama"
            fi
            sleep 1
        done
    fi
else
    echo -e "${YELLOW}No docker-compose.yml found - skipping Nakama startup${NC}"
    echo "Make sure your Nakama server is running manually"
fi

echo ""
echo -e "${GREEN}Launching game instances...${NC}"
echo ""

# Build C# first to ensure everything is compiled
echo -e "${YELLOW}Building C# project...${NC}"
dotnet build --verbosity quiet

# Window settings for side-by-side testing
# 16" MacBook Pro has ~1800x1169 usable space at default scaling
WINDOW_WIDTH=896
WINDOW_HEIGHT=700

# Launch Player 1 (in background) - left side of screen
echo -e "${GREEN}Launching Player 1...${NC}"
"$GODOT_PATH" --path . --windowed --resolution ${WINDOW_WIDTH}x${WINDOW_HEIGHT} --position 0,50 --display/window/size/mode 0 -- --goto-online &
PLAYER1_PID=$!

# Small delay to avoid race conditions
sleep 1

# Launch Player 2 (in background) - right side of screen
echo -e "${GREEN}Launching Player 2...${NC}"
"$GODOT_PATH" --path . --windowed --resolution ${WINDOW_WIDTH}x${WINDOW_HEIGHT} --position ${WINDOW_WIDTH},50 --display/window/size/mode 0 -- --player2 --goto-online &
PLAYER2_PID=$!

echo ""
echo -e "${GREEN}Both instances launched!${NC}"
echo ""
echo "Player 1 PID: $PLAYER1_PID"
echo "Player 2 PID: $PLAYER2_PID"
echo ""
echo -e "${YELLOW}Instructions:${NC}"
echo "  1. Both windows should open at the Online screen"
echo "  2. Click 'Find Match' on both instances"
echo "  3. They should match and start a battle"
echo ""
echo -e "${YELLOW}To stop:${NC}"
echo "  Close both game windows, or run: kill $PLAYER1_PID $PLAYER2_PID"
echo ""

# Wait for both processes (optional - comment out if you want script to exit)
# wait $PLAYER1_PID $PLAYER2_PID
