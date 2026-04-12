---
name: mcc-dev-workflow
description: Build, run, and debug Minecraft Console Client (MCC) against a real local Minecraft Java server on Linux, macOS, or WSL. Use this whenever the user wants to compile MCC, start or inspect a local test server, connect MCC to a server, debug protocol or login issues, validate a code change end-to-end, or run MCC commands on a real server instead of guessing from static code.
---

# MCC Development Workflow

Use this skill when the task needs a real local server loop, not just code reading.

## Defaults

- Solution: `MinecraftClient.sln`
- Runtime target: `.NET 10` / `net10.0`
- Environment: Linux, macOS, or WSL with Java, tmux, python3, and dotnet available
- Default server root after `source tools/mcc-env.sh`: `${MCC_SERVERS:-<repo>/MinecraftOfficial/downloads}`
- Default validation target when the user does not specify a version: `1.21.11`

## Console modes

MCC supports two console modes selectable via `ConsoleMode` in `[Console.General]`:

| Mode | Backend | Best for |
|------|---------|----------|
| `classic` | `ClassicConsoleBackend` (ConsoleInteractive) | Normal use, legacy CI/scripts, `FileInput` mode |
| `tui` | `TuiConsoleBackend` (Avalonia/Consolonia) | Full-screen TUI with scrollable log, command input, popup inventory |

Both modes support the same commands and input/output through `ConsoleIO.Backend`. The mode is determined at startup from config; `BasicIO` CLI arg overrides to simple stdio.

## Core rules

- Prefer a real local server over static reasoning for protocol, login, movement, inventory, entity, or command-path work.
- Treat tmux `mc-*` sessions as shared state. Do not run multi-version server workflows in parallel unless the harness explicitly isolates them.
- For scripted or repeatable runs, use a generated temporary config. Do not edit the repo-root `MinecraftClient.ini` as part of the test loop.
- A server log line containing `Done (` means startup finished. It does not guarantee that RCON is ready on the first attempt. Retry early `mc-rcon` commands.
- When instructions, docs, and code disagree, trust current code and current tool behavior first.

## Shared server, isolated MCC sessions

- `mc-*` commands operate on the shared local Minecraft server.
- `mcc-*` commands operate on one MCC client session.
- The default `session` is the current worktree name.
- The default username is derived from `session`, unless you pass `--username`.
- Session files live under `${TMPDIR:-/tmp}/mcc-debug/<session>/`.
- `MCC_SERVERS` stays the shared server-root override.

Keep shared servers running by default. Do not stop or reset them unless the user explicitly asks for that, or you need to switch server versions.

Two worktrees can debug against one shared server like this:

```bash
# worktree A
cd ~/Minecraft/Minecraft-Console-Client
source tools/mcc-env.sh
mc-start 1.21.11
mcc-debug -v 1.21.11 --file-input

# worktree B
cd ~/Minecraft/Minecraft-Console-Client-foo
source tools/mcc-env.sh
mcc-debug -v 1.21.11 --file-input

# from each worktree, mcc-* targets that worktree's default session
mcc-state
```

If you want two MCC sessions from the same worktree, pass `--session NAME` explicitly.

## tmpfs build mode

Use this on machines with enough RAM when you want worktree-isolated builds outside the repo tree:

```bash
source tools/mcc-env.sh
export MCC_BUILD_MODE=tmpfs
mcc-build
mcc-build-clean
```

`MCC_BUILD_MODE=tmpfs` redirects build output to `/dev/shm/mcc-build/<worktree>/` on Linux, or `${TMPDIR:-/tmp}/mcc-build/<worktree>/` if `/dev/shm` is unavailable.

## Preflight and reset

Before scripted runs, especially on macOS or in a reused tmux environment:

```bash
source tools/mcc-env.sh
mcc-preflight 1.21.11
mc-reset-test-env 1.21.11
```

`mcc-preflight` checks Java, tmux, dotnet, python3, and server directories. It also resolves common Homebrew Java paths on macOS. `mc-reset-test-env` clears stale tmux sessions and stale `stdin.pipe` files before they turn into misleading startup failures.

## Build

```bash
source tools/mcc-env.sh
mcc-build
```

Use `mcc-build` for normal local development so any `MCC_BUILD_MODE=tmpfs` routing stays active. Only use raw `dotnet build` when you are intentionally debugging the build system itself.

## Server management

Interactive shell:

```bash
source tools/mcc-env.sh
SESSION="$(_mcc_resolve_session)"
USERNAME="$(_mcc_resolve_username "$SESSION")"
mc-start 1.21.11
mc-log 1.21.11 100
mc-rcon "op $USERNAME"
mc-stop 1.21.11
```

Non-interactive shell:

```bash
tools/start-server.sh 1.21.11
tools/mc-rcon.sh "op mcc_smoke_a"
```

