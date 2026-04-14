#!/usr/bin/env python3
import argparse
import json
from pathlib import Path
import sys

REPO_ROOT = Path(__file__).resolve().parent.parent
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))

from tools.pathing_theory.report import build_report


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Join theory-aligned live results back to canonical cases.",
    )
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--results", required=True)
    parser.add_argument("--json-out", required=True)
    args = parser.parse_args()

    report = build_report(Path(args.manifest), Path(args.results))
    Path(args.json_out).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote report to {args.json_out}")


if __name__ == "__main__":
    main()
