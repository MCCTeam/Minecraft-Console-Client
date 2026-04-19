# Sidewall Runup Precondition Implementation Plan

I'm using the writing-plans skill to create the implementation plan.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the remaining static-entry sidewall long-descend jumps plan through an explicit backward-then-forward runway setup, while preserving the current all-green `linear` behavior and keeping accepted executions at `replan_count=0` and `turn_stall_count=0`.

**Architecture:** Extend A* with a narrow `EntryPreparationState` keyed into the search node identity, so the pathfinder can distinguish “standing on the launch block unprepared” from “standing on the same block after completing a setup runway.” Keep the resulting path explicit by composing the setup out of ordinary `Traverse` segments, and gate `MoveSidewallParkour` on the prepared state only for the narrow profile that currently needs extra entry momentum.

**Tech Stack:** .NET 10 / C# 14, xUnit, MCC pathing core, `tools/test-parkour.py`, local `1.21.11-Vanilla` live harness

---

## Scope And Guardrails

- Phase 1 activation is intentionally narrow: static-entry `sidewall`, `yDelta == -1`, dominant distance `== 5`, no carry-in.
- Do not edit `MoveParkour` or any generic linear admissibility logic in this plan.
- Do not touch `SprintJumpTemplate` or `SidewallParkourController` in the first pass. If the new explicit runway path exposes a runtime issue later, stop and write a follow-up plan instead of silently expanding scope.
- The workspace is already dirty. Do not reset, discard, or overwrite unrelated changes. Save new docs under `2026-04-19-*` filenames instead of modifying the existing untracked `2026-04-18` sidewall plan.
- The verification gate for this plan is targeted pathing tests plus the live harness, not the full `MinecraftClient.Tests` suite, because the baseline still contains unrelated failures.

## File Structure

- Create: `MinecraftClient/Pathing/Core/EntryPreparationKind.cs`
  Responsibility: enum describing whether a node has no setup state or a sidewall runup state.
- Create: `MinecraftClient/Pathing/Core/EntryPreparationState.cs`
  Responsibility: immutable value object that records launch origin, dominant axis, required steps, and progress through backward/return phases.
- Modify: `MinecraftClient/Pathing/Core/PathNode.cs`
  Responsibility: store `EntryPreparationState` on each node.
- Modify: `MinecraftClient/Pathing/Core/CalculationContext.cs`
  Responsibility: expose the current node’s `EntryPreparationState` to move feasibility.
- Modify: `MinecraftClient/Pathing/Core/AStarPathFinder.cs`
  Responsibility: key nodes by position plus preparation state, seed and advance sidewall runup setup, and preserve explicit traverse segments in the planned path.
- Modify: `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
  Responsibility: classify when a sidewall profile requires setup and validate a prepared setup against launch origin and dominant axis.
- Modify: `MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs`
  Responsibility: reject the narrow long-descend static-entry sidewall profile unless the current node carries a matching prepared setup state.
- Modify: `MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs`
  Responsibility: direct admissibility guard for long-descend sidewall static entry.
- Modify: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
  Responsibility: assert the planner emits explicit setup traverses for the long-descend sidewall profile and does not inject setup into linear routes.
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`
  Responsibility: ensure the planned sidewall setup path still executes at `0 replan`.
- Use for verification only: `tools/test-parkour.py`
  Responsibility: live-harness acceptance, not production code changes.

