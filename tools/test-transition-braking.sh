#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION="${SESSION:-mcc-brake-test}"
USERNAME="${USERNAME:-MCCBot}"

SESSION_ROOT="$(_mcc_session_root "$SESSION")"
LOG="$(_mcc_session_log_file "$SESSION")"

cleanup() {
    mcc-kill --session "$SESSION" >/dev/null 2>&1 || true
}

trap cleanup EXIT

send_mcc() {
    mcc-cmd --session "$SESSION" "$1"
}

log_line_count() {
    if [[ -f "$LOG" ]]; then
        wc -l < "$LOG"
    else
        echo 0
    fi
}

log_since() {
    local from_line="$1"
    if [[ ! -f "$LOG" ]]; then
        return
    fi

    tail -n +"$((from_line + 1))" "$LOG"
}

wait_for_log() {
    local pattern="$1"
    local from_line="${2:-0}"
    local timeout="${3:-30}"

    for _ in $(seq 1 "$timeout"); do
        if log_since_clean "$from_line" | grep -Fq "$pattern"; then
            return 0
        fi
        sleep 1
    done

    return 1
}

wait_for_navigation() {
    local from_line="$1"
    local timeout="${2:-20}"
    local saw_start=0

    for _ in $(seq 1 "$timeout"); do
        local recent
        recent="$(log_since_clean "$from_line")"

        if grep -Fq "[PathMgr] Navigation started" <<<"$recent"; then
            saw_start=1
        fi

        if (( saw_start )) && grep -Fq "[PathMgr] Navigation complete!" <<<"$recent"; then
            return 0
        fi

        if grep -Eq "\[PathMgr\] (Replan failed|Giving up)" <<<"$recent"; then
            echo "$recent" >&2
            return 1
        fi

        if grep -Eq "No path found|\[Navigate\] A\* result: Failed" <<<"$recent"; then
            echo "$recent" >&2
            return 1
        fi

        sleep 1
    done

    echo "Timed out waiting for navigation completion" >&2
    log_since_clean "$from_line" >&2
    return 1
}

log_since_clean() {
    log_since "$1" | sed 's/\x1b\[[0-9;]*m//g'
}

count_replans_since() {
    local from_line="$1"
    log_since_clean "$from_line" | grep -Ec '\[PathMgr\] Replan #|\[PathExec\] Segment .* FAILED' || true
}

assert_no_replans_since() {
    local from_line="$1"
    local count
    count="$(count_replans_since "$from_line")"
    if [[ "$count" != "0" ]]; then
        echo "Expected 0 replans, saw $count" >&2
        log_since_clean "$from_line" >&2
        return 1
    fi
}

assert_no_partial_since() {
    local from_line="$1"
    if log_since_clean "$from_line" | grep -Fq "[Navigate] A* result: Partial"; then
        echo "Expected full success path, saw partial path" >&2
        log_since_clean "$from_line" >&2
        return 1
    fi
}

debug_state_snapshot() {
    local label="$1"
    local from_line
    from_line="$(log_line_count)"
    send_mcc "debug state"
    wait_for_log "Location" "$from_line" 10
    echo ""
    echo "=== Debug state: $label ==="
    log_since_clean "$from_line"
}

capture_debug_state_before_route() {
    debug_state_snapshot "before route - $1"
}

capture_debug_state_after_route() {
    debug_state_snapshot "after route - $1"
}

prepare_independent_route() {
    local label="$1"
    local start_x="$2"
    local start_y="$3"
    local start_z="$4"
    local start_yaw="${5:-270}"
    local start_pitch="${6:-0}"

    echo ""
    echo "Preparing independent route: $label"
    mc-rcon "effect clear $USERNAME" >/dev/null 2>&1 || true
    mc-rcon "tp $USERNAME $start_x $start_y $start_z $start_yaw $start_pitch" >/dev/null
    wait_for_location_in_block "$start_x" "$start_y" "$start_z" 10
}

