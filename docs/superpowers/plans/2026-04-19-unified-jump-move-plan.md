# Unified Jump Move Refactor

Date: 2026-04-19  
Branch: `pathing/jump-entry-direct-yaw`

## Problem

MCC's A* uses a hard-coded enumeration of ~220 `IMove` instances covering the
"jump family" (Traverse, Diagonal, Ascend, DiagonalAscend, DiagonalDescend,
Parkour, SidewallParkour). Each geometric variant is a separate IMove subclass
with its own `Calculate` method that re-implements the same physics checks
(head clearance, run-up, flight path, landing clearance, gap check). Symptoms:

1. **Drift**: the same physics rule is implemented in 3-4 places. A fix to
   `HasDominantAxisRunUp` does not automatically propagate to `HasRunUp`.
2. **Missing combinations silently become "impossible"**: until this week, the
   planner had no `MoveParkour(dx=1, dz=2, yDelta=+1)` entry, so the diagonal
   ascending jump (upper arrow in the user's pyramid image) was rejected
   entirely even though the physics allow it.
3. **Slow expansion**: every A* node runs 220 feasibility checks and a lot of
   them are obviously irrelevant for that position (e.g. sidewall checks when
   there is no wall anywhere near the player).

Baritone does not have this problem: `MovementParkour` is a single class that
dynamically probes reachable landings per direction, and its 8 `Moves` enums
cover the entire movement space.

## Goal

Bring MCC's jump family to a single parameterized move class with one unified
feasibility engine, then evolve to Baritone-style dynamic neighbor expansion.

## Scope

**In scope (unified under `MoveJump` + `JumpDescriptor`)**:

- `MoveTraverse` (dy=0 cardinal)
- `MoveDiagonal` (dy=0 corner)
- `MoveAscend` (dy=+1 cardinal)
- `MoveDiagonalAscend` (dy=+1 corner)
- `MoveDiagonalDescend` (dy=-1 corner)
- `MoveParkour` (dy ∈ {+1, 0, -1, -2}, horiz up to 5 cardinal / sqrt(10) diag)
- `MoveSidewallParkour` (parkour + inner wall requirement)

**Out of scope (stay as their own classes)**:

- `MoveDescend` — dynamic variable-depth fall with water/ladder grab logic
- `MoveSprintDescend` — dynamic landing depth
- `MoveClimb` — ladder/vine vertical movement
- `MoveFall` — pure free fall

These are "descent family" and have a different feasibility model (unknown
landing y, hazard scanning). Future refactor can unify them under a
`MoveFallToLanding` family but that is a separate effort.

## Design

### Data

```csharp
public readonly record struct JumpDescriptor(
    int XOffset,
    int ZOffset,
    int YDelta,
    JumpFlavor Flavor);

public enum JumpFlavor
{
    Walk,         // dy=0, 1 block move, no jump (Traverse/Diagonal)
    Step,         // dy=±1, 1 block move with jump or step-off (Ascend/DiagDescend/DiagAscend)
    SprintJump,   // horiz >= 2 with or without dy (Parkour)
    Sidewall,     // SprintJump + inner-wall clearance (SidewallParkour)
}
```

The descriptor fully describes any jump-family move. `MoveType` (Traverse,
Diagonal, Ascend, Descend, Parkour) is derived from `(Flavor, dy, horiz)` so
downstream consumers (templates, cost tables) keep working.

### Evaluator

`JumpFeasibility.Evaluate(ctx, x, y, z, desc, ref result)` is the single source
of truth. It dispatches on `desc.Flavor` but shares the following primitives:

1. **Guards**: `AllowParkour`, `AllowParkourAscend`, `MaxFallHeight`, `CanSprint`.
2. **Profile check**: geometry falls in the valid range for this flavor.
3. **Head clearance at start**: `y+2` always, plus `y+3` if ascending sprint jump.
4. **Standing material**: reject climbable (ladder/vine) takeoffs.
5. **Destination**: floor solid, body passable, head passable, no hazards.
6. **Run-up**: cold-start reach tables plus prepared-entry lookup
   (`EntryPreparationState`). This replaces both `HasRunUp` and
   `HasDominantAxisRunUp`.
7. **Flight path**: cardinal straight-line column sweep or diagonal
   proportional-step sweep. Needs `y+3` clearance only when ascending.
8. **Wall** (Sidewall only): inner wall presence + outer clearance + arc span.
9. **Gap check**: reject when a direct walk would work.
10. **Cost**: unified sprint/walk cost × horizontal distance + penalties.

### Step 1 — introduce evaluator, existing classes delegate

No behavior change. Each of the 7 existing classes has `Calculate` shrunk to
a single `JumpFeasibility.Evaluate(...)` call with a descriptor derived from
its constructor args. All existing tests pass with the same pass/fail counts.

### Step 2 — single `MoveJump` class

Delete the 7 subclasses. `AStarPathFinder.BuildDefaultMoves` emits
`MoveJump(descriptor)` instances from a declarative list. Tests that instantiate
the old classes are updated to instantiate `MoveJump` with the equivalent
descriptor (or use factory helpers like `MoveJump.Parkour(dx, dz, dy)`).

Templates (`SprintJumpTemplate`, `AscendTemplate`, `SidewallParkourController`,
etc.) dispatch on `Flavor` / `MoveType` already, so they need no changes.

### Step 3 — dynamic neighbor expansion

`AStarPathFinder.Calculate` currently loops over `_allMoves` for every popped
node. Replace with an `IMoveExpander[]` where each expander yields neighbors
on demand:

```csharp
public interface IMoveExpander
{
    void Expand(CalculationContext ctx, int x, int y, int z, Action<MoveResult, MoveType, JumpDescriptor?> emit);
}
```

`JumpExpander.Expand` iterates the 4 cardinals + 4 diagonals. For each
direction, it asks "what is the furthest reachable landing?" by scanning from
max distance down to 1, emitting the first feasible result (the A* cost model
already disprefers short jumps when long jumps work). This yields ~8-16
neighbors per node instead of 220.

Baritone-style partial-path coefficients (`bestSoFar[6]`) are a separate
improvement; not bundled here.

## Risk / rollback

- Step 1 is behavior-preserving and easy to revert (delete evaluator, restore
  the old `Calculate` bodies from git).
- Step 2 deletes code; revert means restoring from git.
- Step 3 changes the A* main loop. Keep the old `BuildDefaultMoves` path behind
  an `_useDynamicExpansion` flag so we can A/B test in live `tools/test-parkour.py`
  runs before deleting the old path.

## Test strategy

- `dotnet test MinecraftClient.Tests` after each step. Baseline is 21 failing
  tests (all pre-existing on this branch). Target: exact same failure set at
  each checkpoint.
- At Step 2 end, verify upper-arrow scenario (this task's motivating bug) still
  plans correctly.
- At Step 3 end, run `tools/test-parkour.py` linear + sidewall + ceiling
  scenarios on a real server.
