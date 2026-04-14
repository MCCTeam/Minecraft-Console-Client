import json
from pathlib import Path


def classify_live_result(expected_result: str, live_result: str) -> str:
    if live_result == "invalid_live_case":
        return "invalid_live_case"
    if expected_result == "pass" and live_result == "pass":
        return "expected_pass/live_pass"
    if expected_result == "pass" and live_result == "fail":
        return "expected_pass/live_fail"
    if expected_result == "reject" and live_result == "reject":
        return "expected_reject/live_reject"
    if expected_result == "reject" and live_result == "pass":
        return "expected_reject/live_unexpected_pass"
    return "invalid_live_case"


def summarize_results(rows: list[dict]) -> dict[str, int]:
    summary: dict[str, int] = {}
    for row in rows:
        key = classify_live_result(row["expected_result"], row["live_result"])
        summary[key] = summary.get(key, 0) + 1
    return summary


def build_report(manifest_path: Path, results_path: Path) -> dict:
    manifest_rows = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest_by_case = {row["case_id"]: row for row in manifest_rows}
    result_rows = [
        json.loads(line)
        for line in results_path.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]

    joined_rows: list[dict] = []
    for row in result_rows:
        manifest = manifest_by_case.get(row["case_id"])
        if manifest is None:
            joined_rows.append({**row, "classification": "invalid_live_case"})
            continue
        joined_rows.append(
            {
                **manifest,
                **row,
                "classification": classify_live_result(
                    manifest["expected_result"],
                    row["live_result"],
                ),
            }
        )

    return {
        "rows": joined_rows,
        "summary": summarize_results(joined_rows),
    }
