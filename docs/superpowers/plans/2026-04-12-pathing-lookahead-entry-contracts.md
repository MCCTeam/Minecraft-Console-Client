# Pathing Lookahead Entry Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade MCC path execution from coarse next-segment braking to explicit entry contracts and short-horizon input selection so turns, jump takeoffs, landings, and final stops converge reliably on 1.21.11 without overshoot.

**Architecture:** Keep A* node expansion and move admissibility mostly unchanged. Extend `PathSegment` with quantitative transition hints derived from the next one or two segments, teach the braking planner to score candidate inputs against those hints with cloned `PlayerPhysics` simulations, and update grounded and airborne templates to hand off only when the next action's entry contract is actually satisfied.

**Tech Stack:** C# 14 / .NET 10, MCC `PlayerPhysics`, xUnit deterministic simulation tests in `MinecraftClient.Tests`, local MCC harness via `tools/mcc-env.sh`, `mcc-build`, `mcc-debug`, `mcc-cmd`, and a shared local 1.21.11 server.

---

## Execution Context

This plan starts from the current repository state, not the older "transition braking from scratch" plan. The test project, `PathTransitionType`, `TransitionBrakingPlanner`, convergence tests, and `tools/test-transition-braking.sh` already exist.

This plan is the follow-on slice for the later requirement from the broken conversation:

- planner and executor should anticipate whether the next action continues momentum, requires a turn, or requires a jump takeoff
- braking may begin on the previous segment
- airborne forward release is valid and should be planned, not guessed
- "precision" means satisfying the next action's entry conditions, not snapping to exact block center

## Scope

In scope:

- add explicit quantitative transition hints to `PathSegment`
- let the execution layer see beyond a coarse `Turn` / `PrepareJump` enum
- replace threshold-only braking decisions with short-horizon candidate simulation
- improve walk, descend, and sprint-jump handoff behavior
- update the local 1.21.11 regression harness to the current `mcc-dev-workflow`

Out of scope:

- changing A* heuristics or move costs unrelated to transition control
- adding a general-purpose "teleport to center" or velocity-zeroing cheat
- expanding the move catalog beyond current traverse / ascend / descend / parkour behavior

## File Structure

### New files

- `MinecraftClient/Pathing/Execution/PathTransitionHints.cs`
  Immutable quantitative exit contract for one segment: desired heading, minimum and maximum exit speed, stability requirements, and short planning horizon.
- `MinecraftClient/Pathing/Execution/TransitionInputProfile.cs`
  Named candidate inputs for the planner to score, such as carry, coast, brake, airborne hold, and airborne release.
- `MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs`
  Clones `PlayerPhysics`, simulates candidate inputs for a small horizon, and scores them against `PathTransitionHints`.
- `MinecraftClient.Tests/Pathing/Execution/PathTransitionHintsTests.cs`
  Verifies the segment builder derives correct hints for straight carry, turn entry, final stop, and prepare-jump cases.
- `MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs`
  Verifies candidate scoring chooses carry, coast, brake, or airborne release in representative scenarios.

### Modified files

- `MinecraftClient/Pathing/Execution/PathSegment.cs`
  Carry the quantitative exit hints alongside `ExitTransition`.
- `MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs`
  Compute hints from `current`, `next`, and `nextNext` segments.
- `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
  Use the lookahead evaluator instead of only distance thresholds.
- `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`
  Add heading-readiness helpers and any small shared utilities needed by the evaluator.
- `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
  Complete only when the current segment satisfies its exit hints.
- `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
  Stop treating "near segment end" as sufficient when the next action needs a slow turn or jump-ready takeoff.
- `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
  Decide whether to carry, coast, or release before stepping off a ledge when the landing must turn or stop.
- `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  Replace heuristic airborne release with contract-aware candidate selection.
- `MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs`
  Extend coarse transition tests to assert the new hint values.
- `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
  Update existing planner tests to assert the new evaluator-backed choices.
