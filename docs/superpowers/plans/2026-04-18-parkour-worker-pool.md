# Parkour Worker Pool Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `tools/test-parkour.py` into a long-lived worker-pool harness that handles server readiness cleanly and emits inspectable per-run artifacts for unattended parallel live testing.

**Architecture:** Keep the existing matrix generation and live outcome rules, but replace per-case MCC relaunch with stable worker contexts that are reset between cases. Add a run-artifact layer so each case and worker has traceable logs, and classify harness failures separately from product failures.

**Tech Stack:** Python 3, unittest/pytest, MCC `mcc-debug`, local `1.21.11-Vanilla` server, RCON, tmux-backed worker sessions.

---

### Task 1: Lock The New Result Schema With Tests

**Files:**
- Modify: `tools/tests/test_test_parkour_metrics.py`
- Test: `tools/tests/test_test_parkour_metrics.py`

- [ ] **Step 1: Write failing tests for run artifacts and skip rows**

Add tests that assert:

```python
def test_result_to_record_includes_worker_session_and_paths(self) -> None:
    case = module.TestCase(
        case_id="linear-flat-gap1",
        family="linear",
        subfamily="flat",
        gap_or_wall=1,
        delta_y=0.0,
        ceiling_height=None,
        wall_offset=None,
        expected="pass",
    )
    result = module.TestResult(
        case=case,
        outcome="pass",
        matched_expected=True,
        replan_count=0,
        turn_stall_count=0,
        near_goal=True,
        total_ticks=42,
        final_position=(109.5, 80.0, 200.5),
        session="parkour-run-1",
        log_path="/tmp/parkour-runs/run-1/workers/1/worker.log",
        event_log_path="/tmp/parkour-runs/run-1/events.jsonl",
        duration_ms=4200,
        error_kind=None,
        skip_reason=None,
    )

    record = module.result_to_record(result, worker_id=1)

    self.assertEqual(record["session"], "parkour-run-1")
    self.assertEqual(record["worker"], 1)
    self.assertEqual(record["duration_ms"], 4200)
    self.assertEqual(record["skip_reason"], None)


def test_make_skip_result_marks_case_as_skipped(self) -> None:
    case = module.TestCase(
        case_id="linear-flat-gap4",
        family="linear",
        subfamily="flat",
        gap_or_wall=4,
        delta_y=0.0,
        ceiling_height=None,
        wall_offset=None,
        expected="pass",
    )

    result = module.make_skip_result(case, "group_failed_earlier")

    self.assertEqual(result.outcome, "skipped")
    self.assertEqual(result.skip_reason, "group_failed_earlier")
    self.assertFalse(result.matched_expected)
```

- [ ] **Step 2: Run the focused test file and confirm RED**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py
```

Expected: failures complaining that `TestResult` lacks the new fields and `make_skip_result` does not exist.

- [ ] **Step 3: Implement the minimal production fields and helpers**

Update `tools/test-parkour.py` so `TestResult` contains:

```python
session: str | None = None
log_path: str | None = None
event_log_path: str | None = None
duration_ms: int | None = None
error_kind: str | None = None
skip_reason: str | None = None
```

Add:

```python
def make_skip_result(case: TestCase, reason: str) -> TestResult:
    return TestResult(
        case=case,
        outcome="skipped",
        matched_expected=False,
        skip_reason=reason,
        error_kind=None,
    )
```

- [ ] **Step 4: Re-run the focused tests and confirm GREEN**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py
```

Expected: all tests in that file pass.

### Task 2: Add Readiness And Harness Error Coverage

**Files:**
- Modify: `tools/tests/test_test_parkour_metrics.py`
- Modify: `tools/test-parkour.py`

- [ ] **Step 1: Write failing tests for readiness and harness classification**

Add tests like:

```python
def test_classify_outcome_prefers_harness_error_when_rcon_is_unavailable(self) -> None:
    metrics = module.LiveMetrics()
    result = module.classify_outcome(metrics, near_goal=None, error_kind="harness_rcon_unavailable")
    self.assertEqual(result, "harness_rcon_unavailable")


def test_wait_for_rcon_ready_retries_until_command_succeeds(self) -> None:
    attempts = {"count": 0}

    class FakeRcon:
        def command(self, _cmd: str) -> str:
            attempts["count"] += 1
            if attempts["count"] < 3:
                raise ConnectionRefusedError("not ready")
            return "There are 0 of a max of 20 players online"

    with mock.patch.object(module.time, "sleep", lambda _seconds: None):
        self.assertTrue(module.wait_for_rcon_ready(FakeRcon(), timeout_seconds=3.0, poll_interval=0.1))
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py -k "harness_error or rcon_ready"
```

Expected: failures because the readiness helper and new classification signature do not exist.

- [ ] **Step 3: Implement minimal readiness helpers**

Add to `tools/test-parkour.py`:

```python
def wait_for_rcon_ready(
    rcon: RconClient,
    timeout_seconds: float = 20.0,
    poll_interval: float = 0.5,
) -> bool:
    deadline = time.monotonic() + timeout_seconds
    while time.monotonic() < deadline:
        try:
            rcon.command("list")
            return True
        except Exception:
            time.sleep(poll_interval)
    return False
```

Update `classify_outcome` to accept `error_kind: str | None = None` and return the harness error directly when present.

