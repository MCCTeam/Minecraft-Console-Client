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
Usage: preflight_test_env.sh [server-dir...]

Checks the local MCC test environment and resolves common Java path issues.
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    usage
    exit 0
fi

ensure_java_in_path
command -v tmux >/dev/null 2>&1 || { echo "tmux was not found on PATH." >&2; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo "dotnet was not found on PATH." >&2; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo "python3 was not found on PATH." >&2; exit 1; }

if [[ ! -d "$MCC_SERVERS" ]]; then
    echo "Server root not found: $MCC_SERVERS" >&2
    exit 1
fi

for server_dir in "$@"; do
    [[ -z "$server_dir" ]] && continue
    if [[ ! -d "$MCC_SERVERS/$server_dir" ]]; then
        echo "Server directory not found: $MCC_SERVERS/$server_dir" >&2
        exit 1
    fi

    remove_stale_stdin_pipe "$server_dir"
done

printf 'MCC_REPO=%s\n' "$MCC_REPO"
printf 'MCC_SERVERS=%s\n' "$MCC_SERVERS"
printf 'JAVA=%s\n' "$(command -v java)"
printf 'TMUX=%s\n' "$(command -v tmux)"
printf 'DOTNET=%s\n' "$(command -v dotnet)"