### Task 1: Freeze The Required Planner Shape In Tests

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`

- [ ] **Step 1: Add a direct move test that proves long-descend sidewall static entry is not directly admissible**

```csharp
// MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs
[Theory]
[InlineData("sidewall-descend-gap5-dy-1-wo0", 5, 0)]
[InlineData("sidewall-descend-gap5-dy-1-wo1", 5, 1)]
public void Calculate_LongDescendStaticEntry_RejectsWithoutPreparedRunup(
    string scenarioId,
    int gap,
    int wallOffset)
{
    World world = SidewallParkourScenarioBuilder.BuildWorld(gap, deltaY: -1, wallOffset);
    var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
    var move = new MoveSidewallParkour(xOffset: -1, zOffset: gap, yDelta: -1);
    MoveResult result = default;

    move.Calculate(ctx, 100, 80, 100, ref result);

    Assert.True(result.IsImpossible, scenarioId);
}
```

- [ ] **Step 2: Add a planner regression that requires explicit setup traverses before the first sidewall jump**

```csharp
// MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
[Theory]
[InlineData("sidewall-descend-gap5-dy-1-wo0", 5, 0)]
[InlineData("sidewall-descend-gap5-dy-1-wo1", 5, 1)]
public void AStar_SidewallLongDescendStaticEntry_PrependsExplicitRunupTraverses(
    string scenarioId,
    int gap,
    int wallOffset)
{
    PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(
        scenarioId,
        gap,
        deltaY: -1,
        wallOffset);
    PathResult result = PathingScenarioRunner.PlanOnly(scenario);
    List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

    Assert.Equal(PathStatus.Success, result.Status);

    int firstParkourIndex = segments.FindIndex(segment => segment.MoveType == MoveType.Parkour);
    Assert.True(firstParkourIndex >= 4, string.Join('\n', segments));
    Assert.All(
        segments.Take(firstParkourIndex),
        segment => Assert.Equal(MoveType.Traverse, segment.MoveType));
    Assert.Equal(new Location(100.5, 80, 100.5), segments[firstParkourIndex - 1].End);
    Assert.Equal(ParkourProfile.Sidewall, segments[firstParkourIndex].ParkourProfile);
}
```

- [ ] **Step 3: Add a linear regression that proves no setup is injected into an already-green route**

```csharp
// MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
[Fact]
public void AStar_LinearFlatGap4_DoesNotInsertRunupSetupSegments()
{
    PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap4", gap: 4, deltaY: 0);
    PathResult result = PathingScenarioRunner.PlanOnly(scenario);
    List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

    Assert.Equal(PathStatus.Success, result.Status);
    Assert.NotEmpty(segments);
    Assert.Equal(MoveType.Parkour, segments[0].MoveType);
}
```

- [ ] **Step 4: Run the focused planner tests and verify they fail before implementation**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~MoveSidewallParkourTests|FullyQualifiedName~LivePathingRegressionTests" -v minimal
```

Expected:

- `Calculate_LongDescendStaticEntry_RejectsWithoutPreparedRunup` fails because the move still admits the jump directly.
- `AStar_SidewallLongDescendStaticEntry_PrependsExplicitRunupTraverses` fails because the planner still tries to jump from a plain origin node.

### Task 2: Add Search-State Types For Planner-Side Setup

**Files:**
- Create: `MinecraftClient/Pathing/Core/EntryPreparationKind.cs`
- Create: `MinecraftClient/Pathing/Core/EntryPreparationState.cs`
- Modify: `MinecraftClient/Pathing/Core/PathNode.cs`
- Modify: `MinecraftClient/Pathing/Core/CalculationContext.cs`

- [ ] **Step 1: Add the preparation-kind enum**

```csharp
// MinecraftClient/Pathing/Core/EntryPreparationKind.cs
namespace MinecraftClient.Pathing.Core
{
    public enum EntryPreparationKind
    {
        None = 0,
        SidewallRunup = 1
    }
}
```

- [ ] **Step 2: Add the immutable preparation-state value object**

```csharp
// MinecraftClient/Pathing/Core/EntryPreparationState.cs
namespace MinecraftClient.Pathing.Core
{
    public readonly record struct EntryPreparationState(
        EntryPreparationKind Kind,
        int OriginX,
        int OriginY,
        int OriginZ,
        int ForwardX,
        int ForwardZ,
        byte RequiredSteps,
        byte BackwardSteps,
        byte ReturnSteps)
    {
        public static EntryPreparationState None => default;

        public bool IsNone => Kind == EntryPreparationKind.None;

        public bool IsPrepared =>
            Kind != EntryPreparationKind.None &&
            BackwardSteps == RequiredSteps &&
            ReturnSteps == RequiredSteps;

        public EntryPreparationState AdvanceBackward() =>
            this with { BackwardSteps = (byte)(BackwardSteps + 1) };

        public EntryPreparationState AdvanceReturn() =>
            this with { ReturnSteps = (byte)(ReturnSteps + 1) };
    }
}
```

