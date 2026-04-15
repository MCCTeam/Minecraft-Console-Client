#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION="${SESSION:-mcc-pathing-long-routes}"
USERNAME="${USERNAME:-MCCBot}"

SESSION_ROOT="$(_mcc_session_root "$SESSION")"
LOG="$(_mcc_session_log_file "$SESSION")"
PLANNER_CONTRACTS="$REPO_ROOT/MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json"
TIMING_BUDGETS="$REPO_ROOT/MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json"

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

start_mcc() {
    mkdir -p "$SESSION_ROOT"
    mcc-kill --session "$SESSION" >/dev/null 2>&1 || true
    mcc-build >/dev/null
    mcc-debug -v "$VERSION" --session "$SESSION" --username "$USERNAME" --file-input --debug-on --no-build >/dev/null
    wait_for_log "Server was successfully joined." 0 40
    send_mcc "debug on"
}

run_same_move_routes() {
    echo "== Same move routes =="

    fill_box 298 79 298 314 79 302 air
    fill_box 298 80 298 314 90 302 air
    fill_box 300 79 300 312 79 300 stone
    run_accepted_route "same-move-straight-traverse-chain" "Same move - straight traverse chain" "300.5" "80" "300.5" "312" "80.00" "300" "270"

    fill_box 318 79 318 330 79 330 air
    fill_box 318 80 318 330 90 330 air
    set_stone 320 79 320
    set_stone 321 79 321
    set_stone 322 79 322
    set_stone 323 79 323
    set_stone 324 79 324
    set_stone 325 79 325
    set_stone 326 79 326
    set_stone 327 79 327
    run_accepted_route "same-move-diagonal-chain" "Same move - diagonal chain" "320.5" "80" "320.5" "327" "80.00" "327" "315"

    fill_box 338 79 338 347 85 342 air
    fill_box 338 80 338 347 90 342 air
    fill_box 340 79 339 340 79 341 stone
    fill_box 341 80 339 341 80 341 stone
    fill_box 342 81 339 342 81 341 stone
    fill_box 343 82 339 343 82 341 stone
    fill_box 344 83 339 344 83 341 stone
    fill_box 345 84 339 345 84 341 stone
    run_accepted_route "same-move-ascend-staircase" "Same move - ascend staircase" "340.5" "80" "340.5" "345" "85.00" "340" "270"

    fill_box 360 79 358 369 85 362 air
    fill_box 360 80 358 369 90 362 air
    fill_box 362 84 359 362 84 361 stone
    fill_box 363 83 359 363 83 361 stone
    fill_box 364 82 359 364 82 361 stone
    fill_box 365 81 359 365 81 361 stone
    fill_box 366 80 359 366 80 361 stone
    fill_box 367 79 359 367 79 361 stone
    run_accepted_route "same-move-descend-staircase" "Same move - descend staircase" "362.5" "85" "360.5" "367" "80.00" "360" "270"

    fill_box 378 79 378 390 79 382 air
    fill_box 378 80 378 390 90 382 air
    set_stone 380 79 380
    set_stone 382 79 380
    set_stone 384 79 380
    set_stone 386 79 380
    set_stone 388 79 380
    run_accepted_route "same-move-aligned-parkour-chain" "Same move - aligned parkour chain" "380.5" "80" "380.5" "388" "80.00" "380" "270"
}

