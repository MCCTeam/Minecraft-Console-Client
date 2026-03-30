#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

RUN_ROOT="${TMPDIR:-/tmp}/mcc-achievements/matrix"
RUN_ID="$(date +%Y%m%d-%H%M%S)"
MATRIX_DIR="$RUN_ROOT/$RUN_ID"
RESULTS_TSV="$MATRIX_DIR/results.tsv"
BUILD_LOG="$MATRIX_DIR/build.log"
REPORT_MD="$MATRIX_DIR/report.md"
PRECHECK_TXT="$MATRIX_DIR/preflight.txt"

mkdir -p "$MATRIX_DIR"

write_row() {
    local fields=("$@")

    while (( ${#fields[@]} < 14 )); do
        fields+=("")
    done

    printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
        "${fields[0]}" "${fields[1]}" "${fields[2]}" "${fields[3]}" "${fields[4]}" "${fields[5]}" "${fields[6]}" \
        "${fields[7]}" "${fields[8]}" "${fields[9]}" "${fields[10]}" "${fields[11]}" "${fields[12]}" \
        "${fields[13]}" >> "$RESULTS_TSV"
}

resolve_server_dir() {
    local version="$1"
    local candidate

    for candidate in "$version" "$version-Vanilla"; do
        if [[ -d "$MCC_SERVERS/$candidate" ]]; then
            printf '%s\n' "$candidate"
            return 0
        fi
    done

    return 1
}

run_version() {
    local version="$1"
    local profile="$2"
    local family="$3"
    local server_dir="$4"
    local summary_env

    if bash "$SCRIPT_DIR/run_achievements_test.sh" --no-build "$server_dir" "$version" "$profile"; then
        :
    fi

    summary_env="${TMPDIR:-/tmp}/mcc-achievements/$server_dir/latest/summary.env"
    if [[ ! -f "$summary_env" ]]; then
        write_row "$version" "$server_dir" "unknown" "$family" "❌" "❌" "❌" "❌" "❌ Fail" \
            "Summary file was not produced." "" "" "" 
        return
    fi

    # shellcheck disable=SC1090
    source "$summary_env"

    write_row "$VERSION" "$SERVER_DIR" "$PORT" "$FAMILY" "$INITIAL_STATUS" "$GRANT_STATUS" "$REVOKE_STATUS" \
        "$API_STATUS" "$VERDICT" "$NOTE" "$RUN_DIR" "$MCC_LOG" "$COPIED_SERVER_LOG" "$COMMAND_LOG"
}

{
    printf 'MCC_SERVERS=%s\n' "$MCC_SERVERS"
    printf 'RUN_DIR=%s\n' "$MATRIX_DIR"
    printf 'DATE=%s\n' "$(date -u '+%Y-%m-%d %H:%M:%S UTC')"
} > "$PRECHECK_TXT"

printf 'Version\tServerDir\tPort\tFamily\tInitial\tGrant\tRevoke\tAPI\tVerdict\tNote\tRunDir\tMccLog\tServerLog\tCommandLog\n' > "$RESULTS_TSV"

JAVA_OK="yes"
TMUX_OK="yes"
DOTNET_OK="yes"
BUILD_OK="yes"

if ! command -v dotnet >/dev/null 2>&1; then
    DOTNET_OK="no"
fi

if ! command -v java >/dev/null 2>&1 || ! java -version >/dev/null 2>&1; then
    JAVA_OK="no"
fi

if ! command -v tmux >/dev/null 2>&1; then
    TMUX_OK="no"
fi

if [[ "$DOTNET_OK" == "yes" ]]; then
    if ! dotnet build "$REPO_ROOT/MinecraftClient.sln" -c Release > "$BUILD_LOG" 2>&1; then
        BUILD_OK="no"
    fi
else
    : > "$BUILD_LOG"
fi

{
    printf 'MCC_SERVERS=%s\n' "$MCC_SERVERS"
    printf 'RUN_DIR=%s\n' "$MATRIX_DIR"
    printf 'DATE=%s\n' "$(date -u '+%Y-%m-%d %H:%M:%S UTC')"
    printf 'dotnet=%s\n' "$DOTNET_OK"
    printf 'java=%s\n' "$JAVA_OK"
    printf 'tmux=%s\n' "$TMUX_OK"
    printf 'build=%s\n' "$BUILD_OK"
} > "$PRECHECK_TXT"

while IFS='|' read -r version profile family; do
    [[ -z "$version" ]] && continue

    if [[ "$DOTNET_OK" != "yes" ]]; then
        write_row "$version" "" "" "$family" "❌" "❌" "❌" "❌" "❌ Fail" \
            "dotnet is not available on PATH."
        continue
    fi

    if [[ "$BUILD_OK" != "yes" ]]; then
        write_row "$version" "" "" "$family" "❌" "❌" "❌" "❌" "❌ Fail" \
            "dotnet build failed. See $BUILD_LOG."
        continue
    fi

    if [[ "$JAVA_OK" != "yes" || "$TMUX_OK" != "yes" ]]; then
        write_row "$version" "" "" "$family" "❌" "❌" "❌" "❌" "❌ Fail" \
            "java or tmux is not available, so live server execution was blocked."
        continue
    fi

    if ! server_dir="$(resolve_server_dir "$version")"; then
        write_row "$version" "" "" "$family" "❌" "❌" "❌" "❌" "⚠️ Partial" \
            "Server directory for $version was not found under $MCC_SERVERS."
        continue
    fi

    run_version "$version" "$profile" "$family" "$server_dir"
done <<'EOF'
1.8|legacy|Legacy 🧱
1.11.2|legacy|Legacy 🧱
1.12.2|modern|First advancements 🌱
1.19.4|modern|Stable modern ✅
1.20|modern|Telemetry edge 1 ⚠️
1.20.2|modern|Telemetry edge 2 ⚠️
1.20.4|modern|End of 1.20.x ⚠️
1.20.6|modern|Post-1.20.6 🔧
1.21.2|modern|1.21.2 family 🔧
1.21.11|modern|showAdvancements 🆕
26.1|modern|Latest supported 🚀
EOF

bash "$SCRIPT_DIR/summarize_achievements_matrix.sh" "$MATRIX_DIR" > "$REPORT_MD"
printf '%s\n' "$MATRIX_DIR"
