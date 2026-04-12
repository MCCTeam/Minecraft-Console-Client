#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"
# shellcheck source=.skills/mcc-integration-testing/scripts/common.sh
source "$SCRIPT_DIR/common.sh"

VERSION="${1:-1.21.11-Vanilla}"
MC_VERSION="${VERSION%-Vanilla}"
if [[ "$MC_VERSION" == "$VERSION" ]]; then
    MC_VERSION="$VERSION"
fi

SESSION_A="parallel-smoke-a-${MC_VERSION//[^a-zA-Z0-9]/_}"
SESSION_B="parallel-smoke-b-${MC_VERSION//[^a-zA-Z0-9]/_}"
USERNAME_A="SmokeA"
USERNAME_B="SmokeB"

RUN_ROOT="${TMPDIR:-/tmp}/mcc-integration-testing"
RUN_ID="$(date +%Y%m%d-%H%M%S)"
RUN_DIR="$RUN_ROOT/parallel-smoke-$RUN_ID"
BUILD_LOG="$RUN_DIR/build.log"
SERVER_TMUX_LOG="$RUN_DIR/server-tmux.log"
SERVER_FILE_LOG="$RUN_DIR/server-latest.log"
SERVER_LOG_FILE="$MCC_SERVERS/$VERSION/logs/latest.log"

LOG_A="$(_mcc_session_log_file "$SESSION_A")"
LOG_B="$(_mcc_session_log_file "$SESSION_B")"
INPUT_A="$(_mcc_session_input_file "$SESSION_A")"
INPUT_B="$(_mcc_session_input_file "$SESSION_B")"
PID_A_FILE="$(_mcc_session_pid_file "$SESSION_A")"
PID_B_FILE="$(_mcc_session_pid_file "$SESSION_B")"

mkdir -p "$RUN_DIR"

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

cleanup() {
    mcc-cmd --session "$SESSION_A" "quit" >/dev/null 2>&1 || true
    mcc-cmd --session "$SESSION_B" "quit" >/dev/null 2>&1 || true
    sleep 1
    mcc-kill --session "$SESSION_A" >/dev/null 2>&1 || true
    mcc-kill --session "$SESSION_B" >/dev/null 2>&1 || true
    mc-stop "$VERSION" >/dev/null 2>&1 || true
    wait_for_server_stop "$VERSION" 20 >/dev/null 2>&1 || true
}
trap cleanup EXIT

assert_session_alive() {
    local session="$1"
    local pid_file="$2"
    if [[ -s "$pid_file" ]]; then
        local pid
        pid="$(tr -cd '0-9' < "$pid_file")"
        if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
            return 0
        fi
    fi

    tmux has-session -t "$(_mcc_tmux_session_name "$session")" 2>/dev/null
}

assert_server_alive() {
    server_running "$VERSION" || fail "Shared server session is not alive"
}

start_file_input_session() {
    local session="$1"
    local username="$2"
    bash "$REPO_ROOT/tools/mcc-debug.sh" \
        --version "$VERSION" \
        --file-input \
        --no-build \
        --session "$session" \
        --username "$username" >/dev/null
}

bash "$SCRIPT_DIR/preflight_test_env.sh" "$VERSION" >/dev/null
bash "$SCRIPT_DIR/reset_shared_test_state.sh" "$VERSION" >/dev/null
"$SCRIPT_DIR/ensure_offline_server.sh" "$VERSION" >/dev/null
mcc-reset-session --session "$SESSION_A" >/dev/null
mcc-reset-session --session "$SESSION_B" >/dev/null

echo "Building MCC..."
mcc-build > "$BUILD_LOG" 2>&1 || fail "mcc-build failed"

echo "Starting shared server..."
mc-start "$VERSION" >/dev/null
wait_for_server_ready "$VERSION" || fail "Server did not become ready"
SERVER_PORT="$(bash "$SCRIPT_DIR/get_server_port.sh" "$VERSION")"
if [[ -z "$SERVER_PORT" ]]; then
    fail "Failed to resolve server port"
fi

echo "Starting MCC session A..."
start_file_input_session "$SESSION_A" "$USERNAME_A"
echo "Starting MCC session B..."
start_file_input_session "$SESSION_B" "$USERNAME_B"

wait_for_file_pattern "$LOG_A" "Server was successfully joined." "session A join success" 90 || fail "Session A failed to join"
wait_for_file_pattern "$LOG_B" "Server was successfully joined." "session B join success" 90 || fail "Session B failed to join"
wait_for_server_log_pattern "$USERNAME_A joined the game" "server join for session A" 30 || fail "Server never logged $USERNAME_A join"
wait_for_server_log_pattern "$USERNAME_B joined the game" "server join for session B" 30 || fail "Server never logged $USERNAME_B join"

echo "Sending debug state to both sessions..."
mcc-cmd --session "$SESSION_A" "debug state"
mcc-cmd --session "$SESSION_B" "debug state"
sleep 2
wait_for_file_pattern "$LOG_A" "[FileInput] > debug state" "session A debug state command" 20 || fail "Session A did not consume debug state"
wait_for_file_pattern "$LOG_B" "[FileInput] > debug state" "session B debug state command" 20 || fail "Session B did not consume debug state"

echo "Killing session A..."
mcc-kill --session "$SESSION_A" >/dev/null 2>&1 || true
sleep 2

assert_session_alive "$SESSION_B" "$PID_B_FILE" || fail "Session B is not alive after killing session A"
assert_server_alive

echo "Verifying session B still responds..."
mcc-cmd --session "$SESSION_B" "health"
wait_for_file_pattern "$LOG_B" "[FileInput] > health" "session B health command after session A kill" 20 || fail "Session B stopped responding after session A kill"

if [[ -s "$PID_A_FILE" ]]; then
    pid_a="$(tr -cd '0-9' < "$PID_A_FILE")"
    if [[ -n "$pid_a" ]] && kill -0 "$pid_a" 2>/dev/null; then
        fail "Session A is still alive after mcc-kill"
    fi
fi

capture_server_logs

cat <<EOF
PASS
Run directory: $RUN_DIR
Server version: $VERSION
Server port: $SERVER_PORT
Session A: $SESSION_A ($USERNAME_A)
Session A input: $INPUT_A
Session A log: $LOG_A
Session B: $SESSION_B ($USERNAME_B)
Session B input: $INPUT_B
Session B log: $LOG_B
Server log: $SERVER_FILE_LOG
Build log: $BUILD_LOG
EOF
