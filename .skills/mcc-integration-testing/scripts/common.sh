#!/usr/bin/env bash

sed_in_place() {
    if [[ "$(uname)" == "Darwin" ]]; then
        sed -i '' "$@"
    else
        sed -i "$@"
    fi
}

ensure_java_in_path() {
    if command -v java >/dev/null 2>&1 && java -version >/dev/null 2>&1; then
        return 0
    fi

    local candidate
    for candidate in \
        "${JAVA_BIN:-}" \
        "/opt/homebrew/opt/openjdk/bin/java" \
        "/usr/local/opt/openjdk/bin/java" \
        "/usr/lib/jvm/default-java/bin/java"
    do
        [[ -z "$candidate" ]] && continue
        if [[ -x "$candidate" ]]; then
            export PATH="$(dirname "$candidate"):$PATH"
            export JAVA_BIN="$candidate"
            if java -version >/dev/null 2>&1; then
                return 0
            fi
        fi
    done

    echo "java was not found on PATH. Install Java or set JAVA_BIN." >&2
    return 1
}

server_session_name() {
    printf 'mc-%s\n' "${1//./_}"
}

server_running() {
    local version="$1"
    mc-list | grep -Fq "$(server_session_name "$version")"
}

wait_for_server_ready() {
    local version="$1"
    local timeout="${2:-60}"
    local elapsed=0

    while (( elapsed < timeout )); do
        if mc-log "$version" 250 2>/dev/null | grep -Fq "Done ("; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    echo "Timed out waiting for $version to become ready" >&2
    return 1
}

wait_for_server_stop() {
    local version="$1"
    local timeout="${2:-60}"
    local elapsed=0

    while (( elapsed < timeout )); do
        if ! server_running "$version"; then
            return 0
        fi
        sleep 1
        ((elapsed += 1))
    done

    mc-kill "$version" --confirm >/dev/null 2>&1 || true

    if ! server_running "$version"; then
        return 0
    fi

    echo "Timed out waiting for $version to stop" >&2
    return 1
}

disable_noisy_bots_in_ini() {
    local ini_file="$1"
    local section

    for section in \
        ScriptScheduler \
        DiscordRpc \
        AntiAFK \
        AutoDig \
        AutoAttack \
        PlayerListLogger \
        ReplayCapture
    do
        sed_in_place "/^\\[ChatBot\\.${section}\\]/,/^\\[/ { s/^Enabled = true/Enabled = false/; }" "$ini_file"
    done
}

remove_stale_stdin_pipe() {
    local version="$1"
    local pipe_path="$MCC_SERVERS/$version/stdin.pipe"

    if [[ -e "$pipe_path" ]] && ! server_running "$version"; then
        rm -f "$pipe_path"
    fi
}
