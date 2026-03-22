#!/bin/bash
# MCC (Minecraft Console Client) Development Utilities
# Source this file to get helper functions: source $MCC_REPO/tools/mcc-env.sh
# Or add to ~/.bashrc:  source "$HOME/Minecraft/Minecraft-Console-Client-milutinke/tools/mcc-env.sh"

TOOLS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export MCC_REPO="$(cd "$TOOLS_DIR/.." && pwd)"
export MCC_SERVERS="$MCC_REPO/MinecraftOfficial/downloads"

# Helper: convert version to tmux session name (dots -> underscores)
_mc-session() { echo "mc-${1//\./_}"; }

# --- Minecraft Server Management ---
mc-start() { "$MCC_REPO/tools/start-server.sh" "${1:-1.20.6}"; }
mc-stop()  { local v="${1:-1.20.6}"; echo "stop" > "$MCC_SERVERS/$v/stdin.pipe"; }
mc-cmd()   { local v="${2:-1.20.6}"; echo "$1" > "$MCC_SERVERS/$v/stdin.pipe"; }
mc-log()   { local s; s=$(_mc-session "${1:-1.20.6}"); tmux capture-pane -t "$s" -p -S "-${2:-50}"; }
mc-kill()  { local v="${1:-1.20.6}" s; s=$(_mc-session "$v"); tmux kill-session -t "$s" 2>/dev/null; rm -f "$MCC_SERVERS/$v/stdin.pipe"; echo "Killed $s"; }
mc-list()  { tmux list-sessions 2>/dev/null | grep "^mc-" || echo "No running MC servers"; }

# --- RCON ---
mc-rcon() { "$MCC_REPO/tools/mc-rcon.sh" "$@"; }

# --- MCC Build/Run ---
mcc-build() { dotnet build "$MCC_REPO/MinecraftClient.sln" -c Release; }
mcc-run()   { cd "$MCC_REPO" && MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release -- CursorBot - "localhost:${1:-25565}" 2>&1; }
mcc-cmd()   { echo "$1" >> "$MCC_REPO/mcc_input.txt"; }
mcc-kill()  { pkill -f "MinecraftClient" 2>/dev/null && echo "MCC killed" || echo "No MCC process found"; }
mcc-reload() {
  mcc-kill
  sleep 1
  mcc-build && mcc-run
}