extract_last_location() {
    local from_line="${1:-0}"

    python3 - "$LOG" "$from_line" <<'PY'
import pathlib
import re
import sys

log_path = pathlib.Path(sys.argv[1])
from_line = int(sys.argv[2])
text = log_path.read_text(errors="ignore")
text = "\n".join(text.splitlines()[from_line:])
text = re.sub(r"\x1b\[[0-9;]*m", "", text)
matches = re.findall(r"Location\s+([-,0-9.]+),\s+([-,0-9.]+),\s+([-,0-9.]+)", text)
if not matches:
    raise SystemExit("No Location line found in MCC log")
x, y, z = matches[-1]
print(f"{x} {y} {z}")
PY
}

assert_inside_goal_block() {
    local actual_x="$1"
    local actual_y="$2"
    local actual_z="$3"
    local expected_x="$4"
    local expected_y="$5"
    local expected_z="$6"

    python3 - <<'PY' "$actual_x" "$actual_y" "$actual_z" "$expected_x" "$expected_y" "$expected_z"
import math
import sys

ax, ay, az, ex, ey, ez = map(float, sys.argv[1:])
if math.floor(ax) != int(ex) or math.floor(az) != int(ez) or abs(ay - ey) > 0.05:
    raise SystemExit(
        f"Expected location inside goal block ({int(ex)}, {ey:.2f}, {int(ez)}), got ({ax:.2f}, {ay:.2f}, {az:.2f})"
    )
PY
}

capture_debug_location() {
    local start_line
    start_line="$(log_line_count)"
    send_mcc "debug state"
    wait_for_log "Location" "$start_line" 10
    extract_last_location "$start_line"
}

wait_for_location_in_block() {
    local expected_x="$1"
    local expected_y="$2"
    local expected_z="$3"
    local timeout="${4:-10}"

    for _ in $(seq 1 "$timeout"); do
        local actual_x actual_y actual_z
        read -r actual_x actual_y actual_z <<< "$(capture_debug_location)"
        if python3 - <<'PY' "$actual_x" "$actual_y" "$actual_z" "$expected_x" "$expected_y" "$expected_z"
import math
import sys

ax, ay, az, ex, ey, ez = map(float, sys.argv[1:])
if math.floor(ax) == int(ex) and math.floor(az) == int(ez) and abs(ay - ey) <= 0.05:
    raise SystemExit(0)
raise SystemExit(1)
PY
        then
            return 0
        fi
        sleep 1
    done

    echo "Timed out waiting for player to reach start block ($expected_x, $expected_y, $expected_z)" >&2
    return 1
}

start_mcc() {
    mcc-kill --session "$SESSION" >/dev/null 2>&1 || true
    mkdir -p "$SESSION_ROOT"
    mcc-build >/dev/null
    mcc-debug -v "$VERSION" --session "$SESSION" --username "$USERNAME" --file-input --no-build --debug-on >/dev/null
    wait_for_log "Server was successfully joined." 0 30
    send_mcc "debug on"
}

run_flat_final_stop() {
    echo "== Flat final stop =="
    mc-rcon "fill 95 79 95 115 79 105 stone" >/dev/null
    mc-rcon "fill 95 80 95 115 85 105 air" >/dev/null
    prepare_independent_route "Flat final stop" "100.5" "80" "100.5" "270"
    capture_debug_state_before_route "Flat final stop"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "goto 103 80 100"
    wait_for_navigation "$start_line" 20
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    capture_debug_state_after_route "Flat final stop"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "103" "80.00" "100"
}

run_parkour_into_turn() {
    echo "== Parkour into turn =="
    mc-rcon "fill 118 79 108 126 79 112 air" >/dev/null
    mc-rcon "fill 118 80 108 126 85 112 air" >/dev/null
    mc-rcon "setblock 120 79 110 stone" >/dev/null
    mc-rcon "setblock 123 79 110 stone" >/dev/null
    mc-rcon "setblock 123 79 111 stone" >/dev/null
    prepare_independent_route "Parkour into turn" "120.5" "80" "110.5" "270"
    capture_debug_state_before_route "Parkour into turn"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "goto 123 80 111"
    wait_for_navigation "$start_line" 20
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    capture_debug_state_after_route "Parkour into turn"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "123" "80.00" "111"
}

start_mcc

mc-rcon "difficulty peaceful" >/dev/null 2>&1 || true
mc-rcon "gamerule doMobSpawning false" >/dev/null 2>&1 || true
mc-rcon "time set day" >/dev/null 2>&1 || true

run_flat_final_stop
run_parkour_into_turn

echo "All transition braking checks passed."
