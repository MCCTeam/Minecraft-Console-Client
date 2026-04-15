import subprocess
import unittest


class PathingLiveScriptTests(unittest.TestCase):
    def test_test_parkour_lists_all_families(self) -> None:
        result = subprocess.run(
            ["python3", "tools/test-parkour.py", "--list-cases"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("linear/flat", result.stdout)
        self.assertIn("linear/ascend", result.stdout)
        self.assertIn("linear/descend", result.stdout)
        self.assertIn("neo/neo", result.stdout)
        self.assertIn("ceiling/headhitter", result.stdout)
        self.assertIn("sidewall/flat", result.stdout)
        self.assertIn("sidewall/ascend", result.stdout)
        self.assertIn("sidewall/descend", result.stdout)

    def test_test_parkour_linear_has_reject_at_max_plus_one(self) -> None:
        result = subprocess.run(
            ["python3", "tools/test-parkour.py", "--list-cases", "--family", "linear"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("linear-flat-gap4", result.stdout)
        self.assertIn("linear-flat-gap5", result.stdout)
        self.assertIn("[PASS]", result.stdout)
        self.assertIn("[REJECT]", result.stdout)

    def test_test_parkour_neo_covers_wall_range(self) -> None:
        result = subprocess.run(
            ["python3", "tools/test-parkour.py", "--list-cases", "--family", "neo"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        for w in range(5):
            self.assertIn(f"neo-neo-wall{w}", result.stdout)
        self.assertIn("neo-neo-wall5", result.stdout)
        self.assertIn("[REJECT]", result.stdout)

    def test_test_pathing_theory_neo_ceiling_lists_theory_cases(self) -> None:
        result = subprocess.run(
            ["bash", "tools/test-pathing-theory-neo-ceiling.sh", "--list-cases"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("neo-neo-sprint-mm12-wall1", result.stdout)
        self.assertIn("ceiling-headhitter-sprint-mm12-gap3-ceil2p5", result.stdout)


if __name__ == "__main__":
    unittest.main()
