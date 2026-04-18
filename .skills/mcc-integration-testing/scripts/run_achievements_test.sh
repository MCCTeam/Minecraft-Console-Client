#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"
# shellcheck source=.skills/mcc-integration-testing/scripts/common.sh
source "$SCRIPT_DIR/common.sh"

usage() {
    cat <<'EOF'
Usage: run_achievements_test.sh [--no-build] <server-dir> <mc-version> <legacy|modern>

Examples:
  .skills/mcc-integration-testing/scripts/run_achievements_test.sh --no-build 1.8 1.8 legacy
  .skills/mcc-integration-testing/scripts/run_achievements_test.sh --no-build 1.21.11-Vanilla 1.21.11 modern
EOF
}

DO_BUILD=true

while [[ $# -gt 0 ]]; do
    case "$1" in
        --no-build) DO_BUILD=false; shift ;;
        --build) DO_BUILD=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) break ;;
    esac
done

if [[ $# -ne 3 ]]; then
    usage >&2
    exit 1
fi

SERVER_DIR="$1"
MC_VERSION="$2"
PROFILE="$3"
SESSION_NAME="achievements-${SERVER_DIR//[^a-zA-Z0-9]/_}-${PROFILE}"
TEST_USERNAME="$(_mcc_resolve_username "$SESSION_NAME")"

if [[ "$PROFILE" != "legacy" && "$PROFILE" != "modern" ]]; then
    echo "Unsupported profile: $PROFILE" >&2
    exit 1
fi

RUN_ROOT="${TMPDIR:-/tmp}/mcc-achievements"
RUN_ID="$(date +%Y%m%d-%H%M%S)"
RUN_DIR="$RUN_ROOT/$SERVER_DIR/$RUN_ID"
LATEST_LINK="$RUN_ROOT/$SERVER_DIR/latest"
MCC_LOG="$RUN_DIR/mcc.log"
BUILD_LOG="$RUN_DIR/build.log"
SERVER_TMUX_LOG="$RUN_DIR/server-tmux.log"
SERVER_FILE_LOG="$RUN_DIR/server-latest.log"
COMMAND_LOG="$RUN_DIR/commands.log"
SUMMARY_ENV="$RUN_DIR/summary.env"
PROBE_SCRIPT="$RUN_DIR/achievement_probe.cs"
CFG="$RUN_DIR/MinecraftClient.$MC_VERSION.ini"
INPUT_FILE="$(_mcc_session_input_file "$SESSION_NAME")"
SERVER_LOG_FILE="$MCC_SERVERS/$SERVER_DIR/logs/latest.log"
TARGET_ID="minecraft:story/root"
TARGET_COMMAND_GRANT="advancement grant $TEST_USERNAME only minecraft:story/root"
TARGET_COMMAND_REVOKE="advancement revoke $TEST_USERNAME only minecraft:story/root"
TARGET_TYPE="Modern 🌱"
PORT="unknown"
MCC_PID=""

INITIAL_STATUS="❌"
GRANT_STATUS="❌"
REVOKE_STATUS="❌"
API_STATUS="❌"
VERDICT="❌ Fail"
NOTE="Run did not complete."
EXECUTED="yes"

if [[ "$PROFILE" == "legacy" ]]; then
    TARGET_ID="achievement.openInventory"
    TARGET_COMMAND_GRANT="achievement give achievement.openInventory $TEST_USERNAME"
    TARGET_COMMAND_REVOKE="achievement take achievement.openInventory $TEST_USERNAME"
    TARGET_TYPE="Legacy 🧱"
fi

mkdir -p "$RUN_DIR"

write_summary() {
    {
        printf 'VERSION=%q\n' "$MC_VERSION"
        printf 'SERVER_DIR=%q\n' "$SERVER_DIR"
        printf 'PROFILE=%q\n' "$PROFILE"
        printf 'FAMILY=%q\n' "$TARGET_TYPE"
        printf 'PORT=%q\n' "$PORT"
        printf 'RUN_DIR=%q\n' "$RUN_DIR"
        printf 'MCC_LOG=%q\n' "$MCC_LOG"
        printf 'SERVER_LOG=%q\n' "$RUN_DIR/server-latest.log"
        printf 'SERVER_FILE_LOG=%q\n' "$SERVER_LOG_FILE"
        printf 'SERVER_TMUX_LOG=%q\n' "$SERVER_TMUX_LOG"
        printf 'COPIED_SERVER_LOG=%q\n' "$RUN_DIR/server-latest.log"
        printf 'COMMAND_LOG=%q\n' "$COMMAND_LOG"
        printf 'SUMMARY_ENV=%q\n' "$SUMMARY_ENV"
        printf 'TARGET_ID=%q\n' "$TARGET_ID"
        printf 'INITIAL_STATUS=%q\n' "$INITIAL_STATUS"
        printf 'GRANT_STATUS=%q\n' "$GRANT_STATUS"
        printf 'REVOKE_STATUS=%q\n' "$REVOKE_STATUS"
        printf 'API_STATUS=%q\n' "$API_STATUS"
        printf 'VERDICT=%q\n' "$VERDICT"
        printf 'NOTE=%q\n' "$NOTE"
        printf 'EXECUTED=%q\n' "$EXECUTED"
    } > "$SUMMARY_ENV"
}

capture_server_logs() {
    mc-log "$SERVER_DIR" 400 > "$SERVER_TMUX_LOG" 2>/dev/null || true
    if [[ -f "$SERVER_LOG_FILE" ]]; then
        cp "$SERVER_LOG_FILE" "$RUN_DIR/server-latest.log" 2>/dev/null || true
    fi
}

cleanup() {
    capture_server_logs

    if [[ -n "${MCC_PID:-}" ]] && kill -0 "$MCC_PID" 2>/dev/null; then
        echo "quit" >> "$INPUT_FILE" 2>/dev/null || true
        sleep 2
        kill "$MCC_PID" 2>/dev/null || true
        wait "$MCC_PID" 2>/dev/null || true
    fi

    ln -sfn "$RUN_DIR" "$LATEST_LINK"
    write_summary
}
trap cleanup EXIT

log_step() {
    printf '[%s] %s\n' "$(date '+%H:%M:%S')" "$1" | tee -a "$COMMAND_LOG"
}

fail() {
    NOTE="$1"
    VERDICT="❌ Fail"
    exit 1
}

wait_for_file_pattern() {
    local file="$1"
    local pattern="$2"
    local description="$3"
    local timeout="${4:-60}"
    local elapsed=0

    while (( elapsed < timeout )); do
        if [[ -f "$file" ]] && grep -Fq "$pattern" "$file"; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    echo "Timed out waiting for: $description" >&2
    return 1
}

write_probe_script() {
    cat > "$PROBE_SCRIPT" <<EOF
//MCCScript 1.0

MCC.LoadBot(new AchievementProbeBot());

//MCCScript Extensions

public class AchievementProbeBot : ChatBot
{
    private const string TargetId = "$TARGET_ID";

    public override void Initialize()
    {
        LogToConsole("[ACH_TEST] probe initialized");
        DumpState("initialize");
    }

    public override void AfterGameJoined()
    {
        LogToConsole("[ACH_TEST] after join");
        DumpState("after_join");
    }

    public override void OnAchievementUpdate(IReadOnlyList<Achievement> updated, IReadOnlyList<string> removedIds, bool reset)
    {
        LogToConsole($"[ACH_TEST] event reset={reset} updated={updated.Count} removed={removedIds.Count}");
        DumpState("event");
    }

    private void DumpState(string origin)
    {
        Achievement[] all = GetAchievements();
        Achievement[] unlocked = GetUnlockedAchievements();
        Achievement[] locked = GetLockedAchievements();
        Achievement? target = null;

        foreach (Achievement entry in all)
        {
            if (entry.Id == TargetId)
            {
                target = entry;
                break;
            }
        }

        string titleState = "missing";
        string completionState = "missing";

        if (target is not null)
        {
            titleState = target.Title is null ? "null" : "present";
            completionState = target.IsCompleted ? "done" : "todo";
        }

        LogToConsole($"[ACH_TEST] snapshot origin={origin} all={all.Length} unlocked={unlocked.Length} locked={locked.Length}");
        LogToConsole($"[ACH_TEST] target_state origin={origin} id={TargetId} title={titleState} completed={completionState}");
    }
}
EOF
}

run_server_command() {
    local cmd="$1"
    local attempt

    log_step "SERVER> $cmd"
    for attempt in 1 2 3 4 5; do
        if mc-rcon "$cmd" >/dev/null 2>&1; then
            sleep 1
            return 0
        fi
        sleep 1
    done

    fail "Server command failed: $cmd"
}

run_mcc_command() {
    local name="$1"
    local cmd="$2"
    local delay="${3:-2}"
    local start_line=0
    local end_line=0

    if [[ -f "$MCC_LOG" ]]; then
        start_line="$(wc -l < "$MCC_LOG")"
    fi

    log_step "MCC> $cmd"
    echo "$cmd" >> "$INPUT_FILE"
    sleep "$delay"

    if [[ -f "$MCC_LOG" ]]; then
        end_line="$(wc -l < "$MCC_LOG")"
    fi

    if (( end_line > start_line )); then
        sed -n "$((start_line + 1)),$((end_line))p" "$MCC_LOG" > "$RUN_DIR/$name.mcc.log"
    else
        : > "$RUN_DIR/$name.mcc.log"
    fi
}

assert_pattern() {
    local file="$1"
    local pattern="$2"
    local description="$3"

    grep -Fq "$pattern" "$file" || fail "$description"
}

if $DO_BUILD; then
    log_step "BUILD> dotnet build MinecraftClient.sln -c Release"
    mcc-build > "$BUILD_LOG" 2>&1 || fail "dotnet build failed."
else
    : > "$BUILD_LOG"
fi

bash "$SCRIPT_DIR/preflight_test_env.sh" "$SERVER_DIR" >/dev/null || fail "Test environment preflight failed."
bash "$SCRIPT_DIR/reset_shared_test_state.sh" "$SERVER_DIR" >/dev/null || fail "Failed to reset shared test state."

if [[ ! -d "$MCC_SERVERS/$SERVER_DIR" ]]; then
    fail "Server directory not found: $MCC_SERVERS/$SERVER_DIR"
fi

bash "$SCRIPT_DIR/prepare_offline_mcc_config.sh" "$CFG" "$MC_VERSION" "$TEST_USERNAME" >/dev/null || fail "Failed to prepare temporary MCC config."
PORT="$(bash "$SCRIPT_DIR/get_server_port.sh" "$SERVER_DIR")"

"$SCRIPT_DIR/ensure_offline_server.sh" "$SERVER_DIR"
write_probe_script

if [[ "$PROFILE" == "legacy" && -f "$MCC_SERVERS/$SERVER_DIR/server.properties" ]]; then
    sed_in_place 's/^use-native-transport=.*/use-native-transport=false/' "$MCC_SERVERS/$SERVER_DIR/server.properties"
fi

mkdir -p "$(dirname "$INPUT_FILE")"
: > "$INPUT_FILE"
rm -f "$MCC_LOG"

log_step "Starting server $SERVER_DIR on port $PORT"
mc-start "$SERVER_DIR" >/dev/null
wait_for_server_ready "$SERVER_DIR" || fail "Server did not become ready."

log_step "Starting MCC for $MC_VERSION"
(
    cd "$REPO_ROOT"
    MCC_FILE_INPUT=1 MCC_INPUT_FILE="$INPUT_FILE" dotnet run --project MinecraftClient -c Release --no-build -- \
        "$CFG" \
        "$TEST_USERNAME" \
        - \
        "localhost:$PORT" \
        "--accounttype=mojang" \
        "--minecraftversion=$MC_VERSION" \
        "--terrainandmovements=true" \
        "--inventoryhandling=true" \
        "--entityhandling=true" \
        "--autorespawn=true" \
        "--debugmessages=true" \
        > "$MCC_LOG" 2>&1
) &
MCC_PID=$!

wait_for_file_pattern "$MCC_LOG" "Server was successfully joined." "MCC join success" 90 || fail "MCC failed to join."
wait_for_file_pattern "$SERVER_LOG_FILE" "$TEST_USERNAME joined the game" "server join entry" 30 || fail "Server never logged the join."

run_server_command "op $TEST_USERNAME"
run_server_command "gamerule sendCommandFeedback true"
if [[ "$PROFILE" == "modern" ]]; then
    run_server_command "gamerule logAdminCommands true"
fi
run_server_command "time set day"
run_server_command "weather clear"

run_mcc_command "load_probe" "script $PROBE_SCRIPT" 3
wait_for_file_pattern "$MCC_LOG" "[ACH_TEST] probe initialized" "probe startup" 30 || fail "Probe script did not initialize."

run_mcc_command "baseline_debug" "debug state" 2
run_mcc_command "baseline_all" "achievement" 2
run_mcc_command "baseline_locked" "achievement locked" 2
run_mcc_command "baseline_unlocked" "achievement unlocked" 2

run_server_command "$TARGET_COMMAND_GRANT"
sleep 3
run_mcc_command "after_grant_all" "achievement" 2
run_mcc_command "after_grant_unlocked" "achievement unlocked" 2

run_server_command "$TARGET_COMMAND_REVOKE"
sleep 3
run_mcc_command "after_revoke_all" "achievement" 2
run_mcc_command "after_revoke_locked" "achievement locked" 2

assert_pattern "$MCC_LOG" "Achievements/Advancements:" "Achievement command header never appeared."

if ! grep -Fq "No achievements/advancements received yet." "$RUN_DIR/baseline_all.mcc.log"; then
    INITIAL_STATUS="✅"
fi

if grep -Fq "$TARGET_ID" "$RUN_DIR/after_grant_unlocked.mcc.log" && grep -Fq "[DONE]" "$RUN_DIR/after_grant_unlocked.mcc.log"; then
    GRANT_STATUS="✅"
fi

if [[ "$PROFILE" == "legacy" ]]; then
    if grep -Fq "$TARGET_ID" "$RUN_DIR/after_revoke_locked.mcc.log" && grep -Fq "[TODO]" "$RUN_DIR/after_revoke_locked.mcc.log"; then
        REVOKE_STATUS="✅"
    fi
else
    if grep -Fq "$TARGET_ID" "$RUN_DIR/after_revoke_locked.mcc.log" && grep -Fq "[TODO]" "$RUN_DIR/after_revoke_locked.mcc.log"; then
        REVOKE_STATUS="✅"
    elif [[ "$GRANT_STATUS" == "✅" ]] && ! grep -Fq "$TARGET_ID" "$RUN_DIR/after_revoke_all.mcc.log"; then
        REVOKE_STATUS="✅"
    fi
fi

if grep -Fq "[ACH_TEST] event" "$MCC_LOG" && grep -Fq "target_state origin=event id=$TARGET_ID title=" "$MCC_LOG"; then
    API_STATUS="✅"
fi

case "$INITIAL_STATUS|$GRANT_STATUS|$REVOKE_STATUS|$API_STATUS" in
    "✅|✅|✅|✅")
        VERDICT="✅ Pass"
        NOTE="All planned achievement checks passed."
        ;;
    *"✅"*)
        VERDICT="⚠️ Partial"
        NOTE="At least one achievement phase passed, but the matrix did not fully clear."
        ;;
    *)
        VERDICT="❌ Fail"
        NOTE="Achievement checks did not produce the expected evidence."
        ;;
esac

run_mcc_command "quit" "quit" 2
NOTE="$NOTE Artifacts saved in $RUN_DIR."
