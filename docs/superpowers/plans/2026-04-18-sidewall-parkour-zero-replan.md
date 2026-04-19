# Sidewall Parkour Zero-Replan Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make all `sidewall` cases in `tools/test-parkour.py --filter sidewall --parallel 6 --version 1.21.11-Vanilla` match theory on `1.21.11-Vanilla`, with every accepted case completing at `replan_count=0` and `turn_stall_count=0`, while preserving the current all-green `linear` matrix.

**Architecture:** Isolate sidewall behavior instead of loosening the generic linear parkour logic. Add an explicit `ParkourProfile.Sidewall` planner-to-executor profile that the planner can admit using dominant-axis runway rules and the executor can follow using a straight runway approach plus controlled in-air bias toward the landing block. Freeze the exact `tools/test-parkour.py` sidewall geometry in .NET regressions first, then implement planner and executor changes behind those tests, and do not call the work done until `sidewall` is `30/30` and `linear` remains `22/22`.

**Tech Stack:** C# 14 / .NET 10, xUnit, MCC pathing core/execution, Python 3 live harness `tools/test-parkour.py`, local `1.21.11-Vanilla` server via `tools/mcc-env.sh`.

---

## Scope And Guardrails

- In scope: `sidewall/flat`, `sidewall/ascend`, and `sidewall/descend` for `wo=0` and `wo=1`, using the exact live geometry from `tools/test-parkour.py`.
- Hard requirement: every accepted sidewall case must finish with `replan_count=0` and `turn_stall_count=0`.
- Hard requirement: `linear` is already fully green in live runs. Do not weaken or rewrite the existing cardinal linear parkour rules just to make sidewall pass.
- Acceptance gate for this plan: targeted green .NET regressions plus `tools/test-parkour.py` sidewall and linear live matrices. Do not use the current full `MinecraftClient.Tests` suite as the gate because the baseline is already `181/198` with 17 unrelated failures.
- Execution note: the fresh baseline evidence came from branch `pathing/jump-entry-direct-yaw` in the main workspace, not an isolated worktree. If the user keeps work in this workspace, do not reset or discard unrelated changes.
- Out of scope for this plan: `neo` and `ceiling` live mismatches. If a helper becomes reusable for those families later, keep it generic, but do not expand verification targets in this plan.

## File Structure

- Create: `MinecraftClient/Pathing/Core/ParkourProfile.cs`
  Responsibility: explicit planner-to-executor profile for `Default` vs `Sidewall` parkour.
- Modify: `MinecraftClient/Pathing/Core/MoveResult.cs`
  Responsibility: carry `ParkourProfile` out of `IMove.Calculate()`.
- Modify: `MinecraftClient/Pathing/Core/PathNode.cs`
  Responsibility: remember which parkour profile produced each node.
- Modify: `MinecraftClient/Pathing/Execution/PathSegment.cs`
  Responsibility: expose per-segment `ParkourProfile`.
- Modify: `MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs`
  Responsibility: thread `ParkourProfile` from planned node to runtime segment.
- Create: `MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs`
  Responsibility: sidewall-specific admissibility with dominant-axis run-up and wall-adjacent arc rules.
- Modify: `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
  Responsibility: shared helpers for dominant-axis runway checks, inside-wall depth validation, and sidewall landing clearance.
- Modify: `MinecraftClient/Pathing/Core/AStarPathFinder.cs`
  Responsibility: register the full sidewall candidate set without disturbing current linear/cardinal move coverage.
- Modify: `MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs`
  Responsibility: explicitly tag generic parkour as `ParkourProfile.Default`; do not change its linear admissibility rules.
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
  Responsibility: compute sidewall approach heading and dominant-axis progress.
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  Responsibility: use the sidewall approach heading on the runway, then rotate toward the landing/exit heading in air without inducing turn stalls or replans.
- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/SidewallParkourScenarioBuilder.cs`
  Responsibility: exact in-memory world builder matching `tools/test-parkour.py::WorldBuilder.build_sidewall_route()`.
- Create: `MinecraftClient.Tests/Pathing/Execution/SidewallParkourScenarioBuilderTests.cs`
  Responsibility: assert the in-memory builder matches live-harness geometry and endpoints.
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs`
  Responsibility: assert `ParkourProfile` survives path-to-segment translation.
- Create: `MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs`
  Responsibility: direct planner admissibility tests for theory-allowed and theory-forbidden sidewall jumps.
- Modify: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
  Responsibility: exact live-coordinate sidewall planner regressions matching the harness.
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  Responsibility: sidewall template convergence and no-spin regressions.
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`
  Responsibility: accepted sidewall chains complete with `0 replan`.
