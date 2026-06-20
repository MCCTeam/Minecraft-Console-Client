#!/bin/bash
# Start a Minecraft server in a tmux session with named pipe for stdin
# Servers live under $MCC_SERVERS or default to MinecraftOfficial/downloads/<version>/.
resolve_java_bin() {
    if command -v java >/dev/null 2>&1 && java -version >/dev/null 2>&1; then
        command -v java
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
            if "$candidate" -version >/dev/null 2>&1; then
                printf '%s\n' "$candidate"
                return 0
            fi
        fi
    done

    return 1
}

VERSION="${1}"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DOWNLOADS="${MCC_SERVERS:-$REPO_ROOT/MinecraftOfficial/downloads}"
DIR="$DOWNLOADS/$VERSION"
PIPE="$DIR/stdin.pipe"
SESSION="mc-${VERSION//\./_}"
JAVA_BIN="$(resolve_java_bin || true)"

if [ -z "$VERSION" ] || [ ! -d "$DIR" ]; then
    echo "Error: Server directory not found${VERSION:+: $DIR}"
    echo "Available versions:"
    ls "$DOWNLOADS" | grep -E '^[0-9]' | sort -V
    exit 1
fi

if [ ! -f "$DIR/server.jar" ]; then
    echo "Error: No server.jar in $DIR"
    exit 1
fi

if ! command -v tmux >/dev/null 2>&1; then
    echo "Error: tmux is required to start local test servers"
    exit 1
fi

if [[ -z "$JAVA_BIN" ]]; then
    echo "Error: Java was not found on PATH. Install Java or set JAVA_BIN." >&2
    exit 1
fi

if tmux has-session -t "$SESSION" 2>/dev/null; then
    echo "Server $VERSION already running in tmux session '$SESSION'"
    echo "View output:  tmux capture-pane -t '$SESSION' -p -S -50"
    echo "Send command: echo 'say hello' > $PIPE"
    exit 0
fi

rm -f "$DIR/world/session.lock"

if [[ -e "$PIPE" && ! -p "$PIPE" ]]; then
    rm -f "$PIPE"
fi

[ -p "$PIPE" ] || mkfifo "$PIPE"

tmux new-session -d -s "$SESSION" -c "$DIR" \
  "tail -f $PIPE | '$JAVA_BIN' -Xmx2G -Xms2G -jar server.jar nogui 2>&1"

echo "Server $VERSION started in tmux session '$SESSION'"
echo "Send commands: echo 'say hello' > $PIPE"
echo "View output:   tmux capture-pane -t '$SESSION' -p -S -50"
