# Zero Replan Live Pathing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate executor-driven replans in deterministic live harness scenarios on `1.21.11-Vanilla`, and add longer-route stability coverage that proves accepted routes finish with `0 replan`.

**Architecture:** Treat any `replan` in accepted deterministic harness scenarios as a bug, not an acceptable recovery path. First lock down harness isolation and `0 replan` assertions so failures are explicit. Then remove per-move-family completion/failure gaps in `WalkTemplate`, `AscendTemplate`, and `DescendTemplate`, and finally add longer multi-segment live routes that combine operations without planner partials or executor retries.

**Tech Stack:** C# 14 / .NET 10, xUnit, MCC local `1.21.11-Vanilla` harness via `tools/mcc-env.sh`, tmux/RCON-driven live tests, MCC pathing core and execution templates.

---

## Current Facts

Latest live evidence from `/tmp/mcc-debug/*/mcc-debug.log`:

- `Flat final stop`: `2` replans
  - `Traverse` failed at `(101.56, 80.00, 100.74)`
  - `Traverse/FinalStop` failed at `(103.36, 80.00, 100.50)`
- `Parkour into turn`: `1` replan
  - final `Traverse/FinalStop` failed around `(122.50, 80.00, 111.36)`
- `Corner ascend around wall`: `1` replan
  - single `Ascend/FinalStop` failed around `(191.38, 81.00, 171.38)`
- `Wall-adjacent descend`: `1` replan
  - single `Descend/FinalStop` failed at `(201.50, 80.00, 200.50)`
- `Ascend chain smoke`: `3` replans
  - all failures occur on chained `Ascend` segments before the partial fallback stops at `(177.46, 83.00, 162.50)`
- `Rejected 3x1 no-run-up gap`: currently not a clean reject
  - planner returns a `Partial` path, then execution replans before failing
- `Rejected 2x1 side-wall jump`: already correct, `0 replan`, direct `A* Failed`

## User Constraints

These constraints override earlier assumptions in this plan:

- MCC currently has no entity collision implementation that would make other players or mobs perturb these tests.
- Other players being online is not itself a movement interference source for this work.
- Residual yaw/pitch between independent scenarios is not a fix target for this plan; templates already steer every tick.
- Residual speed should be recorded for diagnosis, but not “normalized away” inside a route.
- For long accepted routes, residual speed between internal actions is expected and must not be treated as test interference. The route should still finish with `0 replan`.

## Interference Inventory

The harness already disables or controls the environmental variables that matter for this work:

- `difficulty peaceful`
- `gamerule doMobSpawning false`
- fixed test geometry with `fill`/`setblock`
- explicit `tp` before each scenario

Remaining sources of ambiguity that still matter before claiming `0 replan`:

1. Shared live server state
   - The workflow uses shared `mc-*` tmux sessions.
   - This matters for repeatability and logging, but not because of entity collision.

2. Reused MCC session state across scenarios
   - The harness runs multiple scenarios in the same MCC session.
   - Residual speed can carry across scenario boundaries if the next scenario starts too early.
   - Yaw/pitch carryover is not considered a blocker for this plan.

3. Planner timeout / partial-path behavior
   - Some “reject” or long-route scenarios currently hit `A* result: Partial`.
   - Those cases cannot be used as `0 replan` executor proofs until the route size is kept below timeout, or the test is explicitly categorized as a planner-partial case.

4. Current live harness acceptance is weaker than target behavior
   - The harness was updated to accept “already in goal block” completion.
   - That is useful for keeping live validation running, but it currently masks the stronger requirement that accepted deterministic routes should need `0 replan`.

5. Residual-speed observability is currently weak
   - The harness captures final location, but it does not systematically record pre-route speed and per-route terminal speed.
   - We should measure speed, not try to zero it between internal actions.

## Zero-Replan Contract

For this plan, an accepted live pathing scenario passes only if all of the following are true:

- `A* result: Success`
- no `A* result: Partial`
- no `Replan #`
- no `Segment .* FAILED`
- no `Replan failed -- no path found`
- final MCC location is inside the intended goal support block
- `PathMgr` reaches `Navigation complete!`

For rejection scenarios, pass only if:

- `A* result: Failed` or `No path found`
- no `Navigation started`
- no `Replan #`

## File Structure

### Production code

- `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
  - Traverse and diagonal segment runtime; primary target for straight-line and turn-entry `0 replan`.
- `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
  - Single-step ascend execution; primary target for ascend landing and chained ascend stability.