- Use for verification only: `tools/test-parkour.py`
  Responsibility: parallel live matrix verification, not production code changes.

### Task 1: Mirror The Live Sidewall Geometry In Test Fixtures

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/SidewallParkourScenarioBuilder.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/SidewallParkourScenarioBuilderTests.cs`

- [ ] **Step 1: Write the failing builder tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/SidewallParkourScenarioBuilderTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class SidewallParkourScenarioBuilderTests
{
    [Fact]
    public void BuildWorld_FlatGap2Wo0_MatchesLiveRouteGeometry()
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 2, deltaY: 0, wallOffset: 0);

        Assert.Equal(Material.Stone, world.GetBlock(new Location(100, 79, 98)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(100, 79, 99)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(100, 79, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 78, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 79, 102)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(100, 79, 101)).Type);
    }

    [Fact]
    public void BuildWorld_FlatGap3Wo1_ExtendsWallByTwoBlocksAlongRunwaySide()
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 3, deltaY: 0, wallOffset: 1);

        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 78, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 78, 101)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(99, 78, 102)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 79, 103)).Type);
    }

    [Fact]
    public void Create_FlatGap2Wo0_UsesSameStartAndGoalAsLiveHarness()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(
            "sidewall-flat-gap2-wo0",
            gap: 2,
            deltaY: 0,
            wallOffset: 0);

        Assert.Equal(new Location(100.5, 80, 100.5), scenario.Start);
        Assert.Equal(97, scenario.Goal.X);
        Assert.Equal(80, scenario.Goal.Y);
        Assert.Equal(106, scenario.Goal.Z);
        Assert.Equal(0f, scenario.StartYaw);
    }
}
```

- [ ] **Step 2: Run the builder tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~SidewallParkourScenarioBuilderTests -v minimal
```

Expected: FAIL with missing-type errors for `SidewallParkourScenarioBuilder`.

- [ ] **Step 3: Implement the exact sidewall scenario builder**

```csharp
// MinecraftClient.Tests/Pathing/Execution/Scenarios/SidewallParkourScenarioBuilder.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;

namespace MinecraftClient.Tests.Pathing.Execution;

public static class SidewallParkourScenarioBuilder
{
    private const int SegmentCount = 3;
    private const int BaseX = 100;
    private const int BaseY = 80;
    private const int BaseZ = 100;
    private const int FloorY = BaseY - 1;

    public static IEnumerable<object[]> AcceptedCases()
    {
        yield return ["sidewall-flat-gap2-wo0", 2, 0, 0];
        yield return ["sidewall-flat-gap3-wo1", 3, 0, 1];
        yield return ["sidewall-ascend-gap2-dy+1-wo0", 2, 1, 0];
        yield return ["sidewall-ascend-gap3-dy+1-wo1", 3, 1, 1];
        yield return ["sidewall-descend-gap2-dy-1-wo0", 2, -1, 0];
        yield return ["sidewall-descend-gap3-dy-1-wo1", 3, -1, 1];
        yield return ["sidewall-descend-gap2-dy-2-wo0", 2, -2, 0];
        yield return ["sidewall-descend-gap3-dy-2-wo1", 3, -2, 1];
    }

    public static IEnumerable<object[]> RejectedCases()
    {
        yield return ["sidewall-flat-gap5-wo0", 5, 0, 0];
        yield return ["sidewall-flat-gap5-wo1", 5, 0, 1];
        yield return ["sidewall-ascend-gap4-dy+1-wo0", 4, 1, 0];
        yield return ["sidewall-ascend-gap4-dy+1-wo1", 4, 1, 1];
        yield return ["sidewall-descend-gap6-dy-1-wo0", 6, -1, 0];
        yield return ["sidewall-descend-gap6-dy-1-wo1", 6, -1, 1];
        yield return ["sidewall-descend-gap6-dy-2-wo0", 6, -2, 0];
        yield return ["sidewall-descend-gap6-dy-2-wo1", 6, -2, 1];
    }

