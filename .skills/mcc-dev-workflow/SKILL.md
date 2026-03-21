---
name: mcc-development-workflow
description: Documentation of the typical development workflow for Minecraft Console Client (MCC), including project structure, build commands, and debugging steps.
---

# MCC Development Workflow

## Project Overview
- Repo: `~/Minecraft/Minecraft-Console-Client` (env var `$MCC_REPO`)
- Solution: `MinecraftClient.sln` (projects: `MinecraftClient` + `ConsoleInteractive`)
- Build: `dotnet build MinecraftClient.sln -c Release`
- Servers: `~/Minecraft/Servers/` (env var `$MCC_SERVERS`)

## Typical Debug Workflow
1. `~/Minecraft/Servers/start-server.sh 1.20.6-Vanilla` (background)
2. Wait for "Done" in server output
3. Build: `dotnet build $MCC_REPO/MinecraftClient.sln -c Release`
4. Run MCC: `cd $MCC_REPO && MCC_FILE_INPUT=1 dotnet run --project MinecraftClient -c Release -- CursorBot - localhost 2>&1`
5. RCON: `mc-rcon "op CursorBot"`
6. MCC cmd: `echo "inventory player list" >> $MCC_REPO/mcc_input.txt`
7. Read terminal file to see output
8. Kill MCC → rebuild → repeat

## Timing Reference
| Operation | Typical Duration |
|-----------|-----------------|
| MCC startup → join server | ~1s |
| FileInput command → response | <500ms |

## Official Minecraft Server Source (Decompiled)
`$MCC_REPO/MinecraftOfficial/` contains decompiled official server code for protocol reference.
When investigating protocol details (packet structure, field order, NBT format, etc.),
look at the corresponding version's decompiled source as authoritative reference.
