# Shared Server, Isolated MCC Sessions Design

## Goal

Keep local Minecraft server instances shared across worktrees while making MCC debug sessions fully isolated by default.

The desired workflow is:

- Multiple Git worktrees can build in parallel without output collisions.
- One shared local test server can be reused by multiple MCC clients.
- Multiple MCC debug sessions can connect to that shared server at the same time without clobbering each other's tmux sessions, logs, temp config, input file, PID tracking, or usernames.

## Background

The current repository tooling mixes two different scopes:

- Shared infrastructure state, such as local server jars and tmux server sessions
- Per-MCC-client debug state, such as `mcc_input.txt`, `/tmp/mcc-debug`, and the fixed `mcc-debug` tmux session

That works for one active workspace, but it breaks down once multiple worktrees are used at the same time.

Today the main problems are:

1. `MCC_REPO` can leak from one shell into another and point helper commands at the wrong worktree.
2. `mcc-debug.sh` writes all temp artifacts into the same `/tmp/mcc-debug` directory.
3. MCC tmux sessions use the same fixed name, `mcc-debug`.
4. MCC helper commands such as `mcc-kill` operate globally instead of targeting one debug session.
5. The default MCC username is fixed, so two clients connecting to the same server will kick each other.
6. Build outputs live inside each worktree by default, which is correct for isolation, but the workflow does not offer an intentional tmpfs-backed fast path for machines with large RAM.

## Non-Goals

- Do not isolate or duplicate server assets by worktree.
- Do not support multiple independent servers with the same version name running in parallel in this change.
- Do not redesign the Minecraft runtime or MCC account model.
- Do not make tmpfs build output mandatory.

## Design Principles

- Shared resources stay explicit and few.
- Per-session MCC state is isolated by default.
- Repo discovery is local to each script, not inherited from an ambient shell variable.
- Existing workflows should keep working with minimal changes when only one MCC session is active.
- Performance optimizations must not undermine correctness or isolation.

## State Model

### Shared State

The following remain shared across worktrees and shells:

- `MCC_SERVERS`, when explicitly set by the user
- Default server root at `<repo>/MinecraftOfficial/downloads` when `MCC_SERVERS` is not set
- Server tmux session names, still keyed by Minecraft version, for example `mc-1_21_11`
- Server stdin pipes and world data under the shared server root
- RCON access to the shared server

### Per-Session MCC State

Each MCC debug session is keyed by a session identifier.

The following must be isolated per session:

- MCC tmux session
- Temp config
- MCC log
- MCC input file
- MCC PID file
- Session metadata used by helper commands
- Default username

### Per-Worktree Build State

Each worktree gets its own build output root.

Build isolation is keyed by worktree name, not MCC session name, so multiple MCC sessions from one worktree can share the same compiled binaries.

## Naming and Identity

### Session Name

- A new `session` concept is introduced for all `mcc-*` commands.
- If the user passes `--session NAME`, that value is used.
- Otherwise the default is the current Git worktree name.
- If a worktree name cannot be resolved, the fallback is the basename of the current repo root.

### Username

- If the user passes `--username NAME`, that value is used.
- Otherwise the username is derived from the resolved `session`.
- The derived name must:
  - use only Minecraft-safe characters already accepted by the existing config flow
  - be deterministic
  - be at most 16 characters
  - remain stable for the same session across runs

Recommended derivation:

1. Lowercase the session name.
2. Replace invalid characters with underscores.
3. Prefix with `mcc_`.
4. If the result is longer than 16 characters, keep the first 11 characters and append `_` plus the first 4 hexadecimal characters of the SHA-1 of the normalized session string.

Example:

- `feature-ai` -> `mcc_feature_ai`
- `very-long-worktree-name` -> `mcc_veryl_ab12`

This truncation algorithm must be implemented and tested exactly as written so future changes do not silently rename active debug identities.

## Repo and Server Root Resolution

### Repo Root

Helper scripts must stop exporting `MCC_REPO` as shell-global state.

Instead:

- Each script resolves its own repo root from its own location.
- Shared shell helper functions may cache that resolved path internally, but not as a required ambient variable.
- Child commands should receive explicit paths or recompute the repo root themselves.

This removes the current failure mode where a shell sourced from one clone or worktree accidentally drives tools in another.

### Server Root

`MCC_SERVERS` stays as the supported override for the shared server root.

Resolution order:

1. If `MCC_SERVERS` is set, use it.
2. Otherwise use `<resolved repo root>/MinecraftOfficial/downloads`.

This keeps the useful part of the current workflow: one intentionally shared set of server assets.

## File and Process Layout

### MCC Session Root

Each MCC session gets a unique root directory:

- `${TMPDIR:-/tmp}/mcc-debug/<session>/`

That directory contains:

- `MinecraftClient.debug.ini`
- `mcc-debug.log`
- `mcc_input.txt`
- `mcc.pid`
- `session.meta`

### tmux Sessions

MCC tmux sessions must be renamed from the fixed `mcc-debug` to a session-scoped name, for example:

- `mcc-<session>`

Server tmux sessions remain version-scoped:

- `mc-<version>`

### Kill and Reset Scope

