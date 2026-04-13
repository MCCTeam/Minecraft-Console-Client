#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION="mcc-pathing-template"
TEST_ROOT="${TMPDIR:-/tmp}/mcc-pathing-template"
CFG="$TEST_ROOT/MinecraftClient.pathing-template.ini"
LOG="$TEST_ROOT/mcc-pathing-template.log"
INPUT_FILE="$REPO_ROOT/mcc_input.txt"
PREPARE_CFG_SCRIPT="$REPO_ROOT/.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh"
ENSURE_SERVER_SCRIPT="$REPO_ROOT/.skills/mcc-integration-testing/scripts/ensure_offline_server.sh"

mkdir -p "$TEST_ROOT"

send_mcc() {
    echo "$1" >> "$INPUT_FILE"
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
    local timeout="${3:-20}"

    for _ in $(seq 1 "$timeout"); do
        if log_since "$from_line" | grep -Fq "$pattern"; then
            return 0
        fi
        sleep 1
    done

    return 1
}

wait_for_navigation() {
    local from_line="$1"
    local timeout="${2:-25}"

    for _ in $(seq 1 "$timeout"); do
        local recent
        recent="$(log_since "$from_line")"

        if grep -Eq "\\[PathMgr\\] (Replan failed|Giving up)|\\[PathMgr\\] Segment failed, replanning|\\[PathExec\\] Segment .* FAILED" <<<"$recent"; then
            echo "$recent" >&2
            return 1
        fi

        if grep -Fq "[PathMgr] Navigation complete!" <<<"$recent"; then
            return 0
        fi

        sleep 1
    done

    echo "Timed out waiting for navigation completion" >&2
    log_since "$from_line" >&2
    return 1
}

wait_for_failure_signal() {
    local from_line="$1"
    local timeout="${2:-20}"

    for _ in $(seq 1 "$timeout"); do
        local recent
        recent="$(log_since "$from_line")"

        if grep -Eq "\\[PathMgr\\] (Replan failed|Giving up)|No path found|\\[Navigate\\] A\\* result: Failed" <<<"$recent"; then
            return 0
        fi

        sleep 1
    done

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
matches = re.findall(r"Location\s+([-\d.]+),\s+([-\d.]+),\s+([-\d.]+)", text)
if not matches:
    matches = re.findall(r"Segment \d+ complete .* at \(([-\d.]+),([-\d.]+),([-\d.]+)\)", text)
if not matches:
    matches = re.findall(r"pos=\(([-\d.]+),\s*([-\d.]+),\s*([-\d.]+)\)", text)
if not matches:
    raise SystemExit("No location line found in MCC log")
x, y, z = matches[-1]
print(f"{x} {y} {z}")
PY
}

