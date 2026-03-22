#!/bin/bash
# Start a Minecraft server in a tmux session with named pipe for stdin
# Servers live in MinecraftOfficial/downloads/<version>/ alongside the downloaded server.jar
VERSION="${1}"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DOWNLOADS="$REPO_ROOT/MinecraftOfficial/downloads"
DIR="$DOWNLOADS/$VERSION"
PIPE="$DIR/stdin.pipe"
SESSION="mc-${VERSION//\./_}"

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

if tmux has-session -t "$SESSION" 2>/dev/null; then
    echo "Server $VERSION already running in tmux session '$SESSION'"
    echo "View output:  tmux capture-pane -t '$SESSION' -p -S -50"
    echo "Send command: echo 'say hello' > $PIPE"
    exit 0
fi

rm -f "$DIR/world/session.lock"

[ -p "$PIPE" ] || mkfifo "$PIPE"

tmux new-session -d -s "$SESSION" -c "$DIR" \
  "tail -f $PIPE | java -Xmx2G -Xms2G -jar server.jar nogui 2>&1"

echo "Server $VERSION started in tmux session '$SESSION'"
echo "Send commands: echo 'say hello' > $PIPE"
echo "View output:   tmux capture-pane -t '$SESSION' -p -S -50"
