#!/usr/bin/env python3
"""
Minecraft Jump Reachability Simulator (Java Edition 1.14+)

Simulates vanilla player physics tick-by-tick to determine which jump
destinations are reachable. Covers:
  - Linear jumps: flat, ascending (+N), descending (-N)
  - Sprint jumps vs walk jumps
  - Neo jumps (wall jumps): 1-block and 2-block wide walls
  - Headhitter (2bc ceiling) jumps

All physics constants match vanilla 1.21.x / MCC's PhysicsConsts.cs.

Usage:
    python3 sim_jump_reach.py [--verbose] [--csv output.csv]
"""

import argparse
import math
import csv
from dataclasses import dataclass
from typing import Optional

# ============================================================
# Vanilla physics constants (match PhysicsConsts.cs)
# ============================================================

PLAYER_WIDTH = 0.6
PLAYER_HEIGHT = 1.8
STEP_HEIGHT = 0.6

GRAVITY = 0.08
DRAG_Y = 0.98
FRICTION_MULTIPLIER = 0.91
DEFAULT_BLOCK_FRICTION = 0.6
INPUT_FRICTION = 0.98
GROUND_ACCEL_FACTOR = 0.21600002
AIR_ACCEL = 0.02
MOVEMENT_SPEED = 0.1

BASE_JUMP_POWER = 0.42
SPRINT_JUMP_HORIZONTAL_BOOST = 0.2

HORIZONTAL_VELOCITY_THRESHOLD_SQR = 9.0e-6
VERTICAL_VELOCITY_THRESHOLD = 0.003

HALF_WIDTH = PLAYER_WIDTH / 2.0  # 0.3


@dataclass
class TickState:
    tick: int = 0
    x: float = 0.0
    y: float = 0.0
    vx: float = 0.0
    vy: float = 0.0
    on_ground: bool = True


def get_ground_speed(block_friction: float = DEFAULT_BLOCK_FRICTION) -> float:
    f = block_friction * FRICTION_MULTIPLIER
    return MOVEMENT_SPEED * (GROUND_ACCEL_FACTOR / (f * f * f))


def simulate_jump(sprint: bool = True, momentum_ticks: int = 12,
                  ceiling_y: Optional[float] = None,
                  landing_y: float = 0.0,
                  landing_x_start: float = 0.0,
                  max_ticks: int = 200) -> list[TickState]:
    """
    Simulate a complete jump sequence: momentum phase on ground, then jump.

    The player starts at x=0, y=0 on a platform at y=0.

    landing_y: Y coordinate of the landing surface.
    landing_x_start: the X coordinate where the landing surface begins.
        For flat jumps (landing_y=0), this is 0 (same level everywhere).
        For ascending jumps (landing_y>0), this is typically gap_start
        (the landing platform isn't under the player at takeoff).
        For descending jumps (landing_y<0), this is gap_start.

    The starting platform is at y=0 from x=-inf to x=landing_x_start.
    The landing platform is at y=landing_y from x=landing_x_start onward.
    """
    x, y, vx, vy = 0.0, 0.0, 0.0, 0.0
    on_ground = True
    trajectory: list[TickState] = []
    jumped = False
    f_ground = DEFAULT_BLOCK_FRICTION * FRICTION_MULTIPLIER

    trajectory.append(TickState(0, x, y, vx, vy, on_ground))

    for tick in range(1, max_ticks + 1):
        # --- Zero tiny velocity ---
        if vx * vx < HORIZONTAL_VELOCITY_THRESHOLD_SQR:
            vx = 0.0
        if abs(vy) < VERTICAL_VELOCITY_THRESHOLD:
            vy = 0.0

        # --- Jump on the tick after momentum ---
        do_jump = False
        if not jumped and tick > momentum_ticks and on_ground:
            do_jump = True
            jumped = True

        if do_jump:
            vy = max(BASE_JUMP_POWER, vy)
            if sprint:
                vx += SPRINT_JUMP_HORIZONTAL_BOOST

        # --- Input acceleration ---
        forward_input = 1.0 * INPUT_FRICTION
        if on_ground:
            speed = get_ground_speed()
        else:
            speed = AIR_ACCEL
        vx += forward_input * speed

        # --- Move ---
        new_x = x + vx
        new_y = y + vy
        new_on_ground = False

        # Ceiling collision
        if ceiling_y is not None:
            head_y = new_y + PLAYER_HEIGHT
            if head_y > ceiling_y:
                new_y = ceiling_y - PLAYER_HEIGHT
                if vy > 0:
                    vy = 0.0

        # Floor collision: two-region terrain model
        # Region 1: x < landing_x_start -> floor at y=0 (starting platform)
        # Region 2: x >= landing_x_start -> floor at y=landing_y
        # Player bounding box trailing edge is at (new_x - HALF_WIDTH)
        # Use player center for region determination
        if new_x < landing_x_start:
            floor_y = 0.0
        else:
            floor_y = landing_y

        if jumped:
            if new_x >= landing_x_start:
                # Over the landing platform region
                if landing_y >= 0:
                    # Ascending or flat: only land when falling DOWN through the surface
                    if vy <= 0 and y >= landing_y and new_y <= landing_y:
                        new_y = landing_y
                        vy = 0.0
                        new_on_ground = True
                    elif vy <= 0 and new_y <= landing_y:
                        # Already below the surface (fell through on a prior tick
                        # that didn't trigger -- shouldn't happen but safety check)
                        new_y = landing_y
                        vy = 0.0
                        new_on_ground = True
                else:
                    # Descending: land when reaching the lower floor
                    if new_y <= landing_y:
                        new_y = landing_y
                        if vy < 0:
                            vy = 0.0
                        new_on_ground = True

            if not new_on_ground and new_x < landing_x_start:
                # Still over starting platform area or in the gap
                if new_y <= 0.0:
                    new_y = 0.0
                    if vy < 0:
                        vy = 0.0
                    new_on_ground = True
        else:
            # Momentum phase: always on starting platform
            if new_y <= 0.0:
                new_y = 0.0
                if vy < 0:
                    vy = 0.0
                new_on_ground = True

        x = new_x
        y = new_y
        on_ground = new_on_ground

        # --- Post-move: gravity + friction/drag ---
        vy -= GRAVITY
        vy *= DRAG_Y

        if on_ground:
            vx *= f_ground
        else:
            vx *= FRICTION_MULTIPLIER

        trajectory.append(TickState(tick, x, y, vx, vy, on_ground))

        # Stop once landed after being airborne
        if jumped and on_ground:
            break

    return trajectory