- `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
  Add handoff scenarios that fail if the current segment arrives too fast or too slow for the next one.
- `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  Add jump-entry and landing-entry scenarios with explicit residual speed assertions.
- `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
  Keep a deterministic regression for the "parkour into turn" case that started this investigation.
- `tools/test-transition-braking.sh`
  Move the harness to `mcc-build`, `mcc-debug`, and `mcc-cmd`, and extend it with lookahead-sensitive scenarios.
- `docs/guide/pathfinding-research.md`
  Document the new rule: the executor aims for a valid next-action entry state, not a geometric center point.

---

### Task 1: Add Quantitative Transition Hints to `PathSegment`

**Files:**
- Create: `MinecraftClient/Pathing/Execution/PathTransitionHints.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/PathTransitionHintsTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegment.cs`
- Modify: `MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs`

- [ ] **Step 1: Write the failing hint-derivation tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/PathTransitionHintsTests.cs
using System.Collections.Generic;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathTransitionHintsTests
{
    [Fact]
    public void FromPath_AssignsTurnHints_WhenNextSegmentChangesHeading()
    {
        var nodes = BuildNodes(
            (0, 80, 0, MoveType.Traverse),
            (1, 80, 0, MoveType.Traverse),
            (1, 80, 1, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);
        PathTransitionHints hints = segments[0].ExitHints;

        Assert.Equal(PathTransitionType.Turn, segments[0].ExitTransition);
        Assert.True(hints.RequireStableFooting);
        Assert.True(hints.RequireGrounded);
        Assert.Equal(0, hints.DesiredHeadingX);
        Assert.Equal(1, hints.DesiredHeadingZ);
        Assert.InRange(hints.MaxExitSpeed, 0.0, 0.05);
    }

    [Fact]
    public void FromPath_AssignsJumpReadyHints_WhenNextSegmentIsParkour()
    {
        var nodes = BuildNodes(
            (120, 80, 110, MoveType.Traverse),
            (121, 80, 110, MoveType.Traverse),
            (123, 80, 110, MoveType.Parkour));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);
        PathTransitionHints hints = segments[0].ExitHints;

        Assert.Equal(PathTransitionType.PrepareJump, segments[0].ExitTransition);
        Assert.True(hints.RequireJumpReady);
        Assert.False(hints.RequireStableFooting);
        Assert.Equal(1, hints.DesiredHeadingX);
        Assert.Equal(0, hints.DesiredHeadingZ);
        Assert.True(hints.MinExitSpeed >= 0.10, $"MinExitSpeed={hints.MinExitSpeed}");
    }

    [Fact]
    public void FromPath_AssignsPreciseStopHints_WhenSegmentIsFinalStop()
    {
        var nodes = BuildNodes(
            (10, 80, 10, MoveType.Traverse),
            (11, 80, 10, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);
        PathTransitionHints hints = segments[0].ExitHints;

        Assert.Equal(PathTransitionType.FinalStop, segments[0].ExitTransition);
        Assert.True(hints.RequireStableFooting);
        Assert.True(hints.RequireGrounded);
        Assert.InRange(hints.MaxExitSpeed, 0.0, 0.02);
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
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~PathTransitionHintsTests|FullyQualifiedName~PathSegmentBuilderTests" -v minimal
```

Expected: FAIL with compile errors because `PathTransitionHints` and `PathSegment.ExitHints` do not exist yet.

- [ ] **Step 3: Implement the hint type and derive it in the segment builder**

```csharp
// MinecraftClient/Pathing/Execution/PathTransitionHints.cs
namespace MinecraftClient.Pathing.Execution
{
    public sealed record PathTransitionHints(
        int DesiredHeadingX,
        int DesiredHeadingZ,
        double MinExitSpeed,
        double MaxExitSpeed,
        bool RequireStableFooting,
        bool RequireGrounded,
        bool RequireJumpReady,
        bool AllowAirBrake,
        int HorizonTicks)
    {
        public static PathTransitionHints Default { get; } = new(
            DesiredHeadingX: 0,
            DesiredHeadingZ: 0,
            MinExitSpeed: 0.0,
            MaxExitSpeed: double.PositiveInfinity,
            RequireStableFooting: false,
            RequireGrounded: false,
            RequireJumpReady: false,
            AllowAirBrake: false,
            HorizonTicks: 8);
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathSegment.cs
public sealed class PathSegment
{
    public required Location Start { get; init; }
    public required Location End { get; init; }
    public required MoveType MoveType { get; init; }
    public PathTransitionType ExitTransition { get; init; } = PathTransitionType.FinalStop;
    public PathTransitionHints ExitHints { get; init; } = PathTransitionHints.Default;
    public bool PreserveSprint { get; init; }

    public int HeadingX => Math.Sign(End.X - Start.X);
    public int HeadingZ => Math.Sign(End.Z - Start.Z);
}
```

