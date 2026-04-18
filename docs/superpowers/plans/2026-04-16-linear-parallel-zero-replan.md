# Linear Parallel Zero-Replan Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the `tools/test-parkour.py --filter linear --parallel 6` run on `1.21.11-Vanilla` execute all theory-allowed `linear` cases as `pass`, all theory-forbidden cases as `reject`, and detect any `replan` or turn-in-place stall as a test failure.

**Architecture:** Treat the current problem as two coupled systems. First, harden the live harness so parallel runs are trustworthy: worker startup must be deterministic, outcome classification must parse `[PathMetric]` telemetry, and the runner must detect turn-in-place stalls instead of inferring success from `Navigation complete!` plus a loose proximity check. Second, add focused C# regressions for the four currently exposed linear failures, then tune the parkour execution templates and grounded completion thresholds until those scenarios complete without replans, overshoot, or stall behavior.

**Tech Stack:** Python 3, pytest/unittest, C# 14 /.NET 10, MCC pathing runtime, local `1.21.11-Vanilla` server harness, RCON, tmux-backed `mcc-debug`.

---

## Current Facts

- Latest valid executed parallel live run used:
  - `python3 tools/test-parkour.py --filter linear --parallel 6 --version 1.21.11-Vanilla --results /tmp/linear-live-valid-20260416.jsonl`
- That run built all 25 courses, launched 6 workers, and executed 13 cases before group-level stop-at-first-failure logic skipped the rest of each failing group.
- Current observed live mismatches are:
  - `linear-flat-gap1`: expected `pass`, got `fail`
  - `linear-ascend-gap2-dy+1`: expected `pass`, got `fail`
  - `linear-descend-gap2-dy-2`: expected `pass`, got `fail`
  - `linear-descend-gap4-dy-1`: expected `pass`, got `fail`
- Current harness behavior is now trustworthy for this task:
  - parses `[PathMetric]` telemetry
  - fails any pass-case with `replan_count > 0`
  - fails any pass-case with turn-stall detection
  - persists `replan_count`, `turn_stall_count`, `near_goal`, `total_ticks`, and `final_position` to JSONL
- Current C# regression status is narrower than live status:
  - targeted `SprintJumpTemplate` regressions were added and used to drive several execution fixes
  - local C# red lights no longer fully predict the live failure surface
  - the next TDD cycle must add regression coverage for `linear-flat-gap1`, `linear-ascend-gap2-dy+1`, and `linear-descend-gap4-dy-1`, not just the original four
- The parallel worker startup bug in `.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh` remains fixed and covered by tests.
- Local references are available if needed:
  - Decompiled server source under `$MCC_REPO/MinecraftOfficial/<version>-decompiled/`
  - `ThirdpartyReference/baritone`

## File Structure

### Harness / integration loop

- Modify: `.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh`
  - Fix ambiguous argument parsing when the output ini already exists.
- Modify: `tools/test-parkour.py`
  - Add strict live metrics parsing.
  - Add turn-in-place stall detection during navigation.
  - Persist replan/turn metrics into JSONL.
  - Keep parallel worker behavior, but make results trustworthy.
- Modify: `tools/tests/test_pathing_live_scripts.py`
  - Keep startup/config regression coverage and align list-case assertions with the current script interface.
- Create: `tools/tests/test_test_parkour_metrics.py`
  - Focused tests for log parsing, outcome classification, and turn-in-place detection.

### Runtime / movement fixes

- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/LinearParkourScenarioBuilder.cs`
  - Shared helper to construct the same linear layouts the live harness uses.
- Modify: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
  - Planner/executor regressions that mirror the current valid live mismatches.
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  - Fine-grained parkour landing / overshoot regressions, including currently live-failing flat/ascend/descend chains.
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`
  - No-replan regressions for accepted linear chains and any newly observed live failures.
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  - Air-brake and landing-completion behavior for long flat / falling jumps.
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
  - Final-stop and prepare-jump completion thresholds.
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
  - Lookahead braking when the next expected state is a true stop.
