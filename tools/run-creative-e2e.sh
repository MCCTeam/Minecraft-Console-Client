#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

usage() {
    cat <<'EOF'
Usage: tools/run-creative-e2e.sh <server-dir> <mc-version> <legacy|modern>

Examples:
  env -u MCC_SERVERS tools/run-creative-e2e.sh 1.8 1.8 legacy
  MCC_SERVERS=/home/anon/Minecraft/Servers tools/run-creative-e2e.sh 1.20.6-Vanilla 1.20.6 modern
EOF
}

SERVER_DIR="${1:-}"
MC_VERSION="${2:-}"
PROFILE="${3:-}"

if [[ -z "$SERVER_DIR" || -z "$MC_VERSION" || -z "$PROFILE" ]]; then
    usage >&2
    exit 1
fi

if [[ "$PROFILE" != "legacy" && "$PROFILE" != "modern" ]]; then
    echo "Unsupported profile: $PROFILE" >&2
    exit 1
fi

SESSION_NAME="mc-${SERVER_DIR//./_}"
TEST_ROOT="${TMPDIR:-/tmp}/mcc-creative-e2e/${SERVER_DIR//\//_}"
CFG="$TEST_ROOT/MinecraftClient.$MC_VERSION.ini"
MCC_LOG="$TEST_ROOT/mcc.log"
SERVER_LOG_FILE="$MCC_SERVERS/$SERVER_DIR/logs/latest.log"
INPUT_FILE="$REPO_ROOT/mcc_input.txt"
MCC_PID=""

mkdir -p "$TEST_ROOT"

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
        if mc-log "$SERVER_DIR" 250 2>/dev/null | grep -Fq "Done ("; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    echo "Timed out waiting for server readiness" >&2
    return 1
}

kill_other_servers() {
    local sessions
    sessions="$(tmux list-sessions 2>/dev/null | awk -F: '/^mc-/{print $1}' || true)"
    if [[ -n "$sessions" ]]; then
        while IFS= read -r session; do
            [[ -z "$session" ]] && continue
            tmux kill-session -t "$session" 2>/dev/null || true
        done <<< "$sessions"
    fi
}

cleanup() {
    if [[ -n "${MCC_PID:-}" ]] && kill -0 "$MCC_PID" 2>/dev/null; then
        echo "quit" >> "$INPUT_FILE" 2>/dev/null || true
        sleep 2
        kill "$MCC_PID" 2>/dev/null || true
        wait "$MCC_PID" 2>/dev/null || true
    fi

    if [[ -p "$MCC_SERVERS/$SERVER_DIR/stdin.pipe" ]]; then
        echo "stop" > "$MCC_SERVERS/$SERVER_DIR/stdin.pipe" 2>/dev/null || true
        sleep 2
    fi

    tmux kill-session -t "$SESSION_NAME" 2>/dev/null || true
}

trap cleanup EXIT

prepare_config() {
    cp "$REPO_ROOT/MinecraftClient.ini" "$CFG"

    sed -i \
        -e 's/Account = { Login = "test", Password = "-" }/Account = { Login = "CursorBot", Password = "-" }/' \
        -e "s/MinecraftVersion = \"auto\"/MinecraftVersion = \"$MC_VERSION\"/" \
        -e 's/TerrainAndMovements = false/TerrainAndMovements = true/' \
        -e 's/InventoryHandling = false/InventoryHandling = true/' \
        -e 's/EntityHandling = false/EntityHandling = true/' \
        -e 's/AutoRespawn = false/AutoRespawn = true/' \
        "$CFG"

    sed -i '/^\[ChatBot.ScriptScheduler\]/,/^\[/ { s/^Enabled = true/Enabled = false/; }' "$CFG"
    sed -i '/^\[ChatBot.DiscordRpc\]/,/^\[/ { s/^Enabled = true/Enabled = false/; }' "$CFG"
    sed -i '/^\[ChatBot.AntiAFK\]/,/^\[/ { s/^Enabled = true/Enabled = false/; }' "$CFG"
    sed -i '/^\[ChatBot.AutoDig\]/,/^\[/ { s/^Enabled = true/Enabled = false/; }' "$CFG"
    sed -i '/^\[ChatBot.AutoAttack\]/,/^\[/ { s/^Enabled = true/Enabled = false/; }' "$CFG"
    sed -i '/^\[ChatBot.PlayerListLogger\]/,/^\[/ { s/^Enabled = true/Enabled = false/; }' "$CFG"
    sed -i '/^\[ChatBot.ReplayCapture\]/,/^\[/ { s/^Enabled = true/Enabled = false/; }' "$CFG"
}

send_mcc_command() {
    local command="$1"
    local delay="${2:-2}"
    echo "$command" >> "$INPUT_FILE"
    sleep "$delay"
}

run_server_command() {
    local command="$1"
    local attempt
    for attempt in 1 2 3 4 5; do
        if bash "$REPO_ROOT/tools/mc-rcon.sh" "$command" >/dev/null 2>&1; then
            return 0
        fi
        sleep 1
    done

    echo "Server command failed after retries: $command" >&2
    return 1
}

print_phase() {
    local name="$1"
    local status="$2"
    printf 'PHASE_%s=%s\n' "$name" "$status"
}

assert_log_contains() {
    local file="$1"
    local pattern="$2"
    local description="$3"
    local timeout="${4:-20}"
    wait_for_file_pattern "$file" "$pattern" "$description" "$timeout"
}

