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
RUN_ROOT="${TMPDIR:-/tmp}/mcc-integration-testing"
RUN_ID="$(date +%Y%m%d-%H%M%S)"
RUN_DIR="$RUN_ROOT/$RUN_ID"
SERVER_LOG_FILE="$MCC_SERVERS/$VERSION/logs/latest.log"
SESSION_NAME="full-spectrum-${MC_VERSION//[^a-zA-Z0-9]/_}"
TEST_USERNAME="CursorBot"
MCC_LOG="$(_mcc_session_log_file "$SESSION_NAME")"
BUILD_LOG="$RUN_DIR/build.log"
SERVER_TMUX_LOG="$RUN_DIR/server-tmux.log"
SERVER_FILE_LOG="$RUN_DIR/server-latest.log"
INPUT_FILE="$(_mcc_session_input_file "$SESSION_NAME")"
CFG="$RUN_DIR/MinecraftClient.$MC_VERSION.ini"
MCC_PID=""

mkdir -p "$RUN_DIR"

cleanup() {
    mcc-cmd --session "$SESSION_NAME" "quit" >/dev/null 2>&1 || true
    sleep 2
    mcc-kill --session "$SESSION_NAME" >/dev/null 2>&1 || true

    mc-stop "$VERSION" >/dev/null 2>&1 || true
    wait_for_server_stop "$VERSION" 20 >/dev/null 2>&1 || true
}
trap cleanup EXIT

