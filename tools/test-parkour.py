#!/usr/bin/env python3
"""Full-coverage parkour test suite for MCC pathfinding.

Reads momentum-capabilities.json to derive a test matrix, builds multi-segment
jump courses via RCON, and verifies MCC can navigate (or correctly reject) each
one.  Stops testing larger gaps once the first failure is seen per group.

Usage:
    source tools/mcc-env.sh
    python3 tools/test-parkour.py [OPTIONS]

Options:
    --list-cases            Print test matrix and exit
    --dry-run               Build courses only, do not navigate
    --filter PATTERN        Hierarchical filter (see examples below)
    --username NAME         MCC username (default: MCCBot)
    --rcon-port PORT        RCON port (default: 25575)
    --rcon-password PASS    RCON password (default: test123)
    --wait SECONDS          Seconds to wait per navigation (default: 15)
    --results PATH          Write JSONL results to PATH

Filter examples:
    --filter linear                     All linear tests
    --filter linear/flat                Only linear flat (dy=0)
    --filter linear/ascend              Only linear ascend (dy>0)
    --filter linear/descend/dy-1        Linear descend, dy=-1 only
    --filter neo                        All neo tests
    --filter ceiling                    All ceiling tests
    --filter ceiling/headhitter/ceil2.5 Ceiling with height 2.5
    --filter linear-flat-gap4           Exact case_id match

    Multiple filters: --filter linear,neo

Note: sidewall family is excluded by default (identical max_reach to linear,
wall does not affect A* block-level pathfinding).
"""

from __future__ import annotations

import argparse
import json
import math
import os
import re
import socket
import struct
import sys
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

REPO_ROOT = Path(__file__).resolve().parent.parent
CAPABILITIES_PATH = REPO_ROOT / "tools" / "pathing_data" / "momentum-capabilities.json"

SEGMENTS = 3
CLEAR_MARGIN = 7
LINEAR_RUNWAY = 4
SIDEWALL_RUNWAY = 2

CEILING_HEIGHTS_TO_TEST = [2.0, 2.5, 4.0]

# Families whose A* max_reach is identical to linear (wall doesn't affect
# pathfinder block-level decisions).  Excluded from the default matrix.
SKIP_FAMILIES = {"sidewall"}

NEO_LANDING_GAP = 3  # blocks between wall-end and next wall-start (1 air + platform + 1 air)


# ---------------------------------------------------------------------------
# RCON client
# ---------------------------------------------------------------------------

class RconClient:
    def __init__(self, host: str = "localhost", port: int = 25575, password: str = "test123"):
        self._host = host
        self._port = port
        self._password = password
        self._sock: Optional[socket.socket] = None

    def connect(self) -> None:
        self._sock = socket.socket()
        self._sock.settimeout(5)
        self._sock.connect((self._host, self._port))
        self._send(1, 3, self._password)
        resp = self._recv()
        rid = struct.unpack("<i", resp[:4])[0]
        if rid == -1:
            raise RuntimeError("RCON auth failed")

    def command(self, cmd: str) -> str:
        if self._sock is None:
            self.connect()
        self._send(2, 2, cmd)
        resp = self._recv()
        return resp[8:-2].decode(errors="replace")

    def close(self) -> None:
        if self._sock:
            self._sock.close()
            self._sock = None

    def _send(self, req_id: int, pkt_type: int, body: str) -> None:
        encoded = body.encode()
        header = struct.pack("<iii", 10 + len(encoded), req_id, pkt_type)
        assert self._sock is not None
        self._sock.send(header + encoded + b"\x00\x00")

    def _recv(self) -> bytes:
        assert self._sock is not None
        length_data = b""
        while len(length_data) < 4:
            length_data += self._sock.recv(4 - len(length_data))
        length = struct.unpack("<i", length_data)[0]
        data = b""
        while len(data) < length:
            data += self._sock.recv(length - len(data))
        return data


# ---------------------------------------------------------------------------
# MCC command interface
# ---------------------------------------------------------------------------

