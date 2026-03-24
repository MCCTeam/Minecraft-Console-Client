#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <server-dir>" >&2
    exit 1
fi

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
SERVER_DIR_NAME="$1"
SERVERS_ROOT="${MCC_SERVERS:-$REPO_ROOT/MinecraftOfficial/downloads}"
SERVER_DIR="$SERVERS_ROOT/$SERVER_DIR_NAME"
PROPS_FILE="$SERVER_DIR/server.properties"
LATEST_LOG="$SERVER_DIR/logs/latest.log"

if [[ -f "$PROPS_FILE" ]]; then
    PORT_LINE="$(grep -E '^server-port=' "$PROPS_FILE" | tail -n 1 || true)"
    if [[ -n "$PORT_LINE" ]]; then
        PORT="${PORT_LINE#server-port=}"
        if [[ "$PORT" =~ ^[0-9]+$ ]]; then
            printf '%s\n' "$PORT"
            exit 0
        fi
    fi
fi

if [[ -f "$LATEST_LOG" ]]; then
    PORT="$(sed -n 's/.*Starting Minecraft server on .*:\([0-9][0-9]*\).*/\1/p' "$LATEST_LOG" | tail -n 1)"
    if [[ -n "$PORT" ]]; then
        printf '%s\n' "$PORT"
        exit 0
    fi
fi

printf '25565\n'
