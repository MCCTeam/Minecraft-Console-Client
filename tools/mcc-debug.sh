#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

usage() {
    cat <<'EOF'
Usage: tools/mcc-debug.sh [options]

One-step build, server start, and MCC launch for debugging.

Options:
  -v, --version VER     Server directory name (default: 1.21.11-Vanilla)
  -m, --mode MODE       Console mode: classic or tui (default: classic)
  -p, --port PORT       Server port (default: 25565)
  --no-build            Skip dotnet build
  --debug-on            Enable debug messages from the start
  --file-input          Use FileInput mode (classic only; enables mcc-cmd)
  -h, --help            Show this help

Examples:
  tools/mcc-debug.sh                              # Classic mode, default server
  tools/mcc-debug.sh -m tui                       # TUI mode
  tools/mcc-debug.sh -v 1.21.11-Vanilla --debug-on
  tools/mcc-debug.sh --file-input                 # FileInput for script-driven testing
EOF
}

VERSION="1.21.11-Vanilla"
MODE="classic"
PORT="25565"
DO_BUILD=true
DEBUG_ON=false
FILE_INPUT=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        -v|--version) VERSION="$2"; shift 2 ;;
        -m|--mode)    MODE="$2"; shift 2 ;;
        -p|--port)    PORT="$2"; shift 2 ;;
        --no-build)   DO_BUILD=false; shift ;;
        --debug-on)   DEBUG_ON=true; shift ;;
        --file-input) FILE_INPUT=true; shift ;;
        -h|--help)    usage; exit 0 ;;
        *)            echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
    esac
done

TEST_ROOT="${TMPDIR:-/tmp}/mcc-debug"
CFG="$TEST_ROOT/MinecraftClient.debug.ini"
MCC_LOG="$TEST_ROOT/mcc-debug.log"
INPUT_FILE="$REPO_ROOT/mcc_input.txt"
SESSION_NAME="mc-${VERSION//\./_}"

mkdir -p "$TEST_ROOT"

echo "=== MCC Debug Session ==="
echo "  Server:  $VERSION (port $PORT)"
echo "  Mode:    $MODE"
echo "  Config:  $CFG"
echo "  Log:     $MCC_LOG"
echo ""

# --- Build ---
if $DO_BUILD; then
    echo "[1/4] Building MCC..."
    dotnet build "$REPO_ROOT/MinecraftClient.sln" -c Release -v quiet --nologo
    echo "  Build OK"
else
    echo "[1/4] Build skipped (--no-build)"
fi

# --- Prepare config ---
echo "[2/4] Preparing config..."
cp "$REPO_ROOT/MinecraftClient.ini" "$CFG"

sed -i \
    -e 's/Account = { Login = "[^"]*", Password = "[^"]*" }/Account = { Login = "CursorBot", Password = "-" }/' \
    -e 's/TerrainAndMovements = false/TerrainAndMovements = true/' \
    -e 's/InventoryHandling = false/InventoryHandling = true/' \
    -e 's/EntityHandling = false/EntityHandling = true/' \
    "$CFG"

if [[ "$MODE" == "tui" ]]; then
    sed -i 's/ConsoleMode = "classic"/ConsoleMode = "tui"/' "$CFG"
fi

if $DEBUG_ON; then
    sed -i 's/DebugMessages = false/DebugMessages = true/' "$CFG"
fi

echo "  Config ready"

# --- Start server ---
echo "[3/4] Starting server $VERSION..."
if tmux has-session -t "$SESSION_NAME" 2>/dev/null; then
    echo "  Server already running"
