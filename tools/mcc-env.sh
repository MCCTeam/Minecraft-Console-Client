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
MCC_REPO_ROOT="$(cd "$TOOLS_DIR/.." && pwd)"
unset _mcc_env_source
export MCC_REPO="$MCC_REPO_ROOT"
export MCC_SERVERS="${MCC_SERVERS:-$MCC_REPO_ROOT/MinecraftOfficial/downloads}"

_mcc_repo_root() {
  printf '%s\n' "$MCC_REPO_ROOT"
}

_mcc_servers_root() {
  printf '%s\n' "${MCC_SERVERS:-$MCC_REPO_ROOT/MinecraftOfficial/downloads}"
}

_mcc_current_worktree_name() {
  git -C "$MCC_REPO_ROOT" rev-parse --show-toplevel 2>/dev/null | xargs basename
}

_mcc_resolve_session() {
  local explicit="${1:-}"
  if [[ -n "$explicit" ]]; then
    printf '%s\n' "$explicit"
    return 0
  fi

  local worktree
  worktree="$(_mcc_current_worktree_name)"
  if [[ -n "$worktree" ]]; then
    printf '%s\n' "$worktree"
    return 0
  fi

  basename "$MCC_REPO_ROOT"
}

_mcc_sha1_short() {
  if command -v sha1sum >/dev/null 2>&1; then
    printf '%s' "$1" | sha1sum | awk '{print substr($1, 1, 4)}'
  else
    printf '%s' "$1" | shasum -a 1 | awk '{print substr($1, 1, 4)}'
  fi
}

_mcc_resolve_username() {
  local session="$1"
  local normalized
  normalized="$(printf '%s' "$session" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9_]/_/g')"
  local candidate="mcc_${normalized}"
  if (( ${#candidate} <= 16 )); then
    printf '%s\n' "$candidate"
    return 0
  fi

  local hash
  hash="$(_mcc_sha1_short "$normalized")"
  printf '%s_%s\n' "${candidate:0:11}" "$hash"
}

_mcc_session_root() {
  printf '%s/mcc-debug/%s\n' "${TMPDIR:-/tmp}" "$1"
}

_mcc_session_log_file() {
  printf '%s/mcc-debug.log\n' "$(_mcc_session_root "$1")"
}

_mcc_session_input_file() {
  printf '%s/mcc_input.txt\n' "$(_mcc_session_root "$1")"
}

_mcc_session_pid_file() {
  printf '%s/mcc.pid\n' "$(_mcc_session_root "$1")"
}

_mcc_session_meta_file() {
  printf '%s/session.meta\n' "$(_mcc_session_root "$1")"
}

_mcc_tmux_session_name() {
  printf 'mcc-%s\n' "$1"
}

_mcc_build_root() {
  local worktree
  worktree="$(_mcc_current_worktree_name)"
  if [[ -z "$worktree" ]]; then
    worktree="$(basename "$MCC_REPO_ROOT")"
  fi

  if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    if [[ -d /dev/shm && -w /dev/shm ]]; then
      printf '/dev/shm/mcc-build/%s\n' "$worktree"
    else
      printf '%s/mcc-build/%s\n' "${TMPDIR:-/tmp}" "$worktree"
    fi
    return 0
  fi

  printf '%s\n' "$MCC_REPO_ROOT"
}

# Helper: convert version to tmux session name (dots -> underscores)
_mc-session() { echo "mc-${1//\./_}"; }

# --- Minecraft Server Management ---
mc-start() { bash "$MCC_REPO/tools/start-server.sh" "${1:-1.20.6}"; }
mc-stop()  { local v="${1:-1.20.6}"; echo "stop" > "$MCC_SERVERS/$v/stdin.pipe"; }
mc-cmd()   { local v="${2:-1.20.6}"; echo "$1" > "$MCC_SERVERS/$v/stdin.pipe"; }
mc-log()   { local s; s=$(_mc-session "${1:-1.20.6}"); tmux capture-pane -t "$s" -p -S "-${2:-50}"; }
mc-kill()  { local v="${1:-1.20.6}" s; s=$(_mc-session "$v"); tmux kill-session -t "$s" 2>/dev/null; rm -f "$MCC_SERVERS/$v/stdin.pipe"; echo "Killed $s"; }
mc-list()  { tmux list-sessions 2>/dev/null | grep "^mc-" || echo "No running MC servers"; }
mc-wait-ready() { bash "$MCC_REPO/.skills/mcc-integration-testing/scripts/preflight_test_env.sh" "${1:-1.20.6}" >/dev/null && source "$MCC_REPO/.skills/mcc-integration-testing/scripts/common.sh" && wait_for_server_ready "${1:-1.20.6}" "${2:-60}"; }
mc-wait-stop() { source "$MCC_REPO/.skills/mcc-integration-testing/scripts/common.sh" && wait_for_server_stop "${1:-1.20.6}" "${2:-60}"; }
mc-reset-test-env() { bash "$MCC_REPO/.skills/mcc-integration-testing/scripts/reset_shared_test_state.sh" "$@"; }

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
mcc-preflight() { bash "$MCC_REPO/.skills/mcc-integration-testing/scripts/preflight_test_env.sh" "$@"; }