assert_close() {
    local actual_x="$1"
    local actual_y="$2"
    local actual_z="$3"
    local target_x="$4"
    local target_y="$5"
    local target_z="$6"
    local tolerance="${7:-0.2}"

    python3 - <<'PY' "$actual_x" "$actual_y" "$actual_z" "$target_x" "$target_y" "$target_z" "$tolerance"
import math
import sys

ax, ay, az, tx, ty, tz, tol = map(float, sys.argv[1:])
if abs(ax - tx) > tol or abs(ay - ty) > tol or abs(az - tz) > tol:
    raise SystemExit(
        f"Expected ({tx:.2f}, {ty:.2f}, {tz:.2f}) within {tol:.2f}, got ({ax:.2f}, {ay:.2f}, {az:.2f})"
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

start_mcc() {
    bash "$PREPARE_CFG_SCRIPT" "$CFG" "$VERSION" CursorBot >/dev/null

    : > "$INPUT_FILE"
    : > "$LOG"

    tmux kill-session -t "$SESSION" 2>/dev/null || true
    tmux new-session -d -s "$SESSION" -x 160 -y 50 \
        "cd '$REPO_ROOT' && MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release --no-build -- '$CFG' CursorBot - localhost:25565 > '$LOG' 2>&1; echo '=== MCC EXITED ==='; sleep 600"

    wait_for_log "Server was successfully joined." 0 20
    send_mcc "debug on"
    sleep 1
}

run_flat_final_stop() {
    echo "== Flat final stop =="
    mc-rcon "fill 95 79 95 115 79 105 stone" >/dev/null
    mc-rcon "fill 95 80 95 115 85 105 air" >/dev/null
    mc-rcon "tp CursorBot 100.5 80 100.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 103 80 100"
    wait_for_navigation "$start_line" 30

    local x y z
    read -r x y z <<< "$(extract_last_location "$start_line")"
    echo "  Final location: $x $y $z"
    assert_close "$x" "$y" "$z" "103.50" "80.00" "100.50"
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
    mc-rcon "tp CursorBot 120.5 80 110.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 122 80 111"
    wait_for_navigation "$start_line" 30

    local x y z
    read -r x y z <<< "$(extract_last_location "$start_line")"
    echo "  Final location: $x $y $z"
    assert_close "$x" "$y" "$z" "122.50" "80.00" "111.50"
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
    mc-rcon "tp CursorBot 131.5 80 127.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 133 80 127"

    if wait_for_failure_signal "$start_line" 20; then
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
    mc-rcon "tp CursorBot 141.5 80 138.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 144 81 138"

    if wait_for_log "Replan failed" "$start_line" 20; then
        echo "  Pathfinding rejected as expected."
    elif wait_for_navigation "$start_line" 30; then
        local x y z
        read -r x y z <<< "$(extract_last_location "$start_line")"
        if python3 - <<'PY' "$x" "$y" "$z"
import sys
x, y, z = map(float, sys.argv[1:])
tx, ty, tz = 144.5, 81.0, 138.5
tol = 0.2
sys.exit(0 if abs(x - tx) > tol or abs(y - ty) > tol or abs(z - tz) > tol else 1)
PY
        then
            echo "  Pathfinder only reached a partial fallback, rejection accepted."
        else
            echo "  Expected rejection but goal was reached." >&2
            return 1
        fi
    else
        echo "  Expected rejection but navigation continued." >&2
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
    mc-rcon "tp CursorBot 190.5 80 170.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 191 81 171"
    wait_for_navigation "$start_line" 25

    local x y z
    read -r x y z <<< "$(extract_last_location "$start_line")"
    echo "  Final location: $x $y $z"
    assert_close "$x" "$y" "$z" "191.50" "81.00" "171.50" "0.25"
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
    mc-rcon "tp CursorBot 200.5 81 200.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 201 80 200"
    wait_for_navigation "$start_line" 25

    local x y z
    read -r x y z <<< "$(extract_last_location "$start_line")"
    echo "  Final location: $x $y $z"
    assert_close "$x" "$y" "$z" "201.50" "80.00" "200.50" "0.25"
    print_summary "Wall-adjacent descend"
}

run_ascend_chain_smoke() {
    echo "== Ascend chain smoke =="
    mc-rcon "fill 170 79 160 178 79 168 stone" >/dev/null
    mc-rcon "fill 170 80 160 178 85 168 air" >/dev/null
    mc-rcon "setblock 175 80 162 stone" >/dev/null
    mc-rcon "setblock 176 81 162 stone" >/dev/null
    mc-rcon "setblock 177 82 162 stone" >/dev/null
    mc-rcon "fill 178 78 160 182 78 164 stone" >/dev/null
    mc-rcon "fill 178 83 160 182 83 164 air" >/dev/null
    mc-rcon "setblock 181 80 162 minecraft:ladder[facing=east]" >/dev/null
    mc-rcon "setblock 181 81 162 minecraft:ladder[facing=east]" >/dev/null
    mc-rcon "setblock 181 82 162 minecraft:ladder[facing=east]" >/dev/null
    mc-rcon "setblock 181 83 162 minecraft:ladder[facing=east]" >/dev/null
    mc-rcon "tp CursorBot 171.5 80 160.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "pathfind 182 83 162"
    wait_for_navigation "$start_line" 35

    echo "  Ascend chain completed."
    print_summary "Ascend chain smoke"
}

mcc-preflight "$VERSION" >/dev/null
mc-reset-test-env "$VERSION" >/dev/null
bash "$ENSURE_SERVER_SCRIPT" "$VERSION" >/dev/null
mc-start "$VERSION" >/dev/null
mc-wait-ready "$VERSION" 60 >/dev/null
mcc-kill >/dev/null 2>&1 || true
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
