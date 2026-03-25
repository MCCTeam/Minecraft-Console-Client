#!/usr/bin/env bash
# Tail MCC and/or server logs side-by-side or individually.
# Usage:
#   tools/mcc-log-tail.sh                 # tail MCC log only
#   tools/mcc-log-tail.sh --server VER    # tail both MCC and server logs
#   tools/mcc-log-tail.sh --server-only VER  # tail server log only
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

MCC_LOG="${TMPDIR:-/tmp}/mcc-debug/mcc-debug.log"
SERVER_VER=""
SERVER_ONLY=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --server)      SERVER_VER="$2"; shift 2 ;;
        --server-only) SERVER_ONLY=true; SERVER_VER="$2"; shift 2 ;;
        -h|--help)
            echo "Usage: tools/mcc-log-tail.sh [--server VER] [--server-only VER]"
            exit 0 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

if $SERVER_ONLY; then
    if [[ -z "$SERVER_VER" ]]; then
        echo "Specify server version with --server-only VER" >&2
        exit 1
    fi
    SERVER_LOG="$MCC_SERVERS/$SERVER_VER/logs/latest.log"
    echo "=== Tailing server log: $SERVER_LOG ==="
    exec tail -f "$SERVER_LOG"
fi

if [[ -n "$SERVER_VER" ]]; then
    SERVER_LOG="$MCC_SERVERS/$SERVER_VER/logs/latest.log"
    echo "=== Tailing MCC + server logs ==="
    echo "  MCC:    $MCC_LOG"
    echo "  Server: $SERVER_LOG"
    echo ""
    tail -f "$MCC_LOG" "$SERVER_LOG" 2>/dev/null
else
    if [[ ! -f "$MCC_LOG" ]]; then
        echo "No MCC log found at $MCC_LOG"
        echo "Start MCC first with: tools/mcc-debug.sh --file-input"
        exit 1
    fi
    echo "=== Tailing MCC log: $MCC_LOG ==="
    exec tail -f "$MCC_LOG"
fi
