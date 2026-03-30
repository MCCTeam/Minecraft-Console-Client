---
name: mcc-integration-testing
description: >-
  Use when proving MCC behavior on a real local Minecraft server, validating
  runtime or protocol changes end-to-end, exercising movement, physics,
  inventory, entity, chat, or terrain behavior, or running a single-version or
  cross-version regression sweep.
metadata:
  category: discipline
  triggers:
    - integration test
    - real server
    - local server
    - regression sweep
    - rcon
    - tmux
    - offline mode
    - online mode
    - movement
    - physics
    - inventory
    - entity
    - terrain
    - chat
---

# MCC Integration Testing

Use this skill when the task is "prove it on a real server", not just "reason about whether it should work."

Read [references/online-mode.md](references/online-mode.md) when the user asks for Microsoft login, device-code auth, or an online-mode server run. Use [references/command-matrix.md](references/command-matrix.md) for stable MCC-side and RCON-side commands.

## Iron Law

Only say MCC was integration tested when MCC ran against a real local server and the claim is backed by real MCC output plus real server logs.

Calling build-only, reasoning-only, or join-only work "integration tested" is a rules violation, not shorthand.

These do not count as end-to-end proof:

- static reasoning, source comparison, or build success
- join or login success by itself
- a long-lived idle connection by itself
- a grep that only says there were no errors
- testing one shared-route version and silently claiming adjacent versions also passed

If the environment cannot run a real server, say so and report the result as unexecuted or inferred, not integration tested.

## Default target

- Use `1.21.11-Vanilla` unless the user asks for a different version or a version matrix.
- Use `MCC_SERVERS` if it is set. Otherwise the default server root is `MinecraftOfficial/downloads`.

## Guardrails

- Use a real local server.
- Launch MCC against an explicit `localhost:<server-port>` target for repeatable local tests.
- Keep version matrices sequential in shared local environments. The tmux server harness is shared state by default.
- Prefer generated temporary MCC configs for scripted runs so one test does not contaminate the next.
- Default to offline auth in generated temp configs. Do not trust the repo-root `MinecraftClient.ini` account defaults.
- If the user explicitly asks for Microsoft online login, honor that request and generate the temp config for Microsoft auth instead of offline mode.
- For Microsoft auth, prefer an interactive TTY launch with `BasicIO-NoColor` so the device code is easy to read and relay to the user.
- Do not use file-input mode during Microsoft auth. Launch interactively first, complete login, then switch to scripted control only if needed.
- For online-mode tests, prefer a clean temp config with no join-time bots or scheduled tasks. Inherited `ScriptScheduler` or `DiscordRpc` settings can pollute the session and send unintended chat right after login.
- Legacy and modern command syntax differ. Do not assume one server-command profile fits every version.
- Use actual MCC output and actual server logs for assertions. Do not invent success strings.
- Treat server `Done` as startup progress, not RCON readiness. Retry the first RCON command before assuming the setup is broken.
- Run preflight before scripted test loops. On macOS, Java may be installed but not exported on PATH in the shell the harness uses.
- If a change touches shared routing or a version range, test at least one adjacent version that shares that path, or explicitly mark adjacent versions as unexecuted and inferred.
- For palette or version-content changes, probe at least one neighboring or existing item, entity, or block. Do not only check the headline addition.
- Separate product failures from harness failures. Missing logs, stale tmux state, stale `stdin.pipe`, or pre-join `Connection refused` errors are usually environment problems until proven otherwise.

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

Broad validation should usually cover `connect-test`, `item-test`, `entity-test`, `terrain-test`, and `chat-test`.

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

0. run preflight and clear stale shared state when the environment is reused
1. configure the target server for offline testing
2. ensure `eula=true`
3. ensure RCON is enabled
4. build MCC unless the task explicitly reuses a fresh build

Preflight and reset helpers:

```bash
.skills/mcc-integration-testing/scripts/preflight_test_env.sh 1.21.11-Vanilla
.skills/mcc-integration-testing/scripts/reset_shared_test_state.sh 1.21.11-Vanilla
```

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
- `.skills/mcc-integration-testing/scripts/preflight_test_env.sh`
  - verifies Java, tmux, dotnet, python3, server directories, and resolves common Java PATH issues
