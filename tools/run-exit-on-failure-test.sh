#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-26.1-Vanilla}"
MC_VERSION="${VERSION%-Vanilla}"
RUN_DIR="${TMPDIR:-/tmp}/mcc-exit-on-failure/$(date +%Y%m%d-%H%M%S)"
PREPARE_CONFIG="$REPO_ROOT/.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh"
ENSURE_SERVER="$REPO_ROOT/.skills/mcc-integration-testing/scripts/ensure_offline_server.sh"
GET_SERVER_PORT="$REPO_ROOT/.skills/mcc-integration-testing/scripts/get_server_port.sh"
TEST_USERNAME="mcc_exit_failure"
CURRENT_PID=""

mkdir -p "$RUN_DIR"

cleanup() {
    if [[ -n "$CURRENT_PID" ]] && kill -0 "$CURRENT_PID" 2>/dev/null; then
        kill "$CURRENT_PID" 2>/dev/null || true
        wait "$CURRENT_PID" 2>/dev/null || true
    fi
}
trap cleanup EXIT

fail() {
    echo "FAIL: $1" >&2
    echo "Run directory: $RUN_DIR" >&2
    exit 1
}

wait_for_log() {
    local log_file="$1"
    local pattern="$2"
    local description="$3"
    local timeout_seconds="${4:-60}"
    local elapsed=0

    while (( elapsed < timeout_seconds )); do
        if [[ -f "$log_file" ]] && grep -Fq "$pattern" "$log_file"; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    fail "Timed out waiting for $description"
}

wait_for_exit() {
    local pid="$1"
    local timeout_seconds="$2"
    local elapsed=0

    while kill -0 "$pid" 2>/dev/null; do
        if (( elapsed >= timeout_seconds )); then
            return 124
        fi
        sleep 1
        ((elapsed += 1))
    done

    wait "$pid"
}

start_mcc() {
    local config="$1"
    local port="$2"
    local log_file="$3"
    local input_file="$4"

    : > "$input_file"
    MCC_FILE_INPUT=1 MCC_INPUT_FILE="$input_file" \
        _mcc_dotnet_env dotnet run --project "$REPO_ROOT/MinecraftClient" -c Release --no-build -- \
        "$config" "$TEST_USERNAME" "-" "localhost:$port" > "$log_file" 2>&1 &
    CURRENT_PID=$!
}

prepare_config() {
    local output="$1"
    bash "$PREPARE_CONFIG" "$output" "$MC_VERSION" "$TEST_USERNAME" >/dev/null
}

enable_exit_on_failure() {
    local config="$1"
    sed -i 's/^ExitOnFailure = false/ExitOnFailure = true/' "$config"
    sed -i '/^\[ChatBot.AutoRelog\]/,/^\[/ s/^Enabled = false/Enabled = true/' "$config"
}

reserve_unused_port() {
    python3 -c 'import socket; sock = socket.socket(); sock.bind(("127.0.0.1", 0)); print(sock.getsockname()[1]); sock.close()'
}

run_server_command() {
    local command="$1"
    local attempt

    for attempt in 1 2 3 4 5; do
        if mc-rcon "$command"; then
            return 0
        fi
        sleep 1
    done

    fail "RCON command failed: $command"
}

bash "$REPO_ROOT/.skills/mcc-integration-testing/scripts/preflight_test_env.sh" "$VERSION"
bash "$ENSURE_SERVER" "$VERSION"
mcc-build
mc-start "$VERSION" >/dev/null

SERVER_PORT="$(bash "$GET_SERVER_PORT" "$VERSION")"
[[ -n "$SERVER_PORT" ]] || fail "Unable to resolve the server port"

for attempt in 1 2 3 4 5; do
    if run_server_command "list"; then
        break
    fi
    sleep 1
    if [[ "$attempt" == 5 ]]; then
        fail "RCON did not become ready"
    fi
done

echo "[1/3] Server kick exits promptly and bypasses AutoRelog"
KICK_CONFIG="$RUN_DIR/kick.ini"
KICK_LOG="$RUN_DIR/kick.log"
KICK_INPUT="$RUN_DIR/kick.input"
prepare_config "$KICK_CONFIG"
enable_exit_on_failure "$KICK_CONFIG"
start_mcc "$KICK_CONFIG" "$SERVER_PORT" "$KICK_LOG" "$KICK_INPUT"
wait_for_log "$KICK_LOG" "Server was successfully joined." "MCC join"
run_server_command "kick $TEST_USERNAME ExitOnFailure integration test"

if wait_for_exit "$CURRENT_PID" 15; then
    exit_code=0
else
    exit_code=$?
fi
[[ "$exit_code" != 124 ]] || fail "MCC did not exit after the server kick"
CURRENT_PID=""
[[ "$exit_code" == 2 || "$exit_code" == 3 ]] || fail "Expected nonzero kick exit code 2 or 3, got $exit_code"
[[ "$(grep -Fc "Server was successfully joined." "$KICK_LOG")" == 1 ]] || fail "MCC rejoined after a kick"

echo "[2/3] Refused initial TCP connection exits with code 3"
REFUSED_CONFIG="$RUN_DIR/refused.ini"
REFUSED_LOG="$RUN_DIR/refused.log"
REFUSED_INPUT="$RUN_DIR/refused.input"
prepare_config "$REFUSED_CONFIG"
enable_exit_on_failure "$REFUSED_CONFIG"
UNUSED_PORT="$(reserve_unused_port)"
start_mcc "$REFUSED_CONFIG" "$UNUSED_PORT" "$REFUSED_LOG" "$REFUSED_INPUT"

if wait_for_exit "$CURRENT_PID" 15; then
    exit_code=0
else
    exit_code=$?
fi
[[ "$exit_code" != 124 ]] || fail "MCC did not exit after the refused connection"
CURRENT_PID=""
[[ "$exit_code" == 3 ]] || fail "Expected connection-loss exit code 3, got $exit_code"

echo "[3/3] Interactive mode remains at the offline prompt after a kick"
INTERACTIVE_CONFIG="$RUN_DIR/interactive.ini"
INTERACTIVE_LOG="$RUN_DIR/interactive.log"
INTERACTIVE_INPUT="$RUN_DIR/interactive.input"
prepare_config "$INTERACTIVE_CONFIG"
start_mcc "$INTERACTIVE_CONFIG" "$SERVER_PORT" "$INTERACTIVE_LOG" "$INTERACTIVE_INPUT"
wait_for_log "$INTERACTIVE_LOG" "Server was successfully joined." "interactive MCC join"
run_server_command "kick $TEST_USERNAME Interactive-mode regression test"
sleep 5
kill -0 "$CURRENT_PID" 2>/dev/null || fail "Interactive MCC exited after a kick"

echo "PASS: ExitOnFailure integration test"
echo "Run directory: $RUN_DIR"