    internal static PathingExecutionScenario Create(string scenarioId, int gap, int deltaY, int wallOffset, int maxExecutionTicks = 700)
    {
        int endFloorY = FloorY + (deltaY * SegmentCount);
        int endX = BaseX - SegmentCount;
        int endZ = BaseZ + (gap * SegmentCount);

        return new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = () => BuildWorld(gap, deltaY, wallOffset),
            Start = new Location(BaseX + 0.5, BaseY, BaseZ + 0.5),
            Goal = new GoalBlock(endX, endFloorY + 1, endZ),
            StartYaw = 0f,
            MaxExecutionTicks = maxExecutionTicks,
        };
    }

    internal static World BuildWorld(int gap, int deltaY, int wallOffset)
    {
        int maxZ = BaseZ + gap * SegmentCount + 8;
        World world = FlatWorldTestBuilder.CreateStoneFloor(floorY: 0, min: 80, max: maxZ + 8);
        FlatWorldTestBuilder.ClearBox(world, 90, 70, 90, 110, 96, maxZ + 8);

        int curX = BaseX;
        int curY = FloorY;
        int curZ = BaseZ;

        FlatWorldTestBuilder.FillSolid(world, curX, curY, curZ - 2, curX, curY, curZ);

        for (int segment = 0; segment < SegmentCount; segment++)
        {
            int wallX = curX - 1;
            int wallZEnd = curZ + wallOffset;
            int landX = curX - 1;
            int landY = curY + deltaY;
            int landZ = curZ + gap;

            FlatWorldTestBuilder.FillSolid(
                world,
                wallX,
                Math.Min(curY, landY) - 1,
                curZ,
                wallX,
                Math.Max(curY, landY) + 7,
                wallZEnd);
            FlatWorldTestBuilder.SetSolid(world, landX, landY, landZ);

            curX = landX;
            curY = landY;
            curZ = landZ;
        }

        return world;
    }
}
```

- [ ] **Step 4: Run the builder tests to verify they pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~SidewallParkourScenarioBuilderTests -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/Scenarios/SidewallParkourScenarioBuilder.cs \
        MinecraftClient.Tests/Pathing/Execution/SidewallParkourScenarioBuilderTests.cs
git commit -m "test: add sidewall scenario builder fixtures"
```

---

### Task 2: Thread `ParkourProfile` From Planner Nodes To Runtime Segments

**Files:**
- Create: `MinecraftClient/Pathing/Core/ParkourProfile.cs`
- Modify: `MinecraftClient/Pathing/Core/MoveResult.cs`
- Modify: `MinecraftClient/Pathing/Core/PathNode.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegment.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs`
- Modify: `MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs`

- [ ] **Step 1: Write the failing profile-plumbing test**

```csharp
// MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs
[Fact]
public void FromPath_CopiesParkourProfile_ToRuntimeSegment()
{
    var start = new PathNode(100, 80, 100);
    var end = new PathNode(99, 80, 102)
    {
        MoveUsed = MoveType.Parkour,
        ParkourProfile = ParkourProfile.Sidewall
    };

    List<PathSegment> segments = PathSegmentBuilder.FromPath([start, end]);

    Assert.Single(segments);
    Assert.Equal(ParkourProfile.Sidewall, segments[0].ParkourProfile);
}
```

- [ ] **Step 2: Run the profile-plumbing test to verify it fails**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~FromPath_CopiesParkourProfile_ToRuntimeSegment -v minimal
```

Expected: FAIL with missing members such as `ParkourProfile` on `PathNode`, `MoveResult`, and `PathSegment`.

- [ ] **Step 3: Add the profile enum and thread it through planner/runtime data structures**

```csharp
// MinecraftClient/Pathing/Core/ParkourProfile.cs
namespace MinecraftClient.Pathing.Core
{
    public enum ParkourProfile
    {
        None = 0,
        Default = 1,
        Sidewall = 2
    }
}
```

```csharp
// MinecraftClient/Pathing/Core/MoveResult.cs
public struct MoveResult
{
    public int DestX;
    public int DestY;
    public int DestZ;
    public double Cost;
    public ParkourProfile ParkourProfile;

    public void Set(int x, int y, int z, double cost, ParkourProfile parkourProfile = ParkourProfile.None)
    {
        DestX = x;
        DestY = y;
        DestZ = z;
        Cost = cost;
        ParkourProfile = parkourProfile;
    }

