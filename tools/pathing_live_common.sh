#!/usr/bin/env bash

mcc_cmd_live() {
    if [[ -n "${SESSION:-}" ]]; then
        mcc-cmd --session "$SESSION" "$1"
    else
        mcc-cmd "$1"
    fi
}

manifest_cases_for_query() {
    local manifest_path="$1"
    local family_csv="$2"

    python3 - "$manifest_path" "$family_csv" <<'PY'
import json
import sys

with open(sys.argv[1], "r", encoding="utf-8") as handle:
    manifest = json.load(handle)

families = {item for item in sys.argv[2].split(",") if item}
for row in manifest:
    if row["family"] in families and row["movement_mode"] == "sprint" and row["momentum_ticks"] == 12:
        print(row["case_id"])
PY
}

manifest_case_json() {
    local manifest_path="$1"
    local case_id="$2"

    python3 - "$manifest_path" "$case_id" <<'PY'
import json
import sys

with open(sys.argv[1], "r", encoding="utf-8") as handle:
    manifest = json.load(handle)

case_id = sys.argv[2]
row = next(row for row in manifest if row["case_id"] == case_id)
print(json.dumps(row))
PY
}

record_live_result() {
    local results_path="$1"
    local case_json="$2"
    local live_result="$3"
    local log_path="$4"

    python3 - "$results_path" "$case_json" "$live_result" "$log_path" <<'PY'
import json
import sys

row = json.loads(sys.argv[2])
record = {
    "case_id": row["case_id"],
    "bucket_id": row["bucket_id"],
    "world_recipe_id": row["world_recipe_id"],
    "expected_result": row["expected_result"],
    "live_result": sys.argv[3],
    "log_path": sys.argv[4],
}

with open(sys.argv[1], "a", encoding="utf-8") as handle:
    handle.write(json.dumps(record) + "\n")
PY
}

run_test() {
    local name="$1"
    local start_x="$2" start_y="$3" start_z="$4"
    local dest_x="$5" dest_y="$6" dest_z="$7"
    local username="${USERNAME:-MCCBot}"

    TEST_NUM=$((TEST_NUM + 1))
    echo ""
    echo "=== TEST $TEST_NUM: $name ==="
    echo "  Start: ($start_x, $start_y, $start_z) -> Dest: ($dest_x, $dest_y, $dest_z)"

    mcc_cmd_live "respawn" 2>/dev/null || true
    sleep 0.5
    mc-rcon "gamemode creative $username" >/dev/null 2>&1
    sleep 0.3
    mc-rcon "tp $username ${start_x}.5 ${start_y} ${start_z}.5" >/dev/null 2>&1
    sleep 2
    mc-rcon "gamemode survival $username" >/dev/null 2>&1
    sleep 1

    : > "$LOG"
    sleep 0.5

    mcc_cmd_live "pathfind $dest_x $dest_y $dest_z"
    sleep 8

    local a_star_result
    a_star_result=$(grep -a '\[A\*\]' "$LOG" | head -3 | sed 's/\x1b\[[0-9;]*m//g' || true)

    local path_exec
    path_exec=$(grep -a '\[PathExec\]' "$LOG" | sed 's/\x1b\[[0-9;]*m//g' || true)

    local path_mgr
    path_mgr=$(grep -a '\[PathMgr\]' "$LOG" | sed 's/\x1b\[[0-9;]*m//g' || true)

    local nav_segs
    nav_segs=$(grep -a '\[Navigate\].*seg' "$LOG" | sed 's/\x1b\[[0-9;]*m//g' || true)

    local physics_line
    physics_line=$(grep -a '\[Physics\]' "$LOG" | tail -1 | sed 's/\x1b\[[0-9;]*m//g' || true)

    local result="invalid_live_case"
    if echo "$path_mgr" | grep -q "complete"; then
        result="pass"
    elif echo "$a_star_result" | grep -q "Failed"; then
        result="reject"
    elif echo "$path_mgr" | grep -q "Replan failed\|Giving up"; then
        result="fail"
    elif echo "$path_exec" | grep -q "FAILED"; then
        result="fail"
    fi
    LAST_RESULT="$result"

    echo "  A*: $a_star_result"
    echo "  Segments: $nav_segs"
    echo "  Exec: $(echo "$path_exec" | tail -3)"
    echo "  Manager: $(echo "$path_mgr" | tail -2)"
    echo "  Physics: $physics_line"
    echo "  RESULT: $LAST_RESULT"

    RESULTS="${RESULTS}TEST $TEST_NUM ($name): $LAST_RESULT\n"
}
