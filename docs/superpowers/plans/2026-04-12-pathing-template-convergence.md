# Pathing Template Convergence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every path segment MCC agrees to execute stop safely inside the target block support, reject parkour moves that are not yet reliable, and prove traverse, ascend, descend, climb, fall, and sprint-jump behavior on local 1.21.11.

**Architecture:** Keep A* and the existing move catalog mostly intact, but tighten reliability at two boundaries. On the planning side, adopt Baritone-style conservative parkour admissibility so MCC stops accepting jumps it cannot execute consistently. On the execution side, replace center-hunting with support-footprint completion and add a shared grounded-segment controller so walk, ascend, descend, and sprint-jump all use the same transition rules.

**Tech Stack:** C# 14 / .NET 10, MCC `PlayerPhysics`, xUnit deterministic regression tests, local bash harnesses under `tools/`, local offline Minecraft 1.21.11 server via `tools/mcc-env.sh`.

---

## Execution Context

This plan assumes implementation happens in a dedicated worktree even though the current investigation ran in the main workspace. Do not tune flat-stop precision toward exact block center. The success bar is simpler: the player may finish anywhere inside the target block support footprint, but must not drift past the edge once the segment reports success.

## Scope

In scope:

- tighten parkour admissibility until accepted jumps are reliable
- converge grounded template completion rules across walk, ascend, descend, and sprint-jump landing
- preserve working climb and fall behavior with regression coverage
- add deterministic simulation tests and real-server regression scripts

Out of scope for this pass:

- expanding the parkour move catalog beyond moves we can prove reliable
- changing A* heuristics or node expansion rules unrelated to movement correctness
- making `Shift` a full SafeWalk feature for all contexts

## File Structure

### New files

- `MinecraftClient/Pathing/Execution/Templates/TemplateFootingHelper.cs`
  Shared support-footprint math. Answers "is the player's 0.6-wide footprint still fully inside the target block?" and "would current velocity carry it outside next tick?"
- `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
  Shared grounded transition logic for walk, ascend, descend, and sprint-jump landing.
- `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
  Conservative parkour admissibility helper: run-up, shoulder clearance, overshoot safety, and landing validation.
- `MinecraftClient.Tests/Pathing/Execution/TemplateSimulationRunner.cs`
  Deterministic loop that drives `IActionTemplate`, `MovementInput`, and `PlayerPhysics` against a test world.
- `MinecraftClient.Tests/Pathing/Execution/TemplateFootingTests.cs`
  Unit tests for support-footprint completion rules.
- `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
  Simulation tests for walk, ascend, and descend transition behavior.
- `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  Simulation tests for parkour landing, turn preparation, and accepted side-wall jumps.
- `MinecraftClient.Tests/Pathing/Execution/ClimbFallTemplateTests.cs`
  Simulation smoke tests for climb and fall so convergence work does not regress them.
- `MinecraftClient.Tests/Pathing/Moves/MoveParkourTests.cs`
  Planning-time admissibility tests for `MoveParkour`.
- `tools/test-pathing-template-regressions.sh`
  Real-server regression harness for local 1.21.11.

### Modified files

- `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
  Route support-footprint checks through the new helper and expose shared heading/progress helpers.
- `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
  Stop using settle-at-center rules for `PrepareJump`, `Turn`, and `FinalStop`.
- `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
  Use shared grounded completion after landing and treat `PrepareJump` as a handoff, not a settle.
- `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
  Use shared landing recovery and block-support completion instead of center-hunting.
- `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  Split takeoff into explicit phases, release input earlier in air when needed, and finish on target support instead of target center.
- `MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs`
  Replace ad hoc run-up checks with shared conservative feasibility logic.
- `MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs`
  Add helpers to place blocks, carve air, and build side-wall / stair / ladder / gap scenarios.
- `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
  Add cases that match new landing and release thresholds where needed.
- `docs/guide/pathfinding-research.md`
  Document the reliability-first rule: accepted moves must be executable, support-footprint completion is sufficient, and unsupported parkour shapes are rejected.

---

### Task 1: Add Support-Footprint Completion Rules

**Files:**
- Create: `MinecraftClient/Pathing/Execution/Templates/TemplateFootingHelper.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/TemplateFootingTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`