If the servers live outside the repo, set `MCC_SERVERS` before sourcing or invoking the tools:

```bash
export MCC_SERVERS=/home/anon/Minecraft/Servers
source tools/mcc-env.sh
```

## One-step debug session (recommended)

The `tools/mcc-debug.sh` script handles build, server startup, config preparation, and MCC launch in one step:

```bash
source tools/mcc-env.sh

# Classic mode with FileInput (script-driven debugging):
mcc-debug -v 1.21.11 --file-input

# Classic mode interactive (attach via tmux):
mcc-debug -v 1.21.11

# TUI mode:
mcc-debug -v 1.21.11 -m tui

# With debug messages enabled from start:
mcc-debug -v 1.21.11 --file-input --debug-on

# Skip build (already built):
mcc-debug -v 1.21.11 --file-input --no-build
```

### What mcc-debug.sh does

1. Builds MCC (unless `--no-build`)
2. Creates a clean temp config at `${TMPDIR:-/tmp}/mcc-debug/<session>/MinecraftClient.debug.ini`
3. Ensures server is running (starts if not, waits for `Done (`)
4. Launches MCC in a session-scoped tmux session and session-scoped log/input/pid files

### After launch

- **FileInput mode**: drive MCC via `mcc-cmd --session smoke-a "debug state"`, or just `mcc-cmd "debug state"` from the same worktree
- **Interactive/TUI mode**: attach with `tmux attach -t mcc-<session>`
- **Logs**: `mcc-log-mcc --session smoke-a` or `tail -f "${TMPDIR:-/tmp}/mcc-debug/<session>/mcc-debug.log"`
- **Server RCON**: grant op or gamemode to the username derived from that session

## Debug commands (in-game)

### `/debug [on|off]`

Toggles debug logging. Now correctly syncs both `Settings.Config.Logging.DebugMessages` and `McClient.Log.DebugEnabled`.

### `/debug state`

Prints a one-shot summary of MCC's internal state:

```
=== MCC Debug State ===
Server:    localhost:25565
Username:  mcc_smoke_a
Protocol:  774
GameMode:  1
Health:    20.0
Food:      20
Location:  0.50, 80.00, 0.50
TPS:       20.0
Console:   ClassicConsoleBackend    (or TuiConsoleBackend)
Features:  Terrain Inventory Entity
Debug:     ON
Bots (3):  AutoFishing, FileInputBot, ScriptScheduler
Players:   2 online
```

This works in both classic and TUI modes.

## Classic mode debugging

### Agent workflow (FileInput mode)

For agents calling MCC commands programmatically:

```bash
source tools/mcc-env.sh
SESSION="smoke-a"
mcc-debug -v 1.21.11 --file-input --session "$SESSION" --no-build

# Send commands:
mcc-cmd --session "$SESSION" "debug state"
mcc-cmd --session "$SESSION" "inventory player list"
mcc-cmd --session "$SESSION" "entity"

# Check results:
mcc-log-mcc --session "$SESSION"

# Stop:
mcc-cmd --session "$SESSION" "quit"
mcc-kill --session "$SESSION"
mc-stop 1.21.11
```

### Interactive workflow

```bash
source tools/mcc-env.sh
SESSION="live-a"
mcc-debug -v 1.21.11 --session "$SESSION"

# In another terminal:
tmux attach -t "mcc-$SESSION"
# Type commands directly in MCC console
```

## TUI mode debugging

TUI mode runs Consolonia full-screen in a tmux session. Key differences:

1. **No pipe/redirect**: TUI needs a real tty. Cannot `| tee` or redirect stdout.
2. **Log output is in-screen**: all output appears in the scrollable log area.
3. **Keyboard shortcuts**: PageUp/PageDown scroll, ESC exits.
4. **`/debug state`**: the primary way to inspect internal state since external log tailing is not available.
5. **Dialog windows**: `/inventui` opens as an overlay dialog instead of a separate screen.

### Agent workflow for TUI mode

```bash
source tools/mcc-env.sh
SESSION="tui-a"
mcc-debug -v 1.21.11 -m tui --session "$SESSION" --no-build

# Cannot use mcc-cmd (no FileInput); must use tmux send-keys:
tmux send-keys -t "mcc-$SESSION" "/debug state" Enter

# Read TUI screen:
tmux capture-pane -t "mcc-$SESSION" -p -S -30

# Stop:
tmux send-keys -t "mcc-$SESSION" Escape
```

**Caveat with tmux send-keys and Consolonia**: When sending text containing `/`, the Enter key may need to be sent separately:
```bash
tmux send-keys -t "mcc-$SESSION" "/inventory player list"
tmux send-keys -t "mcc-$SESSION" Enter
```