```csharp
// MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs
public static List<PathSegment> FromPath(IReadOnlyList<PathNode> nodes)
{
    var segments = new List<PathSegment>(Math.Max(0, nodes.Count - 1));
    for (int i = 1; i < nodes.Count; i++)
    {
        PathSegment? next = i + 1 < nodes.Count ? CreatePreview(nodes[i], nodes[i + 1]) : null;
        PathSegment? nextNext = i + 2 < nodes.Count ? CreatePreview(nodes[i + 1], nodes[i + 2]) : null;
        PathSegment current = CreatePreview(nodes[i - 1], nodes[i]);

        PathTransitionType exitTransition = Classify(current, next);
        PathTransitionHints exitHints = BuildHints(current, next, nextNext, exitTransition);

        segments.Add(new PathSegment
        {
            Start = current.Start,
            End = current.End,
            MoveType = current.MoveType,
            ExitTransition = exitTransition,
            ExitHints = exitHints,
            PreserveSprint = exitTransition is PathTransitionType.ContinueStraight or PathTransitionType.PrepareJump
        });
    }

    return segments;
}

private static PathTransitionHints BuildHints(PathSegment current, PathSegment? next, PathSegment? nextNext, PathTransitionType exitTransition)
{
    if (next is null)
    {
        return new PathTransitionHints(
            current.HeadingX,
            current.HeadingZ,
            MinExitSpeed: 0.0,
            MaxExitSpeed: 0.01,
            RequireStableFooting: true,
            RequireGrounded: true,
            RequireJumpReady: false,
            AllowAirBrake: false,
            HorizonTicks: 12);
    }

    if (next.MoveType is MoveType.Parkour or MoveType.Ascend)
    {
        double minExitSpeed = next.MoveType == MoveType.Parkour ? 0.12 : 0.10;
        return new PathTransitionHints(
            next.HeadingX,
            next.HeadingZ,
            MinExitSpeed: minExitSpeed,
            MaxExitSpeed: double.PositiveInfinity,
            RequireStableFooting: false,
            RequireGrounded: true,
            RequireJumpReady: true,
            AllowAirBrake: false,
            HorizonTicks: 10);
    }

    bool turning = current.HeadingX != next.HeadingX || current.HeadingZ != next.HeadingZ;
    bool nextImmediatelyJumps = nextNext is not null && nextNext.MoveType is MoveType.Parkour or MoveType.Ascend;

    if (turning)
    {
        return new PathTransitionHints(
            next.HeadingX,
            next.HeadingZ,
            MinExitSpeed: nextImmediatelyJumps ? 0.08 : 0.0,
            MaxExitSpeed: 0.035,
            RequireStableFooting: true,
            RequireGrounded: true,
            RequireJumpReady: nextImmediatelyJumps,
            AllowAirBrake: true,
            HorizonTicks: 12);
    }

    return new PathTransitionHints(
        next.HeadingX,
        next.HeadingZ,
        MinExitSpeed: 0.08,
        MaxExitSpeed: double.PositiveInfinity,
        RequireStableFooting: false,
        RequireGrounded: false,
        RequireJumpReady: false,
        AllowAirBrake: false,
        HorizonTicks: 8);
}
```

- [ ] **Step 4: Re-run the hint tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~PathTransitionHintsTests|FullyQualifiedName~PathSegmentBuilderTests" -v minimal
```

Expected: PASS with all path-segment hint tests green.

- [ ] **Step 5: Commit the segment metadata slice**

```bash
git add MinecraftClient/Pathing/Execution/PathTransitionHints.cs \
        MinecraftClient/Pathing/Execution/PathSegment.cs \
        MinecraftClient/Pathing/Execution/PathSegmentBuilder.cs \
        MinecraftClient.Tests/Pathing/Execution/PathTransitionHintsTests.cs \
        MinecraftClient.Tests/Pathing/Execution/PathSegmentBuilderTests.cs
