# Pathing Execution Regression Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the current execution-layer regressions exposed by the contract/timing harness so deterministic jump-combo and long-route scenarios complete with `0` replans and within their existing budgets.

**Architecture:** Treat the failures as three runtime bugs, not as harness problems. First tighten parkour landing recovery so chained jumps hand off with the right speed instead of stalling or replan-looping. Second make transition braking and lookahead score the next segment entry contract, so mixed turn/ascend/descend routes stop choosing the wrong carry-or-brake profile. Third harden chained ascends for live-runtime carry states so staircases stop burning extra ticks after each landing. Keep the existing JSON contracts, scenario catalog, and shell harnesses unchanged except for verification.

**Tech Stack:** C# 14 / .NET 10, xUnit, MCC pathing execution templates, `PlayerPhysics`, existing `MinecraftClient.Tests` scenario runner and timing contracts, local `1.21.11-Vanilla` live harness via `tools/mcc-env.sh`.

---

## Scope Check

This plan only covers runtime execution fixes in the existing pathing stack.

Out of scope:

- planner-contract schema changes
- theory-matrix generation changes
- telemetry/report format changes
- new live harness features
- broad planner heuristics refactors

## Current Failure Inventory

Focused xUnit evidence from:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution" -v minimal
```

Current failing families:

- repeated parkour chains do not complete cleanly
  - `repeated-cardinal-parkour-chain`: navigation did not complete, `replans=4`
  - `repeated-diagonal-parkour-chain`: expected `0` replans, saw `2`
  - `obstructed-parkour-l-turns`: navigation did not complete, `replans=1`
  - `same-move-aligned-parkour-chain`: navigation did not complete, `replans=4`
- mixed vertical and mixed long routes over-brake or replan unexpectedly
  - `vertical-jump-mix`: expected `0` replans, saw `1`
  - `diagonal-vertical-mix`: expected `0` replans, saw `1`
  - `mixed-traverse-turn-parkour-turn-traverse`: expected `0` replans, saw `1`
  - `mixed-traverse-ascend-parkour-descend`: expected `0` replans, saw `1`
  - `speed-carry-repeated-traverse-descend`: expected `0` replans, saw `1`
  - `speed-carry-repeated-traverse-parkour`: navigation did not complete, `replans=4`

Live harness evidence:

```bash
source tools/mcc-env.sh && bash tools/test-pathing-jump-combos.sh 1.21.11-Vanilla
source tools/mcc-env.sh && bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Current live failures:

- `same-move-ascend-staircase`: `actual=145 max=68`, first four ascend segments each over by roughly `+22` to `+23` ticks
- `vertical-jump-mix`: `actual=54 max=40`
- repeated parkour chains fail with segment failure followed by replan loops

## Problem Map

1. `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
   - landing recovery is still biased toward “settle fully” behavior
   - `pastTarget` release is too blunt for repeated parkour and mixed jump chains
   - completion rules do not preserve enough entry speed for immediate follow-up jumps

2. `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
   - planning still reasons mostly about the current segment
   - special-cases landing-recovery turns, but not the broader mixed-route handoff problem

3. `MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs`
   - air and ground scoring ignore too much next-segment intent
   - current profiles cannot distinguish “slow down for stable turn entry” from “keep enough speed for the next descend or jump”

4. `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
   - chained ascends do not explicitly separate takeoff, airborne, and landing handoff
   - live staircase traces show repeated post-landing delay before the next step starts

5. `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
   - grounded completion is strong for final stops, but too conservative for continue-straight ascend handoff

## File Structure

### Production files

- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`
  - parkour landing recovery completion and in-air release rules
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`
  - next-segment-aware braking decisions
- Modify: `MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs`
  - ground and air profile scoring that considers the next segment contract
- Modify: `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
  - explicit ascend phase handling and faster landing handoff
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`
  - shared completion rules for continue-straight ascend chaining

### Test files

- Modify: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`
  - named regression entry points for representative failing scenarios
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
  - deterministic chained-jump handoff regression
- Modify: `MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs`
  - ground and air next-segment profile regressions
- Modify: `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
  - planner decisions for mixed handoff states
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
  - chained-ascend convergence regression

### Verification only

- Reuse: `tools/test-pathing-jump-combos.sh`
- Reuse: `tools/test-pathing-long-routes.sh`

---