class MccClient:
    def __init__(self, session: str):
        self.session = session
        session_root = Path(os.environ.get("TMPDIR", "/tmp")) / "mcc-debug" / session
        self.input_file = session_root / "mcc_input.txt"
        self.log_file = session_root / "mcc-debug.log"

    def send(self, command: str) -> None:
        self.input_file.parent.mkdir(parents=True, exist_ok=True)
        with self.input_file.open("a") as f:
            f.write(command + "\n")

    def clear_log(self) -> None:
        with self.log_file.open("w") as f:
            f.truncate(0)

    def read_log(self) -> str:
        if not self.log_file.exists():
            return ""
        return self.log_file.read_text(errors="replace")

    def strip_ansi(self, text: str) -> str:
        return re.sub(r"\x1b\[[0-9;]*m", "", text)


# ---------------------------------------------------------------------------
# Test matrix generation
# ---------------------------------------------------------------------------

@dataclass
class TestCase:
    case_id: str
    family: str
    subfamily: str
    gap_or_wall: int
    delta_y: float
    ceiling_height: Optional[float]
    wall_offset: Optional[int]
    expected: str  # "pass" or "reject"

    def group_key(self) -> tuple:
        """Key for stop-at-first-failure grouping."""
        return (self.family, self.subfamily, self.delta_y,
                self.ceiling_height, self.wall_offset)

    def label(self) -> str:
        return self.case_id


def load_capabilities(path: Path) -> list[dict]:
    with path.open() as f:
        return json.load(f)


def derive_test_matrix(caps: list[dict]) -> list[TestCase]:
    """Derive the test matrix from momentum capabilities.

    For each unique (family, subfamily, qualifiers) combination, finds the
    global max_reach across all movement modes and momentum ranges, then
    generates gap values from 0 to max_reach+1.  The max_reach+1 case is the
    sole expected-reject case per group.
    """
    grouped: dict[tuple, int] = {}

    for band in caps:
        family = band["family"]
        subfamily = band["subfamily"]
        metric = band["capability_metric"]
        reach = band["max_reach"]
        if reach is None:
            continue

        dy = band.get("delta_y")
        ceil = band.get("ceiling_height")
        wo = band.get("wall_offset")

        if family == "ceiling":
            if ceil not in CEILING_HEIGHTS_TO_TEST:
                continue

        if family in SKIP_FAMILIES:
            continue

        key = (family, subfamily, dy, ceil, wo, metric)
        if key not in grouped or reach > grouped[key]:
            grouped[key] = reach

    cases: list[TestCase] = []
    for key, max_reach in sorted(grouped.items()):
        family, subfamily, dy, ceil, wo, metric = key

        for value in range(0, max_reach + 2):
            expected = "pass" if value <= max_reach else "reject"

            qualifier_parts = []
            if dy is not None and dy != 0.0:
                qualifier_parts.append(f"dy{dy:+.0f}")
            if ceil is not None:
                qualifier_parts.append(f"ceil{ceil}")
            if wo is not None:
                qualifier_parts.append(f"wo{wo}")

            qualifier_str = "-".join(qualifier_parts) if qualifier_parts else ""
            value_label = f"gap{value}" if metric == "gap_blocks" else f"wall{value}"
            parts = [family, subfamily, value_label]
            if qualifier_str:
                parts.append(qualifier_str)
            case_id = "-".join(parts)

            cases.append(TestCase(
                case_id=case_id,
                family=family,
                subfamily=subfamily,
                gap_or_wall=value,
                delta_y=dy if dy is not None else 0.0,
                ceiling_height=ceil,
                wall_offset=wo,
                expected=expected,
            ))

    return cases


# ---------------------------------------------------------------------------
# Filtering
# ---------------------------------------------------------------------------

