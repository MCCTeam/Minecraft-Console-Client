import unittest
from pathlib import Path

from tools.pathing_theory.primitives import (
    can_reach_gap,
    can_reach_gap_with_side_wall,
    get_landing,
)
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
        self.assertIn(("sidewall", "flat"), families)
        self.assertIn(("sidewall", "ascend"), families)
        self.assertIn(("sidewall", "descend"), families)

        linear_boundary = next(
            case
            for case in cases
            if case.case_id == "linear-flat-sprint-mm12-gap5-dy0p0"
        )
        self.assertFalse(linear_boundary.expected_reachable)
        self.assertTrue(
            linear_boundary.margin is None or linear_boundary.margin < 0.0
        )

    def test_walk_momentum_does_not_turn_gap4_ascend_into_reachable(self) -> None:
        ok, landing_x, needed_x = can_reach_gap(
            gap_blocks=4,
            dy=1.0,
            sprint=False,
            momentum_ticks=12,
        )

        self.assertFalse(ok)
        self.assertTrue(landing_x is None or landing_x < needed_x)

    def test_walk_momentum_can_reach_gap2_ascend_with_edge_takeoff_support(self) -> None:
        ok, landing_x, needed_x = can_reach_gap(
            gap_blocks=2,
            dy=1.0,
            sprint=False,
            momentum_ticks=12,
        )

        self.assertTrue(ok)
        self.assertIsNotNone(landing_x)
        self.assertGreaterEqual(landing_x, needed_x)

    def test_walk_gap3_ascend_does_not_snap_up_after_falling_below_platform(self) -> None:
        landing = get_landing(
            sprint=False,
            target_y=1.0,
            landing_x_start=3.5,
            momentum_ticks=12,
        )

        self.assertIsNone(landing)

    def test_sprint_with_run_up_still_treats_flat_gap5_as_unreachable(self) -> None:
        ok, landing_x, needed_x = can_reach_gap(
            gap_blocks=5,
            dy=0.0,
            sprint=True,
            momentum_ticks=12,
        )

        self.assertFalse(ok)
        self.assertTrue(landing_x is None or landing_x < needed_x)

    def test_sidewall_wo0_margin_is_less_or_equal_to_linear(self) -> None:
        """Side wall should never make a jump easier than the open-air linear case."""
        cases = build_theory_cases()
        linear_by_key: dict[tuple, float | None] = {}
        for c in cases:
            if c.family == "linear":
                key = (c.subfamily, c.movement_mode, c.momentum_ticks,
                       c.gap_blocks, c.delta_y)
                linear_by_key[key] = c.margin

        for c in cases:
            if c.family != "sidewall" or c.wall_offset != 0:
                continue
            key = (c.subfamily, c.movement_mode, c.momentum_ticks,
                   c.gap_blocks, c.delta_y)
            lin_margin = linear_by_key.get(key)
            if lin_margin is None or c.margin is None:
                continue
            self.assertLessEqual(
                c.margin, lin_margin + 1e-9,
                f"sidewall margin {c.margin} > linear margin {lin_margin} "
                f"for {c.case_id}",
            )

    def test_sidewall_walk_descend_gap3_mm0_is_unreachable_wo0(self) -> None:
        """The tight walk descend dy=-2 gap3 mm0 should flip to unreachable with wall."""
        ok, _, _ = can_reach_gap_with_side_wall(
            gap_blocks=3, dy=-2.0, wall_offset=0, sprint=False, momentum_ticks=0,
        )
        self.assertFalse(ok)

    def test_sidewall_sprint_flat_gap4_mm12_is_still_reachable_wo0(self) -> None:
        """Sprint flat gap4 with plenty of margin should survive the wall penalty."""
        ok, landing_x, needed_x = can_reach_gap_with_side_wall(
            gap_blocks=4, dy=0.0, wall_offset=0, sprint=True, momentum_ticks=12,
        )
        self.assertTrue(ok)
        self.assertIsNotNone(landing_x)
        self.assertGreater(landing_x, needed_x)

    def test_theory_markdown_mentions_canonical_live_coverage(self) -> None:
        markdown = Path("tools/pathing_data/theory-matrix.md").read_text(encoding="utf-8")
        self.assertIn("Canonical live coverage", markdown)


if __name__ == "__main__":
    unittest.main()