- [ ] **Step 1: Write the failing support-footprint tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/TemplateFootingTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TemplateFootingTests
{
    [Fact]
    public void IsFootprintInsideTargetBlock_ReturnsTrue_WhenPlayerIsNearEdgeButStillInside()
    {
        bool inside = TemplateFootingHelper.IsFootprintInsideTargetBlock(
            new Location(10.69, 80.0, 4.50),
            new Location(10.50, 80.0, 4.50));

        Assert.True(inside);
    }

    [Fact]
    public void IsFootprintInsideTargetBlock_ReturnsFalse_WhenPlayerCrossesBlockEdge()
    {
        bool inside = TemplateFootingHelper.IsFootprintInsideTargetBlock(
            new Location(10.81, 80.0, 4.50),
            new Location(10.50, 80.0, 4.50));

        Assert.False(inside);
    }

    [Fact]
    public void WillLeaveTargetBlockNextTick_ReturnsTrue_WhenVelocityWouldCarryPastEdge()
    {
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(10.67, 80.0, 4.50),
            DeltaMovement = new Vec3d(0.060, 0.0, 0.0),
            OnGround = true
        };

        bool exitsNextTick = TemplateFootingHelper.WillLeaveTargetBlockNextTick(
            new Location(10.67, 80.0, 4.50),
            physics,
            new Location(10.50, 80.0, 4.50));

        Assert.True(exitsNextTick);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter TemplateFootingTests -v minimal
```

Expected: FAIL with compile errors because `TemplateFootingHelper` and the new helper methods do not exist yet.

- [ ] **Step 3: Implement the support-footprint helper and route `TemplateHelper` through it**

```csharp
// MinecraftClient/Pathing/Execution/Templates/TemplateFootingHelper.cs
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates;

internal static class TemplateFootingHelper
{
    private const double HalfWidth = PhysicsConsts.PlayerWidth / 2.0;

    internal static bool IsFootprintInsideTargetBlock(Location pos, Location target, double epsilon = 1.0E-4)
    {
        double minX = pos.X - HalfWidth;
        double maxX = pos.X + HalfWidth;
        double minZ = pos.Z - HalfWidth;
        double maxZ = pos.Z + HalfWidth;

        double blockMinX = Math.Floor(target.X);
        double blockMaxX = blockMinX + 1.0;
        double blockMinZ = Math.Floor(target.Z);
        double blockMaxZ = blockMinZ + 1.0;

        return minX >= blockMinX - epsilon
            && maxX <= blockMaxX + epsilon
            && minZ >= blockMinZ - epsilon
            && maxZ <= blockMaxZ + epsilon;
    }

    internal static bool WillLeaveTargetBlockNextTick(Location pos, PlayerPhysics physics, Location target, double epsilon = 1.0E-4)
    {
        Location next = new(
            pos.X + physics.DeltaMovement.X,
            pos.Y,
            pos.Z + physics.DeltaMovement.Z);
        return !IsFootprintInsideTargetBlock(next, target, epsilon);
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs
internal static bool IsSettledOnTargetBlock(Location pos, Location target, PlayerPhysics physics,
    double speedThresholdSq = 0.0016)
{
    double horizontalSpeedSq = physics.DeltaMovement.X * physics.DeltaMovement.X
        + physics.DeltaMovement.Z * physics.DeltaMovement.Z;

    if (!TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, target))
        return false;

    if (TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, target))
        return false;

    return horizontalSpeedSq <= speedThresholdSq;
}
```

- [ ] **Step 4: Re-run the support-footprint tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter TemplateFootingTests -v minimal
```

Expected: PASS with `3 Passed`.

- [ ] **Step 5: Commit the support-footprint groundwork**

```bash
git add MinecraftClient/Pathing/Execution/Templates/TemplateFootingHelper.cs \
        MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs \
        MinecraftClient.Tests/Pathing/Execution/TemplateFootingTests.cs
git commit -m "feat: add support-aware template completion checks"
```

---

### Task 2: Tighten Parkour Admissibility to the Reliable Subset

**Files:**
- Create: `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
- Create: `MinecraftClient.Tests/Pathing/Moves/MoveParkourTests.cs`
- Modify: `MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs`

- [ ] **Step 1: Write the failing `MoveParkour` admissibility tests**

```csharp
// MinecraftClient.Tests/Pathing/Moves/MoveParkourTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves.Impl;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

public sealed class MoveParkourTests
{
    [Fact]
    public void Calculate_RejectsThreeByOneSideWall_WhenRunUpIsMissing()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 2);
        FlatWorldTestBuilder.SetSolid(world, 5, 79, 3);
        FlatWorldTestBuilder.FillSolid(world, 4, 79, 2, 4, 81, 2);

        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = new MoveParkour(3, 1);
        MoveResult result = default;

        move.Calculate(ctx, 2, 80, 2, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void Calculate_AcceptsTwoByOneSideWall_WhenTakeoffAndLandingAreClear()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 2);
        FlatWorldTestBuilder.SetSolid(world, 4, 79, 3);
        FlatWorldTestBuilder.FillSolid(world, 4, 79, 2, 4, 81, 2);

        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = new MoveParkour(2, 1);
        MoveResult result = default;

        move.Calculate(ctx, 2, 80, 2, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(4, result.DestX);
        Assert.Equal(80, result.DestY);
        Assert.Equal(3, result.DestZ);
    }

    [Fact]
    public void Calculate_RejectsDiagonalJump_WhenTakeoffShoulderIsBlocked()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 2);
        FlatWorldTestBuilder.SetSolid(world, 4, 79, 4);
        FlatWorldTestBuilder.SetSolid(world, 3, 80, 2);

        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = new MoveParkour(2, 2);
        MoveResult result = default;

        move.Calculate(ctx, 2, 80, 2, ref result);

        Assert.True(result.IsImpossible);
    }
}
```

- [ ] **Step 2: Run the parkour admissibility tests and watch them fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter MoveParkourTests -v minimal
```

