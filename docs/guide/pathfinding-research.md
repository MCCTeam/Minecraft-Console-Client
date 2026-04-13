# Pathfinding Research: Blip-Up Mechanism and Jump Mechanics

## Background

During research for the MCC pathfinding rewrite, we investigated advanced parkour
mechanics in Minecraft Java Edition to determine which movement patterns the new
system should support.

## Blip-Up Mechanism

### What is it

Blip-Up is a physics exploit caused by the **Step-Assist (Stepping)** system
interacting incorrectly with airborne landing. It allows the player to "land"
above ground level and immediately jump again, achieving heights that would
normally be impossible.

### How Step-Assist works (normal case)

When the player walks into an obstacle shorter than 0.6 blocks while on the
ground, the game automatically steps the player over it:

1. Reset the player bounding box to the **position at the start of the tick**
2. Raise the bounding box up by at most 0.6 blocks
3. Move the bounding box horizontally (X axis first, then Z)
4. Lower the bounding box back down by at most 0.6 blocks
5. Compare with the non-stepped movement; keep whichever achieves greater
   horizontal distance

### How Blip-Up exploits it

The critical flaw: step 1 resets the bounding box to the position at the
**start of the tick**, not after landing. If the player was airborne at the
start of the tick but lands during that tick's collision resolution, the
stepping procedure initiates from the **airborne position** (higher than
the ground). The bounding box may not get lowered enough, causing the
player to "land" mid-air while `onGround` is set to `true`.

Since `onGround = true`, the player can immediately jump again from this
elevated position.

### Requirements

- Negative vertical velocity (falling or descending from a jump arc)
- Land next to a wall of relatively low height (lower than the player's
  remaining fall distance on that tick)
- The wall triggers step-assist even though it cannot be directly stepped onto

### Observed test case

The following sequence was observed in Bedrock Edition testing (which has
similar but not identical stepping behavior):

1. Player sneaks to the edge of a purple wool block, facing a wall made of
   diamond blocks. The wall extends 3 blocks outward from the landing block.

2. Player positions camera slightly outward and holds forward while sneaking,
   reaching the extreme edge of the block.

3. Player jumps forward. On the landing tick, they collide with both the
   purple wool surface and the adjacent wall.

4. The stepping system triggers at the airborne position, causing the player
   to "land" slightly above the actual surface. `onGround` becomes true.

5. The player immediately jumps again from this elevated position, gaining
   enough height to reach the top of the wall.

Test images show the player at position (-1, 197, 3) initially, climbing
to (-1, 198, 6) and (-1, 197, 7) via two consecutive jumps where normally
only one jump from ground level would not reach the wall top.

### Version differences

| Version range | Blip-Up status | Notes |
|---|---|---|
| Pre-1.8 | Works (with caveats) | MC-3337 bug affects stepping under ceilings |
| 1.8.0 | Works | Always lowers bounding box by 0.6b; grinding impossible |
| 1.8.1 - 1.13.x | Works | Each consecutive blip adds ~0.104 blocks height |
| 1.9 - 1.13.x | Works (slightly different) | Jump height increased to 1.252 (from 1.249); each blip adds ~0.121 |
| 1.14+ | **Patched** | Bounding box now lowers to `playerHeight - verticalSpeed` instead of fixed 0.6b |
| 1.14+ | "Normal blip" still works | Standard step-assist onto low obstacles is intentional behavior |

### Related mechanics

- **Jump Cancel**: stepping applied to jumping motion instead of landing;
  cancels upward momentum on a slab/stair or ceiling, allowing rapid re-jump
  for momentum gain (2-tick cycle under trapdoor ceiling)
- **Grinding**: chaining jump cancels to accelerate; "stair grinding" on
  stairs or "ceiling grinding" under a low ceiling
- **Normal Blip**: intended behavior where stepping lets you walk onto an
  adjacent block of modest height difference

### Implications for MCC pathfinding

