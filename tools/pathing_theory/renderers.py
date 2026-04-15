import csv
import json
from dataclasses import asdict
from pathlib import Path

from tools.pathing_theory.capabilities import format_momentum_capability_lines
from tools.pathing_theory.models import (
    CanonicalLiveCase,
    MomentumCapabilityBand,
    TheoryCase,
)


def write_theory_artifacts(
    cases: list[TheoryCase],
    canonical_cases: list[CanonicalLiveCase],
    capability_bands: list[MomentumCapabilityBand],
    output_dir: Path,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)

    json_path = output_dir / "theory-matrix.json"
    csv_path = output_dir / "theory-matrix.csv"
    md_path = output_dir / "theory-matrix.md"
    canonical_path = output_dir / "canonical-live-cases.json"
    capability_path = output_dir / "momentum-capabilities.json"
    capability_md_path = output_dir / "momentum-capabilities.md"

    json_path.write_text(
        json.dumps([asdict(case) for case in cases], indent=2) + "\n",
        encoding="utf-8",
    )
    canonical_path.write_text(
        json.dumps([asdict(case) for case in canonical_cases], indent=2) + "\n",
        encoding="utf-8",
    )
    capability_path.write_text(
        json.dumps([asdict(band) for band in capability_bands], indent=2) + "\n",
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
        "## Canonical live coverage",
        "",
        "This file is generated from `tools/pathing_theory/simulator.py` and is the first-wave authority",
        "for theory-aligned linear, neo, headhitter, and sidewall live suites.",
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

    capability_lines = [
        "# Momentum Capabilities",
        "",
        "This file compresses the full theory matrix into `mm` breakpoint bands that can",
        "be consumed directly by the planner.",
        "",
        "| family | subfamily | movement_mode | qualifiers | mm_range | reach |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for band, line in zip(capability_bands, format_momentum_capability_lines(capability_bands)):
        qualifiers: list[str] = []
        if band.delta_y is not None:
            qualifiers.append(f"dy={band.delta_y}")
        if band.ceiling_height is not None:
            qualifiers.append(f"ceil={band.ceiling_height}")
        if band.wall_offset is not None:
            qualifiers.append(f"wo={band.wall_offset}")
        capability_lines.append(
            f"| {band.family} | {band.subfamily} | {band.movement_mode} | "
            f"{', '.join(qualifiers) if qualifiers else '-'} | "
            f"{band.min_mm}..{band.max_mm} | "
            f"{line.split(' | ')[-1]} |"
        )

    capability_md_path.write_text("\n".join(capability_lines) + "\n", encoding="utf-8")