run_mixed_move_routes() {
    echo "== Mixed move routes =="

    fill_box 398 79 398 410 79 406 air
    fill_box 398 80 398 410 90 406 air
    set_stone 400 79 400
    set_stone 401 79 400
    set_stone 402 79 400
    set_stone 402 79 401
    set_stone 402 79 402
    set_stone 404 79 402
    set_stone 405 79 402
    set_stone 406 79 402
    set_stone 406 79 403
    set_stone 406 79 404
    set_stone 407 79 404
    set_stone 408 79 404
    run_accepted_route "mixed-traverse-turn-parkour-turn-traverse" "Mixed - traverse turn parkour turn traverse" "400.5" "80" "400.5" "408" "80.00" "404" "270"

    fill_box 418 79 418 430 82 424 air
    fill_box 418 80 418 430 92 424 air
    set_stone 420 79 420
    set_stone 421 79 421
    set_stone 422 79 422
    set_stone 423 80 422
    set_stone 424 81 422
    set_stone 425 81 422
    set_stone 426 81 422
    set_stone 427 80 422
    set_stone 428 79 422
    run_accepted_route "mixed-diagonal-ascend-traverse-descend" "Mixed - diagonal ascend traverse descend" "420.5" "80" "420.5" "428" "80.00" "422" "315"

    fill_box 438 79 438 450 82 442 air
    fill_box 438 80 438 450 92 442 air
    set_stone 440 79 440
    set_stone 441 79 440
    set_stone 442 80 440
    set_stone 443 81 440
    set_stone 444 81 440
    set_stone 446 81 440
    set_stone 447 80 440
    set_stone 448 79 440
    run_accepted_route "mixed-traverse-ascend-parkour-descend" "Mixed - traverse ascend parkour descend" "440.5" "80" "440.5" "448" "80.00" "440" "270"
}

run_turn_density_routes() {
    echo "== Turn density routes =="

    fill_box 458 79 458 468 79 468 air
    fill_box 458 80 458 468 90 468 air
    set_stone 460 79 460
    set_stone 461 79 460
    set_stone 461 79 461
    set_stone 462 79 462
    set_stone 463 79 462
    set_stone 463 79 463
    set_stone 464 79 464
    set_stone 465 79 464
    set_stone 465 79 465
    set_stone 466 79 466
    run_accepted_route "turn-density-alternating-traverse-diagonal-chain" "Turn density - alternating traverse diagonal chain" "460.5" "80" "460.5" "466" "80.00" "466" "270"
}

run_speed_carry_routes() {
    echo "== Speed carry routes =="

    fill_box 478 79 478 490 83 482 air
    fill_box 478 80 478 490 94 482 air
    set_stone 480 79 480
    set_stone 481 79 480
    set_stone 482 80 480
    set_stone 483 80 480
    set_stone 484 81 480
    set_stone 485 81 480
    set_stone 486 82 480
    set_stone 487 82 480
    set_stone 488 83 480
    run_accepted_route "speed-carry-repeated-traverse-ascend" "Speed carry - repeated traverse ascend" "480.5" "80" "480.5" "488" "84.00" "480" "270"

    fill_box 498 79 498 510 82 502 air
    fill_box 498 80 498 510 94 502 air
    set_stone 500 82 500
    set_stone 501 82 500
    set_stone 502 81 500
    set_stone 503 81 500
    set_stone 504 80 500
    set_stone 505 80 500
    set_stone 506 79 500
    set_stone 507 79 500
    run_accepted_route "speed-carry-repeated-traverse-descend" "Speed carry - repeated traverse descend" "500.5" "83" "500.5" "507" "80.00" "500" "270"

    fill_box 518 79 518 532 79 522 air
    fill_box 518 80 518 532 90 522 air
    set_stone 520 79 520
    set_stone 521 79 520
    set_stone 523 79 520
    set_stone 524 79 520
    set_stone 526 79 520
    set_stone 527 79 520
    set_stone 529 79 520
    run_accepted_route "speed-carry-repeated-traverse-parkour" "Speed carry - repeated traverse parkour" "520.5" "80" "520.5" "529" "80.00" "520" "270"
}

start_mcc

mc-rcon "difficulty peaceful" >/dev/null 2>&1 || true
mc-rcon "gamerule doMobSpawning false" >/dev/null 2>&1 || true
mc-rcon "time set day" >/dev/null 2>&1 || true

run_same_move_routes
run_mixed_move_routes
run_turn_density_routes
run_speed_carry_routes

mcc-kill --session "$SESSION" >/dev/null 2>&1 || true

echo ""
echo "Pathing long-route suite complete."