    public void SetImpossible()
    {
        Cost = ActionCosts.CostInf;
        ParkourProfile = ParkourProfile.None;
    }
}
```

```csharp
// MinecraftClient/Pathing/Core/PathNode.cs
public sealed class PathNode
{
    // existing fields...
    public MoveType MoveUsed;
    public ParkourProfile ParkourProfile;
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathSegment.cs
public sealed class PathSegment
{
    public required Location Start { get; init; }
    public required Location End { get; init; }
    public required MoveType MoveType { get; init; }
    public ParkourProfile ParkourProfile { get; init; } = ParkourProfile.None;
    // existing properties...
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs
private static PathSegment CreatePreview(PathNode start, PathNode end)
{
    return new PathSegment
    {
        Start = new Location(start.X + 0.5, start.Y, start.Z + 0.5),
        End = new Location(end.X + 0.5, end.Y, end.Z + 0.5),
        MoveType = end.MoveUsed,
        ParkourProfile = end.ParkourProfile
    };
}
```

```csharp
// MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs
result.Set(destX, destY, destZ, cost, ParkourProfile.Default);
```

- [ ] **Step 4: Run the profile-plumbing test to verify it passes**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~FromPath_CopiesParkourProfile_ToRuntimeSegment|FullyQualifiedName~FromPath_AnnotatesTraverseIntoParkour_AsPrepareJump" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient/Pathing/Core/ParkourProfile.cs \
        MinecraftClient/Pathing/Core/MoveResult.cs \
        MinecraftClient/Pathing/Core/PathNode.cs \
        MinecraftClient/Pathing/Execution/PathSegment.cs \
        MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs \
        MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs \
        MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs
git commit -m "refactor: thread parkour profile into runtime segments"
```

---

### Task 3: Implement Planner Support For Sidewall Parkour

**Files:**
- Create: `MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs`
- Create: `MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs`
- Modify: `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
- Modify: `MinecraftClient/Pathing/Core/AStarPathFinder.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`

- [ ] **Step 1: Write the failing planner tests for exact theory-allowed and theory-forbidden cases**

```csharp
// MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves.Impl;
using MinecraftClient.Tests.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

public sealed class MoveSidewallParkourTests
{
    [Theory]
    [InlineData("sidewall-flat-gap2-wo0", 2, 0, 0)]
    [InlineData("sidewall-flat-gap3-wo1", 3, 0, 1)]
    [InlineData("sidewall-ascend-gap2-dy+1-wo0", 2, 1, 0)]
    [InlineData("sidewall-ascend-gap3-dy+1-wo1", 3, 1, 1)]
    [InlineData("sidewall-descend-gap2-dy-1-wo0", 2, -1, 0)]
    [InlineData("sidewall-descend-gap3-dy-1-wo1", 3, -1, 1)]
    [InlineData("sidewall-descend-gap2-dy-2-wo0", 2, -2, 0)]
    [InlineData("sidewall-descend-gap3-dy-2-wo1", 3, -2, 1)]
    public void Calculate_AcceptsTheoryAllowedCases(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap, deltaY, wallOffset);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = new MoveSidewallParkour(xOffset: -1, zOffset: gap, yDelta: deltaY);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(ParkourProfile.Sidewall, result.ParkourProfile);
    }

    [Theory]
    [InlineData("sidewall-flat-gap5-wo0", 5, 0, 0)]
    [InlineData("sidewall-flat-gap5-wo1", 5, 0, 1)]
    [InlineData("sidewall-ascend-gap4-dy+1-wo0", 4, 1, 0)]
    [InlineData("sidewall-ascend-gap4-dy+1-wo1", 4, 1, 1)]
    [InlineData("sidewall-descend-gap6-dy-1-wo0", 6, -1, 0)]
    [InlineData("sidewall-descend-gap6-dy-2-wo1", 6, -2, 1)]
    public void Calculate_RejectsTheoryForbiddenCases(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap, deltaY, wallOffset);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = new MoveSidewallParkour(xOffset: -1, zOffset: gap, yDelta: deltaY);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.True(result.IsImpossible);
    }
}
```

```csharp
// MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
[Theory]
[MemberData(nameof(SidewallParkourScenarioBuilder.AcceptedCases), MemberType = typeof(SidewallParkourScenarioBuilder))]
public void AStar_SidewallAcceptedCases_PlanThroughAllThreeJumps(string scenarioId, int gap, int deltaY, int wallOffset)
{
    PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(scenarioId, gap, deltaY, wallOffset);
    PathResult result = PathingScenarioRunner.PlanOnly(scenario);
    List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

    Assert.Equal(PathStatus.Success, result.Status);
    Assert.Equal(3, segments.FindAll(segment => segment.MoveType == MoveType.Parkour).Count);
    Assert.All(segments, segment =>
    {
        if (segment.MoveType == MoveType.Parkour)
            Assert.Equal(ParkourProfile.Sidewall, segment.ParkourProfile);
    });
    Assert.Equal(scenario.Goal.X + 0.5, segments[^1].End.X);
    Assert.Equal(scenario.Goal.Y, segments[^1].End.Y);
    Assert.Equal(scenario.Goal.Z + 0.5, segments[^1].End.Z);
}

