---
name: mcc-integration-testing
description: Repeatable real-server integration testing for Minecraft Console Client against a local Minecraft Java server in offline mode or Microsoft online mode. Use this whenever the user wants to confirm nothing broke, validate runtime or protocol changes end-to-end, exercise movement, physics, inventory, entity handling, or run a single-version or cross-version MCC regression sweep on a real server.
---

# MCC Integration Testing

Use this skill when the task is "prove it still works on a real server", not just "reason about whether it should work."

Read [references/online-mode.md](references/online-mode.md) when the user asks for Microsoft login, device-code auth, or an online-mode server run.

## Default target

- Use `1.21.11-Vanilla` unless the user asks for a different version or a version matrix.
- Use `MCC_SERVERS` if it is set. Otherwise the default server root is `MinecraftOfficial/downloads`.

## Guardrails

- Use a real local server.
- Keep version matrices sequential in shared local environments. The tmux server harness is shared state by default.
- Prefer temporary MCC configs for scripted runs so one test does not contaminate the next.
- Default to offline auth in generated temp configs. Do not trust the repo-root `MinecraftClient.ini` account defaults.
- If the user explicitly asks for Microsoft online login, honor that request and generate the temp config for Microsoft auth instead of offline mode.
- For Microsoft auth, prefer an interactive TTY launch with `BasicIO-NoColor` so the device code is easy to read and relay to the user.
- Do not use file-input mode during Microsoft auth. Launch interactively first, complete login, then switch to scripted control only if needed.
- For online-mode tests, prefer a clean temp config with no join-time bots or scheduled tasks. Inherited `ScriptScheduler` or `DiscordRpc` settings can pollute the session and send unintended chat right after login.
- Legacy and modern command syntax differ. Do not assume one server-command profile fits every version.
- Use actual MCC output and actual server logs for assertions. Do not invent success strings.
- Launch MCC against an explicit `localhost:<server-port>` target for repeatable local tests.

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

By default, the config helper prepares offline auth. To opt into another auth mode for a specific run, set:

```bash
MCC_TEST_ACCOUNT_TYPE=microsoft
MCC_TEST_PASSWORD=
```

Optionally override the login name with the fourth argument to the config helper.

## Scripts and tools

- `.skills/mcc-integration-testing/scripts/ensure_offline_server.sh`
  - configures persistent offline mode and RCON
- `.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh`
  - copies `MinecraftClient.ini`, prepares offline login by default, and can switch to Microsoft auth when explicitly requested
- `.skills/mcc-integration-testing/scripts/get_server_port.sh`
  - resolves the actual local server port from `server.properties` or the latest server log
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
- If MCC reaches Microsoft device-code login during an offline test, stop and inspect the generated temp config before retrying.
- If the user explicitly requests Microsoft online login, set `MCC_TEST_ACCOUNT_TYPE=microsoft` before launching the harness.
- If the user explicitly requests Microsoft online login, use `BasicIO-NoColor` in a real TTY, relay the device code from the TUI, and avoid pressing empty Enter at any auth prompt.
- If the online-mode session sends unexpected chat or commands right after join, inspect inherited bot settings first. `ChatBot.ScriptScheduler` tasks and `ChatBot.DiscordRpc` are common sources of test noise in user-local configs.
- If `dotnet run` cannot see an existing Microsoft session, check whether `SessionCache.db` and `ProfileKeyCache.ini` need to be synced from `MinecraftClient/bin/Release/net10.0/` to the repo root.
- If Microsoft auth keeps prompting even with a valid session cache, verify `Account.Login` matches the cached username exactly.
- If MCC reports `Connection refused`, verify the launched target matches the server's actual `server-port`.
- If multiple versions are being tested, do not start them in parallel unless the harness isolates tmux sessions and input files.
- If a test assertion fails, inspect the real MCC output before changing the code or weakening the assertion.
- If an older server behaves oddly on Linux, check `use-native-transport=false` in `server.properties`.
- If a test should be repeatable, avoid mutating the repo-root `MinecraftClient.ini`.
