---
name: mcc-integration-testing
description: Repeatable real-server integration testing for Minecraft Console Client against a local offline Minecraft Java server. Use this whenever the user wants to confirm nothing broke, validate runtime or protocol changes end-to-end, exercise movement, physics, inventory, entity handling, or run a single-version or cross-version MCC regression sweep on a real server.
---

# MCC Integration Testing

Use this skill when the task is "prove it still works on a real server", not just "reason about whether it should work."

## Default target

- Use `1.21.11-Vanilla` unless the user asks for a different version or a version matrix.
- Use `MCC_SERVERS` if it is set. Otherwise the default server root is `MinecraftOfficial/downloads`.

## Guardrails

- Use a real local server.
- Keep version matrices sequential in shared local environments. The tmux server harness is shared state by default.
- Prefer temporary MCC configs for scripted runs so one test does not contaminate the next.
- Legacy and modern command syntax differ. Do not assume one server-command profile fits every version.
- Use actual MCC output and actual server logs for assertions. Do not invent success strings.

## Choose the test mode

### 1. Single-version deep smoke

Use this when one supported version is enough and you want broad coverage:

```bash
.skills/mcc-integration-testing/scripts/run_full_spectrum_test.sh 1.21.11-Vanilla
```

This covers join, chat, slash commands, internal MCC commands, creative inventory, entity handling, sounds, particles, TNT, kill/respawn, and log assertions.

### 2. Ordered creative-mode E2E

Use this when the user asks for a regression sweep in a strict scenario order such as:

- connect
- send messages
- send commands
- receive messages
- movement
- physics
- mobs
- effects
- inventory

Command:

```bash
MCC_SERVERS=/home/anon/Minecraft/Servers bash tools/run-creative-e2e.sh 1.21.11-Vanilla 1.21.11 modern
```

For legacy targets such as `1.8` or `1.8.9`, switch the final argument to `legacy` and pass the pinned MC version.

### 3. Timing or cadence validation

Use this for TPS, movement-cadence, or packet-cadence work:

- `MinecraftClient/config/sample-script-tick-counter.cs`
- `MinecraftClient/config/sample-script-packet-capture.cs`

Run them against a real server with a temp config and summarize counts from the captured logs.

## Preconditions

Before running any scenario:

1. configure the target server for offline testing
2. ensure `eula=true`
3. ensure RCON is enabled
4. build MCC unless the task explicitly reuses a fresh build

Offline configuration helper:

```bash
.skills/mcc-integration-testing/scripts/ensure_offline_server.sh 1.21.11-Vanilla
```

## Scripts and tools

- `.skills/mcc-integration-testing/scripts/ensure_offline_server.sh`
  - configures persistent offline mode and RCON
- `.skills/mcc-integration-testing/scripts/run_full_spectrum_test.sh`
  - single-version deep smoke with built-in assertions
- `.skills/mcc-integration-testing/scripts/summarize_test_run.sh`
  - summarize the latest full-spectrum run
- `tools/run-creative-e2e.sh`
  - ordered creative-mode E2E regression scenario

## What to report back

Always summarize:

- which version or versions were tested
- which scenario was used
- whether the run was sequential or single-version
- pass or fail per major phase
- concrete evidence from MCC and server logs
- the saved log directory

## Troubleshooting

- If the first RCON command fails, retry it before assuming the setup is broken.
- If multiple versions are being tested, do not start them in parallel unless the harness isolates tmux sessions and input files.
- If a test assertion fails, inspect the real MCC output before changing the code or weakening the assertion.
- If an older server behaves oddly on Linux, check `use-native-transport=false` in `server.properties`.
- If a test should be repeatable, avoid mutating the repo-root `MinecraftClient.ini`.
