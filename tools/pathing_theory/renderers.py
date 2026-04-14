import csv
import json
from dataclasses import asdict
from pathlib import Path

from tools.pathing_theory.models import CanonicalLiveCase, TheoryCase


def write_theory_artifacts(
    cases: list[TheoryCase],
    canonical_cases: list[CanonicalLiveCase],
    output_dir: Path,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)

    json_path = output_dir / "theory-matrix.json"
    csv_path = output_dir / "theory-matrix.csv"
    md_path = output_dir / "theory-matrix.md"
    canonical_path = output_dir / "canonical-live-cases.json"

    json_path.write_text(
        json.dumps([asdict(case) for case in cases], indent=2) + "\n",
        encoding="utf-8",
    )
    canonical_path.write_text(
        json.dumps([asdict(case) for case in canonical_cases], indent=2) + "\n",
        encoding="utf-8",
    )

    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(asdict(cases[0]).keys()))
        writer.writeheader()
        for case in cases:
            writer.writerow(asdict(case))

    lines = [
        "# Theory Matrix",
        "",
        "| family | subfamily | movement_mode | case_id | expected_reachable | margin |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for case in cases:
        lines.append(
            f"| {case.family} | {case.subfamily} | {case.movement_mode} | "
            f"{case.case_id} | {case.expected_reachable} | {case.margin} |"
        )

    md_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
