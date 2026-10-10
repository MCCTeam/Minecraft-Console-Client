#!/bin/bash
# MCC (Minecraft Console Client) Development Utilities Source this file to get helper functions: source $MCC_REPO/tools/mcc-env.sh Or add to ~/.bashrc: source "$HOME/Minecraft/Minecraft-Console-Client/tools/mcc-env.sh"

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
  if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    local build_root
    build_root="$(_mcc_build_root)"
    mkdir -p "$build_root"
    env MCC_BUILD_ROOT="$build_root" MCC_ALLOW_RAW_DOTNET=1 "$@"
    return $?
  fi

  MCC_ALLOW_RAW_DOTNET=1 "$@"
}

MCC_SLN_CONFIG_ARGS=(-p:ShouldUnsetParentConfigurationAndPlatform=false)

mcc-build() {
  local repo_root
  repo_root="$(_mcc_repo_root)"
  _mcc_dotnet_env dotnet build "$repo_root/Mcc.slnx" -c Release "${MCC_SLN_CONFIG_ARGS[@]}"
}
mcc-publish() {
  local repo_root rid=""
  local -a extra_args=()

  repo_root="$(_mcc_repo_root)"

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --rid|-r)
        shift
        if [[ $# -eq 0 ]]; then
          echo "mcc-publish: --rid requires a value" >&2
          return 1
        fi
        rid="$1"
        shift
        ;;
      --)
        shift
        extra_args+=("$@")
        break
        ;;
      *)
        extra_args+=("$1")
        shift
        ;;
    esac
  done

  if [[ -z "$rid" ]]; then
    echo "mcc-publish: missing required --rid <RID>" >&2
    echo "mcc-publish: example: mcc-publish --rid linux-x64" >&2
    return 1
  fi

  # Keep managed assemblies accessible for runtime source-plugin compilation.
  _mcc_dotnet_env dotnet publish "$repo_root/src/Mcc.Cli/Mcc.Cli.csproj" \
    "${MCC_SLN_CONFIG_ARGS[@]}" \
    -r "$rid" \
    --self-contained=true \
    -c Release \
    -p:UseAppHost=true \
    -p:PublishSingleFile=false \
    -p:DebugType=Embedded \
    "${extra_args[@]}"
}
mcc-build-clean() {
  if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    local build_root
    build_root="$(_mcc_build_root)"
    rm -rf "$build_root"
    return 0
  fi

  dotnet clean "$(_mcc_repo_root)/Mcc.slnx" -c Release
}
mcc-test() { _mcc_dotnet_env dotnet test "$MCC_REPO_ROOT/src/Mcc.Cli.Tests/Mcc.Cli.Tests.csproj" -c Release "${MCC_SLN_CONFIG_ARGS[@]}" "$@"; }
mcc-run() {
  local dll="$MCC_REPO_ROOT/src/Mcc.Cli/bin/Release/net10.0/Mcc.Cli.dll"
  if [[ "${MCC_BUILD_MODE:-local}" == tmpfs ]]; then
    dll="$(_mcc_build_root)/Mcc.Cli/bin/Release/net10.0/Mcc.Cli.dll"
  fi
  [[ -f "$dll" ]] || { echo 'Run mcc-build first.' >&2; return 1; }
  local run_root="${MCC_RUN_ROOT:-${TMPDIR:-/tmp}/mcc-2.0-$(basename "$MCC_REPO_ROOT")}"
  mkdir -p "$run_root"
  (cd "$run_root" && dotnet "$dll" "$@")
}
mcc-tui() { mcc-run "$@" --console.General.ConsoleMode=tui; }