## mcc-env.sh quick reference

After `source tools/mcc-env.sh`:

| Function | Description |
|----------|-------------|
| `mc-start VER` | Start MC server in tmux |
| `mc-stop VER` | Graceful stop via stdin pipe |
| `mc-log VER [N]` | Capture last N lines of server output |
| `mc-rcon "CMD"` | Send RCON command |
| `mc-kill VER` | Force-kill server tmux session |
| `mc-list` | List running MC server sessions |
| `mc-wait-ready VER [SEC]` | Wait for server `Done (` |
| `mc-wait-stop VER [SEC]` | Wait for server shutdown, with force-kill fallback |
| `mc-reset-test-env [--all|VER...]` | Reset shared tmux server state and stale pipes |
| `mcc-build` | Build MCC |
| `mcc-publish --rid <RID>` | Publish MCC with the repo's CI-like defaults |
| `mcc-build-clean` | Clear the current worktree's build output |
| `mcc-run [--session NAME] [--username NAME] [--port PORT]` | Convenience wrapper for `mcc-debug --file-input --no-build` |
| `mcc-tui [--session NAME] [--username NAME] [--port PORT]` | Convenience wrapper for `mcc-debug -m tui --no-build` |
| `mcc-cmd [--session NAME] "CMD"` | Append a command to one session's input file |
| `mcc-kill [--session NAME]` | Kill one MCC process and session |
| `mcc-debug [OPTS]` | One-step debug session (see above) |
| `mcc-log-mcc [--session NAME]` | Tail one MCC debug log |
| `mcc-state [--session NAME]` | Send `debug state` and print the last 30 log lines |
| `mcc-preflight [VER...]` | Verify Java, tmux, dotnet, python3, and server dirs |

## Temporary config recipe

```bash
source tools/mcc-env.sh
SESSION="smoke-a"
USERNAME="$(_mcc_resolve_username "$SESSION")"
CFG="$(_mcc_session_root "$SESSION")/MinecraftClient.debug.ini"
mkdir -p "$(_mcc_session_root "$SESSION")"
bash "$MCC_REPO/.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh" \
  "$CFG" \
  "1.21.11" \
  "$USERNAME"
```

For TUI mode, also add:
```bash
sed -i 's/ConsoleMode = "classic"/ConsoleMode = "tui"/' "$CFG"
```

## Verify connection and a basic command

MCC output should include:

- `[MCC] Server was successfully joined.`

Server output should include the session-derived username, for example:

- `mcc_smoke_a joined the game`

Basic command check:

```bash
mcc-cmd --session smoke-a "inventory player list"
```

If a scripted run fails before MCC joins, check for a harness problem before assuming a product regression. Missing `mcc.log`, a pre-join `Connection refused`, or a server that never reached `Done (` usually means shared-state cleanup or startup failed.

## Typical debug loop

1. `source tools/mcc-env.sh`
2. `mcc-debug -v 1.21.11 --file-input` (or `-m tui`)
3. Confirm `Server was successfully joined` in log
4. `mcc-cmd "debug state"` to verify MCC state
5. Run test commands
6. Inspect log output
7. `mcc-cmd "quit"` and `mc-stop 1.21.11`
8. Edit code, rebuild, repeat

## Debugging tips

- **`/debug state` is your primary diagnostic tool** in both modes. Use it first to verify connection, mode, and feature flags.
- **`/debug on` now correctly enables debug logging** at runtime. Previous versions had a bug where `Log.DebugEnabled` was not synced.
- Protocol mismatches usually show up as a version line such as `Server version : 1.21.11 (protocol vNNN)` before the failure.
- If an early `mc-rcon` command fails, retry it before assuming the server setup is broken.
- If a supposedly isolated run behaves strangely, check `tmux list-sessions` and kill stale `mc-*` sessions first.
- Legacy `1.8` and `1.8.9` servers may need `use-native-transport=false` in `server.properties` on some Linux environments.
- For timing-sensitive work, do not trust wall-clock intuition. Use a real server run and capture evidence from logs or test scripts.
- **TUI mode tip**: if the terminal becomes unresponsive after a crash, run `stty sane && reset` to restore it.
- **tmux capture trick**: `tmux capture-pane -t mcc-<session> -p -S -50` captures the last 50 lines of a tmux session without attaching.

## Tool files

| File | Purpose |
|------|---------|
| `tools/mcc-env.sh` | Shell functions for server/MCC management |
| `tools/mcc-debug.sh` | One-step debug session launcher |
| `tools/mcc-log-tail.sh` | Log tailing for MCC and/or server |
| `tools/start-server.sh` | Server lifecycle in tmux |
| `tools/mc-rcon.sh` | RCON command sender |
| `tools/run-creative-e2e.sh` | Full creative mode end-to-end test |
