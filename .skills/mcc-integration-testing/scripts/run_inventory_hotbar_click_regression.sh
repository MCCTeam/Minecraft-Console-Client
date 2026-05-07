#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"
# shellcheck source=.skills/mcc-integration-testing/scripts/common.sh
source "$SCRIPT_DIR/common.sh"

VERSION="${1:-1.20.2}"
MC_VERSION="${VERSION%-Vanilla}"
if [[ "$MC_VERSION" == "$VERSION" ]]; then
    MC_VERSION="$VERSION"
fi

RUN_ROOT="${TMPDIR:-/tmp}/mcc-integration-testing"
RUN_ID="$(date +%Y%m%d-%H%M%S)-hotbar-click"
RUN_DIR="$RUN_ROOT/$RUN_ID"
SERVER_LOG_FILE="$MCC_SERVERS/$VERSION/logs/latest.log"
SESSION_NAME="hotbar-click-${MC_VERSION//[^a-zA-Z0-9]/_}"
TEST_USERNAME="$(_mcc_resolve_username "$SESSION_NAME")"
MCC_LOG="$(_mcc_session_log_file "$SESSION_NAME")"
PID_FILE="$(_mcc_session_pid_file "$SESSION_NAME")"
MCC_TMUX_SESSION="$(_mcc_tmux_session_name "$SESSION_NAME")"
INPUT_FILE="$(_mcc_session_input_file "$SESSION_NAME")"
CFG="$RUN_DIR/MinecraftClient.$MC_VERSION.ini"
BUILD_LOG="$RUN_DIR/build.log"
SERVER_TMUX_LOG="$RUN_DIR/server-tmux.log"
SERVER_FILE_LOG="$RUN_DIR/server-latest.log"

mkdir -p "$RUN_DIR"

cleanup() {
    mcc-cmd --session "$SESSION_NAME" "quit" >/dev/null 2>&1 || true
    sleep 2
    mcc-kill --session "$SESSION_NAME" >/dev/null 2>&1 || true

    mc-stop "$VERSION" --confirm >/dev/null 2>&1 || true
    wait_for_server_stop "$VERSION" 20 >/dev/null 2>&1 || true
}
trap cleanup EXIT

prepare_config() {
    bash "$SCRIPT_DIR/prepare_offline_mcc_config.sh" "$CFG" "$MC_VERSION" "$TEST_USERNAME" >/dev/null
}

wait_for_file_pattern() {
    local file="$1"
    local pattern="$2"
    local description="$3"
    local timeout="${4:-60}"
    local elapsed=0

    while (( elapsed < timeout )); do
        if [[ -f "$file" ]] && grep -Fq "$pattern" "$file"; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    echo "Timed out waiting for: $description" >&2
    return 1
}

wait_for_server_log_pattern() {
    local pattern="$1"
    local description="$2"
    local timeout="${3:-60}"
    local elapsed=0

    while (( elapsed < timeout )); do
        if [[ -f "$SERVER_LOG_FILE" ]] && grep -Fq "$pattern" "$SERVER_LOG_FILE"; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    echo "Timed out waiting for server log: $description" >&2
    return 1
}

capture_server_logs() {
    mc-log "$VERSION" 400 > "$SERVER_TMUX_LOG" 2>/dev/null || true
    if [[ -f "$SERVER_LOG_FILE" ]]; then
        cp "$SERVER_LOG_FILE" "$SERVER_FILE_LOG"
    fi
}

fail() {
    capture_server_logs
    echo "FAIL: $1" >&2
    echo "Run directory: $RUN_DIR" >&2
    exit 1
}

assert_contains() {
    local file="$1"
    local pattern="$2"
    local description="$3"

    grep -Fq "$pattern" "$file" || fail "$description"
}

assert_not_contains() {
    local file="$1"
    local pattern="$2"
    local description="$3"

    if grep -Fq "$pattern" "$file"; then
        fail "$description"
    fi
}

run_server_command() {
    local cmd="$1"
    local attempt
    echo "SERVER> $cmd"
    for attempt in 1 2 3 4 5; do
        if mc-rcon "$cmd" >/dev/null 2>&1; then
            return 0
        fi
        sleep 1
    done
    fail "Server command failed: $cmd"
}

run_mcc_command() {
    local cmd="$1"
    echo "MCC> $cmd"
    mcc-cmd --session "$SESSION_NAME" "$cmd"
    sleep 2
}

start_mcc_session() {
    local -a mcc_args=("$CFG" "$TEST_USERNAME" "-" "localhost:$SERVER_PORT")
    local mcc_args_cmd
    mcc_args_cmd="$(printf '%q ' "${mcc_args[@]}")"

    tmux kill-session -t "$MCC_TMUX_SESSION" 2>/dev/null || true
    rm -f "$PID_FILE"
    tmux new-session -d -s "$MCC_TMUX_SESSION" -x 160 -y 50 \
        "cd '$REPO_ROOT' && printf '%s\n' \"\$\$\" > '$PID_FILE' && exec env MCC_FILE_INPUT=1 MCC_INPUT_FILE='$INPUT_FILE' dotnet run --project MinecraftClient -c Release --no-build -- $mcc_args_cmd > '$MCC_LOG' 2>&1"

    for _ in $(seq 1 25); do
        if [[ -s "$PID_FILE" ]]; then
            return 0
        fi
        sleep 0.2
    done

    fail "Failed to capture MCC PID for session $SESSION_NAME"
}