1. **1.14+ servers (majority of modern servers)**: Blip-Up is patched; the
   pathfinding system does **not** need to account for it. Standard step-up
   (0.6b max) and normal jump height (1.252b) define the reachable space.

2. **Pre-1.14 servers**: if Blip-Up support is desired, the physics engine's
   `CollisionDetector.Collide()` step-up logic must match the version-specific
   behavior precisely. This is deferred to a later phase.

3. **Jump Cancel / Grinding**: these mechanics could theoretically enable
   faster momentum gain, but they require version-specific ceiling heights
   and are considered advanced; deferred to later phases.

4. **Initial scope**: the pathfinding rewrite focuses on standard jump
   physics (1.14+), covering flat jumps, sprint jumps (2-4 blocks),
   ascend/descend, and neo-style wall jumps that are achievable within
   vanilla 1.14+ physics constraints.

## Jump Reachability Simulation Results

The simulation script `tools/sim_jump_reach.py` models vanilla 1.14+ physics
tick-by-tick to determine which jump destinations are reachable. All constants
are sourced from `PhysicsConsts.cs` and match vanilla 1.21.x.

Run with: `python3 tools/sim_jump_reach.py --verbose`

### Key Physics Constants

| Parameter | Value | Source |
|---|---|---|
| Player width | 0.6m | Entity bounding box |
| Player height | 1.8m | Standing pose |
| Base jump power | 0.42 m/tick | LivingEntity.jumpFromGround |
| Sprint jump horizontal boost | +0.2 m/tick | Player sprint bonus |
| Gravity | 0.08 m/tick^2 | Entity gravity |
| Air horizontal drag | 0.91x per tick | Friction multiplier |
| Vertical drag | 0.98x per tick | DragY |
| Air acceleration | 0.02 | LivingEntity.getFrictionInfluencedSpeed |
| Max step height | 0.6m | Step-assist |
| Jump apex | ~1.252b | Computed from physics |

### Jump Apex

The maximum jump height is ~1.252 blocks regardless of horizontal speed
or momentum. Momentum only affects horizontal distance at the apex:

| Mode | Momentum | Apex Y | X at Apex |
|---|---|---|---|
| Walk | 0t | 1.2522 | 0.885 |
| Walk | 12t | 1.2522 | 4.729 |
| Sprint | 0t | 1.2522 | 1.846 |
| Sprint | 12t | 1.2522 | 5.689 |

### Gap Feasibility Matrix (Sprint, 12t Flat Momentum)

Can the player cross a gap of N blocks to a platform at height offset dy?

| Gap | dy=+1.0 | dy=+0.5 | dy=0 | dy=-1 | dy=-2 | dy=-3 | dy=-5 |
|---|---|---|---|---|---|---|---|
| 0 | YES | YES | YES | YES | YES | YES | YES |
| 1 | YES | YES | YES | YES | YES | YES | YES |
| 2 | YES | YES | YES | YES | YES | YES | YES |
| 3 | YES | YES | YES | YES | YES | YES | YES |
| 4 | YES | YES | YES | YES | YES | YES | YES |
| 5 | YES | YES | YES | YES | YES | YES | YES |
| 6 | no | YES | YES | YES | YES | YES | YES |

### Gap Feasibility Matrix (Walk, 12t Momentum)

| Gap | dy=+1.0 | dy=+0.5 | dy=0 | dy=-1 | dy=-2 | dy=-3 | dy=-5 |
|---|---|---|---|---|---|---|---|
| 0 | YES | YES | YES | YES | YES | YES | YES |
| 1 | YES | YES | YES | YES | YES | YES | YES |
| 2 | YES | YES | YES | YES | YES | YES | YES |
| 3 | YES | YES | YES | YES | YES | YES | YES |
| 4 | YES | YES | YES | YES | YES | YES | YES |
| 5 | no | no | YES | YES | YES | YES | YES |

