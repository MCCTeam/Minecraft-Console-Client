# Parkour Worker Pool Design

**Date:** 2026-04-18

**Goal**

Make `tools/test-parkour.py` behave like a real parallel harness on `1.21.11-Vanilla`: keep `--parallel N` as `N` long-lived MCC workers, classify harness failures separately from product failures, and emit artifacts that make unattended runs easy to inspect.

## Problem Statement

The current script has two real operational problems.

1. Server and RCON readiness are assumed too early. In prior use this caused an immediate `ConnectionRefusedError` when the server had been stopped by a previous harness.
2. The parallel path launches and tears down a fresh MCC session for every case. That produces large startup overhead, leaves many session directories behind, and makes logs hard to correlate with results.

The current script also has observability gaps.

- Skipped cases are only visible in stdout.
- JSONL rows do not carry enough metadata to jump directly to the relevant logs.
- There is no per-run summary artifact beyond terminal output.

## Non-Goals

- Changing parkour theory generation or expected pass/reject boundaries.
- Reworking the live outcome rules that treat any replan or turn-stall as a failure for pass cases.
- Generalizing the harness to multi-version shared-state parallelism.

## Chosen Approach

Use a long-lived worker pool.

- Start `N` MCC workers once.
- Assign whole `group_key()` batches to workers to preserve stop-at-first-failure semantics inside a group.
- Reuse the same worker for multiple cases by resetting player state between cases.
- Restart only the individual worker that becomes unhealthy.

This keeps isolation strong enough for unattended live testing while removing the main startup bottleneck.

## Architecture

### Run Controller

The main process will build the course matrix and create a dedicated run directory under `/tmp/parkour-runs/<timestamp>-<token>/`.

It will own:

- server/RCON readiness checks
- world build phase
- worker pool startup
- group scheduling
- summary generation

### Worker Lifecycle

Each worker will keep a stable:

- `worker_id`
- `session`
- `username`
- MCC log path
- worker event log

Worker lifecycle:

1. launch MCC once
2. wait for join confirmation
3. enable debug mode
4. run many assigned cases with reset in between
5. if unhealthy, recycle just that worker
6. quit cleanly during harness shutdown

### Case Execution

Each case will still:

- teleport to the start
- verify local sync via `debug state`
- run `goto`
- sample position/yaw during execution
- parse `[PathMetric]` telemetry
- classify into `pass`, `reject`, `fail`, or a harness-specific error

The zero-replan and zero-turn-stall rule remains unchanged.

### Artifacts

Each run directory will contain:

- `manifest.json`
- `results.jsonl`
- `summary.json`
- `summary.md`
- `events.jsonl`
- `workers/<worker_id>/worker.log`
- `cases/<case_id>.json`

Every result row will include:

- `case_id`
- `family`
- `subfamily`
- `expected`
- `outcome`
- `matched`
- `worker`
- `session`
- `log_path`
- `event_log_path`
- `replan_count`
- `turn_stall_count`
- `near_goal`
- `total_ticks`
- `final_position`
- `duration_ms`
- `skip_reason`
- `error_kind`

Skipped cases will be recorded, not just printed.

## Failure Taxonomy

Observed product behavior and harness behavior must be separated.

Product-side outcomes:

- `pass`
- `reject`
- `fail`

Harness-side outcomes:

- `harness_rcon_unavailable`
- `harness_worker_launch_failed`
- `harness_join_timeout`
- `harness_start_sync_failed`
- `harness_worker_lost`

`matched` remains the top-level boolean used by summaries.

## Logging Model

Terminal output becomes high-signal progress output:

- worker launch / restart
- case start / case finish
- group skip decisions
- mismatch lines
- final summaries by family and outcome

Detailed evidence moves into run artifacts.

## Testing Strategy

### Python unit coverage

Extend `tools/tests/test_test_parkour_metrics.py` to cover:

- worker session naming
- run directory naming
- result row schema
- skip row emission
- summary aggregation
- harness error classification

Extend `tools/tests/test_pathing_live_scripts.py` to cover:

- new CLI-compatible output expectations
- `--list-cases` stability

### Real-server validation

After refactor:

- run targeted Python tests
- run the full `tools/tests` suite
- run a real `tools/test-parkour.py --filter linear --parallel 6 --version 1.21.11-Vanilla`
- confirm that workers are reused across multiple cases and that summary artifacts are emitted

## Risks And Mitigations

- Worker state leaks between cases.
  - Mitigation: centralize `reset_worker_state()` and recycle unhealthy workers.
- Mixed threaded stdout becomes unreadable.
  - Mitigation: keep stdout terse and persist details to per-worker logs.
- Shared server state still limits aggressive parallelism.
  - Mitigation: keep one shared version/server per run and continue atomic group scheduling.

## Acceptance Criteria

- A stopped or not-yet-ready server is reported as a harness problem instead of a raw socket traceback.
- `--parallel 6` keeps approximately six long-lived MCC workers instead of one worker per case.
- Results include executed cases and skipped cases.
- A completed unattended run can be inspected from `summary.md` and `summary.json` without replaying terminal output.
- The zero-replan and zero-turn-stall requirement remains enforced for pass cases.