git commit -m "feat: add quantitative path transition hints"
```

---

### Task 2: Replace Threshold-Only Braking with Short-Horizon Candidate Scoring

**Files:**
- Create: `MinecraftClient/Pathing/Execution/TransitionInputProfile.cs`
- Create: `MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs`
- Create: `MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs`

- [ ] **Step 1: Write the failing evaluator and planner tests**

```csharp
// MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TransitionLookaheadEvaluatorTests
{
    [Fact]
    public void ChooseGroundProfile_PicksBrake_WhenTurnEntryCapsResidualSpeed()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.Turn,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.34, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.156, 0.0, 0.0),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseGroundProfile(
            current,
            new Location(1.34, 80.0, 0.5),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.Brake, profile);
    }

    [Fact]
    public void ChooseGroundProfile_PicksCarry_WhenPrepareJumpNeedsRunUpSpeed()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.12, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.02, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.086, 0.0, 0.0),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseGroundProfile(
            current,
            new Location(1.02, 80.0, 0.5),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.Carry, profile);
    }

    [Fact]
    public void ChooseAirProfile_PicksRelease_WhenLandingNeedsSlowStableEntry()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 118, max: 126);
        var current = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(123.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(123.06, 80.92, 110.5),
            DeltaMovement = new Vec3d(0.31, 0.0, 0.0),
            OnGround = false,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseAirProfile(
            current,
            new Location(123.06, 80.92, 110.5),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.AirRelease, profile);
    }
}
```

- [ ] **Step 2: Run the evaluator-focused tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~TransitionLookaheadEvaluatorTests|FullyQualifiedName~TransitionBrakingPlannerTests" -v minimal
```

Expected: FAIL with compile errors because `TransitionInputProfile` and `TransitionLookaheadEvaluator` do not exist yet.

- [ ] **Step 3: Implement candidate profiles, lookahead scoring, and planner wiring**