### Gap Feasibility Matrix (Standing Sprint Jump, 0t Momentum)

| Gap | dy=+1.0 | dy=+0.5 | dy=0 | dy=-1 | dy=-2 | dy=-3 | dy=-5 |
|---|---|---|---|---|---|---|---|
| 0 | YES | YES | YES | YES | YES | YES | YES |
| 1 | YES | YES | YES | YES | YES | YES | YES |
| 2 | no | YES | YES | YES | YES | YES | YES |
| 3 | no | no | no | no | no | no | no |

### Neo Jump Analysis (Flat, 12t Momentum)

For a wall of N blocks, the player must travel at least N + 0.6m forward
to clear the wall end (accounting for 0.6m player bounding box width).

| Wall Length | Sprint Reach | Needed | Margin | Feasible |
|---|---|---|---|---|
| 1b | 7.728m | 1.6m | +6.128 | YES |
| 2b | 7.728m | 2.6m | +5.128 | YES |
| 3b | 7.728m | 3.6m | +4.128 | YES |
| 4b | 7.728m | 4.6m | +3.128 | YES |

Note: the neo analysis uses simplified straight-line reach. In practice,
the player must also perform a lateral (sideways) movement to round the
wall corner, which reduces effective forward distance slightly. The large
margins suggest all 1-4 block neos are comfortably achievable.

### Ceiling-Constrained Jumps (Sprint, 12t Momentum)

Lower ceilings reduce jump height and therefore reduce horizontal distance:

| Ceiling Height | Landing X | Delta vs Open |
|---|---|---|
| 4.0b (no effect) | 7.728m | +0.000 |
| 3.0b | 7.415m | -0.313 |
| 2.5b | 5.689m | -2.039 |
| 2.0bc (headhitter) | 4.482m | -3.246 |
| 1.8125bc (trapdoor hh) | 4.042m | -3.687 |

### Sprint Jump Trajectory (12 tick momentum, flat landing)

| Tick | Phase | X | Y | VX | VY |
|---|---|---|---|---|---|
| 0-12 | Momentum (ground) | 0 -> 3.09 | 0 | 0 -> 0.156 | 0 |
| 13 | Jump tick | 3.58 | 0.42 | 0.443 | 0.333 |
| 14 | Rising | 4.04 | 0.75 | 0.421 | 0.248 |
| 15 | Rising | 4.48 | 1.00 | 0.401 | 0.165 |
| 16 | Rising | 4.90 | 1.17 | 0.382 | 0.083 |
| 17 | Apex | 5.30 | 1.25 | 0.366 | 0.003 |
| 18 | Falling | 5.69 | 1.25 | 0.351 | -0.075 |
| 19-23 | Falling | 5.69 -> 7.42 | 1.25 -> 0.12 | 0.351 -> 0.293 | accelerating |
| 24 | Landing | 7.73 | 0.00 | 0.171 | 0 |

Total airborne time: 11 ticks (tick 13-24).

### Implications for Pathfinding

Based on these results, the initial pathfinding scope should include:

1. **Standard jumps**: sprint jump can clear up to 5 block gaps (flat)
   and 4-5 block gaps with +1.0 height, with full momentum.

2. **Standing sprint jumps**: only reliable for up to 1 block gap with
   +1 height, or 2 block gap flat. This is relevant for confined spaces
   where a long run-up is unavailable.

3. **Neo jumps (1-2 block walls)**: comfortable margin with sprint.
   The pathfinder should include these as standard movement options.

4. **Ascending jumps (+1 block)**: always feasible with sprint for gaps
   up to 5 blocks. The key constraint is the 1.252 block jump height
   limit, meaning +1.0 is fine but +1.25+ is extremely marginal.

5. **Ceiling constraint**: a 2bc (headhitter) ceiling cuts reach roughly
   in half. The pathfinder should detect ceiling height and adjust the
   maximum jump gap accordingly.