### Task 1: Stabilize Repeated Parkour Landing Recovery

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`

- [ ] **Step 1: Write failing parkour-focused regression tests**

Update `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`:

```csharp
using MinecraftClient.Tests.Pathing.Execution.Contracts;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathTimingContractTests
{
    [Fact]
    public void RepeatedCardinalParkourChain_ExecutionStaysWithinBudget() =>
        AssertScenarioWithinBudget("repeated-cardinal-parkour-chain");

    [Fact]
    public void RepeatedDiagonalParkourChain_ExecutionStaysWithinBudget() =>
        AssertScenarioWithinBudget("repeated-diagonal-parkour-chain");

    private static void AssertScenarioWithinBudget(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathingTimingBudget budget = PathingContractStore.LoadFromRepositoryRoot().GetTiming(scenarioId);
        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

        PathingContractAssert.TimingMatches(budget, result);
    }
}
```

Update `MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs`:

```csharp
[Fact]
public void SprintJumpTemplate_LandingRecovery_LeavesEnoughSpeedForNextParkour()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 578, max: 586);
    FlatWorldTestBuilder.ClearBox(world, 578, 79, 578, 586, 90, 582);
    FlatWorldTestBuilder.SetSolid(world, 580, 79, 580);
    FlatWorldTestBuilder.SetSolid(world, 582, 79, 580);
    FlatWorldTestBuilder.SetSolid(world, 584, 79, 580);

    var current = new PathSegment
    {
        Start = new Location(580.5, 80, 580.5),
        End = new Location(582.5, 80, 580.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.LandingRecovery,
        ExitHints = new PathTransitionHints(1, 0, 0.12, 0.20, false, true, true, true, 12),
        PreserveSprint = true
    };
    var next = new PathSegment
    {
        Start = current.End,
        End = new Location(584.5, 80, 580.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.LandingRecovery,
        ExitHints = new PathTransitionHints(1, 0, 0.12, 0.20, false, true, true, true, 12),
        PreserveSprint = true
    };

    var template = new SprintJumpTemplate(current, next);
    var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

    TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

    Assert.Equal(TemplateState.Complete, state);
    Assert.True(TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, current.End), $"finalPos={finalPos} vel={physics.DeltaMovement}");
    Assert.InRange(physics.DeltaMovement.X, 0.12, 0.30);
}
```

- [ ] **Step 2: Run the focused tests and verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution.PathTimingContractTests.RepeatedCardinalParkourChain_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.RepeatedDiagonalParkourChain_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.SprintJumpTemplateScenarioTests.SprintJumpTemplate_LandingRecovery_LeavesEnoughSpeedForNextParkour" -v minimal
```

Expected: FAIL with either `navigation did not complete`, nonzero replans, or residual speed below the handoff minimum.

- [ ] **Step 3: Make `SprintJumpTemplate` preserve jump-ready handoff instead of over-settling**

Update `MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs`:

```csharp
case Phase.Airborne:
{
    if (!physics.OnGround)
        _leftGround = true;

    bool releaseInAir = ShouldReleaseInAir(pos, physics, world);
    bool hardRelease = releaseInAir;
    if (_segment.ExitTransition != PathTransitionType.LandingRecovery && IsPastTarget(pos))
        hardRelease = true;

    if (hardRelease)
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
}

case Phase.Landing:
    if (ShouldCompleteLandingRecoveryHandoff(pos, physics))
        return TemplateState.Complete;

    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
    TemplateHelper.ApplyDecision(input, decision);
    if (decision.HoldBack)
        TemplateHelper.FaceSegmentHeading(physics, _segment);
    else if (TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment))
        TemplateHelper.FaceExitHeading(physics, _segment);

    if (_segment.ExitTransition == PathTransitionType.ContinueStraight
        && horizDistSq < 2.25
        && Math.Abs(dy) < 1.0)
    {
        return TemplateState.Complete;
    }
    break;

private bool ShouldCompleteLandingRecoveryHandoff(Location pos, PlayerPhysics physics)
{
    if (_segment.ExitTransition != PathTransitionType.LandingRecovery || _nextSegment is null || !physics.OnGround)
        return false;

    double exitSpeed = TemplateHelper.ProjectHorizontalSpeedAlongHint(physics, _segment);
    if (_nextSegment.ExitHints.RequireJumpReady)
    {
        return TemplateFootingHelper.IsCenterInsideTargetBlock(pos, ExpectedEnd)
            && !TemplateFootingHelper.WillCenterLeaveTargetBlockNextTick(pos, physics, ExpectedEnd)
            && exitSpeed >= _nextSegment.ExitHints.MinExitSpeed;
    }

    return TemplateFootingHelper.IsCenterInsideSupportStrip(pos, ExpectedEnd, _nextSegment.End)
        && !TemplateFootingHelper.WillCenterLeaveSupportStripNextTick(pos, physics, ExpectedEnd, _nextSegment.End)
        && exitSpeed <= _segment.ExitHints.MaxExitSpeed;
}
```

