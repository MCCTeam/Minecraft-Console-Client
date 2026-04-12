# Pathing Live Regression Convergence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every remaining movement template that currently passes deterministic simulation but fails on the real 1.21.11 server converge to the same reliable outcome in both environments.

**Architecture:** Keep the existing move catalog and support-footprint completion rules, but close the sim/live gaps at the transition layer. The main tactic is to encode each live-only failure as a deterministic regression first, then fix the responsible handoff logic so braking, heading lock, and completion semantics stay consistent across `SprintJumpTemplate`, grounded recovery, and the local server harness.

**Tech Stack:** C# 14 / .NET 10, MCC `PlayerPhysics`, xUnit, bash harnesses under `tools/`, local offline 1.21.11 server via `tools/mcc-env.sh`.

---

## Execution Context

The user explicitly asked to stay in the current workspace, not a worktree. Do not revert unrelated dirty files. The precision bar is not “exactly at center”; the bar is “footprint fully supported, no unsafe drift past the intended support edge, and no segment failure hidden by replanning”.

## Scope

In scope:

- `LandingRecovery` regressions caused by the braking feature
- short parkour into turn / wall-adjacent follow-up moves that still fail live
- template and planner mismatches where deterministic tests are missing the real-server failure mode
- regression harness updates that fail on any segment failure instead of accepting a later replan

Out of scope for this pass:

- a global SafeWalk / always-sneak system
- new movement types
- large A* or cost-model rewrites unrelated to live regressions

## File Structure

### New files

- `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
  Deterministic reproductions of the currently known live-only failures, seeded from real harness geometry and residual landing states.

### Modified files

- `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
  Teach the planner that `LandingRecovery` may still require a real ground brake before the next heading change.
- `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  Keep landing recovery aligned with the planner and avoid drifting out of the landing support while preparing the next move.
- `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
  Reuse the corrected planner behavior for grounded completion and braking.
- `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  Preserve high-level parkour coverage after the targeted regression tests land.
- `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
  Add planner-level assertions for `LandingRecovery` into turns and other non-straight follow-ups.
- `tools/test-pathing-template-regressions.sh`
  Extend the live harness cases as each new real-only failure is discovered and fixed.

---

### Task 1: Encode The Live `LandingRecovery -> Turn` Failure

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
- Test: `MinecraftClient.Tests/MinecraftClient.Tests.csproj`

- [ ] **Step 1: Write the failing planner and live-geometry regression tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs
[Fact]
public void Plan_BackBrakes_ForLandingRecovery_WhenNextSegmentTurns()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor();
    var physics = CreatePhysics(0.118, 0.000, onGround: true);
    var current = new PathSegment
    {
        Start = new Location(120.5, 80, 110.5),
        End = new Location(122.5, 80, 110.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.LandingRecovery
    };
    var next = new PathSegment
    {
        Start = new Location(122.5, 80, 110.5),
        End = new Location(122.5, 80, 111.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.FinalStop
    };

    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(
        current,
        next,
        new Location(122.56, 80.0, 110.68),
        physics,
        world);

    Assert.False(decision.HoldForward);
    Assert.False(decision.HoldSprint);
    Assert.True(decision.HoldBack);
}
```

```csharp
// MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class LivePathingRegressionTests
{
    [Fact]
    public void LandingRecoveryIntoTurn_HoldsInsideLandingBlock_FromLiveLikeState()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 118, max: 126);
        FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
        FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 122, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 122, 79, 111);
        FlatWorldTestBuilder.SetSolid(world, 120, 80, 111);
        FlatWorldTestBuilder.SetSolid(world, 120, 81, 111);

        var current = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(122.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery
        };
        var next = new PathSegment
        {
            Start = new Location(122.5, 80, 110.5),
            End = new Location(122.5, 80, 111.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(122.56, 80.0, 110.68),
            DeltaMovement = new Vec3d(0.118, 0.0, 0.018),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f,
            Pitch = 0f
        };

        var input = new MovementInput();
        GroundedSegmentController.Apply(current, next, new Location(122.56, 80.0, 110.68), physics, input, world);

        Assert.True(input.Back);
        physics.ApplyInput(input);
        physics.Tick(world);

        Location settled = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(settled, current.End));
    }
}
```

- [ ] **Step 2: Run the targeted tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "Plan_BackBrakes_ForLandingRecovery_WhenNextSegmentTurns|LandingRecoveryIntoTurn_HoldsInsideLandingBlock_FromLiveLikeState" -v minimal
```

