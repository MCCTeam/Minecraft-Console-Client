# MCC 共享服务器与隔离会话 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让多个 Git worktree 共享同一套本地 Minecraft 测试服务器，同时让每个 MCC 调试会话按 `session` 隔离，并支持按 worktree 选择性把构建输出放到 tmpfs。

**Architecture:** 保持 `mc-*` 命令面向共享服务器，保留 `MCC_SERVERS` 作为共享 server root override；把 `mcc-*` 命令改成面向显式 `session` 的客户端会话工具，默认 `session` 取当前 worktree 名，默认用户名从 `session` 派生。构建层通过统一的 helper 和 `Directory.Build.props` 接入可选的 tmpfs 构建根，不再依赖 shell 中泄漏的 `MCC_REPO`。

**Tech Stack:** Bash, tmux, dotnet CLI, MSBuild `Directory.Build.props`, repo 自带的 MCC integration harness

---

### Task 1: 建立会话与构建根解析的基础 helper，并补一个可重复运行的 shell smoke test

**Files:**
- Create: `tools/test-mcc-env.sh`
- Modify: `tools/mcc-env.sh`

- [ ] **Step 1: 先写一个会失败的 shell smoke test，锁定会话名、用户名和路径解析规则**

```bash
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

assert_eq() {
    local expected="$1"
    local actual="$2"
    local label="$3"
    if [[ "$expected" != "$actual" ]]; then
        echo "FAIL: $label" >&2
        echo "  expected: $expected" >&2
        echo "  actual:   $actual" >&2
        exit 1
    fi
}

assert_regex() {
    local regex="$1"
    local actual="$2"
    local label="$3"
    if [[ ! "$actual" =~ $regex ]]; then
        echo "FAIL: $label" >&2
        echo "  regex:  $regex" >&2
        echo "  actual: $actual" >&2
        exit 1
    fi
}

session="$(_mcc_resolve_session "demo-branch")"
assert_eq "demo-branch" "$session" "explicit session"

short_name="$(_mcc_resolve_username "feature-ai")"
assert_eq "mcc_feature_ai" "$short_name" "short derived username"

long_name="$(_mcc_resolve_username "very-long-worktree-name")"
assert_regex '^mcc_[a-z0-9_]{7}_[0-9a-f]{4}$' "$long_name" "long derived username shape"
assert_eq "16" "${#long_name}" "long derived username length"

assert_eq "${TMPDIR:-/tmp}/mcc-debug/demo-branch" "$(_mcc_session_root "demo-branch")" "session root"
assert_eq "mcc-demo-branch" "$(_mcc_tmux_session_name "demo-branch")" "tmux session name"

MCC_BUILD_MODE=tmpfs
build_root="$(_mcc_build_root)"
assert_regex '^(/dev/shm|/tmp)/mcc-build/.+$' "$build_root" "tmpfs build root"

echo "PASS"
```

- [ ] **Step 2: 运行测试，确认它先失败**

Run: `bash tools/test-mcc-env.sh`

Expected: FAIL，错误类似 `_mcc_resolve_session: command not found`

- [ ] **Step 3: 在 `tools/mcc-env.sh` 中加入 repo root、server root、session、username、会话目录、tmux 名和 build root helper**

```bash
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
  else
    basename "$MCC_REPO_ROOT"
  fi
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
```

- [ ] **Step 4: 再跑 smoke test，确认规则都落地**

Run: `bash tools/test-mcc-env.sh`

Expected: PASS

- [ ] **Step 5: 提交这一小步**

```bash
git add tools/mcc-env.sh tools/test-mcc-env.sh
git commit -m "tools: add MCC session and build root helpers"
```

### Task 2: 让 `mcc-*` shell helper 全部按 `session` 工作，并新增安全的 session reset

**Files:**
- Modify: `tools/mcc-env.sh`
- Test: `tools/test-mcc-env.sh`

- [ ] **Step 1: 在 smoke test 里加入 `mcc-cmd` 和 `mcc-reset-session` 的行为检查**

```bash
session="wrapper-smoke"
input_file="$(_mcc_session_input_file "$session")"
rm -rf "$(_mcc_session_root "$session")"

mcc-cmd --session "$session" "debug state"
grep -Fq "debug state" "$input_file"

mcc-reset-session --session "$session"
[[ ! -e "$(_mcc_session_root "$session")" ]]
```

