---
name: mcc-dev-workflow
description: Build, run, and debug Minecraft Console Client (MCC) in WSL. Use when the user wants to compile MCC, start a Minecraft test server, connect MCC to a server, debug MCC protocol issues, or run MCC commands.
---

# MCC Development Workflow

## Project Overview

- Solution: `MinecraftClient.sln` (projects: `MinecraftClient` + `ConsoleInteractive`)
- Build: `dotnet build MinecraftClient.sln -c Release`
- Output: `MinecraftClient/bin/Release/net10.0/MinecraftClient`
- Servers: `MinecraftOfficial/downloads/<version>/` — server.jar + runtime data (config, world, etc.)

Environment: WSL Ubuntu, Java 21, .NET 10 SDK, tmux, python3.

## Compile

```bash
dotnet build MinecraftClient.sln -c Release
```

## Start a Test Server

Servers live in `MinecraftOfficial/downloads/` with directories named by version (e.g. `1.20.6`, `1.21.11`).

```bash
tools/start-server.sh 1.20.6
```

Creates a tmux session `mc-1_20_6` with a named pipe `stdin.pipe` for command input. The server persists across Cursor sessions.

```bash
echo "op CursorBot" > MinecraftOfficial/downloads/1.20.6/stdin.pipe   # server command
tmux capture-pane -t mc-1_20_6 -p -S -50                              # view output
echo "stop" > MinecraftOfficial/downloads/1.20.6/stdin.pipe           # stop
```

### Server config checklist

- `eula.txt`: `eula=true`
- `server.properties`: `online-mode=false` for offline testing

## Run MCC

ConsoleInteractive is patched for non-interactive terminals.

```bash
MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release -- CursorBot - localhost 2>&1
```

- Format: `MinecraftClient <username> <password> <server>`, password `-` = offline mode
- Use `block_until_ms: 0` to background; `sleep 2` then read terminal to confirm join
- Config: `MinecraftClient.ini` (auto-generated). Set `MinecraftVersion = "auto"` unless pinning.

### FileInputBot

Set `MCC_FILE_INPUT=1` (shown above). MCC monitors `mcc_input.txt`:

```bash
echo "inventory player list" >> mcc_input.txt
```

Polled every ~500ms. `sleep 1` then read terminal for response.

### RCON

```bash
tools/mc-rcon.sh "give CursorBot diamond_sword 1"
tools/mc-rcon.sh "op CursorBot"
tools/mc-rcon.sh "say hello" 25575 test123    # explicit port and password
```

## Verify Connection

MCC output: `[MCC] Server was successfully joined.`
Server output: `CursorBot joined the game`

## Server Lifecycle

**Keep the server running** unless you need to restart/switch version/user asks to stop.

Check before starting: `tmux list-sessions 2>/dev/null | grep "^mc-"`

## Typical Debug Workflow

1. `tools/start-server.sh 1.20.6` (background, `block_until_ms: 0`)
2. Wait for "Done": `tmux capture-pane -t mc-1_20_6 -p -S -5`
3. Build: `dotnet build MinecraftClient.sln -c Release`
4. Run MCC: `MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release --no-build -- CursorBot - localhost 2>&1` (background, `block_until_ms: 0`)
5. `sleep 2`, read terminal to confirm join
6. RCON: `tools/mc-rcon.sh "op CursorBot"`
7. MCC cmd: `echo "inventory player list" >> mcc_input.txt`
8. `sleep 1`, read terminal for output
9. `pkill -f MinecraftClient` → rebuild → repeat

| Operation | Typical Duration |
|-----------|-----------------|
| MCC startup → join | ~1s |
| FileInput → response | <500ms |

## Tools

All in `tools/`:

| Script | Purpose |
|--------|---------|
| `start-server.sh <ver>` | Start MC server in tmux |
| `mc-rcon.sh "cmd" [port] [pw]` | RCON command (default: 25575, test123) |
| `decompile.sh --version <ver>` | Decompile MC version + download server.jar |
| `mcc-env.sh` | Source for shell helpers (`mc-start`, `mcc-build`, etc.) |

`mcc-env.sh` exports `$MCC_REPO` and `$MCC_SERVERS` and defines convenience functions. Source it in interactive shells or `~/.bashrc`. In Cursor's non-interactive Shell, use the standalone scripts directly.

## Decompiled Server Source

`MinecraftOfficial/` contains decompiled official server/client code:

```bash
tools/decompile.sh --version 1.21.1                  # server (default)
tools/decompile.sh --version 1.21.1 --side CLIENT    # client
```

Auto-downloads `MinecraftDecompiler.jar` if missing; downloads `server.jar` into `MinecraftOfficial/downloads/<ver>/` for SERVER side.

## Key Code Paths

| Area | Files |
|------|-------|
| Protocol version map | `Protocol/ProtocolHandler.cs` |
| Packet palette (ID mapping) | `Protocol/Handlers/PacketPalettes/PacketPalette*.cs` |
| Core packet handling | `Protocol/Handlers/Protocol18.cs` |
| Data serialization | `Protocol/Handlers/DataTypes.cs` |
| Structured components (1.20.6+) | `Protocol/Handlers/StructuredComponents/` |
| Client logic | `McClient.cs` |
| Config phase packets | `Protocol/Handlers/ConfigurationPacketTypesIn.cs` / `Out.cs` |
| Console I/O library | `ConsoleInteractive/` |

## Debugging Tips

- Debug output: `DebugMessages = true` in `[Logging]` of `MinecraftClient.ini`
- Protocol version shown during connection: `Server version : X.XX.X (protocol vNNN)`
- Server `EncoderException` = protocol mismatch
- Packet reference: https://minecraft.wiki/w/Java_Edition_protocol/Packets
- Use `block_until_ms: 0` for long-running processes, read terminal files for output

## Git Commits

Commit at meaningful milestones. Messages in English with sufficient context.