```csharp
// MinecraftClient/Pathing/Execution/TransitionInputProfile.cs
namespace MinecraftClient.Pathing.Execution
{
    internal enum TransitionInputProfile
    {
        Carry,
        Coast,
        Brake,
        AirHoldForward,
        AirRelease,
        AirBrake
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    internal static class TransitionLookaheadEvaluator
    {
        internal static TransitionInputProfile ChooseGroundProfile(PathSegment segment, Location pos, PlayerPhysics physics, World world)
        {
            TransitionInputProfile[] candidates =
            [
                TransitionInputProfile.Carry,
                TransitionInputProfile.Coast,
                TransitionInputProfile.Brake
            ];

            return ChooseBest(segment, pos, physics, world, candidates);
        }

        internal static TransitionInputProfile ChooseAirProfile(PathSegment segment, Location pos, PlayerPhysics physics, World world)
        {
            TransitionInputProfile[] candidates =
            [
                TransitionInputProfile.AirHoldForward,
                TransitionInputProfile.AirRelease,
                TransitionInputProfile.AirBrake
            ];

            return ChooseBest(segment, pos, physics, world, candidates);
        }

        private static TransitionInputProfile ChooseBest(PathSegment segment, Location pos, PlayerPhysics physics, World world, TransitionInputProfile[] candidates)
        {
            TransitionInputProfile best = candidates[0];
            double bestScore = double.PositiveInfinity;

            foreach (TransitionInputProfile candidate in candidates)
            {
                double score = Score(segment, pos, physics, world, candidate);
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static double Score(PathSegment segment, Location pos, PlayerPhysics physics, World world, TransitionInputProfile candidate)
        {
            PlayerPhysics sim = TemplateHelper.ClonePhysicsForPlanning(physics);
            var input = new MovementInput();
            double score = 0.0;

            for (int tick = 0; tick < segment.ExitHints.HorizonTicks; tick++)
            {
                input.Reset();
                ApplyCandidateInput(input, candidate, segment.PreserveSprint);
                sim.ApplyInput(input);
                sim.Tick(world);
            }

            Location simPos = new(sim.Position.X, sim.Position.Y, sim.Position.Z);
            double forwardSpeed = TemplateHelper.ProjectHorizontalSpeedAlongSegment(sim, segment);

            if (segment.ExitHints.RequireGrounded && !sim.OnGround)
                score += 1000.0;

            if (segment.ExitHints.RequireStableFooting &&
                !TemplateHelper.IsSettledOnTargetBlock(simPos, segment.End, sim))
            {
                score += 1000.0;
            }

            if (forwardSpeed < segment.ExitHints.MinExitSpeed)
                score += (segment.ExitHints.MinExitSpeed - forwardSpeed) * 200.0;

            if (forwardSpeed > segment.ExitHints.MaxExitSpeed)
                score += (forwardSpeed - segment.ExitHints.MaxExitSpeed) * 200.0;

            score += TemplateHelper.HeadingPenaltyDegrees(sim.Yaw, segment.ExitHints.DesiredHeadingX, segment.ExitHints.DesiredHeadingZ);
            score += Math.Abs(segment.End.X - simPos.X) + Math.Abs(segment.End.Z - simPos.Z);

            return score;
        }

        private static void ApplyCandidateInput(MovementInput input, TransitionInputProfile candidate, bool preserveSprint)
        {
            switch (candidate)
            {
                case TransitionInputProfile.Carry:
                case TransitionInputProfile.AirHoldForward:
                    input.Forward = true;
                    input.Sprint = preserveSprint;
                    break;
                case TransitionInputProfile.Brake:
                case TransitionInputProfile.AirBrake:
                    input.Back = true;
                    break;
                case TransitionInputProfile.Coast:
                case TransitionInputProfile.AirRelease:
                default:
                    break;
            }
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs
public static TransitionBrakingDecision Plan(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
{
    TransitionInputProfile profile = physics.OnGround
        ? TransitionLookaheadEvaluator.ChooseGroundProfile(current, pos, physics, world)
        : TransitionLookaheadEvaluator.ChooseAirProfile(current, pos, physics, world);

    return profile switch
    {
        TransitionInputProfile.Carry => TransitionBrakingDecision.CarryMomentum(current.PreserveSprint),
        TransitionInputProfile.Coast => TransitionBrakingDecision.Coast,
        TransitionInputProfile.Brake => TransitionBrakingDecision.Brake,
        TransitionInputProfile.AirHoldForward => TransitionBrakingDecision.CarryMomentum(current.PreserveSprint),
        TransitionInputProfile.AirRelease => TransitionBrakingDecision.Coast,
        TransitionInputProfile.AirBrake => TransitionBrakingDecision.Brake,
        _ => TransitionBrakingDecision.Coast
    };
}

public static bool ShouldReleaseForwardInAir(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
{
    if (!current.ExitHints.AllowAirBrake)
        return false;

    TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseAirProfile(current, pos, physics, world);
    return profile is TransitionInputProfile.AirRelease or TransitionInputProfile.AirBrake;
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs
internal static PlayerPhysics ClonePhysicsForPlanning(PlayerPhysics physics)
{
    return new PlayerPhysics
    {
        Position = physics.Position,
        DeltaMovement = physics.DeltaMovement,
        Yaw = physics.Yaw,
        Pitch = physics.Pitch,
        OnGround = physics.OnGround,
        HorizontalCollision = physics.HorizontalCollision,
        VerticalCollision = physics.VerticalCollision,
        VerticalCollisionBelow = physics.VerticalCollisionBelow,
        FallDistance = physics.FallDistance,
        StuckSpeedMultiplier = physics.StuckSpeedMultiplier,
        Xxa = physics.Xxa,
        Zza = physics.Zza,
        Yya = physics.Yya,
        Jumping = physics.Jumping,
        Sprinting = physics.Sprinting,
        Sneaking = physics.Sneaking,
        CreativeFlying = physics.CreativeFlying,
        InWater = physics.InWater,
        IsUnderWater = physics.IsUnderWater,
        InLava = physics.InLava,
        OnClimbable = physics.OnClimbable,
        HasSlowFalling = physics.HasSlowFalling,
        HasLevitation = physics.HasLevitation,
        LevitationAmplifier = physics.LevitationAmplifier,
        MovementSpeed = physics.MovementSpeed
    };
}

internal static double HeadingPenaltyDegrees(float yaw, int headingX, int headingZ)
{
    if (headingX == 0 && headingZ == 0)
        return 0.0;

    float targetYaw = CalculateYaw(headingX, headingZ);
    float delta = targetYaw - yaw;
    while (delta > 180f) delta -= 360f;
    while (delta < -180f) delta += 360f;
    return Math.Abs(delta) / 10.0;
}
```