Expected: FAIL because `LandingRecovery` currently falls through to the generic coast branch and does not hold `Back`.

- [ ] **Step 3: Commit the failing regression capture**

```bash
git add MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs \
        MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
git commit -m "test: capture live landing recovery turn regression"
```

---

### Task 2: Teach `LandingRecovery` To Brake For Non-Straight Follow-Ups

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`

- [ ] **Step 1: Implement the minimal planner change**

```csharp
// MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs
public static TransitionBrakingDecision Plan(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
{
    if (current.ExitTransition is PathTransitionType.ContinueStraight or PathTransitionType.PrepareJump)
        return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);

    double remaining = RemainingDistanceAlongSegment(current, pos);
    double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));
    double coastStopDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: false);
    double hardBrakeDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: true);

    bool landingNeedsTurnBrake = current.ExitTransition == PathTransitionType.LandingRecovery
        && next is not null
        && (current.HeadingX != next.HeadingX || current.HeadingZ != next.HeadingZ);

    if (current.ExitTransition == PathTransitionType.FinalStop)
    {
        if (remaining < 0.0)
            return TransitionBrakingDecision.Brake;

        if (forwardSpeed > GroundSpeedThreshold && remaining <= hardBrakeDistance + FinalBrakeLead)
            return TransitionBrakingDecision.Brake;

        if (forwardSpeed <= GroundSpeedThreshold && remaining > 0.0)
            return TransitionBrakingDecision.CarryMomentum(preserveSprint: false);
    }

    if ((current.ExitTransition == PathTransitionType.Turn || landingNeedsTurnBrake)
        && remaining <= hardBrakeDistance + TurnBrakeLead)
    {
        return TransitionBrakingDecision.Brake;
    }

    if (remaining <= coastStopDistance + FinalStopLead)
        return TransitionBrakingDecision.Coast;

    return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);
}
```

- [ ] **Step 2: Keep grounded braking aligned with the planner**

```csharp
// MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs
internal static void Apply(PathSegment segment, PathSegment? nextSegment, Location pos, PlayerPhysics physics, MovementInput input, World world)
{
    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(segment, nextSegment, pos, physics, world);
    TemplateHelper.ApplyDecision(input, decision);

    if (decision.HoldBack)
        TemplateHelper.FaceSegmentHeading(physics, segment);
}
```

- [ ] **Step 3: Run the targeted tests to verify they pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "Plan_BackBrakes_ForLandingRecovery_WhenNextSegmentTurns|LandingRecoveryIntoTurn_HoldsInsideLandingBlock_FromLiveLikeState" -v minimal
```

Expected: PASS with `2 Passed`.

- [ ] **Step 4: Commit the planner fix**

```bash
git add MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs \
        MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs \
        MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs \
        MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
git commit -m "fix: brake landing recovery before turns"
```

---

### Task 3: Keep `SprintJumpTemplate` Aligned With The Ground Brake

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Test: `MinecraftClient.Tests/MinecraftClient.Tests.csproj`

- [ ] **Step 1: Add a template-level regression for the exact L-turn geometry**

