# Jump-Entry Direct Yaw Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove unnecessary yaw smoothing in jump-entry states so opposite-yaw jump starts commit immediately without changing normal walk, descend, climb, or final-stop behavior.

**Architecture:** Introduce a small helper-level yaw alignment policy, then opt in only the jump-entry states: sprint-jump approach, ascend pre-jump alignment, grounded prepare-jump freeze, and grounded walk segments that are explicitly preparing a jump. Keep air control, grounded braking, descend, climb, and ordinary walk/final-stop behavior on smooth yaw, and prove the scope boundary with focused unit tests plus sequential live harness runs.

**Tech Stack:** C# 14, .NET 10, xUnit, MCC local harness scripts (`tools/mcc-env.sh`, `mcc-preflight`, `tools/test-pathing-jump-combos.sh`, `tools/test-pathing-long-routes.sh`)

---

## File Map

- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
  - Add a small yaw-alignment helper and heading-facing overloads so templates can request `Smooth` or `Snap` without open-coding raw yaw assignment.
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  - Snap yaw only during `Phase.Approach`; keep air and landing phases on smooth yaw.
- Modify: `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
  - Snap yaw only while aligning for jump commitment; preserve the existing grounded prepare-jump handoff carveout.
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
  - Snap exit heading in the frozen `PrepareJump` turn branch only.
- Modify: `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
  - Use snap yaw only for grounded `PrepareJump` segments with `ExitHints.RequireJumpReady == true`.
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  - Add a focused regression that proves sprint-jump approach snaps immediately from opposite yaw.
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
  - Add focused regressions for ascend pre-jump snap, walk run-up snap, grounded freeze snap, and ordinary final-stop smoothness.