- [ ] **Step 3: Thread the state through path nodes and calculation context**

```csharp
// MinecraftClient/Pathing/Core/PathNode.cs
public EntryPreparationState EntryPreparation;

// MinecraftClient/Pathing/Core/CalculationContext.cs
public EntryPreparationState CurrentEntryPreparation { get; internal set; }
```

- [ ] **Step 4: Run a compile-only build to catch signature issues early**

Run:

```bash
dotnet build MinecraftClient.sln -c Debug
```

Expected:

- The build fails in `AStarPathFinder` and `MoveSidewallParkour` because the new state has not been wired into expansion logic yet.

### Task 3: Key A* By Position Plus Preparation State And Advance The Setup Path

**Files:**
- Modify: `MinecraftClient/Pathing/Core/AStarPathFinder.cs`

- [ ] **Step 1: Add a search-key type inside `AStarPathFinder` and stop deduplicating by packed position alone**

```csharp
// MinecraftClient/Pathing/Core/AStarPathFinder.cs
private readonly record struct NodeKey(long PackedPosition, EntryPreparationState EntryPreparation);

// replace
var nodeMap = new Dictionary<NodeKey, PathNode>(4096);

// replace start-node insertion
nodeMap[new NodeKey(startNode.PackedPosition, startNode.EntryPreparation)] = startNode;
```

- [ ] **Step 2: Push the current node state into the calculation context before each move expansion**

```csharp
// MinecraftClient/Pathing/Core/AStarPathFinder.cs
foreach (var move in _allMoves)
{
    ctx.PreviousMoveType = current.MoveUsed;
    ctx.CurrentEntryPreparation = current.EntryPreparation;
    moveResult.Cost = 0;
    move.Calculate(ctx, current.X, current.Y, current.Z, ref moveResult);
    // ...
}
```

- [ ] **Step 3: Add a helper that seeds, advances, or clears the setup state using only ordinary `Traverse` moves**

```csharp
// MinecraftClient/Pathing/Core/AStarPathFinder.cs
private EntryPreparationState ResolveEntryPreparation(PathNode current, IMove move, in MoveResult moveResult)
{
    EntryPreparationState advanced = AdvanceExistingPreparation(current, move, moveResult);
    if (!advanced.IsNone)
        return advanced;

    if (TryStartSidewallRunupPreparation(current, move, moveResult, out EntryPreparationState started))
        return started;

    return EntryPreparationState.None;
}

private static EntryPreparationState AdvanceExistingPreparation(PathNode current, IMove move, in MoveResult moveResult)
{
    EntryPreparationState state = current.EntryPreparation;
    if (state.IsNone)
        return EntryPreparationState.None;

    if (move.Type != MoveType.Traverse || moveResult.DestY != current.Y)
        return EntryPreparationState.None;

    int stepX = moveResult.DestX - current.X;
    int stepZ = moveResult.DestZ - current.Z;

    if (state.BackwardSteps < state.RequiredSteps &&
        stepX == -state.ForwardX &&
        stepZ == -state.ForwardZ)
    {
        return state.AdvanceBackward();
    }

    if (state.BackwardSteps == state.RequiredSteps &&
        state.ReturnSteps < state.RequiredSteps &&
        stepX == state.ForwardX &&
        stepZ == state.ForwardZ)
    {
        return state.AdvanceReturn();
    }

    return EntryPreparationState.None;
}
```

- [ ] **Step 4: Seed the setup state only when a one-block backward traverse matches a setup-required sidewall profile from the current origin**

