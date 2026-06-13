#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"
source "$REPO_ROOT/.skills/mcc-integration-testing/scripts/common.sh"

usage() {
    cat <<'EOF'
Usage: tools/run-dialog-test.sh <mc-version>

Integration test for MCC dialog system against a real local server.
Tests all 5 dialog types, button actions, cancel/dismiss, and body content.

Examples:
  tools/run-dialog-test.sh 26.1
  tools/run-dialog-test.sh 1.21.11
EOF
}

MC_VERSION="${1:-}"
if [[ -z "$MC_VERSION" ]]; then
    usage >&2
    exit 1
fi

SESSION_NAME="dialog-test-${MC_VERSION//[^a-zA-Z0-9]/_}"
TEST_ROOT="${TMPDIR:-/tmp}/mcc-dialog-test/${MC_VERSION//\//_}"
CFG="$TEST_ROOT/custom.ini"
MCC_LOG="$TEST_ROOT/mcc-output.log"
INPUT_FILE="$TEST_ROOT/mcc_input.txt"
SERVER_PORT="25565"
PASS=0
FAIL=0

cleanup() {
    tmux kill-session -t "$SESSION_NAME" 2>/dev/null || true
}
trap cleanup EXIT

header() {
    echo ""
    echo "===== $* ====="
}

# Strip ANSI escape codes for grep matching
ansi_strip() {
    sed 's/\x1b\[[0-9;]*[a-zA-Z]//g'
}

assert_log() {
    local label="$1"
    local pattern="$2"
    local timeout="${3:-5}"
    local elapsed=0
    while (( elapsed < timeout )); do
        if [[ -f "$MCC_LOG" ]] && ansi_strip < "$MCC_LOG" | grep -Fq "$pattern" 2>/dev/null; then
            echo "  PASS: $label"
            PASS=$((PASS + 1))
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done
    echo "  FAIL: $label (expected: '$pattern')"
    FAIL=$((FAIL + 1))
}

wait_for_pattern() {
    local file="$1"
    local pattern="$2"
    local timeout="${3:-60}"
    local elapsed=0
    while (( elapsed < timeout )); do
        if [[ -f "$file" ]] && ansi_strip < "$file" | grep -Fq "$pattern" 2>/dev/null; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done
    return 1
}

write_input() {
    echo "$1" >> "$INPUT_FILE"
    sleep 1
}

assert_dialog_shown() {
    assert_log "$1 received" "Server showed custom dialog: $2" 10
}

# ---- Setup ----

mkdir -p "$TEST_ROOT"
rm -f "$MCC_LOG" "$INPUT_FILE"

echo "[Dialog Test] Version: $MC_VERSION, Session: $SESSION_NAME"
echo "[Dialog Test] Log: $MCC_LOG"

# Ensure server is running
if ! server_running "$MC_VERSION"; then
    echo "[Setup] Starting server..."
    bash "$REPO_ROOT/tools/start-server.sh" "$MC_VERSION" 2>&1 | tail -1
    wait_for_server_ready "$MC_VERSION" 120
fi

echo "[Setup] Server ready."

# Prepare temp config
echo "[Setup] Preparing MCC config..."
bash "$REPO_ROOT/.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh" \
    "$CFG" "$MC_VERSION" "MCCBot" >/dev/null

# Disable packet debug (fix in-place to avoid dup sections)
sed_in_place \
    -e '/^\[Debug\]/,/^\s*$/d' \
    "$CFG"

cat >> "$CFG" <<TOML

[Debug]
DebugMessages = false
PacketDebugMessages = false
TOML

# Launch MCC with MCC_FILE_INPUT=1 for maximum compatibility
echo "ping" > "$INPUT_FILE"
tmux kill-session -t "$SESSION_NAME" 2>/dev/null || true
sleep 1

cd "$REPO_ROOT"
# FileInputBot ignores config and uses MCC_INPUT_FILE env var only
INPUT_FILE_ABS="$(realpath "$INPUT_FILE")"
tmux new-session -d -s "$SESSION_NAME" \
    "bash -c 'export MCC_FILE_INPUT=1; export MCC_INPUT_FILE=\"$INPUT_FILE_ABS\"; exec dotnet run --no-build --project MinecraftClient -c Release -- \"$CFG\" \"MCCBot\" \"-\" \"localhost:$SERVER_PORT\"' > '$MCC_LOG' 2>&1"

echo "[Setup] Waiting for MCC to join..."
if ! wait_for_pattern "$MCC_LOG" "Server was successfully joined" 90; then
    echo "ERROR: MCC did not join the server. Check $MCC_LOG"
    tail -10 "$MCC_LOG" | ansi_strip
    exit 1