Expected: FAIL because current `MoveParkour` only checks one behind-block for run-up and does not centralize side-clearance logic.

- [ ] **Step 3: Extract conservative feasibility checks and wire `MoveParkour` through them**

```csharp
// MinecraftClient/Pathing/Moves/ParkourFeasibility.cs
using System;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves;

internal static class ParkourFeasibility
{
    internal static int RequiredRunUpBlocks(int xOffset, int zOffset, int yDelta)
    {
        double horizDist = Math.Sqrt((double)(xOffset * xOffset + zOffset * zOffset));
        if (yDelta > 0 || horizDist >= 4.0)
            return 2;
        if (horizDist >= 3.0)
            return 1;
        return 0;
    }

    internal static bool HasRunUp(CalculationContext ctx, int x, int y, int z, int xOffset, int zOffset, int yDelta)
    {
        int stepX = Math.Sign(xOffset);
        int stepZ = Math.Sign(zOffset);
        int required = RequiredRunUpBlocks(xOffset, zOffset, yDelta);

        for (int i = 1; i <= required; i++)
        {
            int rx = x - stepX * i;
            int rz = z - stepZ * i;
            if (!ctx.CanWalkOn(rx, y - 1, rz)
                || !ctx.CanWalkThrough(rx, y, rz)
                || !ctx.CanWalkThrough(rx, y + 1, rz))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasDiagonalTakeoffClearance(CalculationContext ctx, int x, int y, int z, int stepX, int stepZ)
    {
        return ctx.CanWalkThrough(x + stepX, y, z)
            && ctx.CanWalkThrough(x + stepX, y + 1, z)
            && ctx.CanWalkThrough(x, y, z + stepZ)
            && ctx.CanWalkThrough(x, y + 1, z + stepZ);
    }

    internal static bool HasOvershootClearance(CalculationContext ctx, int x, int y, int z)
    {
        return ctx.CanWalkThrough(x, y, z) && ctx.CanWalkThrough(x, y + 1, z);
    }
}
```

```csharp
// MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs
if (!ParkourFeasibility.HasRunUp(ctx, x, y, z, XOffset, ZOffset, _yDelta))
{
    result.SetImpossible();
    return;
}

if (xAbs > 0 && zAbs > 0 && !ParkourFeasibility.HasDiagonalTakeoffClearance(ctx, x, y, z, xSign, zSign))
{
    result.SetImpossible();
    return;
}

int overX = destX + xSign;
int overZ = destZ + zSign;
if (!ParkourFeasibility.HasOvershootClearance(ctx, overX, destY, overZ))
{
    result.SetImpossible();
    return;
}
```

