import importlib.util
import queue
import sys
import threading
import tempfile
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
    class FakeRcon:
        def __init__(self, responses: list[str | Exception]) -> None:
            self._responses = list(responses)
            self.commands: list[str] = []

        def command(self, cmd: str) -> str:
            self.commands.append(cmd)
            if not self._responses:
                return "ok"
            response = self._responses.pop(0)
            if isinstance(response, Exception):
                raise response
            return response

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

    def test_classify_outcome_prefers_harness_error(self) -> None:
        metrics = module.LiveMetrics(route_complete_count=1, navigation_complete_count=1)

        self.assertEqual(
            module.classify_outcome(metrics, near_goal=True, error_kind="harness_rcon_unavailable"),
            "harness_rcon_unavailable",
        )

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

    def test_wait_for_rcon_ready_retries_until_list_succeeds(self) -> None:
        rcon = self.FakeRcon(
            [
                ConnectionRefusedError("not ready"),
                TimeoutError("still not ready"),
                "There are 0 of a max of 20 players online",
            ]
        )

        clock_ticks = [0]

        def fake_monotonic() -> float:
            clock_ticks[0] += 1
            return clock_ticks[0] * 0.1

        with mock.patch.object(module.time, "sleep", lambda _seconds: None):
            with mock.patch.object(module.time, "monotonic", side_effect=fake_monotonic):
                ready = module.wait_for_rcon_ready(
                    rcon,
                    timeout_seconds=1.0,
                    poll_interval=0.1,
                )

        self.assertTrue(ready)
        self.assertEqual(rcon.commands, ["list", "list", "list"])

    def test_connect_rcon_with_retry_retries_initial_connect(self) -> None:
        events: list[str] = []
        attempts = {"count": 0}

        class FakeClient:
            def connect(self) -> None:
                attempts["count"] += 1
                events.append(f"connect-{attempts['count']}")
                if attempts["count"] < 3:
                    raise ConnectionRefusedError("server not ready")

        def fake_factory(*_args, **_kwargs) -> FakeClient:
            return FakeClient()

        clock_ticks = [0]

        def fake_monotonic() -> float:
            clock_ticks[0] += 1
            return clock_ticks[0] * 0.1

        with mock.patch.object(module.time, "sleep", lambda _seconds: None):
            with mock.patch.object(module.time, "monotonic", side_effect=fake_monotonic):
                client = module.connect_rcon_with_retry(
                    host="localhost",
                    port=25575,
                    password="test123",
                    timeout_seconds=1.0,
                    poll_interval=0.1,
                    client_factory=fake_factory,
                )

        self.assertIsNotNone(client)
        self.assertEqual(events, ["connect-1", "connect-2", "connect-3"])

    def test_make_skip_result_records_skip_reason(self) -> None:
        case = module.TestCase(
            case_id="linear-flat-gap4",
            family="linear",
            subfamily="flat",
            gap_or_wall=4,
            delta_y=0.0,
            ceiling_height=None,
            wall_offset=None,
            expected="pass",
        )

        result = module.make_skip_result(case, "group_failed_earlier")

        self.assertEqual(result.outcome, "skipped")
        self.assertEqual(result.skip_reason, "group_failed_earlier")
        self.assertEqual(result.error_kind, None)
        self.assertFalse(result.matched_expected)

    def test_result_to_record_includes_session_paths_and_duration(self) -> None:
        case = module.TestCase(
            case_id="linear-flat-gap1",
            family="linear",
            subfamily="flat",
            gap_or_wall=1,
            delta_y=0.0,
            ceiling_height=None,
            wall_offset=None,
            expected="pass",
        )
        result = module.TestResult(
            case=case,
            outcome="pass",
            matched_expected=True,
            replan_count=0,
            turn_stall_count=0,
            near_goal=True,
            final_position=(109.5, 80.0, 200.5),
            total_ticks=42,
            session="parkour-run123-2",
            log_path="/tmp/parkour-runs/run123/workers/2/worker.log",
            event_log_path="/tmp/parkour-runs/run123/events.jsonl",
            duration_ms=4200,
            error_kind=None,
            skip_reason=None,
        )

        record = module.result_to_record(result, worker_id=2)

        self.assertEqual(record["worker"], 2)
        self.assertEqual(record["session"], "parkour-run123-2")
        self.assertEqual(record["log_path"], "/tmp/parkour-runs/run123/workers/2/worker.log")
        self.assertEqual(record["event_log_path"], "/tmp/parkour-runs/run123/events.jsonl")
        self.assertEqual(record["duration_ms"], 4200)
        self.assertEqual(record["skip_reason"], None)
        self.assertEqual(record["error_kind"], None)

    def test_summarize_results_groups_outcomes_by_family(self) -> None:
        summary = module.summarize_results(
            [
                {"family": "linear", "outcome": "pass", "matched": True},
                {"family": "linear", "outcome": "reject", "matched": True},
                {"family": "linear", "outcome": "skipped", "matched": False},
                {"family": "neo", "outcome": "reject", "matched": False},
            ]
        )

        self.assertEqual(summary["total"], 4)
        self.assertEqual(summary["matched"], 2)
        self.assertEqual(summary["families"]["linear"]["outcomes"]["pass"], 1)
        self.assertEqual(summary["families"]["linear"]["outcomes"]["skipped"], 1)
        self.assertEqual(summary["families"]["neo"]["mismatches"], 1)

    def test_write_summary_files_persists_json_and_markdown(self) -> None:
        summary = {
            "total": 4,
            "matched": 2,
            "mismatched": 2,
            "families": {
                "linear": {
                    "total": 3,
                    "matched": 2,
                    "mismatches": 1,
                    "outcomes": {"pass": 1, "reject": 1, "skipped": 1},
                }
            },
        }

        with tempfile.TemporaryDirectory() as tempdir:
            run_dir = Path(tempdir)
            module.write_summary_files(run_dir, summary)

            summary_json = run_dir / "summary.json"
            summary_md = run_dir / "summary.md"

            self.assertTrue(summary_json.exists())
            self.assertTrue(summary_md.exists())
            self.assertIn('"total": 4', summary_json.read_text(encoding="utf-8"))
            self.assertIn("linear", summary_md.read_text(encoding="utf-8"))

    def test_append_jsonl_record_writes_to_all_requested_paths(self) -> None:
        record = {"case_id": "linear-flat-gap1", "outcome": "pass"}

        with tempfile.TemporaryDirectory() as tempdir:
            base = Path(tempdir)
            path1 = base / "results-a.jsonl"
            path2 = base / "results-b.jsonl"

            module.append_jsonl_record([path1, path2], record)

            self.assertEqual(path1.read_text(encoding="utf-8"), path2.read_text(encoding="utf-8"))
            self.assertIn('"case_id": "linear-flat-gap1"', path1.read_text(encoding="utf-8"))

    def test_worker_loop_reuses_one_worker_context_for_multiple_cases(self) -> None:
        case1 = module.TestCase(
            case_id="linear-flat-gap1",
            family="linear",
            subfamily="flat",
            gap_or_wall=1,
            delta_y=0.0,
            ceiling_height=None,
            wall_offset=None,
            expected="pass",
        )
        case2 = module.TestCase(
            case_id="linear-flat-gap2",
            family="linear",
            subfamily="flat",
            gap_or_wall=2,
            delta_y=0.0,
            ceiling_height=None,
            wall_offset=None,
            expected="pass",
        )
        layout1 = module.CourseLayout(100, 80, 200, 109, 80, 200, (0, 0, 0), (0, 0, 0))
        layout2 = module.CourseLayout(100, 80, 210, 112, 80, 210, (0, 0, 0), (0, 0, 0))
        group_q: queue.Queue = queue.Queue()
        group_q.put((case1.group_key(), [(case1, layout1)]))
        group_q.put((case2.group_key(), [(case2, layout2)]))

        fake_ctx = module.WorkerContext(
            worker_id=1,
            username="MCCBot1",
            session="parkour-run123-1",
            rcon=mock.Mock(),
            mcc=mock.Mock(),
        )
        all_results: list[module.TestResult] = []
        skipped_counter = [0]

        def make_result(case: module.TestCase, *_args, **_kwargs) -> module.TestResult:
            return module.TestResult(case=case, outcome="pass", matched_expected=True)

        with mock.patch.object(module, "launch_worker_context", return_value=fake_ctx) as launch_mock:
            with mock.patch.object(module, "reset_worker_state", create=True, return_value=True) as reset_mock:
                with mock.patch.object(module, "run_single_test", side_effect=make_result) as run_mock:
                    with mock.patch.object(module, "cleanup_workers") as cleanup_mock:
                        module.worker_loop(
                            worker_id=1,
                            base_username="MCCBot",
                            run_token="run123",
                            version="1.21.11-Vanilla",
                            server_port=25565,
                            rcon_port=25575,
                            rcon_password="test123",
                            group_queue=group_q,
                            all_results=all_results,
                            results_lock=threading.Lock(),
                            wait_seconds=15,
                            results_paths=[],
                            workers_registry=[],
                            registry_lock=threading.Lock(),
                            skipped_counter=skipped_counter,
                        )

        self.assertEqual(launch_mock.call_count, 1)
        self.assertEqual(reset_mock.call_count, 2)
        self.assertEqual(run_mock.call_count, 2)
        cleanup_mock.assert_not_called()
        self.assertEqual(len(all_results), 2)


if __name__ == "__main__":
    unittest.main()