- Modify: `MinecraftClient/Pathing/Execution/TemplateHelper.cs`
  - Extract any shared “still moving too fast to count as settled” helpers used by the above.

## Task 1: Lock Down The Parallel Harness Contract

**Files:**
- Modify: `.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh`
- Modify: `tools/tests/test_pathing_live_scripts.py`

- [ ] **Step 1: Write the failing config regression test**

Add this test to `tools/tests/test_pathing_live_scripts.py`:

```python
def test_prepare_offline_config_treats_existing_output_ini_as_output_not_template(self) -> None:
    with tempfile.TemporaryDirectory() as tempdir:
        temp_path = Path(tempdir)
        output_ini = temp_path / "MinecraftClient.debug.ini"
        output_ini.write_text(
            "\n".join(
                [
                    "[Main.General]",
                    'Account = { Login = "OldBot", Password = "" }',
                    'AccountType = "microsoft"',
                    "",
                    "[Main.Advanced]",
                    'MinecraftVersion = "auto"',
                    "TerrainAndMovements = false",
                    "InventoryHandling = false",
                    "EntityHandling = false",
                    "AutoRespawn = false",
                    "",
                ]
            ),
            encoding="utf-8",
        )

        result = subprocess.run(
            [
                "bash",
                str(REPO_ROOT / ".skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh"),
                str(output_ini),
                "1.21.11",
                "MCCBot1",
            ],
            check=False,
            capture_output=True,
            text=True,
            cwd=temp_path,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse((temp_path / "1.21.11").exists())
        content = output_ini.read_text(encoding="utf-8")
        self.assertIn('AccountType = "mojang"', content)
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
python3 -m pytest -q tools/tests/test_pathing_live_scripts.py -k existing_output_ini_as_output_not_template
```

Expected:

```text
FAILED ... AssertionError: True is not false
```

- [ ] **Step 3: Implement the minimal parser fix**

Update `.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh`:

```bash
if [[ $# -ge 3 && "$2" == *.ini ]]; then
    TEMPLATE_INI="$1"
    OUTPUT_INI="$2"
    MC_VERSION="$3"
    LOGIN_NAME="${4:-MCCBot}"
else
    OUTPUT_INI="$1"
    MC_VERSION="$2"
    LOGIN_NAME="${3:-MCCBot}"
fi
```

- [ ] **Step 4: Run the full live-script test file**

Run:

```bash
python3 -m pytest -q tools/tests/test_pathing_live_scripts.py
```

Expected:

```text
5 passed
```

- [ ] **Step 5: Commit**

```bash
git add .skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh \
        tools/tests/test_pathing_live_scripts.py
git commit -m "test-harness: fix parallel worker config bootstrap"
```

## Task 2: Make The Harness Enforce Zero Replan And Turn-Stall Failures

**Files:**
- Create: `tools/tests/test_test_parkour_metrics.py`
- Modify: `tools/test-parkour.py`

- [ ] **Step 1: Write parser/classification tests first**

Create `tools/tests/test_test_parkour_metrics.py` with focused tests like:

```python
def test_classify_outcome_pass_requires_route_complete_and_zero_replans():
    metrics = LiveMetrics(
        planner_status="Success",
        route_complete_count=1,
        navigation_complete_count=1,
        replan_count=0,
        turn_stall_count=0,
    )
    assert classify_outcome(metrics) == "pass"


def test_classify_outcome_replan_is_fail():
    metrics = LiveMetrics(
        planner_status="Success",
        route_complete_count=1,
        navigation_complete_count=1,
        replan_count=1,
        turn_stall_count=0,
    )
    assert classify_outcome(metrics) == "fail"


def test_classify_outcome_turn_stall_is_fail():
    metrics = LiveMetrics(
        planner_status="Success",
        route_complete_count=1,
        navigation_complete_count=1,
        replan_count=0,
        turn_stall_count=1,
    )
    assert classify_outcome(metrics) == "fail"


def test_detect_turn_stall_requires_low_motion_and_large_yaw_change():
    samples = [
        NavigationSample(x=100.5, y=80.0, z=100.5, yaw=0.0),
        NavigationSample(x=100.6, y=80.0, z=100.5, yaw=95.0),
        NavigationSample(x=100.6, y=80.0, z=100.5, yaw=185.0),
    ]
    assert detect_turn_stall(samples) is True
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py
```