- [ ] **Step 4: Re-run the parkour admissibility tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter MoveParkourTests -v minimal
```

Expected: PASS with `3 Passed`.

- [ ] **Step 5: Commit the planner hardening**

```bash
git add MinecraftClient/Pathing/Moves/ParkourFeasibility.cs \
        MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs \
        MinecraftClient.Tests/Pathing/Moves/MoveParkourTests.cs \
        MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs
git commit -m "feat: tighten parkour move admissibility"
```

---

### Task 3: Converge Walk, Ascend, and Descend on Shared Grounded Transition Rules

**Files:**
- Create: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/TemplateSimulationRunner.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs`

- [ ] **Step 1: Write the failing simulation tests for grounded segment handoff**

```csharp
// MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class GroundedTemplateConvergenceTests
{
    [Fact]
    public void WalkTemplate_FinalStop_Completes_WhenFootprintStaysInsideTargetBlock()
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
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 80, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void WalkTemplate_PrepareJump_CompletesWithoutSettlingOnRunUpBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
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
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 40, out _);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(physics.DeltaMovement.X > 0.05);
    }

    [Fact]
    public void DescendTemplate_LandingRecovery_CompletesOnLandingBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        FlatWorldTestBuilder.ClearBox(world, 1, 80, 0, 1, 80, 0);
        FlatWorldTestBuilder.SetSolid(world, 1, 78, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 79, 0.5),
            MoveType = MoveType.Descend,
            ExitTransition = PathTransitionType.LandingRecovery
        };

        var template = new DescendTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 120, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }
}
```

- [ ] **Step 2: Run the grounded simulation tests and watch them fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter GroundedTemplateConvergenceTests -v minimal
```

Expected: FAIL because there is no simulation runner yet and current templates still use settle-at-center rules for `PrepareJump` and landing recovery.

- [ ] **Step 3: Add a shared grounded controller and migrate walk / ascend / descend to it**

```csharp
// MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates;

internal static class GroundedSegmentController
{
    internal static void Apply(PathSegment segment, PathSegment? nextSegment, Location pos, PlayerPhysics physics, MovementInput input, World world)
    {
        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(segment, nextSegment, pos, physics, world);
        TemplateHelper.ApplyDecision(input, decision);
        if (decision.HoldBack)
            TemplateHelper.FaceSegmentHeading(physics, segment);
    }