setup_hotbar_items() {
    run_server_command "clear $TEST_USERNAME"
    run_server_command "gamemode survival $TEST_USERNAME"
    run_server_command "item replace entity $TEST_USERNAME hotbar.0 with minecraft:diamond_sword{display:{Name:'{\"text\":\"Raid Slayed\"}'},Enchantments:[{id:\"minecraft:sweeping\",lvl:3s},{id:\"minecraft:sharpness\",lvl:5s},{id:\"minecraft:looting\",lvl:3s},{id:\"minecraft:mending\",lvl:1s},{id:\"minecraft:unbreaking\",lvl:3s}]}"
    run_server_command "item replace entity $TEST_USERNAME hotbar.1 with minecraft:diamond_chestplate{display:{Name:'{\"text\":\"Raid Plate\"}'},Enchantments:[{id:\"minecraft:thorns\",lvl:3s},{id:\"minecraft:protection\",lvl:4s},{id:\"minecraft:unbreaking\",lvl:3s},{id:\"minecraft:mending\",lvl:1s}]}"
    sleep 2
}

bash "$SCRIPT_DIR/preflight_test_env.sh" "$VERSION" >/dev/null
bash "$SCRIPT_DIR/reset_shared_test_state.sh" "$VERSION" >/dev/null
"$SCRIPT_DIR/ensure_offline_server.sh" "$VERSION"
mcc-reset-session --session "$SESSION_NAME" >/dev/null

echo "Building MCC..."
mcc-build > "$BUILD_LOG" 2>&1 || fail "mcc-build failed"
prepare_config
SERVER_PORT="$(bash "$SCRIPT_DIR/get_server_port.sh" "$VERSION")"
[[ -n "$SERVER_PORT" ]] || fail "Failed to resolve server port"

mkdir -p "$(dirname "$INPUT_FILE")" "$(dirname "$MCC_LOG")"
: > "$INPUT_FILE"
rm -f "$MCC_LOG"

echo "Starting server..."
mc-start "$VERSION" >/dev/null
wait_for_server_ready "$VERSION" || fail "Server did not become ready"

echo "Starting MCC..."
start_mcc_session

wait_for_file_pattern "$MCC_LOG" "Server was successfully joined." "MCC join success" 90 || fail "MCC failed to join"
wait_for_server_log_pattern "$TEST_USERNAME joined the game" "server join entry" 30 || fail "Server never logged the join"

setup_hotbar_items
run_mcc_command "inventory player list"
run_mcc_command "inventory 0 click 36 LeftClick"
run_mcc_command "hotbar_click_regression_leftclick_ok"
wait_for_server_log_pattern "hotbar_click_regression_leftclick_ok" "left-click post-check chat" 20 || fail "Client did not stay connected after left-click"
run_mcc_command "inventory 0 click -999 LeftClick"

setup_hotbar_items
run_mcc_command "inventory player list"
run_mcc_command "inventory 0 click 37 RightClick"
run_mcc_command "hotbar_click_regression_rightclick_ok"
wait_for_server_log_pattern "hotbar_click_regression_rightclick_ok" "right-click post-check chat" 20 || fail "Client did not stay connected after right-click"
run_mcc_command "debug state"

sleep 4
capture_server_logs

assert_contains "$MCC_LOG" "[FileInput] > inventory 0 click 36 LeftClick" "Left-click regression command was not executed"
assert_contains "$MCC_LOG" "[FileInput] > inventory 0 click 37 RightClick" "Right-click regression command was not executed"
assert_contains "$MCC_LOG" "hotbar_click_regression_leftclick_ok" "Left-click post-check chat was not observed in MCC log"
assert_contains "$MCC_LOG" "hotbar_click_regression_rightclick_ok" "Right-click post-check chat was not observed in MCC log"
assert_contains "$MCC_LOG" "=== MCC Debug State ===" "MCC did not remain responsive after the regression checks"
assert_not_contains "$MCC_LOG" "Disconnected by Server" "MCC was disconnected during the hotbar click regression test"
assert_not_contains "$MCC_LOG" "Internal Exception" "MCC hit an internal exception during the hotbar click regression test"
assert_not_contains "$MCC_LOG" "larger than I expected" "MCC reproduced the oversized packet regression"
assert_not_contains "$MCC_LOG" "DecoderException" "MCC reproduced the Netty decoder regression"

assert_contains "$SERVER_FILE_LOG" "$TEST_USERNAME joined the game" "Server never saw $TEST_USERNAME join"
assert_contains "$SERVER_FILE_LOG" "hotbar_click_regression_leftclick_ok" "Server never received the left-click post-check chat"
assert_contains "$SERVER_FILE_LOG" "hotbar_click_regression_rightclick_ok" "Server never received the right-click post-check chat"

cat <<EOF
PASS
Run directory: $RUN_DIR
MCC log: $MCC_LOG
Server log: $SERVER_FILE_LOG
Build log: $BUILD_LOG
EOF