- [ ] **Step 4: Re-run the parkour-focused tests and then the whole jump-combo contract group**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution.PathTimingContractTests.RepeatedCardinalParkourChain_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.RepeatedDiagonalParkourChain_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.JumpCombo_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.SprintJumpTemplateScenarioTests" -v minimal
```

Expected: PASS for the two named regressions and no new failures in the broader jump-template coverage.

- [ ] **Step 5: Commit the parkour landing recovery fix**

```bash
git add MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs \
        MinecraftClient.Tests/Pathing/Execution/SprintJumpTemplateScenarioTests.cs \
        MinecraftClient/Pathing/Execution/Templates/SprintJumpTemplate.cs
git commit -m "fix: preserve jump-ready speed through parkour landing recovery"
```

### Task 2: Make Braking And Lookahead Respect The Next Segment Contract

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs`
- Modify: `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs`
- Modify: `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`

- [ ] **Step 1: Add failing mixed-route regression tests**

Update `MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs`:

```csharp
[Fact]
public void MixedTraverseAscendParkourDescend_ExecutionStaysWithinBudget() =>
    AssertScenarioWithinBudget("mixed-traverse-ascend-parkour-descend");

[Fact]
public void MixedTraverseTurnParkourTurnTraverse_ExecutionStaysWithinBudget() =>
    AssertScenarioWithinBudget("mixed-traverse-turn-parkour-turn-traverse");

[Fact]
public void SpeedCarryRepeatedTraverseDescend_ExecutionStaysWithinBudget() =>
    AssertScenarioWithinBudget("speed-carry-repeated-traverse-descend");
```

Update `MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs`:

```csharp
[Fact]
public void ChooseGroundProfile_PicksBrake_WhenLandingRecoveryTurnWouldOvershootSupportStrip()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 108, max: 126);
    FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
    FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
    FlatWorldTestBuilder.SetSolid(world, 122, 79, 110);
    FlatWorldTestBuilder.SetSolid(world, 122, 79, 111);

    var current = new PathSegment
    {
        Start = new Location(120.5, 80, 110.5),
        End = new Location(122.5, 80, 110.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.LandingRecovery,
        ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
    };
    var next = new PathSegment
    {
        Start = current.End,
        End = new Location(122.5, 80, 111.5),
        MoveType = MoveType.Traverse,
        ExitTransition = PathTransitionType.PrepareJump,
        ExitHints = new PathTransitionHints(0, 1, 0.12, double.PositiveInfinity, false, true, true, false, 10),
        PreserveSprint = true
    };

    var physics = new PlayerPhysics
    {
        Position = new Vec3d(122.58, 80.0, 110.68),
        DeltaMovement = new Vec3d(0.118, 0.0, 0.018),
        OnGround = true,
        MovementSpeed = 0.1f,
        Yaw = 270f
    };

    TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseGroundProfile(
        current,
        next,
        new Location(122.58, 80.0, 110.68),
        physics,
        world);

    Assert.Equal(TransitionInputProfile.Brake, profile);
}
```

Update `MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs`:

```csharp
[Fact]
public void Plan_Carries_ForLandingRecovery_WhenNextDescendStillNeedsRunway()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 438, max: 448);
    FlatWorldTestBuilder.ClearBox(world, 438, 79, 438, 448, 84, 442);
    FlatWorldTestBuilder.SetSolid(world, 440, 79, 440);
    FlatWorldTestBuilder.SetSolid(world, 441, 79, 440);
    FlatWorldTestBuilder.SetSolid(world, 442, 79, 440);
    FlatWorldTestBuilder.SetSolid(world, 443, 80, 440);
    FlatWorldTestBuilder.SetSolid(world, 444, 79, 440);

    var current = new PathSegment
    {
        Start = new Location(441.5, 81, 440.5),
        End = new Location(443.5, 81, 440.5),
        MoveType = MoveType.Parkour,
        ExitTransition = PathTransitionType.LandingRecovery,
        ExitHints = new PathTransitionHints(1, 0, 0.0, 0.035, true, true, false, true, 12)
    };
    var next = new PathSegment
    {
        Start = current.End,
        End = new Location(444.5, 80, 440.5),
        MoveType = MoveType.Descend,
        ExitTransition = PathTransitionType.FinalStop,
        ExitHints = new PathTransitionHints(1, 0, 0.0, 0.02, true, true, false, false, 12)
    };

    var physics = CreatePhysics(0.086, 0.0, onGround: true);
    physics.Position = new Vec3d(443.18, 81.0, 440.5);

    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(
        current,
        next,
        new Location(443.18, 81.0, 440.5),
        physics,
        world);

    Assert.True(decision.HoldForward);
    Assert.False(decision.HoldBack);
}
```

- [ ] **Step 2: Run the mixed-route tests and verify they fail**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution.PathTimingContractTests.MixedTraverseAscendParkourDescend_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.MixedTraverseTurnParkourTurnTraverse_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.SpeedCarryRepeatedTraverseDescend_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.TransitionLookaheadEvaluatorTests.ChooseGroundProfile_PicksBrake_WhenLandingRecoveryTurnWouldOvershootSupportStrip|FullyQualifiedName~Pathing.Execution.TransitionBrakingPlannerTests.Plan_Carries_ForLandingRecovery_WhenNextDescendStillNeedsRunway" -v minimal
```

Expected: FAIL because current lookahead and planner logic either brake when the next segment needs carry, or carry when the turn entry should already be slowing down.

- [ ] **Step 3: Thread `nextSegment` through lookahead scoring and braking decisions**

Update `MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs`:

```csharp
public static TransitionInputProfile ChooseGroundProfile(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
{
    double remaining = TemplateHelper.RemainingDistanceAlongSegment(pos, current);
    double forwardSpeed = Math.Max(0.0,
        TemplateHelper.ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));

    bool requiresJumpEntry = current.ExitHints.RequireJumpReady
        || current.ExitTransition == PathTransitionType.PrepareJump;

    if (current.ExitTransition == PathTransitionType.ContinueStraight && !requiresJumpEntry)
        return TransitionInputProfile.Carry;

    if (requiresJumpEntry)
        return TransitionInputProfile.Carry;

    if (next is not null && current.ExitTransition == PathTransitionType.LandingRecovery)
    {
        bool headingChange = current.HeadingX != next.HeadingX || current.HeadingZ != next.HeadingZ;
        if (headingChange && forwardSpeed > GetTargetMaxExitSpeed(current))
            return TransitionInputProfile.Brake;

        if (next.ExitHints.RequireJumpReady && forwardSpeed < next.ExitHints.MinExitSpeed)
            return TransitionInputProfile.Carry;
    }

    bool requiresSlowEntry = current.ExitHints.RequireStableFooting
        || current.ExitTransition is PathTransitionType.FinalStop or PathTransitionType.Turn
        || (current.ExitTransition == PathTransitionType.LandingRecovery
            && (current.ExitHints.AllowAirBrake || IsFiniteSpeedCap(current)));

    if (!requiresSlowEntry)
        return TransitionInputProfile.Carry;

    double maxExitSpeed = GetTargetMaxExitSpeed(current);
    double hardBrakeDistance = TransitionBrakingPlanner.EstimateGroundStopDistance(
        physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: true);
    double coastStopDistance = TransitionBrakingPlanner.EstimateGroundStopDistance(
        physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: false);

    if (remaining < 0.0)
        return TransitionInputProfile.Brake;

    if (forwardSpeed > maxExitSpeed && remaining <= hardBrakeDistance + 0.10)
        return TransitionInputProfile.Brake;

    if (forwardSpeed <= maxExitSpeed && remaining > 0.0)
        return TransitionInputProfile.Carry;

    if (remaining <= coastStopDistance + 0.06)
        return TransitionInputProfile.Coast;

    return TransitionInputProfile.Carry;
}

