# Pathing Contract Metrics Harness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a pathing test harness that treats planner failure or any replan as a hard failure, and reports per-route plus per-segment tick budgets so slow steps are visible immediately.

**Architecture:** Split the problem into three layers. First, define scenario contracts that describe the expected planner output for each sterile route. Second, add execution telemetry that records actual segment durations and total route ticks without relying on fragile log scraping in unit tests. Third, reuse the same contracts and telemetry output in the live shell harness so unit tests and real-server runs speak the same language: planner contract, zero replan, total ticks, and which segment exceeded budget.

**Tech Stack:** C# 14 / .NET 10, xUnit, MCC pathing execution stack (`PathSegmentManager`, `PathExecutor`, templates), JSON contract files under `MinecraftClient.Tests/TestData`, Bash + Python 3 live harness helpers under `tools/`, local `1.21.11-Vanilla` server via `tools/mcc-env.sh`.

---

## Measurement Contract

- Accepted deterministic routes fail immediately on any of:
  - planner `Failed`
  - planner `Partial`
  - any executor `Replan #`
  - any `[PathExec] Segment ... FAILED`
  - total route ticks above budget
  - any segment ticks above budget
- Rejected routes fail immediately on any of:
  - planner `Success` or `Partial`
  - navigation starting at all
  - any replan attempt
- Timing uses two numbers per route and per segment:
  - `expectedTicks`: best known sterile baseline
  - `maxTicks`: enforced ceiling
- Initial `maxTicks` seeding rule:
  - `maxTicks = expectedTicks + max(2, ceil(expectedTicks * 0.20))`
- After the first bootstrap pass, keep `expectedTicks` fixed in JSON and tune `maxTicks` only where the measured deterministic baseline proves the generic 20% rule is too loose or too tight.

## File Structure

- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenario.cs`
  - immutable scenario definition: id, world builder, start, goal, initial yaw, execution cap
- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs`
  - sterile test worlds for short routes, jump combos, and long routes
- Create: `MinecraftClient.Tests/Pathing/Execution/Contracts/PathingPlannerContract.cs`
  - expected planner result and exact segment sequence
- Create: `MinecraftClient.Tests/Pathing/Execution/Contracts/PathingTimingBudget.cs`
  - route and segment tick budgets
- Create: `MinecraftClient.Tests/Pathing/Execution/Contracts/PathingContractStore.cs`
  - JSON loader for planner contracts and timing budgets
- Create: `MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`
  - expected move sequence per scenario
- Create: `MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`
  - expected ticks and max ticks per route and per segment
- Create: `MinecraftClient/Pathing/Execution/Telemetry/IPathExecutionObserver.cs`
  - execution observer API
- Create: `MinecraftClient/Pathing/Execution/Telemetry/PathExecutionLogObserver.cs`
  - machine-readable live telemetry lines for shell parsing
- Modify: `MinecraftClient/Pathing/Execution/PathExecutor.cs`
  - emit segment start, complete, fail, and per-segment elapsed ticks
- Modify: `MinecraftClient/Pathing/Execution/PathSegmentManager.cs`
  - emit navigation start, complete, replan start, replan success, replan failure, and total route ticks
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/RecordingPathExecutionObserver.cs`
  - in-memory capture of planner/executor events for assertions
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/PathingScenarioRunner.cs`
  - deterministic runner that plans, executes, and returns trace + logs
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/PathingContractAssert.cs`
  - compares actual plan/timing to JSON contracts and prints slow-step tables
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/PathingContractBootstrapWriter.cs`
  - emits ready-to-paste JSON fragments from observed planner/timing traces
- Create: `MinecraftClient.Tests/Pathing/Execution/PathingContractBootstrapTests.cs`
  - explicit bootstrap tests used only to seed contracts
- Create: `MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs`
  - exact planner contract assertions
- Create: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`
  - zero-replan and timing-budget assertions
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`
  - migrate existing manager smoke tests to the shared runner where useful
- Create: `tools/pathing_contract_report.py`
  - shell-side parser for planner contracts, timing budgets, and MCC telemetry lines
- Modify: `tools/test-pathing-jump-combos.sh`
  - call the report helper after each accepted route
- Modify: `tools/test-pathing-long-routes.sh`
  - call the report helper after each accepted route
- Modify: `docs/guide/pathfinding-research.md`
  - document planner vetoes, zero-replan rule, and timing metrics workflow

