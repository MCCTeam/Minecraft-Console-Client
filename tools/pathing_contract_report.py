from __future__ import annotations

import argparse
import json
import pathlib
import re
from dataclasses import dataclass

SEGMENT_RE = re.compile(
    r"\[PathMetric\] segmentComplete index=(?P<index>\d+)"
    r"(?: total=(?P<total>\d+))? move=(?P<move>\w+) ticks=(?P<ticks>\d+)"
)
ROUTE_RE = re.compile(
    r"\[PathMetric\] routeComplete totalTicks=(?P<ticks>\d+)(?: replans=(?P<replans>\d+))?"
)
PLAN_RE = re.compile(
    r"\[Navigate\]\s+seg\[(?P<index>\d+)\] = (?P<move>\w+): "
    r"\((?P<x>-?\d+),(?P<y>-?\d+),(?P<z>-?\d+)\)"
)


@dataclass(frozen=True)
class SegmentMetric:
    index: int
    move: str
    ticks: int


@dataclass(frozen=True)
class PlannedSegment:
    index: int
    move: str
    end_block: tuple[int, int, int]


@dataclass(frozen=True)
class MetricsReport:
    segments: list[SegmentMetric]
    total_ticks: int | None
    replans: int | None
    planned: list[PlannedSegment]


def load_json(path: pathlib.Path) -> dict[str, dict]:
    data = json.loads(path.read_text(encoding="utf-8"))
    return {entry["scenarioId"]: entry for entry in data}


def parse_metrics(text: str) -> MetricsReport:
    segments = [
        SegmentMetric(
            index=int(match["index"]),
            move=match["move"],
            ticks=int(match["ticks"]),
        )
        for match in SEGMENT_RE.finditer(text)
    ]
    route_match = ROUTE_RE.search(text)
    planned = [
        PlannedSegment(
            index=int(match["index"]),
            move=match["move"],
            end_block=(int(match["x"]), int(match["y"]), int(match["z"])),
        )
        for match in PLAN_RE.finditer(text)
    ]
    return MetricsReport(
        segments=segments,
        total_ticks=int(route_match["ticks"]) if route_match else None,
        replans=int(route_match["replans"]) if route_match and route_match["replans"] else None,
        planned=planned,
    )


def validate_report(
    scenario_id: str,
    report: MetricsReport,
    planner_contract: dict,
    timing_budget: dict,
) -> None:
    if report.total_ticks is None:
        raise SystemExit("Missing [PathMetric] routeComplete line")

    expected_planner_segments = planner_contract["segments"]
    if len(report.planned) != len(expected_planner_segments):
        raise SystemExit(
            f"Planner contract mismatch for {scenario_id}: expected {len(expected_planner_segments)} "
            f"segments, saw {len(report.planned)} planned segments"
        )

    for expected, actual in zip(expected_planner_segments, report.planned, strict=True):
        expected_end = expected["endBlock"]
        if actual.move != expected["moveType"] or actual.end_block != (
            expected_end["x"],
            expected_end["y"],
            expected_end["z"],
        ):
            raise SystemExit(
                f"Planner contract mismatch for {scenario_id} segment {actual.index}: "
                f"expected {expected['moveType']} -> ({expected_end['x']},{expected_end['y']},{expected_end['z']}), "
                f"saw {actual.move} -> ({actual.end_block[0]},{actual.end_block[1]},{actual.end_block[2]})"
            )

    expected_timing_segments = timing_budget["segments"]
    if len(report.segments) != len(expected_timing_segments):
        raise SystemExit(
            f"Timing contract mismatch for {scenario_id}: expected {len(expected_timing_segments)} "
            f"segment metrics, saw {len(report.segments)}"
        )

    if report.total_ticks > timing_budget["maxTotalTicks"]:
        raise SystemExit(
            f"Route exceeded budget for {scenario_id}: actual={report.total_ticks} "
            f"max={timing_budget['maxTotalTicks']}"
        )

    for expected, actual in zip(expected_timing_segments, report.segments, strict=True):
        if actual.move != expected["moveType"]:
            raise SystemExit(
                f"Timing move mismatch for {scenario_id} segment {actual.index}: "
                f"expected {expected['moveType']}, saw {actual.move}"
            )
        if actual.ticks > expected["maxTicks"]:
            raise SystemExit(
                f"Segment {actual.index} slow for {scenario_id}: move={actual.move} "
                f"actual={actual.ticks} max={expected['maxTicks']}"
            )


def render_report(scenario_id: str, report: MetricsReport, timing_budget: dict) -> str:
    lines = [
        f"Route {scenario_id}: actual={report.total_ticks} "
        f"expected={timing_budget['expectedTotalTicks']} max={timing_budget['maxTotalTicks']}"
    ]
    for expected, actual in zip(timing_budget["segments"], report.segments, strict=True):
        delta = actual.ticks - expected["expectedTicks"]
        lines.append(
            f"  seg[{actual.index}] move={actual.move} actual={actual.ticks} "
            f"expected={expected['expectedTicks']} max={expected['maxTicks']} delta={delta:+d}"
        )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--scenario-id", required=True)
    parser.add_argument("--log-file", required=True)
    parser.add_argument("--from-line", type=int, required=True)
    parser.add_argument("--planner-contracts", required=True)
    parser.add_argument("--timing-budgets", required=True)
    args = parser.parse_args()

    log_path = pathlib.Path(args.log_file)
    log_lines = log_path.read_text(encoding="utf-8", errors="ignore").splitlines()
    text = "\n".join(log_lines[args.from_line :])

    planner_contract = load_json(pathlib.Path(args.planner_contracts))[args.scenario_id]
    timing_budget = load_json(pathlib.Path(args.timing_budgets))[args.scenario_id]
    report = parse_metrics(text)

    validate_report(args.scenario_id, report, planner_contract, timing_budget)
    print(render_report(args.scenario_id, report, timing_budget))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