def matches_filter(case: TestCase, pattern: str) -> bool:
    """Check if a test case matches a hierarchical filter pattern.

    Supports:
      - Exact case_id match:  "linear-flat-gap4"
      - Family:               "linear"
      - Family/subfamily:     "linear/flat"
      - With qualifiers:      "linear/descend/dy-1", "sidewall/flat/wo0",
                               "ceiling/headhitter/ceil2.5"
    """
    if case.case_id == pattern:
        return True

    parts = pattern.split("/")

    if parts[0] != case.family:
        return False
    if len(parts) == 1:
        return True

    if parts[1] != case.subfamily:
        return False
    if len(parts) == 2:
        return True

    for qualifier in parts[2:]:
        q_lower = qualifier.lower()
        matched = False

        if q_lower.startswith("dy"):
            try:
                target_dy = float(q_lower[2:])
                if case.delta_y == target_dy:
                    matched = True
            except ValueError:
                pass
        elif q_lower.startswith("ceil"):
            try:
                target_ceil = float(q_lower[4:])
                if case.ceiling_height == target_ceil:
                    matched = True
            except ValueError:
                pass
        elif q_lower.startswith("wo"):
            try:
                target_wo = int(q_lower[2:])
                if case.wall_offset == target_wo:
                    matched = True
            except ValueError:
                pass
        elif q_lower.startswith("gap"):
            try:
                target_gap = int(q_lower[3:])
                if case.gap_or_wall == target_gap:
                    matched = True
            except ValueError:
                pass
        elif q_lower.startswith("wall"):
            try:
                target_wall = int(q_lower[4:])
                if case.gap_or_wall == target_wall:
                    matched = True
            except ValueError:
                pass

        if not matched:
            return False

    return True


def apply_filters(cases: list[TestCase], filter_str: str) -> list[TestCase]:
    """Apply comma-separated filter patterns to the case list."""
    patterns = [p.strip() for p in filter_str.split(",")]
    return [c for c in cases if any(matches_filter(c, p) for p in patterns)]


# ---------------------------------------------------------------------------
# World building
# ---------------------------------------------------------------------------

@dataclass
class CourseLayout:
    start_x: int
    start_y: int
    start_z: int
    end_x: int
    end_y: int
    end_z: int
    clear_min: tuple[int, int, int]
    clear_max: tuple[int, int, int]