- [ ] **Step 4: Re-run the planner tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~TransitionLookaheadEvaluatorTests|FullyQualifiedName~TransitionBrakingPlannerTests" -v minimal
```

Expected: PASS with all evaluator and planner tests green.

- [ ] **Step 5: Commit the planner upgrade**

```bash
git add MinecraftClient/Pathing/Execution/TransitionInputProfile.cs \
        MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs \
        MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs \
        MinecraftClient/Pathing/Execution/Templates/TemplateHelper.cs \
        MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs \
        MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs
git commit -m "feat: score transition inputs with short-horizon lookahead"
```

---

### Task 3: Teach Templates to Hand Off Only When Exit Contracts Are Satisfied

**Files:**
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`

- [ ] **Step 1: Add failing convergence tests for turn-entry and jump-entry handoff**

```csharp
// MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs
[Fact]
public void WalkTemplate_TurnIntoParkour_CompletesOnlyWhenTurnEntryIsSlowAndJumpReady()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 120, max: 128);

    var current = new PathSegment
    {
        Start = new Location(120.5, 80, 110.5),
        End = new Location(121.5, 80, 110.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.Turn,
        ExitHints = new PathTransitionHints(0, 1, 0.08, 0.035, true, true, true, true, 12)
    };
    var next = new PathSegment
    {
        Start = new Location(121.5, 80, 110.5),
        End = new Location(121.5, 80, 111.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.PrepareJump,
        ExitHints = new PathTransitionHints(0, 1, 0.12, double.PositiveInfinity, false, true, true, false, 10),
        PreserveSprint = true
    };

    var template = new WalkTemplate(current, next);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

    TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);
    double horizontalSpeed = Math.Sqrt(physics.DeltaMovement.X * physics.DeltaMovement.X + physics.DeltaMovement.Z * physics.DeltaMovement.Z);

    Assert.Equal(TemplateState.Complete, state);
    Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, current.End));
    Assert.InRange(horizontalSpeed, 0.08, 0.20);
}
```

```csharp
// MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs
[Fact]
public void SprintJumpTemplate_LandingRecoveryIntoTurn_CompletesWithLowResidualSpeed()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 118, max: 126);
    FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
    FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
    FlatWorldTestBuilder.SetSolid(world, 123, 79, 110);
    FlatWorldTestBuilder.SetSolid(world, 123, 79, 111);

    var segment = new PathSegment
    {
        Start = new Location(120.5, 80, 110.5),
        End = new Location(123.5, 80, 110.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.LandingRecovery,
        ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
    };
    var next = new PathSegment
    {
        Start = new Location(123.5, 80, 110.5),
        End = new Location(123.5, 80, 111.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.FinalStop
    };

    var template = new SprintJumpTemplate(segment, next);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

    TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 160, out Location finalPos);
    double horizontalSpeed = Math.Sqrt(physics.DeltaMovement.X * physics.DeltaMovement.X + physics.DeltaMovement.Z * physics.DeltaMovement.Z);

    Assert.Equal(TemplateState.Complete, state);
    Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    Assert.InRange(horizontalSpeed, 0.0, 0.04);
}
```

