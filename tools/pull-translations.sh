#!/bin/bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONFIG_FILE="$REPO_ROOT/crowdin.yml"
TMP_CONFIG=""

if [[ -z "${CROWDIN_PERSONAL_TOKEN:-}" && -n "${CROWDIN_TOKEN:-}" ]]; then
    export CROWDIN_PERSONAL_TOKEN="$CROWDIN_TOKEN"
fi

if [[ ! -f "$CONFIG_FILE" ]]; then
    echo "Error: crowdin.yml not found at $CONFIG_FILE" >&2
    exit 1
fi

if [[ -z "${CROWDIN_PROJECT_ID:-}" ]]; then
    echo "Error: CROWDIN_PROJECT_ID is not set" >&2
    exit 1
fi

if [[ -z "${CROWDIN_PERSONAL_TOKEN:-}" ]]; then
    echo "Error: CROWDIN_PERSONAL_TOKEN is not set" >&2
    echo "Tip: the CI workflow stores this as CROWDIN_TOKEN and maps it to CROWDIN_PERSONAL_TOKEN." >&2
    exit 1
fi

run_crowdin() {
    "$@" download translations --all --config "$CONFIG_FILE"
}

cleanup() {
    if [[ -n "$TMP_CONFIG" && -f "$TMP_CONFIG" ]]; then
        rm -f "$TMP_CONFIG"
    fi
}
trap cleanup EXIT

make_temp_config() {
    local base_path="$1"
    TMP_CONFIG="$(mktemp "$REPO_ROOT/.crowdin.local.XXXXXX.yml")"
    sed "s#\"base_path\": \"/\"#\"base_path\": \"$base_path\"#" "$CONFIG_FILE" > "$TMP_CONFIG"
}

cd "$REPO_ROOT"

if command -v crowdin >/dev/null 2>&1; then
    echo "Using local Crowdin CLI"
    make_temp_config "$REPO_ROOT"
    crowdin download translations --all --config "$TMP_CONFIG"
elif command -v docker >/dev/null 2>&1; then
    echo "Using Crowdin CLI via Docker"
    make_temp_config "/work"
    docker run --rm \
        --entrypoint crowdin \
        -e CROWDIN_PROJECT_ID \
        -e CROWDIN_PERSONAL_TOKEN \
        -v "$REPO_ROOT":/work \
        -w /work \
        crowdin/cli:latest \
        download translations --all --config /work/"$(basename "$TMP_CONFIG")"
else
    echo "Error: neither 'crowdin' nor 'docker' is available" >&2
    echo "Install Crowdin CLI or Docker and try again." >&2
    exit 1
fi