```csharp
// MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs
[Fact]
public void SprintJumpTemplate_TwoBlockGap_LandingRecovery_IntoTurn_CompletesWithoutLeavingLandingBlock()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 118, max: 126);
    FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
    FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
    FlatWorldTestBuilder.SetSolid(world, 122, 79, 110);
    FlatWorldTestBuilder.SetSolid(world, 122, 79, 111);
    FlatWorldTestBuilder.SetSolid(world, 120, 80, 111);
    FlatWorldTestBuilder.SetSolid(world, 120, 81, 111);

    var segment = new PathSegment
    {
        Start = new Location(120.5, 80, 110.5),
        End = new Location(122.5, 80, 110.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.LandingRecovery
    };
    var next = new PathSegment
    {
        Start = new Location(122.5, 80, 110.5),
        End = new Location(122.5, 80, 111.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new SprintJumpTemplate(segment, next);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

    TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

    Assert.Equal(TemplateState.Complete, state);
    Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
}
```

- [ ] **Step 2: Make landing recovery respect the same brake/heading contract as grounded segments**

```csharp
// MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs
case Phase.Landing:
    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
    TemplateHelper.ApplyDecision(input, decision);
    if (decision.HoldBack)
        TemplateHelper.FaceSegmentHeading(physics, _segment);

    if (_segment.ExitTransition == PathTransitionType.ContinueStraight
        && horizDistSq < horizToleranceSq && Math.Abs(dy) < vertTolerance)
        return TemplateState.Complete;

    if (_segment.ExitTransition != PathTransitionType.ContinueStraight
        && physics.OnGround
        && TemplateHelper.IsSettledOnTargetBlock(pos, ExpectedEnd, physics))
    {
        return TemplateState.Complete;
    }
    break;
```

- [ ] **Step 3: Run the parkour template test slice**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "SprintJumpTemplate_TwoBlockGap_LandingRecovery_IntoTurn_CompletesWithoutLeavingLandingBlock|SprintJumpTemplate_TwoBlockGap_LandingRecovery_CompletesInsideLandingBlock|SprintJumpTemplate_TwoBlockGap_FinalStop_Completes|SprintJumpTemplate_ThreeBlockGap_FinalStop_Completes" -v minimal
```

Expected: PASS with `4 Passed`.

- [ ] **Step 4: Commit the template alignment**

```bash
git add MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs \
        MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs
git commit -m "fix: align sprint jump landing recovery with turn braking"
```

---

### Task 4: Sweep Remaining Sim/Live Gaps With The Real Harness

**Files:**
- Modify: `tools/test-pathing-template-regressions.sh`
- Modify: `docs/superpowers/plans/2026-04-12-pathing-live-regression-convergence.md`
- Test: `MinecraftClient.Tests/MinecraftClient.Tests.csproj`

- [ ] **Step 1: Extend the live harness with every newly discovered real-only failure**

```bash
# tools/test-pathing-template-regressions.sh
# Add one function per new repro:
# - run_wall_adjacent_landing_recovery
# - run_around_wall_jump_followup
# - run_short_descend_into_turn
# Each function must:
# 1. build the exact world with mc-rcon
# 2. teleport CursorBot
# 3. send the pathfind command
# 4. fail immediately on any "[PathExec] Segment .* FAILED"
# 5. assert the final location or assert explicit planner rejection
```

- [ ] **Step 2: Run the full deterministic suite**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj -v minimal
```

Expected: PASS with the full suite green.

- [ ] **Step 3: Run the release build**

Run:

```bash
dotnet build MinecraftClient.sln -c Release
```

Expected: `Build succeeded.`

- [ ] **Step 4: Run the real 1.21.11 harness**

Run:

```bash
bash tools/test-pathing-template-regressions.sh 1.21.11
```

Expected:

```text
== Flat final stop ==
== Parkour into L-turn ==
== Rejected 2x1 side-wall jump ==
== Rejected 3x1 no-run-up gap ==
All pathing template regression checks passed for 1.21.11.
```

- [ ] **Step 5: Commit the harness convergence**

```bash
git add tools/test-pathing-template-regressions.sh \
        docs/superpowers/plans/2026-04-12-pathing-live-regression-convergence.md
git commit -m "test: extend live pathing regression coverage"
```