- [ ] **Step 2: 运行测试，确认它先因为参数解析或命令不存在而失败**

Run: `bash tools/test-mcc-env.sh`

Expected: FAIL，错误类似 `mcc-reset-session: command not found` 或 `Unknown option: --session`

- [ ] **Step 3: 在 `tools/mcc-env.sh` 里把 `mcc-*` helper 改成 session-aware**

```bash
mcc-build() {
  local repo_root
  repo_root="$(_mcc_repo_root)"
  if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    local build_root
    build_root="$(_mcc_build_root)"
    mkdir -p "$build_root"
    MCC_BUILD_ROOT="$build_root" dotnet build "$repo_root/MinecraftClient.sln" -c Release
  else
    dotnet build "$repo_root/MinecraftClient.sln" -c Release
  fi
}

mcc-cmd() {
  local session=""
  local command=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session) session="$2"; shift 2 ;;
      *) command="$1"; shift ;;
    esac
  done

  [[ -n "$command" ]] || { echo "Usage: mcc-cmd [--session NAME] <command>" >&2; return 1; }
  session="$(_mcc_resolve_session "$session")"
  local input_file
  input_file="$(_mcc_session_input_file "$session")"
  mkdir -p "$(dirname "$input_file")"
  printf '%s\n' "$command" >> "$input_file"
}

mcc-log-mcc() {
  local session="${1:-}"
  if [[ "$session" == "--session" ]]; then
    session="${2:-}"
  fi
  session="$(_mcc_resolve_session "$session")"
  tail -f "$(_mcc_session_log_file "$session")"
}

mcc-state() {
  local session=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session) session="$2"; shift 2 ;;
      *) echo "Unknown option: $1" >&2; return 1 ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  mcc-cmd --session "$session" "debug state"
  sleep 1
  tail -30 "$(_mcc_session_log_file "$session")"
}

mcc-reset-session() {
  local session=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session) session="$2"; shift 2 ;;
      *) echo "Unknown option: $1" >&2; return 1 ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  tmux kill-session -t "$(_mcc_tmux_session_name "$session")" 2>/dev/null || true
  rm -rf "$(_mcc_session_root "$session")"
}
```

- [ ] **Step 4: 运行 smoke test，确认 session input 与 reset 生效**

Run: `bash tools/test-mcc-env.sh`

Expected: PASS

- [ ] **Step 5: 提交这一小步**

```bash
git add tools/mcc-env.sh tools/test-mcc-env.sh
git commit -m "tools: scope MCC shell helpers by session"
```

### Task 3: 改造 `mcc-debug.sh`、`mcc-log-tail.sh` 和 kill 流程，隔离 tmux、log、config、PID、metadata

**Files:**
- Modify: `tools/mcc-debug.sh`
- Modify: `tools/mcc-log-tail.sh`
- Modify: `tools/mcc-env.sh`

- [ ] **Step 1: 用真实命令先验证当前行为会互相覆盖**

Run:

```bash
source tools/mcc-env.sh
mcc-debug -v 1.21.11-Vanilla --file-input --no-build
ls -la /tmp/mcc-debug
tmux list-sessions | grep '^mcc-debug:'
```

Expected: 看到固定目录 `/tmp/mcc-debug` 和固定 tmux 名 `mcc-debug`

- [ ] **Step 2: 在 `tools/mcc-debug.sh` 中加入 `--session` 和 `--username`，并把所有运行时工件改成 session-scoped**