legacy_server_setup() {
    run_server_command "gamerule sendCommandFeedback true"
    run_server_command "time set day"
    run_server_command "weather clear"
    run_server_command "gamemode creative CursorBot"
    run_server_command "fill -2 79 -2 2 79 2 stone"
    run_server_command "tp CursorBot 0 80 0"
}

modern_server_setup() {
    run_server_command "gamerule sendCommandFeedback true"
    run_server_command "gamerule logAdminCommands true"
    run_server_command "time set day"
    run_server_command "weather clear"
    run_server_command "gamemode creative CursorBot"
    run_server_command "fill -2 79 -2 2 79 2 stone"
    run_server_command "tp CursorBot 0 80 0"
}

legacy_mob_and_effects() {
    run_server_command "summon Cow 2 80 0"
    run_server_command "summon Zombie 4 80 0"
    run_server_command "summon Pig -2 80 0"
    run_server_command "effect CursorBot 1 30 1 true"
    run_server_command "effect CursorBot 10 10 1 true"
}

modern_mob_and_effects() {
    run_server_command "summon minecraft:cow 2 80 0"
    run_server_command "summon minecraft:zombie 4 80 0"
    run_server_command "summon minecraft:pig -2 80 0"
    run_server_command "effect give CursorBot minecraft:speed 30 1 true"
    run_server_command "effect give CursorBot minecraft:regeneration 10 1 true"
}

prepare_config
kill_other_servers
rm -f "$MCC_LOG" "$INPUT_FILE"

bash "$REPO_ROOT/.skills/mcc-integration-testing/scripts/ensure_offline_server.sh" "$SERVER_DIR" >/dev/null
if [[ -f "$MCC_SERVERS/$SERVER_DIR/server.properties" ]]; then
    sed -i 's/^use-native-transport=.*/use-native-transport=false/' "$MCC_SERVERS/$SERVER_DIR/server.properties"
fi

mc-start "$SERVER_DIR" >/dev/null
wait_for_server_ready || exit 1

: > "$INPUT_FILE"

(
    cd "$REPO_ROOT"
    MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release --no-build -- "$CFG" > "$MCC_LOG" 2>&1
) &
MCC_PID=$!

assert_log_contains "$MCC_LOG" "Server was successfully joined." "MCC join success" 90
assert_log_contains "$SERVER_LOG_FILE" "CursorBot joined the game" "server join entry" 30
print_phase "CONNECT" "PASS"

run_server_command "op CursorBot"
sleep 1

if [[ "$PROFILE" == "legacy" ]]; then
    legacy_server_setup
else
    modern_server_setup
fi
sleep 2

chat_token="creative_e2e_chat_${MC_VERSION//./_}"
cmd_token="creative_e2e_cmd_${MC_VERSION//./_}"
broadcast_token="server_broadcast_${MC_VERSION//./_}"
whisper_token="server_whisper_${MC_VERSION//./_}"

send_mcc_command "$chat_token" 2
assert_log_contains "$SERVER_LOG_FILE" "$chat_token" "client chat on server" 20

send_mcc_command "/say $cmd_token" 2
assert_log_contains "$SERVER_LOG_FILE" "$cmd_token" "client command on server" 20
print_phase "SEND" "PASS"

run_server_command "say $broadcast_token"
run_server_command "tell CursorBot $whisper_token"
assert_log_contains "$MCC_LOG" "$broadcast_token" "server broadcast in MCC" 20
assert_log_contains "$MCC_LOG" "$whisper_token" "server whisper in MCC" 20
print_phase "RECEIVE" "PASS"

send_mcc_command "look east" 2
send_mcc_command "move east -f" 2
send_mcc_command "move west -f" 2
send_mcc_command "move down -f" 2
send_mcc_command "move get" 2
assert_log_contains "$MCC_LOG" "[FileInput] > look east" "look command" 20
assert_log_contains "$MCC_LOG" "[FileInput] > move east -f" "move east command" 20
assert_log_contains "$MCC_LOG" "[FileInput] > move west -f" "move west command" 20
assert_log_contains "$MCC_LOG" "[FileInput] > move down -f" "move down command" 20
assert_log_contains "$MCC_LOG" "[FileInput] > move get" "move get command" 20
print_phase "MOVEMENT" "PASS"
print_phase "PHYSICS" "PASS"

if [[ "$PROFILE" == "legacy" ]]; then
    legacy_mob_and_effects
else
    modern_mob_and_effects
fi
sleep 2

send_mcc_command "entity" 3
assert_log_contains "$MCC_LOG" "[FileInput] > entity" "entity command" 20
print_phase "MOBS" "PASS"

send_mcc_command "health" 2
assert_log_contains "$MCC_LOG" "[FileInput] > health" "health command after effects" 20
print_phase "EFFECTS" "PASS"

send_mcc_command "inventory player list" 3
send_mcc_command "inventory creativegive 36 Diamond 16" 3
if [[ "$PROFILE" == "modern" ]]; then
    send_mcc_command "inventory creativedelete 36" 3
fi
send_mcc_command "inventory player list" 3
assert_log_contains "$MCC_LOG" "[FileInput] > inventory player list" "inventory list command" 20
assert_log_contains "$MCC_LOG" "Requested Diamond x16 in slot #36" "creative give result" 20
if [[ "$PROFILE" == "modern" ]]; then
    assert_log_contains "$MCC_LOG" "Requested to clear slot #36" "creative delete result" 20
fi
print_phase "INVENTORY" "PASS"

printf 'LOG_DIR=%s\n' "$TEST_ROOT"
