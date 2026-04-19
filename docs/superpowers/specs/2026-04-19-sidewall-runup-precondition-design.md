# Sidewall Runup Precondition Planning

## Context

`sidewall` parkour still has a static-entry gap in the planner: some first-jump sidewall profiles are only physically reliable when the player backs up first, rebuilds sprint momentum along the dominant axis, returns to the launch origin, and then jumps. The current pathfinder cannot represent that distinction because it keys search nodes by position only and only exposes `PreviousMoveType` to move feasibility.

The user explicitly rejected solving this with a runtime-only template hack. The required behavior is planner-driven: if a sidewall jump has an entry-momentum precondition, the produced path should contain explicit ground segments for the setup action, and accepted executions must still complete at `replan_count=0` and `turn_stall_count=0`.

## Requirements

- Keep the current `linear` matrix green. Do not loosen generic linear parkour rules to make sidewall pass.
- Make the setup action planner-visible. The final path must contain explicit ground segments before the first jump instead of hiding the behavior inside `SprintJumpTemplate` or `SidewallParkourController`.
- Keep scope narrow in phase 1. Only static-entry first-jump sidewall profiles may use the new setup mechanism.
- Do not hardcode case ids. Activation must be geometry/profile driven.
- Preserve the current carry-in path semantics. If a sidewall jump is already entered with valid carry, the planner must not inject a setup loop.
- In the current no-entity-collision environment, accepted sidewall cases must still run with `replan_count=0` and `turn_stall_count=0`.

## Non-Goals

- No generic “find any staging area behind me” feature in phase 1.
- No new runtime-only sidewall template family.
- No expansion to `neo`, `ceiling`, or generic `Parkour` in this change.
- No attempt to fix the unrelated `.NET` baseline failures outside the targeted sidewall/linear guard surface.

## Design

### Scope and activation

Phase 1 adds a narrow planner-side precondition for static-entry sidewall jumps that need extra runway. Based on the latest verified matrix, the first activation predicate should be:

- `ParkourProfile.Sidewall`
- no carry-in (`PreviousMoveType` is not `Parkour` or `Descend`)
- descending sidewall (`yDelta == -1`)
- dominant horizontal distance `== 5`

This is intentionally narrow because the current verified live mismatch set is concentrated there and `linear` is already fully green. The predicate is profile-based, not case-id based, so it still follows geometry rather than scenario names.

### Search-state extension

The planner needs an extra discrete state to distinguish:

- standing at the launch origin with no setup
- moving backward to build setup runway
- moving forward back toward the launch origin
- standing at the launch origin after completing the required setup

Add two new core types under `MinecraftClient/Pathing/Core/`:

- `EntryPreparationKind`
- `EntryPreparationState`

`EntryPreparationState` should carry:

- preparation kind, phase, and whether the state is empty
- launch origin (`OriginX`, `OriginY`, `OriginZ`)
- dominant forward axis (`ForwardX`, `ForwardZ`)
- required setup length in blocks
- completed backward steps
- completed return steps

`PathNode` gets an `EntryPreparation` field. `CalculationContext` gets `CurrentEntryPreparation` so move feasibility can inspect the current node’s preparation state during expansion. `AStarPathFinder` must stop keying nodes by packed position only and instead key by `position + entry preparation state`.

This is the critical design point: explicit path segments alone are not enough. Without a search-state distinction, A* would return to the same origin block and still evaluate the jump as a plain zero-entry sidewall jump.

### How setup appears in the path

The setup action should not introduce a new runtime `MoveType`. The visible path should be composed of ordinary ground moves:

1. one or more `Traverse` segments backward along the dominant axis
2. the same number of `Traverse` segments forward along the dominant axis
3. the `Parkour` segment from the original launch block

That keeps the execution layer simple. `PathSegmentBuilder` already marks a ground segment whose next segment is `Parkour` as `PrepareJump`, so the last forward traverse segment will naturally receive jump-ready transition hints without adding a new template concept.

### Starting, advancing, and clearing setup state

The setup state is seeded and advanced in the pathfinder, not inside the movement templates.

#### Starting setup

When A* is expanding a node with empty `EntryPreparationState`, and it considers a one-block `Traverse` that moves exactly opposite the dominant axis of a sidewall jump that requires setup from the current origin, the neighbor should receive a seeded `EntryPreparationState`:

- origin set to the current block
- forward axis set to the jump’s dominant direction
- required steps set from the profile helper
- backward steps initialized to `1`
- return steps initialized to `0`

This keeps the setup path explicit because the first action is still an actual ground move.

#### Advancing setup

While the node is in a setup state:

- a same-level `Traverse` exactly opposite the forward axis increments backward progress until the required count is reached
- after backward progress is full, a same-level `Traverse` exactly along the forward axis increments return progress
- when return progress reaches the required count, the destination must be the original launch origin and the state becomes “prepared”

#### Clearing setup

Any other move clears the setup state immediately:

- `Diagonal`, `Ascend`, `Descend`, `Fall`, `Climb`, `Parkour`
- same-level `Traverse` in the wrong direction
- any move that changes Y
- any return that overshoots the launch origin

This keeps the mechanic narrow and predictable. The phase-1 behavior is “straight backward, then straight forward, then jump,” not a general-purpose staged maneuver planner.

### Sidewall admissibility split

`ParkourFeasibility` should stop treating this as a simple yes/no runway check. It needs to distinguish:

- physically impossible profile
- directly admissible profile
- profile admissible only after explicit setup

Add a helper such as `TryGetRequiredStaticEntryRunupSteps(...)` that returns `0` for direct-entry sidewall jumps and a positive step count for setup-required profiles. In phase 1, this returns `2` for the narrow long-descend sidewall predicate above and `0` otherwise.

`MoveSidewallParkour` then uses the split as follows:

- if the profile does not require setup, keep the current dominant-axis admissibility logic
- if the profile requires setup, reject unless `CurrentEntryPreparation` is a prepared sidewall setup for the same launch origin, same forward axis, and same required length
- if carry-in is present, bypass setup entirely and keep the current carry behavior

### Execution impact

Execution changes should be avoided in phase 1. The produced path now contains explicit ground runway segments before the first sidewall jump, which means the existing transition logic should already hand the last forward traverse segment a `PrepareJump` exit transition.

Do not modify `SprintJumpTemplate` or `SidewallParkourController` unless targeted tests prove the new path shape causes a fresh runtime regression. The primary fix is planner-side.

## Testing Strategy

### Targeted .NET regressions

Add or update tests in:

- `MinecraftClient.Tests/Pathing/Moves/MoveSidewallParkourTests.cs`
- `MinecraftClient.Tests/Pathing/Execution/LivePathingRegressionTests.cs`
- `MinecraftClient.Tests/Pathing/Execution/PathSegmentManagerTests.cs`

The key assertions are:

- static-entry long-descend sidewall rejects without prepared setup
- A* for that profile succeeds and prepends explicit `Traverse` setup segments before the first `Parkour`
- the last setup traverse ends back on the original launch block
- the same sidewall chain still executes with `ReplanCount == 0`
- the linear planner path shape remains unchanged, with no inserted setup segments

### Live harness

Use `tools/test-parkour.py` in split runs to avoid stop-at-first-failure masking:

- `--filter sidewall/descend`
- `--filter sidewall/flat`
- `--filter sidewall/ascend`
- `--filter linear`

Then run the full matrix once after the targeted runs pass.

## Risks and mitigations

- Search-space growth: mitigated by activating setup only for one narrow sidewall profile and by clearing the preparation state on any non-axis-aligned move.
- Linear regression: mitigated by not touching `MoveParkour` or generic linear admissibility in phase 1.
- Runtime drift despite planner fix: mitigated by keeping the new path shape limited to ordinary `Traverse` plus existing `Parkour`, then proving `0 replan` and `0 turn stall` with targeted tests and live harness evidence.

## Validation

- `dotnet test MinecraftClient.Tests --filter "FullyQualifiedName~MoveSidewallParkourTests|FullyQualifiedName~LivePathingRegressionTests|FullyQualifiedName~PathSegmentManagerTests"`
- `dotnet test MinecraftClient.Tests --filter "FullyQualifiedName~Linear"`
- `python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter sidewall/descend`
- `python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter sidewall/flat`
- `python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter sidewall/ascend`
- `python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla --filter linear`
- `python3 tools/test-parkour.py --parallel 6 --version 1.21.11-Vanilla`
