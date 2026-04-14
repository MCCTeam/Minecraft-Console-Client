import subprocess
import unittest


class PathingLiveScriptTests(unittest.TestCase):
    def test_test_parkour_lists_linear_canonical_cases(self) -> None:
        result = subprocess.run(
            ["bash", "tools/test-parkour.sh", "--list-cases"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("linear-flat-sprint-mm12-gap5-dy0p0", result.stdout)
        self.assertIn("linear-ascend-sprint-mm12-gap2-dy1p0", result.stdout)
        self.assertNotIn("linear-flat-walk-mm12-gap5-dy0p0", result.stdout)
        self.assertNotIn("linear-flat-sprint-mm0-gap3-dy0p0", result.stdout)

    def test_test_pathing_theory_neo_ceiling_lists_theory_cases(self) -> None:
        result = subprocess.run(
            ["bash", "tools/test-pathing-theory-neo-ceiling.sh", "--list-cases"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("neo-neo-sprint-mm12-wall1", result.stdout)
        self.assertIn("ceiling-headhitter-sprint-mm12-gap3-ceil2p0", result.stdout)


if __name__ == "__main__":
    unittest.main()
