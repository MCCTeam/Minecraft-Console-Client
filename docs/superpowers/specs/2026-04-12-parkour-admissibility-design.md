# Parkour Admissibility Hardening

## Context
MoveParkour currently prepares sprint jumps with some previous Baritone-inspired checks, but certain configurations (e.g., missing run-up, blocked diagonal shoulders, landing into an immediate wall) still pass planning and fail at execution. The goal is to harden those admissions so that MoveParkour rejects unsafe shapes up front.

## Requirements
- Embed conservative versions of Baritone’s reliability-first checks for run-up length, diagonal shoulder clearance, and landing overshoot into the pathing layer.
- Keep the new logic localized under a Parkour-specific helper so that future moves can share the same checks without duplicating code.
- Tighten MoveParkour to rely on the helper for admissibility decisions and to reject overshoots instead of tolerating them with a cost penalty.
- Add deterministic tests that illustrate the three requested behaviors (3×1 jump without run-up, 2×1 jump with clear takeoff/landing, diagonal jump blocked at a shoulder).
- Run only the targeted test command once with the new test class.

## Design

### ParkourFeasibility helper
- Provide `ParkourFeasibility.HasRunUp(ctx, x, y, z, xOffset, zOffset, yDelta)` that reuses the existing distance thresholds (2.5 with ascend, 3.5 otherwise) but also enforces that the block immediately behind the player is walkable (top surface plus passable columns at head and neck height).
- Provide `ParkourFeasibility.HasDiagonalShoulderClearance(ctx, x, y, z, xOffset, zOffset)` that rejects diagonal jumps unless both orthogonal neighbors at start are passable through the whole torso (y through y+2) so a blocked shoulder can’t clip the AABB.
- Provide `ParkourFeasibility.HasLandingOvershootClearance(ctx, destX, destY, destZ, xSign, zSign)` that fails when the two blocks immediately past the landing spot are not passable at body and head height, preventing collisions after landing.
- Keep the helper static under `Pathing/Moves` to allow reuse by other moves in the future; assume this is acceptable even though only MoveParkour currently uses it.

### MoveParkour adjustments
- Before the existing flight-path, head-clearance, and landing/passability checks, call into the helper to verify run-up, diagonal shoulders, and overshoot.
- Remove the informational overshoot-penalty branch and instead treat blocked overshoot as an immediate rejection.
- Leave the current flight path, head clearance, and destination checks untouched to avoid regressions.

### Testing
- Add `MinecraftClient.Tests.Pathing.Moves.MoveParkourTests` that reuse a flat stone world and toggle blocks to create the three scenarios:
  1. 3×1 side-wall jump lacking a run-up (expect `MoveResult.IsImpossible`).
  2. 2×1 jump with clear takeoff and landing (expect success and the expected destination).
  3. Diagonal jump whose start cardinal neighbor is blocked at shoulder height (expect rejection).
- Each test creates the context with `allowParkour: true`, instantiates the appropriate `MoveParkour`, runs `Calculate`, and asserts on `IsImpossible`.
- Tests will live next to other pathing tests but focus narrowly on parkour admissibility.

## Validation
- Run `dotnet test MinecraftClient.Tests --filter MoveParkourTests`.

## Open questions
- I assumed the helper should be reusable beyond MoveParkour; if you prefer it to stay internal, I can adjust the visibility surface.