### Task 1: Create Contract Types And Seed One Known Scenario

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/Contracts/PathingPlannerContract.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/Contracts/PathingTimingBudget.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/Contracts/PathingContractStore.cs`
- Create: `MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`
- Create: `MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`
- Test: `MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs`

- [ ] **Step 1: Write the failing contract-loader test**

```csharp
using MinecraftClient.Pathing.Core;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathPlanningContractTests
{
    [Fact]
    public void Get_ManagerAcceptedAscendChain_LoadsExactPlannerContract()
    {
        PathingPlannerContract contract = PathingContractStore.GetPlanner("manager-accepted-ascend-chain");

        Assert.Equal(PathStatus.Success, contract.ExpectedStatus);
        Assert.Equal(6, contract.Segments.Length);
        Assert.Collection(contract.Segments,
            segment =>
            {
                Assert.Equal(MoveType.Diagonal, segment.MoveType);
                Assert.Equal(new PathingBlock(171, 80, 160), segment.StartBlock);
                Assert.Equal(new PathingBlock(172, 80, 161), segment.EndBlock);
            },
            segment =>
            {
                Assert.Equal(MoveType.Ascend, segment.MoveType);
                Assert.Equal(new PathingBlock(176, 82, 162), segment.StartBlock);
                Assert.Equal(new PathingBlock(177, 83, 162), segment.EndBlock);
            });
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.PathPlanningContractTests.Get_ManagerAcceptedAscendChain_LoadsExactPlannerContract -v minimal`

Expected: FAIL with a compile error because `PathingPlannerContract`, `PathingContractStore`, and `PathingBlock` do not exist yet.

- [ ] **Step 3: Implement the contract models, store, and the first JSON entries**

`MinecraftClient.Tests/Pathing/Execution/Contracts/PathingPlannerContract.cs`

```csharp
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Tests.Pathing.Execution;

internal sealed record PathingBlock(int X, int Y, int Z);

internal sealed record PathingPlannerSegmentContract
{
    public required MoveType MoveType { get; init; }
    public required PathingBlock StartBlock { get; init; }
    public required PathingBlock EndBlock { get; init; }
}

internal sealed record PathingPlannerContract
{
    public required string ScenarioId { get; init; }
    public required PathStatus ExpectedStatus { get; init; }
    public required PathingPlannerSegmentContract[] Segments { get; init; }
}

internal sealed record PathingSegmentTimingBudget
{
    public required MoveType MoveType { get; init; }
    public required int ExpectedTicks { get; init; }
    public required int MaxTicks { get; init; }
}

internal sealed record PathingTimingBudget
{
    public required string ScenarioId { get; init; }
    public required int ExpectedTotalTicks { get; init; }
    public required int MaxTotalTicks { get; init; }
    public required PathingSegmentTimingBudget[] Segments { get; init; }
}
```

`MinecraftClient.Tests/Pathing/Execution/Contracts/PathingContractStore.cs`

```csharp
using System.Text.Json;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class PathingContractStore
{
    private static readonly Lazy<IReadOnlyDictionary<string, PathingPlannerContract>> PlannerContracts = new(LoadPlanner);
    private static readonly Lazy<IReadOnlyDictionary<string, PathingTimingBudget>> TimingBudgets = new(LoadTiming);

    internal static PathingPlannerContract GetPlanner(string scenarioId) => PlannerContracts.Value[scenarioId];
    internal static PathingTimingBudget GetTiming(string scenarioId) => TimingBudgets.Value[scenarioId];

    private static IReadOnlyDictionary<string, PathingPlannerContract> LoadPlanner() =>
        Load<PathingPlannerContract>("pathing-planner-contracts.json");

    private static IReadOnlyDictionary<string, PathingTimingBudget> LoadTiming() =>
        Load<PathingTimingBudget>("pathing-timing-budgets.json");

    private static IReadOnlyDictionary<string, TContract> Load<TContract>(string fileName) where TContract : class
    {
        string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string path = Path.Combine(repoRoot, "MinecraftClient.Tests", "TestData", "Pathing", fileName);

        using FileStream stream = File.OpenRead(path);
        var contracts = JsonSerializer.Deserialize<TContract[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Failed to deserialize {path}");

        return contracts.ToDictionary(
            contract => (string)typeof(TContract).GetProperty("ScenarioId")!.GetValue(contract)!,
            StringComparer.Ordinal);
    }
}
```

`MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`

```json
[
  {
    "scenarioId": "manager-accepted-ascend-chain",
    "expectedStatus": "Success",
    "segments": [
      { "moveType": "Diagonal", "startBlock": { "x": 171, "y": 80, "z": 160 }, "endBlock": { "x": 172, "y": 80, "z": 161 } },
      { "moveType": "Diagonal", "startBlock": { "x": 172, "y": 80, "z": 161 }, "endBlock": { "x": 173, "y": 80, "z": 162 } },
      { "moveType": "Traverse", "startBlock": { "x": 173, "y": 80, "z": 162 }, "endBlock": { "x": 174, "y": 80, "z": 162 } },
      { "moveType": "Ascend", "startBlock": { "x": 174, "y": 80, "z": 162 }, "endBlock": { "x": 175, "y": 81, "z": 162 } },
      { "moveType": "Ascend", "startBlock": { "x": 175, "y": 81, "z": 162 }, "endBlock": { "x": 176, "y": 82, "z": 162 } },
      { "moveType": "Ascend", "startBlock": { "x": 176, "y": 82, "z": 162 }, "endBlock": { "x": 177, "y": 83, "z": 162 } }
    ]
  }
]
```

`MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`

```json
[
  {
    "scenarioId": "manager-accepted-ascend-chain",
    "expectedTotalTicks": 0,
    "maxTotalTicks": 0,
    "segments": [
      { "moveType": "Diagonal", "expectedTicks": 0, "maxTicks": 0 },
      { "moveType": "Ascend", "expectedTicks": 0, "maxTicks": 0 }
    ]
  }
]
```

Notes:
- `pathing-timing-budgets.json` is intentionally seeded with zeroes only for the one scaffold scenario. Task 3 replaces them with measured values before timing assertions start.
- Keep planner contracts and timing budgets separate so segment sequence can be locked before budgets are calibrated.

- [ ] **Step 4: Run the contract-loader test to verify it passes**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.PathPlanningContractTests.Get_ManagerAcceptedAscendChain_LoadsExactPlannerContract -v minimal`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/Contracts/PathingPlannerContract.cs \
        MinecraftClient.Tests/Pathing/Execution/Contracts/PathingContractStore.cs \
        MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json \
        MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json \
        MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs
git commit -m "test: add pathing contract store scaffold"
```

### Task 2: Add Execution Telemetry And A Deterministic Scenario Runner

**Files:**
- Create: `MinecraftClient/Pathing/Execution/Telemetry/IPathExecutionObserver.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathExecutor.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegmentManager.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenario.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/RecordingPathExecutionObserver.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/PathingScenarioRunner.cs`
- Test: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`

- [ ] **Step 1: Write the failing runner test**

```csharp
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathTimingContractTests
{
    [Fact]
    public void Run_ManagerAcceptedAscendChain_CapturesPerSegmentTicks_AndZeroReplan()
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get("manager-accepted-ascend-chain");

        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

        Assert.Equal(0, result.ReplanCount);
        Assert.True(result.Completed);
        Assert.Equal(6, result.SegmentRuns.Count);
        Assert.All(result.SegmentRuns, run => Assert.True(run.ElapsedTicks > 0));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.PathTimingContractTests.Run_ManagerAcceptedAscendChain_CapturesPerSegmentTicks_AndZeroReplan -v minimal`

Expected: FAIL with missing type errors for `PathingExecutionScenarioCatalog`, `PathingScenarioRunner`, and `PathingScenarioResult`.

- [ ] **Step 3: Implement the observer API, wire it into path execution, and add the first scenario runner**

`MinecraftClient/Pathing/Execution/Telemetry/IPathExecutionObserver.cs`

```csharp
using MinecraftClient.Mapping;

namespace MinecraftClient.Pathing.Execution.Telemetry;

public interface IPathExecutionObserver
{
    void OnNavigationStarted(IReadOnlyList<PathSegment> segments);
    void OnSegmentStarted(int segmentIndex, int totalSegments, PathSegment segment);
    void OnSegmentCompleted(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position);
    void OnSegmentFailed(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position);
    void OnNavigationCompleted(int totalTicks);
    void OnReplanStarted(int replanCount, Location position);
    void OnReplanSucceeded(int replanCount, IReadOnlyList<PathSegment> segments);
    void OnReplanFailed(int replanCount, Location position);
}
```

`MinecraftClient/Pathing/Execution/PathExecutor.cs`

```csharp
using MinecraftClient.Pathing.Execution.Telemetry;

private readonly IPathExecutionObserver? _observer;
private int _segmentTicks;
private int _totalTicks;
public int TotalTicks => _totalTicks;

public PathExecutor(List<PathSegment> segments, Action<string>? debugLog = null, IPathExecutionObserver? observer = null)
{
    _segments = segments;
    _currentIndex = 0;
    _debugLog = debugLog;
    _observer = observer;
    _observer?.OnNavigationStarted(segments);
    AdvanceToNextSegment();
}

public PathExecutorState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
{
    if (_currentTemplate is null)
    {
        input.Reset();
        return PathExecutorState.Complete;
    }

    _segmentTicks++;
    _totalTicks++;
    var state = _currentTemplate.Tick(pos, physics, input, world);

    switch (state)
    {
        case TemplateState.Complete:
            _observer?.OnSegmentCompleted(_currentIndex, _segments.Count, _segments[_currentIndex], _segmentTicks, pos);
            input.Reset();
            _currentIndex++;
            _segmentTicks = 0;
            if (_currentIndex >= _segments.Count)
            {
                _currentTemplate = null;
                return PathExecutorState.Complete;
            }
            AdvanceToNextSegment();
            return PathExecutorState.InProgress;

        case TemplateState.Failed:
            _observer?.OnSegmentFailed(_currentIndex, _segments.Count, _segments[_currentIndex], _segmentTicks, pos);
            input.Reset();
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
        _observer?.OnSegmentStarted(_currentIndex, _segments.Count, seg);
    }
    else
    {
        _currentTemplate = null;
    }
}
```

`MinecraftClient/Pathing/Execution/PathSegmentManager.cs`

```csharp
using MinecraftClient.Pathing.Execution.Telemetry;

private readonly IPathExecutionObserver? _observer;

public PathSegmentManager(Action<string>? debugLog = null, Action<string>? infoLog = null, IPathExecutionObserver? observer = null)
{
    _debugLog = debugLog;
    _infoLog = infoLog;
    _observer = observer;
}

public void StartNavigation(IGoal goal, PathResult result)
{
    _goal = goal;
    _replanCount = 0;
    var segments = PathSegmentBuilder.FromPath(result.Path);
    _executor = new PathExecutor(segments, _debugLog, _observer);
    _infoLog?.Invoke($"[PathMgr] Navigation started: {segments.Count} segments");
}

public void Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
{
    if (_executor is null)
        return;

    var state = _executor.Tick(pos, physics, input, world);

    switch (state)
    {
        case PathExecutorState.Complete:
            _observer?.OnNavigationCompleted(_executor.TotalTicks);
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

private void Replan(Location pos, World world)
{
    _replanCount++;
    _observer?.OnReplanStarted(_replanCount, pos);
    // existing logic...
    if (result.Status == PathStatus.Failed || result.Path.Count < 2)
    {
        _observer?.OnReplanFailed(_replanCount, pos);
        _executor = null;
        _goal = null;
        return;
    }

    var segments = PathSegmentBuilder.FromPath(result.Path);
    _observer?.OnReplanSucceeded(_replanCount, segments);
    _executor = new PathExecutor(segments, _debugLog, _observer);
}
```

`MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenario.cs`

```csharp
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;

namespace MinecraftClient.Tests.Pathing.Execution;

internal sealed record PathingExecutionScenario
{
    public required string Id { get; init; }
    public required Func<World> BuildWorld { get; init; }
    public required Location Start { get; init; }
    public required GoalBlock Goal { get; init; }
    public required float StartYaw { get; init; }
    public required int MaxExecutionTicks { get; init; }
}
```

`MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs`

```csharp
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class PathingExecutionScenarioCatalog
{
    internal static PathingExecutionScenario Get(string scenarioId) => scenarioId switch
    {
        "manager-accepted-ascend-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildManagerAcceptedAscendChain,
            Start = new Location(171.5, 80, 160.5),
            Goal = new GoalBlock(177, 83, 162),
            StartYaw = 315f,
            MaxExecutionTicks = 420
        },
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, null)
    };

    private static World BuildManagerAcceptedAscendChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 158, max: 180);
        FlatWorldTestBuilder.ClearBox(world, 170, 80, 160, 178, 85, 168);
        FlatWorldTestBuilder.SetSolid(world, 175, 80, 162);
        FlatWorldTestBuilder.SetSolid(world, 176, 81, 162);
        FlatWorldTestBuilder.SetSolid(world, 177, 82, 162);
        return world;
    }
}
```

`MinecraftClient.Tests/Pathing/Execution/Support/RecordingPathExecutionObserver.cs`

```csharp
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution.Telemetry;

namespace MinecraftClient.Tests.Pathing.Execution;

internal sealed record PathSegmentRun(int SegmentIndex, MoveType MoveType, int ElapsedTicks, Location Position);

internal sealed class RecordingPathExecutionObserver : IPathExecutionObserver
{
    internal List<PathSegmentRun> SegmentRuns { get; } = new();
    internal int ReplanCount { get; private set; }
    internal int TotalTicks { get; private set; }

    public void OnNavigationStarted(IReadOnlyList<PathSegment> segments) { }
    public void OnSegmentStarted(int segmentIndex, int totalSegments, PathSegment segment) { }

    public void OnSegmentCompleted(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position)
        => SegmentRuns.Add(new PathSegmentRun(segmentIndex, segment.MoveType, elapsedTicks, position));

    public void OnSegmentFailed(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position)
        => SegmentRuns.Add(new PathSegmentRun(segmentIndex, segment.MoveType, elapsedTicks, position));

    public void OnNavigationCompleted(int totalTicks) => TotalTicks = totalTicks;
    public void OnReplanStarted(int replanCount, Location position) => ReplanCount = replanCount;
    public void OnReplanSucceeded(int replanCount, IReadOnlyList<PathSegment> segments) { }
    public void OnReplanFailed(int replanCount, Location position) { }
}
```

`MinecraftClient.Tests/Pathing/Execution/Support/PathingScenarioRunner.cs`

```csharp
using System.Threading;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Physics;

namespace MinecraftClient.Tests.Pathing.Execution;

internal sealed record PathingScenarioResult(
    bool Completed,
    int ReplanCount,
    int TotalTicks,
    IReadOnlyList<PathSegmentRun> SegmentRuns,
    IReadOnlyList<string> DebugLogs,
    IReadOnlyList<string> InfoLogs,
    Location FinalPosition,
    PathResult PlanResult);

internal static class PathingScenarioRunner
{
    internal static PathResult PlanOnly(PathingExecutionScenario scenario)
    {
        World world = scenario.BuildWorld();
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var finder = new AStarPathFinder();

        return finder.Calculate(
            ctx,
            (int)Math.Floor(scenario.Start.X),
            (int)Math.Floor(scenario.Start.Y),
            (int)Math.Floor(scenario.Start.Z),
            scenario.Goal,
            CancellationToken.None,
            timeoutMs: 3000);
    }

    internal static PathingScenarioResult RunAccepted(PathingExecutionScenario scenario)
    {
        World world = scenario.BuildWorld();
        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var observer = new RecordingPathExecutionObserver();
        var manager = new PathSegmentManager(debugLogs.Add, infoLogs.Add, observer);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(scenario.Start, scenario.StartYaw);
        var input = new MovementInput();

        PathResult planResult = PlanOnly(scenario);

        manager.StartNavigation(scenario.Goal, planResult);

        int tick = 0;
        for (; tick < scenario.MaxExecutionTicks && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        return new PathingScenarioResult(
            Completed: !manager.IsNavigating,
            ReplanCount: manager.ReplanCount,
            TotalTicks: tick,
            SegmentRuns: observer.SegmentRuns,
            DebugLogs: debugLogs,
            InfoLogs: infoLogs,
            FinalPosition: new Location(physics.Position.X, physics.Position.Y, physics.Position.Z),
            PlanResult: planResult);
    }
}
```

- [ ] **Step 4: Run the runner test to verify it passes**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.PathTimingContractTests.Run_ManagerAcceptedAscendChain_CapturesPerSegmentTicks_AndZeroReplan -v minimal`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient/Pathing/Execution/Telemetry/IPathExecutionObserver.cs \
        MinecraftClient/Pathing/Execution/PathExecutor.cs \
        MinecraftClient/Pathing/Execution/PathSegmentManager.cs \
        MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenario.cs \
        MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs \
        MinecraftClient.Tests/Pathing/Execution/Support/RecordingPathExecutionObserver.cs \
        MinecraftClient.Tests/Pathing/Execution/Support/PathingScenarioRunner.cs \
        MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs
git commit -m "test: record path execution segment timings"
```

### Task 3: Lock Planner Vetoes And Short-Route Timing Budgets

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/PathingContractAssert.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/Support/PathingContractBootstrapWriter.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/PathingContractBootstrapTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs`
- Modify: `MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`
- Modify: `MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`

- [ ] **Step 1: Write bootstrap tests that print planner and timing JSON fragments for the known short routes**

```csharp
using Xunit.Abstractions;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static PathingExecutionScenario Get(string scenarioId) => scenarioId switch
{
    "same-move-ascend-staircase" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildSameMoveAscendStaircase,
        Start = new Location(340.5, 80, 340.5),
        Goal = new GoalBlock(345, 85, 340),
        StartYaw = 270f,
        MaxExecutionTicks = 420
    },
    "same-move-descend-staircase" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildSameMoveDescendStaircase,
        Start = new Location(362.5, 85, 360.5),
        Goal = new GoalBlock(367, 80, 360),
        StartYaw = 270f,
        MaxExecutionTicks = 420
    },
    "rejected-3x1-invalid-goal" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildRejectedThreeByOneInvalidGoal,
        Start = new Location(141.5, 80, 138.5),
        Goal = new GoalBlock(144, 81, 138),
        StartYaw = 270f,
        MaxExecutionTicks = 80
    },
    _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, null)
};

