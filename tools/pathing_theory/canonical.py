from tools.pathing_theory.models import CanonicalLiveCase, TheoryCase


def _world_recipe_id(case: TheoryCase) -> str:
    if case.family == "linear":
        return f"linear-{case.subfamily}"
    if case.family == "neo":
        return "neo-wall"
    return "ceiling-headhitter"


def _canonical_goal(case: TheoryCase) -> tuple[dict[str, float], dict[str, float]]:
    start = {"x": 100.5, "y": 80.0, "z": 100.5}
    if case.family == "linear":
        goal_y = 80.0 + (case.delta_y or 0.0)
        goal_x = 100 + (case.gap_blocks or 0) + 1
        return start, {"x": float(goal_x), "y": goal_y, "z": 100.0}
    if case.family == "neo":
        goal_z = 100 + (case.wall_width or 1)
        return start, {"x": 102.0, "y": 80.0, "z": float(goal_z)}
    goal_x = 100 + (case.gap_blocks or 0) + 1
    return start, {"x": float(goal_x), "y": 80.0, "z": 100.0}


def build_canonical_live_cases(cases: list[TheoryCase]) -> list[CanonicalLiveCase]:
    live_candidate_cases = [
        case
        for case in cases
        if case.movement_mode == "sprint" and case.momentum_ticks == 12
    ]

    by_bucket: dict[tuple[str, str, str], list[TheoryCase]] = {}
    for case in live_candidate_cases:
        by_bucket.setdefault(
            (case.family, case.subfamily, case.movement_mode),
            [],
        ).append(case)

    canonical_cases: list[CanonicalLiveCase] = []
    for family, subfamily, movement_mode in sorted(by_bucket):
        bucket_cases = by_bucket[(family, subfamily, movement_mode)]
        reachable = sorted(
            [
                case
                for case in bucket_cases
                if case.expected_reachable and case.margin is not None
            ],
            key=lambda case: case.margin,
        )
        unreachable = sorted(
            [case for case in bucket_cases if not case.expected_reachable],
            key=lambda case: float("-inf") if case.margin is None else abs(case.margin),
        )

        selected: list[tuple[str, TheoryCase]] = []
        if reachable:
            easy = next(
                (case for case in reversed(reachable) if (case.margin or 0.0) >= 0.50),
                reachable[-1],
            )
            boundary = reachable[0]
            selected.append(("easy", easy))
            if boundary.case_id != easy.case_id:
                selected.append(("boundary", boundary))
        if unreachable:
            selected.append(("reject", unreachable[0]))

        for difficulty_band, case in selected:
            start, goal = _canonical_goal(case)
            canonical_cases.append(
                CanonicalLiveCase(
                    case_id=case.case_id,
                    bucket_id=f"{family}:{subfamily}:{movement_mode}:{difficulty_band}",
                    family=family,
                    subfamily=subfamily,
                    movement_mode=movement_mode,
                    momentum_ticks=case.momentum_ticks,
                    difficulty_band=difficulty_band,
                    expected_result="pass" if case.expected_reachable else "reject",
                    world_recipe_id=_world_recipe_id(case),
                    gap_blocks=case.gap_blocks,
                    delta_y=case.delta_y,
                    ceiling_height=case.ceiling_height,
                    wall_width=case.wall_width,
                    start=start,
                    goal=goal,
                )
            )

    return canonical_cases
