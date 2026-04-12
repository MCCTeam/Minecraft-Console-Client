# Pathing Transition Braking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build next-segment-aware path execution that clears stale input on segment completion and uses predictive braking / momentum carry so MCC can enter turns, jumps, and final stops precisely on 1.21.11.

**Architecture:** Keep A* pathfinding unchanged and upgrade only the execution layer. First add a small regression test harness, then annotate segments with transition intent, add a deterministic braking planner, and finally let templates use that planner to either preserve momentum, coast, or brake based on the next segment.

**Tech Stack:** C# 14 / .NET 10, MCC `PlayerPhysics`, xUnit for deterministic regression tests, existing `tools/mcc-env.sh` + local 1.21.11 server harness for end-to-end validation.

---

## Execution Context

This plan assumes implementation happens in a dedicated worktree. Do not edit the repo-root `MinecraftClient.ini`; use the existing debug harness and temporary configs under `/tmp/mcc-debug/`.

## File Structure

### New files

- `MinecraftClient.Tests/MinecraftClient.Tests.csproj`
  Test project for path-execution and braking regressions.
- `MinecraftClient.Tests/Pathing/Execution/PathExecutorCompletionTests.cs`
  Locks in the stale-input regression where a completed segment still leaves `Forward`/`Sprint` set.
- `MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs`
  Verifies next-segment transition classification.
- `MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs`
  Minimal deterministic world builder for stone-floor braking tests.
- `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
  Verifies coasting, back-braking, and airborne forward release decisions.
- `MinecraftClient.Tests/Pathing/Execution/TemplateBrakingTests.cs`
  Verifies template-level use of the planner.
- `MinecraftClient/Pathing/Execution/PathTransitionType.cs`
  Enum describing the exit intent of a segment.
- `MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs`
  Converts `PathNode` paths into `PathSegment` lists with transition metadata.
- `MinecraftClient/Pathing/Execution/TransitionBrakingDecision.cs`
  Immutable result of the braking planner.
- `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
  Predictive stop-distance and airborne-release logic shared by templates.
- `tools/test-transition-braking.sh`
  Local 1.21.11 regression script for flat-stop and parkour-into-turn scenarios.

### Modified files

- `MinecraftClient.sln`
  Add the new test project.
- `MinecraftClient/Pathing/Execution/PathSegment.cs`
  Add heading and transition metadata to segments.
- `MinecraftClient/Pathing/Execution/IActionTemplate.cs`
  Pass `World` into template ticks so braking decisions can read friction.
- `MinecraftClient/Pathing/Execution/ActionTemplateFactory.cs`
  Construct templates with the current and next segment.
- `MinecraftClient/Pathing/Execution/PathExecutor.cs`
  Clear inputs on completion/failure, pass `World`, and wire next-segment context into templates.
- `MinecraftClient/Pathing/Execution/PathSegmentManager.cs`
  Swap `PathSegment.FromPath` for the new builder and pass `World` to the executor.
- `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
  Add helpers for settled-state checks and applying braking decisions.
- `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
  Use predictive braking for final stops and turns.
- `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
  Preserve takeoff until the jump is done, then settle according to the next segment.
- `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
  Use post-landing braking for turns/final stops and preserve momentum for straight continuations.
- `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  Release `Forward`/`Sprint` early in the air when the next segment needs a stop or turn.
- `MinecraftClient/Pathing/Execution/Templates/ClimbTemplate.cs`
  Signature-only change to accept `World`.
- `MinecraftClient/Pathing/Execution/Templates/FallTemplate.cs`
  Signature-only change to accept `World`.
- `docs/guide/pathfinding-research.md`
  Document transition-aware braking and how it differs from Baritone’s “goal block occupancy” semantics.

---

### Task 1: Add the Regression Harness and Fix Stale Input on Completion

**Files:**
- Create: `MinecraftClient.Tests/MinecraftClient.Tests.csproj`
- Create: `MinecraftClient.Tests/Pathing/Execution/PathExecutorCompletionTests.cs`
- Modify: `MinecraftClient.sln`
- Modify: `MinecraftClient/Pathing/Execution/PathExecutor.cs`

- [ ] **Step 1: Write the failing test project and failing completion regression**

```xml
<!-- MinecraftClient.Tests/MinecraftClient.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MinecraftClient\MinecraftClient.csproj" />
  </ItemGroup>
