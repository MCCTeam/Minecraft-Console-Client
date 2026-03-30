#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"

sed_in_place() {
    if [[ "$(uname)" == "Darwin" ]]; then
        sed -i '' "$@"
    else
        sed -i "$@"
    fi
}

VERSION="${1:-1.21.11-Vanilla}"
SERVER_DIR="${MCC_SERVERS:?}/$VERSION"
PROPS_FILE="$SERVER_DIR/server.properties"
SESSION_NAME="mc-${VERSION//./_}"

if [[ ! -d "$SERVER_DIR" ]]; then
    echo "Server directory not found: $SERVER_DIR" >&2
    exit 1
fi

if [[ ! -f "$SERVER_DIR/eula.txt" ]] || ! grep -Eq '^eula=true$' "$SERVER_DIR/eula.txt"; then
    echo "Missing accepted EULA in $SERVER_DIR/eula.txt" >&2
    exit 1
fi

server_running() {
    mc-list | grep -Fq "$SESSION_NAME"
}

wait_for_server_ready() {
    local timeout="${1:-60}"
    local elapsed=0
    while (( elapsed < timeout )); do
        if mc-log "$VERSION" 200 2>/dev/null | grep -Fq "Done ("; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done
    echo "Timed out waiting for $VERSION to become ready" >&2
    return 1
}

wait_for_server_stop() {
    local timeout="${1:-60}"
    local elapsed=0
    while (( elapsed < timeout )); do
        if ! server_running; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    # Legacy servers can leave the tmux session around after stdin stop.
    # Fall back to force-killing the session so the harness can continue.
    mc-kill "$VERSION" >/dev/null 2>&1 || true

    if ! server_running; then
        return 0
    fi

    echo "Timed out waiting for $VERSION to stop" >&2
    return 1
}

upsert_property() {
    local key="$1"
    local value="$2"

    if grep -Eq "^${key}=" "$PROPS_FILE"; then
        sed_in_place "s#^${key}=.*#${key}=${value}#" "$PROPS_FILE"
    else
        printf '%s=%s\n' "$key" "$value" >> "$PROPS_FILE"
    fi
}

if [[ ! -f "$PROPS_FILE" ]]; then
    mc-start "$VERSION"
    wait_for_server_ready
    mc-stop "$VERSION"
    wait_for_server_stop
fi

if server_running; then
    mc-stop "$VERSION"
    wait_for_server_stop
fi

upsert_property "online-mode" "false"
upsert_property "enforce-secure-profile" "false"
upsert_property "enable-rcon" "true"
upsert_property "rcon.port" "25575"
upsert_property "rcon.password" "test123"

echo "Configured $VERSION for persistent offline testing"