- `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
  - Landing/final-stop descend execution.
- `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
  - Shared grounded completion and braking gate; likely the common source of “already good enough, but segment still fails one tick later”.
- `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
  - Shared geometry and settle helpers.
- `MinecraftClient/Pathing/Execution/PathSegmentManager.cs`
  - Replan handling and live navigation orchestration.
- `MinecraftClient/Pathing/Core/AStarPathFinder.cs`
  - Planner timeout and partial-path behavior.
- `MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs`
  - Run-up / jump-feasibility checks for the `3x1` rejection case.
- `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
  - Supporting parkour admissibility logic.
- `ThirdpartyReference/baritone/src/main/java/baritone/pathing/path/PathExecutor.java`
  - Reference executor behavior for timeout, splice/repath, and movement handoff semantics.
- `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementTraverse.java`
  - Reference traverse completion semantics.
- `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementAscend.java`
  - Reference ascend landing and post-jump settle logic.
- `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementDescend.java`
  - Reference descend safe-mode and landing behavior.
- `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementParkour.java`
  - Reference parkour admissibility and handoff behavior.

### Tests and harness

- `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
  - Deterministic convergence tests for walk, descend, and future ascend/diagonal cases.
- `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`
  - Manager-level replan and already-in-goal behavior.
- `MinecraftClient.Tests/Pathing/Execution/PathExecutorCompletionTests.cs`
  - Multi-segment executor behavior.
- `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  - Parkour and landing handoff cases.
- `tools/test-transition-braking.sh`
  - Short accepted-route live harness.
- `tools/test-pathing-template-regressions.sh`
  - Current mixed live regression suite.
- `tools/test-pathing-long-routes.sh`
  - New long-route `0 replan` live suite.
- `docs/guide/pathfinding-research.md`
  - Pathing behavior contract and live test expectations.

---

### Task 1: Freeze the Zero-Replan Harness Contract

**Files:**
- Modify: `tools/test-transition-braking.sh`
- Modify: `tools/test-pathing-template-regressions.sh`
- Create: `tools/test-pathing-long-routes.sh`
- Modify: `docs/guide/pathfinding-research.md`

- [ ] **Step 1: Add live log helpers that fail on any accepted-route replan**

Add shell helpers to both existing harness scripts:

```bash
count_replans_since() {
    local from_line="$1"
    log_since "$from_line" | grep -Ec "\\[PathMgr\\] Replan #|\\[PathExec\\] Segment .* FAILED" || true
}

assert_no_replans_since() {
    local from_line="$1"
    local count
    count="$(count_replans_since "$from_line")"
    if [[ "$count" != "0" ]]; then
        echo "Expected 0 replans, saw $count" >&2
        log_since "$from_line" >&2
        return 1
    fi
}

assert_no_partial_since() {
    local from_line="$1"
    if log_since "$from_line" | grep -Fq "[Navigate] A* result: Partial"; then
        echo "Expected full success path, saw partial path" >&2
        log_since "$from_line" >&2
        return 1
    fi
}
```

- [ ] **Step 2: Normalize only the state that should differ between independent scenarios**

For accepted live scenarios, add:

```bash
mc-rcon "effect clear $USERNAME" >/dev/null 2>&1 || true
mc-rcon "tp $USERNAME 100.5 80 100.5" >/dev/null
sleep 2
send_mcc "debug state"
```

Do not add special yaw/pitch normalization. Record the pre-route state instead.
Do reset position and allow enough time that residual speed from the previous independent scenario is observable in logs.

- [ ] **Step 3: Separate accepted-route assertions from rejection-route assertions**

Update accepted-route scenarios to require:

```bash
wait_for_navigation "$start_line" 30
assert_no_partial_since "$start_line"
assert_no_replans_since "$start_line"
```

Update rejection scenarios to require:

```bash
wait_for_failure_signal "$start_line" 20
if log_since "$start_line" | grep -Eq "\\[PathMgr\\] Replan #|\\[PathExec\\] Segment .* FAILED"; then
    echo "Expected direct rejection, saw execution replan" >&2
    return 1
fi
```

- [ ] **Step 4: Create a new long-route live harness**

Create `tools/test-pathing-long-routes.sh` with three accepted-route buckets:

```bash
run_same_move_routes
run_mixed_move_routes
run_turn_density_routes
```

Each accepted route must assert:

```bash
wait_for_navigation "$start_line" 45
assert_no_partial_since "$start_line"
assert_no_replans_since "$start_line"
read -r x y z <<< "$(capture_debug_location)"
assert_inside_goal_block "$x" "$y" "$z" "$goal_x" "$goal_y" "$goal_z"
```

