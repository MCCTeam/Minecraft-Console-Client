from tools.pathing_theory.models import TheoryCase
from tools.pathing_theory.primitives import (
    PLAYER_WIDTH,
    TARGET_BLOCK_WIDTH,
    can_reach_gap,
    can_reach_gap_with_side_wall,
    get_apex,
    get_landing,
)

MAX_MOMENTUM_TICKS = 12


def _float_token(value: float) -> str:
    return f"{value:.1f}".replace("-", "m").replace(".", "p")


def build_theory_cases() -> list[TheoryCase]:
    cases: list[TheoryCase] = []

    for sprint, movement_mode in [
        (False, "walk"),
        (True, "sprint"),
    ]:
        for momentum_ticks in range(0, MAX_MOMENTUM_TICKS + 1):
            apex_y, _ = get_apex(sprint=sprint, momentum_ticks=momentum_ticks)
            for gap in range(0, 8):
                for delta_y in [0.0, 1.0, -1.0, -2.0]:
                    ok, landing_x, needed_x = can_reach_gap(
                        gap_blocks=gap,
                        dy=delta_y,
                        sprint=sprint,
                        momentum_ticks=momentum_ticks,
                    )
                    subfamily = (
                        "flat"
                        if delta_y == 0.0
                        else "ascend"
                        if delta_y > 0.0
                        else "descend"
                    )
                    cases.append(
                        TheoryCase(
                            case_id=(
                                f"linear-{subfamily}-{movement_mode}-mm{momentum_ticks}"
                                f"-gap{gap}-dy{_float_token(delta_y)}"
                            ),
                            family="linear",
                            subfamily=subfamily,
                            movement_mode=movement_mode,
                            momentum_ticks=momentum_ticks,
                            gap_blocks=gap,
                            delta_y=delta_y,
                            ceiling_height=None,
                            wall_width=None,
                            wall_offset=None,
                            expected_reachable=ok,
                            landing_x=landing_x,
                            apex_y=apex_y,
                            margin=None if landing_x is None else landing_x - needed_x,
                        )
                    )

    for sprint, movement_mode in [
        (False, "walk"),
        (True, "sprint"),
    ]:
        for momentum_ticks in range(0, MAX_MOMENTUM_TICKS + 1):
            apex_y, _ = get_apex(sprint=sprint, momentum_ticks=momentum_ticks)
            landing = get_landing(
                sprint=sprint,
                target_y=0.0,
                landing_x_start=0.0,
                momentum_ticks=momentum_ticks,
            )
            for wall_width in [1, 2, 3, 4]:
                landing_x = None if landing is None else landing[0]
                needed_x = wall_width + PLAYER_WIDTH
                margin = None if landing_x is None else landing_x - needed_x
                cases.append(
                    TheoryCase(
                        case_id=f"neo-neo-{movement_mode}-mm{momentum_ticks}-wall{wall_width}",
                        family="neo",
                        subfamily="neo",
                        movement_mode=movement_mode,
                        momentum_ticks=momentum_ticks,
                        gap_blocks=None,
                        delta_y=0.0,
                        ceiling_height=None,
                        wall_width=wall_width,
                        wall_offset=None,
                        expected_reachable=margin is not None and margin >= 0.0,
                        landing_x=landing_x,
                        apex_y=apex_y,
                        margin=margin,
                    )
                )

    for momentum_ticks in range(0, MAX_MOMENTUM_TICKS + 1):
        for ceiling_height in [4.0, 3.0, 2.5, 2.0, 1.8125]:
            apex_y, _ = get_apex(
                sprint=True,
                momentum_ticks=momentum_ticks,
                ceiling_y=ceiling_height,
            )
            for gap in [1, 2, 3, 4]:
                landing = get_landing(
                    sprint=True,
                    target_y=0.0,
                    landing_x_start=0.5 + gap,
                    momentum_ticks=momentum_ticks,
                    ceiling_y=ceiling_height,
                    landing_width=TARGET_BLOCK_WIDTH,
                )
                landing_x = None if landing is None else landing[0]
                needed_x = 0.5 + gap - (PLAYER_WIDTH / 2.0)
                margin = None if landing_x is None else landing_x - needed_x
                cases.append(
                    TheoryCase(
                        case_id=(
                            f"ceiling-headhitter-sprint-mm{momentum_ticks}-gap{gap}"
                            f"-ceil{str(ceiling_height).replace('.', 'p')}"
                        ),
                        family="ceiling",
                        subfamily="headhitter",
                        movement_mode="sprint",
                        momentum_ticks=momentum_ticks,
                        gap_blocks=gap,
                        delta_y=0.0,
                        ceiling_height=ceiling_height,
                        wall_width=None,
                        wall_offset=None,
                        expected_reachable=margin is not None and margin >= 0.0,
                        landing_x=landing_x,
                        apex_y=apex_y,
                        margin=margin,
                    )
                )

    # --- Side-wall jump family ---
    for sprint, movement_mode in [
        (False, "walk"),
        (True, "sprint"),
    ]:
        for momentum_ticks in range(0, MAX_MOMENTUM_TICKS + 1):
            apex_y, _ = get_apex(sprint=sprint, momentum_ticks=momentum_ticks)
            for wall_offset in [0, 1]:
                for gap in range(0, 8):
                    for delta_y in [0.0, 1.0, -1.0, -2.0]:
                        ok, landing_x, needed_x = can_reach_gap_with_side_wall(
                            gap_blocks=gap,
                            dy=delta_y,
                            wall_offset=wall_offset,
                            sprint=sprint,
                            momentum_ticks=momentum_ticks,
                        )
                        subfamily = (
                            "flat"
                            if delta_y == 0.0
                            else "ascend"
                            if delta_y > 0.0
                            else "descend"
                        )
                        cases.append(
                            TheoryCase(
                                case_id=(
                                    f"sidewall-{subfamily}-{movement_mode}-mm{momentum_ticks}"
                                    f"-gap{gap}-dy{_float_token(delta_y)}-wo{wall_offset}"
                                ),
                                family="sidewall",
                                subfamily=subfamily,
                                movement_mode=movement_mode,
                                momentum_ticks=momentum_ticks,
                                gap_blocks=gap,
                                delta_y=delta_y,
                                ceiling_height=None,
                                wall_width=None,
                                wall_offset=wall_offset,
                                expected_reachable=ok,
                                landing_x=landing_x,
                                apex_y=apex_y,
                                margin=None if landing_x is None else landing_x - needed_x,
                            )
                        )

    return cases