fi
echo "[Setup] MCC joined."
sleep 2

# ---- Tests ----

header "1. Notice Dialog"
mc-rcon 'dialog show MCCBot {type:"minecraft:notice", title:{text:"Notice Title"}}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "notice dialog" "Notice Title"
write_input "dialog show"
assert_log "notice type" "Type: Notice" 10
assert_log "notice OK button" "OK (close)" 3

header "2. Confirmation Dialog"
mc-rcon 'dialog show MCCBot {type:"minecraft:confirmation", title:{text:"Confirm?"}, yes:{label:{text:"Yes"}}, no:{label:{text:"No"}}}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "confirmation" "Confirm?"
write_input "dialog show"
assert_log "confirmation type" "Type: Confirmation" 10
assert_log "yes button" "Yes (close)" 3
assert_log "no button" "No (close)" 3

header "3. Multi-Action Dialog"
mc-rcon 'dialog show MCCBot {type:"minecraft:multi_action", title:{text:"Choose"}, actions:[{label:{text:"Alpha"}}, {label:{text:"Beta"}}, {label:{text:"Gamma"}}]}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "multi_action" "Choose"
write_input "dialog show"
assert_log "multi_action type" "Type: Multi-action" 10
assert_log "multi_action button 1" "Alpha (close)" 3
assert_log "multi_action button 2" "Beta (close)" 3
assert_log "multi_action button 3" "Gamma (close)" 3

header "4. Dialog-List Dialog"
mc-rcon 'dialog show MCCBot {type:"minecraft:dialog_list", title:{text:"List"}, dialogs:[{type:"minecraft:notice", title:{text:"Sub One"}}, {type:"minecraft:notice", title:{text:"Sub Two"}}]}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "dialog_list" "List"
write_input "dialog show"
assert_log "dialog_list type" "Type: Dialog list" 10
assert_log "dialog_list sub 1" "Sub One (show dialog)" 3
assert_log "dialog_list sub 2" "Sub Two (show dialog)" 3

header "5. Server-Links Dialog"
mc-rcon 'dialog show MCCBot {type:"minecraft:server_links", title:{text:"Links"}}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "server_links" "Links"
write_input "dialog show"
assert_log "server_links type" "Type: Server links" 10

header "6. Body Content"
mc-rcon 'dialog show MCCBot {type:"minecraft:notice", title:{text:"With Body"}, body:[{type:"minecraft:plain_message", contents:{text:"Hello from body"}}]}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "body dialog" "With Body"
write_input "dialog show"
assert_log "body content" "Hello from body" 10

header "7. Custom run_command Action"
mc-rcon 'dialog show MCCBot {type:"minecraft:notice", title:{text:"Run Cmd"}, action:{label:{text:"/list"}, action:{type:"minecraft:run_command", command:"/list"}}}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "command action" "Run Cmd"
write_input "dialog show"
assert_log "command action button" "/list (command)" 10
write_input "dialog click 1"
assert_log "command executed" "There are " 10

header "8. show_dialog Action (nested)"
mc-rcon 'dialog show MCCBot {type:"minecraft:notice", title:{text:"First"}, action:{label:{text:"Next"}, action:{type:"minecraft:show_dialog", dialog:{type:"minecraft:notice", title:{text:"Second"}}}}}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "first dialog" "First"
write_input "dialog click 1"
assert_dialog_shown "nested dialog" "Second"

header "9. Dialog Cancel"
mc-rcon 'dialog show MCCBot {type:"minecraft:notice", title:{text:"Cancel Me"}}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "cancel test dialog" "Cancel Me"
write_input "dialog cancel"
assert_log "cancel closed dialog" "Dialog action closed locally" 10

header "10. Dialog Click-Label"
mc-rcon 'dialog show MCCBot {type:"minecraft:multi_action", title:{text:"Label Test"}, actions:[{label:{text:"Pick Me"}}, {label:{text:"Leave Me"}}]}' 2>&1 | ansi_strip | grep -v "^$"
assert_dialog_shown "click-label dialog" "Label Test"
write_input "dialog click-label Pick Me"
assert_log "click-label worked" "Dialog action closed locally" 10

# ---- Results ----

echo ""
echo "=========================================="
echo " Dialog Integration Test Results"
echo "=========================================="
echo " PASS: $PASS"
echo " FAIL: $FAIL"
echo "------------------------------------------"

if [[ $FAIL -gt 0 ]]; then
    echo "FAILURES DETECTED. Full log: $MCC_LOG"
    echo "Last 20 lines:"
    ansi_strip < "$MCC_LOG" | tail -20
    exit 1
else
    echo "ALL TESTS PASSED."
    exit 0
fi