prepare_config() {
    bash "$SCRIPT_DIR/prepare_offline_mcc_config.sh" "$CFG" "$MC_VERSION" "$TEST_USERNAME" >/dev/null
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

bash "$SCRIPT_DIR/preflight_test_env.sh" "$VERSION" >/dev/null
bash "$SCRIPT_DIR/reset_shared_test_state.sh" "$VERSION" >/dev/null
"$SCRIPT_DIR/ensure_offline_server.sh" "$VERSION"
mcc-reset-session --session "$SESSION_NAME" >/dev/null
echo "Building MCC..."
mcc-build > "$BUILD_LOG" 2>&1 || fail "mcc-build failed"
prepare_config
SERVER_PORT="$(bash "$SCRIPT_DIR/get_server_port.sh" "$VERSION")"

mkdir -p "$(dirname "$INPUT_FILE")" "$(dirname "$MCC_LOG")"
: > "$INPUT_FILE"
rm -f "$MCC_LOG"

echo "Starting server..."
mc-start "$VERSION" >/dev/null
wait_for_server_ready "$VERSION" || fail "Server did not become ready"

echo "Starting MCC..."
(
    cd "$REPO_ROOT"
    MCC_FILE_INPUT=1 MCC_INPUT_FILE="$INPUT_FILE" \
        dotnet run --project MinecraftClient -c Release --no-build -- "$CFG" "$TEST_USERNAME" - "localhost:$SERVER_PORT" > "$MCC_LOG" 2>&1
) &
MCC_PID=$!

wait_for_file_pattern "$MCC_LOG" "Server was successfully joined." "MCC join success" 90 || fail "MCC failed to join"
wait_for_server_log_pattern "$TEST_USERNAME joined the game" "server join entry" 30 || fail "Server never logged the join"

run_server_command "op $TEST_USERNAME"
run_server_command "gamerule sendCommandFeedback true"
run_server_command "gamerule logAdminCommands true"
run_server_command "time set day"
run_server_command "weather clear"
sleep 2

# ── Phase 1: Basic status and info commands ──
run_mcc_command "health"
run_mcc_command "list"
run_mcc_command "inventory player list"
run_mcc_command "/gamemode creative"
run_mcc_command "inventory creativegive 36 Diamond 16"
run_mcc_command "inventory player list"
run_mcc_command "entity"
run_mcc_command "/time query daytime"
run_mcc_command "smoke_test_from_mcc_full_spectrum"

# ── Phase 2: Movement and look commands ──
run_mcc_command "/tp $TEST_USERNAME 0 -60 0"
sleep 3
run_mcc_command "look up"
sleep 1
run_mcc_command "look down"
sleep 1
run_mcc_command "look east"
sleep 1

# ── Phase 3: Advanced inventory operations ──
run_mcc_command "inventory creativegive 37 IronSword 1"
run_mcc_command "inventory creativegive 38 GoldenApple 8"
run_mcc_command "inventory player list"
run_mcc_command "inventory creativeclear 38"
run_mcc_command "inventory player list"

# ── Phase 4: Block placement and interaction ──
run_server_command "execute as $TEST_USERNAME at @s run fill ~1 ~ ~1 ~3 ~2 ~3 minecraft:stone"
sleep 2
run_server_command "execute as $TEST_USERNAME at @s run setblock ~5 ~ ~5 minecraft:chest"
sleep 1
run_server_command "execute as $TEST_USERNAME at @s run setblock ~5 ~1 ~5 minecraft:furnace"
sleep 1
run_server_command "execute as $TEST_USERNAME at @s run setblock ~6 ~ ~5 minecraft:crafting_table"
sleep 1

# ── Phase 5: Entity spawning (expanded coverage) ──
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:cow ~2 ~ ~"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:zombie ~4 ~ ~"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:creeper ~6 ~ ~"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:skeleton ~8 ~ ~"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:villager ~-2 ~ ~"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:allay ~-4 ~ ~"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:armor_stand ~ ~ ~2"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:item_display ~-6 ~ ~ {item:{id:\"minecraft:diamond\",count:1}}"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:spider ~10 ~ ~"
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:pig ~-8 ~ ~"

sleep 2
run_mcc_command "entity"

# ── Phase 6: Effects and environment ──
run_server_command "effect give $TEST_USERNAME minecraft:speed 30 1"
sleep 2
run_mcc_command "health"
run_server_command "effect give $TEST_USERNAME minecraft:regeneration 10 1"
sleep 2
run_mcc_command "health"

# ── Phase 7: Gamemode cycling ──
run_mcc_command "/gamemode survival"
sleep 2
run_mcc_command "health"
run_mcc_command "/gamemode creative"
sleep 2

# ── Phase 8: Dimension change (nether) ──
run_server_command "execute in minecraft:the_nether run tp $TEST_USERNAME 0 64 0"
sleep 4
run_mcc_command "health"
run_server_command "execute in minecraft:overworld run tp $TEST_USERNAME 0 -60 0"
sleep 4

# ── Phase 9: Server chat and whisper ──
run_server_command "say Hello from the server console"
sleep 2
run_server_command "msg $TEST_USERNAME This is a private whisper"
sleep 2
run_mcc_command "integration_test_chat_response"

# ── Phase 10: Particles, sounds, and explosions ──
run_server_command "execute as $TEST_USERNAME at @s run particle minecraft:happy_villager ~ ~1 ~ 0.5 0.5 0.5 0 12 force"
run_server_command "execute as $TEST_USERNAME at @s run particle minecraft:end_rod ~ ~1 ~ 0.5 0.5 0.5 0.01 20 force"
run_server_command "execute as $TEST_USERNAME at @s run particle minecraft:explosion ~ ~1 ~ 0 0 0 0 1 force"
run_server_command "execute as $TEST_USERNAME at @s run particle minecraft:totem_of_undying ~ ~1 ~ 0.5 0.5 0.5 0.1 20 force"
run_server_command "execute as $TEST_USERNAME at @s run particle minecraft:flame ~ ~1 ~ 0.2 0.2 0.2 0.02 30 force"
run_server_command "execute as $TEST_USERNAME at @s run particle minecraft:heart ~ ~2 ~ 0.3 0.3 0.3 0 5 force"

run_server_command "execute as $TEST_USERNAME at @s run playsound minecraft:entity.lightning_bolt.thunder master $TEST_USERNAME ~ ~ ~ 1 1 0"
run_server_command "execute as $TEST_USERNAME at @s run playsound minecraft:block.note_block.bell master $TEST_USERNAME ~ ~ ~ 1 1 0"
run_server_command "execute as $TEST_USERNAME at @s run playsound minecraft:entity.experience_orb.pickup master $TEST_USERNAME ~ ~ ~ 1 1 0"

run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:tnt ~3 ~ ~"
sleep 2
run_server_command "execute as $TEST_USERNAME at @s run summon minecraft:tnt ~6 ~ ~"

# ── Phase 11: Kill and respawn cycle ──
run_mcc_command "/gamemode survival"
sleep 2
run_server_command "kill $TEST_USERNAME"
sleep 4
run_mcc_command "respawn"
sleep 4
run_mcc_command "health"
run_mcc_command "/gamemode creative"
sleep 2

sleep 6
capture_server_logs

# ── Assertions: MCC log ──
assert_contains "$MCC_LOG" "Server was successfully joined." "MCC never joined the server"
assert_contains "$MCC_LOG" "[FileInput] > inventory player list" "Inventory command was not executed"
assert_contains "$MCC_LOG" "[FileInput] > entity" "Entity command was not executed"
assert_contains "$MCC_LOG" "[FileInput] > /gamemode creative" "Creative mode command was not executed from MCC"
assert_contains "$MCC_LOG" "Requested Diamond x16 in slot #36" "Creative inventory give did not succeed"
assert_contains "$MCC_LOG" "smoke_test_from_mcc_full_spectrum" "Client-originated chat was not observed"
assert_contains "$MCC_LOG" "[FileInput] > look up" "Look command was not executed"
assert_contains "$MCC_LOG" "[FileInput] > /gamemode survival" "Survival mode switch was not executed"
assert_contains "$MCC_LOG" "[FileInput] > respawn" "Respawn command was not executed"
assert_contains "$MCC_LOG" "[FileInput] > health" "Health command was not executed"
assert_contains "$MCC_LOG" "integration_test_chat_response" "Chat response test message was not observed"
assert_not_contains "$MCC_LOG" "Please enable InventoryHandling" "Inventory handling is still disabled"
assert_not_contains "$MCC_LOG" "Please enable EntityHandling" "Entity handling is still disabled"
assert_not_contains "$MCC_LOG" "You must be in Creative gamemode" "Creative mode was not active when creativegive ran"
assert_not_contains "$MCC_LOG" "Failed to load settings" "MCC failed to reload its config"
assert_not_contains "$MCC_LOG" "NullReferenceException" "A NullReferenceException occurred during the test"

# ── Assertions: Server log ──
assert_contains "$SERVER_FILE_LOG" "$TEST_USERNAME joined the game" "Server never saw $TEST_USERNAME join"
assert_contains "$SERVER_FILE_LOG" "smoke_test_from_mcc_full_spectrum" "Server never received the client chat message"
assert_contains "$SERVER_FILE_LOG" "Displaying particle minecraft:happy_villager" "Particle events were not recorded on the server"
assert_contains "$SERVER_FILE_LOG" "Played sound minecraft:block.note_block.bell to $TEST_USERNAME" "Sound events were not recorded on the server"
assert_contains "$SERVER_FILE_LOG" "Summoned new Primed TNT" "TNT summon did not occur on the server"
assert_contains "$SERVER_FILE_LOG" "integration_test_chat_response" "Server never received the chat response test message"
assert_contains "$SERVER_FILE_LOG" "Hello from the server console" "Server say command was not logged"
assert_contains "$SERVER_FILE_LOG" "Killed $TEST_USERNAME" "Server kill command did not execute"
assert_not_contains "$SERVER_FILE_LOG" "Sending unknown packet 'clientbound/minecraft:disconnect'" "Server hit the disconnect packet regression during the test"

cat <<EOF
PASS
Run directory: $RUN_DIR
MCC log: $MCC_LOG
Server log: $SERVER_FILE_LOG
Build log: $BUILD_LOG
EOF