else
    # Ensure offline mode
    SERVER_DIR="$MCC_SERVERS/$VERSION"
    if [[ -f "$SERVER_DIR/server.properties" ]]; then
        sed -i 's/^online-mode=.*/online-mode=false/' "$SERVER_DIR/server.properties"
        grep -q "^enable-rcon=" "$SERVER_DIR/server.properties" || echo "enable-rcon=true" >> "$SERVER_DIR/server.properties"
        grep -q "^rcon.password=" "$SERVER_DIR/server.properties" || echo "rcon.password=test123" >> "$SERVER_DIR/server.properties"
        grep -q "^rcon.port=" "$SERVER_DIR/server.properties" || echo "rcon.port=25575" >> "$SERVER_DIR/server.properties"
    fi
    mc-start "$VERSION" >/dev/null

    echo -n "  Waiting for server..."
    for i in $(seq 1 60); do
        if mc-log "$VERSION" 250 2>/dev/null | grep -Fq "Done ("; then
            echo " ready (${i}s)"
            break
        fi
        echo -n "."
        sleep 1
        if [[ $i -eq 60 ]]; then
            echo " TIMEOUT"
            echo "Server failed to start. Check: tmux attach -t $SESSION_NAME"
            exit 1
        fi
    done
fi

# --- Launch MCC ---
echo "[4/4] Launching MCC in $MODE mode..."
: > "$INPUT_FILE"
rm -f "$MCC_LOG"

MCC_ARGS=("$CFG" "CursorBot" "-" "localhost:$PORT")

if [[ "$MODE" == "tui" ]]; then
    # TUI mode: needs a real tty — no pipes or redirects allowed
    tmux kill-session -t mcc-debug 2>/dev/null || true
    tmux new-session -d -s mcc-debug -x 160 -y 50 \
        "cd '$REPO_ROOT' && dotnet run --project MinecraftClient -c Release --no-build -- ${MCC_ARGS[*]}; echo '=== MCC EXITED ==='; sleep 600"
    echo ""
    echo "  TUI mode started in tmux session 'mcc-debug'"
    echo "  (TUI mode uses a real terminal; log file is not available, use MCC's /debug command)"
    echo ""
    echo "  Attach:   tmux attach -t mcc-debug"
    echo "  Detach:   Ctrl+B, D"
    echo "  Kill MCC: tmux kill-session -t mcc-debug"
    echo ""
elif $FILE_INPUT; then
    # FileInput mode: run in background, drive via mcc_input.txt
    (
        cd "$REPO_ROOT"
        MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release --no-build -- "${MCC_ARGS[@]}" > "$MCC_LOG" 2>&1
    ) &
    MCC_PID=$!
    echo "  MCC PID: $MCC_PID"
    echo ""

    sleep 5
    if kill -0 "$MCC_PID" 2>/dev/null; then
        if grep -Fq "Server was successfully joined" "$MCC_LOG" 2>/dev/null; then
            echo "  MCC connected successfully!"
        else
            echo "  MCC started (check $MCC_LOG for status)"
        fi
    else
        echo "  MCC exited unexpectedly. Check $MCC_LOG"
        exit 1
    fi

    echo ""
    echo "  Send commands: echo 'debug state' >> $INPUT_FILE"
    echo "  Tail log:      tail -f $MCC_LOG"
    echo "  Stop MCC:      echo 'quit' >> $INPUT_FILE"
    echo "  Stop server:   mc-stop $VERSION"
    echo ""
else
    # Interactive classic mode: run in tmux (no pipe — ConsoleInteractive also needs tty)
    tmux kill-session -t mcc-debug 2>/dev/null || true
    tmux new-session -d -s mcc-debug -x 160 -y 50 \
        "cd '$REPO_ROOT' && dotnet run --project MinecraftClient -c Release --no-build -- ${MCC_ARGS[*]}; echo '=== MCC EXITED ==='; sleep 600"
    echo ""
    echo "  Classic mode started in tmux session 'mcc-debug'"
    echo ""
    echo "  Attach:   tmux attach -t mcc-debug"
    echo "  Detach:   Ctrl+B, D"
    echo "  Kill MCC: tmux kill-session -t mcc-debug"
    echo "  Note: Use MCC's built-in /debug command or enable LogToFile for log output"
    echo ""
fi

echo "Quick commands:"
echo "  mc-rcon 'op CursorBot'      # Give operator"
echo "  mc-rcon 'gamemode creative'  # Creative mode"
echo "  mc-stop $VERSION             # Stop server"