### Task 1: Add Failing Sprint-Jump Snap Test

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`

- [ ] **Step 1: Write the failing test**

Add this test near the existing opposite-yaw sprint-jump regressions:

```csharp
[Fact]
public void SprintJumpTemplate_Approach_SnapsYawImmediatelyFromOppositeYaw()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
    FlatWorldTestBuilder.ClearBox(world, 0, 79, 0, 4, 82, 1);
    FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
    FlatWorldTestBuilder.SetSolid(world, 2, 79, 0);

    var segment = new PathSegment
    {
        Start = new Location(0.5, 80, 0.5),
        End = new Location(2.5, 80, 0.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new SprintJumpTemplate(segment, null);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
    var input = new MovementInput();

    TemplateState state = template.Tick(segment.Start, physics, input, world);

    Assert.Equal(TemplateState.InProgress, state);
    Assert.InRange(physics.Yaw, 269.9f, 270.1f);
    Assert.True(input.Forward);
    Assert.True(input.Sprint);
    Assert.True(input.Jump);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~SprintJumpTemplate_Approach_SnapsYawImmediatelyFromOppositeYaw" -v minimal
```

Expected:
- `FAIL`
- The failure should show `physics.Yaw` still near `125` and movement input still blocked by the turn-in-place gate.

- [ ] **Step 3: Write minimal implementation**

In `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`, add the alignment helper and overloads:

```csharp
internal enum YawAlignmentMode
{
    Smooth,
    Snap
}

internal static float AlignYaw(float current, float target, YawAlignmentMode mode, float maxStep = MaxYawStepPerTick)
{
    target = NormalizeYaw(target);
    return mode == YawAlignmentMode.Snap
        ? target
        : SmoothYaw(current, target, maxStep);
}

internal static void FaceSegmentHeading(PlayerPhysics physics, PathSegment segment, YawAlignmentMode mode = YawAlignmentMode.Smooth)
{
    float headingYaw = CalculateYaw(segment.HeadingX, segment.HeadingZ);
    physics.Yaw = AlignYaw(physics.Yaw, headingYaw, mode);
}

internal static void FaceExitHeading(PlayerPhysics physics, PathSegment segment, YawAlignmentMode mode = YawAlignmentMode.Smooth)
{
    float headingYaw = GetExitHeadingYaw(segment);
    physics.Yaw = AlignYaw(physics.Yaw, headingYaw, mode);
}

private static float NormalizeYaw(float yaw)
{
    while (yaw < 0f) yaw += 360f;
    while (yaw >= 360f) yaw -= 360f;
    return yaw;
}
```

In `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`, switch only `Phase.Approach` to snap yaw:

```csharp
YawAlignmentMode yawMode = _phase == Phase.Approach
    ? YawAlignmentMode.Snap
    : YawAlignmentMode.Smooth;

physics.Yaw = TemplateHelper.AlignYaw(physics.Yaw, targetYaw, yawMode);
physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);
```

- [ ] **Step 4: Run tests to verify they pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~SprintJumpTemplate_Approach_SnapsYawImmediatelyFromOppositeYaw|FullyQualifiedName~SprintJumpTemplate_TwoBlockGap_FinalStop_CompletesFromOppositeYawWithinTwentyTicks|FullyQualifiedName~SprintJumpTemplate_ThreeBlockGap_FinalStop_Completes" -v minimal
```

Expected:
- `PASS`
- The new test passes.
- The existing opposite-yaw timing regression stays green.
- The 3-block final-stop sprint jump still completes.

- [ ] **Step 5: Commit**

```bash
git add \
  MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs \
  MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs \
  MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs
git commit -m "pathing: snap yaw for sprint jump approach"
```

### Task 2: Add Failing Ascend And Frozen Prepare-Jump Snap Tests

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`

- [ ] **Step 1: Write the failing tests**

Add these tests to `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs` near the existing prepare-jump regressions:

```csharp
[Fact]
public void AscendTemplate_PrepareJump_SnapsYawImmediatelyFromOppositeYaw()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 338, max: 344);
    FlatWorldTestBuilder.ClearBox(world, 340, 80, 338, 344, 84, 342);
    FlatWorldTestBuilder.FillSolid(world, 341, 80, 339, 341, 80, 341);
    FlatWorldTestBuilder.FillSolid(world, 342, 81, 339, 342, 81, 341);

    var segment = new PathSegment
    {
        Start = new Location(340.5, 80, 340.5),
        End = new Location(341.5, 81, 340.5),
        MoveType = MoveType.Ascend,
        ExitTransition = PathTransitionType.PrepareJump,
        ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
        PreserveSprint = true
    };
    var next = new PathSegment
    {
        Start = new Location(341.5, 81, 340.5),
        End = new Location(342.5, 82, 340.5),
        MoveType = MoveType.Ascend,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new AscendTemplate(segment, next);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
    var input = new MovementInput();

    TemplateState state = template.Tick(segment.Start, physics, input, world);

    Assert.Equal(TemplateState.InProgress, state);
    Assert.InRange(physics.Yaw, 269.9f, 270.1f);
    Assert.True(input.Forward);
    Assert.True(input.Sprint);
    Assert.True(input.Jump);
}