Each accepted route must also log:

```bash
capture_debug_state_before_route
capture_debug_state_after_route
```

to record start/end speed and location for diagnosis. Do not reset any state between internal actions of a single route.

- [ ] **Step 5: Document the zero-replan live contract**

Add a short section to `docs/guide/pathfinding-research.md`:

```md
### Deterministic live route contract

For the short-route and long-route 1.21.11 live harnesses, accepted routes must complete with:

- `A* result: Success`
- `0 replan`
- `0` template segment failures
- final position inside the goal support block

Rejection scenarios must fail before execution starts.
```

- [ ] **Step 6: Run harness baselines and record current failures**

Run:

```bash
source tools/mcc-env.sh
bash tools/test-transition-braking.sh 1.21.11-Vanilla
bash tools/test-pathing-template-regressions.sh 1.21.11-Vanilla
```

Expected right now: FAIL because the accepted scenarios still produce replans.

---

### Task 2: Baritone Reference Pass Before Code Changes

**Files:**
- Read: `ThirdpartyReference/baritone/src/main/java/baritone/pathing/path/PathExecutor.java`
- Read: `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementTraverse.java`
- Read: `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementAscend.java`
- Read: `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementDescend.java`
- Read: `ThirdpartyReference/baritone/src/main/java/baritone/pathing/movement/movements/MovementParkour.java`
- Modify: `docs/guide/pathfinding-research.md`

- [ ] **Step 1: Extract only the behavior that applies to MCC’s zero-replan goal**

Summarize, in MCC terms:

- when Baritone considers a movement complete
- when it keeps controlling after landing instead of immediately failing
- how it handles path executor timeout and repath
- which parkour and descend transitions depend on next-movement awareness

- [ ] **Step 2: Write down the allowed and disallowed Baritone borrow list**

Add to `docs/guide/pathfinding-research.md`:

```md
### Baritone reference notes for zero-replan work

Borrow:
- landing-aware completion
- next-movement-aware descend/ascend handoff
- conservative parkour admissibility

Do not borrow:
- GoalBlock occupancy semantics that allow success with sloppy live execution
- executor repath tolerance as a substitute for deterministic harness stability
```

- [ ] **Step 3: Do not change MCC code in this task**

This task is design grounding only. The output is a short written comparison that later tasks can cite.

---