## Reliability-first rule

Every movement proposal generated by the MCC pathfinder must be grounded in reality: if a move is accepted, it must be one the bot can execute in vanilla 1.21.11 physics. That means the final support footprint is the ultimate arbiter: if the planner can get the player onto a solid block (even if they momentarily hover over air during the transition), the move is considered valid. Conversely, any shape that would finish without block contact, rely on unsupported parkour tricks, or require a start-up/run-up that the current layout cannot provide must be rejected rather than downgraded to a risky heuristic.

The new regression harness in `tools/test-pathing-template-regressions.sh` codifies this rule by automating:

1. Flat-stopping scenarios that ensure the arrival block is within the planner’s tolerance.
2. Parkour + L-turn footprints to watch for actual support at the destination.
3. Side-wall jump acceptance conditioned on an executable landing.
4. A 3×1 no-run-up rejection to prevent non-executable plans from sneaking through.
5. Mixed ascend/descend/climb smoke cases so that both vertical transitions and ladder climbs respect the reliable support requirement.

## Deterministic live route contract

For the short-route and long-route `1.21.11-Vanilla` live harnesses, accepted routes must complete with all of the following:

- `A* result: Success`
- `0 replan`
- `0` template segment failures
- final position inside the intended goal support block
- `PathMgr` reporting `Navigation complete!`

For rejection scenarios, the requirement is stricter:

- `A* result: Failed` or `No path found`
- no navigation start
- no executor-driven `replan`

Residual speed carried from one movement to the next inside a route is expected and must not be normalized away just to satisfy the harness. The route is only considered reliable if that natural speed carry still produces `0 replan`.

## Baritone Reference Notes For Zero-Replan Work

MCC can borrow specific ideas from the local Baritone reference under `ThirdpartyReference/baritone/`, but not its looser success semantics.

Borrow:

- landing-aware completion, where movement logic keeps controlling after touchdown instead of failing immediately
- next-movement-aware descend and ascend handoff behavior
- conservative parkour admissibility, especially around run-up, overshoot, and blocked landing shapes
- executor timeout and movement-stuck heuristics as diagnostic input, not as acceptance criteria

Do not borrow:

- `GoalBlock` occupancy semantics as a substitute for deterministic execution quality
- executor repath tolerance as proof that a movement is reliable
- any behavior that lets accepted deterministic harness routes succeed only by falling back to `replan`

For this work, Baritone is a movement-control reference, not a correctness oracle. MCC's accepted live routes must still finish with `0 replan` in the deterministic harness.

Keeping the rule explicit here reminds future contributors that the planner should never promise a move that physically cannot finish with block contact.

## Regression Harness Workflow

The scripts in `tools/` now match the `mcc-dev-workflow` defaults: they call
`source tools/mcc-env.sh`, rely on a shared `mc-*` server running `1.21.11-Vanilla`,
and launch MCC through `mcc-build`, `mcc-debug`, and `mcc-cmd` wrappers. The
harnesses reuse the existing server session instead of stopping and restarting
it, which keeps shared test infrastructure stable and honors the instruction to
keep `mc-*` servers running unless another version or explicit reset is required.
When editing or extending the harness, preserve the `mcc-*` invocation pattern
and the existing log/tail helpers so the scripts stay compatible with the updated
workflow.

## References

- [Minecraft Parkour Wiki: Blip](https://www.mcpk.wiki/wiki/Blip)
- [Minecraft Parkour Wiki: Stepping](https://www.mcpk.wiki/wiki/Stepping)
- [Minecraft Parkour Wiki: Jump Cancel](https://www.mcpk.wiki/wiki/Jump_Cancel)
- [Minecraft Parkour Wiki: Parkour Nomenclature](https://www.mcpk.wiki/wiki/Parkour_Nomenclature)
- [Minecraft Parkour Wiki: Collisions](https://www.mcpk.wiki/wiki/Collisions)