class WorldBuilder:
    def __init__(self, rcon: RconClient, base_x: int = 100, base_y: int = 80):
        self.rcon = rcon
        self.base_x = base_x
        self.base_y = base_y
        self._z_cursor = 100

    def allocate_z(self, width: int = 1) -> int:
        z = self._z_cursor
        self._z_cursor += width + 2 * CLEAR_MARGIN + 5
        return z

    def clear_area(self, x1: int, y1: int, z1: int, x2: int, y2: int, z2: int) -> None:
        dx = x2 - x1
        dz = z2 - z1
        # MC fill command has a 32768-block limit per call; chunk if needed
        chunk_size = 48
        for cx in range(x1, x2 + 1, chunk_size):
            for cz in range(z1, z2 + 1, chunk_size):
                ex = min(cx + chunk_size - 1, x2)
                ez = min(cz + chunk_size - 1, z2)
                self.rcon.command(f"fill {cx} {y1} {cz} {ex} {y2} {ez} air")

    def set_block(self, x: int, y: int, z: int, block: str = "stone") -> None:
        self.rcon.command(f"setblock {x} {y} {z} {block}")

    def fill_blocks(self, x1: int, y1: int, z1: int, x2: int, y2: int, z2: int,
                    block: str = "stone") -> None:
        self.rcon.command(f"fill {x1} {y1} {z1} {x2} {y2} {z2} {block}")

    def build_linear_route(self, case: TestCase) -> CourseLayout:
        gap = case.gap_or_wall
        dy = case.delta_y
        bx = self.base_x
        by = self.base_y
        bz = self.allocate_z()
        floor_y = by - 1

        platform_stride = gap + 1
        total_x = LINEAR_RUNWAY + SEGMENTS * platform_stride + 2

        max_dy_extent = int(abs(dy) * SEGMENTS) + 2
        x_min = bx - CLEAR_MARGIN
        x_max = bx + total_x + CLEAR_MARGIN
        y_min = min(floor_y, floor_y + int(dy * SEGMENTS)) - CLEAR_MARGIN
        y_max = max(floor_y, floor_y + int(dy * SEGMENTS)) + CLEAR_MARGIN
        z_min = bz - CLEAR_MARGIN
        z_max = bz + CLEAR_MARGIN

        self.clear_area(x_min, y_min, z_min, x_max, y_max, z_max)

        for rx in range(LINEAR_RUNWAY):
            self.set_block(bx + rx, floor_y, bz)

        last_x = bx + LINEAR_RUNWAY - 1
        last_y = floor_y

        for seg in range(SEGMENTS):
            plat_x = last_x + gap + 1
            plat_y = last_y + int(dy)
            self.set_block(plat_x, plat_y, bz)
            last_x = plat_x
            last_y = plat_y

        return CourseLayout(
            start_x=bx, start_y=by, start_z=bz,
            end_x=last_x, end_y=last_y + 1, end_z=bz,
            clear_min=(x_min, y_min, z_min),
            clear_max=(x_max, y_max, z_max),
        )

    def build_neo_route(self, case: TestCase) -> CourseLayout:
        """Neo jump: player must jump around a wall to reach the next platform.

        Layout (top view, each segment):
            [Runway/Platform at Z=cur_z]
            [Wall: 1 block in X, wall_width blocks in Z, 4 blocks tall]
            [1 block air gap]
            [Landing platform]
            [1 block air gap]
            [Next wall...]

        The wall runs along Z starting from the current Z.  The player
        jumps around the wall edge in the +Z direction to reach the landing.
        """
        wall_width = case.gap_or_wall
        bx = self.base_x
        by = self.base_y
        bz = self.allocate_z(width=(wall_width + NEO_LANDING_GAP) * SEGMENTS + 10)
        floor_y = by - 1

        z_extent = SEGMENTS * (wall_width + NEO_LANDING_GAP) + 10
        total_x = LINEAR_RUNWAY + SEGMENTS * 2 + 5

        x_min = bx - CLEAR_MARGIN
        x_max = bx + total_x + CLEAR_MARGIN
        y_min = floor_y - CLEAR_MARGIN
        y_max = floor_y + CLEAR_MARGIN
        z_min = bz - CLEAR_MARGIN
        z_max = bz + z_extent + CLEAR_MARGIN

        self.clear_area(x_min, y_min, z_min, x_max, y_max, z_max)

        # Runway
        for rx in range(LINEAR_RUNWAY):
            self.set_block(bx + rx, floor_y, bz)

        seg_x = bx + LINEAR_RUNWAY - 1
        cur_z = bz

        for seg in range(SEGMENTS):
            wall_x = seg_x + 1

            if wall_width > 0:
                wall_z_start = cur_z
                wall_z_end = cur_z + wall_width - 1
                self.fill_blocks(wall_x, floor_y, wall_z_start,
                                 wall_x, floor_y + 3, wall_z_end)

            # Landing: 1 block of air, then platform, then 1 block of air
            landing_z = cur_z + wall_width + 1  # 1 air gap after wall
            self.set_block(wall_x + 1, floor_y, landing_z)

            seg_x = wall_x + 1
            cur_z = landing_z + 2  # 1 air gap after landing before next wall

        end_x = seg_x
        end_z = cur_z - 2  # last landing position

        return CourseLayout(
            start_x=bx, start_y=by, start_z=bz,
            end_x=end_x, end_y=by, end_z=end_z,
            clear_min=(x_min, y_min, z_min),
            clear_max=(x_max, y_max, z_max),
        )

    def build_sidewall_route(self, case: TestCase) -> CourseLayout:
        """Sidewall jump: platforms along a massive wall face.

        The wall is directly behind the platforms (Z+1), tall and thick,
        constraining backward movement.  Player jumps between 1x1 platforms
        that are at different X offsets and Y heights along the wall.

        Layout (side view, looking from -Z toward +Z / toward the wall):

            [=== MASSIVE WALL (Z=bz+1 to bz+6, full height) ===]
            |                                                    |
            |    [P3] at X+2*stride, Y+2*dy                     |
            |                                                    |
            |         [P2] at X+stride, Y+dy                    |
            |                                                    |
            | [Start/Runway] at X, Y                             |
            [====================================================]
                              (open air below/in front)

        Wall_offset controls distance from platform to wall:
          wo=0: wall at Z=bz+1 (directly behind)
          wo=1: wall at Z=bz+2 (1 block gap)
        """
        gap = case.gap_or_wall
        dy = case.delta_y
        wo = case.wall_offset if case.wall_offset is not None else 0
        bx = self.base_x
        by = self.base_y
        bz = self.allocate_z(width=8 + wo)
        floor_y = by - 1

        platform_stride = gap + 1
        total_x = SIDEWALL_RUNWAY + SEGMENTS * platform_stride + 2

        x_min = bx - CLEAR_MARGIN
        x_max = bx + total_x + CLEAR_MARGIN
        y_min = min(floor_y, floor_y + int(dy * SEGMENTS)) - CLEAR_MARGIN
        y_max = max(floor_y, floor_y + int(dy * SEGMENTS)) + CLEAR_MARGIN
        z_min = bz - CLEAR_MARGIN
        z_max = bz + 8 + wo + CLEAR_MARGIN

        self.clear_area(x_min, y_min, z_min, x_max, y_max, z_max)

        # Runway (shorter than linear -- 2 blocks like the reference image)
        for rx in range(SIDEWALL_RUNWAY):
            self.set_block(bx + rx, floor_y, bz)

        last_x = bx + SIDEWALL_RUNWAY - 1
        last_y = floor_y

        for seg in range(SEGMENTS):
            plat_x = last_x + gap + 1
            plat_y = last_y + int(dy)
            self.set_block(plat_x, plat_y, bz)
            last_x = plat_x
            last_y = plat_y

        # Massive wall behind the platforms
        wall_z_start = bz + 1 + wo
        wall_z_end = bz + 6 + wo  # 6 blocks thick
        wall_y_low = min(floor_y, last_y) - 2
        wall_y_high = max(floor_y, last_y) + 5
        wall_x_start = bx - 1
        wall_x_end = last_x + 1
        self.fill_blocks(wall_x_start, wall_y_low, wall_z_start,
                         wall_x_end, wall_y_high, wall_z_end)

        return CourseLayout(
            start_x=bx, start_y=by, start_z=bz,
            end_x=last_x, end_y=last_y + 1, end_z=bz,
            clear_min=(x_min, y_min, z_min),
            clear_max=(x_max, y_max, z_max),
        )

    def build_ceiling_route(self, case: TestCase) -> CourseLayout:
        gap = case.gap_or_wall
        ceil_height = case.ceiling_height or 4.0
        bx = self.base_x
        by = self.base_y
        bz = self.allocate_z()
        floor_y = by - 1

        platform_stride = gap + 1
        total_x = LINEAR_RUNWAY + SEGMENTS * platform_stride + 2

        x_min = bx - CLEAR_MARGIN
        x_max = bx + total_x + CLEAR_MARGIN
        ceil_y = floor_y + int(ceil_height) + 1
        y_min = floor_y - CLEAR_MARGIN
        y_max = ceil_y + CLEAR_MARGIN
        z_min = bz - CLEAR_MARGIN
        z_max = bz + CLEAR_MARGIN

        self.clear_area(x_min, y_min, z_min, x_max, y_max, z_max)

        for rx in range(LINEAR_RUNWAY):
            self.set_block(bx + rx, floor_y, bz)

        last_x = bx + LINEAR_RUNWAY - 1
        for seg in range(SEGMENTS):
            plat_x = last_x + gap + 1
            self.set_block(plat_x, floor_y, bz)
            last_x = plat_x

        ceil_block_y = floor_y + math.ceil(ceil_height)
        self.fill_blocks(bx - 1, ceil_block_y, bz - 1, last_x + 1, ceil_block_y, bz + 1)

        return CourseLayout(
            start_x=bx, start_y=by, start_z=bz,
            end_x=last_x, end_y=by, end_z=bz,
            clear_min=(x_min, y_min, z_min),
            clear_max=(x_max, y_max, z_max),
        )

    def build(self, case: TestCase) -> CourseLayout:
        if case.family == "linear":
            return self.build_linear_route(case)
        if case.family == "neo":
            return self.build_neo_route(case)
        if case.family == "sidewall":
            return self.build_sidewall_route(case)
        if case.family == "ceiling":
            return self.build_ceiling_route(case)
        raise ValueError(f"Unknown family: {case.family}")