</Project>
```

```csharp
// MinecraftClient.Tests/Pathing/Execution/PathExecutorCompletionTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathExecutorCompletionTests
{
    [Fact]
    public void Tick_ClearsMovementInput_WhenSegmentCompletes()
    {
        var executor = new PathExecutor(new List<PathSegment>
        {
            new()
            {
                Start = new Location(0.5, 80, 0.5),
                End = new Location(1.5, 80, 0.5),
                MoveType = MoveType.Traverse
            }
        });

        var physics = new PlayerPhysics
        {
            Yaw = 270f,
            Pitch = 0f
        };
        var input = new MovementInput();
        var pos = new Location(1.45, 80, 0.5);

        PathExecutorState state = executor.Tick(pos, physics, input);

        Assert.Equal(PathExecutorState.Complete, state);
        Assert.False(input.Forward);
        Assert.False(input.Sprint);
        Assert.False(input.Jump);
        Assert.False(input.Back);
    }
}
```

- [ ] **Step 2: Add the test project to the solution and run the test to verify it fails**

Run:

```bash
dotnet sln MinecraftClient.sln add MinecraftClient.Tests/MinecraftClient.Tests.csproj
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter Tick_ClearsMovementInput_WhenSegmentCompletes -v minimal
```

Expected: FAIL because `PathExecutor.Tick()` returns `Complete` while `input.Forward` is still `true`.

- [ ] **Step 3: Write the minimal implementation in the executor**

```csharp
// MinecraftClient/Pathing/Execution/PathExecutor.cs
public PathExecutorState Tick(Location pos, PlayerPhysics physics, MovementInput input)
{
    if (_currentTemplate is null)
    {
        input.Reset();
        return PathExecutorState.Complete;
    }

    var state = _currentTemplate.Tick(pos, physics, input);

    switch (state)
    {
        case TemplateState.Complete:
            input.Reset();
            _debugLog?.Invoke($"[PathExec] Segment {_currentIndex} complete " +
                $"({_segments[_currentIndex].MoveType}) at ({pos.X:F2},{pos.Y:F2},{pos.Z:F2})");
            _currentIndex++;
            if (_currentIndex >= _segments.Count)
            {
                _currentTemplate = null;
                _debugLog?.Invoke("[PathExec] All segments complete!");
                return PathExecutorState.Complete;
            }
            AdvanceToNextSegment();
            return PathExecutorState.InProgress;

        case TemplateState.Failed:
            input.Reset();
            _debugLog?.Invoke($"[PathExec] Segment {_currentIndex} FAILED " +
                $"({_segments[_currentIndex].MoveType}) at ({pos.X:F2},{pos.Y:F2},{pos.Z:F2}), " +
                $"target was ({_currentTemplate.ExpectedEnd.X:F2},{_currentTemplate.ExpectedEnd.Y:F2},{_currentTemplate.ExpectedEnd.Z:F2})");
            return PathExecutorState.Failed;

        default:
            return PathExecutorState.InProgress;
    }
}
```

- [ ] **Step 4: Run the test project and make sure the regression passes**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter Tick_ClearsMovementInput_WhenSegmentCompletes -v minimal
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient.sln \
  MinecraftClient.Tests/MinecraftClient.Tests.csproj \
  MinecraftClient.Tests/Pathing/Execution/PathExecutorCompletionTests.cs \
  MinecraftClient/Pathing/Execution/PathExecutor.cs
git commit -m "test: lock path executor completion input reset"
```

