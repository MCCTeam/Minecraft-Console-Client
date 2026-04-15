# Theory-Aligned Pathing Regression Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a first-wave pathing regression workflow where `tools/sim_jump_reach.py` generates the authoritative theory matrix plus canonical live cases, and theory-aligned live harness scripts validate representative linear, neo, and ceiling-constrained jumps against that authority.

**Architecture:** Split the work into three layers. First, extract a reusable Python theory module from `tools/sim_jump_reach.py` so it can generate a stable case table instead of only printing ad-hoc console output. Second, generate versioned theory artifacts and canonical live-case manifests under `tools/pathing_data/`, then add a report layer that joins live results back to theory case IDs. Third, refactor the linear live harness and add a new neo and headhitter harness that consume canonical cases instead of hardcoding expected outcomes.

**Tech Stack:** Python 3 standard library (`argparse`, `csv`, `json`, `dataclasses`, `unittest`), Bash harness scripts on top of `tools/mcc-env.sh`, versioned JSON/CSV/Markdown artifacts under `tools/pathing_data/`, existing MCC live debug loop on `1.21.11-Vanilla`.

---

## Scope Check

This plan intentionally covers only the first-wave scope from [theory-aligned-pathing-regression-design.md](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/docs/superpowers/specs/2026-04-13-theory-aligned-pathing-regression-design.md):

- theory authority from `tools/sim_jump_reach.py`
- first-wave movement families only:
  - linear flat
  - linear ascend
  - linear descend
  - neo
  - ceiling-constrained or headhitter
- canonical live coverage only
- specialized live suites stay out of scope except for documentation positioning

Do not expand this plan to repeated parkour chains, landing-recovery into turns, braking metrics, long-route mixed execution, or C# runtime refactors. Those already have their own committed work and separate plans.

## File Structure

### Python theory layer

- Create: `tools/pathing_theory/__init__.py`
  - package marker for the reusable theory/export code
- Create: `tools/pathing_theory/models.py`
  - dataclasses for theory cases, canonical live cases, live results, and report rows
- Create: `tools/pathing_theory/primitives.py`
  - extracted jump physics constants and low-level reachability helpers moved out of the CLI entry point
- Create: `tools/pathing_theory/simulator.py`
  - reusable case generation built on `tools/pathing_theory/primitives.py` without importing the CLI entry point
- Create: `tools/pathing_theory/canonical.py`
  - bucket selection and canonical live-case derivation
- Create: `tools/pathing_theory/renderers.py`
  - JSON/CSV/Markdown writers for theory outputs
- Create: `tools/pathing_theory/report.py`
  - join live result rows back to canonical cases and render summary outputs
- Modify: `tools/sim_jump_reach.py`
  - keep as the public CLI entry point, but delegate to the new reusable modules
- Create: `tools/pathing_theory_report.py`
  - small CLI wrapper around `tools/pathing_theory/report.py`

### Versioned data artifacts

- Create: `tools/pathing_data/theory-matrix.json`
  - full machine-readable theory matrix
- Create: `tools/pathing_data/theory-matrix.csv`
  - CSV view of the same matrix
- Create: `tools/pathing_data/theory-matrix.md`
  - human-readable summary from the same in-memory data
- Create: `tools/pathing_data/canonical-live-cases.json`
  - versioned canonical live cases consumed by shell harnesses

These files are intentionally tracked. Regeneration happens explicitly when theory changes, so running the live harnesses does not dirty the worktree.

### Python tests

- Create: `tools/tests/__init__.py`
  - package marker for `unittest` discovery
- Create: `tools/tests/test_pathing_theory_matrix.py`
  - verifies case generation and output file contents
- Create: `tools/tests/test_pathing_canonical_cases.py`
  - verifies deterministic bucket selection
- Create: `tools/tests/test_pathing_theory_report.py`
  - verifies theory/live join and summary classification
- Create: `tools/tests/test_pathing_live_scripts.py`
  - subprocess-based checks for `--list-cases` support and manifest consumption

### Live harness layer

- Create: `tools/pathing_live_common.sh`
  - shared manifest parsing, per-case recording, and common MCC session helpers for the theory-aligned suites
- Modify: `tools/test-parkour.sh`
  - turn into the main theory-aligned linear-jump suite
- Create: `tools/test-pathing-theory-neo-ceiling.sh`
  - theory-aligned suite for canonical `neo` and `ceiling` buckets

### Documentation

- Modify: `docs/guide/pathfinding-research.md`
  - document the theory matrix workflow, canonical live coverage, regeneration commands, and how specialized live suites differ from theory-aligned suites

---

### Task 1: Extract Reusable Theory Case Generation

**Files:**
- Create: `tools/pathing_theory/__init__.py`
- Create: `tools/pathing_theory/models.py`
- Create: `tools/pathing_theory/primitives.py`
- Create: `tools/pathing_theory/simulator.py`
- Modify: `tools/sim_jump_reach.py`
- Create: `tools/tests/__init__.py`
- Test: `tools/tests/test_pathing_theory_matrix.py`

- [ ] **Step 1: Write the failing theory-matrix generation test**

Create `tools/tests/test_pathing_theory_matrix.py`:

```python
import unittest

from tools.pathing_theory.simulator import build_theory_cases


class PathingTheoryMatrixTests(unittest.TestCase):
    def test_build_theory_cases_returns_first_wave_families(self) -> None:
        cases = build_theory_cases()
        families = {(case.family, case.subfamily) for case in cases}

        self.assertIn(("linear", "flat"), families)
        self.assertIn(("linear", "ascend"), families)
        self.assertIn(("linear", "descend"), families)
        self.assertIn(("neo", "neo"), families)
        self.assertIn(("ceiling", "headhitter"), families)

        linear_boundary = next(
            case for case in cases
            if case.case_id == "linear-flat-sprint-mm12-gap5-dy0p0"
        )
        self.assertTrue(linear_boundary.expected_reachable)
        self.assertGreater(linear_boundary.margin, 0.0)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_theory_matrix -v
```

