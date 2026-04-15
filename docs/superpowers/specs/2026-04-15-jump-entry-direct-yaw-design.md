# Jump-Entry Direct Yaw Design

## Context
Recent pathing work exposed a consistent execution cost in sterile test worlds: jump-capable segments can spend several ticks rotating in place before they are willing to commit to the action. This is most visible in short parkour jumps and ascend chains started from opposite yaw, where the path is correct and no replan should happen, but execution still burns ticks on gradual heading convergence.

The current implementation uses `TemplateHelper.SmoothYaw(...)` across all movement templates. That smooth turn is not purely visual. Forward and back inputs are resolved using the current `physics.Yaw`, so intermediate yaw values change real acceleration, timing, and landing state. Because of that, replacing all yaw smoothing with snap rotation would be a behavior change across the whole execution system, not just a cosmetic cleanup.

The goal of this change is narrower: remove unnecessary yaw convergence cost only in states where the controller is explicitly trying to become jump-ready, while preserving current grounded braking, descend timing, climb centering, and final-stop settling behavior elsewhere.

## Requirements
- Remove avoidable yaw-convergence tax from jump-entry states.
- Preserve the current hard requirement of `0 replan` in sterile live test worlds.
- Preserve planner behavior and existing path contracts.
- Keep pitch smoothing unchanged.
- Do not change normal `Walk`, `Descend`, `Climb`, or grounded final-stop semantics in the first pass.
- Keep the existing guarded `PrepareJump` handoff behavior intact.

## Approaches

### 1. Global snap yaw in all templates
Replace all `SmoothYaw(...)` calls with direct target yaw assignment.

Pros:
- Simplest implementation model.
- Removes all rotation latency.

Cons:
- Changes grounded traversal, descent lip approach, climb centering, and turn braking at once.
- Would invalidate current assumptions in templates that use heading penalty and gradual exit-heading bias as part of real motion control.
- Too broad for the current bug and too risky for the current regression surface.

### 2. Phase-scoped direct yaw only in jump-entry states
Keep smoothing by default, but explicitly snap yaw in states whose sole purpose is to prepare for a jump.

Pros:
- Targets the observed cost directly.
- Preserves current non-jump motion semantics.
- Matches the theoretical intent: if the state is already waiting for jump-ready alignment, gradual turning is wasted time.

Cons:
- Requires template-specific gating instead of one global rule.

This is the recommended approach.

### 3. Faster smoothing instead of snap
Raise `MaxYawStepPerTick` or add a faster smoothing mode for some templates.

Pros:
- Smaller conceptual jump from the current implementation.

Cons:
- Keeps the same state model and the same basic failure mode, only with smaller delays.
- Makes behavior harder to reason about because "how fast is fast enough" becomes another tuning problem.

This is not recommended for the first pass.

## Design

### Scope boundary
The first pass should only change yaw behavior in jump-entry states:

- `SprintJumpTemplate` while approaching takeoff
- `AscendTemplate` while aligning for jump commitment
- `GroundedSegmentController` when freezing in place for `PrepareJump`
- `WalkTemplate` only when the segment is a grounded jump-entry segment with `ExitHints.RequireJumpReady == true`

The first pass should not change:

- ordinary `WalkTemplate` traversal, turn, or final-stop control
- `DescendTemplate`
- `ClimbTemplate`
- air control during jump flight
- grounded landing recovery and final-stop braking after a jump

### Yaw policy model
Add an explicit notion of yaw alignment mode at the helper layer, with two behaviors:

- `Smooth`
- `Snap`

The helper should centralize the policy so templates do not open-code direct `physics.Yaw = targetYaw` in unrelated ways. Pitch should remain smooth.

The implementation does not need a broad architecture. A small helper API is enough, for example:

- `AlignYaw(current, target, mode)`
- or a narrowly named helper such as `SnapYaw(target)`

The key contract is that templates opt into snap only when they are inside the jump-entry boundary above.

### Sprint jump behavior
`SprintJumpTemplate` should use direct yaw alignment during `Phase.Approach`.