### Task 2: Add Transition Metadata to Path Segments

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs`
- Create: `MinecraftClient/Pathing/Execution/PathTransitionType.cs`
- Create: `MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegment.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegmentManager.cs`

- [ ] **Step 1: Write failing tests for transition classification**

```csharp
// MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathSegmentBuilderTests
{
    [Fact]
    public void FromPath_AnnotatesStraightTraverse_AsContinueStraight()
    {
        var nodes = BuildNodes(
            (0, 80, 0, MoveType.Traverse),
            (1, 80, 0, MoveType.Traverse),
            (2, 80, 0, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);

        Assert.Equal(PathTransitionType.ContinueStraight, segments[0].ExitTransition);
        Assert.True(segments[0].PreserveSprint);
    }

    [Fact]
    public void FromPath_AnnotatesOrthogonalTraverse_AsTurn()
    {
        var nodes = BuildNodes(
            (0, 80, 0, MoveType.Traverse),
            (1, 80, 0, MoveType.Traverse),
            (1, 80, 1, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);

        Assert.Equal(PathTransitionType.Turn, segments[0].ExitTransition);
        Assert.False(segments[0].PreserveSprint);
    }

    [Fact]
    public void FromPath_AnnotatesTraverseIntoParkour_AsPrepareJump()
    {
        var nodes = BuildNodes(
            (120, 80, 110, MoveType.Traverse),
            (121, 80, 110, MoveType.Traverse),
            (123, 80, 110, MoveType.Parkour));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);

        Assert.Equal(PathTransitionType.PrepareJump, segments[0].ExitTransition);
        Assert.True(segments[0].PreserveSprint);
    }

    private static List<PathNode> BuildNodes(params (int x, int y, int z, MoveType moveUsed)[] raw)
    {
        var result = new List<PathNode>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            var node = new PathNode(raw[i].x, raw[i].y, raw[i].z);
            if (i > 0)
                node.MoveUsed = raw[i].moveUsed;
            result.Add(node);
        }
        return result;
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter PathSegmentBuilderTests -v minimal
```

Expected: FAIL because `PathSegmentBuilder` and `PathTransitionType` do not exist yet.

- [ ] **Step 3: Add the transition enum and extend `PathSegment`**

```csharp
// MinecraftClient/Pathing/Execution/PathTransitionType.cs
namespace MinecraftClient.Pathing.Execution
{
    public enum PathTransitionType
    {
        FinalStop,
        ContinueStraight,
        Turn,
        PrepareJump,
        LandingRecovery
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathSegment.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Execution
{
    public sealed class PathSegment
    {
        public required Location Start { get; init; }
        public required Location End { get; init; }
        public required MoveType MoveType { get; init; }
        public PathTransitionType ExitTransition { get; init; } = PathTransitionType.FinalStop;
        public bool PreserveSprint { get; init; }

        public int HeadingX => Math.Sign(End.X - Start.X);
        public int HeadingZ => Math.Sign(End.Z - Start.Z);

        public override string ToString() =>
            $"{MoveType}: ({Start.X:F1},{Start.Y:F1},{Start.Z:F1})->({End.X:F1},{End.Y:F1},{End.Z:F1}), transition={ExitTransition}, preserveSprint={PreserveSprint}";
    }
}
```

- [ ] **Step 4: Add the builder and switch the manager to use it**

```csharp
// MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs
using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Execution
{
    public static class PathSegmentBuilder
    {
        public static List<PathSegment> FromPath(IReadOnlyList<PathNode> nodes)
        {
            var segments = new List<PathSegment>(Math.Max(0, nodes.Count - 1));
            for (int i = 1; i < nodes.Count; i++)
            {
                PathSegment? next = null;
                if (i + 1 < nodes.Count)
                {
                    var nextNode = nodes[i + 1];
                    var curr = nodes[i];
                    next = new PathSegment
                    {
                        Start = new Location(curr.X + 0.5, curr.Y, curr.Z + 0.5),
                        End = new Location(nextNode.X + 0.5, nextNode.Y, nextNode.Z + 0.5),
                        MoveType = nextNode.MoveUsed
                    };
                }

                var prev = nodes[i - 1];
                var currNode = nodes[i];
                var current = new PathSegment
                {
                    Start = new Location(prev.X + 0.5, prev.Y, prev.Z + 0.5),
                    End = new Location(currNode.X + 0.5, currNode.Y, currNode.Z + 0.5),
                    MoveType = currNode.MoveUsed
                };

                PathTransitionType exitTransition = Classify(current, next);
                segments.Add(new PathSegment
                {
                    Start = current.Start,
                    End = current.End,
                    MoveType = current.MoveType,
                    ExitTransition = exitTransition,
                    PreserveSprint = exitTransition is PathTransitionType.ContinueStraight or PathTransitionType.PrepareJump
                });
            }
            return segments;
        }

        private static PathTransitionType Classify(PathSegment current, PathSegment? next)
        {
            if (next is null)
                return PathTransitionType.FinalStop;

            if (next.MoveType is MoveType.Parkour or MoveType.Ascend)
                return PathTransitionType.PrepareJump;

            if (current.MoveType is MoveType.Parkour or MoveType.Descend or MoveType.Fall)
                return PathTransitionType.LandingRecovery;

            if (current.HeadingX == next.HeadingX && current.HeadingZ == next.HeadingZ)
                return PathTransitionType.ContinueStraight;

            return PathTransitionType.Turn;
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathSegmentManager.cs
public void StartNavigation(IGoal goal, PathResult result)
{
    _goal = goal;
    _replanCount = 0;
    var segments = PathSegmentBuilder.FromPath(result.Path);
    _executor = new PathExecutor(segments, _debugLog);
    _infoLog?.Invoke($"[PathMgr] Navigation started: {segments.Count} segments");
}

private void Replan(Location pos, World world)
{
    // existing code omitted for brevity above

    var segments = PathSegmentBuilder.FromPath(result.Path);
    _executor = new PathExecutor(segments, _debugLog);
    _infoLog?.Invoke($"[PathMgr] Replanned: {segments.Count} segments (replan #{_replanCount})");
}
```

- [ ] **Step 5: Run the tests and make sure the builder is green**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter PathSegmentBuilderTests -v minimal
```

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs \
  MinecraftClient/Pathing/Execution/PathTransitionType.cs \
  MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs \
  MinecraftClient/Pathing/Execution/PathSegment.cs \
  MinecraftClient/Pathing/Execution/PathSegmentManager.cs
git commit -m "feat: annotate path segments with transition intent"
```

### Task 3: Add the Predictive Braking Planner

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
- Create: `MinecraftClient/Pathing/Execution/TransitionBrakingDecision.cs`
- Create: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`

- [ ] **Step 1: Write failing deterministic planner tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs
using MinecraftClient.Mapping;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class FlatWorldTestBuilder
{
    public static World CreateStoneFloor(int floorY = 79, int min = -32, int max = 32)
    {
        World.LoadDefaultDimensions1206Plus();
        World.SetDimension("minecraft:overworld");

        var world = new World();
        int minChunk = (int)Math.Floor(min / 16.0);
        int maxChunk = (int)Math.Floor(max / 16.0);

        for (int chunkX = minChunk; chunkX <= maxChunk; chunkX++)
        {
            for (int chunkZ = minChunk; chunkZ <= maxChunk; chunkZ++)
            {
                world[chunkX, chunkZ] = new ChunkColumn(24) { FullyLoaded = true };
            }
        }

        for (int x = min; x <= max; x++)
        {
            for (int z = min; z <= max; z++)
            {
                world.SetBlock(new Location(x, floorY, z), new Block(1));
            }
        }

        return world;
    }
}
```

```csharp
// MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TransitionBrakingPlannerTests
{
    [Fact]
    public void Plan_ReturnsCarryMomentum_ForContinueStraight()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var physics = CreatePhysics(0.156, 0.0, onGround: true);
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.ContinueStraight,
            PreserveSprint = true
        };

        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(current, null, new Location(1.05, 80, 0.5), physics, world);

        Assert.True(decision.HoldForward);
        Assert.True(decision.HoldSprint);
        Assert.False(decision.HoldBack);
    }

    [Fact]
    public void Plan_ReleasesForward_ForFinalStop_WhenRemainingRunwayIsTooShort()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var physics = CreatePhysics(0.156, 0.0, onGround: true);
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop,
            PreserveSprint = false
        };

        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(current, null, new Location(1.38, 80, 0.5), physics, world);

        Assert.False(decision.HoldForward);
        Assert.False(decision.HoldSprint);
        Assert.False(decision.HoldBack);
    }

    [Fact]
    public void ShouldReleaseForwardInAir_ReturnsTrue_ForParkourIntoTurn()
    {
        var physics = CreatePhysics(0.32, 0.0, onGround: false);
        var current = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(123.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.Turn,
            PreserveSprint = false
        };
        var next = new PathSegment
        {
            Start = new Location(123.5, 80, 110.5),
            End = new Location(123.5, 80, 111.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        bool release = TransitionBrakingPlanner.ShouldReleaseForwardInAir(current, next, new Location(123.18, 80.92, 110.5), physics);

        Assert.True(release);
    }

    private static PlayerPhysics CreatePhysics(double deltaX, double deltaZ, bool onGround)
    {
        return new PlayerPhysics
        {
            Position = new Vec3d(0.0, 80.0, 0.0),
            DeltaMovement = new Vec3d(deltaX, 0.0, deltaZ),
            OnGround = onGround,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };
    }
}
```

- [ ] **Step 2: Run the planner tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter TransitionBrakingPlannerTests -v minimal
```

Expected: FAIL because `TransitionBrakingPlanner` and `TransitionBrakingDecision` do not exist yet.

- [ ] **Step 3: Add the decision type**

```csharp
// MinecraftClient/Pathing/Execution/TransitionBrakingDecision.cs
namespace MinecraftClient.Pathing.Execution
{
    public readonly record struct TransitionBrakingDecision(bool HoldForward, bool HoldSprint, bool HoldBack)
    {
        public static TransitionBrakingDecision CarryMomentum(bool preserveSprint) =>
            new(true, preserveSprint, false);

        public static TransitionBrakingDecision Coast =>
            new(false, false, false);

        public static TransitionBrakingDecision Brake =>
            new(false, false, true);
    }
}
```

- [ ] **Step 4: Add the braking planner**

```csharp
// MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    public static class TransitionBrakingPlanner
    {
        private const double GroundSpeedThreshold = 0.03;
        private const int MaxSimulationTicks = 12;
        private const double FinalStopLead = 0.04;
        private const double TurnBrakeLead = 0.08;
        private const double AirReleaseLead = 0.08;

        public static TransitionBrakingDecision Plan(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
        {
            if (current.ExitTransition is PathTransitionType.ContinueStraight or PathTransitionType.PrepareJump)
                return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);

            double remaining = RemainingDistanceAlongSegment(current, pos);
            double coastStopDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: false);
            double hardBrakeDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: true);

            if (current.ExitTransition == PathTransitionType.Turn && remaining <= hardBrakeDistance + TurnBrakeLead)
                return TransitionBrakingDecision.Brake;

            if (remaining <= coastStopDistance + FinalStopLead)
                return TransitionBrakingDecision.Coast;

            return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);
        }

        public static bool ShouldReleaseForwardInAir(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics)
        {
            if (current.ExitTransition is not (PathTransitionType.FinalStop or PathTransitionType.Turn or PathTransitionType.LandingRecovery))
                return false;

            double remaining = RemainingDistanceAlongSegment(current, pos);
            double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));

            return remaining <= forwardSpeed + AirReleaseLead;
        }

        public static double EstimateGroundStopDistance(PlayerPhysics physics, World world, int headingX, int headingZ, bool applyBackBrake)
        {
            if (!physics.OnGround)
                return 0.0;

            double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, headingX, headingZ));
            if (forwardSpeed <= GroundSpeedThreshold)
                return 0.0;

            float blockFriction = PlayerPhysics.GetMaterialFriction(
                world.GetBlock(new Location(physics.Position.X, physics.Position.Y - 0.5000010, physics.Position.Z)).Type);
            double drag = blockFriction * PhysicsConsts.FrictionMultiplier;
            double acceleration = physics.MovementSpeed
                                  * (PhysicsConsts.GroundAccelerationFactor / (drag * drag * drag))
                                  * PhysicsConsts.InputFriction;

            if (applyBackBrake)
                acceleration *= 0.98;

            double distance = 0.0;
            double speed = forwardSpeed;
            for (int tick = 0; tick < MaxSimulationTicks; tick++)
            {
                distance += speed;
                speed = applyBackBrake
                    ? Math.Max(0.0, (speed - acceleration) * drag)
                    : speed * drag;

                if (speed <= GroundSpeedThreshold)
                    break;
            }

            return distance;
        }

        private static double RemainingDistanceAlongSegment(PathSegment current, Location pos)
        {
            double dx = current.End.X - pos.X;
            double dz = current.End.Z - pos.Z;
            return dx * current.HeadingX + dz * current.HeadingZ;
        }

        private static double ProjectHorizontalSpeedAlongHeading(PlayerPhysics physics, int headingX, int headingZ)
        {
            return physics.DeltaMovement.X * headingX + physics.DeltaMovement.Z * headingZ;
        }
    }
}
```

- [ ] **Step 5: Run the tests and make sure the planner is green**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter TransitionBrakingPlannerTests -v minimal
```

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/FlatWorldTestBuilder.cs \
  MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs \
  MinecraftClient/Pathing/Execution/TransitionBrakingDecision.cs \
  MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs
git commit -m "feat: add predictive transition braking planner"
```

### Task 4: Wire the Planner into the Templates and Executor

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/TemplateBrakingTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/IActionTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/ActionTemplateFactory.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathExecutor.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegmentManager.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/ClimbTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/FallTemplate.cs`

- [ ] **Step 1: Write failing template-level tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/TemplateBrakingTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TemplateBrakingTests
{
    [Fact]
    public void WalkTemplate_CoastsInsteadOfHoldingForward_WhenFinalStopIsClose()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop,
            PreserveSprint = false
        };

        var template = new WalkTemplate(segment, null);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.38, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.156, 0.0, 0.0),
            OnGround = true,
            Yaw = 270f
        };
        var input = new MovementInput();

        TemplateState state = template.Tick(new Location(1.38, 80, 0.5), physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.False(input.Forward);
        Assert.False(input.Sprint);
        Assert.False(input.Back);
    }

    [Fact]
    public void WalkTemplate_KeepsForward_WhenTransitionContinuesStraight()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.ContinueStraight,
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(2.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(current, next);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.10, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.140, 0.0, 0.0),
            OnGround = true,
            Yaw = 270f
        };
        var input = new MovementInput();

        TemplateState state = template.Tick(new Location(1.10, 80, 0.5), physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.True(input.Forward);
        Assert.True(input.Sprint);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter TemplateBrakingTests -v minimal
```

Expected: FAIL because templates do not accept `PathSegment`/`World` yet and do not consult the braking planner.

- [ ] **Step 3: Change the executor and template plumbing to pass `World` and next-segment context**

```csharp
// MinecraftClient/Pathing/Execution/IActionTemplate.cs
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    public interface IActionTemplate
    {
        Location ExpectedStart { get; }
        Location ExpectedEnd { get; }

        TemplateState Tick(Location currentPos, PlayerPhysics physics, MovementInput input, World world);
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/ActionTemplateFactory.cs
using System;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution.Templates;

namespace MinecraftClient.Pathing.Execution
{
    public static class ActionTemplateFactory
    {
        public static IActionTemplate Create(PathSegment segment, PathSegment? nextSegment)
        {
            return segment.MoveType switch
            {
                MoveType.Traverse => new WalkTemplate(segment, nextSegment),
                MoveType.Diagonal => new WalkTemplate(segment, nextSegment),
                MoveType.Ascend   => new AscendTemplate(segment, nextSegment),
                MoveType.Descend  => new DescendTemplate(segment, nextSegment),
                MoveType.Fall     => new FallTemplate(segment, nextSegment),
                MoveType.Climb    => new ClimbTemplate(segment, nextSegment),
                MoveType.Parkour  => new SprintJumpTemplate(segment, nextSegment),
                _ => throw new ArgumentException($"Unknown MoveType: {segment.MoveType}")
            };
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathExecutor.cs
public PathExecutorState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
{
    if (_currentTemplate is null)
    {
        input.Reset();
        return PathExecutorState.Complete;
    }

    var state = _currentTemplate.Tick(pos, physics, input, world);

    switch (state)
    {
        case TemplateState.Complete:
            input.Reset();
            _debugLog?.Invoke($"[PathExec] Segment {_currentIndex} complete " +
                $"({_segments[_currentIndex].MoveType}) at ({pos.X:F2},{pos.Y:F2},{pos.Z:F2})");
            _currentIndex++;
            if (_currentIndex >= _segments.Count)
            {
                _currentTemplate = null;
                _debugLog?.Invoke("[PathExec] All segments complete!");
                return PathExecutorState.Complete;
            }
            AdvanceToNextSegment();
            return PathExecutorState.InProgress;

        case TemplateState.Failed:
            input.Reset();
            _debugLog?.Invoke($"[PathExec] Segment {_currentIndex} FAILED " +
                $"({_segments[_currentIndex].MoveType}) at ({pos.X:F2},{pos.Y:F2},{pos.Z:F2}), " +
                $"target was ({_currentTemplate.ExpectedEnd.X:F2},{_currentTemplate.ExpectedEnd.Y:F2},{_currentTemplate.ExpectedEnd.Z:F2})");
            return PathExecutorState.Failed;

        default:
            return PathExecutorState.InProgress;
    }
}

private void AdvanceToNextSegment()
{
    if (_currentIndex < _segments.Count)
    {
        var seg = _segments[_currentIndex];
        PathSegment? next = _currentIndex + 1 < _segments.Count ? _segments[_currentIndex + 1] : null;
        _currentTemplate = ActionTemplateFactory.Create(seg, next);
        _debugLog?.Invoke($"[PathExec] Starting segment {_currentIndex}/{_segments.Count}: {seg}");
    }
    else
    {
        _currentTemplate = null;
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathSegmentManager.cs
public void Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
{
    if (_executor is null)
        return;

    var state = _executor.Tick(pos, physics, input, world);

    switch (state)
    {
        case PathExecutorState.Complete:
            _infoLog?.Invoke("[PathMgr] Navigation complete!");
            _executor = null;
            _goal = null;
            break;

        case PathExecutorState.Failed:
            _infoLog?.Invoke("[PathMgr] Segment failed, replanning...");
            Replan(pos, world);
            break;
    }
}
```

- [ ] **Step 4: Wire the planner into the templates**

```csharp
// MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    internal static class TemplateHelper
    {
        // existing methods omitted

        internal static void ApplyDecision(MovementInput input, TransitionBrakingDecision decision)
        {
            input.Forward = decision.HoldForward;
            input.Sprint = decision.HoldSprint;
            input.Back = decision.HoldBack;
        }

        internal static bool IsSettledAtEnd(Location pos, Location target, PlayerPhysics physics, double horizThresholdSq = 0.01, double speedThresholdSq = 0.0009)
        {
            double dx = target.X - pos.X;
            double dz = target.Z - pos.Z;
            double horizontalSpeedSq = physics.DeltaMovement.X * physics.DeltaMovement.X
                                       + physics.DeltaMovement.Z * physics.DeltaMovement.Z;
            return dx * dx + dz * dz <= horizThresholdSq && horizontalSpeedSq <= speedThresholdSq;
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    public sealed class WalkTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private int _tickCount;
        private Location _lastPos;
        private int _stuckTicks;

        public WalkTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
            _lastPos = segment.Start;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
            TemplateHelper.ApplyDecision(input, decision);

            if (_segment.ExitTransition == PathTransitionType.ContinueStraight && TemplateHelper.IsNear(pos, ExpectedEnd, horizThresholdSq: 0.20))
                return TemplateState.Complete;

            if (_segment.ExitTransition != PathTransitionType.ContinueStraight && TemplateHelper.IsSettledAtEnd(pos, ExpectedEnd, physics))
                return TemplateState.Complete;

            double movedSq = TemplateHelper.HorizontalDistanceSq(pos, _lastPos);
            _stuckTicks = movedSq < 0.0005 ? _stuckTicks + 1 : 0;
            _lastPos = pos;

            if (_stuckTicks > 40 || _tickCount > 100)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    public sealed class AscendTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private int _tickCount;
        private Location _lastPos;
        private int _stuckTicks;

        public AscendTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
            _lastPos = segment.Start;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            input.Forward = true;
            input.Sprint = true;

            if (physics.OnGround && dy > 0.1)
                input.Jump = true;

            if (physics.OnGround && Math.Abs(dy) < 0.15)
            {
                TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                TemplateHelper.ApplyDecision(input, decision);
                if (_segment.ExitTransition != PathTransitionType.ContinueStraight && TemplateHelper.IsSettledAtEnd(pos, ExpectedEnd, physics, horizThresholdSq: 0.02))
                    return TemplateState.Complete;
            }
            else if (dx * dx + dz * dz < 0.25 && Math.Abs(dy) < 0.8)
            {
                return TemplateState.Complete;
            }

            double movedSq = TemplateHelper.HorizontalDistanceSq(pos, _lastPos);
            double movedY = Math.Abs(pos.Y - _lastPos.Y);
            _stuckTicks = (movedSq < 0.0005 && movedY < 0.001) ? _stuckTicks + 1 : 0;
            _lastPos = pos;

            if (_stuckTicks > 40 || _tickCount > 80)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    public sealed class DescendTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private int _tickCount;
        private bool _hasFallen;
        private readonly bool _needsSprint;

        public DescendTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
            double hdx = segment.End.X - segment.Start.X;
            double hdz = segment.End.Z - segment.Start.Z;
            _needsSprint = (hdx * hdx + hdz * hdz) > 2.25;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;

            if (!physics.OnGround)
                _hasFallen = true;

            if (_hasFallen && physics.OnGround)
            {
                TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                TemplateHelper.ApplyDecision(input, decision);

                if (_segment.ExitTransition == PathTransitionType.ContinueStraight && horizDistSq < 0.5 && Math.Abs(dy) < 0.8)
                    return TemplateState.Complete;

                if (_segment.ExitTransition != PathTransitionType.ContinueStraight && TemplateHelper.IsSettledAtEnd(pos, ExpectedEnd, physics, horizThresholdSq: 0.02))
                    return TemplateState.Complete;
            }
            else if (horizDistSq > 0.01)
            {
                float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
                float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
                physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
                physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);
                input.Forward = true;
                if (_needsSprint)
                    input.Sprint = true;
            }

            if (pos.Y > ExpectedStart.Y + 2.0 || _tickCount > 200)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs
using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    public sealed class SprintJumpTemplate : IActionTemplate
    {
        private enum Phase { Approach, Airborne, Landing }

        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private readonly double _horizDist;
        private int _tickCount;
        private Phase _phase = Phase.Approach;
        private bool _leftGround;

        private const float YawToleranceDeg = 5f;

        public SprintJumpTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
            double dx = segment.End.X - segment.Start.X;
            double dz = segment.End.Z - segment.Start.Z;
            _horizDist = Math.Sqrt(dx * dx + dz * dz);
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;

            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            switch (_phase)
            {
                case Phase.Approach:
                    input.Forward = true;
                    input.Sprint = true;
                    if (physics.OnGround)
                    {
                        double fromStartSq = TemplateHelper.HorizontalDistanceSq(pos, ExpectedStart);
                        float yawDelta = YawDifference(physics.Yaw, targetYaw);
                        double minApproachSq = _horizDist >= 4.0 ? 0.36 : _horizDist > 2.5 ? 0.09 : 0.0;
                        if (yawDelta < YawToleranceDeg && fromStartSq >= minApproachSq)
                        {
                            input.Jump = true;
                            _phase = Phase.Airborne;
                        }
                    }
                    break;

                case Phase.Airborne:
                    if (!physics.OnGround)
                        _leftGround = true;

                    bool releaseInAir = TransitionBrakingPlanner.ShouldReleaseForwardInAir(_segment, _nextSegment, pos, physics);
                    if (releaseInAir || IsPastTarget(pos))
                    {
                        input.Forward = false;
                        input.Sprint = false;
                    }
                    else
                    {
                        input.Forward = true;
                        input.Sprint = true;
                    }

                    if (_leftGround && physics.OnGround)
                    {
                        _phase = Phase.Landing;
                        goto case Phase.Landing;
                    }
                    break;

                case Phase.Landing:
                    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                    TemplateHelper.ApplyDecision(input, decision);

                    if (_segment.ExitTransition == PathTransitionType.ContinueStraight && horizDistSq < 1.0 && Math.Abs(dy) < 1.0)
                        return TemplateState.Complete;

                    if (_segment.ExitTransition != PathTransitionType.ContinueStraight && TemplateHelper.IsSettledAtEnd(pos, ExpectedEnd, physics, horizThresholdSq: 0.04))
                        return TemplateState.Complete;
                    break;
            }

            if (pos.Y < ExpectedEnd.Y - 4.0 || _tickCount > 60)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }

        private bool IsPastTarget(Location pos)
        {
            double dirX = ExpectedEnd.X - ExpectedStart.X;
            double dirZ = ExpectedEnd.Z - ExpectedStart.Z;
            double len = Math.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len < 0.001) return false;
            dirX /= len;
            dirZ /= len;

            double relX = pos.X - ExpectedEnd.X;
            double relZ = pos.Z - ExpectedEnd.Z;
            double dot = relX * dirX + relZ * dirZ;
            return dot > 0.0;
        }

        private static float YawDifference(float current, float target)
        {
            float delta = target - current;
            while (delta > 180f) delta -= 360f;
            while (delta < -180f) delta += 360f;
            return Math.Abs(delta);
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/ClimbTemplate.cs and FallTemplate.cs
// Signature-only example to apply verbatim in both files:
public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
{
    // existing body unchanged
}
```

- [ ] **Step 5: Run the test suite for the executor, planner, and templates**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "PathExecutorCompletionTests|PathSegmentBuilderTests|TransitionBrakingPlannerTests|TemplateBrakingTests" -v minimal
```

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/TemplateBrakingTests.cs \
  MinecraftClient/Pathing/Execution/IActionTemplate.cs \
  MinecraftClient/Pathing/Execution/ActionTemplateFactory.cs \
  MinecraftClient/Pathing/Execution/PathExecutor.cs \
  MinecraftClient/Pathing/Execution/PathSegmentManager.cs \
  MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs \
  MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs \
  MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs \
  MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs \
  MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs \
  MinecraftClient/Pathing/Execution/Templates/ClimbTemplate.cs \
  MinecraftClient/Pathing/Execution/Templates/FallTemplate.cs
git commit -m "feat: wire transition braking into path templates"
```

### Task 5: Tune on a Real 1.21.11 Server and Document the Behavior

**Files:**
- Create: `tools/test-transition-braking.sh`
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
- Modify: `docs/guide/pathfinding-research.md`

- [ ] **Step 1: Write the failing 1.21.11 integration regression script**

```bash
#!/usr/bin/env bash
# tools/test-transition-braking.sh
set -euo pipefail

source "$(dirname "$0")/mcc-env.sh"

VERSION="1.21.11-Vanilla"
SESSION="mcc-brake-test"
CFG="/tmp/mcc-debug/MinecraftClient.debug.ini"

send_mcc() {
    tmux send-keys -t "$SESSION" "$1" Enter
}

capture_pane() {
    tmux capture-pane -t "$SESSION" -p -S -120
}

extract_last_location() {
    capture_pane | python3 - <<'PY'
import re
import sys

text = sys.stdin.read()
matches = re.findall(r"Location\s+([-\d.]+),\s+([-\d.]+),\s+([-\d.]+)", text)
if not matches:
    raise SystemExit("No Location line found in tmux capture")
x, y, z = matches[-1]
print(f"{x} {y} {z}")
PY
}

assert_close() {
    local actual_x="$1"
    local actual_y="$2"
    local actual_z="$3"
    local expected_x="$4"
    local expected_y="$5"
    local expected_z="$6"
    local tolerance="${7:-0.05}"

    python3 - <<'PY' "$actual_x" "$actual_y" "$actual_z" "$expected_x" "$expected_y" "$expected_z" "$tolerance"
import math
import sys

ax, ay, az, ex, ey, ez, tol = map(float, sys.argv[1:])
if abs(ax - ex) > tol or abs(ay - ey) > tol or abs(az - ez) > tol:
    raise SystemExit(
        f"Expected ({ex:.2f}, {ey:.2f}, {ez:.2f}) within {tol:.2f}, got ({ax:.2f}, {ay:.2f}, {az:.2f})"
    )
PY
}

source tools/mcc-env.sh
mcc-preflight "$VERSION" >/dev/null
mc-reset-test-env "$VERSION" >/dev/null
mc-start "$VERSION" >/dev/null

if ! tmux has-session -t "$SESSION" 2>/dev/null; then
    tmux new-session -d -s "$SESSION" -x 160 -y 50 \
        "cd '$MCC_REPO' && dotnet run --project MinecraftClient -c Release --no-build -- '$CFG' CursorBot - localhost:25565; echo '=== MCC EXITED ==='; sleep 600"
    sleep 5
fi

mc-rcon "difficulty peaceful" >/dev/null 2>&1 || true
send_mcc "/debug on"
sleep 1

echo "== Flat final stop =="
mc-rcon "fill 95 79 95 115 79 105 stone" >/dev/null
mc-rcon "fill 95 80 95 115 85 105 air" >/dev/null
mc-rcon "tp CursorBot 100.5 80 100.5" >/dev/null
sleep 2
send_mcc "/goto 103 80 100"
sleep 5
send_mcc "/debug state"
sleep 1
read -r x y z <<< "$(extract_last_location)"
assert_close "$x" "$y" "$z" "103.50" "80.00" "100.50"

echo "== Parkour into turn =="
mc-rcon "fill 118 79 108 126 79 112 air" >/dev/null
mc-rcon "setblock 120 79 110 stone" >/dev/null
mc-rcon "setblock 123 79 110 stone" >/dev/null
mc-rcon "setblock 123 79 111 stone" >/dev/null
mc-rcon "tp CursorBot 120.5 80 110.5" >/dev/null
sleep 2
send_mcc "/goto 123 80 111"
sleep 6
send_mcc "/debug state"
sleep 1
read -r x y z <<< "$(extract_last_location)"
assert_close "$x" "$y" "$z" "123.50" "80.00" "111.50"

echo "All transition braking checks passed."
```

- [ ] **Step 2: Run the real-server regression script and verify it fails before tuning**

Run:

```bash
chmod +x tools/test-transition-braking.sh
dotnet build MinecraftClient.sln -c Release
bash tools/test-transition-braking.sh
```

Expected: FAIL on at least one scenario because the initial planner constants will still be slightly loose on real 1.21.11 physics.

- [ ] **Step 3: Tune the planner constants based on the live-server results**

```csharp
// MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs
private const double GroundSpeedThreshold = 0.025;
private const int MaxSimulationTicks = 14;
private const double FinalStopLead = 0.06;
private const double TurnBrakeLead = 0.10;
private const double AirReleaseLead = 0.14;

public static TransitionBrakingDecision Plan(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
{
    if (current.ExitTransition is PathTransitionType.ContinueStraight or PathTransitionType.PrepareJump)
        return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);

    double remaining = RemainingDistanceAlongSegment(current, pos);
    double coastStopDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: false);
    double hardBrakeDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: true);

    if (current.ExitTransition == PathTransitionType.Turn && remaining <= hardBrakeDistance + TurnBrakeLead)
        return TransitionBrakingDecision.Brake;

    if (remaining <= coastStopDistance + FinalStopLead)
        return TransitionBrakingDecision.Coast;

    return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);
}

public static bool ShouldReleaseForwardInAir(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics)
{
    if (current.ExitTransition is not (PathTransitionType.FinalStop or PathTransitionType.Turn or PathTransitionType.LandingRecovery))
        return false;

    double remaining = RemainingDistanceAlongSegment(current, pos);
    double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));

    return remaining <= forwardSpeed + AirReleaseLead;
}
```

- [ ] **Step 4: Re-run the local 1.21.11 regression script**

Run:

```bash
dotnet build MinecraftClient.sln -c Release
bash tools/test-transition-braking.sh
```

Expected: PASS with both scenarios landing within `0.05` blocks of the intended final center.

- [ ] **Step 5: Update the pathfinding research doc**

```md
<!-- docs/guide/pathfinding-research.md -->
## Transition-Aware Braking

MCC path execution now evaluates the next segment before finishing the current one.
The executor uses three exit styles:

- `ContinueStraight`: finish early and preserve sprint so the next segment consumes the current velocity.
- `Turn` / `FinalStop`: release `Forward` early, then optionally tap `Back` on ground when the predicted stop distance is larger than the remaining runway.
- `PrepareJump` / `LandingRecovery`: preserve takeoff speed into jumps, but allow airborne forward release when the next segment is a turn or final stop.

This deliberately differs from Baritone's default semantics.
Baritone treats many overshoots as success because the goal condition is usually "player feet entered the goal block".
MCC still uses block-goal semantics for path success, but the final segment controller now tries to settle near the target center on flat 1.21.11 terrain instead of accepting the old overshoot.
```

- [ ] **Step 6: Commit**

```bash
git add tools/test-transition-braking.sh \
  MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs \
  docs/guide/pathfinding-research.md
git commit -m "feat: validate and document transition-aware braking"
```

## Self-Review

### Spec coverage

- Transition-aware braking based on the next segment: covered by Task 2 and Task 3.
- Clear stale input on segment completion: covered by Task 1.
- Airborne forward release before a turn or final stop: covered by Task 3 and Task 4.
- Walk / ascend / descend / parkour execution changes: covered by Task 4.
- Real 1.21.11 validation: covered by Task 5.
- Documentation update: covered by Task 5.

### Placeholder scan

- No `TODO`, `TBD`, “similar to Task N”, or “write tests for the above” placeholders remain.
- Every task includes exact file paths, exact commands, and code blocks for the specific change.

### Type consistency

- Transition enum name is `PathTransitionType` everywhere.
- Builder name is `PathSegmentBuilder` everywhere.
- Planner name is `TransitionBrakingPlanner` everywhere.
- Planner output type is `TransitionBrakingDecision` everywhere.
