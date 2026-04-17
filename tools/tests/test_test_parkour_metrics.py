import importlib.util
import sys
import unittest
from unittest import mock
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "tools" / "test-parkour.py"

spec = importlib.util.spec_from_file_location("test_parkour_script", SCRIPT_PATH)
assert spec is not None
module = importlib.util.module_from_spec(spec)
assert spec.loader is not None
sys.modules[spec.name] = module
spec.loader.exec_module(module)


class ParkourMetricsTests(unittest.TestCase):
    class FakeMccClient:
        def __init__(self, logs: list[str]) -> None:
            self._logs = logs
            self._read_index = 0
            self.sent_commands: list[str] = []

        def log_length(self) -> int:
            return 0

        def send(self, command: str) -> None:
            self.sent_commands.append(command)

        def read_log_from(self, _offset: int) -> str:
            if self._read_index < len(self._logs):
                log = self._logs[self._read_index]
                self._read_index += 1
                return log
            return self._logs[-1]

    @staticmethod
    def build_debug_state_log(x: float, y: float, z: float, *, on_ground: bool) -> str:
        return "\n".join(
            [
                "=== MCC Debug State ===",
                f"Location  {x:.2f}, {y:.2f}, {z:.2f}",
                f"OnGround  {'true' if on_ground else 'false'}",
            ]
        )

    def test_build_worker_session_name_scopes_workers_to_a_specific_run(self) -> None:
        self.assertEqual(
            module.build_worker_session_name("20260417t1715z", 4),
            "parkour-20260417t1715z-4",
        )

    def test_build_case_session_name_is_unique_per_case_for_same_worker(self) -> None:
        first = module.build_case_session_name("20260417t1715z", 4, 1)
        second = module.build_case_session_name("20260417t1715z", 4, 2)

        self.assertEqual(first, "parkour-20260417t1715z-4-c1")
        self.assertEqual(second, "parkour-20260417t1715z-4-c2")
        self.assertNotEqual(first, second)

    def test_build_case_username_is_unique_per_case_for_same_worker(self) -> None:
        first = module.build_case_username("MCCBot", 4, 1)
        second = module.build_case_username("MCCBot", 4, 2)

        self.assertEqual(first, "MCCBot4c1")
        self.assertEqual(second, "MCCBot4c2")
        self.assertNotEqual(first, second)

    def test_parse_live_metrics_collects_machine_readable_counts(self) -> None:
        log = "\n".join(
            [
                "[DEBUG] [PathMetric] routeStart segments=4",
                "[DEBUG] [PathMetric] segmentStart index=0 total=4 move=Traverse transition=ContinueStraight",
                "[DEBUG] [PathMetric] segmentComplete index=0 total=4 move=Traverse ticks=7 x=101.50 y=80.00 z=315.50",
                "[DEBUG] [PathMetric] replanStart count=1 x=101.50 y=80.00 z=315.50",
                "[DEBUG] [PathMetric] replanSuccess count=1 segments=4",
                "[DEBUG] [PathMetric] routeComplete totalTicks=19 replans=1",
                "[MCC] [PathMgr] Navigation complete!",
            ]
        )

        metrics = module.parse_live_metrics(log)

        self.assertEqual(metrics.route_start_count, 1)
        self.assertEqual(metrics.route_complete_count, 1)
        self.assertEqual(metrics.navigation_complete_count, 1)
        self.assertEqual(metrics.replan_count, 1)
        self.assertEqual(metrics.final_metric_position, (101.5, 80.0, 315.5))

    def test_classify_outcome_pass_requires_zero_replans_and_zero_turn_stalls(self) -> None:
        metrics = module.LiveMetrics(
            route_complete_count=1,
            navigation_complete_count=1,
            replan_count=0,
            turn_stall_count=0,
        )

        self.assertEqual(module.classify_outcome(metrics, near_goal=True), "pass")
        self.assertEqual(module.classify_outcome(metrics, near_goal=False), "fail")

    def test_classify_outcome_replan_is_fail(self) -> None:
        metrics = module.LiveMetrics(
            route_complete_count=1,
            navigation_complete_count=1,
            replan_count=1,
            turn_stall_count=0,
        )

        self.assertEqual(module.classify_outcome(metrics, near_goal=True), "fail")

    def test_classify_outcome_turn_stall_is_fail(self) -> None:
        metrics = module.LiveMetrics(
            route_complete_count=1,
            navigation_complete_count=1,
            replan_count=0,
            turn_stall_count=1,
        )

        self.assertEqual(module.classify_outcome(metrics, near_goal=True), "fail")

    def test_classify_outcome_planner_reject_stays_reject(self) -> None:
        metrics = module.LiveMetrics(planner_reject_count=1)

        self.assertEqual(module.classify_outcome(metrics, near_goal=False), "reject")

    def test_classify_outcome_a_star_failed_without_route_start_is_reject(self) -> None:
        log = "\n".join(
            [
                "[DEBUG] [A*] Start (100,80,324), goal=GoalBlock(121, 80, 324)",
                "[DEBUG] [A*] Failed, 11986 nodes, 1301ms",
                "[MCC] [Navigate] A* result: Failed, nodes=11986, time=1301ms, path length=0",
                "[MCC] [FileInput] No path found (11986 nodes explored in 1301ms)",
            ]
        )

        metrics = module.parse_live_metrics(log)

        self.assertEqual(module.classify_outcome(metrics, near_goal=False), "reject")

    def test_count_turn_stalls_requires_large_yaw_swings_with_low_motion(self) -> None:
        samples = [
            module.NavigationSample(x=100.5, y=80.0, z=100.5, yaw=0.0),
            module.NavigationSample(x=100.55, y=80.0, z=100.5, yaw=70.0),
            module.NavigationSample(x=100.58, y=80.0, z=100.5, yaw=150.0),
            module.NavigationSample(x=100.60, y=80.0, z=100.5, yaw=235.0),
        ]

        self.assertEqual(module.count_turn_stalls(samples), 1)

    def test_parse_entity_fields_reads_position_and_rotation(self) -> None:
        pos = module.parse_entity_position("[1.5d, 80.0d, -12.25d]")
        yaw_pitch = module.parse_entity_rotation("[90.0f, 15.0f]")

        self.assertEqual(pos, (1.5, 80.0, -12.25))
        self.assertEqual(yaw_pitch, (90.0, 15.0))

    def test_parse_debug_state_location_reads_latest_location(self) -> None:
        log = "\n".join(
            [
                "=== MCC Debug State ===",
                "Location  100.50, 80.00, 216.50",
                "=== MCC Debug State ===",
                "Location  112.25, 79.00, 297.50",
            ]
        )

        self.assertEqual(
            module.parse_debug_state_location(log),
            (112.25, 79.0, 297.5),
        )

    def test_parse_debug_state_snapshot_reads_latest_location_and_on_ground(self) -> None:
        log = "\n".join(
            [
                "=== MCC Debug State ===",
                "Location  100.50, 80.00, 216.50",
                "OnGround  true",
                "=== MCC Debug State ===",
                "Location  112.25, 79.00, 297.50",
                "OnGround  false",
            ]
        )

        snapshot = module.parse_debug_state_snapshot(log)

        self.assertIsNotNone(snapshot)
        assert snapshot is not None
        self.assertEqual(snapshot.location, (112.25, 79.0, 297.5))
        self.assertFalse(snapshot.on_ground)

    def test_is_near_expected_position_requires_tight_xyz_tolerance(self) -> None:
        self.assertTrue(
            module.is_near_expected_position(
                (100.5, 80.0, 216.5),
                (100.5, 80.0, 216.5),
            )
        )
        self.assertFalse(
            module.is_near_expected_position(
                (100.5, 79.45, 216.5),
                (100.5, 80.0, 216.5),
            )
        )
        self.assertFalse(
            module.is_near_expected_position(
                (100.72, 80.0, 216.5),
                (100.5, 80.0, 216.5),
            )
        )

    def test_wait_for_local_start_sync_requires_local_on_ground(self) -> None:
        expected = (100.5, 80.0, 225.5)
        client = self.FakeMccClient(
            [
                self.build_debug_state_log(*expected, on_ground=False),
                self.build_debug_state_log(*expected, on_ground=False),
                self.build_debug_state_log(*expected, on_ground=False),
                self.build_debug_state_log(*expected, on_ground=True),
                self.build_debug_state_log(*expected, on_ground=True),
                self.build_debug_state_log(*expected, on_ground=True),
            ]
        )

        clock_ticks = [0]

        def fake_monotonic() -> float:
            clock_ticks[0] += 1
            return clock_ticks[0] * 0.1

        with mock.patch.object(module.time, "sleep", lambda _seconds: None):
            with mock.patch.object(module.time, "monotonic", side_effect=fake_monotonic):
                synced = module.wait_for_local_start_sync(
                    client,
                    expected,
                    timeout_seconds=2.0,
                    stable_reads_required=3,
                )

        self.assertTrue(synced)
        self.assertEqual(client.sent_commands, ["debug state"] * 6)


if __name__ == "__main__":
    unittest.main()