### Task 3: Reproduce Each Replan Family With Deterministic Tests

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathExecutorCompletionTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`

- [ ] **Step 1: Add a walk final-stop red test for the live `(103.36, 80.00, 100.50)` case**

Add a test that seeds physics at the live near-goal position and expects completion without failure:

```csharp
[Fact]
public void WalkTemplate_FinalStop_Completes_FromLiveNearGoalState_WithoutFailure()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 95, max: 115);
    var segment = new PathSegment
    {
        Start = new Location(102.5, 80, 100.5),
        End = new Location(103.5, 80, 100.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new WalkTemplate(segment, null);
    var physics = new PlayerPhysics
    {
        Position = new Vec3d(103.36, 80.0, 100.50),
        DeltaMovement = new Vec3d(0.0346, 0.0, 0.0),
        OnGround = true,
        MovementSpeed = 0.1f,
        Yaw = 270f
    };

    TemplateState state = TemplateSimulationRunner.Run(template, physics, world, 40, out _);
    Assert.Equal(TemplateState.Complete, state);
}
```

- [ ] **Step 2: Add an ascend final-stop red test for the live `(191.38, 81.00, 171.38)` case**

```csharp
[Fact]
public void AscendTemplate_FinalStop_Completes_FromLiveLandingState_WithoutFailure()
{
    // build the same corner-ascend world as the live harness
    // seed physics at the live failure position
    // expect TemplateState.Complete within a short horizon
}
```

- [ ] **Step 3: Add a descend final-stop red test for the live `(201.50, 80.00, 200.50)` case**

```csharp
[Fact]
public void DescendTemplate_FinalStop_Completes_FromLiveLandingState_WithoutFailure()
{
    // build the wall-adjacent descend world
    // seed physics at the live failure position
    // expect TemplateState.Complete
}
```

- [ ] **Step 4: Add a manager-level red test that accepted routes finish with zero replans**

Extend `PathSegmentManagerTests.cs` with a short accepted path:

```csharp
[Fact]
public void Tick_ShortAcceptedPath_CompletesWithoutIncrementingReplanCount()
{
    // start manager with a 3-node flat path
    // run ticks until completion
    // assert manager.ReplanCount == 0
}
```

- [ ] **Step 5: Run the focused red test set**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~GroundedTemplateConvergenceTests|FullyQualifiedName~PathExecutorCompletionTests|FullyQualifiedName~PathSegmentManagerTests|FullyQualifiedName~SprintJumpTemplateScenarioTests" -v minimal
```

Expected initially: FAIL on the new live-parity cases.

---

### Task 4: Remove Zero-Replan Gaps In Walk And FinalStop Execution

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`

- [ ] **Step 1: Make `WalkTemplate` prefer completion over fail once goal support is already valid**

Before the template returns `TemplateState.Failed`, ensure it checks an explicit “good enough for this segment” predicate first:

```csharp
if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
    return TemplateState.Complete;

if (_stuckTicks > 40 || _tickCount > maxTicks)
    return TemplateState.Failed;
```

The final implementation must preserve that order even when the bot is slightly off center but already inside valid goal support.

- [ ] **Step 2: Tighten final-stop control so the last traverse segment stops without lateral drift**

Update planner/controller logic for `PathTransitionType.FinalStop` to penalize cross-axis drift near the end plane:

```csharp
double lateralError = TemplateHelper.CrossTrackDistance(pos, segment);
if (segment.ExitTransition == PathTransitionType.FinalStop && lateralError > 0.10)
{
    // reduce forward carry and bias facing back to segment heading
}
```

- [ ] **Step 3: Ensure `ContinueStraight -> FinalStop` handoff drops sprint early enough**

Use the final live traces as the acceptance reference:

- flat final stop must not replan at segment `0`
- final stop segment must complete before the `(103.36, 80.00, 100.50)` failure state
- do not rely on yaw/pitch reset to make this pass
- residual speed within the route is allowed as long as the route still completes with `0 replan`

- [ ] **Step 4: Run focused deterministic tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~WalkTemplate_FinalStop|FullyQualifiedName~PathExecutorCompletionTests|FullyQualifiedName~PathSegmentManagerTests" -v minimal
```

Expected: PASS with the new walk/final-stop tests green.

---

### Task 5: Remove Zero-Replan Gaps In Ascend Execution

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`

- [ ] **Step 1: Add a dedicated post-landing settle phase in `AscendTemplate`**

Current code always sets:

```csharp
input.Forward = true;
input.Sprint = true;
```

Replace that with a landing-aware phase:

```csharp
bool landedOnTargetLevel = physics.OnGround && Math.Abs(dy) < 0.2;
if (landedOnTargetLevel)
{
    GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
    if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
        return TemplateState.Complete;
}
else
{
    input.Forward = true;
    input.Sprint = true;
    if (physics.OnGround && dy > 0.1)
        input.Jump = true;
}
```

The acceptance bar is not “zero residual speed after each ascend”. The acceptance bar is “the accepted route continues without a replan”.

- [ ] **Step 2: Add live-parity ascend tests**

Add deterministic tests for:

- corner ascend final stop
- chained ascend middle segment
- chained ascend final segment

- [ ] **Step 3: Run the focused ascend suite**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Ascend|FullyQualifiedName~Corner ascend|FullyQualifiedName~PathSegmentManagerTests" -v minimal
```

Expected: PASS with no new regressions in existing ascend tests.

---

### Task 6: Remove Zero-Replan Gaps In Descend Execution

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`

- [ ] **Step 1: Make landing-state final stop complete immediately when support is already valid**

Preserve the landing-phase completion ordering:

```csharp
if (physics.OnGround && Math.Abs(dy) < 1.0)
{
    GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
    if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
        return TemplateState.Complete;
}
```

The acceptance case is the exact live failure state at `(201.50, 80.00, 200.50)`.

- [ ] **Step 2: Add descend live-parity tests**

Add tests that seed the exact live landing state and assert `TemplateState.Complete` rather than `Failed`.

- [ ] **Step 3: Run the focused descend suite**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~DescendTemplate|FullyQualifiedName~GroundedTemplateConvergenceTests" -v minimal
```

Expected: PASS with the new descend test green.

---

### Task 7: Make Rejection Scenarios Reject Before Execution Starts

**Files:**
- Modify: `MinecraftClient/Pathing/Core/AStarPathFinder.cs`
- Modify: `MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs`
- Modify: `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
- Modify: `tools/test-pathing-template-regressions.sh`

- [ ] **Step 1: Add a deterministic rejection test for the `3x1 no-run-up` case**

Create or extend a pathfinder-level test that asserts:

```csharp
Assert.Equal(PathStatus.Failed, result.Status);
Assert.Empty(PathSegmentBuilder.FromPath(result.Path));
```

for the exact live `141.5 -> 144.5/81/138.5` layout.

- [ ] **Step 2: Tighten parkour admissibility before planner partial fallback is considered acceptable**

Review the current path:

- `Traverse -> Ascend` to a partial fallback at `(143,81,138)`

The fix should make this route non-admissible in the first place if the intended gap cannot be completed with valid run-up.
- Use `ThirdpartyReference/baritone/.../MovementParkour.java` and the prior admissibility spec as references, but keep MCC’s execution contract stricter: direct reject in this harness.

- [ ] **Step 3: Re-run rejection-only live validation**

Run:

```bash
source tools/mcc-env.sh
bash tools/test-pathing-template-regressions.sh 1.21.11-Vanilla
```

Expected for rejection scenarios:

- `2x1 side-wall jump`: direct reject, `0 replan`
- `3x1 no-run-up gap`: direct reject, `0 replan`

---

### Task 8: Add Long-Route Zero-Replan Stability Coverage

**Files:**
- Create: `tools/test-pathing-long-routes.sh`
- Modify: `docs/guide/pathfinding-research.md`

- [ ] **Step 1: Add same-operation long routes**

Use route lengths that remain comfortably below planner timeout:

- straight traverse chain: `8-12` blocks
- diagonal zig-zag chain: `6-8` segments
- ascend staircase: `4-6` ascends
- descend staircase: `4-6` descends
- aligned parkour chain: `3-4` jumps

Each must require:

- `A* result: Success`
- `0 replan`
- final location inside goal block

- [ ] **Step 2: Add mixed-operation long routes**

Recommended mixed routes:

- `Traverse -> Turn -> Parkour -> Turn -> Traverse -> FinalStop`
- `Diagonal -> Ascend -> Traverse -> Descend -> FinalStop`
- `Traverse -> Ascend -> Traverse -> Parkour -> Descend -> FinalStop`

Keep all mixed routes inside a small pre-cleared test region so chunk loading is not the variable under test.
Do not reset speed or orientation between internal actions of a route. A route only passes if the naturally carried speed across those actions still yields `0 replan`.

- [ ] **Step 3: Add turn-density routes**

Add one route with frequent heading changes and no jumps:

- `8-10` short traverse/diagonal segments
- every segment should change heading
- must still finish with `0 replan`

- [ ] **Step 4: Add speed-carry long routes**

Add one route each for:

- repeated `Traverse -> Ascend`
- repeated `Traverse -> Descend`
- repeated `Traverse -> Parkour`

These routes exist specifically to prove that residual speed between actions does not force replans in deterministic conditions.

- [ ] **Step 5: Run the long-route harness**

Run:

```bash
source tools/mcc-env.sh
bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Expected: PASS with every accepted route showing `0 replan`.

---

### Task 9: Full Verification

**Files:**
- No new files

- [ ] **Step 1: Run deterministic test coverage**

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution" -v minimal
```

Expected: PASS, `0 failed`.

- [ ] **Step 2: Run release build**

```bash
source tools/mcc-env.sh
mcc-build
```

Expected: `Build succeeded. 0 Warning(s), 0 Error(s)`.

- [ ] **Step 3: Run short live suites**

```bash
source tools/mcc-env.sh
bash tools/test-transition-braking.sh 1.21.11-Vanilla
bash tools/test-pathing-template-regressions.sh 1.21.11-Vanilla
```

Expected:

- accepted scenarios: `0 replan`
- rejection scenarios: direct reject, `0 replan`

- [ ] **Step 4: Run long-route live suite**

```bash
source tools/mcc-env.sh
bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Expected: PASS, no accepted route triggers `replan`.

## Self-Review

Spec coverage:

- `0 replan` short deterministic live scenarios: covered by Tasks 1-6
- interference inventory first: covered near top of this plan, revised to remove non-applicable entity and yaw/pitch concerns
- Baritone comparison before code changes: covered by Task 2
- rejection cleanup: covered by Task 7
- long-path stability coverage, including speed-carry routes: covered by Task 8

Placeholder scan:

- No `TODO` or `TBD` placeholders remain
- Concrete files, scenarios, and commands are included

Type consistency:

- File paths and move/template names match current codebase names:
  - `WalkTemplate`
  - `AscendTemplate`
  - `DescendTemplate`
  - `GroundedSegmentController`
  - `AStarPathFinder`