[Theory]
[MemberData(nameof(SidewallParkourScenarioBuilder.RejectedCases), MemberType = typeof(SidewallParkourScenarioBuilder))]
public void AStar_SidewallRejectedCases_RejectBeforeExecution(string scenarioId, int gap, int deltaY, int wallOffset)
{
    PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(scenarioId, gap, deltaY, wallOffset);
    PathResult result = PathingScenarioRunner.PlanOnly(scenario);

    Assert.Equal(PathStatus.Failed, result.Status);
    Assert.Empty(PathSegmentBuilder.FromPath(result.Path));
}
```

- [ ] **Step 2: Run the planner tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~MoveSidewallParkourTests|FullyQualifiedName~AStar_Sidewall" -v minimal
```

Expected: FAIL because `MoveSidewallParkour` does not exist yet and the planner currently has no sidewall candidate family.

- [ ] **Step 3: Implement a dedicated sidewall move and register the full candidate table**

```csharp
// MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    public sealed class MoveSidewallParkour : IMove
    {
        public MoveType Type => MoveType.Parkour;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        private readonly int _yDelta;

        public MoveSidewallParkour(int xOffset, int zOffset, int yDelta = 0)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
            _yDelta = yDelta;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            if (!ctx.AllowParkour || !ctx.CanSprint)
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.IsSidewallProfile(XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            ParkourFeasibility.GetSidewallAxes(XOffset, ZOffset, out int forwardX, out int forwardZ, out int lateralX, out int lateralZ);

            int destX = x + XOffset;
            int destY = y + _yDelta;
            int destZ = z + ZOffset;

            if (!ctx.CanWalkThrough(x, y + 2, z))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasDominantAxisRunUp(ctx, x, y, z, forwardX, forwardZ, XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasSidewallArcClearance(ctx, x, y, z, forwardX, forwardZ, lateralX, lateralZ, XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasSidewallLandingClearance(ctx, destX, destY, destZ, forwardX, forwardZ, lateralX, lateralZ))
            {
                result.SetImpossible();
                return;
            }

            double horizDist = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            double cost = _yDelta switch
            {
                > 0 => horizDist * ctx.SprintCost + ctx.JumpPenalty * 2,
                < 0 => horizDist * ctx.SprintCost + ctx.JumpPenalty + ActionCosts.FallCost(-_yDelta),
                _ => horizDist * ctx.SprintCost + ctx.JumpPenalty,
            };

            result.Set(destX, destY, destZ, cost, ParkourProfile.Sidewall);
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Moves/ParkourFeasibility.cs
internal static bool IsSidewallProfile(int xOffset, int zOffset, int yDelta)
{
    int absX = Math.Abs(xOffset);
    int absZ = Math.Abs(zOffset);
    int major = Math.Max(absX, absZ);
    int minor = Math.Min(absX, absZ);

    return minor == 1
        && major >= 2
        && major <= 5
        && yDelta is >= -2 and <= 1;
}

internal static void GetSidewallAxes(int xOffset, int zOffset, out int forwardX, out int forwardZ, out int lateralX, out int lateralZ)
{
    if (Math.Abs(xOffset) > Math.Abs(zOffset))
    {
        forwardX = Math.Sign(xOffset);
        forwardZ = 0;
        lateralX = 0;
        lateralZ = Math.Sign(zOffset);
    }
    else
    {
        forwardX = 0;
        forwardZ = Math.Sign(zOffset);
        lateralX = Math.Sign(xOffset);
        lateralZ = 0;
    }
}

internal static bool HasDominantAxisRunUp(CalculationContext ctx, int x, int y, int z, int forwardX, int forwardZ, int xOffset, int zOffset, int yDelta)
{
    int requiredBlocks = yDelta switch
    {
        > 0 => 2,
        < 0 when Math.Max(Math.Abs(xOffset), Math.Abs(zOffset)) >= 5 => 2,
        < 0 => 1,
        _ when Math.Max(Math.Abs(xOffset), Math.Abs(zOffset)) >= 4 => 2,
        _ => 1,
    };

    for (int i = 1; i <= requiredBlocks; i++)
    {
        int rx = x - forwardX * i;
        int rz = z - forwardZ * i;
        if (!ctx.CanWalkOn(rx, y - 1, rz) || !IsColumnPassable(ctx, rx, y, rz))
            return false;
    }

    return true;
}

internal static bool HasSidewallArcClearance(CalculationContext ctx, int x, int y, int z, int forwardX, int forwardZ, int lateralX, int lateralZ, int xOffset, int zOffset, int yDelta)
{
    int major = Math.Max(Math.Abs(xOffset), Math.Abs(zOffset));
    int insideWallDepth = 0;

    for (int step = 0; step < 2; step++)
    {
        int wx = x + lateralX + (forwardX * step);
        int wz = z + lateralZ + (forwardZ * step);
        if (ctx.CanWalkThrough(wx, y, wz) && ctx.CanWalkThrough(wx, y + 1, wz))
            break;
        insideWallDepth++;
    }

    if (insideWallDepth is < 1 or > 2)
        return false;

    for (int step = 1; step <= major; step++)
    {
        int cx = x + (forwardX * step);
        int cz = z + (forwardZ * step);
        if (!IsColumnPassable(ctx, cx, y, cz))
            return false;
    }

    int outsideX = x - lateralX;
    int outsideZ = z - lateralZ;
    return IsColumnPassable(ctx, outsideX, y, outsideZ);
}

internal static bool HasSidewallLandingClearance(CalculationContext ctx, int destX, int destY, int destZ, int forwardX, int forwardZ, int lateralX, int lateralZ)
{
    if (!ctx.CanWalkOn(destX, destY - 1, destZ))
        return false;

    if (!IsColumnPassable(ctx, destX, destY, destZ))
        return false;

    if (!IsColumnPassable(ctx, destX + forwardX, destY, destZ + forwardZ))
        return false;

    if (!IsColumnPassable(ctx, destX - lateralX, destY, destZ - lateralZ))
        return false;

    return true;
}
```