[Fact]
public void WalkTemplate_PrepareJump_FreezeForTurn_SnapsExitHeadingImmediately()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor();
    var current = new PathSegment
    {
        Start = new Location(0.5, 80, 0.5),
        End = new Location(1.5, 80, 0.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.PrepareJump,
        ExitHints = new PathTransitionHints(0, 1, 0.10, double.PositiveInfinity, false, true, true, false, 10),
        PreserveSprint = true
    };
    var next = new PathSegment
    {
        Start = new Location(1.5, 80, 0.5),
        End = new Location(1.5, 80, 1.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new WalkTemplate(current, next);
    var physics = new PlayerPhysics
    {
        Position = new Vec3d(1.5, 80.0, 0.5),
        DeltaMovement = Vec3d.Zero,
        OnGround = true,
        MovementSpeed = 0.1f,
        Yaw = 180f,
        Pitch = 0f
    };
    var input = new MovementInput();

    TemplateState state = template.Tick(new Location(1.5, 80, 0.5), physics, input, world);

    Assert.Equal(TemplateState.InProgress, state);
    Assert.InRange(physics.Yaw, -0.1f, 0.1f);
    Assert.False(input.Forward);
    Assert.False(input.Sprint);
    Assert.False(input.Back);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~AscendTemplate_PrepareJump_SnapsYawImmediatelyFromOppositeYaw|FullyQualifiedName~WalkTemplate_PrepareJump_FreezeForTurn_SnapsExitHeadingImmediately" -v minimal
```

Expected:
- `FAIL`
- The ascend test should show yaw still part-way through the turn.
- The frozen prepare-jump test should show yaw still around `145` instead of `0`.

- [ ] **Step 3: Write minimal implementation**

In `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`, snap yaw only before jump commitment and keep the handoff carveout:

```csharp
bool snapYawForJumpCommit = !_initiatedJump && !groundedPrepareJumpHandoff;
physics.Yaw = TemplateHelper.AlignYaw(
    physics.Yaw,
    targetYaw,
    snapYawForJumpCommit ? YawAlignmentMode.Snap : YawAlignmentMode.Smooth);
physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);
```

In `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`, snap the frozen exit-heading turn:

```csharp
if (segment.ExitTransition == PathTransitionType.PrepareJump
    && segment.ExitHints.RequireJumpReady
    && physics.OnGround
    && TemplateFootingHelper.IsCenterInsideTargetBlock(pos, segment.End)
    && IsReadyToFreezeForTurn(segment, pos)
    && TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment) > 8.0)
{
    input.Forward = false;
    input.Sprint = false;
    input.Back = false;
    TemplateHelper.FaceExitHeading(physics, segment, YawAlignmentMode.Snap);
    return;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~AscendTemplate_PrepareJump_SnapsYawImmediatelyFromOppositeYaw|FullyQualifiedName~WalkTemplate_PrepareJump_FreezeForTurn_SnapsExitHeadingImmediately|FullyQualifiedName~AscendTemplate_PrepareJump_CompletesFromOppositeYawWithinTwentyTicks|FullyQualifiedName~WalkTemplate_TurnIntoParkour_CompletesOnlyWhenTurnEntryIsSlowAndJumpReady" -v minimal
```

Expected:
- `PASS`
- The new snap regressions pass.
- Existing opposite-yaw ascend timing stays green.
- The turn-into-parkour convergence regression still passes.

- [ ] **Step 5: Commit**

```bash
git add \
  MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs \
  MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs \
  MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs
git commit -m "pathing: snap yaw for jump-ready grounded handoffs"
```

### Task 3: Add Failing Walk Jump-Entry Scope Tests

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`

- [ ] **Step 1: Write the failing tests**

Add these tests to `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs` near the existing walk prepare-jump coverage:

```csharp
[Fact]
public void WalkTemplate_PrepareJump_SnapsYawImmediatelyDuringRunUp()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor();
    var current = new PathSegment
    {
        Start = new Location(0.5, 80, 0.5),
        End = new Location(1.5, 80, 0.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.PrepareJump,
        ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
        PreserveSprint = true
    };
    var next = new PathSegment
    {
        Start = new Location(1.5, 80, 0.5),
        End = new Location(3.5, 80, 0.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new WalkTemplate(current, next);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 90f);
    var input = new MovementInput();

    TemplateState state = template.Tick(current.Start, physics, input, world);

    Assert.Equal(TemplateState.InProgress, state);
    Assert.InRange(physics.Yaw, 269.9f, 270.1f);
    Assert.True(input.Forward);
    Assert.True(input.Sprint);
}

[Fact]
public void WalkTemplate_FinalStop_RetainsSmoothYawOutsideJumpEntry()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor();
    var segment = new PathSegment
    {
        Start = new Location(0.5, 80, 0.5),
        End = new Location(1.5, 80, 0.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new WalkTemplate(segment, null);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
    var input = new MovementInput();

    TemplateState state = template.Tick(segment.Start, physics, input, world);

    Assert.Equal(TemplateState.InProgress, state);
    Assert.InRange(physics.Yaw, 124.9f, 125.1f);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~WalkTemplate_PrepareJump_SnapsYawImmediatelyDuringRunUp|FullyQualifiedName~WalkTemplate_FinalStop_RetainsSmoothYawOutsideJumpEntry" -v minimal
```

Expected:
- `FAIL`
- The prepare-jump test should show smooth partial rotation instead of an immediate snap.
- The final-stop control test should already pass and act as the scope guard for the next step.

- [ ] **Step 3: Write minimal implementation**

In `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`, gate snap yaw to grounded jump-entry segments only:

```csharp
bool snapYawForJumpEntry = physics.OnGround
    && _segment.ExitTransition == PathTransitionType.PrepareJump
    && _segment.ExitHints.RequireJumpReady;

float targetYaw = TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment)
    ? TemplateHelper.GetExitHeadingYaw(_segment)
    : TemplateHelper.CalculateYaw(dx, dz);

physics.Yaw = TemplateHelper.AlignYaw(
    physics.Yaw,
    targetYaw,
    snapYawForJumpEntry ? YawAlignmentMode.Snap : YawAlignmentMode.Smooth);
physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);
```

- [ ] **Step 4: Run tests to verify they pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~WalkTemplate_PrepareJump_SnapsYawImmediatelyDuringRunUp|FullyQualifiedName~WalkTemplate_FinalStop_RetainsSmoothYawOutsideJumpEntry|FullyQualifiedName~WalkTemplate_PrepareJump_CompletesWithoutSettlingOnRunUpBlock|FullyQualifiedName~WalkTemplate_DiagonalPrepareJumpIntoAscend_CompletesFromTargetBlockEntry" -v minimal
```

Expected:
- `PASS`
- The new run-up snap regression passes.
- The final-stop scope guard stays green.
- Existing walk prepare-jump convergence regressions remain green.

- [ ] **Step 5: Commit**

```bash
git add \
  MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs \
  MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs
git commit -m "pathing: snap yaw only for grounded jump-entry walk states"
```

### Task 4: Full Verification And Evidence Capture

**Files:**
- Modify only if timing evidence demands it:
  - `MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`
  - `MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`
- Verify:
  - `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  - `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
  - `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
  - `MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs`
  - `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`

- [ ] **Step 1: Run the focused unit regression set**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~SprintJumpTemplateScenarioTests|FullyQualifiedName~GroundedTemplateConvergenceTests|FullyQualifiedName~LivePathingRegressionTests|FullyQualifiedName~MoveParkourTests.Accepts4x1JumpWithoutRearSupport_WhenTakeoffBlockProvidesRunway|FullyQualifiedName~PathPlanningContractTests.Scenario_PlannerMatchesContract|FullyQualifiedName~PathTimingContractTests.JumpCombo_ExecutionStaysWithinBudget|FullyQualifiedName~PathTimingContractTests.LongRoute_ExecutionStaysWithinBudget" -v minimal
```

Expected:
- `PASS`
- No planner regressions.
- No timing budget failures.

- [ ] **Step 2: If a timing contract fails, refresh it from evidence before rerunning**

Use the bootstrap printer first:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~PathingContractBootstrapTests" -v minimal
```

Only if a contract mismatch is stable and explained by the new snap behavior, update the matching JSON entries with the printed values, then rerun the focused contract tests:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~PathPlanningContractTests.Scenario_PlannerMatchesContract|FullyQualifiedName~PathTimingContractTests.JumpCombo_ExecutionStaysWithinBudget|FullyQualifiedName~PathTimingContractTests.LongRoute_ExecutionStaysWithinBudget" -v minimal
```

Expected:
- Either no JSON changes are needed, or the rerun passes with fresh values backed by bootstrap output.

- [ ] **Step 3: Run jump-combo live harness sequentially**

Run:

```bash
bash -lc 'source tools/mcc-env.sh && mcc-preflight 1.21.11-Vanilla && bash tools/test-pathing-jump-combos.sh 1.21.11-Vanilla'
```

Expected:
- `PASS` summary for all jump-combo scenarios.
- No `Replan #`, `Partial`, `Replan failed`, or `Giving up`.

- [ ] **Step 4: Run long-route live harness sequentially**

Run:

```bash
bash -lc 'source tools/mcc-env.sh && mcc-preflight 1.21.11-Vanilla && bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla'
```

Expected:
- `Pathing long-route suite complete.`
- No `Replan #`, `Partial`, `Replan failed`, or `Giving up`.
- Repeated jump-entry routes remain within current max budgets.

- [ ] **Step 5: Commit only additional contract refreshes from Task 4**

If Task 4 needed no JSON or script edits, do not create another commit. Record that verification completed with no additional file changes.

If timing contracts changed in Task 4, commit only those refreshes:

```bash
git add MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json
git commit -m "test: refresh jump-entry snap yaw timing budgets"
```
