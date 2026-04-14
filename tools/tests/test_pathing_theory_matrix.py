import unittest
from pathlib import Path

from tools.pathing_theory.simulator import build_theory_cases


class PathingTheoryMatrixTests(unittest.TestCase):
    def test_build_theory_cases_returns_first_wave_families(self) -> None:
        cases = build_theory_cases()
        families = {(case.family, case.subfamily) for case in cases}

        self.assertIn(("linear", "flat"), families)
        self.assertIn(("linear", "ascend"), families)
        self.assertIn(("linear", "descend"), families)
        self.assertIn(("neo", "neo"), families)
        self.assertIn(("ceiling", "headhitter"), families)

        linear_boundary = next(
            case
            for case in cases
            if case.case_id == "linear-flat-sprint-mm12-gap5-dy0p0"
        )
        self.assertTrue(linear_boundary.expected_reachable)
        self.assertGreater(linear_boundary.margin, 0.0)

    def test_theory_markdown_mentions_canonical_live_coverage(self) -> None:
        markdown = Path("tools/pathing_data/theory-matrix.md").read_text(encoding="utf-8")
        self.assertIn("Canonical live coverage", markdown)


if __name__ == "__main__":
    unittest.main()
