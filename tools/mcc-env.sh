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
  local worktree_root
  if ! worktree_root="$(git -C "$MCC_REPO_ROOT" rev-parse --show-toplevel 2>/dev/null)"; then
    return 0
  fi

  if [[ -z "$worktree_root" ]]; then
    return 0
  fi

  basename "$worktree_root"
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

_mcc_dotnet_env() {
  local -n env_ref="$1"
  env_ref=()

  if [[ "${MCC_BUILD_MODE:-local}" != "tmpfs" ]]; then
    return 0
  fi

  local build_root
  build_root="$(_mcc_build_root)"
  mkdir -p "$build_root"
  env_ref+=("MCC_BUILD_ROOT=$build_root")
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
mcc-build() {
  local repo_root
  local -a dotnet_env
  repo_root="$(_mcc_repo_root)"
  _mcc_dotnet_env dotnet_env
  env "${dotnet_env[@]}" dotnet build "$repo_root/MinecraftClient.sln" -c Release
}
mcc-build-clean() {
  if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    local build_root
    build_root="$(_mcc_build_root)"
    rm -rf "$build_root"
    return 0
  fi

  dotnet clean "$(_mcc_repo_root)/MinecraftClient.sln" -c Release
}
mcc-run()   {
  local port="${1:-25565}"
  shift || true
  cd "$MCC_REPO" && MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release -- CursorBot - "localhost:${port}" "$@" 2>&1
}
mcc-cmd() {
  local session=""
  local -a command_parts=()
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session)
        shift
        if [[ $# -eq 0 ]]; then
          echo "mcc-cmd: --session requires a value" >&2
          return 1
        fi
        session="$1"
        shift
        ;;
      *)
        command_parts+=("$1")
        shift
        ;;
    esac
  done

  if [[ ${#command_parts[@]} -eq 0 ]]; then
    echo "Usage: mcc-cmd [--session NAME] <command>" >&2
    return 1
  fi
  session="$(_mcc_resolve_session "$session")"
  local input_file
  input_file="$(_mcc_session_input_file "$session")"
  mkdir -p "$(dirname "$input_file")"
  local command
  command="${command_parts[*]}"
  printf '%s\n' "$command" >> "$input_file"
}
mcc-kill()  {
  local session=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session)
        shift
        if [[ $# -eq 0 ]]; then
          echo "mcc-kill: --session requires a value" >&2
          return 1
        fi
        session="$1"
        shift
        ;;
      *)
        echo "Unknown option: $1" >&2
        return 1
        ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  local pid_file meta_file tmux_session pid pid_comm pid_args
  local killed=false
  pid_file="$(_mcc_session_pid_file "$session")"
  meta_file="$(_mcc_session_meta_file "$session")"
  tmux_session="$(_mcc_tmux_session_name "$session")"

  if [[ -f "$pid_file" ]]; then
    pid="$(tr -cd '0-9' < "$pid_file")"
    if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
      pid_comm="$(ps -p "$pid" -o comm= 2>/dev/null | tr -d '[:space:]')"
      pid_args="$(ps -p "$pid" -o args= 2>/dev/null || true)"
      if [[ "$pid_comm" == "MinecraftClient" ]] || { [[ "$pid_comm" == "dotnet" ]] && [[ "$pid_args" == *"MinecraftClient"* ]]; }; then
        kill "$pid" 2>/dev/null || true
        echo "Killed MCC PID $pid for session '$session'"
        killed=true
      else
        echo "Refusing to kill PID $pid for session '$session': unexpected process '$pid_comm'"
      fi
    else
      echo "No live MCC PID found for session '$session' (pid file: $pid_file)"
    fi
  fi

  if tmux has-session -t "$tmux_session" 2>/dev/null; then
    tmux kill-session -t "$tmux_session" 2>/dev/null || true
    echo "Killed tmux session '$tmux_session'"
    killed=true
  fi

  if [[ -f "$pid_file" || -f "$meta_file" ]]; then
    rm -f "$pid_file" "$meta_file"
  fi

  if [[ "$killed" == false ]]; then
    echo "No MCC process or tmux session found for session '$session'"
  fi
}
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

_mcc_session_log_tail() {
  local session="$1"
  local log_file
  log_file="$(_mcc_session_log_file "$session")"
  if [[ -e "$log_file" ]]; then
    tail -n 30 "$log_file" 2>/dev/null
  else
    echo "No MCC log found"
  fi
}

_mcc_session_log_follow() {
  local session="$1"
  local log_file
  log_file="$(_mcc_session_log_file "$session")"
  tail -f "$log_file" 2>/dev/null || echo "No MCC log found"
}

# --- Debug helpers ---
mcc-debug()   { bash "$MCC_REPO/tools/mcc-debug.sh" "$@"; }
mcc-log-mcc() {
  local session=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session)
        shift
        if [[ $# -eq 0 ]]; then
          echo "mcc-log-mcc: --session requires a value" >&2
          return 1
        fi
        session="$1"
        shift
        ;;
      *)
        echo "Unknown option: $1" >&2
        return 1
        ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  _mcc_session_log_follow "$session"
}
mcc-state() {
  local session=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session)
        shift
        if [[ $# -eq 0 ]]; then
          echo "mcc-state: --session requires a value" >&2
          return 1
        fi
        session="$1"
        shift
        ;;
      *)
        echo "Unknown option: $1" >&2
        return 1
        ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  mcc-cmd --session "$session" "debug state"
  sleep 1
  _mcc_session_log_tail "$session"
}
mcc-preflight() { bash "$MCC_REPO/.skills/mcc-integration-testing/scripts/preflight_test_env.sh" "$@"; }
mcc-reset-session() {
  local session=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session)
        shift
        if [[ $# -eq 0 ]]; then
          echo "mcc-reset-session: --session requires a value" >&2
          return 1
        fi
        session="$1"
        shift
        ;;
      *)
        echo "Unknown option: $1" >&2
        return 1
        ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  tmux kill-session -t "$(_mcc_tmux_session_name "$session")" 2>/dev/null || true
  rm -rf "$(_mcc_session_root "$session")"
}