Expected: FAIL with `ModuleNotFoundError: No module named 'tools.pathing_theory'`.

- [ ] **Step 3: Implement the reusable theory models and case generator**

Create `tools/pathing_theory/models.py`:

```python
from dataclasses import dataclass


@dataclass(frozen=True)
class TheoryCase:
    case_id: str
    family: str
    subfamily: str
    movement_mode: str
    momentum_ticks: int
    gap_blocks: int | None
    delta_y: float | None
    ceiling_height: float | None
    wall_width: int | None
    expected_reachable: bool
    landing_x: float | None
    apex_y: float | None
    margin: float | None
    notes: str = ""
```

Create `tools/pathing_theory/primitives.py`:

```python
from dataclasses import dataclass
from typing import Optional

# Move these symbols from `tools/sim_jump_reach.py` into this module without
# changing their behavior:
# - PLAYER_WIDTH, PLAYER_HEIGHT, STEP_HEIGHT
# - GRAVITY, DRAG_Y, FRICTION_MULTIPLIER, DEFAULT_BLOCK_FRICTION
# - INPUT_FRICTION, GROUND_ACCEL_FACTOR, AIR_ACCEL, MOVEMENT_SPEED
# - BASE_JUMP_POWER, SPRINT_JUMP_HORIZONTAL_BOOST
# - HORIZONTAL_VELOCITY_THRESHOLD_SQR, VERTICAL_VELOCITY_THRESHOLD, HALF_WIDTH
# - TickState
# - get_ground_speed()
# - simulate_jump()
# - get_landing()
# - get_apex()
# - can_reach_gap()
```

Create `tools/pathing_theory/simulator.py`:

```python
from tools.pathing_theory.models import TheoryCase
from tools.pathing_theory.primitives import PLAYER_WIDTH, can_reach_gap, get_apex, get_landing


def _float_token(value: float) -> str:
    token = f"{value:.1f}".replace("-", "m").replace(".", "p")
    return token


def build_theory_cases() -> list[TheoryCase]:
    cases: list[TheoryCase] = []

    for sprint, movement_mode, momentum_ticks in [
        (False, "walk", 12),
        (True, "sprint", 0),
        (True, "sprint", 12),
    ]:
        for gap in range(0, 7):
            for delta_y in [0.0, 1.0, -1.0, -2.0]:
                ok, landing_x, needed_x = can_reach_gap(
                    gap_blocks=gap,
                    dy=delta_y,
                    sprint=sprint,
                    momentum_ticks=momentum_ticks,
                )
                apex_y, _ = get_apex(sprint=sprint, momentum_ticks=momentum_ticks)
                subfamily = (
                    "flat" if delta_y == 0.0
                    else "ascend" if delta_y > 0.0
                    else "descend"
                )
                cases.append(
                    TheoryCase(
                        case_id=f"linear-{subfamily}-{movement_mode}-mm{momentum_ticks}-gap{gap}-dy{_float_token(delta_y)}",
                        family="linear",
                        subfamily=subfamily,
                        movement_mode=movement_mode,
                        momentum_ticks=momentum_ticks,
                        gap_blocks=gap,
                        delta_y=delta_y,
                        ceiling_height=None,
                        wall_width=None,
                        expected_reachable=ok,
                        landing_x=landing_x,
                        apex_y=apex_y,
                        margin=None if landing_x is None else landing_x - needed_x,
                    )
                )

    landing = get_landing(sprint=True, target_y=0.0, landing_x_start=0.0, momentum_ticks=12)
    for wall_width in [1, 2, 3, 4]:
        landing_x = None if landing is None else landing[0]
        needed_x = wall_width + PLAYER_WIDTH
        margin = None if landing_x is None else landing_x - needed_x
        cases.append(
            TheoryCase(
                case_id=f"neo-neo-sprint-mm12-wall{wall_width}",
                family="neo",
                subfamily="neo",
                movement_mode="sprint",
                momentum_ticks=12,
                gap_blocks=None,
                delta_y=0.0,
                ceiling_height=None,
                wall_width=wall_width,
                expected_reachable=margin is not None and margin >= 0.0,
                landing_x=landing_x,
                apex_y=get_apex(sprint=True, momentum_ticks=12)[0],
                margin=margin,
            )
        )

    for ceiling_height in [4.0, 3.0, 2.5, 2.0, 1.8125]:
        for gap in [1, 2, 3, 4]:
            landing = get_landing(
                sprint=True,
                target_y=0.0,
                landing_x_start=0.5 + gap,
                momentum_ticks=12,
                ceiling_y=ceiling_height,
            )
            landing_x = None if landing is None else landing[0]
            needed_x = 0.5 + gap + (PLAYER_WIDTH / 2.0)
            margin = None if landing_x is None else landing_x - needed_x
            cases.append(
                TheoryCase(
                    case_id=f"ceiling-headhitter-sprint-mm12-gap{gap}-ceil{str(ceiling_height).replace('.', 'p')}",
                    family="ceiling",
                    subfamily="headhitter",
                    movement_mode="sprint",
                    momentum_ticks=12,
                    gap_blocks=gap,
                    delta_y=0.0,
                    ceiling_height=ceiling_height,
                    wall_width=None,
                    expected_reachable=margin is not None and margin >= 0.0,
                    landing_x=landing_x,
                    apex_y=get_apex(sprint=True, momentum_ticks=12, ceiling_y=ceiling_height)[0],
                    margin=margin,
                )
            )

    return cases
```

Modify the top of `tools/sim_jump_reach.py` so the CLI imports the extracted primitives and the new case builder without creating a circular import:

