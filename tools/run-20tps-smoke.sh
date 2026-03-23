#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

usage() {
    cat <<'EOF'
Usage: tools/run-20tps-smoke.sh <server-dir> <mc-version> [modern|legacy]

Examples:
  MCC_SERVERS=/home/anon/Minecraft/Servers tools/run-20tps-smoke.sh 1.20.6-Vanilla 1.20.6 modern
  tools/run-20tps-smoke.sh 1.8.9 1.8.9 legacy
EOF
}

SERVER_DIR="${1:-}"
MC_VERSION="${2:-}"
PROFILE="${3:-modern}"

if [[ -z "$SERVER_DIR" || -z "$MC_VERSION" ]]; then
    usage >&2
    exit 1
fi

if [[ "$PROFILE" != "modern" && "$PROFILE" != "legacy" ]]; then
    echo "Unsupported profile: $PROFILE" >&2
    exit 1
fi

SESSION_NAME="mc-${SERVER_DIR//./_}"
TEST_ROOT="${TMPDIR:-/tmp}/mcc-20tps-tests/${SERVER_DIR//\//_}"
CFG="$TEST_ROOT/MinecraftClient.$MC_VERSION.ini"
MCC_LOG="$TEST_ROOT/mcc.log"
BUILD_LOG="$TEST_ROOT/build.log"
PLAYER_LOG="$TEST_ROOT/playerlog.txt"
INPUT_FILE="$REPO_ROOT/mcc_input.txt"
SERVER_LOG_FILE="$MCC_SERVERS/$SERVER_DIR/logs/latest.log"
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
    local timeout="${1:-90}"
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

    if [[ "$PROFILE" == "modern" ]]; then
        sed -i '/^\[ChatBot.AntiAFK\]/,/^\[/ { s/^Enabled = false/Enabled = true/; s/^Delay = .*/Delay = { min = 8.0, max = 8.0 }/; s#^Command = .*#Command = "\/help"#; }' "$CFG"
        sed -i '/^\[ChatBot.AutoAttack\]/,/^\[/ { s/^Enabled = false/Enabled = true/; }' "$CFG"
        sed -i '/^\[ChatBot.AutoDig\]/,/^\[/ { s/^Enabled = false/Enabled = true/; }' "$CFG"
        sed -i '/^\[ChatBot.PlayerListLogger\]/,/^\[/ { s/^Enabled = false/Enabled = true/; s#^File = .*#File = "'"$PLAYER_LOG"'"#; s/^Delay = .*/Delay = 2.0/; }' "$CFG"
        sed -i '/^\[ChatBot.ReplayCapture\]/,/^\[/ { s/^Enabled = false/Enabled = true/; s/^Backup_Interval = .*/Backup_Interval = 2.0/; }' "$CFG"
    fi
}

send_mcc_command() {
    local command="$1"
    local delay="${2:-2}"
    echo "$command" >> "$INPUT_FILE"
    sleep "$delay"
}

parse_tick_summary() {
    local summary="$1"
    local updates=0
    local tps=""

    if [[ "$summary" =~ updates=([0-9]+),[[:space:]]seconds=([0-9]+),[[:space:]]tps=([0-9]+\.[0-9]+) ]]; then
        updates="${BASH_REMATCH[1]}"
        tps="${BASH_REMATCH[3]}"
    else
        echo "Unable to parse tick summary: $summary" >&2
        return 1
    fi

    if (( updates < 90 || updates > 110 )); then
        echo "Unexpected tick count: $summary" >&2
        return 1
    fi

    printf 'TICK_SUMMARY=%s\n' "$summary"
}

parse_packet_summary() {
    local summary="$1"
    local total=0
    local movement=0
    local position=0
    local posrot=0
    local rotation=0

    if [[ "$summary" =~ total=([0-9]+),[[:space:]]movement=([0-9]+),[[:space:]]position=([0-9]+),[[:space:]]posrot=([0-9]+),[[:space:]]rotation=([0-9]+) ]]; then
        total="${BASH_REMATCH[1]}"
        movement="${BASH_REMATCH[2]}"
        position="${BASH_REMATCH[3]}"
        posrot="${BASH_REMATCH[4]}"
        rotation="${BASH_REMATCH[5]}"
    else
        echo "Unable to parse packet summary: $summary" >&2
        return 1
    fi

    if [[ "$PROFILE" == "legacy" ]]; then
        if (( total < 90 || total > 110 || position < 4 || position > 6 || movement < 85 )); then
            echo "Unexpected legacy packet cadence: $summary" >&2
            return 1
        fi
    else
        if (( total < 4 || total > 7 || position < 4 || position > 7 || movement > 1 || posrot > 1 || rotation > 1 )); then
            echo "Unexpected modern packet cadence: $summary" >&2
            return 1
        fi
    fi

    printf 'PACKET_SUMMARY=%s\n' "$summary"
}