# ---------------------------------------------------------------------------
# Test execution
# ---------------------------------------------------------------------------

@dataclass
class TestResult:
    case: TestCase
    outcome: str  # "pass", "reject", "fail", "invalid_live_case"
    matched_expected: bool
    log_excerpt: str = ""


def resolve_session() -> str:
    explicit = os.environ.get("SESSION", "")
    if explicit:
        return explicit
    repo = os.environ.get("MCC_REPO_ROOT", "")
    if repo:
        return Path(repo).name
    return Path.cwd().name


def run_single_test(
    case: TestCase,
    layout: CourseLayout,
    rcon: RconClient,
    mcc: MccClient,
    username: str,
    wait_seconds: int = 15,
) -> TestResult:
    rcon.command(f"gamemode creative {username}")
    time.sleep(0.3)
    rcon.command(f"tp {username} {layout.start_x}.5 {layout.start_y} {layout.start_z}.5")
    time.sleep(1)
    rcon.command(f"gamemode survival {username}")
    time.sleep(0.5)

    mcc.clear_log()
    time.sleep(0.3)

    mcc.send(f"goto {layout.end_x} {layout.end_y} {layout.end_z}")
    time.sleep(wait_seconds)

    raw_log = mcc.read_log()
    log = mcc.strip_ansi(raw_log)
    all_lines = log.splitlines()

    a_star_lines = [l for l in all_lines if "[A*]" in l][:3]
    path_mgr_lines = [l for l in all_lines if "[PathMgr]" in l]
    path_exec_lines = [l for l in all_lines if "[PathExec]" in l]
    move_lines = [l for l in all_lines if "FileInput" in l or "path" in l.lower()
                  or "move" in l.lower() or "navigate" in l.lower()]

    outcome = "invalid_live_case"
    full_text = log.lower()
    mgr_text = "\n".join(path_mgr_lines)
    astar_text = "\n".join(a_star_lines)
    exec_text = "\n".join(path_exec_lines)

    if "navigation complete" in full_text:
        outcome = "pass"
    elif "complete" in mgr_text.lower():
        outcome = "pass"
    elif "failed to compute a safe path" in full_text:
        outcome = "reject"
    elif "not a reachable" in full_text:
        outcome = "reject"
    elif "no path" in full_text:
        outcome = "reject"
    elif "Failed" in astar_text:
        outcome = "reject"
    elif "Replan failed" in mgr_text or "Giving up" in mgr_text:
        outcome = "fail"
    elif "FAILED" in exec_text:
        outcome = "fail"
    elif "failed" in full_text:
        outcome = "fail"

    excerpt_lines = []
    if a_star_lines:
        excerpt_lines.append(f"  A*: {a_star_lines[0]}")
    if path_mgr_lines:
        excerpt_lines.append(f"  Mgr: {path_mgr_lines[-1]}")
    relevant = [l for l in all_lines if "path" in l.lower() or "move" in l.lower()
                or "navigate" in l.lower() or "A*" in l]
    if not excerpt_lines and relevant:
        excerpt_lines.append(f"  Log: {relevant[0]}")

    return TestResult(
        case=case,
        outcome=outcome,
        matched_expected=(outcome == case.expected),
        log_excerpt="\n".join(excerpt_lines),
    )