```bash
SESSION=""
USERNAME=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --session)  SESSION="$2"; shift 2 ;;
        --username) USERNAME="$2"; shift 2 ;;
        -v|--version) VERSION="$2"; shift 2 ;;
        -m|--mode) MODE="$2"; shift 2 ;;
        -p|--port) PORT="$2"; PORT_SET_BY_USER=true; shift 2 ;;
        --no-build) DO_BUILD=false; shift ;;
        --debug-on) DEBUG_ON=true; shift ;;
        --file-input) FILE_INPUT=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
    esac
done

SESSION="$(_mcc_resolve_session "$SESSION")"
USERNAME="${USERNAME:-$(_mcc_resolve_username "$SESSION")}"
SESSION_ROOT="$(_mcc_session_root "$SESSION")"
CFG="$SESSION_ROOT/MinecraftClient.debug.ini"
MCC_LOG="$(_mcc_session_log_file "$SESSION")"
INPUT_FILE="$(_mcc_session_input_file "$SESSION")"
PID_FILE="$(_mcc_session_pid_file "$SESSION")"
META_FILE="$(_mcc_session_meta_file "$SESSION")"
MCC_TMUX_SESSION="$(_mcc_tmux_session_name "$SESSION")"

mkdir -p "$SESSION_ROOT"

cat > "$META_FILE" <<EOF
SESSION=$SESSION
USERNAME=$USERNAME
MODE=$MODE
CFG=$CFG
LOG=$MCC_LOG
INPUT=$INPUT_FILE
PID_FILE=$PID_FILE
TMUX_SESSION=$MCC_TMUX_SESSION
SERVER_VERSION=$VERSION
EOF

MCC_ARGS=("$CFG" "$USERNAME" "-" "localhost:$PORT")
```

- [ ] **Step 3: 把 `mcc-kill` 改成精准杀当前 session，不再 `pkill -f MinecraftClient`**

```bash
mcc-kill() {
  local session=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session) session="$2"; shift 2 ;;
      *) echo "Unknown option: $1" >&2; return 1 ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  local pid_file meta_file tmux_name
  pid_file="$(_mcc_session_pid_file "$session")"
  meta_file="$(_mcc_session_meta_file "$session")"
  tmux_name="$(_mcc_tmux_session_name "$session")"

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid"
      wait "$pid" 2>/dev/null || true
    fi
    rm -f "$pid_file"
  fi

  tmux kill-session -t "$tmux_name" 2>/dev/null || true
  [[ -f "$meta_file" ]] && rm -f "$meta_file"
}
```

- [ ] **Step 4: 让 `tools/mcc-log-tail.sh` 支持 `--session`，读取 session-specific log**

```bash
SESSION=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --session) SESSION="$2"; shift 2 ;;
        --server) SERVER_VER="$2"; shift 2 ;;
        --server-only) SERVER_ONLY=true; SERVER_VER="$2"; shift 2 ;;
        -h|--help) echo "Usage: tools/mcc-log-tail.sh [--session NAME] [--server VER] [--server-only VER]"; exit 0 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

SESSION="$(_mcc_resolve_session "$SESSION")"
MCC_LOG="$(_mcc_session_log_file "$SESSION")"
```

- [ ] **Step 5: 用两个显式 session 跑一次真实 smoke**

Run:

```bash
source tools/mcc-env.sh
mcc-debug -v 1.21.11-Vanilla --session smoke-a --username SmokeA --file-input --no-build
mcc-debug -v 1.21.11-Vanilla --session smoke-b --username SmokeB --file-input --no-build
test -f "$(_mcc_session_log_file smoke-a)"
test -f "$(_mcc_session_log_file smoke-b)"
test -f "$(_mcc_session_meta_file smoke-a)"
test -f "$(_mcc_session_meta_file smoke-b)"
tmux list-sessions | grep '^mcc-smoke-a:'
tmux list-sessions | grep '^mcc-smoke-b:'
```

Expected: 两套独立文件与两套 tmux 会话都存在

- [ ] **Step 6: 提交这一小步**

```bash
git add tools/mcc-debug.sh tools/mcc-log-tail.sh tools/mcc-env.sh
git commit -m "tools: isolate MCC debug state by session"
```

### Task 4: 加入按 worktree 选择性启用的 tmpfs 构建根，并修正外置 `obj` 的 MSBuild 过滤问题

**Files:**
- Create: `Directory.Build.props`
- Modify: `tools/mcc-env.sh`
- Test: `tools/test-mcc-env.sh`

- [ ] **Step 1: 先用当前仓库验证“直接外置 obj 会失败”，把这个回归固定住**

Run:

```bash
rm -rf /tmp/mcc-plan-build-smoke
dotnet build MinecraftClient.sln -c Release -v minimal --nologo \
  -p:BaseOutputPath=/tmp/mcc-plan-build-smoke/bin/ \
  -p:BaseIntermediateOutputPath=/tmp/mcc-plan-build-smoke/obj/ \
  -p:MSBuildProjectExtensionsPath=/tmp/mcc-plan-build-smoke/obj/
```