run_modern_extras() {
    local dig_result=""
    local zombie_result=""
    local anti_afk_hits=0
    local replay_file=""
    local playerlog_lines=0

    bash "$REPO_ROOT/tools/mc-rcon.sh" "tp CursorBot 0 -60 0" >/dev/null
    sleep 2

    bash "$REPO_ROOT/tools/mc-rcon.sh" "setblock 2 -60 0 minecraft:stone" >/dev/null
    sleep 1
    send_mcc_command "look 2 -60 0" 2
    send_mcc_command "autodig start" 6
    dig_result="$(bash "$REPO_ROOT/tools/mc-rcon.sh" "execute if block 2 -60 0 minecraft:air run say DIG_OK" || true)"

    bash "$REPO_ROOT/tools/mc-rcon.sh" "summon minecraft:zombie 2 -60 2" >/dev/null
    sleep 5
    zombie_result="$(bash "$REPO_ROOT/tools/mc-rcon.sh" "data get entity @e[type=minecraft:zombie,limit=1,sort=nearest] Health" || true)"

    sleep 5

    anti_afk_hits="$(grep -F "Sending '/help'" "$MCC_LOG" | wc -l | tr -d ' ')"
    replay_file="$(find "$REPO_ROOT" -maxdepth 2 -type f -name '*.mcpr' | head -n 1 || true)"
    playerlog_lines="$(wc -l < "$PLAYER_LOG" 2>/dev/null || echo 0)"

    if (( anti_afk_hits < 1 )); then
        echo "AntiAFK never fired during modern smoke test" >&2
        return 1
    fi

    if [[ -z "$replay_file" ]]; then
        echo "ReplayCapture did not create a replay file" >&2
        return 1
    fi

    if (( playerlog_lines < 1 )); then
        echo "PlayerListLogger did not write any output" >&2
        return 1
    fi

    printf 'DIG_RESULT=%s\n' "$dig_result"
    printf 'ZOMBIE_RESULT=%s\n' "$zombie_result"
    printf 'ANTI_AFK_HIT=%s\n' "$anti_afk_hits"
    printf 'REPLAY_FILE=%s\n' "$replay_file"
    printf 'PLAYERLOG_LINES=%s\n' "$playerlog_lines"
}

prepare_config
kill_other_servers
rm -f "$MCC_LOG" "$BUILD_LOG" "$PLAYER_LOG" "$INPUT_FILE"
rm -rf "$REPO_ROOT/replay_recordings" "$REPO_ROOT/recording_cache"
find "$REPO_ROOT" -maxdepth 1 -type f \( -name 'replay_recordings\\*' -o -name 'recording_cache\\*' \) -delete

if [[ "${MCC_SKIP_BUILD:-0}" != "1" ]]; then
    mcc-build > "$BUILD_LOG" 2>&1
fi

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

wait_for_file_pattern "$MCC_LOG" "Server was successfully joined." "MCC join success" 90
wait_for_file_pattern "$SERVER_LOG_FILE" "CursorBot joined the game" "server join entry" 30

bash "$REPO_ROOT/tools/mc-rcon.sh" "op CursorBot" >/dev/null
sleep 2

if [[ "$PROFILE" == "modern" ]]; then
    send_mcc_command "script MinecraftClient/config/sample-script-tick-counter.cs" 1
    wait_for_file_pattern "$MCC_LOG" "Tick counter summary:" "tick summary" 20
    tick_summary="$(grep -F "Tick counter summary:" "$MCC_LOG" | tail -n 1)"
    parse_tick_summary "$tick_summary"
fi

send_mcc_command "script MinecraftClient/config/sample-script-packet-capture.cs" 1
wait_for_file_pattern "$MCC_LOG" "Packet cadence summary" "packet summary" 20
packet_summary="$(grep -F "Packet cadence summary" "$MCC_LOG" | tail -n 1)"
parse_packet_summary "$packet_summary"

send_mcc_command "health" 2
wait_for_file_pattern "$MCC_LOG" "[FileInput] > health" "health command" 10

if [[ "$PROFILE" == "modern" ]]; then
    send_mcc_command "inventory player list" 2
    send_mcc_command "entity" 2
else
    send_mcc_command "look east" 2
    wait_for_file_pattern "$MCC_LOG" "[FileInput] > look east" "look command" 10
fi

fileinput_count="$(grep -F "[FileInput] >" "$MCC_LOG" | wc -l | tr -d ' ')"
health_hit="$(grep -F "[FileInput] > health" "$MCC_LOG" | wc -l | tr -d ' ')"

if [[ "$PROFILE" == "modern" ]]; then
    entity_hit="$(grep -F "[FileInput] > entity" "$MCC_LOG" | wc -l | tr -d ' ')"
    inventory_hit="$(grep -F "[FileInput] > inventory player list" "$MCC_LOG" | wc -l | tr -d ' ')"

    if (( fileinput_count < 3 || health_hit < 1 )); then
        echo "Basic modern FileInput commands did not complete as expected" >&2
        exit 1
    fi

    printf 'FILEINPUT_COUNT=%s\n' "$fileinput_count"
    printf 'HEALTH_HIT=%s\n' "$health_hit"
    printf 'ENTITY_HIT=%s\n' "$entity_hit"
    printf 'INVENTORY_HIT=%s\n' "$inventory_hit"
else
    look_hit="$(grep -F "[FileInput] > look east" "$MCC_LOG" | wc -l | tr -d ' ')"

    if (( fileinput_count < 3 || health_hit < 1 || look_hit < 1 )); then
        echo "Basic legacy FileInput commands did not complete as expected" >&2
        exit 1
    fi

    printf 'FILEINPUT_COUNT=%s\n' "$fileinput_count"
    printf 'HEALTH_HIT=%s\n' "$health_hit"
    printf 'LOOK_HIT=%s\n' "$look_hit"
fi

if [[ "$PROFILE" == "modern" ]]; then
    run_modern_extras
fi

printf 'LOG_DIR=%s\n' "$TEST_ROOT"