```python
from tools.pathing_theory.primitives import PLAYER_WIDTH, can_reach_gap, get_apex, get_landing
from tools.pathing_theory.simulator import build_theory_cases
```

- [ ] **Step 4: Run the theory-matrix test to verify it passes**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_theory_matrix -v
```

Expected: PASS with `test_build_theory_cases_returns_first_wave_families ... ok`.

- [ ] **Step 5: Commit**

```bash
git add tools/pathing_theory/__init__.py \
        tools/pathing_theory/models.py \
        tools/pathing_theory/primitives.py \
        tools/pathing_theory/simulator.py \
        tools/sim_jump_reach.py \
        tools/tests/__init__.py \
        tools/tests/test_pathing_theory_matrix.py
git commit -m "feat: extract reusable pathing theory generator"
```

### Task 2: Generate Versioned Theory Artifacts And Canonical Live Cases

**Files:**
- Create: `tools/pathing_theory/canonical.py`
- Create: `tools/pathing_theory/renderers.py`
- Modify: `tools/pathing_theory/models.py`
- Modify: `tools/sim_jump_reach.py`
- Create: `tools/pathing_data/theory-matrix.json`
- Create: `tools/pathing_data/theory-matrix.csv`
- Create: `tools/pathing_data/theory-matrix.md`
- Create: `tools/pathing_data/canonical-live-cases.json`
- Test: `tools/tests/test_pathing_canonical_cases.py`
- Test: `tools/tests/test_pathing_theory_matrix.py`

- [ ] **Step 1: Write the failing canonical-selection and export tests**

Create `tools/tests/test_pathing_canonical_cases.py`:

```python
import json
import tempfile
import unittest
from pathlib import Path

from tools.pathing_theory.canonical import build_canonical_live_cases
from tools.pathing_theory.renderers import write_theory_artifacts
from tools.pathing_theory.simulator import build_theory_cases


class CanonicalPathingCaseTests(unittest.TestCase):
    def test_build_canonical_live_cases_picks_easy_boundary_and_reject(self) -> None:
        canonical_cases = build_canonical_live_cases(build_theory_cases())
        bucket_ids = {case.bucket_id for case in canonical_cases}

        self.assertTrue(all(case.movement_mode == "sprint" for case in canonical_cases))
        self.assertTrue(all(case.momentum_ticks == 12 for case in canonical_cases))
        self.assertIn("linear:flat:sprint:easy", bucket_ids)
        self.assertIn("linear:flat:sprint:boundary", bucket_ids)
        self.assertIn("linear:flat:sprint:reject", bucket_ids)
        self.assertIn("neo:neo:sprint:boundary", bucket_ids)
        self.assertIn("ceiling:headhitter:sprint:boundary", bucket_ids)

    def test_write_theory_artifacts_writes_json_csv_and_markdown_from_same_cases(self) -> None:
        cases = build_theory_cases()

        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = Path(temp_dir)
            write_theory_artifacts(cases, build_canonical_live_cases(cases), output_dir)

            json_path = output_dir / "theory-matrix.json"
            csv_path = output_dir / "theory-matrix.csv"
            md_path = output_dir / "theory-matrix.md"
            canonical_path = output_dir / "canonical-live-cases.json"

            self.assertTrue(json_path.exists())
            self.assertTrue(csv_path.exists())
            self.assertTrue(md_path.exists())
            self.assertTrue(canonical_path.exists())

            exported_cases = json.loads(json_path.read_text())
            self.assertEqual(len(cases), len(exported_cases))
            self.assertIn("| family | subfamily | movement_mode |", md_path.read_text())


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_canonical_cases -v
```

Expected: FAIL with `ModuleNotFoundError` for `tools.pathing_theory.canonical` or `renderers`.

- [ ] **Step 3: Implement deterministic canonical selection and artifact rendering**

Extend `tools/pathing_theory/models.py`:

```python
@dataclass(frozen=True)
class CanonicalLiveCase:
    case_id: str
    bucket_id: str
    family: str
    subfamily: str
    movement_mode: str
    momentum_ticks: int
    difficulty_band: str
    expected_result: str
    world_recipe_id: str
    gap_blocks: int | None
    delta_y: float | None
    ceiling_height: float | None
    wall_width: int | None
    start: dict[str, float]
    goal: dict[str, float]
```

Project the full theory matrix down to the sprint, 12-tick momentum lane for live execution. Keep walk and standing-sprint rows in `theory-matrix.*`, but do not emit them into `canonical-live-cases.json` until the live harness can intentionally force those movement modes.

Create `tools/pathing_theory/canonical.py`:

```python
from tools.pathing_theory.models import CanonicalLiveCase, TheoryCase


def _world_recipe_id(case: TheoryCase) -> str:
    if case.family == "linear":
        return f"linear-{case.subfamily}"
    if case.family == "neo":
        return "neo-wall"
    return "ceiling-headhitter"


def _canonical_goal(case: TheoryCase) -> tuple[dict[str, float], dict[str, float]]:
    start = {"x": 100.5, "y": 80.0, "z": 100.5}
    if case.family == "linear":
        goal_y = 80.0 + (case.delta_y or 0.0)
        goal_x = 100 + (case.gap_blocks or 0) + 1
        return start, {"x": float(goal_x), "y": goal_y, "z": 100.0}
    if case.family == "neo":
        goal_z = 100 + (case.wall_width or 1)
        return start, {"x": 102.0, "y": 80.0, "z": float(goal_z)}
    goal_x = 100 + (case.gap_blocks or 0) + 1
    return start, {"x": float(goal_x), "y": 80.0, "z": 100.0}