Expected:

```text
FAILED ... NameError / AttributeError for LiveMetrics, classify_outcome, NavigationSample, detect_turn_stall
```

- [ ] **Step 3: Add strict live metrics and turn-stall sampling**

In `tools/test-parkour.py`, add the minimal structures and parsing helpers:

```python
@dataclass
class NavigationSample:
    x: float
    y: float
    z: float
    yaw: float


@dataclass
class LiveMetrics:
    planner_status: str | None = None
    route_complete_count: int = 0
    navigation_complete_count: int = 0
    replan_count: int = 0
    replan_failed_count: int = 0
    segment_failed_count: int = 0
    turn_stall_count: int = 0


def classify_outcome(metrics: LiveMetrics) -> str:
    if metrics.replan_count or metrics.replan_failed_count or metrics.segment_failed_count or metrics.turn_stall_count:
        return "fail"
    if metrics.planner_status in {"Partial", "Failed"}:
        return "reject"
    if metrics.route_complete_count or metrics.navigation_complete_count:
        return "pass"
    return "invalid_live_case"
```

Also add navigation polling that samples both position and rotation during `wait_seconds`:

```python
def get_player_pose(rcon: RconClient, username: str) -> NavigationSample | None:
    pos = rcon.command(f"data get entity {username} Pos")
    rot = rcon.command(f"data get entity {username} Rotation")
    ...
    return NavigationSample(x, y, z, yaw)
```

```python
def detect_turn_stall(samples: list[NavigationSample]) -> bool:
    if len(samples) < 3:
        return False
    total_motion = sum(math.dist((a.x, a.z), (b.x, b.z)) for a, b in zip(samples, samples[1:]))
    total_yaw = sum(abs(normalize_yaw_delta(b.yaw - a.yaw)) for a, b in zip(samples, samples[1:]))
    return total_motion < 1.0 and total_yaw >= 180.0
```

- [ ] **Step 4: Persist the new metrics into JSONL**

Extend the JSONL write in `tools/test-parkour.py`:

```python
f.write(json.dumps({
    "case_id": case.case_id,
    "expected": case.expected,
    "outcome": result.outcome,
    "matched": result.matched_expected,
    "worker": worker_id,
    "planner_status": result.metrics.planner_status,
    "replan_count": result.metrics.replan_count,
    "replan_failed_count": result.metrics.replan_failed_count,
    "segment_failed_count": result.metrics.segment_failed_count,
    "turn_stall_count": result.metrics.turn_stall_count,
}) + "\n")
```

- [ ] **Step 5: Verify the harness tests pass**

Run:

```bash
python3 -m pytest -q tools/tests/test_test_parkour_metrics.py
python3 -m pytest -q tools/tests/test_pathing_live_scripts.py
```

Expected:

```text
all green
```

- [ ] **Step 6: Commit**

```bash
git add tools/test-parkour.py \
        tools/tests/test_test_parkour_metrics.py \
        tools/tests/test_pathing_live_scripts.py
git commit -m "test-harness: enforce zero-replan and turn-stall failures"
```

## Task 3: Reproduce The Four Live Linear Failures In C# Tests

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/LinearParkourScenarioBuilder.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`

- [ ] **Step 1: Add a shared linear world builder helper**

Create `MinecraftClient.Tests/Pathing/Execution/Scenarios/LinearParkourScenarioBuilder.cs`:

```csharp
using MinecraftClient.Mapping;

namespace MinecraftClient.Tests.Pathing.Execution.Scenarios;

