#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION="mcc-pathing-template"
USERNAME="MCCBot"

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
    local timeout="${2:-25}"
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

wait_for_failure_signal() {
    local from_line="$1"
    local timeout="${2:-20}"

    for _ in $(seq 1 "$timeout"); do
        local recent
        recent="$(log_since_clean "$from_line")"

        if grep -Eq "No path found|\[Navigate\] A\* result: Failed" <<<"$recent"; then
            return 0
        fi

        sleep 1
    done

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

    echo ""
    echo "Preparing independent route: $label"
    mc-rcon "effect clear $USERNAME" >/dev/null 2>&1 || true
    mc-rcon "tp $USERNAME $start_x $start_y $start_z" >/dev/null
    wait_for_location_in_block "$start_x" "$start_y" "$start_z" 10
}

assert_direct_rejection_since() {
    local from_line="$1"
    if ! log_since_clean "$from_line" | grep -Eq "No path found|\[Navigate\] A\* result: Failed"; then
        echo "Expected direct rejection before navigation execution" >&2
        log_since_clean "$from_line" >&2
        return 1
    fi
    if log_since_clean "$from_line" | grep -Fq "[PathMgr] Navigation started"; then
        echo "Expected rejection before navigation started" >&2
        log_since_clean "$from_line" >&2
        return 1
    fi
    assert_no_replans_since "$from_line"
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
    matches = re.findall(r"Segment \d+ complete .* at \(([-,0-9.]+),([-,0-9.]+),([-,0-9.]+)\)", text)
if not matches:
    matches = re.findall(r"pos=\(([-,0-9.]+),\s*([-,0-9.]+),\s*([-,0-9.]+)\)", text)
if not matches:
    raise SystemExit("No location line found in MCC log")
x, y, z = matches[-1]
print(f"{x} {y} {z}")
PY
}

assert_inside_goal_block() {
    local actual_x="$1"
    local actual_y="$2"
    local actual_z="$3"
    local target_x="$4"
    local target_y="$5"
    local target_z="$6"

    python3 - <<'PY' "$actual_x" "$actual_y" "$actual_z" "$target_x" "$target_y" "$target_z"
import math
import sys

ax, ay, az, tx, ty, tz = map(float, sys.argv[1:])
if math.floor(ax) != int(tx) or math.floor(az) != int(tz) or abs(ay - ty) > 0.05:
    raise SystemExit(
        f"Expected location inside goal block ({int(tx)}, {ty:.2f}, {int(tz)}), got ({ax:.2f}, {ay:.2f}, {az:.2f})"
    )
PY
}

print_summary() {
    local header="$1"

    echo ""
    echo "----- $header -----"
    if [[ -f "$LOG" ]]; then
        tail -n 40 "$LOG" | sed 's/\x1b\[[0-9;]*m//g'
    else
        echo "(no log available yet)"
    fi
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
    mkdir -p "$SESSION_ROOT"
    mcc-kill --session "$SESSION" >/dev/null 2>&1 || true
    mcc-build >/dev/null
    mcc-debug -v "$VERSION" --session "$SESSION" --username "$USERNAME" --file-input --debug-on --no-build >/dev/null
    wait_for_log "Server was successfully joined." 0 40
    send_mcc "debug on"
}

run_flat_final_stop() {
    echo "== Flat final stop =="
    mc-rcon "fill 95 79 95 115 79 105 stone" >/dev/null
    mc-rcon "fill 95 80 95 115 85 105 air" >/dev/null
    prepare_independent_route "Flat final stop" "100.5" "80" "100.5"
    capture_debug_state_before_route "Flat final stop"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 103 80 100"
    wait_for_navigation "$start_line" 30
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    capture_debug_state_after_route "Flat final stop"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "  Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "103" "80.00" "100"
    print_summary "Flat final stop"
}

run_parkour_into_turn() {
    echo "== Parkour into L-turn =="
    mc-rcon "fill 118 79 108 126 79 112 air" >/dev/null
    mc-rcon "fill 118 80 108 126 90 112 air" >/dev/null
    mc-rcon "setblock 120 79 110 stone" >/dev/null
    mc-rcon "setblock 122 79 110 stone" >/dev/null
    mc-rcon "setblock 122 79 111 stone" >/dev/null
    mc-rcon "setblock 120 80 111 stone" >/dev/null
    mc-rcon "setblock 120 81 111 stone" >/dev/null
    prepare_independent_route "Parkour into L-turn" "120.5" "80" "110.5"
    capture_debug_state_before_route "Parkour into L-turn"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 122 80 111"
    wait_for_navigation "$start_line" 30
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    capture_debug_state_after_route "Parkour into L-turn"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "  Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "122" "80.00" "111"
    print_summary "Parkour into L-turn"
}