Expected: FAIL，包含重复 `AssemblyInfo` 或 `TargetFrameworkAttribute` 一类错误

- [ ] **Step 2: 新建 `Directory.Build.props`，统一把 `MCC_BUILD_ROOT` 接到每个项目自己的外置 `bin/obj`，并显式排除仓库内 `bin/obj`**

```xml
<Project>
  <PropertyGroup>
    <DefaultItemExcludes>$(DefaultItemExcludes);**/bin/**;**/obj/**</DefaultItemExcludes>
  </PropertyGroup>

  <PropertyGroup Condition="'$(MCC_BUILD_ROOT)' != ''">
    <BaseOutputPath>$(MCC_BUILD_ROOT)/$(MSBuildProjectName)/bin/</BaseOutputPath>
    <BaseIntermediateOutputPath>$(MCC_BUILD_ROOT)/$(MSBuildProjectName)/obj/</BaseIntermediateOutputPath>
    <MSBuildProjectExtensionsPath>$(BaseIntermediateOutputPath)</MSBuildProjectExtensionsPath>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: 在 `tools/mcc-env.sh` 中把 `mcc-build`、`mcc-run`、`mcc-tui`、`mcc-debug` 的 `dotnet` 调用统一接入 `MCC_BUILD_MODE=tmpfs`**

```bash
_mcc_dotnet_env() {
  if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    local build_root
    build_root="$(_mcc_build_root)"
    mkdir -p "$build_root"
    env MCC_BUILD_ROOT="$build_root" "$@"
    return 0
  fi

  "$@"
}

mcc-run() {
  local session="" username="" port="25565"
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session) session="$2"; shift 2 ;;
      --username) username="$2"; shift 2 ;;
      --port) port="$2"; shift 2 ;;
      *) break ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  username="${username:-$(_mcc_resolve_username "$session")}"
  mkdir -p "$(_mcc_session_root "$session")"
  _mcc_dotnet_env env \
    MCC_FILE_INPUT=1 \
    MCC_INPUT_FILE="$(_mcc_session_input_file "$session")" \
    dotnet run --project "$(_mcc_repo_root)/MinecraftClient" -c Release -- \
    "$(_mcc_session_root "$session")/MinecraftClient.debug.ini" \
    "$username" \
    - \
    "localhost:$port"
}

mcc-tui() {
  local session="" username="" port="25565"
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --session) session="$2"; shift 2 ;;
      --username) username="$2"; shift 2 ;;
      --port) port="$2"; shift 2 ;;
      *) break ;;
    esac
  done

  session="$(_mcc_resolve_session "$session")"
  username="${username:-$(_mcc_resolve_username "$session")}"
  tmux new-session -d -s "$(_mcc_tmux_session_name "$session")" -x 160 -y 50 \
    "cd '$(_mcc_repo_root)' && MCC_BUILD_MODE='${MCC_BUILD_MODE:-local}' bash tools/mcc-debug.sh --session '$session' --username '$username' -p '$port' -m tui --no-build"
}

mcc-build-clean() {
  if [[ "${MCC_BUILD_MODE:-local}" == "tmpfs" ]]; then
    rm -rf "$(_mcc_build_root)"
  else
    dotnet clean "$(_mcc_repo_root)/MinecraftClient.sln" -c Release
  fi
}
```

- [ ] **Step 4: 扩展 smoke test，检查 `tmpfs` build root helper 和清理命令**

```bash
MCC_BUILD_MODE=tmpfs
build_root="$(_mcc_build_root)"
mkdir -p "$build_root/probe"
mcc-build-clean
[[ ! -e "$build_root/probe" ]]
```

- [ ] **Step 5: 用 tmpfs 模式重新 build，确认能成功且产物出现在 worktree 专属构建根**

Run:

```bash
source tools/mcc-env.sh
export MCC_BUILD_MODE=tmpfs
mcc-build
find "$(_mcc_build_root)" -maxdepth 3 -type d | sed -n '1,20p'
```

Expected: `Build succeeded.`，并且构建目录位于 `/dev/shm/mcc-build/<worktree>/` 或 `/tmp/mcc-build/<worktree>/`

- [ ] **Step 6: 提交这一小步**

```bash
git add Directory.Build.props tools/mcc-env.sh tools/test-mcc-env.sh
git commit -m "build: add worktree-aware tmpfs build mode"
```

### Task 5: 更新 integration harness，并新增“单服务器双会话”自动化 smoke test

**Files:**
- Create: `.skills/mcc-integration-testing/scripts/run_parallel_session_smoke_test.sh`
- Modify: `.skills/mcc-integration-testing/scripts/reset_shared_test_state.sh`
- Modify: `.skills/mcc-integration-testing/scripts/run_full_spectrum_test.sh`
- Modify: `tools/run-creative-e2e.sh`

- [ ] **Step 1: 新增并行会话 smoke test 脚本，先把预期行为写下来**

```bash
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"
source "$SCRIPT_DIR/common.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION_A="parallel-a"
SESSION_B="parallel-b"
USER_A="ParallelA"
USER_B="ParallelB"