public static TransitionInputProfile ChooseAirProfile(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
{
    if (!current.ExitHints.AllowAirBrake)
        return TransitionInputProfile.AirHoldForward;

    TransitionInputProfile[] candidates =
    [
        TransitionInputProfile.AirHoldForward,
        TransitionInputProfile.AirRelease,
        TransitionInputProfile.AirBrake
    ];

    return ChooseBest(current, next, pos, physics, world, candidates);
}

private static TransitionInputProfile ChooseBest(PathSegment segment, PathSegment? next, Location pos, PlayerPhysics physics, World world,
    TransitionInputProfile[] candidates)
{
    TransitionInputProfile best = candidates[0];
    double bestScore = double.PositiveInfinity;

    foreach (TransitionInputProfile candidate in candidates)
    {
        double score = Score(segment, next, pos, physics, world, candidate);
        if (score < bestScore)
        {
            best = candidate;
            bestScore = score;
        }
    }

    return best;
}

private static double Score(PathSegment segment, PathSegment? next, Location pos, PlayerPhysics physics, World world, TransitionInputProfile candidate)
{
    PlayerPhysics sim = TemplateHelper.ClonePhysicsForPlanning(physics);
    sim.Position = new Vec3d(pos.X, pos.Y, pos.Z);

    var input = new MovementInput();
    Location simPos = pos;

    for (int tick = 0; tick < segment.ExitHints.HorizonTicks; tick++)
    {
        if (TemplateHelper.ShouldBiasTowardExitHeading(simPos, segment))
            TemplateHelper.FaceExitHeading(sim, segment);

        input.Reset();
        ApplyCandidateInput(input, candidate, segment);
        sim.ApplyInput(input);
        sim.Tick(world);
        simPos = new Location(sim.Position.X, sim.Position.Y, sim.Position.Z);
    }

    double score = ScoreNextSegmentEntry(segment, next, simPos, sim);
    score += TemplateHelper.HeadingPenaltyDegrees(sim.Yaw, segment);
    score += Math.Abs(TemplateHelper.RemainingDistanceAlongSegment(simPos, segment)) * 10.0;
    return score;
}

private static double ScoreNextSegmentEntry(PathSegment current, PathSegment? next, Location simPos, PlayerPhysics sim)
{
    if (next is null)
        return 0.0;

    double score = 0.0;

    if (current.ExitTransition == PathTransitionType.LandingRecovery
        && (current.HeadingX != next.HeadingX || current.HeadingZ != next.HeadingZ)
        && !TemplateFootingHelper.IsCenterInsideSupportStrip(simPos, current.End, next.End))
    {
        score += 1200.0;
    }

    if (next.ExitHints.RequireJumpReady)
    {
        double nextSpeed = TemplateHelper.ProjectHorizontalSpeedAlongHeading(sim, next.HeadingX, next.HeadingZ);
        if (nextSpeed < next.ExitHints.MinExitSpeed)
            score += (next.ExitHints.MinExitSpeed - nextSpeed) * 600.0;
    }

    return score;
}
```

Update `MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs`:

```csharp
TransitionInputProfile profile;
if (physics.OnGround)
{
    profile = TransitionLookaheadEvaluator.ChooseGroundProfile(current, next, pos, physics, world);
}
else
{
    if (!current.ExitHints.AllowAirBrake)
        return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);

    profile = TransitionLookaheadEvaluator.ChooseAirProfile(current, next, pos, physics, world);
}

return profile switch
{
    TransitionInputProfile.Carry => TransitionBrakingDecision.CarryMomentum(current.PreserveSprint || next?.ExitHints.RequireJumpReady == true),
    TransitionInputProfile.Coast => TransitionBrakingDecision.Coast,
    TransitionInputProfile.Brake => TransitionBrakingDecision.Brake,
    TransitionInputProfile.AirHoldForward => TransitionBrakingDecision.CarryMomentum(current.PreserveSprint || next?.ExitHints.RequireJumpReady == true),
    TransitionInputProfile.AirRelease => TransitionBrakingDecision.Coast,
    TransitionInputProfile.AirBrake => TransitionBrakingDecision.Brake,
    _ => TransitionBrakingDecision.Coast
};
```

- [ ] **Step 4: Re-run focused mixed-route tests and the broader long-route contract group**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution.PathTimingContractTests.MixedTraverseAscendParkourDescend_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.MixedTraverseTurnParkourTurnTraverse_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.SpeedCarryRepeatedTraverseDescend_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.LongRoute_ExecutionStaysWithinBudget|FullyQualifiedName~Pathing.Execution.TransitionLookaheadEvaluatorTests|FullyQualifiedName~Pathing.Execution.TransitionBrakingPlannerTests" -v minimal
```