    internal static bool ShouldComplete(PathSegment segment, Location pos, PlayerPhysics physics)
    {
        return segment.ExitTransition switch
        {
            PathTransitionType.ContinueStraight => TemplateHelper.IsNear(pos, segment.End, horizThresholdSq: 0.09),
            PathTransitionType.PrepareJump => TemplateHelper.HasReachedSegmentEndPlane(pos, segment),
            _ => TemplateHelper.IsSettledOnTargetBlock(pos, segment.End, physics)
        };
    }
}
```

```csharp
// MinecraftClient.Tests/Pathing/Execution/TemplateSimulationRunner.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class TemplateSimulationRunner
{
    internal static PlayerPhysics CreateGroundedPhysics(Location start, float yaw)
    {
        return new PlayerPhysics
        {
            Position = new Vec3d(start.X, start.Y, start.Z),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = yaw
        };
    }

    internal static TemplateState Run(IActionTemplate template, PlayerPhysics physics, World world, int maxTicks, out Location finalPos)
    {
        var input = new MovementInput();
        TemplateState state = TemplateState.InProgress;

        for (int tick = 0; tick < maxTicks && state == TemplateState.InProgress; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            physics.ApplyInput(input);
            physics.Tick(world);
        }

        finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        return state;
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs
GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);

if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
    return TemplateState.Complete;
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs
internal static bool HasReachedSegmentEndPlane(Location pos, PathSegment segment)
{
    double dx = pos.X - segment.End.X;
    double dz = pos.Z - segment.End.Z;
    return dx * segment.HeadingX + dz * segment.HeadingZ >= -0.05;
}
```

- [ ] **Step 4: Re-run the grounded simulation tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter GroundedTemplateConvergenceTests -v minimal
```

Expected: PASS with `3 Passed`.

- [ ] **Step 5: Commit the grounded-template convergence work**

```bash
git add MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs \
        MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs \
        MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs \
        MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs \
        MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs \
        MinecraftClient.Tests/Pathing/Execution/TemplateSimulationRunner.cs \
        MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs \
        MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs
git commit -m "feat: converge grounded path execution templates"
```

---

### Task 4: Rework Sprint Jump Execution Around Committed Takeoff and Support-Aware Landing

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`

- [ ] **Step 1: Write the failing sprint-jump scenario tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class SprintJumpTemplateScenarioTests
{
    [Fact]
    public void SprintJumpTemplate_ParkourIntoTurn_LandsInsideTargetSupport()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.ClearBox(world, 1, 79, 0, 2, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 3, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 3, 79, 1);

        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(3.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery
        };
        var next = new PathSegment
        {
            Start = new Location(3.5, 80, 0.5),
            End = new Location(3.5, 80, 1.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new SprintJumpTemplate(current, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 80, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, current.End));
    }

    [Fact]
    public void SprintJumpTemplate_TwoByOneSideWall_Completes()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.ClearBox(world, 1, 79, 0, 1, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 1);
        FlatWorldTestBuilder.FillSolid(world, 2, 79, 0, 2, 81, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(2.5, 80, 1.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new SprintJumpTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 315f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 80, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }
}
```

- [ ] **Step 2: Run the sprint-jump scenario tests and confirm they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter SprintJumpTemplateScenarioTests -v minimal
```

Expected: FAIL because the current template still overshoots landing blocks and treats landing recovery as a late braking problem instead of a committed takeoff plus controlled handoff.

- [ ] **Step 3: Introduce explicit jump phases and support-aware landing completion**

```csharp
// MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs
private enum Phase
{
    Approach,
    CommitJump,
    Airborne,
    LandingRecovery
}

case Phase.Approach:
    input.Forward = true;
    input.Sprint = true;
    if (physics.OnGround && YawDifference(physics.Yaw, targetYaw) < YawToleranceDeg && ReadyForTakeoff(pos))
    {
        _phase = Phase.CommitJump;
    }
    break;

case Phase.CommitJump:
    input.Forward = true;
    input.Sprint = true;
    input.Jump = physics.OnGround;
    if (!physics.OnGround)
    {
        _leftGround = true;
        _phase = Phase.Airborne;
    }
    break;

case Phase.Airborne:
    bool releaseNow = TransitionBrakingPlanner.ShouldReleaseForwardInAir(_segment, _nextSegment, pos, physics)
        || TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, ExpectedEnd);
    input.Forward = !releaseNow;
    input.Sprint = !releaseNow;
    if (_leftGround && physics.OnGround)
        _phase = Phase.LandingRecovery;
    break;

case Phase.LandingRecovery:
    GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
    if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
        return TemplateState.Complete;
    break;
```

- [ ] **Step 4: Re-run sprint-jump tests plus braking planner tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "SprintJumpTemplateScenarioTests|TransitionBrakingPlannerTests" -v minimal
```

Expected: PASS with all sprint-jump and braking tests green.

- [ ] **Step 5: Commit the sprint-jump convergence**

```bash
git add MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs \
        MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs \
        MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs
git commit -m "feat: stabilize sprint jump execution transitions"
```

---

### Task 5: Add Regression Coverage for Climb / Fall and Real-Server Template Matrix

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/ClimbFallTemplateTests.cs`
- Create: `tools/test-pathing-template-regressions.sh`
- Modify: `docs/guide/pathfinding-research.md`

- [ ] **Step 1: Write the remaining simulation smoke tests and the local server harness**

```csharp
// MinecraftClient.Tests/Pathing/Execution/ClimbFallTemplateTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class ClimbFallTemplateTests
{
    [Fact]
    public void ClimbTemplate_UpwardMove_StillCompletes()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 8);
        FlatWorldTestBuilder.FillSolid(world, 0, 79, 0, 0, 82, 0);
        FlatWorldTestBuilder.SetClimbable(world, 0, 80, 0);
        FlatWorldTestBuilder.SetClimbable(world, 0, 81, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(0.5, 81, 0.5),
            MoveType = MoveType.Climb
        };

        var template = new ClimbTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 0f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 120, out _);

        Assert.Equal(TemplateState.Complete, state);
    }
}
```

```bash
#!/usr/bin/env bash
# tools/test-pathing-template-regressions.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
INPUT_FILE="$REPO_ROOT/mcc_input.txt"
LOG_DIR="${TMPDIR:-/tmp}/mcc-debug"
LOG_FILE="$LOG_DIR/mcc-template-regressions.log"
CFG="$LOG_DIR/MinecraftClient.template-regressions.ini"