def get_landing(sprint: bool, target_y: float,
                landing_x_start: float = 0.0,
                momentum_ticks: int = 12,
                ceiling_y: Optional[float] = None) -> Optional[tuple[float, float]]:
    """Get (x, y) where the player lands. Returns None if no landing."""
    traj = simulate_jump(sprint=sprint, momentum_ticks=momentum_ticks,
                         ceiling_y=ceiling_y, landing_y=target_y,
                         landing_x_start=landing_x_start)
    was_air = False
    for s in traj:
        if not s.on_ground:
            was_air = True
        if was_air and s.on_ground:
            return s.x, s.y
    return None


def get_apex(sprint: bool, momentum_ticks: int = 12,
             ceiling_y: Optional[float] = None) -> tuple[float, float]:
    traj = simulate_jump(sprint=sprint, momentum_ticks=momentum_ticks,
                         ceiling_y=ceiling_y, landing_y=-1000.0,
                         landing_x_start=0.0, max_ticks=300)
    best_y, best_x = 0.0, 0.0
    for s in traj:
        if s.y > best_y:
            best_y = s.y
            best_x = s.x
    return best_y, best_x


def can_reach_gap(gap_blocks: int, dy: float, sprint: bool = True,
                  momentum_ticks: int = 12) -> tuple[bool, Optional[float], float]:
    """
    Check if the player can cross a gap of `gap_blocks` blocks to a surface
    at height offset `dy`.

    Geometry (player starts centered on block, center at x=0):
      - Starting platform right edge: x = 0.5
      - Gap: 0.5 to 0.5 + gap_blocks
      - Landing platform left edge: x = 0.5 + gap_blocks
      - Player center must reach x >= 0.5 + gap_blocks + HALF_WIDTH to land
        (trailing bounding box edge clears the gap)

    For ascending jumps (dy > 0):
      - Landing surface at y=dy begins at x = 0.5 + gap_blocks
      - The gap region has NO floor (void) if gap > 0, or floor at dy if gap = 0

    For gap = 0 and dy > 0:
      - This means stepping up to an adjacent block 1m higher.
      - Player just needs to jump and move forward 1 block.
    """
    if dy > 1.252:
        return False, None, 0.0

    needed_x = 0.5 + gap_blocks + HALF_WIDTH
    landing_platform_start = 0.5 + gap_blocks

    # For gap=0 ascending, the landing platform is right next to the start
    if gap_blocks == 0 and dy > 0:
        landing_platform_start = 0.5

    result = get_landing(sprint=sprint, target_y=dy,
                         landing_x_start=landing_platform_start,
                         momentum_ticks=momentum_ticks)
    if result is None:
        return False, None, needed_x

    lx, ly = result
    # Check if we actually landed on the target surface (not back on start)
    if abs(ly - dy) > 0.01:
        # Landed back on starting platform
        return False, lx, needed_x

    # For gap > 0, check player center is past the gap
    if gap_blocks > 0 and lx < needed_x:
        return False, lx, needed_x

    return True, lx, needed_x


