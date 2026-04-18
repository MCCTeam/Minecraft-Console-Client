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
    --username NAME         MCC username base (default: MCCBot)
    --rcon-port PORT        RCON port (default: 25575)
    --rcon-password PASS    RCON password (default: test123)
    --wait SECONDS          Seconds to wait per navigation (default: 15)
    --results PATH          Write JSONL results to PATH
    --parallel N            Run N MCC instances in parallel (default: 6)
    --version VER           MC server version for auto-launching MCC
                            (default: 1.21.11-Vanilla)
    --server-port PORT      MC server port for auto-launched clients
                            (default: 25565)

Filter examples:
    --filter linear                     All linear tests
    --filter linear/flat                Only linear flat (dy=0)
    --filter linear/ascend              Only linear ascend (dy>0)
    --filter linear/descend/dy-1        Linear descend, dy=-1 only
    --filter neo                        All neo tests
    --filter ceiling                    All ceiling tests
    --filter ceiling/headhitter/ceil2.5 Ceiling with height 2.5
    --filter linear-flat-gap4           Exact case_id match
    --filter sidewall/flat/wo0          Sidewall flat with wall_offset=0

    Multiple filters: --filter linear,sidewall
"""

from __future__ import annotations

import argparse
import json
import math
import os
import queue
import re
import socket
import struct
import subprocess
import sys
import threading
import time
import uuid
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, Optional

REPO_ROOT = Path(__file__).resolve().parent.parent
CAPABILITIES_PATH = REPO_ROOT / "tools" / "pathing_data" / "momentum-capabilities.json"

SEGMENTS = 3
CLEAR_PADDING = 8   # air padding on each side of a course in Z
Y_CLEAR_HALF = 30   # clear 30 blocks above and below floor in Y
LINEAR_RUNWAY = 4
SIDEWALL_RUNWAY = 2

CEILING_HEIGHTS_TO_TEST = [2, 3, 4]

SKIP_FAMILIES: set[str] = set()

NEO_MAX_WALL_WIDTH = 3  # theoretical max passable width; 4 is the first reject
POLL_INTERVAL_SECONDS = 0.25
TURN_STALL_MIN_SAMPLES = 4
TURN_STALL_WINDOW_MAX_TRAVEL = 0.35
TURN_STALL_MIN_CUMULATIVE_YAW = 180.0
TURN_STALL_MIN_PER_STEP_YAW = 35.0

ANSI_ESCAPE_RE = re.compile(r"\x1b\[[0-9;]*m")
ENTITY_POS_RE = re.compile(r"\[\s*(-?[\d.]+)d,\s*(-?[\d.]+)d,\s*(-?[\d.]+)d\]")
ENTITY_ROT_RE = re.compile(r"\[\s*(-?[\d.]+)f,\s*(-?[\d.]+)f\]")
DEBUG_STATE_LOCATION_RE = re.compile(
    r"Location\s*:?\s*(?P<x>-?[\d.]+),\s*(?P<y>-?[\d.]+),\s*(?P<z>-?[\d.]+)"
)
DEBUG_STATE_ON_GROUND_RE = re.compile(r"OnGround\s*:?\s*(?P<value>true|false)", re.IGNORECASE)
ROUTE_COMPLETE_RE = re.compile(
    r"\[PathMetric\] routeComplete totalTicks=(?P<ticks>\d+)(?: replans=(?P<replans>\d+))?"
)
SEGMENT_COMPLETE_RE = re.compile(
    r"\[PathMetric\] segmentComplete .* x=(?P<x>-?[\d.]+) y=(?P<y>-?[\d.]+) z=(?P<z>-?[\d.]+)"
)
SEGMENT_FAILED_RE = re.compile(
    r"\[PathMetric\] segmentFailed .* x=(?P<x>-?[\d.]+) y=(?P<y>-?[\d.]+) z=(?P<z>-?[\d.]+)"
)
REPLAN_START_RE = re.compile(r"\[PathMetric\] replanStart count=(?P<count>\d+)")

REJECT_PATTERNS = (
    "failed to compute a safe path",
    "not a reachable",
    "no path",
)


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


def connect_rcon_with_retry(
    host: str = "localhost",
    port: int = 25575,
    password: str = "test123",
    timeout_seconds: float = 20.0,
    poll_interval: float = 0.5,
    client_factory: Callable[..., RconClient] | None = None,
) -> RconClient:
    deadline = time.monotonic() + timeout_seconds
    last_error: Exception | None = None

    while time.monotonic() < deadline:
        client = client_factory(host=host, port=port, password=password) if client_factory else RconClient(host=host, port=port, password=password)
        try:
            client.connect()
            return client
        except Exception as exc:
            last_error = exc
            try:
                client.close()
            except Exception:
                pass
            time.sleep(poll_interval)

    if last_error is not None:
        raise RuntimeError(f"RCON unavailable on {host}:{port}") from last_error
    raise RuntimeError(f"RCON unavailable on {host}:{port}")


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

    def log_length(self) -> int:
        if not self.log_file.exists():
            return 0
        return self.log_file.stat().st_size

    def read_log(self) -> str:
        if not self.log_file.exists():
            return ""
        return self.log_file.read_text(errors="replace").replace("\x00", "")

    def read_log_from(self, offset: int) -> str:
        if not self.log_file.exists():
            return ""
        with self.log_file.open("rb") as f:
            f.seek(offset)
            return f.read().decode(errors="replace").replace("\x00", "")

    def strip_ansi(self, text: str) -> str:
        return ANSI_ESCAPE_RE.sub("", text)


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

        if family == "neo":
            max_reach = min(max_reach, NEO_MAX_WALL_WIDTH)

        for value in range(0, max_reach + 2):
            if family == "neo" and value == 0:
                continue

            if family == "ceiling" and value == 0:
                continue

            if family == "sidewall":
                wt = (wo or 0) + 1
                if value <= wt:
                    continue

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
    Z_START = 100

    def __init__(self, rcon: RconClient, base_x: int = 100, base_y: int = 80):
        self.rcon = rcon
        self.base_x = base_x
        self.base_y = base_y
        self._z_cursor = self.Z_START

    def allocate_z(self, width: int = 1) -> int:
        """Allocate a Z band for a course.

        Layout: [CLEAR_PADDING] [course width] ...
        Adjacent courses share the padding between them: the trailing
        padding of course N is the leading padding of course N+1.
        """
        z = self._z_cursor + CLEAR_PADDING
        self._z_cursor = z + width
        return z

    def reset_z(self) -> None:
        self._z_cursor = self.Z_START

    def compute_z_extent(self, cases: list) -> int:
        """Dry-run Z allocation to find the final Z cursor value."""
        saved = self._z_cursor
        for case in cases:
            width = self._course_z_width(case)
            self.allocate_z(width)
        end = self._z_cursor + CLEAR_PADDING
        self._z_cursor = saved
        return end

    def _course_z_width(self, case: TestCase) -> int:
        if case.family == "sidewall":
            gap = case.gap_or_wall
            return gap * SEGMENTS + 5
        elif case.family == "neo":
            return 5
        else:
            return 1

    def forceload_region(self, z_end: int) -> None:
        """Force-load all chunks covering the test region."""
        x_min = self.base_x - 20
        x_max = self.base_x + 40
        z_min = self.Z_START - CLEAR_PADDING
        z_max = z_end
        cx_min = x_min >> 4
        cx_max = x_max >> 4
        cz_min = z_min >> 4
        cz_max = z_max >> 4
        for cx in range(cx_min, cx_max + 1):
            self.rcon.command(
                f"forceload add {cx * 16} {cz_min * 16} {cx * 16 + 15} {cz_max * 16 + 15}"
            )
        print(f"  Force-loaded chunks: X=[{cx_min},{cx_max}] Z=[{cz_min},{cz_max}]")

    def forceload_remove(self) -> None:
        self.rcon.command("forceload remove all")

    def clear_entire_region(self, z_end: int) -> None:
        """Clear the full test region from Z_START to z_end."""
        x_min = self.base_x - 20
        x_max = self.base_x + 40
        y_min = self.base_y - 1 - Y_CLEAR_HALF
        y_max = self.base_y - 1 + Y_CLEAR_HALF
        z_min = self.Z_START - CLEAR_PADDING
        z_max = z_end
        print(f"  Clearing region: X=[{x_min},{x_max}] Y=[{y_min},{y_max}] Z=[{z_min},{z_max}]")
        self._fill_volume(x_min, y_min, z_min, x_max, y_max, z_max, "air")

    def _fill_volume(self, x1: int, y1: int, z1: int,
                     x2: int, y2: int, z2: int, block: str) -> None:
        """Fill a volume respecting MC's 32768-block-per-call limit."""
        dx = x2 - x1 + 1
        dy = y2 - y1 + 1
        max_z_per_call = max(1, 32768 // (dx * dy))
        for cz in range(z1, z2 + 1, max_z_per_call):
            ez = min(cz + max_z_per_call - 1, z2)
            self.rcon.command(f"fill {x1} {y1} {cz} {x2} {y2} {ez} {block}")

    def set_block(self, x: int, y: int, z: int, block: str = "stone") -> None:
        self.rcon.command(f"setblock {x} {y} {z} {block}")

    def fill_blocks(self, x1: int, y1: int, z1: int, x2: int, y2: int, z2: int,
                    block: str = "stone") -> None:
        self.rcon.command(f"fill {x1} {y1} {z1} {x2} {y2} {z2} {block}")

    def _make_layout(self, bx: int, by: int, bz: int,
                     end_x: int, end_y: int, end_z: int) -> CourseLayout:
        return CourseLayout(
            start_x=bx, start_y=by, start_z=bz,
            end_x=end_x, end_y=end_y, end_z=end_z,
            clear_min=(0, 0, 0), clear_max=(0, 0, 0),
        )

    def build_linear_route(self, case: TestCase) -> CourseLayout:
        gap = case.gap_or_wall
        dy = case.delta_y
        bx = self.base_x
        by = self.base_y
        bz = self.allocate_z()
        floor_y = by - 1

        self.fill_blocks(bx, floor_y, bz, bx + LINEAR_RUNWAY - 1, floor_y, bz)

        last_x = bx + LINEAR_RUNWAY - 1
        last_y = floor_y

        for seg in range(SEGMENTS):
            plat_x = last_x + gap + 1
            plat_y = last_y + int(dy)
            self.set_block(plat_x, plat_y, bz)
            last_x = plat_x
            last_y = plat_y

        return self._make_layout(bx, by, bz, last_x, last_y + 1, bz)

    def build_neo_route(self, case: TestCase) -> CourseLayout:
        """Neo jump: wall blocks the +X path, player detours via +Z.

        Top view of one segment (wall_width=3):

            Z ^
              |
          bz+1|  ...(detour: player jumps to Z+1 in air, past wall, back)...
              |
          bz  |  [Runway]  [W][W][W]  [Land][Land]  [W][W][W] ...
              |  X=0..3    X=4..6      X=7..8        X=9..11
              +----------------------------------------------> X

        The wall is wall_width blocks in X, 1 block in Z (at Z=bz), 8 tall.
        2-block landing gap between consecutive walls.
        """
        wall_width = case.gap_or_wall
        bx = self.base_x
        by = self.base_y
        bz = self.allocate_z(width=5)
        floor_y = by - 1

        cur_x = bx

        self.fill_blocks(bx, floor_y, bz, bx + LINEAR_RUNWAY - 1, floor_y, bz)
        cur_x += LINEAR_RUNWAY - 1

        for seg in range(SEGMENTS):
            wall_start_x = cur_x + 1
            wall_end_x = wall_start_x + wall_width - 1
            if wall_width > 0:
                self.fill_blocks(wall_start_x, floor_y, bz,
                                 wall_end_x, floor_y + 7, bz)

            land_x1 = wall_end_x + 1
            land_x2 = land_x1 + 1
            self.set_block(land_x1, floor_y, bz)
            self.set_block(land_x2, floor_y, bz)

            cur_x = land_x2

        return self._make_layout(bx, by, bz, cur_x, by, bz)

    def build_sidewall_route(self, case: TestCase) -> CourseLayout:
        """Around-the-wall jump (绕墙跳).

        Parameters:
          gap_or_wall = gap  : Z-distance between start and target platforms
          wall_offset = wall_thickness (1 or 2): Z-depth of the wall
          delta_y: Y-offset of target relative to start (+1, 0, -1, -2)

        Top view (one segment, gap=3, wall_thickness=1, dy=+1):

            Z ^
              |
          bz+3|  [Target]  X=bx-1, Y=floor_y+dy
              |
          bz+2|  (air)
              |
          bz+1|  (air)
              |
          bz  |  [Start]   X=bx       [WALL] X=bx-1, Z=bz..bz+wt-1, 8 tall
              +-------> X

        The wall is at X=bx-1 (one block to -X of start), starts at same
        Z as the start platform, extends wt blocks in +Z, and is 8 tall.
        The target is at X=bx-1, Z=bz+gap, Y=floor_y+dy.
        Player must jump from start, around the wall's -X edge,
        and land on the target.
        """
        gap = case.gap_or_wall
        dy = int(case.delta_y)
        wt = (case.wall_offset or 0) + 1  # wall_offset=0 -> 1 thick, =1 -> 2 thick
        bx = self.base_x
        by = self.base_y
        floor_y = by - 1

        total_z = gap * SEGMENTS + 5
        bz = self.allocate_z(width=total_z)

        cur_x = bx
        cur_y = floor_y
        cur_z = bz

        # Starting platform with 2-block runway in -Z direction
        self.fill_blocks(cur_x, cur_y, cur_z - 2, cur_x, cur_y, cur_z)

        for seg in range(SEGMENTS):
            wall_x = cur_x - 1
            wall_z_start = cur_z
            wall_z_end = cur_z + wt - 1
            wall_y_low = min(cur_y, cur_y + dy) - 1
            wall_y_high = max(cur_y, cur_y + dy) + 7

            self.fill_blocks(wall_x, wall_y_low, wall_z_start,
                             wall_x, wall_y_high, wall_z_end)

            land_x = cur_x - 1
            land_y = cur_y + dy
            land_z = cur_z + gap

            self.set_block(land_x, land_y, land_z)

            cur_x = land_x
            cur_y = land_y
            cur_z = land_z

        return self._make_layout(bx, by, bz, cur_x, cur_y + 1, cur_z)

    def build_ceiling_route(self, case: TestCase) -> CourseLayout:
        gap = case.gap_or_wall
        ceil_height = case.ceiling_height or 4.0
        bx = self.base_x
        by = self.base_y
        bz = self.allocate_z()
        floor_y = by - 1

        self.fill_blocks(bx, floor_y, bz, bx + LINEAR_RUNWAY - 1, floor_y, bz)

        last_x = bx + LINEAR_RUNWAY - 1
        for seg in range(SEGMENTS):
            plat_x = last_x + gap + 1
            self.set_block(plat_x, floor_y, bz)
            last_x = plat_x

        ceil_block_y = floor_y + 1 + int(ceil_height)
        self.fill_blocks(bx - 1, ceil_block_y, bz - 1, last_x + 1, ceil_block_y, bz + 1)

        return self._make_layout(bx, by, bz, last_x, by, bz)

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

@dataclass(frozen=True)
class NavigationSample:
    x: float
    y: float
    z: float
    yaw: float


@dataclass
class LiveMetrics:
    route_start_count: int = 0
    route_complete_count: int = 0
    navigation_complete_count: int = 0
    segment_failed_count: int = 0
    replan_count: int = 0
    replan_failed_count: int = 0
    planner_reject_count: int = 0
    generic_fail_count: int = 0
    final_metric_position: tuple[float, float, float] | None = None
    total_ticks: int | None = None
    turn_stall_count: int = 0


@dataclass
class TestResult:
    case: TestCase
    outcome: str  # "pass", "reject", "fail", "invalid_live_case"
    matched_expected: bool
    replan_count: int = 0
    turn_stall_count: int = 0
    near_goal: bool | None = None
    final_position: tuple[float, float, float] | None = None
    total_ticks: int | None = None
    log_excerpt: str = ""
    session: str | None = None
    log_path: str | None = None
    event_log_path: str | None = None
    duration_ms: int | None = None
    error_kind: str | None = None
    skip_reason: str | None = None


@dataclass(frozen=True)
class DebugStateSnapshot:
    location: tuple[float, float, float]
    on_ground: bool | None


def resolve_session() -> str:
    explicit = os.environ.get("SESSION", "")
    if explicit:
        return explicit
    repo = os.environ.get("MCC_REPO_ROOT", "")
    if repo:
        return Path(repo).name
    return Path.cwd().name


def make_parallel_run_token() -> str:
    return uuid.uuid4().hex[:10]


def build_worker_session_name(run_token: str, worker_id: int) -> str:
    return f"parkour-{run_token}-{worker_id}"


def build_case_session_name(run_token: str, worker_id: int, case_index: int) -> str:
    return f"parkour-{run_token}-{worker_id}-c{case_index}"


def build_worker_username(base_username: str, worker_id: int) -> str:
    return f"{base_username}{worker_id}"


def build_case_username(base_username: str, worker_id: int, case_index: int) -> str:
    return f"{base_username}{worker_id}c{case_index}"


def parse_entity_position(text: str) -> tuple[float, float, float] | None:
    match = ENTITY_POS_RE.search(text)
    if not match:
        return None
    return float(match.group(1)), float(match.group(2)), float(match.group(3))


def parse_entity_rotation(text: str) -> tuple[float, float] | None:
    match = ENTITY_ROT_RE.search(text)
    if not match:
        return None
    return float(match.group(1)), float(match.group(2))


def parse_debug_state_location(text: str) -> tuple[float, float, float] | None:
    snapshot = parse_debug_state_snapshot(text)
    if snapshot is None:
        return None
    return snapshot.location


def parse_debug_state_snapshot(text: str) -> DebugStateSnapshot | None:
    clean_text = ANSI_ESCAPE_RE.sub("", text).replace("\x00", "")
    blocks = [block for block in clean_text.split("=== MCC Debug State ===") if block.strip()]
    if not blocks:
        return None

    block = blocks[-1]
    location_match = DEBUG_STATE_LOCATION_RE.search(block)
    if location_match is None:
        return None

    on_ground_match = DEBUG_STATE_ON_GROUND_RE.search(block)
    return DebugStateSnapshot(
        location=(
            float(location_match.group("x")),
            float(location_match.group("y")),
            float(location_match.group("z")),
        ),
        on_ground=None if on_ground_match is None else on_ground_match.group("value").lower() == "true",
    )


def is_near_expected_position(
    actual: tuple[float, float, float],
    expected: tuple[float, float, float],
    horiz_tolerance: float = 0.18,
    vert_tolerance: float = 0.05,
) -> bool:
    return (
        abs(actual[0] - expected[0]) <= horiz_tolerance
        and abs(actual[2] - expected[2]) <= horiz_tolerance
        and math.floor(actual[1]) == math.floor(expected[1])
        and abs(actual[1] - expected[1]) <= vert_tolerance
    )


def wait_for_local_start_sync(
    mcc: MccClient,
    expected_position: tuple[float, float, float],
    timeout_seconds: float = 8.0,
    stable_reads_required: int = 3,
) -> bool:
    log_offset = mcc.log_length()
    deadline = time.monotonic() + timeout_seconds
    stable_reads = 0
    last_snapshot: DebugStateSnapshot | None = None

    while time.monotonic() < deadline:
        mcc.send("debug state")
        time.sleep(0.25)

        snapshot = parse_debug_state_snapshot(mcc.read_log_from(log_offset))
        if snapshot is None:
            time.sleep(0.15)
            continue

        if is_near_expected_position(snapshot.location, expected_position) and snapshot.on_ground is True:
            if (
                last_snapshot is None
                or last_snapshot.on_ground is not True
                or is_near_expected_position(
                    snapshot.location,
                    last_snapshot.location,
                    horiz_tolerance=0.08,
                    vert_tolerance=0.08,
                )
            ):
                stable_reads += 1
            else:
                stable_reads = 1

            last_snapshot = snapshot
            if stable_reads >= stable_reads_required:
                return True
        else:
            stable_reads = 0
            last_snapshot = snapshot

        time.sleep(0.15)

    return False


def get_player_sample(rcon: RconClient, username: str) -> NavigationSample | None:
    try:
        pos = parse_entity_position(rcon.command(f"data get entity {username} Pos"))
        rotation = parse_entity_rotation(rcon.command(f"data get entity {username} Rotation"))
    except Exception:
        return None

    if pos is None or rotation is None:
        return None

    return NavigationSample(
        x=pos[0],
        y=pos[1],
        z=pos[2],
        yaw=rotation[0],
    )


def normalize_yaw_delta(previous_yaw: float, current_yaw: float) -> float:
    delta = (current_yaw - previous_yaw + 180.0) % 360.0 - 180.0
    return abs(delta)


def horizontal_distance(a: NavigationSample, b: NavigationSample) -> float:
    return math.hypot(a.x - b.x, a.z - b.z)


def count_turn_stalls(samples: list[NavigationSample]) -> int:
    if len(samples) < TURN_STALL_MIN_SAMPLES:
        return 0

    count = 0
    window_start = 0
    while window_start <= len(samples) - TURN_STALL_MIN_SAMPLES:
        base = samples[window_start]
        cumulative_yaw = 0.0
        large_swings = 0
        matched = False

        for idx in range(window_start + 1, len(samples)):
            sample = samples[idx]
            if horizontal_distance(base, sample) > TURN_STALL_WINDOW_MAX_TRAVEL:
                break

            yaw_delta = normalize_yaw_delta(samples[idx - 1].yaw, sample.yaw)
            cumulative_yaw += yaw_delta
            if yaw_delta >= TURN_STALL_MIN_PER_STEP_YAW:
                large_swings += 1

            sample_count = idx - window_start + 1
            if (
                sample_count >= TURN_STALL_MIN_SAMPLES
                and large_swings >= TURN_STALL_MIN_SAMPLES - 1
                and cumulative_yaw >= TURN_STALL_MIN_CUMULATIVE_YAW
            ):
                count += 1
                window_start = idx + 1
                matched = True
                break

        if not matched:
            window_start += 1

    return count


def parse_live_metrics(text: str) -> LiveMetrics:
    metrics = LiveMetrics()
    clean_text = ANSI_ESCAPE_RE.sub("", text).replace("\x00", "")

    for raw_line in clean_text.splitlines():
        line = raw_line.strip()
        lower = line.lower()

        if "[PathMetric] routeStart" in line:
            metrics.route_start_count += 1

        if "[PathMgr] Navigation complete!" in line:
            metrics.navigation_complete_count += 1

        route_match = ROUTE_COMPLETE_RE.search(line)
        if route_match:
            metrics.route_complete_count += 1
            metrics.total_ticks = int(route_match.group("ticks"))
            replans = route_match.group("replans")
            if replans is not None:
                metrics.replan_count = max(metrics.replan_count, int(replans))

        replan_match = REPLAN_START_RE.search(line)
        if replan_match:
            metrics.replan_count = max(metrics.replan_count, int(replan_match.group("count")))

        segment_complete_match = SEGMENT_COMPLETE_RE.search(line)
        if segment_complete_match:
            metrics.final_metric_position = (
                float(segment_complete_match.group("x")),
                float(segment_complete_match.group("y")),
                float(segment_complete_match.group("z")),
            )

        segment_failed_match = SEGMENT_FAILED_RE.search(line)
        if segment_failed_match:
            metrics.segment_failed_count += 1
            metrics.final_metric_position = (
                float(segment_failed_match.group("x")),
                float(segment_failed_match.group("y")),
                float(segment_failed_match.group("z")),
            )

        if "replan failed" in lower or "giving up" in lower:
            metrics.replan_failed_count += 1

        planner_reject_line = (
            any(pattern in lower for pattern in REJECT_PATTERNS)
            or ("[a*]" in lower and "failed" in lower)
            or ("a* result: failed" in lower)
        )
        if planner_reject_line:
            metrics.planner_reject_count += 1

        if (
            "failed" in lower
            and "replan failed" not in lower
            and "failed to compute a safe path" not in lower
            and not planner_reject_line
        ):
            metrics.generic_fail_count += 1

    return metrics


def has_terminal_metrics(metrics: LiveMetrics) -> bool:
    return (
        metrics.route_complete_count > 0
        or metrics.segment_failed_count > 0
        or metrics.replan_failed_count > 0
        or (metrics.planner_reject_count > 0 and metrics.route_start_count == 0)
    )


def wait_for_rcon_ready(
    rcon: RconClient,
    timeout_seconds: float = 20.0,
    poll_interval: float = 0.5,
) -> bool:
    deadline = time.monotonic() + timeout_seconds
    while time.monotonic() < deadline:
        try:
            rcon.command("list")
            return True
        except Exception:
            time.sleep(poll_interval)
    return False


def is_near_goal(
    position: tuple[float, float, float] | None,
    layout: CourseLayout,
) -> bool | None:
    if position is None:
        return None

    px, py, pz = position
    goal_x = layout.end_x + 0.5
    goal_y = float(layout.end_y)
    goal_z = layout.end_z + 0.5
    return (
        abs(px - goal_x) <= 1.25
        and abs(pz - goal_z) <= 1.25
        and abs(py - goal_y) <= 2.0
    )


def classify_outcome(
    metrics: LiveMetrics,
    near_goal: bool | None,
    error_kind: str | None = None,
) -> str:
    if error_kind is not None:
        return error_kind

    if metrics.segment_failed_count > 0 or metrics.replan_failed_count > 0 or metrics.generic_fail_count > 0:
        return "fail"

    if metrics.route_complete_count > 0 or metrics.navigation_complete_count > 0:
        if metrics.replan_count > 0 or metrics.turn_stall_count > 0:
            return "fail"
        if near_goal is False:
            return "fail"
        return "pass"

    if metrics.planner_reject_count > 0 and metrics.route_start_count == 0:
        return "reject"

    return "invalid_live_case"


def run_single_test(
    case: TestCase,
    layout: CourseLayout,
    rcon: RconClient,
    mcc: MccClient,
    username: str,
    wait_seconds: int = 15,
) -> TestResult:
    start_time = time.monotonic()
    log_offset = mcc.log_length()

    mcc.send(f"send ===== TEST: {case.case_id} (expect: {case.expected}) =====")
    time.sleep(0.2)
    mcc.send(f"goto {layout.end_x} {layout.end_y} {layout.end_z}")
    deadline = time.monotonic() + wait_seconds
    settle_deadline: float | None = None
    samples: list[NavigationSample] = []
    metrics = LiveMetrics()

    while time.monotonic() < deadline:
        sample = get_player_sample(rcon, username)
        if sample is not None:
            samples.append(sample)

        log = mcc.strip_ansi(mcc.read_log_from(log_offset))
        metrics = parse_live_metrics(log)
        if has_terminal_metrics(metrics):
            if settle_deadline is None:
                settle_deadline = time.monotonic() + 0.5
            elif time.monotonic() >= settle_deadline:
                break

        time.sleep(POLL_INTERVAL_SECONDS)

    raw_log = mcc.read_log_from(log_offset)
    log = mcc.strip_ansi(raw_log)
    all_lines = log.splitlines()
    metrics = parse_live_metrics(log)
    metrics.turn_stall_count = count_turn_stalls(samples)

    a_star_lines = [l for l in all_lines if "[A*]" in l][:3]
    path_mgr_lines = [l for l in all_lines if "[PathMgr]" in l]

    sampled_position = None
    if samples:
        last_sample = samples[-1]
        sampled_position = (last_sample.x, last_sample.y, last_sample.z)

    if metrics.route_complete_count > 0 and metrics.final_metric_position is not None:
        final_position = metrics.final_metric_position
    else:
        final_position = sampled_position or metrics.final_metric_position

    near_goal = is_near_goal(final_position, layout)
    outcome = classify_outcome(metrics, near_goal)

    excerpt_lines = []
    if a_star_lines:
        excerpt_lines.append(f"  A*: {a_star_lines[0]}")
    if path_mgr_lines:
        excerpt_lines.append(f"  Mgr: {path_mgr_lines[-1]}")
    excerpt_lines.append(
        f"  Metrics: routes={metrics.route_complete_count} replans={metrics.replan_count} "
        f"turn_stalls={metrics.turn_stall_count} ticks={metrics.total_ticks}"
    )
    if final_position is not None:
        px, py, pz = final_position
        goal_x = layout.end_x + 0.5
        goal_y = float(layout.end_y)
        goal_z = layout.end_z + 0.5
        excerpt_lines.append(
            f"  Pos: ({px:.1f},{py:.1f},{pz:.1f}) goal=({goal_x:.1f},{goal_y:.1f},{goal_z:.1f}) "
            f"near={near_goal}"
        )
    relevant = [l for l in all_lines if "path" in l.lower() or "move" in l.lower()
                or "navigate" in l.lower() or "A*" in l]
    if not excerpt_lines and relevant:
        excerpt_lines.append(f"  Log: {relevant[0]}")

    return TestResult(
        case=case,
        outcome=outcome,
        matched_expected=(outcome == case.expected),
        replan_count=metrics.replan_count,
        turn_stall_count=metrics.turn_stall_count,
        near_goal=near_goal,
        final_position=final_position,
        total_ticks=metrics.total_ticks,
        log_excerpt="\n".join(excerpt_lines),
        session=mcc.session,
        log_path=str(mcc.log_file),
        duration_ms=int((time.monotonic() - start_time) * 1000),
    )


# ---------------------------------------------------------------------------
# Stop-at-first-failure logic
# ---------------------------------------------------------------------------

def should_skip(case: TestCase, failed_groups: set[tuple]) -> bool:
    """Skip this case if its group already had a failure at a smaller gap."""
    return case.group_key() in failed_groups


def make_skip_result(case: TestCase, reason: str) -> TestResult:
    return TestResult(
        case=case,
        outcome="skipped",
        matched_expected=False,
        skip_reason=reason,
    )


def make_harness_result(
    case: TestCase,
    error_kind: str,
    log_excerpt: str,
    session: str | None = None,
    log_path: str | None = None,
) -> TestResult:
    return TestResult(
        case=case,
        outcome=error_kind,
        matched_expected=False,
        log_excerpt=log_excerpt,
        session=session,
        log_path=log_path,
        error_kind=error_kind,
    )


def result_to_record(result: TestResult, worker_id: int | None = None) -> dict[str, object]:
    record: dict[str, object] = {
        "case_id": result.case.case_id,
        "family": result.case.family,
        "subfamily": result.case.subfamily,
        "gap_or_wall": result.case.gap_or_wall,
        "expected": result.case.expected,
        "outcome": result.outcome,
        "matched": result.matched_expected,
        "replan_count": result.replan_count,
        "turn_stall_count": result.turn_stall_count,
        "near_goal": result.near_goal,
        "total_ticks": result.total_ticks,
        "final_position": list(result.final_position) if result.final_position is not None else None,
        "session": result.session,
        "log_path": result.log_path,
        "event_log_path": result.event_log_path,
        "duration_ms": result.duration_ms,
        "error_kind": result.error_kind,
        "skip_reason": result.skip_reason,
    }
    if worker_id is not None:
        record["worker"] = worker_id
    return record


def summarize_results(records: list[dict[str, object]]) -> dict[str, object]:
    summary: dict[str, object] = {
        "total": len(records),
        "matched": sum(1 for r in records if bool(r.get("matched"))),
        "mismatched": sum(1 for r in records if not bool(r.get("matched"))),
        "families": {},
    }

    families: dict[str, dict[str, object]] = {}
    for record in records:
        family = str(record.get("family", "unknown"))
        outcome = str(record.get("outcome", "unknown"))
        matched = bool(record.get("matched"))
        family_summary = families.setdefault(
            family,
            {
                "total": 0,
                "matched": 0,
                "mismatches": 0,
                "outcomes": {},
            },
        )
        family_summary["total"] = int(family_summary["total"]) + 1
        if matched:
            family_summary["matched"] = int(family_summary["matched"]) + 1
        else:
            family_summary["mismatches"] = int(family_summary["mismatches"]) + 1
        outcomes = family_summary["outcomes"]
        assert isinstance(outcomes, dict)
        outcomes[outcome] = int(outcomes.get(outcome, 0)) + 1

    summary["families"] = families
    return summary


def create_run_dir(base_dir: Path | None = None) -> Path:
    root = base_dir or (Path(os.environ.get("TMPDIR", "/tmp")) / "parkour-runs")
    timestamp = time.strftime("%Y%m%d-%H%M%S", time.gmtime())
    run_dir = root / f"{timestamp}-{make_parallel_run_token()}"
    run_dir.mkdir(parents=True, exist_ok=False)
    return run_dir


def write_summary_files(run_dir: Path, summary: dict[str, object]) -> None:
    run_dir.mkdir(parents=True, exist_ok=True)
    summary_json = run_dir / "summary.json"
    summary_md = run_dir / "summary.md"

    summary_json.write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    lines = [
        "# Parkour Summary",
        "",
        f"- Total: {summary.get('total', 0)}",
        f"- Matched: {summary.get('matched', 0)}",
        f"- Mismatched: {summary.get('mismatched', 0)}",
        "",
        "## Families",
        "",
    ]

    families = summary.get("families", {})
    if isinstance(families, dict):
        for family, family_summary in sorted(families.items()):
            lines.append(f"### {family}")
            if isinstance(family_summary, dict):
                lines.append(f"- Total: {family_summary.get('total', 0)}")
                lines.append(f"- Matched: {family_summary.get('matched', 0)}")
                lines.append(f"- Mismatches: {family_summary.get('mismatches', 0)}")
                outcomes = family_summary.get("outcomes", {})
                if isinstance(outcomes, dict):
                    for outcome, count in sorted(outcomes.items()):
                        lines.append(f"- {outcome}: {count}")
            lines.append("")

    summary_md.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def append_jsonl_record(paths: list[Path], record: dict[str, object]) -> None:
    payload = json.dumps(record) + "\n"
    seen: set[Path] = set()
    for path in paths:
        if path in seen:
            continue
        seen.add(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("a", encoding="utf-8") as f:
            f.write(payload)


# ---------------------------------------------------------------------------
# Parallel worker infrastructure
# ---------------------------------------------------------------------------

@dataclass
class WorkerContext:
    worker_id: int
    username: str
    session: str
    rcon: RconClient
    mcc: MccClient


_print_lock = threading.Lock()


def _tprint(*args: object, **kwargs: object) -> None:
    """Thread-safe print."""
    with _print_lock:
        print(*args, **kwargs)


def _launch_one_mcc(worker_id: int, username: str, session: str,
                     version: str, server_port: int) -> None:
    """Start one MCC instance via mcc-debug. Blocks until mcc-debug returns."""
    mcc_env_sh = REPO_ROOT / "tools" / "mcc-env.sh"
    shell_cmd = (
        f"source {mcc_env_sh} && "
        f"mcc-debug --session {session} --username {username} "
        f"--file-input -v {version} -p {server_port} "
        f"--no-build --debug-on"
    )
    result = subprocess.run(["bash", "-c", shell_cmd],
                            capture_output=True, text=True)
    if result.returncode != 0:
        _tprint(f"  [W{worker_id}] ERROR launching MCC:")
        if result.stdout.strip():
            _tprint(f"    stdout: {result.stdout.strip()}")
        if result.stderr.strip():
            _tprint(f"    stderr: {result.stderr.strip()}")
        raise RuntimeError(f"Failed to launch worker {worker_id}")


def _wait_for_join(mcc: MccClient, timeout: float = 30) -> bool:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        log = mcc.read_log()
        if "Server was successfully joined" in log:
            return True
        time.sleep(1)
    return False


def launch_worker_context(
    worker_id: int,
    username: str,
    session: str,
    version: str,
    server_port: int,
    rcon_port: int,
    rcon_password: str,
) -> WorkerContext | None:
    _tprint(f"  [W{worker_id}] Launching: session={session} username={username}")
    try:
        _launch_one_mcc(worker_id, username, session, version, server_port)
    except RuntimeError:
        _tprint(f"  [W{worker_id}] Failed to launch, exiting case.")
        return None

    rcon = connect_rcon_with_retry(port=rcon_port, password=rcon_password, timeout_seconds=10.0)
    mcc = MccClient(session)

    if _wait_for_join(mcc):
        _tprint(f"  [W{worker_id}] {username} connected to server.")
    else:
        _tprint(f"  [W{worker_id}] WARNING: {username} join not confirmed "
                "(continuing anyway)")

    try:
        admin_rcon = RconClient(port=rcon_port, password=rcon_password)
        admin_rcon.connect()
        admin_rcon.command(f"op {username}")
        admin_rcon.close()
    except Exception:
        pass

    mcc.send("debug on")
    time.sleep(2)

    return WorkerContext(
        worker_id=worker_id,
        username=username,
        session=session,
        rcon=rcon,
        mcc=mcc,
    )


def register_worker_context(
    ctx: WorkerContext,
    workers_registry: list[WorkerContext],
    registry_lock: threading.Lock,
) -> None:
    with registry_lock:
        workers_registry.append(ctx)


def reset_worker_state(ctx: WorkerContext, layout: CourseLayout) -> bool:
    expected_start_position = (
        layout.start_x + 0.5,
        float(layout.start_y),
        layout.start_z + 0.5,
    )

    for _attempt in range(2):
        ctx.rcon.command(f"gamemode creative {ctx.username}")
        ctx.rcon.command(
            f"tp {ctx.username} {layout.start_x}.5 {layout.start_y} {layout.start_z}.5"
        )
        time.sleep(2)
        ctx.rcon.command(f"gamemode survival {ctx.username}")
        time.sleep(0.5)
        if wait_for_local_start_sync(ctx.mcc, expected_start_position):
            return True
        time.sleep(0.5)

    return False


def worker_loop(
    worker_id: int,
    base_username: str,
    run_token: str,
    version: str,
    server_port: int,
    rcon_port: int,
    rcon_password: str,
    group_queue: queue.Queue,
    all_results: list[TestResult],
    results_lock: threading.Lock,
    wait_seconds: int,
    results_paths: list[Path],
    workers_registry: list[WorkerContext],
    registry_lock: threading.Lock,
    skipped_counter: list[int],
) -> None:
    """Run assigned groups while reusing one MCC session per worker."""
    local_results: list[TestResult] = []
    local_skipped = 0
    failed_groups: set[tuple] = set()
    worker_session = build_worker_session_name(run_token, worker_id)
    worker_username = build_worker_username(base_username, worker_id)
    ctx: WorkerContext | None = None

    def write_result(result: TestResult) -> None:
        with results_lock:
            append_jsonl_record(results_paths, result_to_record(result, worker_id))

    def ensure_worker() -> WorkerContext | None:
        nonlocal ctx
        if ctx is not None:
            return ctx

        launched = launch_worker_context(
            worker_id=worker_id,
            username=worker_username,
            session=worker_session,
            version=version,
            server_port=server_port,
            rcon_port=rcon_port,
            rcon_password=rcon_password,
        )
        if launched is not None:
            ctx = launched
            register_worker_context(ctx, workers_registry, registry_lock)
        return ctx

    while True:
        try:
            _, items = group_queue.get_nowait()
        except queue.Empty:
            break

        for case, layout in items:
            if case.group_key() in failed_groups:
                local_skipped += 1
                _tprint(f"  [W{worker_id}] {case.case_id} -- SKIPPED")
                skipped = make_skip_result(case, "group_failed_earlier")
                local_results.append(skipped)
                write_result(skipped)
                continue

            _tprint(f"  [W{worker_id}] {case.case_id} (expect: {case.expected})"
                    f"  route=({layout.start_x},{layout.start_y},{layout.start_z})"
                    f" -> ({layout.end_x},{layout.end_y},{layout.end_z})")

            current_ctx = ensure_worker()
            if current_ctx is None:
                result = make_harness_result(
                    case,
                    error_kind="harness_worker_launch_failed",
                    log_excerpt="  Harness: failed to launch worker session",
                    session=worker_session,
                )
            else:
                reset_ok = False
                try:
                    reset_ok = reset_worker_state(current_ctx, layout)
                except Exception:
                    reset_ok = False

                if not reset_ok:
                    cleanup_workers([current_ctx])
                    ctx = None
                    current_ctx = ensure_worker()
                    if current_ctx is not None:
                        try:
                            reset_ok = reset_worker_state(current_ctx, layout)
                        except Exception:
                            reset_ok = False

                if current_ctx is None:
                    result = make_harness_result(
                        case,
                        error_kind="harness_worker_launch_failed",
                        log_excerpt="  Harness: failed to relaunch worker session",
                        session=worker_session,
                    )
                elif not reset_ok:
                    result = make_harness_result(
                        case,
                        error_kind="harness_start_sync_failed",
                        log_excerpt=(
                            "  Harness: local MCC position did not stabilize at test start "
                            f"goal=({layout.start_x + 0.5:.1f},{float(layout.start_y):.1f},{layout.start_z + 0.5:.1f})"
                        ),
                        session=current_ctx.session,
                        log_path=str(current_ctx.mcc.log_file),
                    )
                else:
                    result = run_single_test(
                        case,
                        layout,
                        current_ctx.rcon,
                        current_ctx.mcc,
                        current_ctx.username,
                        wait_seconds,
                    )

            local_results.append(result)

            status = "OK" if result.matched_expected else "MISMATCH"
            _tprint(f"  [W{worker_id}] {case.case_id}: "
                    f"{result.outcome} [{status}]")
            if result.log_excerpt:
                _tprint(result.log_excerpt)

            if result.outcome in ("reject", "fail") and case.expected == "pass":
                failed_groups.add(case.group_key())
                _tprint(f"  [W{worker_id}] >> Group failed -- "
                        f"skipping larger values")

            write_result(result)

        group_queue.task_done()

    with results_lock:
        all_results.extend(local_results)
        skipped_counter[0] += local_skipped

    _tprint(f"  [W{worker_id}] Finished: {len(local_results)} tests, "
            f"{local_skipped} skipped")


def cleanup_workers(workers: list[WorkerContext]) -> None:
    """Shut down all MCC instances."""
    for w in workers:
        try:
            w.mcc.send("quit")
        except Exception:
            pass
        w.rcon.close()

    time.sleep(2)

    for w in workers:
        tmux_session = f"mcc-{w.session}"
        subprocess.run(
            ["tmux", "kill-session", "-t", tmux_session],
            capture_output=True,
        )

    # Clean up pid/meta files
    tmpdir = os.environ.get("TMPDIR", "/tmp")
    for w in workers:
        session_root = Path(tmpdir) / "mcc-debug" / w.session
        for f in ["mcc.pid", "session.meta"]:
            fpath = session_root / f
            if fpath.exists():
                fpath.unlink()


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
    parser.add_argument("--username", type=str, default="MCCBot",
                        help="MCC username (base name when --parallel > 1)")
    parser.add_argument("--wait", type=int, default=15,
                        help="Seconds to wait for navigation per test")
    parser.add_argument("--results", type=str, default=None,
                        help="Path for JSONL results output")
    parser.add_argument("--parallel", type=int, default=6,
                        help="Number of parallel MCC instances (default: 6)")
    parser.add_argument("--version", type=str, default="1.21.11-Vanilla",
                        help="MC server version for auto-launching MCC")
    parser.add_argument("--server-port", type=int, default=25565,
                        help="MC server port for auto-launched clients")
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

    try:
        rcon = connect_rcon_with_retry(
            port=args.rcon_port,
            password=args.rcon_password,
            timeout_seconds=30.0,
        )
    except Exception as exc:
        print(f"Harness error: RCON unavailable on localhost:{args.rcon_port}: {exc}")
        sys.exit(2)

    rcon.command("difficulty peaceful")
    rcon.command("gamerule doMobSpawning false")
    rcon.command("time set day")

    builder = WorldBuilder(rcon)

    if args.dry_run:
        print("=== DRY RUN: building worlds only ===")
        rcon.command(f"gamemode creative {args.username}")

        z_end = builder.compute_z_extent(all_cases)
        builder.forceload_region(z_end)
        print("  Clearing entire test region...")
        builder.clear_entire_region(z_end)

        for i, case in enumerate(all_cases, 1):
            layout = builder.build(case)
            print(f"  {case.case_id}: start=({layout.start_x},{layout.start_y},{layout.start_z})"
                  f" end=({layout.end_x},{layout.end_y},{layout.end_z})")

        builder.forceload_remove()
        rcon.command(f"tp {args.username} {builder.base_x}.5 "
                     f"{builder.base_y + 5} {builder.Z_START + CLEAR_PADDING}.5")
        rcon.close()
        print(f"\nBuilt {len(all_cases)} courses.")
        return

    run_dir = create_run_dir()
    canonical_results_path = run_dir / "results.jsonl"
    results_paths = [canonical_results_path]
    if args.results:
        results_paths.append(Path(args.results))
    for path in results_paths:
        path.parent.mkdir(parents=True, exist_ok=True)

    # Phase 1: Clear region and build all courses up front
    print("=" * 60)
    print("  Phase 1: Building all courses")
    print("=" * 60)
    print(f"  Run artifacts: {run_dir}")
    print(f"  Results JSONL: {canonical_results_path}")
    if args.results:
        print(f"  External Results JSONL: {Path(args.results)}")

    rcon.command(f"gamemode creative {args.username}")

    z_end = builder.compute_z_extent(all_cases)
    builder.forceload_region(z_end)
    print("  Clearing entire test region...")
    builder.clear_entire_region(z_end)

    layouts: list[tuple[TestCase, CourseLayout]] = []
    for i, case in enumerate(all_cases, 1):
        layout = builder.build(case)
        layouts.append((case, layout))
        print(f"  [{i}/{len(all_cases)}] {case.case_id}: "
              f"({layout.start_x},{layout.start_y},{layout.start_z}) -> "
              f"({layout.end_x},{layout.end_y},{layout.end_z})")

    print(f"\n  Built {len(layouts)} courses.")

    # Phase 2: Run tests
    n_parallel = args.parallel
    try:
        if n_parallel > 1:
            _run_parallel(layouts, rcon, args, results_paths, run_dir, n_parallel)
        else:
            _run_serial(layouts, rcon, args, results_paths, run_dir)
    finally:
        builder.forceload_remove()


def _run_serial(
    layouts: list[tuple[TestCase, CourseLayout]],
    rcon: RconClient,
    args: argparse.Namespace,
    results_paths: list[Path],
    run_dir: Path,
) -> None:
    """Original serial test execution path."""
    session = resolve_session()
    mcc = MccClient(session)

    print()
    print("=" * 60)
    print("  Phase 2: Running tests (serial)")
    print("=" * 60)
    print(f"  Cases: {len(layouts)}")
    print(f"  Username: {args.username}")
    print(f"  Session: {session}")
    print(f"  Wait: {args.wait}s per test")
    print()

    results: list[TestResult] = []
    failed_groups: set[tuple] = set()
    skipped = 0
    serial_ctx = WorkerContext(
        worker_id=0,
        username=args.username,
        session=session,
        rcon=rcon,
        mcc=mcc,
    )

    for i, (case, layout) in enumerate(layouts, 1):
        if should_skip(case, failed_groups):
            skipped += 1
            print(f"  [{i}/{len(layouts)}] {case.case_id} -- SKIPPED (group already failed)")
            skipped_result = make_skip_result(case, "group_failed_earlier")
            results.append(skipped_result)
            append_jsonl_record(results_paths, result_to_record(skipped_result))
            continue

        print(f"\n--- [{i}/{len(layouts)}] {case.case_id} (expect: {case.expected}) ---")
        print(f"  Route: ({layout.start_x},{layout.start_y},{layout.start_z}) -> "
              f"({layout.end_x},{layout.end_y},{layout.end_z})")

        if reset_worker_state(serial_ctx, layout):
            result = run_single_test(
                case, layout, rcon, mcc, args.username, args.wait,
            )
        else:
            result = make_harness_result(
                case,
                error_kind="harness_start_sync_failed",
                log_excerpt=(
                    "  Harness: local MCC position did not stabilize at test start "
                    f"goal=({layout.start_x + 0.5:.1f},{float(layout.start_y):.1f},{layout.start_z + 0.5:.1f})"
                ),
                session=session,
                log_path=str(mcc.log_file),
            )
        results.append(result)

        status = "OK" if result.matched_expected else "MISMATCH"
        print(f"  Outcome: {result.outcome} [{status}]")
        if result.log_excerpt:
            print(result.log_excerpt)

        if result.outcome in ("reject", "fail") and case.expected == "pass":
            failed_groups.add(case.group_key())
            print(f"  >> Group failed at {case.family}/{case.subfamily} "
                  f"gap/wall={case.gap_or_wall} -- skipping larger values")

        append_jsonl_record(results_paths, result_to_record(result))

    rcon.close()
    _print_summary(results, skipped, run_dir)


def _run_parallel(
    layouts: list[tuple[TestCase, CourseLayout]],
    rcon: RconClient,
    args: argparse.Namespace,
    results_paths: list[Path],
    run_dir: Path,
    n_parallel: int,
) -> None:
    """Parallel test execution with streaming worker launch.

    Each worker thread handles its own lifecycle: launch MCC, wait for join,
    op/debug-on, then immediately start pulling work from the shared queue.
    No need to wait for all workers before starting tests.
    """
    # Group cases by group_key for atomic distribution
    groups: dict[tuple, list[tuple[TestCase, CourseLayout]]] = {}
    for case, layout in layouts:
        groups.setdefault(case.group_key(), []).append((case, layout))

    group_q: queue.Queue = queue.Queue()
    for key, items in groups.items():
        group_q.put((key, items))

    print()
    print("=" * 60)
    print(f"  Launching {n_parallel} workers ({len(groups)} groups, "
          f"{len(layouts)} cases)")
    print("=" * 60)
    print(f"  Wait: {args.wait}s per test")
    print()

    all_results: list[TestResult] = []
    results_lock = threading.Lock()
    workers_registry: list[WorkerContext] = []
    registry_lock = threading.Lock()
    threads: list[threading.Thread] = []
    skipped_counter = [0]
    run_token = make_parallel_run_token()

    for i in range(1, n_parallel + 1):
        t = threading.Thread(
            target=worker_loop,
            args=(i, args.username, run_token, args.version, args.server_port,
                  args.rcon_port, args.rcon_password,
                  group_q, all_results, results_lock,
                  args.wait, results_paths,
                  workers_registry, registry_lock, skipped_counter),
            daemon=True,
        )
        threads.append(t)
        t.start()

    for t in threads:
        t.join()

    rcon.close()

    print()
    print("=" * 60)
    print("  Cleanup")
    print("=" * 60)
    cleanup_workers(workers_registry)
    print("  All workers stopped.")

    _print_summary(all_results, skipped=skipped_counter[0], run_dir=run_dir)


def _print_summary(results: list[TestResult], skipped: int, run_dir: Path) -> None:
    print("\n" + "=" * 60)
    print("  SUMMARY")
    print("=" * 60)

    passed = [r for r in results if r.matched_expected]
    failed = [r for r in results if not r.matched_expected]
    summary = summarize_results([result_to_record(r) for r in results])
    write_summary_files(run_dir, summary)

    print(f"\n  {len(passed)}/{len(results)} matched expectations")
    if skipped:
        print(f"  {skipped} cases skipped (stop-at-first-failure)")
    print(f"  Summary dir: {run_dir}")

    if failed:
        print(f"\n  MISMATCHES ({len(failed)}):")
        for r in failed:
            print(f"    {r.case.case_id}: expected={r.case.expected} got={r.outcome}")

    sys.exit(0 if not failed else 1)


if __name__ == "__main__":
    main()
