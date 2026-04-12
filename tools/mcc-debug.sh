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
  --session NAME        Session name for scoped runtime artifacts
  --username NAME       MCC username (default: resolved from session)
  --no-build            Skip dotnet build
  --debug-on            Enable debug messages from the start
  --file-input          Use FileInput mode (classic only; enables mcc-cmd)
  -h, --help            Show this help

Examples:
  tools/mcc-debug.sh                              # Classic mode, default server
  tools/mcc-debug.sh -m tui                       # TUI mode
  tools/mcc-debug.sh -v 1.21.11-Vanilla --debug-on
  tools/mcc-debug.sh --session smoke-a --username SmokeA
  tools/mcc-debug.sh --file-input                 # FileInput for script-driven testing
EOF
}

VERSION="1.21.11-Vanilla"
MODE="classic"
PORT="25565"
PORT_SET_BY_USER=false
SESSION=""
USERNAME=""
DO_BUILD=true
DEBUG_ON=false
FILE_INPUT=false
BUILD_ROOT="$(_mcc_build_root)"

while [[ $# -gt 0 ]]; do
    case "$1" in
        -v|--version)
            if [[ $# -lt 2 ]]; then
                echo "$1 requires a value" >&2
                exit 1
            fi
            VERSION="$2"
            shift 2
            ;;
        -m|--mode)
            if [[ $# -lt 2 ]]; then
                echo "$1 requires a value" >&2
                exit 1
            fi
            MODE="$2"
            shift 2
            ;;
        -p|--port)
            if [[ $# -lt 2 ]]; then
                echo "$1 requires a value" >&2
                exit 1
            fi
            PORT="$2"
            PORT_SET_BY_USER=true
            shift 2
            ;;
        --session)
            if [[ $# -lt 2 ]]; then
                echo "--session requires a value" >&2
                exit 1
            fi
            SESSION="$2"
            shift 2
            ;;
        --username)
            if [[ $# -lt 2 ]]; then
                echo "--username requires a value" >&2
                exit 1
            fi
            USERNAME="$2"
            shift 2
            ;;
        --no-build)   DO_BUILD=false; shift ;;
        --debug-on)   DEBUG_ON=true; shift ;;
        --file-input) FILE_INPUT=true; shift ;;
        -h|--help)    usage; exit 0 ;;
        *)            echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
    esac
done

SESSION="$(_mcc_resolve_session "$SESSION")"
if [[ -z "$USERNAME" ]]; then
    USERNAME="$(_mcc_resolve_username "$SESSION")"
fi

if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    mkdir -p "$BUILD_ROOT"
fi

SESSION_ROOT="$(_mcc_session_root "$SESSION")"
CFG="$SESSION_ROOT/MinecraftClient.debug.ini"
MCC_LOG="$(_mcc_session_log_file "$SESSION")"
INPUT_FILE="$(_mcc_session_input_file "$SESSION")"
PID_FILE="$(_mcc_session_pid_file "$SESSION")"
META_FILE="$(_mcc_session_meta_file "$SESSION")"
MCC_TMUX_SESSION="$(_mcc_tmux_session_name "$SESSION")"
SESSION_NAME="mc-${VERSION//\./_}"
PREPARE_CFG_SCRIPT="$REPO_ROOT/.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh"
ENSURE_SERVER_SCRIPT="$REPO_ROOT/.skills/mcc-integration-testing/scripts/ensure_offline_server.sh"
PREFLIGHT_SCRIPT="$REPO_ROOT/.skills/mcc-integration-testing/scripts/preflight_test_env.sh"
GET_PORT_SCRIPT="$REPO_ROOT/.skills/mcc-integration-testing/scripts/get_server_port.sh"

mkdir -p "$SESSION_ROOT"

echo "=== MCC Debug Session ==="
echo "  Server:  $VERSION (port $PORT)"
echo "  Session: $SESSION"
echo "  User:    $USERNAME"
echo "  Mode:    $MODE"
echo "  Build:   $BUILD_ROOT"
echo "  Root:    $SESSION_ROOT"
echo "  Config:  $CFG"
echo "  Log:     $MCC_LOG"
echo "  Input:   $INPUT_FILE"
echo "  PID:     $PID_FILE"
echo "  Meta:    $META_FILE"
echo "  Tmux:    $MCC_TMUX_SESSION"
echo ""

bash "$PREFLIGHT_SCRIPT" "$VERSION" >/dev/null

# --- Build ---
if $DO_BUILD; then
    echo "[1/4] Building MCC..."
    _mcc_dotnet_env dotnet build "$REPO_ROOT/MinecraftClient.sln" -c Release -v quiet --nologo
    echo "  Build OK"
else
    echo "[1/4] Build skipped (--no-build)"
fi

# --- Prepare config ---
echo "[2/4] Preparing config..."
bash "$PREPARE_CFG_SCRIPT" "$CFG" "${VERSION%-Vanilla}" "$USERNAME" >/dev/null

if [[ "$MODE" == "tui" ]]; then
    if [[ "$(uname)" == "Darwin" ]]; then
        sed -i '' 's/ConsoleMode = "classic"/ConsoleMode = "tui"/' "$CFG"
    else
        sed -i 's/ConsoleMode = "classic"/ConsoleMode = "tui"/' "$CFG"
    fi
fi

if $DEBUG_ON; then
    if [[ "$(uname)" == "Darwin" ]]; then
        sed -i '' 's/DebugMessages = false/DebugMessages = true/' "$CFG"
    else
        sed -i 's/DebugMessages = false/DebugMessages = true/' "$CFG"
    fi
fi

echo "  Config ready"

# --- Start server ---
echo "[3/4] Starting server $VERSION..."
if tmux has-session -t "$SESSION_NAME" 2>/dev/null; then
    echo "  Server already running"
else
    bash "$ENSURE_SERVER_SCRIPT" "$VERSION" >/dev/null
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

if ! $PORT_SET_BY_USER; then
    PORT="$(bash "$GET_PORT_SCRIPT" "$VERSION")"
fi

RUNTIME_MODE="$MODE"
if $FILE_INPUT; then
    RUNTIME_MODE="${MODE}-file-input"
fi

cat > "$META_FILE" <<EOF
session=$SESSION
user=$USERNAME
mode=$RUNTIME_MODE
config=$CFG
log=$MCC_LOG
input=$INPUT_FILE
pid=$PID_FILE
tmux=$MCC_TMUX_SESSION
server_version=$VERSION
server_port=$PORT
EOF

# --- Launch MCC ---
echo "[4/4] Launching MCC in $MODE mode..."
: > "$INPUT_FILE"
rm -f "$MCC_LOG"
rm -f "$PID_FILE"

MCC_ARGS=("$CFG" "$USERNAME" "-" "localhost:$PORT")
MCC_ARGS_CMD="$(printf '%q ' "${MCC_ARGS[@]}")"

RUNTIME_APP="$(_mcc_runtime_app_path || true)"
if [[ -z "$RUNTIME_APP" ]]; then
    echo "  Failed to find built MCC runtime under $(_mcc_runtime_output_dir)" >&2
    echo "  Build first with: source tools/mcc-env.sh && mcc-build" >&2
    exit 1
fi

if [[ "$RUNTIME_APP" == *.dll ]]; then
    MCC_LAUNCHER=(dotnet "$RUNTIME_APP")
else
    MCC_LAUNCHER=("$RUNTIME_APP")
fi
MCC_LAUNCHER_CMD="$(printf '%q ' "${MCC_LAUNCHER[@]}")"

if [[ "$MODE" == "tui" ]]; then
    # TUI mode: needs a real tty - no pipes or redirects allowed
    tmux kill-session -t "$MCC_TMUX_SESSION" 2>/dev/null || true
    tmux new-session -d -s "$MCC_TMUX_SESSION" -x 160 -y 50 \
        "cd '$REPO_ROOT' && $MCC_LAUNCHER_CMD $MCC_ARGS_CMD; echo '=== MCC EXITED ==='; sleep 600"
    echo ""
    echo "  TUI mode started in tmux session '$MCC_TMUX_SESSION'"
    echo "  (TUI mode uses a real terminal; log file is not available, use MCC's /debug command)"
    echo ""
    echo "  Attach:   tmux attach -t $MCC_TMUX_SESSION"
    echo "  Detach:   Ctrl+B, D"
    echo "  Kill MCC: tmux kill-session -t $MCC_TMUX_SESSION"
    echo ""
elif $FILE_INPUT; then
    # FileInput mode: run in detached tmux, drive via session-specific input file
    tmux kill-session -t "$MCC_TMUX_SESSION" 2>/dev/null || true
    tmux new-session -d -s "$MCC_TMUX_SESSION" -x 160 -y 50 \
        "cd '$REPO_ROOT' && printf '%s\n' \"\$\$\" > '$PID_FILE' && exec env MCC_FILE_INPUT=1 MCC_INPUT_FILE='$INPUT_FILE' $MCC_LAUNCHER_CMD $MCC_ARGS_CMD > '$MCC_LOG' 2>&1"

    for _ in $(seq 1 25); do
        if [[ -s "$PID_FILE" ]]; then
            break
        fi
        sleep 0.2
    done
    if [[ ! -s "$PID_FILE" ]]; then
        echo "  Failed to capture MCC PID in $PID_FILE"
        exit 1
    fi

    MCC_PID="$(tr -cd '0-9' < "$PID_FILE")"
    if [[ -z "$MCC_PID" ]]; then
        echo "  Invalid PID content in $PID_FILE"
        exit 1
    fi

    echo "  MCC PID: $MCC_PID"
    echo "  MCC PID file: $PID_FILE"
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
    echo "  Metadata:      cat $META_FILE"
    echo "  Attach (optional): tmux attach -t $MCC_TMUX_SESSION"
    echo "  Stop MCC:      echo 'quit' >> $INPUT_FILE"
    echo "  Stop server:   mc-stop $VERSION"
    echo "                 shared servers stay up by default; rerun with --confirm only if you really need to stop it"
    echo ""
else
    # Interactive classic mode: run in tmux (no pipe - ConsoleInteractive also needs tty)
    tmux kill-session -t "$MCC_TMUX_SESSION" 2>/dev/null || true
    tmux new-session -d -s "$MCC_TMUX_SESSION" -x 160 -y 50 \
        "cd '$REPO_ROOT' && $MCC_LAUNCHER_CMD $MCC_ARGS_CMD; echo '=== MCC EXITED ==='; sleep 600"
    echo ""
    echo "  Classic mode started in tmux session '$MCC_TMUX_SESSION'"
    echo ""
    echo "  Attach:   tmux attach -t $MCC_TMUX_SESSION"
    echo "  Detach:   Ctrl+B, D"
    echo "  Kill MCC: tmux kill-session -t $MCC_TMUX_SESSION"
    echo "  Note: Use MCC's built-in /debug command or enable LogToFile for log output"
    echo ""
fi

echo "Quick commands:"
echo "  mc-rcon 'op $USERNAME'      # Give operator"
echo "  mc-rcon 'gamemode creative'  # Creative mode"
echo "  mc-stop $VERSION             # shared server stays up by default; rerun with --confirm only when needed"