# ============================================================
# Main analysis
# ============================================================

def analyze_all(verbose: bool = False) -> list[dict]:
    results = []

    print("=" * 78)
    print("  Minecraft Jump Reachability Analysis (Java 1.14+)")
    print("  Physics: vanilla 1.21.x constants from PhysicsConsts.cs")
    print("=" * 78)

    # --- Part 1: Apex ---
    print("\n[1] Jump Apex (Maximum Height)")
    print(f"    {'Mode':<8} {'Momentum':>8} {'Apex Y':>10} {'X at Apex':>12}")
    print(f"    {'----':<8} {'--------':>8} {'------':>10} {'---------':>12}")
    for sprint in [False, True]:
        for mm in [0, 6, 12, 20]:
            ay, ax = get_apex(sprint=sprint, momentum_ticks=mm)
            label = "Sprint" if sprint else "Walk"
            print(f"    {label:<8} {mm:>6}t  {ay:>10.4f} {ax:>12.4f}")
            results.append({'type': 'apex', 'sprint': sprint,
                            'momentum': mm, 'apex_y': ay, 'x_at_apex': ax})

    # --- Part 2: Landing distances (flat and descending) ---
    print(f"\n[2] Landing Distance (sprint, 12t momentum)")
    print(f"    {'dy':>6}  {'Landing X':>12}")
    print(f"    {'--':>6}  {'---------':>12}")
    for dy in [0.0, -1.0, -2.0, -3.0, -5.0, -10.0]:
        r = get_landing(sprint=True, target_y=dy,
                        landing_x_start=0.0 if dy <= 0 else 0.5,
                        momentum_ticks=12)
        sign = "+" if dy > 0 else " " if dy == 0 else ""
        if r:
            print(f"    {sign}{dy:>5.1f}  {r[0]:>12.4f}m")
        else:
            print(f"    {sign}{dy:>5.1f}  {'N/A':>12}")

    # --- Part 3: Full feasibility matrix ---
    print(f"\n[3] Gap Feasibility Matrix (Sprint, 12t momentum)")
    print(f"    Player width={PLAYER_WIDTH}m, max jump height=~1.252b")
    print()

    dy_values = [1.0, 0.5, 0.0, -1.0, -2.0, -3.0, -5.0]
    header = f"    {'Gap':>4}"
    for dy in dy_values:
        sign = "+" if dy > 0 else ""
        header += f" {sign}{dy:>5.1f}"
    print(header)
    print(f"    {'----':>4}" + " ------" * len(dy_values))

    for gap in range(0, 7):
        row = f"    {gap:>4}"
        for dy in dy_values:
            ok, lx, needed = can_reach_gap(gap, dy, sprint=True, momentum_ticks=12)
            if ok:
                row += f" {'YES':>6}"
            elif lx is None:
                row += f" {'N/A':>6}"
            else:
                row += f" {'no':>6}"
        print(row)

    # Walk version
    print(f"\n    Walk jump (no sprint), 12t momentum:")
    header = f"    {'Gap':>4}"
    for dy in dy_values:
        sign = "+" if dy > 0 else ""
        header += f" {sign}{dy:>5.1f}"
    print(header)
    print(f"    {'----':>4}" + " ------" * len(dy_values))

    for gap in range(0, 6):
        row = f"    {gap:>4}"
        for dy in dy_values:
            ok, lx, needed = can_reach_gap(gap, dy, sprint=False, momentum_ticks=12)
            if ok:
                row += f" {'YES':>6}"
            elif lx is None:
                row += f" {'N/A':>6}"
            else:
                row += f" {'no':>6}"
        print(row)

    # Standing jump (0 momentum)
    print(f"\n    Standing sprint jump (0t momentum):")
    header = f"    {'Gap':>4}"
    for dy in dy_values:
        sign = "+" if dy > 0 else ""
        header += f" {sign}{dy:>5.1f}"
    print(header)
    print(f"    {'----':>4}" + " ------" * len(dy_values))

    for gap in range(0, 5):
        row = f"    {gap:>4}"
        for dy in dy_values:
            ok, lx, needed = can_reach_gap(gap, dy, sprint=True, momentum_ticks=0)
            if ok:
                row += f" {'YES':>6}"
            elif lx is None:
                row += f" {'N/A':>6}"
            else:
                row += f" {'no':>6}"
        print(row)

    # --- Part 4: Neo analysis ---
    print(f"\n[4] Neo Jump Analysis (flat, 12t momentum)")
    print(f"    Wall extends perpendicular to movement.")
    print(f"    Player must travel wall_length + {PLAYER_WIDTH}m to clear wall end.\n")
    print(f"    {'Wall':>5} {'Mode':<8} {'LandingX':>10} {'Needed':>10} {'Margin':>10} {'OK':>6}")
    print(f"    {'----':>5} {'----':<8} {'--------':>10} {'------':>10} {'------':>10} {'--':>6}")

    for wall_len in [1, 2, 3, 4]:
        for sprint in [True, False]:
            r = get_landing(sprint=sprint, target_y=0.0,
                            landing_x_start=0.0, momentum_ticks=12)
            label = "Sprint" if sprint else "Walk"
            if r is None:
                print(f"    {wall_len:>5} {label:<8} {'N/A':>10}")
                continue
            lx = r[0]
            needed = wall_len + PLAYER_WIDTH
            margin = lx - needed
            ok = "YES" if margin >= 0 else "no"
            print(f"    {wall_len:>5} {label:<8} {lx:>10.4f} {needed:>10.4f} "
                  f"{margin:>+10.4f} {ok:>6}")
            results.append({'type': 'neo', 'wall': wall_len, 'sprint': sprint,
                            'reach': lx, 'needed': needed, 'margin': margin,
                            'ok': margin >= 0})

    # --- Part 5: Ceiling ---
    print(f"\n[5] Ceiling-Constrained Jumps (Sprint, 12t mm, flat)")
    base_r = get_landing(sprint=True, target_y=0.0, momentum_ticks=12)
    base_lx = base_r[0] if base_r else 0
    print(f"    {'Ceiling':>8} {'LandingX':>12} {'Delta':>10}")
    for ceil in [4.0, 3.0, 2.5, 2.0, 1.8125]:
        r = get_landing(sprint=True, target_y=0.0, momentum_ticks=12,
                        ceiling_y=ceil)
        if r:
            diff = r[0] - base_lx
            print(f"    {ceil:>7.4f}b {r[0]:>11.4f}m {diff:>+10.4f}")
        else:
            print(f"    {ceil:>7.4f}b {'N/A':>12}")

    # --- Part 6: Verbose ---
    if verbose:
        for label, sp in [("Sprint", True), ("Walk", False)]:
            print(f"\n[V] {label} Jump Trajectory (12t momentum, flat)")
            print(f"    {'Tick':>4} {'X':>10} {'Y':>10} {'VX':>10} {'VY':>10} {'Gnd':>5}")
            traj = simulate_jump(sprint=sp, momentum_ticks=12, landing_y=0.0)
            for s in traj:
                g = "G" if s.on_ground else ""
                print(f"    {s.tick:>4} {s.x:>10.4f} {s.y:>10.4f} "
                      f"{s.vx:>10.6f} {s.vy:>10.6f} {g:>5}")

        # +1 ascending sprint jump
        print(f"\n[V] Sprint +1 Ascending Trajectory (12t mm, gap=1)")
        print(f"    {'Tick':>4} {'X':>10} {'Y':>10} {'VX':>10} {'VY':>10} {'Gnd':>5}")
        traj = simulate_jump(sprint=True, momentum_ticks=12,
                             landing_y=1.0, landing_x_start=1.5)
        for s in traj:
            g = "G" if s.on_ground else ""
            print(f"    {s.tick:>4} {s.x:>10.4f} {s.y:>10.4f} "
                  f"{s.vx:>10.6f} {s.vy:>10.6f} {g:>5}")

    return results


def main():
    parser = argparse.ArgumentParser(
        description="Minecraft jump reachability simulator (Java 1.14+)")
    parser.add_argument("--verbose", "-v", action="store_true",
                        help="Print per-tick trajectory data")
    parser.add_argument("--csv", type=str, default=None,
                        help="Export results to CSV file")
    args = parser.parse_args()

    results = analyze_all(verbose=args.verbose)

    if args.csv and results:
        keys = set()
        for r in results:
            keys.update(r.keys())
        with open(args.csv, "w", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=sorted(keys))
            writer.writeheader()
            writer.writerows(results)
        print(f"\nResults exported to {args.csv}")


if __name__ == "__main__":
    main()
