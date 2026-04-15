import subprocess
import unittest

from tools.pathing_theory.capabilities import build_momentum_capability_bands
from tools.pathing_theory.simulator import build_theory_cases


class PathingCapabilityTests(unittest.TestCase):
    def test_linear_descend_sprint_dy_minus2_compresses_into_mm_breakpoints(self) -> None:
        bands = build_momentum_capability_bands(build_theory_cases())
        rows = [
            band
            for band in bands
            if band.family == "linear"
            and band.subfamily == "descend"
            and band.movement_mode == "sprint"
            and band.delta_y == -2.0
        ]

        self.assertEqual(
            [(band.min_mm, band.max_mm, band.max_reach) for band in rows],
            [(0, 0, 4), (1, 12, 5)],
        )

    def test_linear_ascend_walk_dy_plus1_compresses_into_mm_breakpoints(self) -> None:
        bands = build_momentum_capability_bands(build_theory_cases())
        rows = [
            band
            for band in bands
            if band.family == "linear"
            and band.subfamily == "ascend"
            and band.movement_mode == "walk"
            and band.delta_y == 1.0
        ]

        self.assertEqual(
            [(band.min_mm, band.max_mm, band.max_reach) for band in rows],
            [(0, 0, 1), (1, 12, 2)],
        )

    def test_ceiling_sprint_height_2p5_has_late_mm_breakpoint(self) -> None:
        bands = build_momentum_capability_bands(build_theory_cases())
        rows = [
            band
            for band in bands
            if band.family == "ceiling"
            and band.subfamily == "headhitter"
            and band.movement_mode == "sprint"
            and band.ceiling_height == 2.5
        ]

        self.assertEqual(
            [(band.min_mm, band.max_mm, band.max_reach) for band in rows],
            [(0, 7, 2), (8, 12, 3)],
        )

    def test_sim_jump_reach_lists_momentum_capabilities(self) -> None:
        result = subprocess.run(
            ["python3", "tools/sim_jump_reach.py", "--list-capabilities"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("linear | descend | sprint | dy=-2.0 | mm=0..0 | max_gap=4", result.stdout)
        self.assertIn("linear | ascend | walk | dy=1.0 | mm=1..12 | max_gap=2", result.stdout)


if __name__ == "__main__":
    unittest.main()