internal static class LinearParkourScenarioBuilder
{
    internal static World Build(int gap, int deltaY, out Location start, out Location end)
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 90, max: 140);
        FlatWorldTestBuilder.ClearBox(world, 96, 70, 96, 132, 90, 104);

        start = new Location(100.5, 80, 100.5);
        int floorY = 79;
        FlatWorldTestBuilder.FillSolid(world, 100, floorY, 100, 103, floorY, 100);

        int lastX = 103;
        int lastY = floorY;
        for (int i = 0; i < 3; i++)
        {
            int platX = lastX + gap + 1;
            int platY = lastY + deltaY;
            FlatWorldTestBuilder.SetSolid(world, platX, platY, 100);
            lastX = platX;
            lastY = platY;
        }

        end = new Location(lastX + 0.5, lastY + 1, 100.5);
        return world;
    }
}
```

- [ ] **Step 2: Write the failing manager-level regressions**

Extend `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`:

```csharp
[Fact]
public void Tick_LinearAscendGap1DyPlus1_CompletesWithoutReplan()
{
    World world = LinearParkourScenarioBuilder.Build(gap: 1, deltaY: 1, out Location start, out Location end);
    var result = BuildLinearPathResult(start, end, MoveType.Parkour);
    var manager = new PathSegmentManager(debugLog: _ => { }, infoLog: _ => { });
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(start, yaw: 270f);
    var input = new MovementInput();

    manager.StartNavigation(new GoalBlock((int)Math.Floor(end.X), (int)end.Y, (int)Math.Floor(end.Z)), result);
    RunManager(manager, physics, input, world, maxTicks: 420);

    Assert.False(manager.IsNavigating);
    Assert.Equal(0, manager.ReplanCount);
    Assert.True(Math.Abs(physics.Position.X - end.X) < 1.0);
}
```

Mirror the same structure for:
- `gap: 2, deltaY: -2`
- `gap: 3, deltaY: -1`
- `gap: 4, deltaY: 0`

- [ ] **Step 3: Run those tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~PathSegmentManagerTests|FullyQualifiedName~LivePathingRegressionTests|FullyQualifiedName~SprintJumpTemplateScenarioTests" -v minimal
```

Expected:

```text
FAIL with at least the four new linear regressions
```

- [ ] **Step 4: Add lower-level template regressions for overshoot / fall**

Extend `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs` with:

```csharp
[Fact]
public void SprintJumpTemplate_LinearFlatGap4_FinalStop_StopsInsideTargetBlock() { ... }

[Fact]
public void SprintJumpTemplate_LinearDescendGap3DyMinus1_FinalStop_DoesNotOvershoot() { ... }

[Fact]
public void SprintJumpTemplate_LinearAscendGap1DyPlus1_FinalStop_DoesNotFallAfterLanding() { ... }
```

These tests should assert both:

```csharp
Assert.Equal(TemplateState.Complete, state);
Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
```

- [ ] **Step 5: Commit the failing tests only after they are green later**

No commit here yet. Keep them staged with the implementation in Task 4.

## Task 4: Fix Parkour Execution For The Four Exposed Linear Failures

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`

- [ ] **Step 1: Fix long-jump overshoot before touching completion gates**

In `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`, prefer early release for true final stops when the predicted holding path overshoots the landing block:

```csharp
if (_segment.ExitTransition == PathTransitionType.FinalStop && !physics.OnGround)
{
    Location? landingIfHolding = PredictLandingPosition(physics, world, holdForward: true, holdSprint: true);
    Location? landingIfReleased = PredictLandingPosition(physics, world, holdForward: false, holdSprint: false);

    if (landingIfHolding is not null
        && landingIfReleased is not null
        && !TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfHolding.Value, ExpectedEnd)
        && TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfReleased.Value, ExpectedEnd))
    {
        input.Forward = false;
        input.Sprint = false;
    }
}
```

- [ ] **Step 2: Tighten final-stop completion so “complete” cannot happen short of the block**

In `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`:

```csharp
if (segment.ExitTransition == PathTransitionType.FinalStop
    && physics.OnGround
    && TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, segment.End)
    && !TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, segment.End))
{
    return TemplateHelper.GetHorizontalSpeed(physics) <= 0.05;
}
```

Do not allow `FinalStop` completion through looser `IsNear(...)` checks.

- [ ] **Step 3: Give landing-recovery / prepare-jump transitions a stricter settle plane**

In `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`, keep `PrepareJump` from completing while the player is still materially before the end plane:

```csharp
if (segment.MoveType == MoveType.Parkour
    && segment.ExitTransition == PathTransitionType.PrepareJump)
{
    return physics.OnGround
        && TemplateHelper.RemainingDistanceAlongSegment(pos, segment) <= 0.20
        && exitSpeed >= segment.ExitHints.MinExitSpeed;
}
```

- [ ] **Step 4: Re-run the focused C# regressions until green**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~SprintJumpTemplateScenarioTests|FullyQualifiedName~PathSegmentManagerTests|FullyQualifiedName~LivePathingRegressionTests" -v minimal
```