```csharp
// MinecraftClient/Pathing/Core/AStarPathFinder.cs
private bool TryStartSidewallRunupPreparation(
    PathNode current,
    IMove move,
    in MoveResult moveResult,
    out EntryPreparationState state)
{
    state = EntryPreparationState.None;

    if (current.EntryPreparation.Kind != EntryPreparationKind.None ||
        move.Type != MoveType.Traverse ||
        moveResult.DestY != current.Y)
    {
        return false;
    }

    int stepX = moveResult.DestX - current.X;
    int stepZ = moveResult.DestZ - current.Z;

    foreach (MoveSidewallParkour sidewallMove in _allMoves.OfType<MoveSidewallParkour>())
    {
        if (!ParkourFeasibility.TryGetRequiredStaticEntryRunupSteps(
                current.MoveUsed,
                sidewallMove.XOffset,
                sidewallMove.ZOffset,
                sidewallMove.YDelta,
                out int requiredSteps))
        {
            continue;
        }

        ParkourFeasibility.GetSidewallAxes(
            sidewallMove.XOffset,
            sidewallMove.ZOffset,
            out int forwardX,
            out int forwardZ,
            out _,
            out _);

        if (stepX == -forwardX && stepZ == -forwardZ)
        {
            state = new EntryPreparationState(
                EntryPreparationKind.SidewallRunup,
                current.X,
                current.Y,
                current.Z,
                forwardX,
                forwardZ,
                (byte)requiredSteps,
                BackwardSteps: 1,
                ReturnSteps: 0);
            return true;
        }
    }

    return false;
}
```

- [ ] **Step 5: Attach the resolved state to the neighbor before node lookup and update the node-map key**

```csharp
// MinecraftClient/Pathing/Core/AStarPathFinder.cs
EntryPreparationState nextPreparation = ResolveEntryPreparation(current, move, moveResult);
var key = new NodeKey(PathNode.Pack(nx, ny, nz), nextPreparation);

if (nodeMap.TryGetValue(key, out var neighbor))
{
    // existing better-path update
    neighbor.EntryPreparation = nextPreparation;
}
else
{
    neighbor = new PathNode(nx, ny, nz)
    {
        GCost = tentativeG,
        HCost = goal.Heuristic(nx, ny, nz),
        Parent = current,
        MoveUsed = move.Type,
        ParkourProfile = moveResult.ParkourProfile,
        EntryPreparation = nextPreparation,
        IsOpen = true
    };
    nodeMap[key] = neighbor;
    openSet.Insert(neighbor);
}
```

- [ ] **Step 6: Run the focused planner tests again**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~MoveSidewallParkourTests|FullyQualifiedName~LivePathingRegressionTests" -v minimal
```

Expected:

- The compile-time errors are gone.
- The planner-shape test still fails until `MoveSidewallParkour` recognizes the prepared state.

### Task 4: Gate The Narrow Sidewall Profile On Prepared Setup

**Files:**
- Modify: `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`
- Modify: `MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs`

- [ ] **Step 1: Add a helper that classifies setup-required sidewall profiles**

```csharp
// MinecraftClient/Pathing/Moves/ParkourFeasibility.cs
public static bool TryGetRequiredStaticEntryRunupSteps(
    MoveType previousMoveType,
    int xOffset,
    int zOffset,
    int yDelta,
    out int requiredSteps)
{
    requiredSteps = 0;

    if (previousMoveType is MoveType.Parkour or MoveType.Descend)
        return false;

    int major = Math.Max(Math.Abs(xOffset), Math.Abs(zOffset));
    if (yDelta == -1 && major == 5)
    {
        requiredSteps = 2;
        return true;
    }

    return false;
}
```

- [ ] **Step 2: Add a helper that validates a prepared setup against the exact launch origin and dominant axis**

```csharp
// MinecraftClient/Pathing/Moves/ParkourFeasibility.cs
public static bool HasPreparedRunup(
    EntryPreparationState state,
    int x,
    int y,
    int z,
    int forwardX,
    int forwardZ,
    int requiredSteps)
{
    return state.Kind == EntryPreparationKind.SidewallRunup &&
        state.IsPrepared &&
        state.OriginX == x &&
        state.OriginY == y &&
        state.OriginZ == z &&
        state.ForwardX == forwardX &&
        state.ForwardZ == forwardZ &&
        state.RequiredSteps == requiredSteps;
}
```

- [ ] **Step 3: Expose `YDelta` from `MoveSidewallParkour` so the pathfinder can inspect sidewall candidates**

```csharp
// MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs
public int YDelta => _yDelta;
```

- [ ] **Step 4: Reject the narrow static-entry profile unless the current node carries a matching prepared setup**

```csharp
// MinecraftClient/Pathing/Moves/Impl/MoveSidewallParkour.cs
ParkourFeasibility.GetSidewallAxes(XOffset, ZOffset, out int forwardX, out int forwardZ, out int lateralX, out int lateralZ);