```csharp
// MinecraftClient/Pathing/Core/AStarPathFinder.cs
foreach (int dx in offsets)
{
    foreach (int dz in offsets)
    {
        foreach (int distance in new[] { 2, 3, 4, 5 })
        {
            moves.Add(new MoveSidewallParkour(dx, dz * distance));
            moves.Add(new MoveSidewallParkour(dx * distance, dz));

            if (distance <= 3)
            {
                moves.Add(new MoveSidewallParkour(dx, dz * distance, yDelta: 1));
                moves.Add(new MoveSidewallParkour(dx * distance, dz, yDelta: 1));
            }

            moves.Add(new MoveSidewallParkour(dx, dz * distance, yDelta: -1));
            moves.Add(new MoveSidewallParkour(dx * distance, dz, yDelta: -1));
            moves.Add(new MoveSidewallParkour(dx, dz * distance, yDelta: -2));
            moves.Add(new MoveSidewallParkour(dx * distance, dz, yDelta: -2));
        }
    }
}
```

- [ ] **Step 4: Run the planner tests to verify they pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~MoveSidewallParkourTests|FullyQualifiedName~AStar_Sidewall" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs \
        MinecraftClient/Pathing/Moves/ParkourFeasibility.cs \
        MinecraftClient/Pathing/Core/AStarPathFinder.cs \
        MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs \
        MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
git commit -m "feat: add sidewall parkour planner support"
```

---

### Task 4: Teach The Executor To Take Sidewall Jumps Without Replan Or Spin

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`