- [ ] **Step 2: Run the convergence tests to verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~GroundedTemplateConvergenceTests|FullyQualifiedName~SprintJumpTemplateScenarioTests|FullyQualifiedName~LivePathingRegressionTests" -v minimal
```

Expected: FAIL because grounded completion and airborne release still use coarse threshold logic.

- [ ] **Step 3: Update grounded and airborne templates to obey `ExitHints`**

```csharp
// MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs
internal static bool ShouldComplete(PathSegment segment, Location pos, PlayerPhysics physics)
{
    if (segment.ExitHints.RequireJumpReady)
    {
        return physics.OnGround
            && TemplateHelper.HasReachedSegmentEndPlane(pos, segment)
            && TemplateHelper.ProjectHorizontalSpeedAlongSegment(physics, segment) >= segment.ExitHints.MinExitSpeed
            && TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment.ExitHints.DesiredHeadingX, segment.ExitHints.DesiredHeadingZ) <= 1.0;
    }

    if (segment.ExitHints.RequireStableFooting)
    {
        return physics.OnGround
            && TemplateHelper.IsSettledOnTargetBlock(pos, segment.End, physics)
            && TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment.ExitHints.DesiredHeadingX, segment.ExitHints.DesiredHeadingZ) <= 2.0;
    }

    return segment.ExitTransition switch
    {
        PathTransitionType.ContinueStraight => TemplateHelper.IsNear(pos, segment.End, horizThresholdSq: 0.09),
        PathTransitionType.PrepareJump => TemplateHelper.HasReachedSegmentEndPlane(pos, segment)
            && TemplateHelper.ProjectHorizontalSpeedAlongSegment(physics, segment) >= segment.ExitHints.MinExitSpeed,
        _ => TemplateHelper.HasReachedSegmentEndPlane(pos, segment)
    };
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs
else if (horizDistSq > 0.01)
{
    physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
    if (_hasFallen || YawDifference(physics.Yaw, targetYaw) <= PreDropYawToleranceDeg)
    {
        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);

        if (!_hasFallen && _segment.ExitHints.AllowAirBrake && decision == TransitionBrakingDecision.Coast)
        {
            input.Forward = false;
            input.Sprint = false;
        }
        else
        {
            TemplateHelper.ApplyDecision(input, decision);
        }
    }
}
```

```csharp
// MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs
private bool ShouldReleaseInAir(Location pos, PlayerPhysics physics, World world)
{
    if (!_segment.ExitHints.AllowAirBrake)
        return false;

    TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseAirProfile(_segment, pos, physics, world);
    return profile is TransitionInputProfile.AirRelease or TransitionInputProfile.AirBrake;
}

case Phase.Landing:
    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
    TemplateHelper.ApplyDecision(input, decision);

    if (physics.OnGround && GroundedSegmentController.ShouldComplete(_segment, pos, physics))
        return TemplateState.Complete;
    break;
```

- [ ] **Step 4: Re-run the convergence tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~GroundedTemplateConvergenceTests|FullyQualifiedName~SprintJumpTemplateScenarioTests|FullyQualifiedName~LivePathingRegressionTests" -v minimal
```

Expected: PASS with the turn-entry and landing-entry regressions green.

- [ ] **Step 5: Commit the template handoff slice**

```bash
git add MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs \
        MinecraftClient/Pathing/Execution/Templates/WalkTemplate.cs \
        MinecraftClient/Pathing/Execution/Templates/DescendTemplate.cs \
        MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs \
        MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs \
        MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs \
        MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs
git commit -m "feat: honor transition entry contracts in templates"
```

---

### Task 4: Modernize the 1.21.11 Live Regression Harness and Document the New Semantics

**Files:**
- Modify: `tools/test-transition-braking.sh`
- Modify: `tools/test-pathing-template-regressions.sh`
- Modify: `docs/guide/pathfinding-research.md`

- [ ] **Step 1: Rewrite the live harness around `mcc-build`, `mcc-debug`, and `mcc-cmd`**

```bash
# tools/test-transition-braking.sh
source "$REPO_ROOT/tools/mcc-env.sh"

VERSION="${1:-1.21.11-Vanilla}"
SESSION="${2:-brake-lookahead}"
USERNAME="${3:-$(_mcc_resolve_username "$SESSION")}"

send_mcc() {
    mcc-cmd --session "$SESSION" "$1"
}

start_mcc() {
    mcc-build >/dev/null
    mcc-debug -v "$VERSION" --file-input --session "$SESSION" --username "$USERNAME" --no-build --debug-on >/dev/null
    mc-rcon "op $USERNAME" >/dev/null
}

capture_debug_location() {
    local root="${TMPDIR:-/tmp}/mcc-debug/$SESSION"
    local log="$root/mcc-debug.log"
    local start_line
    start_line="$(wc -l < "$log")"
    send_mcc "debug state"
    for _ in $(seq 1 10); do
        if tail -n +"$((start_line + 1))" "$log" | grep -Fq "Location:"; then
            python3 - "$log" "$start_line" <<'PY'
import pathlib
import re
import sys

path = pathlib.Path(sys.argv[1])
start_line = int(sys.argv[2])
text = "\n".join(path.read_text(errors="ignore").splitlines()[start_line:])
match = re.findall(r"Location:\s+([-\d.]+),\s+([-\d.]+),\s+([-\d.]+)", text)
if not match:
    raise SystemExit("No Location line found")
x, y, z = match[-1]
print(f"{x} {y} {z}")
PY
            return 0
        fi
        sleep 1
    done
    return 1
}
```