if (ParkourFeasibility.TryGetRequiredStaticEntryRunupSteps(
        ctx.PreviousMoveType,
        XOffset,
        ZOffset,
        _yDelta,
        out int requiredSteps))
{
    if (!ParkourFeasibility.HasPreparedRunup(
            ctx.CurrentEntryPreparation,
            x,
            y,
            z,
            forwardX,
            forwardZ,
            requiredSteps))
    {
        result.SetImpossible();
        return;
    }
}
else if (!ParkourFeasibility.HasDominantAxisRunUp(ctx, x, y, z, forwardX, forwardZ, XOffset, ZOffset, _yDelta))
{
    result.SetImpossible();
    return;
}
```

- [ ] **Step 5: Run the focused planner tests and verify they now pass**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~MoveSidewallParkourTests|FullyQualifiedName~LivePathingRegressionTests" -v minimal
```

Expected:

- `Calculate_LongDescendStaticEntry_RejectsWithoutPreparedRunup` passes.
- `AStar_SidewallLongDescendStaticEntry_PrependsExplicitRunupTraverses` passes.
- `AStar_LinearFlatGap4_DoesNotInsertRunupSetupSegments` passes.

### Task 5: Prove The New Path Still Executes At Zero Replan

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`

- [ ] **Step 1: Add a focused execution regression for the two long-descend sidewall profiles**

```csharp
// MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs
[Theory]
[InlineData("sidewall-descend-gap5-dy-1-wo0", 5, 0)]
[InlineData("sidewall-descend-gap5-dy-1-wo1", 5, 1)]
public void Tick_SidewallLongDescendRunupSetup_CompletesWithoutReplan(
    string scenarioId,
    int gap,
    int wallOffset)
{
    PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(
        scenarioId,
        gap,
        deltaY: -1,
        wallOffset);
    PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);
    Location goalLocation = new(scenario.Goal.X + 0.5, scenario.Goal.Y, scenario.Goal.Z + 0.5);

    Assert.True(
        result.Completed &&
        result.ReplanCount == 0 &&
        TemplateFootingHelper.IsFootprintInsideTargetBlock(result.FinalPosition, goalLocation),
        $"scenario={scenarioId} completed={result.Completed} replans={result.ReplanCount} final={result.FinalPosition}\n" +
        $"info={string.Join('\n', result.InfoLogs)}\ndebug={string.Join('\n', result.DebugLogs)}");
}
```

- [ ] **Step 2: Run the targeted execution tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~PathSegmentManagerTests|FullyQualifiedName~Linear" -v minimal
```

Expected:

- The two new sidewall long-descend execution tests pass at `ReplanCount == 0`.
- Existing linear execution guards remain green.

### Task 6: Run Live Harness Verification

**Files:** No production-file changes in this task.

- [ ] **Step 1: Run the split live sidewall and linear matrices**

Run:

```bash
python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter sidewall/descend
python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter sidewall/flat
python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter sidewall/ascend
python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter linear
```

Expected:

- `sidewall-descend-gap5-dy-1-wo0` and `sidewall-descend-gap5-dy-1-wo1` move from mismatch to match.
- No accepted linear case regresses.
- Accepted runs show `replan_count=0` and no turn-stall trace.

- [ ] **Step 2: Run the full matrix once after the split runs are green**

Run:

```bash
python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla
```

Expected:

- The matrix summary reflects sidewall alignment without creating new linear mismatches.

## Self-Review Checklist

- The implementation intentionally does not modify `SprintJumpTemplate`, `SidewallParkourController`, or generic `MoveParkour` logic.
- Every new planner state transition is driven by an explicit same-level `Traverse`, so the resulting path stays visible and inspectable.
- The node-map key change is the only search-core behavior broad enough to affect unrelated routes; that is why the linear planner and execution guards are part of the mandatory validation set.