Why:
- This phase already treats heading alignment as a hard precondition for jumping.
- When started from opposite yaw, smooth turning creates pure startup tax before the jump can begin.
- For short `FinalStop` jumps, this cost is disproportionately large relative to route time.

Effect:
- `yawAligned` becomes immediately satisfiable once the state ticks.
- The template can begin acceleration or jump commitment on the same tick rather than waiting several ticks for smooth convergence.
- The recent short-jump air-brake latch remains unchanged and still handles landing-side overshoot.

### Ascend behavior
`AscendTemplate` should use direct yaw alignment in the pre-jump phase, except for the existing grounded prepare-jump handoff carveout.

Why:
- Ascend already waits for heading readiness before jumping.
- The current opposite-yaw staircase spin is the same problem as short parkour: a jump state paying a gradual-turn tax before action.
- The existing `groundedPrepareJumpHandoff` guard must still prevent double ownership of yaw at the moment control is handed to the next jump-ready segment.

Effect:
- Opposite-yaw ascend starts become immediate.
- Existing handoff protection remains intact.

### Grounded prepare-jump freeze
When `GroundedSegmentController` enters its freeze-for-turn branch for `PrepareJump`, it should directly align to exit heading rather than smoothing.

Why:
- In this branch, movement is already frozen.
- There is no benefit to burning extra ticks on a smooth turn while stationary.
- This is the cleanest place to remove residual turn latency for grounded jump handoffs.

### WalkTemplate jump-entry alignment
`WalkTemplate` should keep smooth yaw for normal traversal. It should switch to direct yaw only for grounded segments that are explicitly preparing for a jump:

- `ExitTransition == PrepareJump`
- `ExitHints.RequireJumpReady == true`

Why:
- This is still part of the jump-entry pipeline, not ordinary path following.
- Snap yaw here allows the segment to convert remaining forward ticks into the correct heading immediately, which better preserves exit-speed intent for the next jump.
- Restricting this to jump-ready segments avoids changing ordinary traversal and braking behavior.

### Non-goals
This change does not attempt to:

- remove all yaw smoothing from execution
- re-tune descend, climb, or landing-recovery behavior
- change planner costs or move admissibility
- rewrite transition braking around direct yaw assumptions

## Expected effects

### Positive effects
- Short opposite-yaw parkour and ascend starts should lose their upfront turn tax.
- Repeated jump-entry chains should start more promptly.
- Jump-ready handoff states should stop wasting ticks while frozen.

### Risks
- Jump-entry segments may now redirect horizontal acceleration more abruptly.
- A few jump-entry timing expectations may improve by 1 to 5 ticks and need contract updates only if they are stricter than reality.

These risks are acceptable because the affected scope is intentionally limited to states whose semantics are already "become jump-ready now."

## Validation
- Unit tests:
  - `SprintJumpTemplate_TwoBlockGap_FinalStop_CompletesFromOppositeYawWithinTwentyTicks`
  - `AscendTemplate_PrepareJump_CompletesFromOppositeYawWithinTwentyTicks`
  - existing grounded convergence and sprint-jump scenario suites
- Contract tests:
  - planner contracts remain unchanged
  - timing contracts are rerun and only updated if fresh evidence shows stable, improved timings
- Live harnesses, sequentially:
  - `tools/test-pathing-jump-combos.sh`
  - `tools/test-pathing-long-routes.sh`
- Success criteria:
  - no new replans in sterile routes
  - no regression in planner-selected long-jump chains
  - short opposite-yaw jump regressions stay green

## Delivery order
1. Add the yaw-policy helper.
2. Apply snap yaw to `SprintJumpTemplate` approach.
3. Apply snap yaw to `AscendTemplate` pre-jump alignment.
4. Apply snap yaw to `GroundedSegmentController` prepare-jump freeze.
5. Apply gated snap yaw to `WalkTemplate` jump-entry alignment only.
6. Rerun focused unit suites.
7. Rerun live jump-combo and long-route harnesses sequentially.

## Open questions
- None for the first pass. `Descend` and `Climb` are explicitly deferred until there is evidence that their current smoothing is the next limiting factor.