run_side_wall_jump() {
    echo "== Rejected 2x1 side-wall jump =="
    mc-rcon "fill 130 79 124 138 79 132 air" >/dev/null
    mc-rcon "fill 130 80 124 138 84 132 air" >/dev/null
    mc-rcon "setblock 131 79 127 stone" >/dev/null
    mc-rcon "setblock 133 79 127 stone" >/dev/null
    mc-rcon "setblock 132 80 126 stone" >/dev/null
    mc-rcon "setblock 132 81 126 stone" >/dev/null
    mc-rcon "setblock 133 80 126 stone" >/dev/null
    mc-rcon "setblock 133 81 126 stone" >/dev/null
    prepare_independent_route "Rejected 2x1 side-wall jump" "131.5" "80" "127.5"
    capture_debug_state_before_route "Rejected 2x1 side-wall jump"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 133 80 127"

    if wait_for_failure_signal "$start_line" 20; then
        assert_direct_rejection_since "$start_line"
        echo "  Pathfinding rejected as expected."
    else
        echo "  Expected rejection but navigation continued." >&2
        log_since "$start_line" >&2
        return 1
    fi

    print_summary "2x1 side-wall rejection"
}

run_reject_3x1_gap() {
    echo "== Rejected 3x1 no-run-up gap =="
    mc-rcon "fill 140 79 135 148 79 140 stone" >/dev/null
    mc-rcon "fill 140 80 135 148 85 140 air" >/dev/null
    mc-rcon "setblock 143 80 138 stone" >/dev/null
    prepare_independent_route "Rejected 3x1 no-run-up gap" "141.5" "80" "138.5"
    capture_debug_state_before_route "Rejected 3x1 gap"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 144 81 138"

    if wait_for_failure_signal "$start_line" 20; then
        assert_direct_rejection_since "$start_line"
        echo "  Pathfinding rejected as expected."
    else
        echo "  Expected rejection but navigation continued." >&2
        log_since "$start_line" >&2
        return 1
    fi

    print_summary "3x1 no-run-up rejection"
}

run_corner_ascend_around_wall() {
    echo "== Corner ascend around wall smoke =="
    mc-rcon "fill 188 79 168 194 84 174 air" >/dev/null
    mc-rcon "setblock 190 79 170 stone" >/dev/null
    mc-rcon "setblock 191 80 171 stone" >/dev/null
    mc-rcon "setblock 191 80 170 stone" >/dev/null
    mc-rcon "setblock 191 81 170 stone" >/dev/null
    prepare_independent_route "Corner ascend around wall" "190.5" "80" "170.5"
    capture_debug_state_before_route "Corner ascend around wall"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 191 81 171"
    wait_for_navigation "$start_line" 25
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    capture_debug_state_after_route "Corner ascend around wall"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "  Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "191" "81.00" "171"
    print_summary "Corner ascend around wall"
}

run_wall_adjacent_descend_smoke() {
    echo "== Wall-adjacent descend smoke =="
    mc-rcon "fill 198 79 198 204 84 202 air" >/dev/null
    mc-rcon "fill 201 79 199 203 79 201 stone" >/dev/null
    mc-rcon "setblock 200 80 200 stone" >/dev/null
    mc-rcon "setblock 200 80 199 stone" >/dev/null
    mc-rcon "setblock 201 80 199 stone" >/dev/null
    mc-rcon "setblock 202 80 199 stone" >/dev/null
    mc-rcon "setblock 201 81 199 stone" >/dev/null
    mc-rcon "setblock 202 81 199 stone" >/dev/null
    prepare_independent_route "Wall-adjacent descend" "200.5" "81" "200.5"
    capture_debug_state_before_route "Wall-adjacent descend"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 201 80 200"
    wait_for_navigation "$start_line" 25
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    capture_debug_state_after_route "Wall-adjacent descend"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "  Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "201" "80.00" "200"
    print_summary "Wall-adjacent descend"
}

run_ascend_chain_smoke() {
    echo "== Ascend chain smoke =="
    mc-rcon "fill 170 79 160 178 79 168 stone" >/dev/null
    mc-rcon "fill 170 80 160 178 85 168 air" >/dev/null
    mc-rcon "setblock 175 80 162 stone" >/dev/null
    mc-rcon "setblock 176 81 162 stone" >/dev/null
    mc-rcon "setblock 177 82 162 stone" >/dev/null
    prepare_independent_route "Ascend chain smoke" "171.5" "80" "160.5"
    capture_debug_state_before_route "Ascend chain smoke"
    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 177 83 162"
    wait_for_navigation "$start_line" 35
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    capture_debug_state_after_route "Ascend chain smoke"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "  Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "177" "83.00" "162"

    print_summary "Ascend chain smoke"
}

start_mcc

mc-rcon "difficulty peaceful" >/dev/null 2>&1 || true
mc-rcon "gamerule doMobSpawning false" >/dev/null 2>&1 || true
mc-rcon "time set day" >/dev/null 2>&1 || true

run_flat_final_stop
run_parkour_into_turn
run_side_wall_jump
run_reject_3x1_gap
run_corner_ascend_around_wall
run_wall_adjacent_descend_smoke
run_ascend_chain_smoke

echo ""
echo "Pathing template regression suite complete."
