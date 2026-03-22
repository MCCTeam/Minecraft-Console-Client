#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
RUN_ROOT="${TMPDIR:-/tmp}/mcc-integration-testing"
RUN_ID="$(date +%Y%m%d-%H%M%S)"
RUN_DIR="$RUN_ROOT/$RUN_ID"
SERVER_LOG_FILE="$MCC_SERVERS/$VERSION/logs/latest.log"
MCC_LOG="$RUN_DIR/mcc.log"
BUILD_LOG="$RUN_DIR/build.log"
SERVER_TMUX_LOG="$RUN_DIR/server-tmux.log"
SERVER_FILE_LOG="$RUN_DIR/server-latest.log"
INPUT_FILE="$REPO_ROOT/mcc_input.txt"
MCC_PID=""

mkdir -p "$RUN_DIR"

cleanup() {
    if [[ -n "${MCC_PID:-}" ]] && kill -0 "$MCC_PID" 2>/dev/null; then
        mcc-cmd "quit" >/dev/null 2>&1 || true
        sleep 2
        kill "$MCC_PID" 2>/dev/null || true
        wait "$MCC_PID" 2>/dev/null || true
    fi

    mc-stop "$VERSION" >/dev/null 2>&1 || true
}
trap cleanup EXIT

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

wait_for_server_ready() {
    local timeout="${1:-60}"
    local elapsed=0

    while (( elapsed < timeout )); do
        if mc-log "$VERSION" 250 2>/dev/null | grep -Fq "Done ("; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    echo "Timed out waiting for server readiness" >&2
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
    echo "SERVER> $cmd"
    mc-rcon "$cmd" >/dev/null || fail "Server command failed: $cmd"
}

run_mcc_command() {
    local cmd="$1"
    echo "MCC> $cmd"
    mcc-cmd "$cmd"
    sleep 2
}

"$SCRIPT_DIR/ensure_offline_server.sh" "$VERSION"

: > "$INPUT_FILE"

echo "Building MCC..."
mcc-build > "$BUILD_LOG" 2>&1 || fail "mcc-build failed"

echo "Starting server..."
mc-start "$VERSION" >/dev/null
wait_for_server_ready || fail "Server did not become ready"

echo "Starting MCC..."
mcc-run 25565 > "$MCC_LOG" 2>&1 &
MCC_PID=$!

wait_for_file_pattern "$MCC_LOG" "Server was successfully joined." "MCC join success" 90 || fail "MCC failed to join"
wait_for_server_log_pattern "CursorBot joined the game" "server join entry" 30 || fail "Server never logged the join"

run_server_command "op CursorBot"
run_server_command "gamerule sendCommandFeedback true"
run_server_command "gamerule logAdminCommands true"
run_server_command "time set day"
run_server_command "weather clear"
sleep 2

run_mcc_command "health"
run_mcc_command "list"
run_mcc_command "inventory player list"
run_mcc_command "/gamemode creative"
run_mcc_command "inventory creativegive 36 Diamond 16"
run_mcc_command "inventory player list"
run_mcc_command "entity"
run_mcc_command "/time query daytime"
run_mcc_command "smoke_test_from_mcc_full_spectrum"

run_server_command "execute as CursorBot at @s run summon minecraft:cow ~2 ~ ~"
run_server_command "execute as CursorBot at @s run summon minecraft:zombie ~4 ~ ~"
run_server_command "execute as CursorBot at @s run summon minecraft:creeper ~6 ~ ~"
run_server_command "execute as CursorBot at @s run summon minecraft:skeleton ~8 ~ ~"
run_server_command "execute as CursorBot at @s run summon minecraft:villager ~-2 ~ ~"
run_server_command "execute as CursorBot at @s run summon minecraft:allay ~-4 ~ ~"
run_server_command "execute as CursorBot at @s run summon minecraft:armor_stand ~ ~ ~2"

sleep 2
run_mcc_command "entity"

run_server_command "execute as CursorBot at @s run particle minecraft:happy_villager ~ ~1 ~ 0.5 0.5 0.5 0 12 force"
run_server_command "execute as CursorBot at @s run particle minecraft:end_rod ~ ~1 ~ 0.5 0.5 0.5 0.01 20 force"
run_server_command "execute as CursorBot at @s run particle minecraft:explosion ~ ~1 ~ 0 0 0 0 1 force"
run_server_command "execute as CursorBot at @s run particle minecraft:totem_of_undying ~ ~1 ~ 0.5 0.5 0.5 0.1 20 force"

run_server_command "execute as CursorBot at @s run playsound minecraft:entity.lightning_bolt.thunder master CursorBot ~ ~ ~ 1 1 0"
run_server_command "execute as CursorBot at @s run playsound minecraft:block.note_block.bell master CursorBot ~ ~ ~ 1 1 0"

run_server_command "execute as CursorBot at @s run summon minecraft:tnt ~3 ~ ~"
sleep 2
run_server_command "execute as CursorBot at @s run summon minecraft:tnt ~6 ~ ~"

sleep 6
capture_server_logs

assert_contains "$MCC_LOG" "Server was successfully joined." "MCC never joined the server"
assert_contains "$MCC_LOG" "[FileInput] > inventory player list" "Inventory command was not executed"
assert_contains "$MCC_LOG" "[FileInput] > entity" "Entity command was not executed"
assert_contains "$MCC_LOG" "[FileInput] > /gamemode creative" "Creative mode command was not executed from MCC"
assert_contains "$MCC_LOG" "Requested Diamond x16 in slot #36" "Creative inventory give did not succeed"
assert_contains "$MCC_LOG" "smoke_test_from_mcc_full_spectrum" "Client-originated chat was not observed"
assert_not_contains "$MCC_LOG" "Please enable InventoryHandling" "Inventory handling is still disabled"
assert_not_contains "$MCC_LOG" "Please enable EntityHandling" "Entity handling is still disabled"
assert_not_contains "$MCC_LOG" "You must be in Creative gamemode" "Creative mode was not active when creativegive ran"
assert_not_contains "$MCC_LOG" "Failed to load settings" "MCC failed to reload its config"

assert_contains "$SERVER_FILE_LOG" "CursorBot joined the game" "Server never saw CursorBot join"
assert_contains "$SERVER_FILE_LOG" "smoke_test_from_mcc_full_spectrum" "Server never received the client chat message"
assert_contains "$SERVER_FILE_LOG" "Displaying particle minecraft:happy_villager" "Particle events were not recorded on the server"
assert_contains "$SERVER_FILE_LOG" "Played sound minecraft:block.note_block.bell to CursorBot" "Sound events were not recorded on the server"
assert_contains "$SERVER_FILE_LOG" "Summoned new Primed TNT" "TNT summon did not occur on the server"
assert_not_contains "$SERVER_FILE_LOG" "Sending unknown packet 'clientbound/minecraft:disconnect'" "Server hit the disconnect packet regression during the test"

cat <<EOF
PASS
Run directory: $RUN_DIR
MCC log: $MCC_LOG
Server log: $SERVER_FILE_LOG
Build log: $BUILD_LOG
EOF
