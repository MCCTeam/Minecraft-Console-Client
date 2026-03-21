---
name: mcc-integration-testing
description: Repeatable local offline integration testing for Minecraft Console Client against a local Minecraft Java server. Use this whenever the user wants to validate MCC end-to-end against a local server, switch the server to persistent offline mode, run chat or server commands through FileInputBot, exercise inventory/entity handling, or perform deeper smoke testing with mobs, particles, sounds, TNT, and operator actions.
---

# MCC Integration Testing

Use this skill for local MCC validation against the user's `mc-*` and `mcc-*` shell helpers.

## Workflow

1. Source `~/.zshrc` in command invocations.
2. Do not read `~/.zshrc` directly.
3. Ensure the target server is configured for persistent offline testing:
   - `online-mode=false`
   - `enforce-secure-profile=false`
   - `enable-rcon=true`
   - `rcon.password=test123`
4. Build with `mcc-build`.
5. Run the scripted scenario with `scripts/run_full_spectrum_test.sh`.
6. Summarize the evidence with `scripts/summarize_test_run.sh`.

## Required Preconditions

- Server jar exists under `~/Minecraft/Servers/<version>/server.jar`
- `eula.txt` contains `eula=true`
- Repo root `MinecraftClient.ini` is the offline MCC test profile
- The MCC config round-trip issue must be fixed before relying on repeated launches

## Scripts

- `scripts/ensure_offline_server.sh`
  - Generates `server.properties` if missing
  - Applies persistent offline and RCON settings
- `scripts/run_full_spectrum_test.sh`
  - Builds MCC
  - Starts server and MCC
  - Runs the full-spectrum scenario
  - Verifies key MCC and server log assertions
- `scripts/summarize_test_run.sh`
  - Prints the most relevant evidence from the latest run directory

## Scenario Coverage

The scripted run should cover:

- offline join
- MCC-originated chat
- MCC-originated slash command
- internal MCC commands such as `health`, `list`, `inventory`, and `entity`
- OP + creative mode
- passive and hostile mob spawns
- representative sound and particle events
- TNT / explosion handling

If a command syntax needs to be checked, use `references/command-matrix.md`.