send_mcc() {
    printf '%s\n' "$1" >> "$INPUT_FILE"
}

wait_for_log() {
    local pattern="$1"
    local timeout="${2:-20}"
    for _ in $(seq 1 "$timeout"); do
        if grep -Fq "$pattern" "$LOG_FILE"; then
            return 0
        fi
        sleep 1
    done
    return 1
}

run_case() {
    local name="$1"
    local command="$2"
    local expected="$3"
    echo "== $name =="
    : > "$LOG_FILE"
    send_mcc "$command"
    wait_for_log "$expected" 20
    grep -E "\\[PathMgr\\]|\\[PathExec\\]|\\[A\\*\\]" "$LOG_FILE" | tail -20
}

mcc-preflight "$VERSION" >/dev/null
mc-start "$VERSION" >/dev/null
mc-wait-ready "$VERSION" 60 >/dev/null
echo "Prepare temp config at $CFG before first run"
echo "Use this harness to validate:"
echo "1. flat final stop"
echo "2. parkour into L turn"
echo "3. 2x1 side wall parkour"
echo "4. 3x1 no-run-up rejection"
echo "5. ascend + descend + climb smoke"
```

- [ ] **Step 2: Run the full unit suite plus the real-server matrix**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj -v minimal
dotnet build MinecraftClient.sln -c Release
bash tools/test-pathing-template-regressions.sh 1.21.11-Vanilla
```

Expected:

- unit tests: PASS
- build: PASS
- real server: positive evidence that flat final stop, parkour into turn, accepted 2x1 side-wall, and mixed non-parkour segments complete
- real server: positive evidence that rejected parkour shapes are rejected up front instead of failing mid-execution

- [ ] **Step 3: Document the new reliability rule**

```md
<!-- docs/guide/pathfinding-research.md -->
## Reliability-First Execution Rule

MCC no longer treats block-center precision as the stop criterion for path execution.
A segment is considered safely complete when the player's full support footprint remains
inside the destination block and current velocity would not carry it beyond the edge on
the next tick.

For parkour, planning is intentionally conservative:

- if a jump shape is not covered by deterministic simulation plus local 1.21.11 regression
  evidence, reject it during planning
- if a jump is accepted, execution must land on supported destination footprint without
  relying on replan to rescue overshoot
```

- [ ] **Step 4: Re-run the docs-adjacent validation commands**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj -v minimal
dotnet build MinecraftClient.sln -c Release
```

Expected: PASS. No code or docs edits in this task should break the test suite or build.

- [ ] **Step 5: Commit the regression matrix and documentation**

```bash
git add MinecraftClient.Tests/Pathing/Execution/ClimbFallTemplateTests.cs \
        tools/test-pathing-template-regressions.sh \
        docs/guide/pathfinding-research.md
git commit -m "test: add pathing template regression matrix"
```

---

## Verification Checklist

Before calling this project done, the implementing agent must have fresh evidence for all of the following:

- `MoveParkourTests` passes
- `TemplateFootingTests` passes
- `GroundedTemplateConvergenceTests` passes
- `SprintJumpTemplateScenarioTests` passes
- `ClimbFallTemplateTests` passes
- full `MinecraftClient.Tests` project passes
- `dotnet build MinecraftClient.sln -c Release` passes
- `tools/test-pathing-template-regressions.sh 1.21.11-Vanilla` shows positive runtime evidence for:
  - flat final stop stays within target block support
  - parkour into L-turn completes without rescue replan
  - accepted 2x1 side-wall jump completes
  - rejected 3x1 no-run-up shape is refused by planning
  - mixed ascend / descend / climb route still completes

## Coverage Check

This plan covers every user-facing requirement from the current thread:

- Flat stopping is no longer centered around exact block center.
- Success is defined as not leaving the block support footprint.
- Complex parkour issues discovered in local 1.21.11 testing are addressed.
- All current template families are included, either as changed code or protected by regression tests.
- Real local server validation remains part of the definition of done.
