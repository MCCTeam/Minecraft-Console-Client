#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=tools/mcc-env.sh
source "$REPO_ROOT/tools/mcc-env.sh"
# shellcheck source=.skills/mcc-integration-testing/scripts/common.sh
source "$SCRIPT_DIR/common.sh"

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
    wait_for_server_ready "$VERSION"
    mc-stop "$VERSION"
    wait_for_server_stop "$VERSION"
fi

if server_running; then
    mc-stop "$VERSION"
    wait_for_server_stop "$VERSION"
fi

upsert_property "online-mode" "false"
upsert_property "enforce-secure-profile" "false"
upsert_property "enable-rcon" "true"
upsert_property "rcon.port" "25575"
upsert_property "rcon.password" "test123"

echo "Configured $VERSION for persistent offline testing"