# ---------------------------------------------------------------------------
# Stop-at-first-failure logic
# ---------------------------------------------------------------------------

def should_skip(case: TestCase, failed_groups: set[tuple]) -> bool:
    """Skip this case if its group already had a failure at a smaller gap."""
    return case.group_key() in failed_groups


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Full-coverage parkour test suite",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__.split("Usage:")[0],
    )
    parser.add_argument("--list-cases", action="store_true",
                        help="Print test matrix and exit")
    parser.add_argument("--dry-run", action="store_true",
                        help="Build worlds but don't run MCC navigation")
    parser.add_argument("--filter", type=str, default=None,
                        help="Comma-separated hierarchical filters")
    parser.add_argument("--rcon-port", type=int, default=25575)
    parser.add_argument("--rcon-password", type=str, default="test123")
    parser.add_argument("--username", type=str, default="MCCBot")
    parser.add_argument("--wait", type=int, default=15,
                        help="Seconds to wait for navigation per test")
    parser.add_argument("--results", type=str, default=None,
                        help="Path for JSONL results output")
    args = parser.parse_args()

    caps = load_capabilities(CAPABILITIES_PATH)
    all_cases = derive_test_matrix(caps)

    if args.filter:
        all_cases = apply_filters(all_cases, args.filter)

    if args.list_cases:
        pass_count = sum(1 for c in all_cases if c.expected == "pass")
        reject_count = sum(1 for c in all_cases if c.expected == "reject")
        print(f"Total: {len(all_cases)} cases ({pass_count} pass, {reject_count} reject)")

        current_group = ""
        for c in all_cases:
            group = f"{c.family}/{c.subfamily}"
            if group != current_group:
                print(f"\n  {group}:")
                current_group = group
            marker = "PASS" if c.expected == "pass" else "REJECT"
            quals = []
            if c.delta_y != 0.0:
                quals.append(f"dy={c.delta_y:+.0f}")
            if c.ceiling_height is not None:
                quals.append(f"ceil={c.ceiling_height}")
            if c.wall_offset is not None:
                quals.append(f"wo={c.wall_offset}")
            q = f" ({', '.join(quals)})" if quals else ""
            metric = "gap" if c.family != "neo" else "wall"
            print(f"    {c.case_id:<50} {metric}={c.gap_or_wall}  [{marker}]{q}")
        return

    rcon = RconClient(port=args.rcon_port, password=args.rcon_password)
    rcon.connect()

    rcon.command("difficulty peaceful")
    rcon.command("gamerule doMobSpawning false")
    rcon.command("time set day")

    builder = WorldBuilder(rcon)

    if args.dry_run:
        print("=== DRY RUN: building worlds only ===")
        for case in all_cases:
            layout = builder.build(case)
            print(f"  {case.case_id}: start=({layout.start_x},{layout.start_y},{layout.start_z})"
                  f" end=({layout.end_x},{layout.end_y},{layout.end_z})")
        rcon.close()
        print(f"\nBuilt {len(all_cases)} courses.")
        return

    session = resolve_session()
    mcc = MccClient(session)

    results_path = Path(args.results) if args.results else None
    if results_path:
        results_path.parent.mkdir(parents=True, exist_ok=True)

    print("=" * 60)
    print("  MCC Full-Coverage Parkour Test Suite")
    print("=" * 60)
    print(f"  Cases: {len(all_cases)}")
    print(f"  Username: {args.username}")
    print(f"  Session: {session}")
    print(f"  Wait: {args.wait}s per test")
    print()

    results: list[TestResult] = []
    failed_groups: set[tuple] = set()
    skipped = 0

    for i, case in enumerate(all_cases, 1):
        if should_skip(case, failed_groups):
            skipped += 1
            print(f"  [{i}/{len(all_cases)}] {case.case_id} -- SKIPPED (group already failed)")
            continue

        print(f"\n--- [{i}/{len(all_cases)}] {case.case_id} (expect: {case.expected}) ---")

        layout = builder.build(case)
        print(f"  Route: ({layout.start_x},{layout.start_y},{layout.start_z}) -> "
              f"({layout.end_x},{layout.end_y},{layout.end_z})")

        result = run_single_test(
            case, layout, rcon, mcc, args.username, args.wait,
        )
        results.append(result)

        status = "OK" if result.matched_expected else "MISMATCH"
        print(f"  Outcome: {result.outcome} [{status}]")
        if result.log_excerpt:
            print(result.log_excerpt)

        # Stop-at-first-failure: only trigger on definitive navigation
        # failures (reject/fail), not on setup issues (invalid_live_case).
        if result.outcome in ("reject", "fail") and case.expected == "pass":
            failed_groups.add(case.group_key())
            print(f"  >> Group failed at {case.family}/{case.subfamily} "
                  f"gap/wall={case.gap_or_wall} -- skipping larger values")

        if results_path:
            with results_path.open("a") as f:
                f.write(json.dumps({
                    "case_id": case.case_id,
                    "family": case.family,
                    "subfamily": case.subfamily,
                    "gap_or_wall": case.gap_or_wall,
                    "expected": case.expected,
                    "outcome": result.outcome,
                    "matched": result.matched_expected,
                }) + "\n")

    rcon.close()

    print("\n" + "=" * 60)
    print("  SUMMARY")
    print("=" * 60)

    passed = [r for r in results if r.matched_expected]
    failed = [r for r in results if not r.matched_expected]

    print(f"\n  {len(passed)}/{len(results)} matched expectations")
    if skipped:
        print(f"  {skipped} cases skipped (stop-at-first-failure)")

    if failed:
        print(f"\n  MISMATCHES ({len(failed)}):")
        for r in failed:
            print(f"    {r.case.case_id}: expected={r.case.expected} got={r.outcome}")

    sys.exit(0 if not failed else 1)


if __name__ == "__main__":
    main()