def build_canonical_live_cases(cases: list[TheoryCase]) -> list[CanonicalLiveCase]:
    live_candidate_cases = [
        case for case in cases
        if case.movement_mode == "sprint" and case.momentum_ticks == 12
    ]

    by_bucket: dict[tuple[str, str, str], list[TheoryCase]] = {}
    for case in live_candidate_cases:
        by_bucket.setdefault((case.family, case.subfamily, case.movement_mode), []).append(case)

    canonical_cases: list[CanonicalLiveCase] = []
    for family, subfamily, movement_mode in sorted(by_bucket):
        bucket_cases = by_bucket[(family, subfamily, movement_mode)]
        reachable = sorted(
            [case for case in bucket_cases if case.expected_reachable and case.margin is not None],
            key=lambda case: case.margin,
        )
        unreachable = sorted(
            [case for case in bucket_cases if not case.expected_reachable],
            key=lambda case: float("-inf") if case.margin is None else abs(case.margin),
        )

        selected: list[tuple[str, TheoryCase]] = []
        if reachable:
            easy = next((case for case in reversed(reachable) if (case.margin or 0.0) >= 0.50), reachable[-1])
            boundary = reachable[0]
            selected.append(("easy", easy))
            if boundary.case_id != easy.case_id:
                selected.append(("boundary", boundary))
        if unreachable:
            reject = unreachable[0]
            selected.append(("reject", reject))

        for difficulty_band, case in selected:
            start, goal = _canonical_goal(case)
            canonical_cases.append(
                CanonicalLiveCase(
                    case_id=case.case_id,
                    bucket_id=f"{family}:{subfamily}:{movement_mode}:{difficulty_band}",
                    family=family,
                    subfamily=subfamily,
                    movement_mode=movement_mode,
                    momentum_ticks=case.momentum_ticks,
                    difficulty_band=difficulty_band,
                    expected_result="pass" if case.expected_reachable else "reject",
                    world_recipe_id=_world_recipe_id(case),
                    gap_blocks=case.gap_blocks,
                    delta_y=case.delta_y,
                    ceiling_height=case.ceiling_height,
                    wall_width=case.wall_width,
                    start=start,
                    goal=goal,
                )
            )

    return canonical_cases
```

Create `tools/pathing_theory/renderers.py`:

```python
import csv
import json
from dataclasses import asdict
from pathlib import Path

from tools.pathing_theory.models import CanonicalLiveCase, TheoryCase


