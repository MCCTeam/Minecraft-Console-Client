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
- Default validation target when the user does not specify a version: `1.21.11-Vanilla`

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
mc-start 1.21.11-Vanilla
mc-log 1.21.11-Vanilla 100
mc-rcon "op CursorBot"
mc-stop 1.21.11-Vanilla
```

Non-interactive shell:

```bash
tools/start-server.sh 1.21.11-Vanilla
tools/mc-rcon.sh "op CursorBot"
```

If the servers live outside the repo, set `MCC_SERVERS` before sourcing or invoking the tools:

```bash
export MCC_SERVERS=/home/anon/Minecraft/Servers
source tools/mcc-env.sh
```

## Recommended automation recipe

Use this for reproducible local runs:

1. Source `tools/mcc-env.sh`.
2. Pick a concrete server directory, usually `1.21.11-Vanilla`.
3. Copy `MinecraftClient.ini` to a temp location and pin the account, version, and feature gates there.
4. Start the server and wait for `Done (` in the tmux log.
5. Launch MCC with `MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release --no-build -- "<temp-config>"`.
6. Drive MCC through `mcc_input.txt`.
7. Stop MCC and the server cleanly, then keep the temp logs.

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

Add extra `sed -i` edits only for the scenario you are testing.

## Run MCC

Direct temp-config launch:

```bash
cd "$MCC_REPO"
MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release --no-build -- "$CFG" 2>&1
```

Quick manual launch with the repo-root config:

```bash
source tools/mcc-env.sh
mcc-run 25565
```

`mcc-run` is fine for manual smoke tests. Use a temp config for repeatable automation.

## Verify connection and a basic command

MCC output should include:

- `[MCC] Server was successfully joined.`

Server output should include:

- `CursorBot joined the game`

Basic command check:

```bash
echo "inventory player list" >> mcc_input.txt
```

## Typical debug loop

1. `source tools/mcc-env.sh`
2. `dotnet build MinecraftClient.sln -c Release`
3. start `1.21.11-Vanilla`
4. wait for `Done (`
5. launch MCC with a temp config
6. confirm the join in both logs
7. run one MCC command and one server command
8. inspect logs
9. stop both sides and iterate

## Debugging tips

- Protocol mismatches usually show up as a version line such as `Server version : 1.21.11 (protocol vNNN)` before the failure.
- If an early `mc-rcon` command fails, retry it before assuming the server setup is broken.
- If a supposedly isolated run behaves strangely, check `tmux list-sessions` and kill stale `mc-*` sessions first.
- Legacy `1.8` and `1.8.9` servers may need `use-native-transport=false` in `server.properties` on some Linux environments.
- For timing-sensitive work, do not trust wall-clock intuition. Use a real server run and capture evidence from logs or test scripts.
