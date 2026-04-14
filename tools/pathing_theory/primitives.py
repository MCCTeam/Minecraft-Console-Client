from dataclasses import dataclass
from typing import Optional

PLAYER_WIDTH = 0.6
PLAYER_HEIGHT = 1.8
STEP_HEIGHT = 0.6

GRAVITY = 0.08
DRAG_Y = 0.98
FRICTION_MULTIPLIER = 0.91
DEFAULT_BLOCK_FRICTION = 0.6
INPUT_FRICTION = 0.98
GROUND_ACCEL_FACTOR = 0.21600002
AIR_ACCEL = 0.02
MOVEMENT_SPEED = 0.1

BASE_JUMP_POWER = 0.42
SPRINT_JUMP_HORIZONTAL_BOOST = 0.2

HORIZONTAL_VELOCITY_THRESHOLD_SQR = 9.0e-6
VERTICAL_VELOCITY_THRESHOLD = 0.003

HALF_WIDTH = PLAYER_WIDTH / 2.0


@dataclass
class TickState:
    tick: int = 0
    x: float = 0.0
    y: float = 0.0
    vx: float = 0.0
    vy: float = 0.0
    on_ground: bool = True


def get_ground_speed(block_friction: float = DEFAULT_BLOCK_FRICTION) -> float:
    friction = block_friction * FRICTION_MULTIPLIER
    return MOVEMENT_SPEED * (GROUND_ACCEL_FACTOR / (friction * friction * friction))


def simulate_jump(
    sprint: bool = True,
    momentum_ticks: int = 12,
    ceiling_y: Optional[float] = None,
    landing_y: float = 0.0,
    landing_x_start: float = 0.0,
    max_ticks: int = 200,
) -> list[TickState]:
    x, y, vx, vy = 0.0, 0.0, 0.0, 0.0
    on_ground = True
    trajectory: list[TickState] = []
    jumped = False
    ground_friction = DEFAULT_BLOCK_FRICTION * FRICTION_MULTIPLIER

    trajectory.append(TickState(0, x, y, vx, vy, on_ground))

    for tick in range(1, max_ticks + 1):
        if vx * vx < HORIZONTAL_VELOCITY_THRESHOLD_SQR:
            vx = 0.0
        if abs(vy) < VERTICAL_VELOCITY_THRESHOLD:
            vy = 0.0

        do_jump = False
        if not jumped and tick > momentum_ticks and on_ground:
            do_jump = True
            jumped = True

        if do_jump:
            vy = max(BASE_JUMP_POWER, vy)
            if sprint:
                vx += SPRINT_JUMP_HORIZONTAL_BOOST

        forward_input = 1.0 * INPUT_FRICTION
        speed = get_ground_speed() if on_ground else AIR_ACCEL
        vx += forward_input * speed

        new_x = x + vx
        new_y = y + vy
        new_on_ground = False

        if ceiling_y is not None:
            head_y = new_y + PLAYER_HEIGHT
            if head_y > ceiling_y:
                new_y = ceiling_y - PLAYER_HEIGHT
                if vy > 0:
                    vy = 0.0

        floor_y = 0.0 if new_x < landing_x_start else landing_y

        if jumped:
            if new_x >= landing_x_start:
                if landing_y >= 0:
                    if vy <= 0 and y >= landing_y and new_y <= landing_y:
                        new_y = landing_y
                        vy = 0.0
                        new_on_ground = True
                    elif vy <= 0 and new_y <= landing_y:
                        new_y = landing_y
                        vy = 0.0
                        new_on_ground = True
                else:
                    if new_y <= landing_y:
                        new_y = landing_y
                        if vy < 0:
                            vy = 0.0
                        new_on_ground = True

            if not new_on_ground and new_x < landing_x_start and new_y <= floor_y:
                new_y = floor_y
                if vy < 0:
                    vy = 0.0
                new_on_ground = True
        elif new_y <= 0.0:
            new_y = 0.0
            if vy < 0:
                vy = 0.0
            new_on_ground = True

        x = new_x
        y = new_y
        on_ground = new_on_ground

        vy -= GRAVITY
        vy *= DRAG_Y

        if on_ground:
            vx *= ground_friction
        else:
            vx *= FRICTION_MULTIPLIER

        trajectory.append(TickState(tick, x, y, vx, vy, on_ground))

        if jumped and on_ground:
            break

    return trajectory


def get_landing(
    sprint: bool,
    target_y: float,
    landing_x_start: float = 0.0,
    momentum_ticks: int = 12,
    ceiling_y: Optional[float] = None,
) -> Optional[tuple[float, float]]:
    trajectory = simulate_jump(
        sprint=sprint,
        momentum_ticks=momentum_ticks,
        ceiling_y=ceiling_y,
        landing_y=target_y,
        landing_x_start=landing_x_start,
    )
    was_air = False
    for state in trajectory:
        if not state.on_ground:
            was_air = True
        if was_air and state.on_ground:
            return state.x, state.y
    return None


def get_apex(
    sprint: bool,
    momentum_ticks: int = 12,
    ceiling_y: Optional[float] = None,
) -> tuple[float, float]:
    trajectory = simulate_jump(
        sprint=sprint,
        momentum_ticks=momentum_ticks,
        ceiling_y=ceiling_y,
        landing_y=-1000.0,
        landing_x_start=0.0,
        max_ticks=300,
    )
    best_y, best_x = 0.0, 0.0
    for state in trajectory:
        if state.y > best_y:
            best_y = state.y
            best_x = state.x
    return best_y, best_x


def can_reach_gap(
    gap_blocks: int,
    dy: float,
    sprint: bool = True,
    momentum_ticks: int = 12,
) -> tuple[bool, Optional[float], float]:
    if dy > 1.252:
        return False, None, 0.0

    needed_x = 0.5 + gap_blocks + HALF_WIDTH
    landing_platform_start = 0.5 + gap_blocks

    if gap_blocks == 0 and dy > 0:
        landing_platform_start = 0.5

    result = get_landing(
        sprint=sprint,
        target_y=dy,
        landing_x_start=landing_platform_start,
        momentum_ticks=momentum_ticks,
    )
    if result is None:
        return False, None, needed_x

    landing_x, landing_y = result
    if abs(landing_y - dy) > 0.01:
        return False, landing_x, needed_x

    if gap_blocks > 0 and landing_x < needed_x:
        return False, landing_x, needed_x

    return True, landing_x, needed_x