public sealed class PathingContractBootstrapTests
{
    private readonly ITestOutputHelper _output;

    public PathingContractBootstrapTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("same-move-ascend-staircase")]
    [InlineData("same-move-descend-staircase")]
    [InlineData("rejected-3x1-invalid-goal")]
    public void PrintShortRouteContractFragments(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);

        _output.WriteLine(PathingContractBootstrapWriter.WritePlannerFragment(scenarioId, planResult));
        if (planResult.Status == PathStatus.Success)
            _output.WriteLine(PathingContractBootstrapWriter.WriteTimingFragment(scenarioId, PathingScenarioRunner.RunAccepted(scenario)));
    }
}
```

- [ ] **Step 2: Run the bootstrap tests and capture the JSON fragments**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.PathingContractBootstrapTests.PrintShortRouteContractFragments -v minimal`

Expected:
- PASS
- output contains ready-to-paste JSON for:
  - `same-move-ascend-staircase`
  - `same-move-descend-staircase`
  - `rejected-3x1-invalid-goal`

- [ ] **Step 3: Paste the emitted fragments into the contract files, then write the enforcing assertions**

`MinecraftClient.Tests/Pathing/Execution/Support/PathingContractAssert.cs`

```csharp
using System.Text;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Core;
using Xunit;
using Xunit.Sdk;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class PathingContractAssert
{
    internal static void PlannerMatches(PathingPlannerContract contract, IReadOnlyList<PathSegment> segments, PathResult result)
    {
        if (result.Status != contract.ExpectedStatus)
            throw new XunitException($"planner status mismatch: expected {contract.ExpectedStatus}, got {result.Status}");

        if (segments.Count != contract.Segments.Length)
            throw new XunitException($"segment count mismatch: expected {contract.Segments.Length}, got {segments.Count}");

        for (int i = 0; i < segments.Count; i++)
        {
            PathSegment actual = segments[i];
            PathingPlannerSegmentContract expected = contract.Segments[i];

            Assert.Equal(expected.MoveType, actual.MoveType);
            Assert.Equal(expected.StartBlock, ToBlock(actual.Start));
            Assert.Equal(expected.EndBlock, ToBlock(actual.End));
        }
    }

    internal static void TimingMatches(PathingTimingBudget budget, PathingScenarioResult result)
    {
        if (!result.Completed)
            throw new XunitException("navigation did not complete");
        if (result.ReplanCount != 0)
            throw new XunitException($"expected 0 replans, saw {result.ReplanCount}\n{Format(result, budget)}");
        if (result.TotalTicks > budget.MaxTotalTicks)
            throw new XunitException($"route exceeded budget: actual={result.TotalTicks} max={budget.MaxTotalTicks}\n{Format(result, budget)}");
        if (result.SegmentRuns.Count != budget.Segments.Length)
            throw new XunitException($"segment timing count mismatch: actual={result.SegmentRuns.Count} expected={budget.Segments.Length}");

        for (int i = 0; i < budget.Segments.Length; i++)
        {
            if (result.SegmentRuns[i].ElapsedTicks > budget.Segments[i].MaxTicks)
                throw new XunitException($"segment {i} exceeded budget\n{Format(result, budget)}");
        }
    }

    private static string Format(PathingScenarioResult result, PathingTimingBudget budget)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"route actual={result.TotalTicks} expected={budget.ExpectedTotalTicks} max={budget.MaxTotalTicks}");
        for (int i = 0; i < result.SegmentRuns.Count; i++)
        {
            var actual = result.SegmentRuns[i];
            var expected = budget.Segments[i];
            sb.AppendLine($"seg[{i}] move={actual.MoveType} actual={actual.ElapsedTicks} expected={expected.ExpectedTicks} max={expected.MaxTicks}");
        }
        return sb.ToString();
    }

    private static PathingBlock ToBlock(Location location) =>
        new((int)Math.Floor(location.X), (int)Math.Floor(location.Y), (int)Math.Floor(location.Z));
}
```

`MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs`

```csharp
[Theory]
[InlineData("same-move-ascend-staircase")]
[InlineData("same-move-descend-staircase")]
[InlineData("rejected-3x1-invalid-goal")]
public void Scenario_PlannerMatchesContract(string scenarioId)
{
    PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
    PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);
    PathingPlannerContract contract = PathingContractStore.GetPlanner(scenarioId);

    PathingContractAssert.PlannerMatches(contract, PathSegmentBuilder.FromPath(planResult.Path), planResult);
}
```

`MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`

```csharp
[Theory]
[InlineData("same-move-ascend-staircase")]
[InlineData("same-move-descend-staircase")]
public void Scenario_ExecutionStaysWithinTimingBudget(string scenarioId)
{
    PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
    PathingTimingBudget budget = PathingContractStore.GetTiming(scenarioId);
    PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

    PathingContractAssert.TimingMatches(budget, result);
}
```

- [ ] **Step 4: Run the short-route planner and timing tests**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution.PathPlanningContractTests|FullyQualifiedName~Pathing.Execution.PathTimingContractTests" -v minimal`

Expected:
- planner tests PASS for the two accepted short routes and the direct planner rejection
- timing tests PASS for ascend and descend staircase

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/Support/PathingContractAssert.cs \
        MinecraftClient.Tests/Pathing/Execution/Support/PathingContractBootstrapWriter.cs \
        MinecraftClient.Tests/Pathing/Execution/PathingContractBootstrapTests.cs \
        MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs \
        MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json \
        MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json \
        MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs \
        MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs
git commit -m "test: lock short route planner and timing contracts"
```

### Task 4: Bootstrap And Lock Complex Jump-Combo Contracts

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs`
- Modify: `MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`
- Modify: `MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathingContractBootstrapTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`

- [ ] **Step 1: Add the jump-combo sterile worlds and bootstrap coverage**

`MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs`

```csharp
internal static PathingExecutionScenario Get(string scenarioId) => scenarioId switch
{
    "repeated-cardinal-parkour-chain" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildRepeatedCardinalParkourChain,
        Start = new Location(580.5, 80, 580.5),
        Goal = new GoalBlock(588, 80, 580),
        StartYaw = 270f,
        MaxExecutionTicks = 420
    },
    "repeated-diagonal-parkour-chain" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildRepeatedDiagonalParkourChain,
        Start = new Location(600.5, 80, 600.5),
        Goal = new GoalBlock(606, 80, 606),
        StartYaw = 315f,
        MaxExecutionTicks = 420
    },
    "obstructed-parkour-l-turns" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildObstructedParkourLTurns,
        Start = new Location(620.5, 80, 620.5),
        Goal = new GoalBlock(626, 80, 622),
        StartYaw = 270f,
        MaxExecutionTicks = 420
    },
    "vertical-jump-mix" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildVerticalJumpMix,
        Start = new Location(640.5, 80, 620.5),
        Goal = new GoalBlock(648, 80, 620),
        StartYaw = 270f,
        MaxExecutionTicks = 420
    },
    "diagonal-vertical-mix" => new PathingExecutionScenario
    {
        Id = scenarioId,
        BuildWorld = BuildDiagonalVerticalMix,
        Start = new Location(680.5, 80, 620.5),
        Goal = new GoalBlock(684, 80, 624),
        StartYaw = 315f,
        MaxExecutionTicks = 420
    },
    _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, null)
};
```

Add world builders that mirror the live harness coordinates exactly:

```csharp
private static World BuildRepeatedCardinalParkourChain()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 578, max: 590);
    FlatWorldTestBuilder.ClearBox(world, 578, 79, 578, 590, 90, 582);
    FlatWorldTestBuilder.SetSolid(world, 580, 79, 580);
    FlatWorldTestBuilder.SetSolid(world, 582, 79, 580);
    FlatWorldTestBuilder.SetSolid(world, 584, 79, 580);
    FlatWorldTestBuilder.SetSolid(world, 586, 79, 580);
    FlatWorldTestBuilder.SetSolid(world, 588, 79, 580);
    return world;
}

private static World BuildRepeatedDiagonalParkourChain()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 598, max: 608);
    FlatWorldTestBuilder.ClearBox(world, 598, 79, 598, 608, 90, 608);
    FlatWorldTestBuilder.SetSolid(world, 600, 79, 600);
    FlatWorldTestBuilder.SetSolid(world, 602, 79, 602);
    FlatWorldTestBuilder.SetSolid(world, 604, 79, 604);
    FlatWorldTestBuilder.SetSolid(world, 606, 79, 606);
    return world;
}
```

Also port the exact geometry from:
- `tools/test-pathing-jump-combos.sh:264`
- `tools/test-pathing-jump-combos.sh:275`
- `tools/test-pathing-jump-combos.sh:285`

Update bootstrap coverage:

