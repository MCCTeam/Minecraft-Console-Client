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
import csv
from tools.pathing_theory.primitives import (
    PLAYER_WIDTH,
    can_reach_gap,
    get_apex,
    get_landing,
    simulate_jump,
)
from tools.pathing_theory.simulator import build_theory_cases


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