bash "$SCRIPT_DIR/preflight_test_env.sh" "$VERSION" >/dev/null
bash "$SCRIPT_DIR/reset_shared_test_state.sh" "$VERSION" >/dev/null
mcc-reset-session --session "$SESSION_A"
mcc-reset-session --session "$SESSION_B"
mcc-build
mc-start "$VERSION" >/dev/null
wait_for_server_ready "$VERSION"

mcc-debug -v "$VERSION" --session "$SESSION_A" --username "$USER_A" --file-input --no-build
mcc-debug -v "$VERSION" --session "$SESSION_B" --username "$USER_B" --file-input --no-build

grep -Fq "Server was successfully joined" "$(_mcc_session_log_file "$SESSION_A")"
grep -Fq "Server was successfully joined" "$(_mcc_session_log_file "$SESSION_B")"
mcc-cmd --session "$SESSION_A" "debug state"
mcc-cmd --session "$SESSION_B" "debug state"

mcc-kill --session "$SESSION_A"
tmux has-session -t "$(_mcc_tmux_session_name "$SESSION_B")"
mc-log "$VERSION" 50 | grep -Fq "$USER_B joined the game"
```

- [ ] **Step 2: 运行脚本，确认它先因为新参数或旧的共享路径逻辑而失败**

Run: `bash .skills/mcc-integration-testing/scripts/run_parallel_session_smoke_test.sh 1.21.11-Vanilla`

Expected: FAIL，错误类似 `Unknown option: --session`、固定 `mcc_input.txt` 被共用，或者只有一个客户端会话存活

- [ ] **Step 3: 把 `reset_shared_test_state.sh` 缩回“只管共享服务器”，不要再删除 repo 根下 `mcc_input.txt`**

```bash
if [[ $# -eq 0 || "${1:-}" == "--all" ]]; then
    while IFS= read -r session_name; do
        [[ -z "$session_name" ]] && continue
        kill_named_session "$session_name"
    done < <(tmux list-sessions 2>/dev/null | awk -F: '/^mc-/{print $1}' || true)

    while IFS= read -r pipe_path; do
        [[ -z "$pipe_path" ]] && continue
        if [[ ! -p "$pipe_path" ]]; then
            rm -f "$pipe_path"
        fi
    done < <(find "$MCC_SERVERS" -maxdepth 2 -name 'stdin.pipe' 2>/dev/null || true)
fi
```

- [ ] **Step 4: 更新现有 integration 脚本，让它们使用 session-specific input/log，而不是 repo 根下固定文件**

```bash
SESSION="creative-e2e-${SERVER_DIR//\//_}"
USERNAME="$(_mcc_resolve_username "$SESSION")"
INPUT_FILE="$(_mcc_session_input_file "$SESSION")"
MCC_LOG="$(_mcc_session_log_file "$SESSION")"

rm -rf "$(_mcc_session_root "$SESSION")"
mkdir -p "$(_mcc_session_root "$SESSION")"

(
    cd "$REPO_ROOT"
    MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release --no-build -- \
        "$CFG" \
        "$USERNAME" \
        - \
        "localhost:$SERVER_PORT" \
        > "$MCC_LOG" 2>&1
) &
```

- [ ] **Step 5: 重新运行并行 smoke test 与现有 creative/full-spectrum 回归**

Run:

```bash
bash .skills/mcc-integration-testing/scripts/run_parallel_session_smoke_test.sh 1.21.11-Vanilla
bash tools/run-creative-e2e.sh 1.21.11-Vanilla 1.21.11 modern
bash .skills/mcc-integration-testing/scripts/run_full_spectrum_test.sh 1.21.11-Vanilla
```

Expected: 三个脚本都 PASS；并行 smoke test 中一个 session 被 kill 后，另一个 session 和共享服务器继续存活

- [ ] **Step 6: 提交这一小步**

```bash
git add .skills/mcc-integration-testing/scripts/reset_shared_test_state.sh \
  .skills/mcc-integration-testing/scripts/run_full_spectrum_test.sh \
  .skills/mcc-integration-testing/scripts/run_parallel_session_smoke_test.sh \
  tools/run-creative-e2e.sh
git commit -m "test: cover shared server with isolated MCC sessions"
```

### Task 6: 更新文档，并给出两个 worktree 并行调试的标准操作流程

**Files:**
- Modify: `tools/README.md`
- Modify: `docs/guide/ai-assisted-development.md`
- Modify: `.skills/mcc-dev-workflow/SKILL.md`

- [ ] **Step 1: 在文档里先补“共享服务器，隔离 MCC 会话”的核心规则**

```md
## Shared Server, Isolated MCC Sessions

- `mc-*` commands operate on the shared local Minecraft server.
- `mcc-*` commands operate on one MCC client session.
- Default `session` = current worktree name.
- Default username is derived from `session`, so two worktrees can join the same server without kicking each other.
```

- [ ] **Step 2: 在 `mcc-dev-workflow` 文档和 skill 里补两个 worktree 的真实示例**

````md
```bash
# worktree A
cd ~/Minecraft/Minecraft-Console-Client
source tools/mcc-env.sh
mc-start 1.21.11-Vanilla
mcc-debug -v 1.21.11-Vanilla --file-input

# worktree B
cd ~/Minecraft/Minecraft-Console-Client-foo
source tools/mcc-env.sh
mcc-debug -v 1.21.11-Vanilla --file-input

# Each worktree gets:
# - its own session
# - its own username
# - its own log/input/config/tmux session
# - the same shared server
```
````

- [ ] **Step 3: 在文档里加入 tmpfs 构建模式与清理命令**

````md
### tmpfs build mode

```bash
source tools/mcc-env.sh
export MCC_BUILD_MODE=tmpfs
mcc-build
mcc-build-clean
```

When `MCC_BUILD_MODE=tmpfs`, build outputs are redirected to `/dev/shm/mcc-build/<worktree>/` on Linux, or `${TMPDIR:-/tmp}/mcc-build/<worktree>/` if `/dev/shm` is unavailable.
````

- [ ] **Step 4: 跑最终验证矩阵并保存关键输出**

Run:

```bash
bash tools/test-mcc-env.sh
source tools/mcc-env.sh && unset MCC_BUILD_MODE && mcc-build
source tools/mcc-env.sh && export MCC_BUILD_MODE=tmpfs && mcc-build
bash .skills/mcc-integration-testing/scripts/run_parallel_session_smoke_test.sh 1.21.11-Vanilla
```

Expected:

```text
PASS
Build succeeded.
Build succeeded.
parallel session smoke: PASS
```

- [ ] **Step 5: 提交文档与最终验证结果**

```bash
git add tools/README.md docs/guide/ai-assisted-development.md .skills/mcc-dev-workflow/SKILL.md
git commit -m "docs: document shared server and isolated MCC sessions"
```

### Self-Review Checklist

- [ ] 计划里的 `session`、`username`、`MCC_BUILD_MODE`、`MCC_BUILD_ROOT` 命名在所有任务中一致
- [ ] 没有任何步骤重新引入 repo 根下固定 `mcc_input.txt`
- [ ] 没有任何步骤重新使用 `pkill -f "MinecraftClient"`
- [ ] 并行 smoke test 明确验证“共享服务器 + 双会话 + 单边 kill 不影响另一边”
- [ ] tmpfs build 任务明确覆盖了当前已知的 `obj` 外置重复编译问题