```csharp
[Theory]
[InlineData("repeated-cardinal-parkour-chain")]
[InlineData("repeated-diagonal-parkour-chain")]
[InlineData("obstructed-parkour-l-turns")]
[InlineData("vertical-jump-mix")]
[InlineData("diagonal-vertical-mix")]
public void PrintJumpComboContractFragments(string scenarioId)
{
    PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
    PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);

    _output.WriteLine(PathingContractBootstrapWriter.WritePlannerFragment(scenarioId, planResult));
    _output.WriteLine(PathingContractBootstrapWriter.WriteTimingFragment(scenarioId, PathingScenarioRunner.RunAccepted(scenario)));
}
```

- [ ] **Step 2: Run the bootstrap tests and paste the emitted planner contracts and timing budgets**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.PathingContractBootstrapTests.PrintJumpComboContractFragments -v minimal`

Expected:
- PASS
- output contains planner JSON and timing JSON for all five jump-combo scenarios

- [ ] **Step 3: Write enforcing theories for the jump-combo planner and timing contracts**

```csharp
[Theory]
[InlineData("repeated-cardinal-parkour-chain")]
[InlineData("repeated-diagonal-parkour-chain")]
[InlineData("obstructed-parkour-l-turns")]
[InlineData("vertical-jump-mix")]
[InlineData("diagonal-vertical-mix")]
public void JumpCombo_PlannerMatchesContract(string scenarioId)
{
    PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
    PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);
    PathingContractAssert.PlannerMatches(
        PathingContractStore.GetPlanner(scenarioId),
        PathSegmentBuilder.FromPath(planResult.Path),
        planResult);
}

[Theory]
[InlineData("repeated-cardinal-parkour-chain")]
[InlineData("repeated-diagonal-parkour-chain")]
[InlineData("obstructed-parkour-l-turns")]
[InlineData("vertical-jump-mix")]
[InlineData("diagonal-vertical-mix")]
public void JumpCombo_ExecutionStaysWithinBudget(string scenarioId)
{
    PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
    PathingTimingBudget budget = PathingContractStore.GetTiming(scenarioId);
    PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

    PathingContractAssert.TimingMatches(budget, result);
}
```

- [ ] **Step 4: Run the jump-combo contract suite**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution.JumpCombo_" -v minimal`

Expected:
- currently this suite may FAIL before later implementation work
- failure output must now show exactly which segment exceeded budget or triggered replan

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs \
        MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json \
        MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json \
        MinecraftClient.Tests/Pathing/Execution/PathingContractBootstrapTests.cs \
        MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs \
        MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs
git commit -m "test: add jump combo planner and timing contracts"
```

### Task 5: Bootstrap And Lock Long-Route Contracts

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs`
- Modify: `MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`
- Modify: `MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathingContractBootstrapTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathPlanningContractTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`

- [ ] **Step 1: Port the long-route geometry and bootstrap the planner/timing JSON**

Add scenario ids that mirror the shell suite exactly:

```csharp
"same-move-straight-traverse-chain",
"same-move-diagonal-chain",
"same-move-ascend-staircase",
"same-move-descend-staircase",
"same-move-aligned-parkour-chain",
"mixed-traverse-turn-parkour-turn-traverse",
"mixed-diagonal-ascend-traverse-descend",
"mixed-traverse-ascend-parkour-descend",
"turn-density-alternating-traverse-diagonal-chain",
"speed-carry-repeated-traverse-ascend",
"speed-carry-repeated-traverse-descend",
"speed-carry-repeated-traverse-parkour"
```

Mirror the shell layouts from:
- `tools/test-pathing-long-routes.sh:64`
- `tools/test-pathing-long-routes.sh:88`
- `tools/test-pathing-long-routes.sh:134`
- `tools/test-pathing-long-routes.sh:171`

Update bootstrap coverage:

```csharp
[Theory]
[InlineData("same-move-straight-traverse-chain")]
[InlineData("same-move-diagonal-chain")]
[InlineData("same-move-ascend-staircase")]
[InlineData("same-move-descend-staircase")]
[InlineData("same-move-aligned-parkour-chain")]
[InlineData("mixed-traverse-turn-parkour-turn-traverse")]
[InlineData("mixed-diagonal-ascend-traverse-descend")]
[InlineData("mixed-traverse-ascend-parkour-descend")]
[InlineData("turn-density-alternating-traverse-diagonal-chain")]
[InlineData("speed-carry-repeated-traverse-ascend")]
[InlineData("speed-carry-repeated-traverse-descend")]
[InlineData("speed-carry-repeated-traverse-parkour")]
public void PrintLongRouteContractFragments(string scenarioId)
{
    PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
    PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);

    _output.WriteLine(PathingContractBootstrapWriter.WritePlannerFragment(scenarioId, planResult));
    _output.WriteLine(PathingContractBootstrapWriter.WriteTimingFragment(scenarioId, PathingScenarioRunner.RunAccepted(scenario)));
}
```

- [ ] **Step 2: Run the bootstrap tests and paste the long-route JSON**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.PathingContractBootstrapTests.PrintLongRouteContractFragments -v minimal`

Expected:
- PASS
- output contains planner and timing fragments for all long-route scenarios

- [ ] **Step 3: Add enforcing theories for the long-route contracts**

```csharp
[Theory]
[InlineData("same-move-straight-traverse-chain")]
[InlineData("same-move-diagonal-chain")]
[InlineData("same-move-ascend-staircase")]
[InlineData("same-move-descend-staircase")]
[InlineData("same-move-aligned-parkour-chain")]
[InlineData("mixed-traverse-turn-parkour-turn-traverse")]
[InlineData("mixed-diagonal-ascend-traverse-descend")]
[InlineData("mixed-traverse-ascend-parkour-descend")]
[InlineData("turn-density-alternating-traverse-diagonal-chain")]
[InlineData("speed-carry-repeated-traverse-ascend")]
[InlineData("speed-carry-repeated-traverse-descend")]
[InlineData("speed-carry-repeated-traverse-parkour")]
public void LongRoute_ExecutionStaysWithinBudget(string scenarioId)
{
    PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
    PathingTimingBudget budget = PathingContractStore.GetTiming(scenarioId);
    PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

    PathingContractAssert.TimingMatches(budget, result);
}
```

- [ ] **Step 4: Run the long-route timing suite**

Run: `dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.LongRoute_ -v minimal`

Expected:
- initially some cases may FAIL
- every failure message must identify the route total and the exact slow segment index/move

- [ ] **Step 5: Commit**

```bash
git add MinecraftClient.Tests/Pathing/Execution/Scenarios/PathingExecutionScenarioCatalog.cs \
        MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json \
        MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json \
        MinecraftClient.Tests/Pathing/Execution/PathingContractBootstrapTests.cs \
        MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs
git commit -m "test: add long route timing contracts"
```

### Task 6: Expose The Same Metrics In The Live Harness

**Files:**
- Create: `MinecraftClient/Pathing/Execution/Telemetry/PathExecutionLogObserver.cs`
- Modify: `MinecraftClient/McClient.cs`
- Modify: `MinecraftClient/Resources/Translations/Translations.resx`
- Modify: `MinecraftClient/Resources/Translations/Translations.Designer.cs`
- Create: `tools/pathing_contract_report.py`
- Create: `tools/tests/test_pathing_contract_report.py`
- Modify: `tools/test-pathing-jump-combos.sh`
- Modify: `tools/test-pathing-long-routes.sh`

- [ ] **Step 1: Write the failing live-report parser test against a saved log slice**

Create a tiny fixture log under `tools/testdata/pathing-contract-report.sample.log` and a Python test:

```python
from pathing_contract_report import parse_metrics

def test_parse_metrics_reads_route_and_segment_ticks(tmp_path):
    log = tmp_path / "sample.log"
    log.write_text(
        "[PathMetric] routeStart segments=4\n"
        "[PathMetric] segmentComplete index=0 move=Parkour ticks=17\n"
        "[PathMetric] segmentComplete index=1 move=Parkour ticks=16\n"
        "[PathMetric] routeComplete totalTicks=70 replans=0\n",
        encoding="utf-8",
    )

    report = parse_metrics(log.read_text(encoding="utf-8"))

    assert report.total_ticks == 70
    assert [segment.ticks for segment in report.segments] == [17, 16]
```

- [ ] **Step 2: Run the parser test to verify it fails**

Run: `python3 -m pytest tools/tests/test_pathing_contract_report.py -q`

Expected: FAIL because `pathing_contract_report.py` does not exist yet.

- [ ] **Step 3: Implement a machine-readable log observer and the shell report helper**

`MinecraftClient/Pathing/Execution/Telemetry/PathExecutionLogObserver.cs`

```csharp
using MinecraftClient.Mapping;
using MinecraftClient.Resources;

namespace MinecraftClient.Pathing.Execution.Telemetry;

public sealed class PathExecutionLogObserver : IPathExecutionObserver
{
    private readonly Action<string>? _debug;
    private int _routeTicks;

    public PathExecutionLogObserver(Action<string>? debug) => _debug = debug;

    public void OnNavigationStarted(IReadOnlyList<PathSegment> segments)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_route_start, segments.Count));

    public void OnSegmentStarted(int segmentIndex, int totalSegments, PathSegment segment)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_segment_start,
            segmentIndex, totalSegments, segment.MoveType, segment.ExitTransition));

    public void OnSegmentCompleted(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_segment_complete,
            segmentIndex, totalSegments, segment.MoveType, elapsedTicks, position.X, position.Y, position.Z));

    public void OnSegmentFailed(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_segment_failed,
            segmentIndex, totalSegments, segment.MoveType, elapsedTicks, position.X, position.Y, position.Z));

    public void OnNavigationCompleted(int totalTicks)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_route_complete, totalTicks));

    public void OnReplanStarted(int replanCount, Location position)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_replan_start,
            replanCount, position.X, position.Y, position.Z));

    public void OnReplanSucceeded(int replanCount, IReadOnlyList<PathSegment> segments)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_replan_success, replanCount, segments.Count));

    public void OnReplanFailed(int replanCount, Location position)
        => _debug?.Invoke(string.Format(Translations.pathing_metric_replan_failed,
            replanCount, position.X, position.Y, position.Z));
}
```

Add the matching translation entries so the new user-visible log lines stay inside the localization system:

```xml
<data name="pathing.metric.route_start" xml:space="preserve"><value>[PathMetric] routeStart segments={0}</value></data>
<data name="pathing.metric.segment_start" xml:space="preserve"><value>[PathMetric] segmentStart index={0} total={1} move={2} transition={3}</value></data>
<data name="pathing.metric.segment_complete" xml:space="preserve"><value>[PathMetric] segmentComplete index={0} total={1} move={2} ticks={3} x={4:F2} y={5:F2} z={6:F2}</value></data>
<data name="pathing.metric.segment_failed" xml:space="preserve"><value>[PathMetric] segmentFailed index={0} total={1} move={2} ticks={3} x={4:F2} y={5:F2} z={6:F2}</value></data>
<data name="pathing.metric.route_complete" xml:space="preserve"><value>[PathMetric] routeComplete totalTicks={0}</value></data>
<data name="pathing.metric.replan_start" xml:space="preserve"><value>[PathMetric] replanStart count={0} x={1:F2} y={2:F2} z={3:F2}</value></data>
<data name="pathing.metric.replan_success" xml:space="preserve"><value>[PathMetric] replanSuccess count={0} segments={1}</value></data>
<data name="pathing.metric.replan_failed" xml:space="preserve"><value>[PathMetric] replanFailed count={0} x={1:F2} y={2:F2} z={3:F2}</value></data>
```

`MinecraftClient/McClient.cs`

```csharp
pathSegmentManager = new Pathing.Execution.PathSegmentManager(
    debugLog: msg => Log.Debug(msg),
    infoLog: msg => Log.Info(msg),
    observer: new Pathing.Execution.Telemetry.PathExecutionLogObserver(msg => Log.Debug(msg)));
```

`tools/pathing_contract_report.py`

```python
from __future__ import annotations

import json
import math
import pathlib
import re
from dataclasses import dataclass

SEGMENT_RE = re.compile(r"\[PathMetric\] segmentComplete index=(?P<index>\d+) total=(?P<total>\d+) move=(?P<move>\w+) ticks=(?P<ticks>\d+)")
ROUTE_RE = re.compile(r"\[PathMetric\] routeComplete totalTicks=(?P<ticks>\d+)")
PLAN_RE = re.compile(r"\[Navigate\]\s+seg\[(?P<index>\d+)\] = (?P<move>\w+): \((?P<x>-?\d+),(?P<y>-?\d+),(?P<z>-?\d+)\)")

@dataclass
class SegmentMetric:
    index: int
    move: str
    ticks: int

def load_json(path: pathlib.Path):
    return {entry["scenarioId"]: entry for entry in json.loads(path.read_text(encoding="utf-8"))}