- `.skills/mcc-integration-testing/scripts/reset_shared_test_state.sh`
  - clears stale tmux sessions and stale `stdin.pipe` files before a rerun
- `.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh`
  - generates a clean temporary MCC config, prepares offline login by default, disables noisy bots, and can switch to Microsoft auth when explicitly requested
- `.skills/mcc-integration-testing/scripts/get_server_port.sh`
  - resolves the actual local server port from `server.properties` or the latest server log
- `.skills/mcc-integration-testing/scripts/run_full_spectrum_test.sh`
  - single-version deep smoke with built-in assertions
- `.skills/mcc-integration-testing/scripts/summarize_test_run.sh`
  - summarize the latest full-spectrum run
- `tools/run-creative-e2e.sh`
  - ordered creative-mode E2E regression scenario

## Evidence Discipline

In every report, separate:

- `Executed`: exact scripts, commands, versions, auth mode, and whether the run was sequential or single-version
- `Observed`: exact MCC output, exact server-log evidence, and the saved log directory
- `Inferred`: conclusions not directly shown by that run's runtime evidence
- `Harness issues`: setup or runner problems such as missing Java on PATH, stale tmux sessions, stale `stdin.pipe`, missing log artifacts, or failed config generation

Never upgrade inferred claims to observed facts. Absence of errors is supporting evidence only; pair it with a positive assertion for the feature under test.

## Red Flags

Stop and fix the test plan if you are about to:

- claim movement, inventory, entity, terrain, physics, or chat coverage from join success alone
- reuse repo-root `MinecraftClient.ini` or another user-local stateful config
- run multi-version tests in parallel in a shared tmux or shared server environment
- let inherited bots, schedulers, or other user-local noise send chat or commands during validation

## What to report back

Always summarize:

- which version or versions were tested
- which port or ports were used
- which auth mode and scenario were used
- whether the run was sequential or single-version
- the exact scripts or commands executed
- pass or fail per major phase
- concrete evidence from MCC and server logs
- the saved log directory
- what was not executed and what remains inferred
- which adjacent versions were not run but were mentioned

## When Not to Use

- build-only verification
- static protocol or source comparison with no real server run
- documentation or prompt work
- code review requests that do not ask for executed runtime proof

## Troubleshooting

- If the first RCON command fails, retry it before assuming the setup is broken.
- If Java is installed but the harness still says it is missing, run `preflight_test_env.sh`. This resolves common Homebrew Java paths on macOS.
- If MCC reaches Microsoft device-code login during an offline test, stop and inspect the generated temp config before retrying.
- If the user explicitly requests Microsoft online login, set `MCC_TEST_ACCOUNT_TYPE=microsoft` before launching the harness.
- If the user explicitly requests Microsoft online login, use `BasicIO-NoColor` in a real TTY, relay the device code from the TUI, and avoid pressing empty Enter at any auth prompt.
- If the online-mode session sends unexpected chat or commands right after join, inspect inherited bot settings first. `ChatBot.ScriptScheduler` tasks and `ChatBot.DiscordRpc` are common sources of test noise in user-local configs.
- If `dotnet run` cannot see an existing Microsoft session, check whether `SessionCache.db` and `ProfileKeyCache.ini` need to be synced from `MinecraftClient/bin/Release/net10.0/` to the repo root.
- If Microsoft auth keeps prompting even with a valid session cache, verify `Account.Login` matches the cached username exactly.
- If MCC reports `Connection refused`, verify the launched target matches the server's actual `server-port`.
- If MCC reports `Connection refused` immediately after a server start, also check for stale shared state: old tmux sessions, a stale `stdin.pipe`, or a server that never actually reached `Done (`.
- If multiple versions are being tested, do not start them in parallel unless the harness isolates tmux sessions and input files.
- If a test assertion fails, inspect the real MCC output before changing the code or weakening the assertion.
- If an older server behaves oddly on Linux, check `use-native-transport=false` in `server.properties`.
- If a matrix row fails before producing `mcc.log` or a command transcript, treat it as a harness failure, fix the environment, and rerun that row before drawing product conclusions.