def write_theory_artifacts(
    cases: list[TheoryCase],
    canonical_cases: list[CanonicalLiveCase],
    output_dir: Path,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)

    json_path = output_dir / "theory-matrix.json"
    csv_path = output_dir / "theory-matrix.csv"
    md_path = output_dir / "theory-matrix.md"
    canonical_path = output_dir / "canonical-live-cases.json"

    json_path.write_text(json.dumps([asdict(case) for case in cases], indent=2) + "\n")
    canonical_path.write_text(json.dumps([asdict(case) for case in canonical_cases], indent=2) + "\n")

    with csv_path.open("w", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(asdict(cases[0]).keys()))
        writer.writeheader()
        for case in cases:
            writer.writerow(asdict(case))

    lines = [
        "# Theory Matrix",
        "",
        "| family | subfamily | movement_mode | case_id | expected_reachable | margin |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for case in cases:
        lines.append(
            f"| {case.family} | {case.subfamily} | {case.movement_mode} | {case.case_id} | "
            f"{case.expected_reachable} | {case.margin} |"
        )
    md_path.write_text("\n".join(lines) + "\n")
```

Modify `tools/sim_jump_reach.py` to add an explicit generation command:

```python
from pathlib import Path

from tools.pathing_theory.canonical import build_canonical_live_cases
from tools.pathing_theory.renderers import write_theory_artifacts
from tools.pathing_theory.simulator import build_theory_cases


def main() -> None:
    parser = argparse.ArgumentParser(description="Minecraft jump reachability simulator (Java 1.14+)")
    parser.add_argument("--verbose", "-v", action="store_true", help="Print per-tick trajectory data")
    parser.add_argument("--csv", type=str, default=None, help="Export results to CSV file")
    parser.add_argument("--write-artifacts", type=str, default=None, help="Write tracked theory artifacts to a directory")
    args = parser.parse_args()

    if args.write_artifacts:
        cases = build_theory_cases()
        canonical_cases = build_canonical_live_cases(cases)
        write_theory_artifacts(cases, canonical_cases, Path(args.write_artifacts))
        print(f"Wrote theory artifacts to {args.write_artifacts}")
        return

    results = analyze_all(verbose=args.verbose)
    if args.csv and results:
        keys = set()
        for row in results:
            keys.update(row.keys())
        with open(args.csv, "w", newline="") as handle:
            writer = csv.DictWriter(handle, fieldnames=sorted(keys))
            writer.writeheader()
            writer.writerows(results)
        print(f"\nResults exported to {args.csv}")
```

- [ ] **Step 4: Run the tests, then generate the tracked artifacts**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_canonical_cases -v
python3 tools/sim_jump_reach.py --write-artifacts tools/pathing_data
```

Expected:

- the unit test passes
- the CLI prints `Wrote theory artifacts to tools/pathing_data`
- the following files exist:
  - `tools/pathing_data/theory-matrix.json`
  - `tools/pathing_data/theory-matrix.csv`
  - `tools/pathing_data/theory-matrix.md`
  - `tools/pathing_data/canonical-live-cases.json`

- [ ] **Step 5: Commit**

```bash
git add tools/pathing_theory/models.py \
        tools/pathing_theory/canonical.py \
        tools/pathing_theory/renderers.py \
        tools/sim_jump_reach.py \
        tools/tests/test_pathing_canonical_cases.py \
        tools/pathing_data/theory-matrix.json \
        tools/pathing_data/theory-matrix.csv \
        tools/pathing_data/theory-matrix.md \
        tools/pathing_data/canonical-live-cases.json
git commit -m "feat: generate theory-aligned pathing artifacts"
```

### Task 3: Add Theory-To-Live Comparison Reporting

**Files:**
- Create: `tools/pathing_theory/report.py`
- Create: `tools/pathing_theory_report.py`
- Create: `tools/tests/test_pathing_theory_report.py`

- [ ] **Step 1: Write the failing report-classification test**

Create `tools/tests/test_pathing_theory_report.py`:

```python
import json
import tempfile
import unittest
from pathlib import Path

from tools.pathing_theory.report import build_report, classify_live_result, summarize_results


class PathingTheoryReportTests(unittest.TestCase):
    def test_classify_live_result_distinguishes_expected_pass_and_reject(self) -> None:
        self.assertEqual(classify_live_result("pass", "pass"), "expected_pass/live_pass")
        self.assertEqual(classify_live_result("pass", "fail"), "expected_pass/live_fail")
        self.assertEqual(classify_live_result("reject", "reject"), "expected_reject/live_reject")
        self.assertEqual(classify_live_result("reject", "pass"), "expected_reject/live_unexpected_pass")

    def test_summarize_results_counts_each_status(self) -> None:
        rows = [
            {"case_id": "a", "expected_result": "pass", "live_result": "pass"},
            {"case_id": "b", "expected_result": "pass", "live_result": "fail"},
            {"case_id": "c", "expected_result": "reject", "live_result": "reject"},
        ]

        summary = summarize_results(rows)

        self.assertEqual(summary["expected_pass/live_pass"], 1)
        self.assertEqual(summary["expected_pass/live_fail"], 1)
        self.assertEqual(summary["expected_reject/live_reject"], 1)

    def test_build_report_keeps_case_traceability_fields(self) -> None:
        manifest_rows = [
            {
                "case_id": "linear-flat-sprint-mm12-gap5-dy0p0",
                "bucket_id": "linear:flat:sprint:boundary",
                "world_recipe_id": "linear-flat",
                "expected_result": "pass",
            }
        ]
        result_row = {
            "case_id": "linear-flat-sprint-mm12-gap5-dy0p0",
            "live_result": "pass",
            "log_path": "/tmp/mcc-debug/mcc-debug.log",
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            manifest_path = Path(temp_dir) / "manifest.json"
            results_path = Path(temp_dir) / "results.jsonl"
            manifest_path.write_text(json.dumps(manifest_rows), encoding="utf-8")
            results_path.write_text(json.dumps(result_row) + "\n", encoding="utf-8")

            report = build_report(manifest_path, results_path)

        row = report["rows"][0]
        self.assertEqual(row["bucket_id"], "linear:flat:sprint:boundary")
        self.assertEqual(row["world_recipe_id"], "linear-flat")
        self.assertEqual(row["log_path"], "/tmp/mcc-debug/mcc-debug.log")
        self.assertEqual(row["classification"], "expected_pass/live_pass")
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_theory_report -v
```

Expected: FAIL with `ModuleNotFoundError: No module named 'tools.pathing_theory.report'`.

- [ ] **Step 3: Implement report classification, joining, and CLI output**

Create `tools/pathing_theory/report.py`:

```python
import json
from pathlib import Path


def classify_live_result(expected_result: str, live_result: str) -> str:
    if live_result == "invalid_live_case":
        return "invalid_live_case"
    if expected_result == "pass" and live_result == "pass":
        return "expected_pass/live_pass"
    if expected_result == "pass" and live_result == "fail":
        return "expected_pass/live_fail"
    if expected_result == "reject" and live_result == "reject":
        return "expected_reject/live_reject"
    if expected_result == "reject" and live_result == "pass":
        return "expected_reject/live_unexpected_pass"
    return "invalid_live_case"


def summarize_results(rows: list[dict]) -> dict[str, int]:
    summary: dict[str, int] = {}
    for row in rows:
        key = classify_live_result(row["expected_result"], row["live_result"])
        summary[key] = summary.get(key, 0) + 1
    return summary


def build_report(manifest_path: Path, results_path: Path) -> dict:
    manifest_rows = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest_by_case = {row["case_id"]: row for row in manifest_rows}
    result_rows = [
        json.loads(line)
        for line in results_path.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]

    joined_rows: list[dict] = []
    for row in result_rows:
        manifest = manifest_by_case.get(row["case_id"])
        if manifest is None:
            joined_rows.append({**row, "classification": "invalid_live_case"})
            continue
        joined_rows.append(
            {
                **manifest,
                **row,
                "classification": classify_live_result(manifest["expected_result"], row["live_result"]),
            }
        )

    return {
        "rows": joined_rows,
        "summary": summarize_results(joined_rows),
    }
```

Create `tools/pathing_theory_report.py`:

```python
#!/usr/bin/env python3
import argparse
import json
from pathlib import Path

from tools.pathing_theory.report import build_report


def main() -> None:
    parser = argparse.ArgumentParser(description="Join theory-aligned live results back to canonical cases.")
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--results", required=True)
    parser.add_argument("--json-out", required=True)
    args = parser.parse_args()

    report = build_report(Path(args.manifest), Path(args.results))
    Path(args.json_out).write_text(json.dumps(report, indent=2) + "\n")
    print(f"Wrote report to {args.json_out}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 4: Run the report test to verify it passes**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_theory_report -v
```

Expected: PASS with both tests green.

- [ ] **Step 5: Commit**

```bash
git add tools/pathing_theory/report.py \
        tools/pathing_theory_report.py \
        tools/tests/test_pathing_theory_report.py
git commit -m "feat: add theory-to-live pathing report"
```

### Task 4: Refactor The Linear Live Harness To Consume Canonical Cases

**Files:**
- Create: `tools/pathing_live_common.sh`
- Modify: `tools/test-parkour.sh`
- Create: `tools/tests/test_pathing_live_scripts.py`

- [ ] **Step 1: Write the failing linear-suite manifest smoke test**

Create `tools/tests/test_pathing_live_scripts.py`:

```python
import subprocess
import unittest


class PathingLiveScriptTests(unittest.TestCase):
    def test_test_parkour_lists_linear_canonical_cases(self) -> None:
        result = subprocess.run(
            ["bash", "tools/test-parkour.sh", "--list-cases"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("linear-flat-sprint-mm12-gap5-dy0p0", result.stdout)
        self.assertIn("linear-ascend-sprint-mm12-gap2-dy1p0", result.stdout)
        self.assertNotIn("linear-flat-walk-mm12-gap5-dy0p0", result.stdout)
        self.assertNotIn("linear-flat-sprint-mm0-gap3-dy0p0", result.stdout)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the smoke test to verify it fails**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_live_scripts.PathingLiveScriptTests.test_test_parkour_lists_linear_canonical_cases -v
```

Expected: FAIL because `tools/test-parkour.sh` does not understand `--list-cases`.

- [ ] **Step 3: Add shared manifest helpers and make `test-parkour.sh` data-driven**

Create `tools/pathing_live_common.sh`:

```bash
#!/usr/bin/env bash

manifest_cases_for_query() {
    local manifest_path="$1"
    local family_csv="$2"

    python3 - "$manifest_path" "$family_csv" <<'PY'
import json
import sys

manifest = json.load(open(sys.argv[1], "r", encoding="utf-8"))
families = {item for item in sys.argv[2].split(",") if item}
for row in manifest:
    if row["family"] in families and row["movement_mode"] == "sprint" and row["momentum_ticks"] == 12:
        print(row["case_id"])
PY
}

manifest_case_json() {
    local manifest_path="$1"
    local case_id="$2"

    python3 - "$manifest_path" "$case_id" <<'PY'
import json
import sys

manifest = json.load(open(sys.argv[1], "r", encoding="utf-8"))
case_id = sys.argv[2]
row = next(row for row in manifest if row["case_id"] == case_id)
print(json.dumps(row))
PY
}

record_live_result() {
    local results_path="$1"
    local case_json="$2"
    local live_result="$3"
    local log_path="$4"

    python3 - "$results_path" "$case_json" "$live_result" "$log_path" <<'PY'
import json
import sys

row = json.loads(sys.argv[2])
record = {
    "case_id": row["case_id"],
    "bucket_id": row["bucket_id"],
    "world_recipe_id": row["world_recipe_id"],
    "expected_result": row["expected_result"],
    "live_result": sys.argv[3],
    "log_path": sys.argv[4],
}
with open(sys.argv[1], "a", encoding="utf-8") as handle:
    handle.write(json.dumps(record) + "\n")
PY
}
```

Modify the top of `tools/test-parkour.sh`:

```bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"
source "$REPO_ROOT/tools/pathing_live_common.sh"

MANIFEST="$REPO_ROOT/tools/pathing_data/canonical-live-cases.json"
RESULTS_FILE="${RESULTS_FILE:-/tmp/mcc-debug/pathing-live-results.jsonl}"
LOG="/tmp/mcc-debug/mcc-debug.log"

if [[ "${1:-}" == "--list-cases" ]]; then
    manifest_cases_for_query "$MANIFEST" "linear"
    exit 0
fi

: > "$RESULTS_FILE"
```

Add a data-driven runner to `tools/test-parkour.sh`.

First, keep the existing `run_test()` helper but replace the old hardcoded result enum with these normalized live-result values before it returns:

```bash
    local result="invalid_live_case"
    if echo "$path_mgr" | grep -q "complete"; then
        result="pass"
    elif echo "$a_star_result" | grep -q "Failed"; then
        result="reject"
    elif echo "$path_mgr" | grep -q "Replan failed\|Giving up"; then
        result="fail"
    elif echo "$path_exec" | grep -q "FAILED"; then
        result="fail"
    fi
    LAST_RESULT="$result"
```

Next, move the finalized `run_test()` helper into `tools/pathing_live_common.sh` so both theory-aligned shell suites reuse the same MCC log parsing logic and the same `LAST_RESULT` contract.

Then replace the hardcoded case list in `tools/test-parkour.sh` with:

```bash
run_manifest_case() {
    local case_id="$1"
    local case_json
    case_json="$(manifest_case_json "$MANIFEST" "$case_id")"

    read -r world_recipe start_x start_y start_z goal_x goal_y goal_z < <(
        python3 - "$case_json" <<'PY'
import json
import sys

row = json.loads(sys.argv[1])
print(
    row["world_recipe_id"],
    row["start"]["x"],
    row["start"]["y"],
    row["start"]["z"],
    row["goal"]["x"],
    row["goal"]["y"],
    row["goal"]["z"],
)
PY
    )

    local landing_block_y=$(( ${goal_y%.*} - 1 ))

    case "$world_recipe" in
        linear-flat|linear-ascend|linear-descend)
            mc-rcon "fill 95 80 95 115 90 105 air" >/dev/null
            mc-rcon "fill 95 79 95 115 79 105 air" >/dev/null
            mc-rcon "setblock 100 79 100 stone" >/dev/null
            mc-rcon "setblock ${goal_x%.*} ${landing_block_y} ${goal_z%.*} stone" >/dev/null
            ;;
        *)
            echo "Unsupported world recipe for test-parkour.sh: $world_recipe" >&2
            return 1
            ;;
    esac

    run_test "$case_id" "${start_x%.*}" "${start_y%.*}" "${start_z%.*}" "${goal_x%.*}" "${goal_y%.*}" "${goal_z%.*}"
    record_live_result "$RESULTS_FILE" "$case_json" "$LAST_RESULT" "$LOG"
}

while IFS= read -r case_id; do
    run_manifest_case "$case_id"
done < <(manifest_cases_for_query "$MANIFEST" "linear")
```

Leave the existing low-level MCC log parsing logic intact apart from the normalized result names. This task changes case sourcing and result recording, not the underlying MCC log parsing heuristics.

- [ ] **Step 4: Run the smoke test and shell syntax check**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_live_scripts.PathingLiveScriptTests.test_test_parkour_lists_linear_canonical_cases -v
bash -n tools/pathing_live_common.sh tools/test-parkour.sh
```

Expected:

- the unit test passes
- `bash -n` prints nothing and exits `0`

- [ ] **Step 5: Commit**

```bash
git add tools/pathing_live_common.sh \
        tools/test-parkour.sh \
        tools/tests/test_pathing_live_scripts.py
git commit -m "test: make linear pathing suite manifest-driven"
```

### Task 5: Add The Theory-Aligned Neo And Ceiling Suite

**Files:**
- Modify: `tools/tests/test_pathing_live_scripts.py`
- Create: `tools/test-pathing-theory-neo-ceiling.sh`

- [ ] **Step 1: Write the failing neo and ceiling listing test**

Append to `tools/tests/test_pathing_live_scripts.py`:

```python
    def test_test_pathing_theory_neo_ceiling_lists_theory_cases(self) -> None:
        result = subprocess.run(
            ["bash", "tools/test-pathing-theory-neo-ceiling.sh", "--list-cases"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("neo-neo-sprint-mm12-wall1", result.stdout)
        self.assertIn("ceiling-headhitter-sprint-mm12-gap3-ceil2p0", result.stdout)
```

- [ ] **Step 2: Run the listing tests to verify the new one fails**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_live_scripts -v
```

Expected: FAIL because `tools/test-pathing-theory-neo-ceiling.sh` does not exist yet.

- [ ] **Step 3: Implement the theory-aligned neo and ceiling suite**

Create `tools/test-pathing-theory-neo-ceiling.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"
source "$REPO_ROOT/tools/pathing_live_common.sh"

MANIFEST="$REPO_ROOT/tools/pathing_data/canonical-live-cases.json"
RESULTS_FILE="${RESULTS_FILE:-/tmp/mcc-debug/pathing-live-results.jsonl}"
LOG="/tmp/mcc-debug/mcc-debug.log"

if [[ "${1:-}" == "--list-cases" ]]; then
    manifest_cases_for_query "$MANIFEST" "neo,ceiling"
    exit 0
fi

: > "$RESULTS_FILE"

setup_neo_wall() {
    local wall_width="$1"
    local goal_z="$2"
    mc-rcon "fill 95 79 95 115 90 115 air" >/dev/null
    mc-rcon "setblock 100 79 100 stone" >/dev/null
    mc-rcon "fill 101 79 100 101 79 $((99 + wall_width)) stone" >/dev/null
    mc-rcon "setblock 102 79 ${goal_z} stone" >/dev/null
}

setup_ceiling_headhitter() {
    local goal_x="$1"
    local ceiling_y="$2"
    mc-rcon "fill 95 79 95 115 90 105 air" >/dev/null
    mc-rcon "setblock 100 79 100 stone" >/dev/null
    mc-rcon "setblock ${goal_x} 79 100 stone" >/dev/null
    mc-rcon "fill 100 ${ceiling_y} 100 ${goal_x} ${ceiling_y} 100 stone" >/dev/null
}
```

Because Task 4 moved `run_test()` into `tools/pathing_live_common.sh`, this script can reuse that helper directly. Add the per-case runner:

```bash
run_manifest_case() {
    local case_id="$1"
    local case_json
    case_json="$(manifest_case_json "$MANIFEST" "$case_id")"

    read -r world_recipe start_x start_y start_z goal_x goal_y goal_z ceiling_height wall_width < <(
        python3 - "$case_json" <<'PY'
import json
import sys

row = json.loads(sys.argv[1])
print(
    row["world_recipe_id"],
    row["start"]["x"],
    row["start"]["y"],
    row["start"]["z"],
    row["goal"]["x"],
    row["goal"]["y"],
    row["goal"]["z"],
    row.get("ceiling_height", "null"),
    row.get("wall_width", "null"),
)
PY
    )

    case "$world_recipe" in
        neo-wall)
            setup_neo_wall "${wall_width%.*}" "${goal_z%.*}"
            ;;
        ceiling-headhitter)
            setup_ceiling_headhitter "${goal_x%.*}" "${ceiling_height%.*}"
            ;;
        *)
            echo "Unsupported world recipe for theory neo/ceiling suite: $world_recipe" >&2
            return 1
            ;;
    esac

    run_test "$case_id" "${start_x%.*}" "${start_y%.*}" "${start_z%.*}" "${goal_x%.*}" "${goal_y%.*}" "${goal_z%.*}"
    record_live_result "$RESULTS_FILE" "$case_json" "$LAST_RESULT" "$LOG"
}

while IFS= read -r case_id; do
    run_manifest_case "$case_id"
done < <(manifest_cases_for_query "$MANIFEST" "neo,ceiling")
```

Keep the suite scoped to listing plus canonical execution. Do not add mixed-route or braking scenarios here.

- [ ] **Step 4: Run the listing tests and syntax check**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_live_scripts -v
bash -n tools/test-pathing-theory-neo-ceiling.sh
```

Expected:

- all tests in `tools.tests.test_pathing_live_scripts` pass
- shell syntax check exits `0`

- [ ] **Step 5: Commit**

```bash
git add tools/tests/test_pathing_live_scripts.py \
        tools/test-pathing-theory-neo-ceiling.sh
git commit -m "test: add theory-aligned neo and ceiling suite"
```

### Task 6: Document The Workflow And Run Final Regeneration Checks

**Files:**
- Modify: `docs/guide/pathfinding-research.md`
- Modify: `tools/pathing_data/theory-matrix.json`
- Modify: `tools/pathing_data/theory-matrix.csv`
- Modify: `tools/pathing_data/theory-matrix.md`
- Modify: `tools/pathing_data/canonical-live-cases.json`

- [ ] **Step 1: Add the failing documentation check**

Append to `tools/tests/test_pathing_theory_matrix.py`:

```python
    def test_theory_markdown_mentions_canonical_live_coverage(self) -> None:
        markdown = Path("tools/pathing_data/theory-matrix.md").read_text()
        self.assertIn("Canonical live coverage", markdown)
```

Also add the missing import at the top of the test file:

```python
from pathlib import Path
```

- [ ] **Step 2: Run the documentation check to verify it fails**

Run:

```bash
python3 -m unittest tools.tests.test_pathing_theory_matrix.PathingTheoryMatrixTests.test_theory_markdown_mentions_canonical_live_coverage -v
```

Expected: FAIL because the generated Markdown does not yet include that section.

- [ ] **Step 3: Update the Markdown renderer, regenerate artifacts, and document the workflow**

Modify the Markdown generation in `tools/pathing_theory/renderers.py`:

```python
    lines = [
        "# Theory Matrix",
        "",
        "## Canonical live coverage",
        "",
        "This file is generated from `tools/sim_jump_reach.py` and is the first-wave authority",
        "for theory-aligned linear, neo, and headhitter live suites.",
        "",
        "| family | subfamily | movement_mode | case_id | expected_reachable | margin |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
```

Add this section to `docs/guide/pathfinding-research.md`:

```md
## Theory-Aligned Regression Workflow

The first-wave authority now comes from `tools/sim_jump_reach.py`, which writes:

- `tools/pathing_data/theory-matrix.json`
- `tools/pathing_data/theory-matrix.csv`
- `tools/pathing_data/theory-matrix.md`
- `tools/pathing_data/canonical-live-cases.json`

Regenerate them with:

```bash
python3 tools/sim_jump_reach.py --write-artifacts tools/pathing_data
```

Theory-aligned live suites consume the canonical manifest instead of embedding
their own pass and reject expectations:

- `tools/test-parkour.sh`
- `tools/test-pathing-theory-neo-ceiling.sh`

Each theory-aligned live run appends ephemeral JSONL rows to
`/tmp/mcc-debug/pathing-live-results.jsonl`. Join them back to theory with:

```bash
python3 tools/pathing_theory_report.py \
  --manifest tools/pathing_data/canonical-live-cases.json \
  --results /tmp/mcc-debug/pathing-live-results.jsonl \
  --json-out /tmp/mcc-debug/pathing-theory-report.json
```

The specialized live suites remain useful, but they are not part of the
first-wave theory contract:

- `tools/test-pathing-jump-combos.sh`
- `tools/test-pathing-template-regressions.sh`
- `tools/test-pathing-long-routes.sh`
- `tools/test-transition-braking.sh`
```

Regenerate the tracked artifacts:

```bash
python3 tools/sim_jump_reach.py --write-artifacts tools/pathing_data
```

- [ ] **Step 4: Run the full first-wave verification set**

Run:

```bash
python3 -m unittest discover -s tools/tests -p 'test_*.py' -v
python3 tools/sim_jump_reach.py --write-artifacts tools/pathing_data
bash -n tools/pathing_live_common.sh tools/test-parkour.sh tools/test-pathing-theory-neo-ceiling.sh
```

Expected:

- all Python tests pass
- theory artifacts regenerate cleanly
- all three shell scripts pass syntax checks

- [ ] **Step 5: Commit**

```bash
git add docs/guide/pathfinding-research.md \
        tools/pathing_theory/renderers.py \
        tools/pathing_data/theory-matrix.json \
        tools/pathing_data/theory-matrix.csv \
        tools/pathing_data/theory-matrix.md \
        tools/pathing_data/canonical-live-cases.json
git commit -m "docs: document theory-aligned pathing workflow"
```

## Self-Review

### Spec coverage

- Theory authority from `tools/sim_jump_reach.py`
  - Covered by Task 1 and Task 2
- Machine-readable and human-readable outputs from one source
  - Covered by Task 2 and Task 6
- Canonical live coverage instead of replaying every theory case
  - Covered by Task 2, Task 4, and Task 5
- Traceability from live cases back to theory case IDs
  - Covered by Task 2, Task 3, Task 4, and Task 5
- Specialized live suites remain out of the first-wave theory contract
  - Covered by Task 6 documentation
- Current MCC local workflow preserved
  - Covered by Task 4 and Task 5 by reusing `tools/mcc-env.sh`

No uncovered spec requirements remain.

### Placeholder scan

- Searched this plan for `TBD`, `TODO`, and “implement later”
- Replaced vague “refactor harness” wording with concrete files, CLI flags, dataclasses, and commands
- Repeated the exact file paths and commands for every task instead of using “similar to previous task”

### Type consistency

- `TheoryCase`, `CanonicalLiveCase`, and report rows are introduced before any later task consumes them
- `tools/pathing_theory/primitives.py` prevents `tools/sim_jump_reach.py` and `tools/pathing_theory/simulator.py` from importing each other
- `build_theory_cases`, `build_canonical_live_cases`, `write_theory_artifacts`, `classify_live_result`, and `build_report` use consistent names throughout
- `CanonicalLiveCase` now carries `momentum_ticks`, `gap_blocks`, `delta_y`, `ceiling_height`, and `wall_width`, which are the same geometry fields the live suites consume
- The live scripts always consume `tools/pathing_data/canonical-live-cases.json`, not mixed manifest names