Expected: PASS for the new explicit regressions and no new failures in the broader lookahead/braking coverage.

- [ ] **Step 5: Commit the mixed-route braking fix**

```bash
git add MinecraftClient.Tests/Pathing/Execution/PathTimingContractTests.cs \
        MinecraftClient.Tests/Pathing/Execution/TransitionLookaheadEvaluatorTests.cs \
        MinecraftClient.Tests/Pathing/Execution/TransitionBrakingPlannerTests.cs \
        MinecraftClient/Pathing/Execution/TransitionLookaheadEvaluator.cs \
        MinecraftClient/Pathing/Execution/TransitionBrakingPlanner.cs
git commit -m "fix: align transition lookahead with next segment entry"
```

### Task 3: Remove Chained-Ascend Landing Stall In Live Staircases

**Files:**
- Modify: `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`
- Modify: `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`

- [ ] **Step 1: Add a failing chained-ascend convergence test**

Update `MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs`:

```csharp
[Fact]
public void AscendTemplate_ContinueStraight_CompletesWithoutSettlingToZeroSpeed()
{
    World world = FlatWorldTestBuilder.CreateStoneFloor(min: 338, max: 347);
    FlatWorldTestBuilder.ClearBox(world, 340, 80, 338, 347, 86, 342);
    FlatWorldTestBuilder.FillSolid(world, 341, 80, 339, 341, 80, 341);
    FlatWorldTestBuilder.FillSolid(world, 342, 81, 339, 342, 81, 341);
    FlatWorldTestBuilder.FillSolid(world, 343, 82, 339, 343, 82, 341);

    var current = new PathSegment
    {
        Start = new Location(341.5, 81, 340.5),
        End = new Location(342.5, 82, 340.5),
        MoveType = MoveType.Ascend,
        ExitTransition = PathTransitionType.ContinueStraight,
        ExitHints = new PathTransitionHints(1, 0, 0.08, double.PositiveInfinity, false, true, false, false, 8),
        PreserveSprint = true
    };
    var next = new PathSegment
    {
        Start = current.End,
        End = new Location(343.5, 83, 340.5),
        MoveType = MoveType.Ascend,
        ExitTransition = PathTransitionType.ContinueStraight,
        ExitHints = new PathTransitionHints(1, 0, 0.08, double.PositiveInfinity, false, true, false, false, 8),
        PreserveSprint = true
    };

    var template = new AscendTemplate(current, next);
    var physics = new PlayerPhysics
    {
        Position = new Vec3d(current.Start.X, current.Start.Y, current.Start.Z),
        DeltaMovement = new Vec3d(0.11, 0.0, 0.0),
        OnGround = true,
        MovementSpeed = 0.1f,
        Yaw = 270f,
        Pitch = 0f
    };

    var input = new MovementInput();
    TemplateState state = TemplateState.InProgress;
    int ticks = 0;
    for (; ticks < 30; ticks++)
    {
        input.Reset();
        Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
        state = template.Tick(pos, physics, input, world);
        if (state != TemplateState.InProgress)
            break;

        physics.ApplyInput(input);
        physics.Tick(world);
    }

    Assert.Equal(TemplateState.Complete, state);
    Assert.InRange(ticks, 1, 14);
    Assert.InRange(physics.DeltaMovement.X, 0.05, 0.20);
}
```

- [ ] **Step 2: Run the new unit test and the live long-route harness to confirm current failure**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter FullyQualifiedName~Pathing.Execution.GroundedTemplateConvergenceTests.AscendTemplate_ContinueStraight_CompletesWithoutSettlingToZeroSpeed -v minimal
source tools/mcc-env.sh && bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Expected: the unit test fails on tick count or residual speed, and the live harness still reports `same-move-ascend-staircase` over budget.

- [ ] **Step 3: Split ascend execution into takeoff, airborne, and landing handoff**

Update `MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs`:

```csharp
private enum Phase { Takeoff, Airborne, Landing }

private Phase _phase = Phase.Takeoff;
private bool _leftGround;
private int _landingTicks;

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

    switch (_phase)
    {
        case Phase.Takeoff:
            input.Forward = true;
            input.Sprint = true;
            if (physics.OnGround && dy > 0.1)
            {
                input.Jump = true;
                _phase = Phase.Airborne;
            }
            break;

        case Phase.Airborne:
            input.Forward = true;
            input.Sprint = true;
            if (!physics.OnGround)
                _leftGround = true;
            if (_leftGround && physics.OnGround)
            {
                _phase = Phase.Landing;
                _landingTicks = 0;
                goto case Phase.Landing;
            }
            break;

        case Phase.Landing:
            _landingTicks++;
            GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
            if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                return TemplateState.Complete;
            break;
    }

    if (_stuckTicks > 20 || _tickCount > 50)
        return TemplateState.Failed;

    return TemplateState.InProgress;
}
```

Update `MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs`:

```csharp
if (segment.MoveType == MoveType.Ascend
    && segment.ExitTransition == PathTransitionType.ContinueStraight
    && physics.OnGround
    && TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, segment.End)
    && !TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, segment.End))
{
    double exitSpeed = TemplateHelper.ProjectHorizontalSpeedAlongHint(physics, segment);
    return exitSpeed >= Math.Max(0.02, segment.ExitHints.MinExitSpeed);
}
```

- [ ] **Step 4: Re-run the ascend convergence test and the live long-route harness**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution.GroundedTemplateConvergenceTests.AscendTemplate_ContinueStraight_CompletesWithoutSettlingToZeroSpeed|FullyQualifiedName~Pathing.Execution.PathTimingContractTests.Scenario_ExecutionStaysWithinTimingBudget" -v minimal
source tools/mcc-env.sh && bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Expected: PASS for the new unit test and the live long-route suite, including `same-move-ascend-staircase`.

- [ ] **Step 5: Commit the ascend convergence fix**

```bash
git add MinecraftClient.Tests/Pathing/Execution/GroundedTemplateConvergenceTests.cs \
        MinecraftClient/Pathing/Execution/Templates/AscendTemplate.cs \
        MinecraftClient/Pathing/Execution/Templates/GroundedSegmentController.cs
git commit -m "fix: reduce chained ascend landing stalls"
```

### Task 4: Run The Full Regression Sweep And Stop On Any Residual Family

**Files:**
- No code changes required unless verification reveals a new, scoped defect

- [ ] **Step 1: Re-run all focused pathing execution tests**

Run:

```bash
dotnet test MinecraftClient.Tests/MinecraftClient.Tests.csproj --filter "FullyQualifiedName~Pathing.Execution" -v minimal
```

Expected: PASS with `0` failing pathing execution tests.

- [ ] **Step 2: Re-run the live accepted-route suites that previously failed**

Run:

```bash
source tools/mcc-env.sh && bash tools/test-pathing-jump-combos.sh 1.21.11-Vanilla
source tools/mcc-env.sh && bash tools/test-pathing-long-routes.sh 1.21.11-Vanilla
```

Expected: both scripts exit `0`, with no accepted-route replans and no contract-budget overruns.

- [ ] **Step 3: If any live case still fails, capture the exact family before doing more coding**

Use the existing contract report output already printed by the harnesses. Record:

```text
scenario id
total actual / max ticks
which segment index exceeded
whether the failure was replan, timeout, or budget overrun
```

Do not widen scope beyond:

- parkour landing recovery
- next-segment braking/lookahead
- chained ascend landing handoff

- [ ] **Step 4: End the plan cleanly once verification is green**

Run:

```bash
git status --short
```

Expected: only the intentional runtime/test edits from Tasks 1 through 3 remain. If verification is green and no extra follow-up patch was needed, do not create an empty commit. If verification exposes a new defect family, stop and write a separate scoped plan instead of slipping extra repair work into this one.

## Self-Review

Spec coverage check:

- repeated parkour failures map to Task 1
- mixed-route carry/brake failures map to Task 2
- live staircase ascend overrun maps to Task 3
- full xUnit and live verification maps to Task 4

Placeholder scan:

- no `TODO`, `TBD`, or “similar to above” placeholders remain
- each task includes concrete file paths, test code, commands, and commit steps

Type consistency:

- all next-segment-aware changes consistently use `PathSegment? next`
- named test helpers use `AssertScenarioWithinBudget`
- runtime fixes stay inside the already failing execution files