Expected:

```text
PASS for the four new linear regressions and no collateral failures in the touched suites
```

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs \
        MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs \
        MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs \
        MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs \
        MinecraftClient.Tests/Pathing/Execution/Scenarios/LinearParkourScenarioBuilder.cs \
        MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs \
        MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs \
        MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs
git commit -m "pathing: stabilize linear parkour landings"
```

## Task 5: Prove The Full Parallel Linear Matrix On 1.21.11

**Files:**
- Modify: `tools/test-parkour.py` only if the previous tasks revealed missing diagnostics
- Verify: `/tmp/main-linear-after-update-parallel-fixed.jsonl` replacement run

- [ ] **Step 1: Run the strict parallel linear sweep**

Run:

```bash
export MCC_SERVERS=/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/MinecraftOfficial/downloads
python3 tools/test-parkour.py --filter linear --parallel 6 --results /tmp/linear-parallel-zero-replan-final.jsonl
```

Expected:

```text
25/25 matched expectations
```

- [ ] **Step 2: Machine-check the JSONL**

Run:

```bash
python3 - <<'PY'
import json
from pathlib import Path
rows = [json.loads(line) for line in Path('/tmp/linear-parallel-zero-replan-final.jsonl').read_text().splitlines() if line.strip()]
print('rows', len(rows))
print('mismatches', sum(not r['matched'] for r in rows))
print('pass_replan_nonzero', sum(r['outcome'] == 'pass' and r.get('replan_count', 0) != 0 for r in rows))
print('pass_turn_nonzero', sum(r['outcome'] == 'pass' and r.get('turn_stall_count', 0) != 0 for r in rows))
PY
```

Expected:

```text
rows 25
mismatches 0
pass_replan_nonzero 0
pass_turn_nonzero 0
```

- [ ] **Step 3: Save the observed failing logs if anything remains**

If the run is not green, copy the live log roots before retrying:

```bash
cp /tmp/main-linear-after-update-parallel-fixed.jsonl /tmp/linear-parallel-investigation-last.jsonl
cp /tmp/mcc-debug/parkour-*/mcc-debug.log /tmp/ 2>/dev/null || true
```

- [ ] **Step 4: Commit the final harness/result adjustments**

```bash
git add tools/test-parkour.py tools/tests/test_test_parkour_metrics.py
git commit -m "test-harness: verify linear zero-replan parallel sweep"
```

## Self-Review

- Spec coverage:
  - Parallel run on `1.21.11`: covered in Task 5.
  - Collect test results first: covered in Current Facts and Task 5.
  - Complete all theory-allowed `linear` passes: covered in Tasks 3-5.
  - Zero replan detection: covered in Task 2 and Task 5 JSONL validation.
  - Zero turn-in-place detection: covered in Task 2 via turn-stall sampling and Task 5 JSONL validation.
  - Autonomous TDD flow: every production change task begins with failing tests.
- Placeholder scan:
  - No `TODO`/`TBD`.
  - Every code-change task names exact files and commands.
- Type consistency:
  - `LiveMetrics`, `NavigationSample`, `detect_turn_stall`, and `classify_outcome` are named consistently across harness tasks.
  - `LinearParkourScenarioBuilder.Build(...)` is reused consistently across C# regression tasks.