- [ ] **Step 4: Re-run focused tests and confirm GREEN**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py -k "harness_error or rcon_ready"
```

Expected: passing tests.

### Task 3: Convert Per-Case Relaunch Into A Long-Lived Worker Pool

**Files:**
- Modify: `tools/tests/test_test_parkour_metrics.py`
- Modify: `tools/test-parkour.py`

- [ ] **Step 1: Write failing tests for worker reuse bookkeeping**

Add tests asserting that one worker can execute multiple cases without changing session naming:

```python
def test_build_worker_session_name_is_stable_for_multiple_cases(self) -> None:
    self.assertEqual(module.build_worker_session_name("run123", 2), "parkour-run123-2")


def test_result_rows_can_share_one_worker_session_across_cases(self) -> None:
    case1 = "linear-flat-gap1"
    case2 = "linear-flat-gap2"
    session = module.build_worker_session_name("run123", 2)
    self.assertEqual(session, "parkour-run123-2")
    self.assertEqual(session, module.build_worker_session_name("run123", 2))
```

- [ ] **Step 2: Run the focused tests and confirm RED if needed**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py -k "worker_session"
```

Expected: either existing coverage passes already or new assertions fail because the worker model still depends on per-case session allocation elsewhere.

- [ ] **Step 3: Implement long-lived worker contexts**

In `tools/test-parkour.py`:

- keep `build_worker_session_name()` as the canonical stable session name
- stop calling `build_case_session_name()` from `worker_loop()`
- launch one `WorkerContext` per thread at worker start
- add `reset_worker_state(ctx, layout)` to reposition and resync between cases
- restart only that worker when reset or health checks fail

The main shape should become:

```python
ctx = ensure_worker_context(...)
for case, layout in items:
    if case.group_key() in failed_groups:
        ...
        continue

    ctx = ensure_worker_context(...)
    reset = reset_worker_state(ctx, layout)
    if not reset.ok:
        cleanup_workers([ctx])
        ctx = relaunch_worker_context(...)
        reset = reset_worker_state(ctx, layout)
```

- [ ] **Step 4: Re-run focused metric tests**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py
```

Expected: file stays green after the worker-pool refactor.

### Task 4: Emit Run Directories, Per-Case Records, And Summaries

**Files:**
- Modify: `tools/tests/test_test_parkour_metrics.py`
- Modify: `tools/test-parkour.py`

- [ ] **Step 1: Write failing tests for summary aggregation**

Add tests like:

```python
def test_summarize_results_groups_by_family_and_outcome(self) -> None:
    summary = module.summarize_results(
        [
            {"family": "linear", "outcome": "pass", "matched": True},
            {"family": "linear", "outcome": "reject", "matched": True},
            {"family": "neo", "outcome": "reject", "matched": False},
            {"family": "linear", "outcome": "skipped", "matched": False},
        ]
    )

    self.assertEqual(summary["families"]["linear"]["outcomes"]["pass"], 1)
    self.assertEqual(summary["families"]["linear"]["outcomes"]["skipped"], 1)
    self.assertEqual(summary["families"]["neo"]["mismatches"], 1)
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py -k summarize_results
```

Expected: missing helper failures.

- [ ] **Step 3: Implement run-artifact helpers**

Add helpers in `tools/test-parkour.py`:

```python
def create_run_dir(base_dir: Path | None = None) -> Path: ...
def write_case_artifact(run_dir: Path, result: TestResult) -> None: ...
def summarize_results(records: list[dict[str, object]]) -> dict[str, object]: ...
def write_summary_files(run_dir: Path, summary: dict[str, object]) -> None: ...
```

Update `_run_parallel()` and `_run_serial()` to:

- create one run directory
- append JSONL rows there even when `--results` is omitted
- persist skip rows
- write `summary.json` and `summary.md`

- [ ] **Step 4: Re-run targeted tests and then full tools suite**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py
python3 -m pytest -q tools/tests
```

Expected: full `tools/tests` suite passes.

### Task 5: Verify The Real Harness Fixes Against 1.21.11

**Files:**
- Modify: `tools/test-parkour.py`
- Test: real-server execution only

- [ ] **Step 1: Run a filtered live smoke on linear with long-lived workers**

Run:

```bash
source tools/mcc-env.sh && \
python3 tools/test-parkour.py \
  --filter linear \
  --parallel 6 \
  --version 1.21.11-Vanilla \
  --results /tmp/parkour-linear-worker-pool.jsonl
```

Expected:

- worker startup lines show six stable worker sessions
- multiple cases reuse the same worker/session ids
- summary artifacts are written under `/tmp/parkour-runs/...`

- [ ] **Step 2: Inspect the emitted summary and worker logs**

Run:

```bash
latest_run=$(ls -td /tmp/parkour-runs/* | head -n 1)
printf '%s\n' "$latest_run"
sed -n '1,220p' "$latest_run/summary.md"
find "$latest_run/workers" -maxdepth 2 -type f | sort
```

Expected:

- `summary.md` exists
- worker logs exist
- skipped cases are represented in the summary

- [ ] **Step 3: Run one full live matrix to prove unattended output quality**

Run:

```bash
source tools/mcc-env.sh && \
python3 tools/test-parkour.py \
  --parallel 6 \
  --version 1.21.11-Vanilla \
  --results /tmp/parkour-full-worker-pool.jsonl
```

Expected:

- no raw `ConnectionRefusedError` at startup when the server is merely late
- a completed `summary.md` and `summary.json`
- case rows include `worker`, `session`, `log_path`, `duration_ms`, and skip metadata

- [ ] **Step 4: Commit**

```bash
git add tools/test-parkour.py \
        tools/tests/test_test_parkour_metrics.py \
        tools/tests/test_pathing_live_scripts.py \
        docs/superpowers/specs/2026-04-18-parkour-worker-pool-design.md \
        docs/superpowers/plans/2026-04-18-parkour-worker-pool.md
git commit -m "test-harness: add long-lived parkour worker pool"
```
