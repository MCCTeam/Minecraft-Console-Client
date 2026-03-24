---
name: mcc-dev-workflow
description: Build, run, and debug Minecraft Console Client (MCC) against a real local Minecraft Java server in WSL. Use this whenever the user wants to compile MCC, start or inspect a local test server, connect MCC to a server, debug protocol or login issues, validate a code change end-to-end, or run MCC commands on a real server instead of guessing from static code.
---

# MCC Development Workflow

Use this skill when the task needs a real local server loop, not just code reading.

## Defaults

- Solution: `MinecraftClient.sln`
- Runtime target: `.NET 10` / `net10.0`
- Environment: WSL Ubuntu, Java 21, tmux, python3
- Default server root: `${MCC_SERVERS:-$MCC_REPO/MinecraftOfficial/downloads}`
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
- For scripted or repeatable runs, prefer a temporary config copied from `MinecraftClient.ini`. Use the repo-root config only for ad hoc manual work.
- A server log line containing `Done (` means startup finished. It does not guarantee that RCON is ready on the first attempt. Retry early `mc-rcon` commands.
- When instructions, docs, and code disagree, trust current code and current tool behavior first.

## Build

```bash
dotnet build MinecraftClient.sln -c Release
```

## Server management

Interactive shell:

```bash
source tools/mcc-env.sh
mc-start 1.21.11
mc-log 1.21.11 100
mc-rcon "op CursorBot"
mc-stop 1.21.11
```

Non-interactive shell:

```bash
tools/start-server.sh 1.21.11
tools/mc-rcon.sh "op CursorBot"
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
2. Creates a temp config at `/tmp/mcc-debug/MinecraftClient.debug.ini` with CursorBot account, Terrain/Inventory/Entity enabled
3. Ensures server is running (starts if not, waits for `Done (`)
4. Launches MCC in the specified mode

### After launch

- **FileInput mode**: drive MCC via `mcc-cmd "debug state"` or `echo "debug state" >> mcc_input.txt`
- **Interactive/TUI mode**: attach with `tmux attach -t mcc-debug`, type commands directly
- **Logs**: `tail -f /tmp/mcc-debug/mcc-debug.log` (FileInput mode only; TUI/interactive mode outputs to tmux)
- **Server RCON**: `mc-rcon "op CursorBot"`, `mc-rcon "gamemode creative CursorBot"`

## Debug commands (in-game)

### `/debug [on|off]`

Toggles debug logging. Now correctly syncs both `Settings.Config.Logging.DebugMessages` and `McClient.Log.DebugEnabled`.

### `/debug state`

Prints a one-shot summary of MCC's internal state:

```
=== MCC Debug State ===
Server:    localhost:25565
Username:  CursorBot
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
mcc-debug -v 1.21.11 --file-input --no-build

# Send commands:
mcc-cmd "debug state"
mcc-cmd "inventory player list"
mcc-cmd "entity"

# Check results:
tail -20 /tmp/mcc-debug/mcc-debug.log

# Stop:
mcc-cmd "quit"
mc-stop 1.21.11
```

### Interactive workflow

```bash
source tools/mcc-env.sh
mcc-debug -v 1.21.11

# In another terminal:
tmux attach -t mcc-debug
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
mcc-debug -v 1.21.11 -m tui --no-build

# Cannot use mcc-cmd (no FileInput); must use tmux send-keys:
tmux send-keys -t mcc-debug "/debug state" Enter

# Read TUI screen:
tmux capture-pane -t mcc-debug -p -S -30

# Stop:
tmux send-keys -t mcc-debug Escape
```

**Caveat with tmux send-keys and Consolonia**: When sending text containing `/`, the Enter key may need to be sent separately:
```bash
tmux send-keys -t mcc-debug "/inventory player list"
tmux send-keys -t mcc-debug Enter
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
| `mcc-build` | Build MCC |
| `mcc-run [PORT]` | Run MCC classic+FileInput on port |
| `mcc-tui [PORT]` | Run MCC TUI mode in tmux |
| `mcc-cmd "CMD"` | Append command to mcc_input.txt |
| `mcc-kill` | Kill MCC process and debug session |
| `mcc-debug [OPTS]` | One-step debug session (see above) |
| `mcc-log-mcc` | Tail MCC debug log |
| `mcc-state` | Send `debug state` and print last 30 log lines |

## Temporary config recipe

```bash
source tools/mcc-env.sh
TEST_ROOT="${TMPDIR:-/tmp}/mcc-dev"
CFG="$TEST_ROOT/MinecraftClient.1.21.11.ini"
mkdir -p "$TEST_ROOT"
cp "$MCC_REPO/MinecraftClient.ini" "$CFG"
sed -i \
  -e 's/Account = { Login = "test", Password = "-" }/Account = { Login = "CursorBot", Password = "-" }/' \
  -e 's/MinecraftVersion = "auto"/MinecraftVersion = "1.21.11"/' \
  -e 's/TerrainAndMovements = false/TerrainAndMovements = true/' \
  -e 's/InventoryHandling = false/InventoryHandling = true/' \
  -e 's/EntityHandling = false/EntityHandling = true/' \
  "$CFG"
```

For TUI mode, also add:
```bash
sed -i 's/ConsoleMode = "classic"/ConsoleMode = "tui"/' "$CFG"
```

## Verify connection and a basic command

MCC output should include:

- `[MCC] Server was successfully joined.`

Server output should include:

- `CursorBot joined the game`

Basic command check:

```bash
mcc-cmd "inventory player list"
```

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
- **tmux capture trick**: `tmux capture-pane -t mcc-debug -p -S -50` captures the last 50 lines of a tmux session without attaching.

## Tool files

| File | Purpose |
|------|---------|
| `tools/mcc-env.sh` | Shell functions for server/MCC management |
| `tools/mcc-debug.sh` | One-step debug session launcher |
| `tools/mcc-log-tail.sh` | Log tailing for MCC and/or server |
| `tools/start-server.sh` | Server lifecycle in tmux |
| `tools/mc-rcon.sh` | RCON command sender |
| `tools/run-creative-e2e.sh` | Full creative mode end-to-end test |
