import json
import tempfile
import unittest
from pathlib import Path

from tools.pathing_theory.report import build_report, classify_live_result, summarize_results


class PathingTheoryReportTests(unittest.TestCase):
    def test_classify_live_result_distinguishes_expected_pass_and_reject(self) -> None:
        self.assertEqual(classify_live_result("pass", "pass"), "expected_pass/live_pass")
        self.assertEqual(classify_live_result("pass", "fail"), "expected_pass/live_fail")
        self.assertEqual(classify_live_result("reject", "reject"), "expected_reject/live_reject")
        self.assertEqual(classify_live_result("reject", "pass"), "expected_reject/live_unexpected_pass")

    def test_summarize_results_counts_each_status(self) -> None:
        rows = [
            {"case_id": "a", "expected_result": "pass", "live_result": "pass"},
            {"case_id": "b", "expected_result": "pass", "live_result": "fail"},
            {"case_id": "c", "expected_result": "reject", "live_result": "reject"},
        ]

        summary = summarize_results(rows)

        self.assertEqual(summary["expected_pass/live_pass"], 1)
        self.assertEqual(summary["expected_pass/live_fail"], 1)
        self.assertEqual(summary["expected_reject/live_reject"], 1)

    def test_build_report_keeps_case_traceability_fields(self) -> None:
        manifest_rows = [
            {
                "case_id": "linear-flat-sprint-mm12-gap5-dy0p0",
                "bucket_id": "linear:flat:sprint:boundary",
                "world_recipe_id": "linear-flat",
                "expected_result": "pass",
            }
        ]
        result_row = {
            "case_id": "linear-flat-sprint-mm12-gap5-dy0p0",
            "live_result": "pass",
            "log_path": "/tmp/mcc-debug/mcc-debug.log",
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            manifest_path = Path(temp_dir) / "manifest.json"
            results_path = Path(temp_dir) / "results.jsonl"
            manifest_path.write_text(json.dumps(manifest_rows), encoding="utf-8")
            results_path.write_text(json.dumps(result_row) + "\n", encoding="utf-8")

            report = build_report(manifest_path, results_path)

        row = report["rows"][0]
        self.assertEqual(row["bucket_id"], "linear:flat:sprint:boundary")
        self.assertEqual(row["world_recipe_id"], "linear-flat")
        self.assertEqual(row["log_path"], "/tmp/mcc-debug/mcc-debug.log")
        self.assertEqual(row["classification"], "expected_pass/live_pass")


if __name__ == "__main__":
    unittest.main()
