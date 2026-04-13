#!/usr/bin/env bash
# Automated parkour jump test for MCC pathfinding
# Usage: source tools/mcc-env.sh && bash tools/test-parkour.sh
#
# Prerequisites:
# - MCC connected with FileInput mode
# - CursorBot is OP
# - Server at 1.21.11-Vanilla

set -euo pipefail
source "$(dirname "$0")/mcc-env.sh"

LOG="/tmp/mcc-debug/mcc-debug.log"
RESULTS=""
TEST_NUM=0

run_test() {
    local name="$1"
    local start_x="$2" start_y="$3" start_z="$4"
    local dest_x="$5"  dest_y="$6"  dest_z="$7"
    
    TEST_NUM=$((TEST_NUM + 1))
    echo ""
    echo "=== TEST $TEST_NUM: $name ==="
    echo "  Start: ($start_x, $start_y, $start_z) -> Dest: ($dest_x, $dest_y, $dest_z)"
    
    # Respawn if dead, set creative, tp, then survival
    mcc-cmd "respawn" 2>/dev/null
    sleep 0.5
    mc-rcon "gamemode creative CursorBot" >/dev/null 2>&1
    sleep 0.3
    mc-rcon "tp CursorBot ${start_x}.5 ${start_y} ${start_z}.5" >/dev/null 2>&1
    sleep 2
    mc-rcon "gamemode survival CursorBot" >/dev/null 2>&1
    sleep 1
    
    # Clear log
    : > "$LOG"
    sleep 0.5
    
    # Execute pathfind
    mcc-cmd "pathfind $dest_x $dest_y $dest_z"
    sleep 8
    
    # Analyze result
    local a_star_result
    a_star_result=$(grep -a '\[A\*\]' "$LOG" | head -3 | sed 's/\x1b\[[0-9;]*m//g')
    
    local path_exec
    path_exec=$(grep -a '\[PathExec\]' "$LOG" | sed 's/\x1b\[[0-9;]*m//g')
    
    local path_mgr
    path_mgr=$(grep -a '\[PathMgr\]' "$LOG" | sed 's/\x1b\[[0-9;]*m//g')
    
    local nav_segs
    nav_segs=$(grep -a '\[Navigate\].*seg' "$LOG" | sed 's/\x1b\[[0-9;]*m//g')
    
    # Get final position
    local physics_line
    physics_line=$(grep -a '\[Physics\]' "$LOG" | tail -1 | sed 's/\x1b\[[0-9;]*m//g')
    
    # Check success/failure
    local result="UNKNOWN"
    if echo "$path_mgr" | grep -q "complete"; then
        result="PASS"
    elif echo "$path_mgr" | grep -q "Replan failed\|Giving up"; then
        result="FAIL"
    elif echo "$path_exec" | grep -q "FAILED"; then
        result="FAIL"
    elif echo "$a_star_result" | grep -q "Failed"; then
        result="NO_PATH"
    fi
    
    echo "  A*: $a_star_result"
    echo "  Segments: $nav_segs"
    echo "  Exec: $(echo "$path_exec" | tail -3)"
    echo "  Manager: $(echo "$path_mgr" | tail -2)"
    echo "  Physics: $physics_line"
    echo "  RESULT: $result"
    
    RESULTS="${RESULTS}TEST $TEST_NUM ($name): $result\n"
}

echo "========================================"
echo " MCC Parkour Jump Test Suite"
echo "========================================"

# Flat gap tests (same Y level)
run_test "Gap 1 flat"    100 100 100  102 100 100
run_test "Gap 2 flat"    100 100 102  103 100 102
run_test "Gap 3 flat"    100 100 104  104 100 104
run_test "Gap 4 flat"    100 100 106  105 100 106

# Ascend tests (+1Y)
run_test "Gap 1 up +1"   100 100 108  102 101 108
run_test "Gap 2 up +1"   100 100 110  103 101 110

# Descend tests (-1Y)
run_test "Gap 1 down -1"  100 100 112  102 99 112
run_test "Gap 2 down -1"  100 100 114  103 99 114

# Descend tests (-2Y)
run_test "Gap 1 down -2"  100 100 94   102 98 94
run_test "Gap 2 down -2"  100 100 92   103 98 92

echo ""
echo "========================================"
echo " SUMMARY"
echo "========================================"
echo -e "$RESULTS"
