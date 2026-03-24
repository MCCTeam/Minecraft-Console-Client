#!/bin/bash
# MCC (Minecraft Console Client) Development Utilities
# Source this file to get helper functions: source $MCC_REPO/tools/mcc-env.sh
# Or add to ~/.bashrc:  source "$HOME/Minecraft/Minecraft-Console-Client/tools/mcc-env.sh"

if [[ -n "${BASH_SOURCE[0]:-}" ]]; then
  _mcc_env_source="${BASH_SOURCE[0]}"
elif [[ -n "${ZSH_VERSION:-}" ]]; then
  _mcc_env_source="${(%):-%N}"
else
  _mcc_env_source="$0"
fi

TOOLS_DIR="$(cd "$(dirname "$_mcc_env_source")" && pwd)"
unset _mcc_env_source
export MCC_REPO="$(cd "$TOOLS_DIR/.." && pwd)"
export MCC_SERVERS="${MCC_SERVERS:-$MCC_REPO/MinecraftOfficial/downloads}"

# Helper: convert version to tmux session name (dots -> underscores)
_mc-session() { echo "mc-${1//\./_}"; }

# --- Minecraft Server Management ---
mc-start() { bash "$MCC_REPO/tools/start-server.sh" "${1:-1.20.6}"; }
mc-stop()  { local v="${1:-1.20.6}"; echo "stop" > "$MCC_SERVERS/$v/stdin.pipe"; }
mc-cmd()   { local v="${2:-1.20.6}"; echo "$1" > "$MCC_SERVERS/$v/stdin.pipe"; }
mc-log()   { local s; s=$(_mc-session "${1:-1.20.6}"); tmux capture-pane -t "$s" -p -S "-${2:-50}"; }
mc-kill()  { local v="${1:-1.20.6}" s; s=$(_mc-session "$v"); tmux kill-session -t "$s" 2>/dev/null; rm -f "$MCC_SERVERS/$v/stdin.pipe"; echo "Killed $s"; }
mc-list()  { tmux list-sessions 2>/dev/null | grep "^mc-" || echo "No running MC servers"; }

# --- RCON ---
mc-rcon() { bash "$MCC_REPO/tools/mc-rcon.sh" "$@"; }

# --- MCC Build/Run ---
mcc-build() { dotnet build "$MCC_REPO/MinecraftClient.sln" -c Release; }
mcc-run()   {
  local port="${1:-25565}"
  shift || true
  cd "$MCC_REPO" && MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release -- CursorBot - "localhost:${port}" "$@" 2>&1
}
mcc-cmd()   { echo "$1" >> "$MCC_REPO/mcc_input.txt"; }
mcc-kill()  { pkill -f "MinecraftClient" 2>/dev/null && echo "MCC killed" || echo "No MCC process found"; tmux kill-session -t mcc-debug 2>/dev/null || true; }
mcc-reload() {
  mcc-kill
  sleep 1
  mcc-build && mcc-run
}

# --- TUI Mode ---
mcc-tui()   {
  local port="${1:-25565}"
  shift || true
  tmux new-session -d -s mcc-debug -x 160 -y 50 \
    "cd '$MCC_REPO' && dotnet run --project MinecraftClient -c Release -- CursorBot - localhost:${port} $* 2>&1; echo '=== MCC EXITED ==='; sleep 600"
  echo "TUI mode launched in tmux session 'mcc-debug'"
  echo "Attach: tmux attach -t mcc-debug"
}

# --- Debug helpers ---
mcc-debug()   { bash "$MCC_REPO/tools/mcc-debug.sh" "$@"; }
mcc-log-mcc() { tail -f "${TMPDIR:-/tmp}/mcc-debug/mcc-debug.log" 2>/dev/null || echo "No MCC log found"; }
mcc-state()   { echo "debug state" >> "$MCC_REPO/mcc_input.txt"; sleep 1; tail -30 "${TMPDIR:-/tmp}/mcc-debug/mcc-debug.log" 2>/dev/null; }
