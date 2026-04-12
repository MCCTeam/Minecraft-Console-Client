# Slab Support Design, Scheme Two

Date: 2026-04-12
Status: Approved for implementation planning

## Summary

This design adds basic slab support to the current A* pathfinder without introducing half-block nodes.

The goal is practical rather than perfect: make normal routing work across slabs, allow takeoff from slabs, allow landing on slabs when the fall is still within the current safe range, and keep the search space close to what it is today.

The key constraint is that the current pathfinder stores integer `(x, y, z)` nodes and the execution layer still expects block-center waypoints. That is staying in place for this iteration.

## What This Change Should Cover

- Walking across bottom slabs, top slabs, and full blocks
- Moving up and down neighboring `0.5` block height differences
- Starting jumps from slabs
- Landing on slabs when the effective fall height is safe
- Keeping current parkour and descend behavior stable instead of trying to make slab parkour exhaustive

## What This Change Will Not Cover

- True half-block path nodes
- A general solution for all non-full-block surfaces such as stairs, carpets, snow layers, trapdoors, and similar terrain
- Full slab-aware parkour optimization
- A new cost model tuned around half-block travel times

## Current Problem

The physics layer can already step up `0.5` blocks and collide with slab shapes correctly. The planning layer cannot. It still treats movement as if every valid floor is a full block surface.

That mismatch shows up in three places:

- `MoveHelper` still answers most walkability questions at the `Material` level.
- The move set assumes floor height changes happen in whole blocks.
- Path segments still convert nodes to `(x + 0.5, y, z + 0.5)` and do not carry surface-height metadata.

Because of that, basic slab terrain is either invisible to the planner or handled inconsistently.

## Core Approach

### Keep Integer Nodes

The pathfinder will keep integer `(x, y, z)` nodes. This avoids doubling the vertical state space and keeps the current move graph shape.

The cost is that slabs must be represented indirectly. That is acceptable for this iteration because the target is reliable routing, not full geometric precision.

### Add Surface Profiles

Planning will stop asking only "is this material solid?" and instead ask "what standing surface does this block column provide?"

Each relevant block column will map to a small surface profile:

- `None`
- `FullBlock`
- `TopSlab`
- `BottomSlab`

For the first implementation, the source of truth is `BlockShapes`. Slabs already have distinct collision boxes there, including top and bottom variants.

The profile also exposes the standing surface top Y relative to the block base:

- `FullBlock` -> `1.0`
- `TopSlab` -> `1.0`
- `BottomSlab` -> `0.5`
- `None` -> not standable

This gives the planner enough information to answer the questions it actually needs:

- can the player stand here
- how high is the standing surface
- what is the effective fall height if the player lands here

### Use an Alias-Y Model

Nodes remain integer Y values even when the actual standing surface is at `.5`.

The aliasing rule is:

- a bottom slab standing surface inside block `(x, y - 1, z)` is still represented by node `y`
- the node Y means "feet are in this logical cell", not "feet are exactly on integer Y"

This preserves compatibility with the current pathfinder and avoids widening the state space.

## Movement Rules

### Traverse And Diagonal Movement

Flat movement will become "same effective standing height" movement, not just "same integer Y" movement.

These cases should be allowed:

- full block to full block
- full block to top slab
- top slab to full block
- bottom slab to bottom slab

These cases should not be forced through the flat move set:

- full block to bottom slab
- bottom slab to full block

Those are `-0.5` and `+0.5` height changes and should be handled explicitly.

### Half-Step Moves

Add dedicated half-step moves:

- `MoveHalfAscend`
- `MoveHalfDescend`

First implementation scope:

- cardinal half-step moves are included
- diagonal half-step moves are out of scope

These moves are for adjacent columns whose standing surface differs by `0.5`.

Execution for half-step moves must not press jump. The physics engine should handle them as a step-up or controlled walk-down.

### Full-Block Ascend And Descend

Existing `MoveAscend`, `MoveDescend`, `MoveFall`, and `MoveSprintDescend` remain in place, but their landing and clearance checks become surface-aware.

The main difference is that the destination surface is no longer assumed to be exactly one block high relative to the block base.

### Parkour

Parkour is not getting a full slab rewrite in this iteration.

The planner should:

- allow takeoff from a slab if the start surface is valid
- allow landing on a slab if the effective fall and required clearance are valid
- avoid adding new slab-specific parkour move families in this change

This keeps the change small enough to validate.

## Safe Landing Rule For Bottom Slabs

Bottom slabs should be allowed as fall destinations when the effective fall height is still within the current safe fall limit.

This rule replaces the earlier blanket rejection.

### Definition

Use:

`effectiveFallHeight = startSurfaceTopY - landingSurfaceTopY`

with both heights measured in world coordinates.

Given the current `MaxFallHeight = 3.0`, these examples should hold:

- bottom slab to a bottom slab three blocks lower: allowed, because `0.5 -> -2.5` is an effective fall of `3.0`
- full block to a bottom slab `2.5` blocks lower: allowed
- full block to a bottom slab `3.5` blocks lower: rejected

This matches the behavior we want:

- support realistic slab landings
- keep the current safety ceiling
- avoid special casing by integer block count alone

### Cost Model

The fall cost table is still integer-based. For half-block fall distances, the first iteration will round up when consulting the fall-cost table.

Examples:

- `2.0` -> use `FallCost(2)`
- `2.5` -> use `FallCost(3)`
- `3.0` -> use `FallCost(3)`

This is slightly conservative, which is fine for now. It avoids pretending the path is cheaper than the current planner knows how to represent.

## Execution Layer Changes

The execution layer needs a small amount of slab metadata so completion checks do not rely on loose tolerances alone.

`PathSegment` should carry enough information for templates to know whether the start or end uses a half-height standing surface. A minimal version is:

- start surface offset
- end surface offset

with offsets of `0.0` or `-0.5` relative to the logical node Y.

This metadata is only for execution and verification. It should not turn into a new search-state dimension.

### New Templates

Add:

- `HalfAscendTemplate`
- `HalfDescendTemplate`

Behavior:

- face the target
- move forward
- do not sprint in the first implementation
- do not press jump
- use tighter completion checks that include the expected end surface offset

Existing templates may also need small updates so slab takeoff and slab landing do not cause false stuck detection or early completion.

## File-Level Impact

Expected touch points:

- `MinecraftClient/Pathing/Moves/MoveHelper.cs`
- `MinecraftClient/Pathing/Core/CalculationContext.cs`
- `MinecraftClient/Pathing/Moves/Impl/MoveTraverse.cs`
- `MinecraftClient/Pathing/Moves/Impl/MoveDiagonal.cs`
- `MinecraftClient/Pathing/Moves/Impl/MoveAscend.cs`
- `MinecraftClient/Pathing/Moves/Impl/MoveDescend.cs`
- `MinecraftClient/Pathing/Moves/Impl/MoveFall.cs`
- `MinecraftClient/Pathing/Moves/Impl/MoveSprintDescend.cs`
- new half-step move files under `MinecraftClient/Pathing/Moves/Impl/`
- `MinecraftClient/Pathing/Core/AStarPathFinder.cs`
- `MinecraftClient/Pathing/Execution/PathSegment.cs`
- new half-step template files under `MinecraftClient/Pathing/Execution/Templates/`
- template factory / executor wiring

## Performance Expectations

This design should not materially expand the search space because nodes stay integer-based.

The expected overhead comes from:

- extra `BlockShapes` lookups during move validation
- a few more comparisons per move
- a small number of extra move types

That is a constant-factor increase, not a state explosion.

The main thing to avoid is introducing separate `.0` and `.5` Y states into the open set. This design does not do that.

## Risks

### Alias-Y Drift

The biggest risk is mismatch between logical node Y and the player's actual surface height. If the segment metadata is too thin, templates may oscillate, finish too early, or trigger unnecessary replans.

### Clearance Mistakes

A bottom slab under a low ceiling is the easiest place to get this wrong. Surface-aware standability is not enough by itself. The move checks still need to verify body and head clearance against the actual shapes involved.

### Scope Creep

Once slab support works, stairs and snow layers will look tempting. They are out of scope for this change.

## Test Plan

### Planner-Level Cases

Build focused tests around these scenarios:

- full -> bottom slab
- bottom slab -> full
- bottom slab -> bottom slab
- full -> top slab
- top slab -> full
- slab takeoff for jump and parkour moves
- solid landing on top slab
- solid landing on bottom slab with effective fall `<= 3.0`
- solid landing on bottom slab with effective fall `> 3.0`
- slab under a low ceiling

### Physics And Execution Checks

Use `tools/sim_jump_reach.py` to validate the intended reachability envelope and then run local server checks for:

- `/goto` across mixed full-block and slab terrain
- repeated slab transitions without replan loops
- takeoff from slab to slab and slab to full block
- landing on bottom slabs at `2.5` and `3.0` effective fall distances
- rejection of `3.5` effective-fall bottom-slab landings

## Implementation Notes

The first implementation should favor readable helper code over micro-optimizing shape checks. If the new helper becomes hot, caching can be added after behavior is stable.

The safest rollout order is:

1. Add surface-profile helpers
2. Update landing logic and safe-fall logic
3. Add half-step moves and templates
4. Expand move coverage only after the basic route cases are stable

## Decision

Proceed with scheme two:

- integer nodes stay
- slab surfaces are modeled through shape-aware helpers
- bottom slab landings are allowed when effective fall height stays within the existing safe limit
- no attempt is made to solve the general non-full-block terrain problem in this pass
