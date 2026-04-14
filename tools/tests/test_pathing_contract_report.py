from pathlib import Path

import pytest

from tools.pathing_contract_report import MetricsReport, SegmentMetric, parse_metrics, validate_report


def test_parse_metrics_reads_route_and_segment_ticks(tmp_path: Path) -> None:
    fixture = Path("tools/testdata/pathing-contract-report.sample.log")
    log = tmp_path / "sample.log"
    log.write_text(fixture.read_text(encoding="utf-8"), encoding="utf-8")

    report = parse_metrics(log.read_text(encoding="utf-8"))

    assert report.total_ticks == 70
    assert [segment.ticks for segment in report.segments] == [17, 16]


def test_validate_report_includes_route_and_segment_table_on_budget_failure() -> None:
    report = MetricsReport(
        segments=[SegmentMetric(index=0, move="Parkour", ticks=17)],
        total_ticks=70,
        replans=0,
        planned=[],
    )
    planner_contract = {"segments": []}
    timing_budget = {
        "expectedTotalTicks": 60,
        "maxTotalTicks": 65,
        "segments": [
            {"moveType": "Parkour", "expectedTicks": 15, "maxTicks": 16},
        ],
    }

    with pytest.raises(SystemExit) as exc:
        validate_report("sample-scenario", report, planner_contract, timing_budget)

    message = str(exc.value)
    assert "Route sample-scenario: actual=70 expected=60 max=65" in message
    assert "seg[0] move=Parkour actual=17 expected=15 max=16 delta=+2" in message