def parse_metrics(text: str):
    segments = [SegmentMetric(int(m["index"]), m["move"], int(m["ticks"])) for m in SEGMENT_RE.finditer(text)]
    route_match = ROUTE_RE.search(text)
    total_ticks = int(route_match["ticks"]) if route_match else None
    planned = [(int(m["index"]), m["move"], (int(m["x"]), int(m["y"]), int(m["z"]))) for m in PLAN_RE.finditer(text)]
    return {"segments": segments, "totalTicks": total_ticks, "planned": planned}

def main() -> int:
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--scenario-id", required=True)
    parser.add_argument("--log-file", required=True)
    parser.add_argument("--from-line", type=int, required=True)
    parser.add_argument("--planner-contracts", required=True)
    parser.add_argument("--timing-budgets", required=True)
    args = parser.parse_args()

    log_path = pathlib.Path(args.log_file)
    text = "\n".join(log_path.read_text(encoding="utf-8", errors="ignore").splitlines()[args.from_line:])
    planner = load_json(pathlib.Path(args.planner_contracts))[args.scenario_id]
    timing = load_json(pathlib.Path(args.timing_budgets))[args.scenario_id]
    actual = parse_metrics(text)

    if actual["totalTicks"] is None:
        raise SystemExit("Missing [PathMetric] routeComplete line")
    if actual["totalTicks"] > timing["maxTotalTicks"]:
        raise SystemExit(f"Route exceeded budget: actual={actual['totalTicks']} max={timing['maxTotalTicks']}")

    for expected, actual_segment in zip(timing["segments"], actual["segments"], strict=True):
        if actual_segment.ticks > expected["maxTicks"]:
            raise SystemExit(
                f"Segment {actual_segment.index} slow: move={actual_segment.move} actual={actual_segment.ticks} max={expected['maxTicks']}"
            )

    print(f"Route {args.scenario_id}: actual={actual['totalTicks']} expected={timing['expectedTotalTicks']} max={timing['maxTotalTicks']}")
    for expected, actual_segment in zip(timing["segments"], actual["segments"], strict=True):
        delta = actual_segment.ticks - expected["expectedTicks"]
        print(
            f"  seg[{actual_segment.index}] move={actual_segment.move} actual={actual_segment.ticks} "
            f"expected={expected['expectedTicks']} max={expected['maxTicks']} delta={delta:+d}"
        )
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 4: Integrate the report helper into both live scripts**

Add to `tools/test-pathing-jump-combos.sh` and `tools/test-pathing-long-routes.sh` inside `run_accepted_route()` after `assert_no_replans_since`:

```bash
python3 "$REPO_ROOT/tools/pathing_contract_report.py" \
    --scenario-id "$scenario_id" \
    --log-file "$LOG" \
    --from-line "$start_line" \
    --planner-contracts "$REPO_ROOT/MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json" \
    --timing-budgets "$REPO_ROOT/MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json"
```

Change the route helper signature so each case passes both a display label and the stable contract id:

```bash
run_accepted_route() {
    local scenario_id="$1"
    local label="$2"
    local start_x="$3"
    local start_y="$4"
    local start_z="$5"
    local goal_x="$6"
    local goal_y="$7"
    local goal_z="$8"
    local timeout="${9:-45}"
    # existing body...
}
```

- [ ] **Step 5: Run the parser tests and both live suites**

Run:

```bash
python3 -m pytest tools/tests/test_pathing_contract_report.py -q
source tools/mcc-env.sh && bash tools/test-pathing-jump-combos.sh 1.21.11-Vanilla
source tools/mcc-env.sh && bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Expected:
- parser test PASS
- live harness now prints route totals and per-segment slow-step tables
- accepted routes fail on planner mismatch, replan, or budget overrun

- [ ] **Step 6: Commit**

```bash
git add MinecraftClient/Pathing/Execution/Telemetry/PathExecutionLogObserver.cs \
        MinecraftClient/McClient.cs \
        MinecraftClient/Resources/Translations/Translations.resx \
        MinecraftClient/Resources/Translations/Translations.Designer.cs \
        tools/pathing_contract_report.py \
        tools/test-pathing-jump-combos.sh \
        tools/test-pathing-long-routes.sh
git commit -m "test: surface path timing contracts in live harness"
```

### Task 7: Document The Workflow And Final Verification

**Files:**
- Modify: `docs/guide/pathfinding-research.md`

- [ ] **Step 1: Add a short contract section to the docs**

```md
### Deterministic pathing contract

Accepted sterile routes must satisfy all of the following:

- planner result is `Success`
- planner result is not `Partial`
- navigation completes with `0 replan`
- every executed segment stays within its checked-in tick budget
- total route ticks stay within the checked-in route budget

Rejected routes must fail during planning and must never start navigation.

The authoritative contract files are:

- `MinecraftClient.Tests/TestData/Pathing/pathing-planner-contracts.json`
- `MinecraftClient.Tests/TestData/Pathing/pathing-timing-budgets.json`
```

- [ ] **Step 2: Run the full focused verification set**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution" -v minimal
source tools/mcc-env.sh && bash tools/test-pathing-jump-combos.sh 1.21.11-Vanilla
source tools/mcc-env.sh && bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Expected:
- xUnit pathing suite passes for the currently fixed scenarios
- live suites print per-route and per-segment timing metrics
- any remaining pathing regressions now fail with explicit slow-step or replan diagnostics

- [ ] **Step 3: Commit**

```bash
git add docs/guide/pathfinding-research.md
git commit -m "docs: document pathing contract metrics workflow"
```

## Self-Review

- Spec coverage:
  - planner failure and partial path are hard vetoes: covered in Tasks 3, 4, 5, and 6
  - zero replan for sterile accepted routes: covered in Tasks 2, 3, 4, 5, and 6
  - total route timing constraint: covered in Tasks 3, 4, 5, and 6
  - per-step timing visibility: covered in `PathingContractAssert` and `tools/pathing_contract_report.py`
  - ability to identify which action is slow: covered by segment-level contract assertions and live-shell report output
- Placeholder scan:
  - no `TODO`, `TBD`, or “similar to task N” references remain
  - the only “measure then paste” flow is explicit bootstrap output, not an unspecified placeholder
- Type consistency:
  - shared names are fixed across tasks: `PathingExecutionScenario`, `PathingScenarioResult`, `PathingPlannerContract`, `PathingTimingBudget`, `PathingContractStore`, `PathingContractAssert`

Plan complete and saved to `docs/superpowers/plans/2026-04-13-pathing-contract-metrics-harness.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
