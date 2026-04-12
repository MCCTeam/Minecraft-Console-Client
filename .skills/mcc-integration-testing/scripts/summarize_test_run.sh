#!/usr/bin/env zsh
set -euo pipefail

RUN_ROOT="${TMPDIR:-/tmp}/mcc-integration-testing"
RUN_DIR="${1:-$(find "$RUN_ROOT" -mindepth 1 -maxdepth 1 -type d | sort | tail -n 1)}"

if [[ -z "${RUN_DIR:-}" ]] || [[ ! -d "$RUN_DIR" ]]; then
    echo "Run directory not found" >&2
    exit 1
fi

MCC_LOG="$RUN_DIR/mcc.log"
SERVER_LOG="$RUN_DIR/server-latest.log"
BUILD_LOG="$RUN_DIR/build.log"

echo "Run directory: $RUN_DIR"
echo
echo "Build result:"
grep -E "Warning\(s\)|Error\(s\)|Time Elapsed" "$BUILD_LOG" || true
echo
echo "MCC highlights:"
grep -E "Server was successfully joined|FileInput|smoke_test_from_mcc_full_spectrum|There are [0-9]+ of a max|health|Creative" "$MCC_LOG" || true
echo
echo "Server highlights:"
grep -E "joined the game|Made .* a server operator|game mode|smoke_test_from_mcc_full_spectrum|summon|particle|playsound|tnt" "$SERVER_LOG" || true