- `mcc-kill --session X` only stops the MCC process and tmux session for `X`.
- A new `mcc-reset-session` command should clear only session-scoped files and tmux sessions.
- Existing shared server reset logic must continue to operate on server state only.
- No `mcc-*` command may kill all `MinecraftClient` processes by pattern.

## Command Interface

### Shared Server Commands

These remain shared-resource commands and do not take `--session`:

- `mc-start`
- `mc-stop`
- `mc-log`
- `mc-rcon`
- `mc-wait-ready`
- `mc-wait-stop`

### Session-Scoped MCC Commands

These commands gain `--session`, and where applicable `--username`:

- `mcc-debug`
- `mcc-run`
- `mcc-tui`
- `mcc-cmd`
- `mcc-log-mcc`
- `mcc-state`
- `mcc-kill`

Expected defaults:

- `--session`: current worktree name
- `--username`: derived from session

Example commands:

```bash
mcc-debug -v 1.21.11 --session alice-a --username AliceA --file-input
mcc-debug -v 1.21.11 --session alice-b --username AliceB --file-input
mcc-cmd --session alice-a "debug state"
mcc-log-mcc --session alice-b
mcc-kill --session alice-a
mcc-reset-session --session alice-b
```

## Build Output Isolation

### Base Rule

Build outputs must be isolated by worktree, not mixed across worktrees.

This should be implemented by introducing a build root override passed through the helper scripts into `dotnet build` and `dotnet run`, without requiring users to manually edit project files per worktree.

The important behavior is:

- Worktree A writes to build root A
- Worktree B writes to build root B
- Both can build concurrently without sharing `bin/obj`

### tmpfs Acceleration

Add an opt-in tmpfs-backed build root for machines with large RAM.

Suggested layout:

- `/dev/shm/mcc-build/<worktree>/` on Linux
- fallback to `${TMPDIR:-/tmp}/mcc-build/<worktree>/` when `/dev/shm` is unavailable

Requirements:

- tmpfs mode is optional, not mandatory
- the resolved build root must be printed in debug output
- a new `mcc-build-clean` command should clear build outputs for the current worktree
- helper scripts must fail clearly if the chosen build root is not writable

### Compatibility

If tmpfs mode is disabled, behavior should remain functionally equivalent to today, except for the added worktree-aware path selection.

## Backward Compatibility

- Existing single-session workflows should still work without requiring `--session`.
- Existing external `MCC_SERVERS` setups must continue to work.
- Existing server-side helper scripts should keep their public behavior.
- Existing documentation examples can be updated gradually, but the basic commands should remain recognizable.

## Error Handling

The tooling should fail early and clearly for:

- missing server root
- invalid or empty session name
- invalid derived username after normalization
- requested tmux session already in use by a different live MCC process
- unwritable tmpfs or build root
- missing PID file on targeted kill operations

When possible, the error should print the resolved repo root, shared server root, session name, username, and session root to make misconfiguration obvious.

## Validation Plan

### Manual Verification Matrix

1. Build from two different worktrees at the same time and confirm isolated output roots.
2. Start one shared `1.21.11` server and confirm only one `mc-1_21_11` session exists.
3. Launch two MCC sessions from two different worktrees without explicit usernames and confirm distinct derived usernames.
4. Join both clients to the shared server and confirm neither client disconnects the other.
5. Send different commands through each session's input file and confirm only the intended client responds.
6. Tail each session's log and confirm no cross-session log mixing.
7. Kill one MCC session and confirm:
   - the other MCC session stays connected
   - the shared server remains running
8. Enable tmpfs build mode in one worktree and verify outputs are written to the resolved tmpfs path.
9. Rebuild after cleaning tmpfs outputs and confirm the worktree still functions.

### Regression Checks

- Single-session debug loop still works with no explicit `--session`
- Shared server helpers still work when `MCC_SERVERS` points outside the repo
- TUI mode still works inside tmux with session-specific naming

## Implementation Notes

The most likely files to change are:

- `tools/mcc-env.sh`
- `tools/mcc-debug.sh`
- `tools/start-server.sh`
- `.skills/mcc-integration-testing/scripts/preflight_test_env.sh`
- `.skills/mcc-integration-testing/scripts/reset_shared_test_state.sh`
- `tools/README.md`
- `docs/guide/ai-assisted-development.md`
- `.skills/mcc-dev-workflow/SKILL.md`

If build-output redirection is implemented through MSBuild configuration rather than pure shell arguments, related project or props files may also need changes.

## Open Decisions Resolved In This Design

- Keep `MCC_SERVERS`: yes
- Keep `MCC_REPO` as shell-global state: no
- Shared server instances across worktrees: yes
- MCC debug isolation keyed by explicit `session`: yes
- Default `session`: current worktree name
- Default username: derived from session
- Build output isolation key: current worktree
- tmpfs build output: opt-in optimization

## Summary

The resulting workflow intentionally shares the expensive and durable part of local MCC development, the server installation and running server instance, while isolating the volatile and user-specific part, the MCC client debug session and build outputs.

That gives parallel worktree development without forcing duplicate local servers, while removing the current collisions around shell state, tmux naming, temp files, input routing, and username reuse.
