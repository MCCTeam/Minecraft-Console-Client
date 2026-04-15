import json
import tempfile
import unittest
from pathlib import Path

from tools.pathing_theory.capabilities import build_momentum_capability_bands
from tools.pathing_theory.canonical import build_canonical_live_cases
from tools.pathing_theory.renderers import write_theory_artifacts
from tools.pathing_theory.simulator import build_theory_cases


class CanonicalPathingCaseTests(unittest.TestCase):
    def test_build_canonical_live_cases_picks_easy_boundary_and_reject(self) -> None:
        canonical_cases = build_canonical_live_cases(build_theory_cases())
        bucket_ids = {case.bucket_id for case in canonical_cases}

        self.assertTrue(all(case.movement_mode == "sprint" for case in canonical_cases))
        self.assertTrue(all(case.momentum_ticks == 12 for case in canonical_cases))
        self.assertIn("linear:flat:sprint:easy", bucket_ids)
        self.assertIn("linear:flat:sprint:reject", bucket_ids)
        self.assertIn("linear:descend:sprint:boundary", bucket_ids)
        self.assertIn("neo:neo:sprint:boundary", bucket_ids)
        self.assertIn("ceiling:headhitter:sprint:boundary", bucket_ids)

    def test_write_theory_artifacts_writes_json_csv_and_markdown_from_same_cases(self) -> None:
        cases = build_theory_cases()

        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = Path(temp_dir)
            write_theory_artifacts(
                cases,
                build_canonical_live_cases(cases),
                build_momentum_capability_bands(cases),
                output_dir,
            )

            json_path = output_dir / "theory-matrix.json"
            csv_path = output_dir / "theory-matrix.csv"
            md_path = output_dir / "theory-matrix.md"
            canonical_path = output_dir / "canonical-live-cases.json"
            capability_path = output_dir / "momentum-capabilities.json"
            capability_md_path = output_dir / "momentum-capabilities.md"

            self.assertTrue(json_path.exists())
            self.assertTrue(csv_path.exists())
            self.assertTrue(md_path.exists())
            self.assertTrue(canonical_path.exists())
            self.assertTrue(capability_path.exists())
            self.assertTrue(capability_md_path.exists())

            exported_cases = json.loads(json_path.read_text())
            self.assertEqual(len(cases), len(exported_cases))
            self.assertIn("| family | subfamily | movement_mode |", md_path.read_text())


if __name__ == "__main__":
    unittest.main()
