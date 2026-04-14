#!/usr/bin/env bash
# Automated parkour jump test for MCC pathfinding
# Usage: source tools/mcc-env.sh && bash tools/test-parkour.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"
source "$REPO_ROOT/tools/pathing_live_common.sh"

MANIFEST="$REPO_ROOT/tools/pathing_data/canonical-live-cases.json"
RESULTS_FILE="${RESULTS_FILE:-/tmp/mcc-debug/pathing-live-results.jsonl}"
LOG="/tmp/mcc-debug/mcc-debug.log"
RESULTS=""
TEST_NUM=0
LAST_RESULT="invalid_live_case"

if [[ "${1:-}" == "--list-cases" ]]; then
    manifest_cases_for_query "$MANIFEST" "linear"
    exit 0
fi

mkdir -p "$(dirname "$RESULTS_FILE")"
: > "$RESULTS_FILE"

run_manifest_case() {
    local case_id="$1"
    local case_json
    case_json="$(manifest_case_json "$MANIFEST" "$case_id")"

    read -r world_recipe start_x start_y start_z goal_x goal_y goal_z < <(
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
)
PY
    )

    local landing_block_y=$(( ${goal_y%.*} - 1 ))

    case "$world_recipe" in
        linear-flat|linear-ascend|linear-descend)
            mc-rcon "fill 95 80 95 115 90 105 air" >/dev/null
            mc-rcon "fill 95 79 95 115 79 105 air" >/dev/null
            mc-rcon "setblock 100 79 100 stone" >/dev/null
            mc-rcon "setblock ${goal_x%.*} ${landing_block_y} ${goal_z%.*} stone" >/dev/null
            ;;
        *)
            echo "Unsupported world recipe for test-parkour.sh: $world_recipe" >&2
            return 1
            ;;
    esac

    run_test "$case_id" "${start_x%.*}" "${start_y%.*}" "${start_z%.*}" "${goal_x%.*}" "${goal_y%.*}" "${goal_z%.*}"
    record_live_result "$RESULTS_FILE" "$case_json" "$LAST_RESULT" "$LOG"
}

echo "========================================"
echo " MCC Parkour Jump Test Suite"
echo "========================================"

while IFS= read -r case_id; do
    run_manifest_case "$case_id"
done < <(manifest_cases_for_query "$MANIFEST" "linear")

echo ""
echo "========================================"
echo " SUMMARY"
echo "========================================"
echo -e "$RESULTS"
