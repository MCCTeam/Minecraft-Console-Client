from pathlib import Path

from tools.pathing_contract_report import parse_metrics


def test_parse_metrics_reads_route_and_segment_ticks(tmp_path: Path) -> None:
    fixture = Path("tools/testdata/pathing-contract-report.sample.log")
    log = tmp_path / "sample.log"
    log.write_text(fixture.read_text(encoding="utf-8"), encoding="utf-8")

    report = parse_metrics(log.read_text(encoding="utf-8"))

    assert report.total_ticks == 70
    assert [segment.ticks for segment in report.segments] == [17, 16]