- [ ] **Step 1: Write the failing execution tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs
[Fact]
public void SprintJumpTemplate_SidewallFlatGap2_FinalStop_CompletesInsideLandingBlock()
{
    World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 2, deltaY: 0, wallOffset: 0);
    var segment = new PathSegment
    {
        Start = new Location(100.5, 80, 100.5),
        End = new Location(99.5, 80, 102.5),
        MoveType = MoveType.Parkour,
        ParkourProfile = ParkourProfile.Sidewall,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new SprintJumpTemplate(segment, null);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 0f);

    TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 120, out Location finalPos);

    Assert.Equal(TemplateState.Complete, state);
    Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
}
```

```csharp
// MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs
[Theory]
[MemberData(nameof(SidewallParkourScenarioBuilder.AcceptedCases), MemberType = typeof(SidewallParkourScenarioBuilder))]
public void Tick_SidewallAcceptedCases_CompletesWithoutReplan(string scenarioId, int gap, int deltaY, int wallOffset)
{
    PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(scenarioId, gap, deltaY, wallOffset);
    PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

    Assert.True(
        result.Completed && result.ReplanCount == 0,
        $"scenario={scenarioId} completed={result.Completed} replans={result.ReplanCount} final={result.FinalPosition}\n" +
        $"{string.Join('\n', result.InfoLogs)}\n{string.Join('\n', result.DebugLogs)}");
}
```

- [ ] **Step 2: Run the execution tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~SprintJumpTemplate_Sidewall|FullyQualifiedName~Tick_SidewallAcceptedCases" -v minimal
```

Expected: FAIL because the current template turns toward the landing yaw before it has built runway momentum, which either stalls in place or forces a rescue replan after a bad takeoff.

- [ ] **Step 3: Use `ParkourProfile.Sidewall` to separate runway heading from landing heading**

```csharp
// MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs
internal static void GetApproachHeading(PathSegment segment, out int headingX, out int headingZ)
{
    if (segment.ParkourProfile == ParkourProfile.Sidewall)
    {
        double dx = Math.Abs(segment.End.X - segment.Start.X);
        double dz = Math.Abs(segment.End.Z - segment.Start.Z);

        if (dx > dz)
        {
            headingX = segment.HeadingX;
            headingZ = 0;
        }
        else
        {
            headingX = 0;
            headingZ = segment.HeadingZ;
        }

        return;
    }

    headingX = segment.HeadingX;
    headingZ = segment.HeadingZ;
}

internal static float GetApproachYaw(PathSegment segment)
{
    GetApproachHeading(segment, out int headingX, out int headingZ);
    return CalculateYaw(headingX, headingZ);
}

internal static double ProgressAlongApproach(Location start, Location pos, PathSegment segment)
{
    GetApproachHeading(segment, out int headingX, out int headingZ);
    return ((pos.X - start.X) * headingX) + ((pos.Z - start.Z) * headingZ);
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs
float approachYaw = TemplateHelper.GetApproachYaw(_segment);
float activeYaw = _phase == Phase.Approach ? approachYaw : targetYaw;

physics.Yaw = groundedPrepareJumpHandoff
    ? TemplateHelper.SmoothYaw(physics.Yaw, TemplateHelper.GetExitHeadingYaw(_segment))
    : TemplateHelper.SmoothYaw(physics.Yaw, activeYaw);

case Phase.Approach:
    if (physics.OnGround)
    {
        double approachProgress = TemplateHelper.ProgressAlongApproach(ExpectedStart, pos, _segment);
        float yawDelta = YawDifference(physics.Yaw, approachYaw);
        bool turnInPlace = yawDelta > 35f;
        input.Forward = !turnInPlace;
        input.Sprint = !turnInPlace;

        double minApproachDistance = _segment.ParkourProfile == ParkourProfile.Sidewall
            ? 0.9
            : _horizDist >= 5.0 ? 0.8
            : _horizDist >= 4.0 ? 0.6
            : _horizDist > 3.5 ? 0.3
            : 0.0;

        if (yawDelta < YawToleranceDeg && approachProgress >= minApproachDistance)
        {
            input.Jump = true;
            _phase = Phase.Airborne;
        }
    }
    break;

case Phase.Airborne:
    if (_segment.ParkourProfile == ParkourProfile.Sidewall && _leftGround)
        physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw, maxStep: 20f);
    break;
```

- [ ] **Step 4: Run the execution tests to verify they pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~SprintJumpTemplate_Sidewall|FullyQualifiedName~Tick_SidewallAcceptedCases" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs \
        MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs \
        MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs \
        MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs
