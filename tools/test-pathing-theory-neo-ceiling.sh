#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"
source "$REPO_ROOT/tools/pathing_live_common.sh"

SESSION="${SESSION:-$(_mcc_resolve_session)}"
USERNAME="${USERNAME:-MCCBot}"
MANIFEST="$REPO_ROOT/tools/pathing_data/canonical-live-cases.json"
RESULTS_FILE="${RESULTS_FILE:-$(_mcc_session_root "$SESSION")/pathing-live-results.jsonl}"
LOG="$(_mcc_session_log_file "$SESSION")"
RESULTS=""
TEST_NUM=0
LAST_RESULT="invalid_live_case"

if [[ "${1:-}" == "--list-cases" ]]; then
    manifest_cases_for_query "$MANIFEST" "neo,ceiling"
    exit 0
fi

mkdir -p "$(dirname "$RESULTS_FILE")"
: > "$RESULTS_FILE"

setup_neo_wall() {
    local wall_width="$1"
    local goal_z="$2"
    mc-rcon "fill 95 79 95 115 90 115 air" >/dev/null
    mc-rcon "setblock 100 79 100 stone" >/dev/null
    mc-rcon "fill 101 79 100 101 79 $((99 + wall_width)) stone" >/dev/null
    mc-rcon "setblock 102 79 ${goal_z} stone" >/dev/null
}

setup_ceiling_headhitter() {
    local goal_x="$1"
    local ceiling_y="$2"
    mc-rcon "fill 95 79 95 115 90 105 air" >/dev/null
    mc-rcon "setblock 100 79 100 stone" >/dev/null
    mc-rcon "setblock ${goal_x} 79 100 stone" >/dev/null
    mc-rcon "fill 100 ${ceiling_y} 100 ${goal_x} ${ceiling_y} 100 stone" >/dev/null
}

run_manifest_case() {
    local case_id="$1"
    local case_json
    case_json="$(manifest_case_json "$MANIFEST" "$case_id")"

    read -r world_recipe start_x start_y start_z goal_x goal_y goal_z ceiling_height wall_width < <(
        python3 - "$case_json" <<'PY'
import json
import sys

row = json.loads(sys.argv[1])
print(
    row["world_recipe_id"],
    row["start"]["x"],
    row["start"]["y"],
    row["start"]["z"],
    row["goal"]["x"],
    row["goal"]["y"],
    row["goal"]["z"],
    row.get("ceiling_height", "null"),
    row.get("wall_width", "null"),
)
PY
    )

    case "$world_recipe" in
        neo-wall)
            setup_neo_wall "${wall_width%.*}" "${goal_z%.*}"
            ;;
        ceiling-headhitter)
            setup_ceiling_headhitter "${goal_x%.*}" "${ceiling_height%.*}"
            ;;
        *)
            echo "Unsupported world recipe for theory neo/ceiling suite: $world_recipe" >&2
            return 1
            ;;
    esac

    run_test "$case_id" "${start_x%.*}" "${start_y%.*}" "${start_z%.*}" "${goal_x%.*}" "${goal_y%.*}" "${goal_z%.*}"
    record_live_result "$RESULTS_FILE" "$case_json" "$LAST_RESULT" "$LOG"
}

echo "========================================"
echo " Theory-Aligned Neo And Ceiling Suite"
echo "========================================"

while IFS= read -r case_id; do
    run_manifest_case "$case_id"
done < <(manifest_cases_for_query "$MANIFEST" "neo,ceiling")

echo ""
echo "========================================"
echo " SUMMARY"
echo "========================================"
echo -e "$RESULTS"
