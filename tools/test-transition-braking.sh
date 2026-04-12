#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION="mcc-brake-test"
TEST_ROOT="${TMPDIR:-/tmp}/mcc-debug"
CFG="$TEST_ROOT/MinecraftClient.transition-braking.ini"
LOG="$TEST_ROOT/mcc-transition-braking.log"
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
    local timeout="${2:-20}"

    for _ in $(seq 1 "$timeout"); do
        local recent
        recent="$(log_since "$from_line")"

        if grep -Fq "[PathMgr] Navigation complete!" <<<"$recent"; then
            return 0
        fi

        if grep -Eq "\\[PathMgr\\] (Replan failed|Giving up)|\\[PathExec\\] Segment .* FAILED" <<<"$recent"; then
            echo "$recent" >&2
            return 1
        fi

        sleep 1
    done

    echo "Timed out waiting for navigation completion" >&2
    log_since "$from_line" >&2
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
    raise SystemExit("No Location line found in MCC log")
x, y, z = matches[-1]
print(f"{x} {y} {z}")
PY
}

assert_close() {
    local actual_x="$1"
    local actual_y="$2"
    local actual_z="$3"
    local expected_x="$4"
    local expected_y="$5"
    local expected_z="$6"
    local tolerance="${7:-0.05}"

    python3 - <<'PY' "$actual_x" "$actual_y" "$actual_z" "$expected_x" "$expected_y" "$expected_z" "$tolerance"
import math
import sys

ax, ay, az, ex, ey, ez, tol = map(float, sys.argv[1:])
if abs(ax - ex) > tol or abs(ay - ey) > tol or abs(az - ez) > tol:
    raise SystemExit(
        f"Expected ({ex:.2f}, {ey:.2f}, {ez:.2f}) within {tol:.2f}, got ({ax:.2f}, {ay:.2f}, {az:.2f})"
    )
PY
}

capture_debug_location() {
    local start_line
    start_line="$(log_line_count)"
    send_mcc "debug state"
    wait_for_log "Location" "$start_line" 5
    extract_last_location "$start_line"
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
    send_mcc "goto 103 80 100"
    wait_for_navigation "$start_line" 20
    sleep 1

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "Final location: $x $y $z"
    assert_close "$x" "$y" "$z" "103.50" "80.00" "100.50"
}

run_parkour_into_turn() {
    echo "== Parkour into turn =="
    mc-rcon "fill 118 79 108 126 79 112 air" >/dev/null
    mc-rcon "setblock 120 79 110 stone" >/dev/null
    mc-rcon "setblock 123 79 110 stone" >/dev/null
    mc-rcon "setblock 123 79 111 stone" >/dev/null
    mc-rcon "tp CursorBot 120.5 80 110.5" >/dev/null
    sleep 2

    local start_line
    start_line="$(log_line_count)"
    send_mcc "goto 123 80 111"
    wait_for_navigation "$start_line" 20
    sleep 1

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "Final location: $x $y $z"
    assert_close "$x" "$y" "$z" "123.50" "80.00" "111.50"
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

echo "All transition braking checks passed."
