#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION="mcc-pathing-jump-combos"
USERNAME="MCCBot"

SESSION_ROOT="$(_mcc_session_root "$SESSION")"
LOG="$(_mcc_session_log_file "$SESSION")"
PLANNER_CONTRACTS="$REPO_ROOT/MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json"
TIMING_BUDGETS="$REPO_ROOT/MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json"

PASSED_CASES=()
FAILED_CASES=()

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

log_since_clean() {
    log_since "$1" | sed 's/\x1b\[[0-9;]*m//g'
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
    local timeout="${2:-45}"
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

fill_box() {
    mc-rcon "fill $1 $2 $3 $4 $5 $6 $7" >/dev/null
}

set_stone() {
    mc-rcon "setblock $1 $2 $3 stone" >/dev/null
}

run_accepted_route() {
    local scenario_id="$1"
    local label="$2"
    local start_x="$3"
    local start_y="$4"
    local start_z="$5"
    local goal_x="$6"
    local goal_y="$7"
    local goal_z="$8"
    local start_yaw="${9:-270}"
    local start_pitch="${10:-0}"
    local timeout="${11:-45}"

    prepare_independent_route "$label" "$start_x" "$start_y" "$start_z" "$start_yaw" "$start_pitch"
    capture_debug_state_before_route "$label"

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind $goal_x $goal_y $goal_z"
    wait_for_navigation "$start_line" "$timeout"
    assert_no_partial_since "$start_line"
    assert_no_replans_since "$start_line"
    python3 "$REPO_ROOT/tools/pathing_contract_report.py" \
        --scenario-id "$scenario_id" \
        --log-file "$LOG" \
        --from-line "$start_line" \
        --planner-contracts "$PLANNER_CONTRACTS" \
        --timing-budgets "$TIMING_BUDGETS"
    capture_debug_state_after_route "$label"

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "  Final location: $x $y $z"
    assert_inside_goal_block "$x" "$y" "$z" "$goal_x" "$goal_y" "$goal_z"
    print_summary "$label"
}

run_case() {
    local label="$1"
    shift

    echo ""
    echo "== $label =="
    set +e
    (
        set -e
        "$@"
    )
    local status=$?
    set -e

    if [[ $status -eq 0 ]]; then
        PASSED_CASES+=("$label")
        echo "RESULT: PASS - $label"
    else
        FAILED_CASES+=("$label")
        echo "RESULT: FAIL - $label"
    fi
}

start_mcc() {
    mkdir -p "$SESSION_ROOT"
    mcc-kill --session "$SESSION" >/dev/null 2>&1 || true
    mcc-build >/dev/null
    mcc-debug -v "$VERSION" --session "$SESSION" --username "$USERNAME" --file-input --debug-on --no-build >/dev/null
    wait_for_log "Server was successfully joined." 0 40
    send_mcc "debug on"
}

scenario_repeated_cardinal_parkour() {
    fill_box 578 79 578 590 79 582 air
    fill_box 578 80 578 590 90 582 air
    set_stone 580 79 580
    set_stone 582 79 580
    set_stone 584 79 580
    set_stone 586 79 580
    set_stone 588 79 580
    run_accepted_route "repeated-cardinal-parkour-chain" "Repeated jump - cardinal parkour chain" "580.5" "80" "580.5" "588" "80.00" "580" "270"
}

scenario_repeated_diagonal_parkour() {
    fill_box 598 79 598 608 79 608 air
    fill_box 598 80 598 608 90 608 air
    set_stone 600 79 600
    set_stone 602 79 602
    set_stone 604 79 604
    set_stone 606 79 606
    run_accepted_route "repeated-diagonal-parkour-chain" "Repeated jump - diagonal parkour chain" "600.5" "80" "600.5" "606" "80.00" "606" "315"
}

scenario_obstructed_parkour_turn_mix() {
    fill_box 618 79 618 628 79 624 air
    fill_box 618 80 618 628 90 624 air
    set_stone 620 79 620
    set_stone 622 79 620
    set_stone 622 79 621
    set_stone 624 79 621
    set_stone 624 79 622
    set_stone 626 79 622
    set_stone 620 80 621
    set_stone 620 81 621
    set_stone 622 80 622
    set_stone 622 81 622
    run_accepted_route "obstructed-parkour-l-turns" "Obstructed jump mix - repeated parkour L-turns" "620.5" "80" "620.5" "626" "80.00" "622" "270"
}

scenario_parkour_ascend_descend_chain() {
    fill_box 638 79 618 650 80 622 air
    fill_box 638 81 618 650 92 622 air
    set_stone 640 79 620
    set_stone 642 80 620
    set_stone 644 79 620
    set_stone 646 80 620
    set_stone 648 79 620
    run_accepted_route "vertical-jump-mix" "Vertical jump mix - parkour ascend descend chain" "640.5" "80" "620.5" "648" "80.00" "620" "270"
}

scenario_diagonal_ascend_descend_chain() {
    fill_box 678 79 618 686 80 626 air
    fill_box 678 81 618 686 92 626 air
    set_stone 680 79 620
    set_stone 681 80 621
    set_stone 682 79 622
    set_stone 683 80 623
    set_stone 684 79 624
    run_accepted_route "diagonal-vertical-mix" "Diagonal vertical mix - ascend descend chain" "680.5" "80" "620.5" "684" "80.00" "624" "315"
}

start_mcc

mc-rcon "difficulty peaceful" >/dev/null 2>&1 || true
mc-rcon "gamerule doMobSpawning false" >/dev/null 2>&1 || true
mc-rcon "time set day" >/dev/null 2>&1 || true

run_case "Repeated jump - cardinal parkour chain" scenario_repeated_cardinal_parkour
run_case "Repeated jump - diagonal parkour chain" scenario_repeated_diagonal_parkour
run_case "Obstructed jump mix - repeated parkour L-turns" scenario_obstructed_parkour_turn_mix
run_case "Vertical jump mix - parkour ascend descend chain" scenario_parkour_ascend_descend_chain
run_case "Diagonal vertical mix - ascend descend chain" scenario_diagonal_ascend_descend_chain

echo ""
echo "Jump combo summary:"
for label in "${PASSED_CASES[@]}"; do
    echo "  PASS  $label"
done
for label in "${FAILED_CASES[@]}"; do
    echo "  FAIL  $label"
done

if [[ ${#FAILED_CASES[@]} -ne 0 ]]; then
    exit 1
fi

echo ""
echo "Pathing jump-combo suite complete."