- [ ] **Step 2: Add one straight-stop scenario and one turn-into-jump scenario to the harness**

```bash
# tools/test-transition-braking.sh
run_turn_into_jump() {
    echo "== Turn into jump runway =="
    mc-rcon "fill 120 79 110 126 79 114 stone" >/dev/null
    mc-rcon "fill 120 80 110 126 85 114 air" >/dev/null
    mc-rcon "setblock 123 79 112 air" >/dev/null
    mc-rcon "setblock 124 79 112 stone" >/dev/null
    mc-rcon "tp $USERNAME 120.5 80 110.5" >/dev/null
    sleep 2

    send_mcc "goto 124 80 112"
    sleep 6

    local x y z
    read -r x y z <<< "$(capture_debug_location)"
    echo "Final location: $x $y $z"

    python3 - <<'PY' "$x" "$z"
import sys
x = float(sys.argv[1])
z = float(sys.argv[2])
if not (123.20 <= x <= 124.10 and 111.80 <= z <= 112.60):
    raise SystemExit(f"Unexpected turn-into-jump finish: ({x:.2f}, {z:.2f})")
PY
}
```

- [ ] **Step 3: Update the pathfinding research doc to describe entry contracts**

```md
<!-- docs/guide/pathfinding-research.md -->
## Transition Entry Contracts

Path execution no longer aims for a visual block center as the primary success rule.
Instead, each `PathSegment` carries quantitative exit hints that describe what the next
segment needs:

- desired heading at handoff
- minimum exit speed when the next action is a jump takeoff
- maximum exit speed when the next action is a turn or final stop
- whether stable grounded footing is required before handoff
- whether airborne forward release is allowed before landing

This keeps MCC physically honest while still making segment boundaries precise enough
for chained turns and jumps on 1.21.11.
```

- [ ] **Step 4: Run the full validation loop**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution" -v minimal
source tools/mcc-env.sh && mcc-build
source tools/mcc-env.sh && bash tools/test-transition-braking.sh 1.21.11-Vanilla
source tools/mcc-env.sh && bash tools/test-pathing-template-regressions.sh 1.21.11-Vanilla
```

Expected:

- `dotnet test` reports all pathing execution tests passing
- `mcc-build` exits `0`
- `tools/test-transition-braking.sh` prints `All transition braking checks passed.`
- `tools/test-pathing-template-regressions.sh` exits `0`

- [ ] **Step 5: Commit the harness and documentation updates**

```bash
git add tools/test-transition-braking.sh \
        tools/test-pathing-template-regressions.sh \
        docs/guide/pathfinding-research.md
git commit -m "test: validate lookahead path transitions on 1.21.11"
```

---

## Self-Review

**Spec coverage**

- "planner should know whether the next step continues or turns": covered by Task 1 transition hints and Task 2 evaluator scoring
- "braking can start on the previous step": covered by Task 2 candidate evaluation and Task 3 grounded template handoff rules
- "airborne forward release should be planned": covered by Task 2 `ChooseAirProfile()` and Task 3 `SprintJumpTemplate`
- "continue with the new dev workflow on 1.21.11": covered by Task 4 harness modernization and validation commands

**Placeholder scan**

- No `TODO`, `TBD`, or "implement later" markers remain
- Every code-changing step includes concrete code blocks
- Every verification step includes exact commands and expected results

**Type consistency**

- `PathTransitionHints` is the only new segment metadata type
- `TransitionInputProfile` is the only new candidate-input enum
- `TransitionLookaheadEvaluator` is the only new evaluator type used by `TransitionBrakingPlanner`
