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
Usage: reset_shared_test_state.sh [--all | <server-dir>...]

Kills shared server tmux test sessions and removes stale server stdin pipes.
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    usage
    exit 0
fi

kill_named_session() {
    local session_name="$1"
    tmux kill-session -t "$session_name" 2>/dev/null || true
}

if [[ $# -eq 0 || "${1:-}" == "--all" ]]; then
    while IFS= read -r session_name; do
        [[ -z "$session_name" ]] && continue
        kill_named_session "$session_name"
    done < <(tmux list-sessions 2>/dev/null | awk -F: '/^mc-/{print $1}' || true)

    while IFS= read -r pipe_path; do
        [[ -z "$pipe_path" ]] && continue
        if [[ ! -p "$pipe_path" ]]; then
            rm -f "$pipe_path"
        fi
    done < <(find "$MCC_SERVERS" -maxdepth 2 -name 'stdin.pipe' 2>/dev/null || true)
else
    for version in "$@"; do
        kill_named_session "$(server_session_name "$version")"
        remove_stale_stdin_pipe "$version"
    done
fi