git commit -m "feat: execute sidewall parkour without replan"
```

---

### Task 5: Verify Sidewall Live Matrix And Protect Linear

**Files:**
- Test: `MinecraftClient.Tests/Pathing/Execution/SidewallParkourScenarioBuilderTests.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs`
- Test: `MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`

- [ ] **Step 1: Run the targeted .NET sidewall regression suite**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~SidewallParkourScenarioBuilderTests|FullyQualifiedName~FromPath_CopiesParkourProfile_ToRuntimeSegment|FullyQualifiedName~MoveSidewallParkourTests|FullyQualifiedName~AStar_Sidewall|FullyQualifiedName~SprintJumpTemplate_Sidewall|FullyQualifiedName~Tick_SidewallAcceptedCases" -v minimal
```

Expected: PASS.

- [ ] **Step 2: Run the full sidewall live matrix in parallel**

Run:

```bash
source tools/mcc-env.sh && python3 tools/test-parkour.py --filter sidewall --parallel 6 --version 1.21.11-Vanilla --results /tmp/sidewall-parkour-final.jsonl
```

Expected: summary reports `30/30 matched expectations` and `0 cases skipped`.

- [ ] **Step 3: Prove the sidewall JSONL has zero replan and zero turn-stall on accepted cases**

Run:

```bash
python3 - <<'PY'
import json
from pathlib import Path

rows = [json.loads(line) for line in Path('/tmp/sidewall-parkour-final.jsonl').read_text().splitlines() if line.strip()]
assert len(rows) == 30, len(rows)
assert sum(1 for row in rows if row["matched"]) == 30
assert all(
    row["outcome"] != "pass" or (row["replan_count"] == 0 and row["turn_stall_count"] == 0)
    for row in rows
)
print("rows", len(rows))
print("matched", sum(1 for row in rows if row["matched"]))
print("pass_cases", sum(1 for row in rows if row["outcome"] == "pass"))
print("reject_cases", sum(1 for row in rows if row["outcome"] == "reject"))
PY
```

Expected:

```text
rows 30
matched 30
pass_cases 22
reject_cases 8
```

- [ ] **Step 4: Re-run the linear live matrix as a hard regression guard**

Run:

```bash
source tools/mcc-env.sh && python3 tools/test-parkour.py --filter linear --parallel 6 --version 1.21.11-Vanilla --results /tmp/linear-guard-after-sidewall.jsonl
python3 - <<'PY'
import json
from pathlib import Path

rows = [json.loads(line) for line in Path('/tmp/linear-guard-after-sidewall.jsonl').read_text().splitlines() if line.strip()]
assert len(rows) == 22, len(rows)
assert sum(1 for row in rows if row["matched"]) == 22
assert all(
    row["outcome"] != "pass" or (row["replan_count"] == 0 and row["turn_stall_count"] == 0)
    for row in rows
)
print("rows", len(rows))
print("matched", sum(1 for row in rows if row["matched"]))
print("pass_cases", sum(1 for row in rows if row["outcome"] == "pass"))
print("reject_cases", sum(1 for row in rows if row["outcome"] == "reject"))
PY
```

Expected:

```text
rows 22
matched 22
pass_cases 18
reject_cases 4
```

- [ ] **Step 5: Re-run the existing green linear .NET regressions**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Tick_Linear|FullyQualifiedName~AStar_Linear|FullyQualifiedName~PathSegmentManager_LiveCoordinateLinear" -v minimal
```

Expected: PASS.

## Self-Review

**1. Spec coverage**

- Sidewall pass coverage: Task 3 adds exact theory-allowed and theory-forbidden planner tests; Task 4 adds executor no-replan tests; Task 5 runs the full `sidewall` live matrix.
- Zero replan / zero turn stall: Task 4 enforces `PathSegmentManager` no-replan behavior; Task 5 validates JSONL `replan_count` and `turn_stall_count`.
- Parallel verification through `tools/test-parkour.py`: Task 5 uses `--parallel 6`.
- Preserve linear: Task 5 re-runs both live `linear` matrix and existing green linear .NET regressions.
- Excluding `neo` and `ceiling`: called out explicitly in Scope And Guardrails.

**2. Placeholder scan**

- No `TODO`, `TBD`, or “similar to above” placeholders remain.
- Every task lists exact file paths, concrete test names, explicit commands, and concrete code identifiers.

**3. Type consistency**

- `ParkourProfile` is the single profile type threaded across `MoveResult`, `PathNode`, and `PathSegment`.
- `MoveSidewallParkour` is the dedicated planner type; generic `MoveParkour` remains tagged as `ParkourProfile.Default`.
- `SidewallParkourScenarioBuilder` is the shared fixture source used by move tests, live planner tests, and manager tests.
