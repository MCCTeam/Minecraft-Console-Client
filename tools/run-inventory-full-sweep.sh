#!/usr/bin/env bash
set -u -o pipefail

SCRIPT_SELF="${BASH_SOURCE[0]}"
while [[ -L "$SCRIPT_SELF" ]]; do
    SCRIPT_DIRNAME="$(cd -P "$(dirname "$SCRIPT_SELF")" >/dev/null 2>&1 && pwd)"
    SCRIPT_SELF="$(readlink "$SCRIPT_SELF")"
    [[ "$SCRIPT_SELF" != /* ]] && SCRIPT_SELF="$SCRIPT_DIRNAME/$SCRIPT_SELF"
done
REPO_ROOT="$(cd -P "$(dirname "$SCRIPT_SELF")/.." >/dev/null 2>&1 && pwd)"
SCRIPT_DIR="$REPO_ROOT/.skills/mcc-integration-testing/scripts"
RUN_ROOT="${RUN_ROOT:-/tmp/mcc-inventory-full-sweep/$(date +%Y%m%d-%H%M%S)}"
VERSIONS="${VERSIONS_OVERRIDE:-1.8 1.9 1.10 1.11 1.12 1.13 1.14 1.15 1.16 1.17 1.18 1.19 1.20 1.21 26.1}"
STOP_ON_FAIL="${STOP_ON_FAIL:-1}"

usage() {
    cat <<'USAGE'
Usage: tools/run-inventory-full-sweep.sh [options]

Runs MCC inventory command/API coverage against real local Minecraft servers.
The matrix is sequential because mc-* tmux sessions are shared state.

Options:
  --versions "1.20.4 1.21.11"  Space-separated versions to test.
  --keep-going                 Continue after failures.
  --stop-on-fail               Stop on first failure. Default.
  -h, --help                   Show this help.

Environment overrides:
  VERSIONS_OVERRIDE, RUN_ROOT, STOP_ON_FAIL, MCC_SERVERS.

Examples:
  tools/run-inventory-full-sweep.sh --versions "1.21.10 1.21.11"
USAGE
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --versions)
            VERSIONS="$2"
            shift 2
            ;;
        --keep-going)
            STOP_ON_FAIL=0
            shift
            ;;
        --stop-on-fail)
            STOP_ON_FAIL=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

source "$REPO_ROOT/tools/mcc-env.sh"
source "$SCRIPT_DIR/common.sh"

mkdir -p "$RUN_ROOT"
SUMMARY="$RUN_ROOT/summary.tsv"
printf 'target\tstatus\tdetail\tlog\n' > "$SUMMARY"

wait_for_file_pattern_local() {
    local file="$1"
    local pattern="$2"
    local timeout="${3:-10}"
    local end=$((SECONDS + timeout))
    while (( SECONDS < end )); do
        if [[ -f "$file" ]] && grep -Eq "$pattern" "$file"; then
            return 0
        fi
        sleep 0.2
    done
    return 1
}

sanitize_version() {
    printf '%s' "$1" | tr '.-' '__'
}

server_target_for() {
    local version="$1"
    local dir
    if [[ -d "${MCC_SERVERS:-}/$version-Vanilla" ]]; then
        printf '%s-Vanilla' "$version"
    elif [[ -d "$REPO_ROOT/MinecraftOfficial/downloads/$version" ]]; then
        printf '%s' "$version"
    else
        printf '%s-Vanilla' "$version"
    fi
}

server_dir_for() {
    local target="$1"
    local root="${MCC_SERVERS:-$REPO_ROOT/MinecraftOfficial/downloads}"
    printf '%s/%s\n' "$root" "$target"
}

rcon_port_for() {
    local target="$1"
    local props
    props="$(server_dir_for "$target")/server.properties"
    if [[ -f "$props" ]]; then
        local port_line
        port_line="$(grep -E '^rcon\.port=' "$props" | tail -n 1 || true)"
        if [[ -n "$port_line" ]]; then
            printf '%s\n' "${port_line#rcon.port=}"
            return 0
        fi
    fi
    printf '25575\n'
}

run_rcon() {
    local port="$1"
    local command="$2"
    local attempt
    for attempt in 1 2 3 4 5; do
        if mc-rcon "$command" "$port" >/dev/null 2>&1; then
            return 0
        fi
        sleep 1
    done
    return 1
}

run_rcon_or_detail() {
    local port="$1"
    local cmd="$2"
    if run_rcon "$port" "$cmd"; then
        return 0
    fi
    FAIL_DETAIL="rcon failed: $cmd"
    return 1
}

run_rcon_any_or_detail() {
    local port="$1"
    local detail="$2"
    shift 2
    local cmd
    for cmd in "$@"; do
        if run_rcon "$port" "$cmd"; then
            return 0
        fi
    done
    FAIL_DETAIL="$detail"
    return 1
}

run_rcon_any() {
    local port="$1"
    shift
    local cmd
    for cmd in "$@"; do
        if run_rcon "$port" "$cmd"; then
            return 0
        fi
    done
    return 1
}

run_rcon_each() {
    local port="$1"
    shift
    local cmd
    for cmd in "$@"; do
        run_rcon "$port" "$cmd" || true
    done
}

send_mcc_command() {
    local session="$1"
    local log_file="$2"
    local command="$3"
    local delay="${4:-1}"
    local block_file="$5"
    local mark
    mark="$(wc -c < "$log_file" 2>/dev/null || printf '0')"
    mcc-cmd --session "$session" "$command" >/dev/null
    sleep "$delay"
    LAST_BLOCK="$(tail -c "+$((mark + 1))" "$log_file" 2>/dev/null || true)"
    {
        printf '\n>>> %s\n' "$command"
        printf '%s\n' "$LAST_BLOCK"
    } >> "$block_file"
}

assert_contains() {
    grep -Eq "$2" <<<"$1" || { FAIL_DETAIL="$3"; return 1; }
}

assert_not_contains() {
    if grep -Eq "$2" <<<"$1"; then
        FAIL_DETAIL="$3"
        return 1
    fi
}

assert_no_runtime_crash() {
    local log_file="$1"
    if grep -Eq 'Queue empty|Unhandled exception|Object reference not set|Failed to parse packet|Failed to process incoming packet|Connection has been lost' "$log_file"; then
        FAIL_DETAIL="runtime log contains crash/disconnect marker"
        return 1
    fi
}

clear_dropped_items() {
    local port="$1"
    run_rcon_any "$port" "kill @e[type=item]" "kill @e[type=Item]" >/dev/null 2>&1 || true
}

open_chest() {
    local session="$1"
    local log_file="$2"
    local block_file="$3"
    send_mcc_command "$session" "$log_file" "useblock 1 80 0" 2 "$block_file"
    if wait_for_file_pattern_local "$log_file" "Inventory # 1 opened: Chest" 4; then
        return 0
    fi
    send_mcc_command "$session" "$log_file" "useblock 1 80 0" 2 "$block_file"
    wait_for_file_pattern_local "$log_file" "Inventory # 1 opened: Chest" 12
}

setup_world() {
    local port="$1"
    run_rcon_or_detail "$port" "gamerule sendCommandFeedback true" || return 1
    run_rcon "$port" "gamerule keepInventory true" || true
    run_rcon "$port" "time set day" || true
    run_rcon "$port" "weather clear" || true
    run_rcon "$port" "difficulty peaceful" || true
}

setup_area() {
    local port="$1"
    run_rcon_each "$port" "fill -2 78 -3 3 82 3 air 0 replace" "fill -2 78 -3 3 82 3 air" "fill -2 78 -3 3 82 3 minecraft:air"
    run_rcon_each "$port" "fill -2 79 -3 3 79 3 stone 0 replace" "fill -2 79 -3 3 79 3 stone" "fill -2 79 -3 3 79 3 minecraft:stone"
    run_rcon_each "$port" "setblock 1 80 0 air 0 replace" "setblock 1 80 0 air" "setblock 1 80 0 minecraft:air"
    run_rcon_each "$port" "setblock 1 80 0 chest 0 replace" "setblock 1 80 0 chest" "setblock 1 80 0 minecraft:chest"
    run_rcon_each "$port" "blockdata 1 80 0 {Items:[]}" "data merge block 1 80 0 {Items:[]}"
}

setup_player() {
    local port="$1"
    local username="$2"
    run_rcon "$port" "op $username" || true
    run_rcon "$port" "gamemode creative $username" || return 1
    run_rcon_any "$port" "tp $username 1.5 80 2.5" "tp $username 1 80 2" || true
}

run_inventory_sequence() {
    local version="$1"
    local rcon_port="$2"
    local session="$3"
    local username="$4"
    local log_file="$5"
    local block_file="$6"

    send_mcc_command "$session" "$log_file" "inventory player drop -1 all" 1 "$block_file" || true
    for slot in 36 37 38 39 40 41 42 43 44; do
        send_mcc_command "$session" "$log_file" "inventory creativedelete $slot" 0.2 "$block_file" || true
    done

    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" 'Inventory #0 - Player Inventory' "player inventory did not list" || return 1
    send_mcc_command "$session" "$log_file" "inventory inventories" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#0[[:space:]]+- Player Inventory' "inventory discovery did not list player inventory" || return 1

    send_mcc_command "$session" "$log_file" "inventory creativegive 36 Diamond 16" 1 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#36[[:space:]]*: x16[[:space:]]+Diamond' "creativegive did not populate player slot 36" || return 1
    send_mcc_command "$session" "$log_file" "inventory search Diamond 16" 1 "$block_file"
    assert_contains "$LAST_BLOCK" 'Diamond' "inventory search did not find Diamond" || return 1
    send_mcc_command "$session" "$log_file" "inventory creativedelete 36" 1 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_not_contains "$LAST_BLOCK" '#36[[:space:]]*: x16[[:space:]]+Diamond' "creativedelete left Diamond in player slot 36" || return 1

    send_mcc_command "$session" "$log_file" "inventory creativegive 36 Dirt 3" 1 "$block_file"
    run_rcon "$rcon_port" "gamemode survival $username" || return 1
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory player click 36 right" 2 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#36[[:space:]]*: x1[[:space:]]+Dirt' "player right-click did not halve Dirt stack" || return 1
    assert_contains "$LAST_BLOCK" '#-1[[:space:]]*: x2[[:space:]]+Dirt' "player right-click did not put Dirt on cursor" || return 1
    send_mcc_command "$session" "$log_file" "inventory player click 36 left" 2 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#36[[:space:]]*: x3[[:space:]]+Dirt' "player left-click did not merge Dirt back into slot 36" || return 1
    assert_not_contains "$LAST_BLOCK" '#-1[[:space:]]*: x[0-9]+[[:space:]]+Dirt' "player left-click merge left Dirt on cursor" || return 1

    run_rcon_any "$rcon_port" "tp $username 1.5 80 2.5" "tp $username 1 80 2" || true
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory player drop 36" 0.2 "$block_file"
    clear_dropped_items "$rcon_port"
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#36[[:space:]]*: x2[[:space:]]+Dirt' "single drop did not decrement Dirt stack" || return 1

    run_rcon "$rcon_port" "gamemode creative $username" || return 1
    sleep 1
    clear_dropped_items "$rcon_port"
    send_mcc_command "$session" "$log_file" "inventory creativedelete 36" 1 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory creativegive 36 Dirt 3" 1 "$block_file"
    run_rcon "$rcon_port" "gamemode survival $username" || return 1
    run_rcon_any "$rcon_port" "tp $username 1.5 80 2.5" "tp $username 1 80 2" || true
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory player drop 36 all" 0.2 "$block_file"
    clear_dropped_items "$rcon_port"
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_not_contains "$LAST_BLOCK" '#36[[:space:]]*: x[0-9]+[[:space:]]+Dirt' "drop all left Dirt in player slot 36" || return 1

    run_rcon "$rcon_port" "gamemode creative $username" || return 1
    run_rcon_any "$rcon_port" "tp $username 1.5 80 2.5" "tp $username 1 80 2" || true
    sleep 2
    send_mcc_command "$session" "$log_file" "inventory creativegive 36 Diamond 16" 1 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory creativegive 37 GoldIngot 7" 1 "$block_file"
    send_mcc_command "$session" "$log_file" "changeslot 9" 1 "$block_file"
    run_rcon "$rcon_port" "gamemode survival $username" || return 1
    sleep 1
    open_chest "$session" "$log_file" "$block_file" || { FAIL_DETAIL="chest did not open"; return 1; }

    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#54[[:space:]]*: x16[[:space:]]+Diamond' "container list did not mirror player slot 36 as chest slot 54" || return 1
    assert_contains "$LAST_BLOCK" '#55[[:space:]]*: x7[[:space:]]+Gold[[:space:]]+Ingot' "container list did not mirror player slot 37 as chest slot 55" || return 1

    send_mcc_command "$session" "$log_file" "inventory container click 54 ShiftClick" 2 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#0[[:space:]]*: x16[[:space:]]+Diamond' "container shift-click did not move Diamond to chest slot 0" || return 1
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_not_contains "$LAST_BLOCK" '#36[[:space:]]*: x16[[:space:]]+Diamond' "mirrored player slot 36 still showed shifted Diamond" || return 1

    send_mcc_command "$session" "$log_file" "inventory container click 55 ShiftRightClick" 2 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#1[[:space:]]*: x7[[:space:]]+Gold[[:space:]]+Ingot' "container shift-right-click did not move GoldIngot to chest slot 1" || return 1
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_not_contains "$LAST_BLOCK" '#37[[:space:]]*: x7[[:space:]]+Gold[[:space:]]+Ingot' "player slot 37 still showed shifted GoldIngot" || return 1

    send_mcc_command "$session" "$log_file" "inventory search GoldIngot 7" 1 "$block_file"
    assert_contains "$LAST_BLOCK" 'Gold[[:space:]]+Ingot' "inventory search did not find GoldIngot after moving to container" || return 1

    send_mcc_command "$session" "$log_file" "inventory container click 0 right" 2 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#0[[:space:]]*: x8[[:space:]]+Diamond' "container right-click did not halve chest stack" || return 1
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#-1[[:space:]]*: x8[[:space:]]+Diamond' "container right-click did not put half stack on cursor" || return 1

    send_mcc_command "$session" "$log_file" "inventory container click 2 right" 2 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#2[[:space:]]*: x1[[:space:]]+Diamond' "container right-click did not place one item into empty slot 2" || return 1
    send_mcc_command "$session" "$log_file" "inventory container click 2 left" 2 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#2[[:space:]]*: x8[[:space:]]+Diamond' "container left-click did not merge cursor into slot 2" || return 1
    send_mcc_command "$session" "$log_file" "inventory player list" 1 "$block_file"
    assert_not_contains "$LAST_BLOCK" '#-1[[:space:]]*: x[0-9]+[[:space:]]+Diamond' "container left-click merge left Diamond on cursor" || return 1

    send_mcc_command "$session" "$log_file" "inventory container drop 2" 0.2 "$block_file"
    clear_dropped_items "$rcon_port"
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_contains "$LAST_BLOCK" '#2[[:space:]]*: x7[[:space:]]+Diamond' "container single drop did not decrement chest slot 2" || return 1
    send_mcc_command "$session" "$log_file" "inventory container drop 2 all" 0.2 "$block_file"
    clear_dropped_items "$rcon_port"
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory container list" 1 "$block_file"
    assert_not_contains "$LAST_BLOCK" '#2[[:space:]]*: x[0-9]+[[:space:]]+Diamond' "container drop all left Diamond in chest slot 2" || return 1

    send_mcc_command "$session" "$log_file" "inventory container close" 1 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory inventories" 1 "$block_file"
    assert_not_contains "$LAST_BLOCK" '#1[[:space:]]*-' "container close left inventory #1 visible" || return 1

    run_rcon "$rcon_port" "gamemode creative $username" || return 1
    sleep 1
    send_mcc_command "$session" "$log_file" "inventory creativegive 37 Emerald 1" 1 "$block_file"
    send_mcc_command "$session" "$log_file" "inventory player click 37 middle" 1 "$block_file"
    assert_contains "$LAST_BLOCK" 'middle' "middle-click command path did not execute" || return 1

    assert_no_runtime_crash "$log_file" || return 1
}

run_one_version() {
    local version="$1"
    local target
    target="$(server_target_for "$version")"
    local safe session username version_dir cfg log_file block_file mcc_root rcon_port
    safe="$(sanitize_version "$version")"
    session="inventory-full-$safe"
    username="InvF${safe//_/}"
    username="${username:0:16}"
    version_dir="$RUN_ROOT/$version"
    cfg="$version_dir/MinecraftClient.ini"
    log_file="/tmp/mcc-debug/$session/mcc-debug.log"
    block_file="$version_dir/command-blocks.log"
    mkdir -p "$version_dir" "/tmp/mcc-debug/$session"
    : > "$log_file"
    : > "$block_file"

    echo "== inventory $version =="
    bash "$SCRIPT_DIR/ensure_offline_server.sh" "$target" >/dev/null || { printf '%s\tFAIL\t%s\t%s\n' "$version" "server setup failed" "$log_file" >> "$SUMMARY"; return 1; }
    mc-start "$target" >/dev/null || { printf '%s\tFAIL\t%s\t%s\n' "$version" "server start failed" "$log_file" >> "$SUMMARY"; return 1; }
    wait_for_server_ready "$target" >/dev/null || true
    rcon_port="$(rcon_port_for "$target")"

    bash "$SCRIPT_DIR/prepare_offline_mcc_config.sh" "$cfg" "$version" "$username" >/dev/null || { printf '%s\tFAIL\t%s\t%s\n' "$version" "config setup failed" "$log_file" >> "$SUMMARY"; mc-stop "$target" --confirm >/dev/null 2>&1 || true; return 1; }
    sed -i 's#^Server = .*#Server = { Host = "localhost", Port = 25565 }#' "$cfg"
    FAIL_DETAIL=""
    setup_world "$rcon_port" || { printf '%s\tFAIL\t%s\t%s\n' "$version" "${FAIL_DETAIL:-world setup failed}" "$log_file" >> "$SUMMARY"; mc-stop "$target" --confirm >/dev/null 2>&1 || true; return 1; }

    mcc_root="$(dirname "$cfg")"
    mkdir -p "/tmp/mcc-debug/$session"
    local input_file="/tmp/mcc-debug/$session/mcc_input.txt"
    local pid_file="/tmp/mcc-debug/$session/mcc.pid"
    : > "$input_file"
    (
        cd "$mcc_root" || exit 1
        printf '%s\n' "$$" > "$pid_file"
        exec env MCC_FILE_INPUT=1 MCC_INPUT_FILE="$input_file" dotnet run --project "$REPO_ROOT/MinecraftClient" -c Release --no-build > "$log_file" 2>&1
    ) &
    local mcc_pid=$!
    printf '%s\n' "$mcc_pid" > "$pid_file"

    local ok=0
    if ! wait_for_file_pattern_local "$log_file" "Server was successfully joined" 40; then
        FAIL_DETAIL="MCC did not join server"
        ok=1
    else
        setup_player "$rcon_port" "$username" || { FAIL_DETAIL="player setup failed after join"; ok=1; }
        setup_area "$rcon_port" || { ok=1; }
        setup_player "$rcon_port" "$username" || { FAIL_DETAIL="player setup failed after area setup"; ok=1; }
        sleep 2
        if [[ -z "${FAIL_DETAIL:-}" ]]; then
            FAIL_DETAIL=""
            run_inventory_sequence "$version" "$rcon_port" "$session" "$username" "$log_file" "$block_file"
            ok=$?
        fi
    fi

    kill "$mcc_pid" >/dev/null 2>&1 || true
    wait "$mcc_pid" >/dev/null 2>&1 || true
    mc-stop "$target" --confirm >/dev/null 2>&1 || true
    wait_for_server_stop "$target" >/dev/null 2>&1 || true

    if [[ "$ok" -eq 0 ]]; then
        printf '%s\tPASS\tfull inventory command/API sweep\t%s\n' "$version" "$log_file" >> "$SUMMARY"
        echo "PASS $version"
        return 0
    fi

    printf '%s\tFAIL\t%s\t%s\n' "$version" "${FAIL_DETAIL:-unknown failure}" "$log_file" >> "$SUMMARY"
    echo "${FAIL_DETAIL:-unknown failure}" >&2
    echo "FAIL $version"
    return 1
}

overall=0
for version in $VERSIONS; do
    if ! run_one_version "$version"; then
        overall=1
        [[ "$STOP_ON_FAIL" == "1" ]] && break
    fi
done

echo "SUMMARY=$SUMMARY"

exit "$overall"
