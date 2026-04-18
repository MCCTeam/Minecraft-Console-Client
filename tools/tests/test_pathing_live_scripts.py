import subprocess
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]


class PathingLiveScriptTests(unittest.TestCase):
    def test_prepare_offline_config_treats_existing_output_ini_as_output_not_template(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            temp_path = Path(tempdir)
            output_ini = temp_path / "MinecraftClient.debug.ini"
            output_ini.write_text(
                "\n".join(
                    [
                        "[Main.General]",
                        'Account = { Login = "OldBot", Password = "" }',
                        'AccountType = "microsoft"',
                        "",
                        "[Main.Advanced]",
                        'MinecraftVersion = "auto"',
                        "TerrainAndMovements = false",
                        "InventoryHandling = false",
                        "EntityHandling = false",
                        "AutoRespawn = false",
                        "",
                    ]
                ),
                encoding="utf-8",
            )

            result = subprocess.run(
                [
                    "bash",
                    str(REPO_ROOT / ".skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh"),
                    str(output_ini),
                    "1.21.11",
                    "MCCBot1",
                ],
                check=False,
                capture_output=True,
                text=True,
                cwd=temp_path,
            )

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse((temp_path / "1.21.11").exists())

            content = output_ini.read_text(encoding="utf-8")
            self.assertIn('Account = { Login = "MCCBot1", Password = "-" }', content)
            self.assertIn('AccountType = "mojang"', content)
            self.assertIn('MinecraftVersion = "1.21.11"', content)

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
            ["python3", "tools/test-parkour.py", "--list-cases", "--filter", "linear"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("linear-flat-gap4", result.stdout)
        self.assertIn("linear-flat-gap5", result.stdout)
        self.assertIn("[PASS]", result.stdout)
        self.assertIn("[REJECT]", result.stdout)

    def test_test_parkour_linear_marks_live_boundary_cases_as_pass(self) -> None:
        result = subprocess.run(
            ["python3", "tools/test-parkour.py", "--list-cases", "--filter", "linear"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("linear-flat-gap4", result.stdout)
        self.assertIn("linear-ascend-gap1-dy+1", result.stdout)
        self.assertIn("linear-descend-gap2-dy-2", result.stdout)
        self.assertIn("linear-descend-gap3-dy-1", result.stdout)
        self.assertIn("linear-flat-gap4                                   gap=4  [PASS]", result.stdout)
        self.assertIn("linear-ascend-gap1-dy+1                            gap=1  [PASS]", result.stdout)
        self.assertIn("linear-descend-gap2-dy-2                           gap=2  [PASS]", result.stdout)
        self.assertIn("linear-descend-gap3-dy-1                           gap=3  [PASS]", result.stdout)

    def test_test_parkour_neo_covers_wall_range(self) -> None:
        result = subprocess.run(
            ["python3", "tools/test-parkour.py", "--list-cases", "--filter", "neo"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        for w in range(1, 5):
            self.assertIn(f"neo-neo-wall{w}", result.stdout)
        self.assertNotIn("neo-neo-wall0", result.stdout)
        self.assertNotIn("neo-neo-wall5", result.stdout)
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
