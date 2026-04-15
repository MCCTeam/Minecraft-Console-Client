import math
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
# Reliable late-jump timing is slightly before the full 0.8 block walk-off limit.
# Baritone uses a 0.7 threshold for the analogous 2-gap flat parkour execution.
EDGE_TAKEOFF_X = 0.7
TARGET_BLOCK_WIDTH = 1.0


@dataclass
class TickState:
    tick: int = 0
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0
    vx: float = 0.0
    vy: float = 0.0
    vz: float = 0.0
    on_ground: bool = True


def get_ground_speed(block_friction: float = DEFAULT_BLOCK_FRICTION) -> float:
    friction = block_friction * FRICTION_MULTIPLIER
    return MOVEMENT_SPEED * (GROUND_ACCEL_FACTOR / (friction * friction * friction))


def build_momentum_velocity(
    momentum_ticks: int,
    block_friction: float = DEFAULT_BLOCK_FRICTION,
) -> float:
    vx = 0.0
    ground_friction = block_friction * FRICTION_MULTIPLIER
    for _ in range(momentum_ticks):
        vx += INPUT_FRICTION * get_ground_speed(block_friction)
        vx *= ground_friction
    return vx


def build_momentum_velocity_2d(
    momentum_ticks: int,
    yaw_rad: float,
    strafe_input: float = 0.0,
    block_friction: float = DEFAULT_BLOCK_FRICTION,
    wall_z: Optional[float] = None,
    start_z: float = 0.0,
) -> tuple[float, float, float]:
    """Build pre-jump velocity with yaw and optional strafe, returning (vx, vz, z).

    Simulates ground ticks before the jump edge, accounting for yaw-split
    acceleration, optional strafe, and wall collision on the z axis.
    """
    cos_yaw = math.cos(yaw_rad)
    sin_yaw = math.sin(yaw_rad)
    ground_friction = block_friction * FRICTION_MULTIPLIER
    ground_speed = get_ground_speed(block_friction)
    vx, vz, z = 0.0, 0.0, start_z

    for _ in range(momentum_ticks):
        input_x = (cos_yaw + strafe_input * (-sin_yaw)) * INPUT_FRICTION
        input_z = (sin_yaw + strafe_input * cos_yaw) * INPUT_FRICTION
        vx += input_x * ground_speed
        vz += input_z * ground_speed

        z += vz
        if wall_z is not None and z + HALF_WIDTH > wall_z:
            z = wall_z - HALF_WIDTH
            if vz > 0:
                vz = 0.0

        vx *= ground_friction
        vz *= ground_friction

    return vx, vz, z


def _get_overlap_window(
    start_x: float,
    end_x: float,
    landing_x_start: float,
    landing_width: Optional[float],
) -> Optional[tuple[float, float]]:
    min_center_x = landing_x_start - HALF_WIDTH
    max_center_x = (
        None if landing_width is None else landing_x_start + landing_width + HALF_WIDTH
    )

    if start_x > end_x:
        start_x, end_x = end_x, start_x

    delta_x = end_x - start_x
    if delta_x == 0.0:
        if start_x < min_center_x:
            return None
        if max_center_x is not None and start_x > max_center_x:
            return None
        return 0.0, 1.0

    if end_x < min_center_x:
        return None

    enter_t = 0.0 if start_x >= min_center_x else (min_center_x - start_x) / delta_x

    if max_center_x is None:
        exit_t = 1.0
    else:
        if start_x > max_center_x:
            return None
        exit_t = 1.0 if end_x <= max_center_x else (max_center_x - start_x) / delta_x

    if exit_t < 0.0 or enter_t > 1.0 or enter_t > exit_t:
        return None

    return max(0.0, enter_t), min(1.0, exit_t)


def _find_landing_contact(
    start_x: float,
    start_y: float,
    end_x: float,
    end_y: float,
    landing_y: float,
    landing_x_start: float,
    landing_width: Optional[float],
) -> Optional[tuple[float, float]]:
    if start_y < landing_y or end_y > landing_y or start_y == end_y:
        return None

    overlap_window = _get_overlap_window(
        start_x=start_x,
        end_x=end_x,
        landing_x_start=landing_x_start,
        landing_width=landing_width,
    )
    if overlap_window is None:
        return None

    landing_t = (start_y - landing_y) / (start_y - end_y)
    enter_t, exit_t = overlap_window
    if landing_t < enter_t or landing_t > exit_t:
        return None

    landing_x = start_x + (end_x - start_x) * landing_t
    return landing_x, landing_y


def simulate_jump(
    sprint: bool = True,
    momentum_ticks: int = 12,
    ceiling_y: Optional[float] = None,
    landing_y: float = 0.0,
    landing_x_start: float = 0.0,
    landing_width: Optional[float] = None,
    max_ticks: int = 200,
    yaw_degrees: float = 0.0,
    strafe_input: float = 0.0,
    wall_z: Optional[float] = None,
    start_z: float = 0.0,
) -> list[TickState]:
    yaw_rad = math.radians(yaw_degrees)
    cos_yaw = math.cos(yaw_rad)
    sin_yaw = math.sin(yaw_rad)
    has_lateral = yaw_degrees != 0.0 or strafe_input != 0.0 or wall_z is not None

    if has_lateral:
        vx, vz, z = build_momentum_velocity_2d(
            momentum_ticks, yaw_rad, strafe_input,
            wall_z=wall_z, start_z=start_z,
        )
    else:
        vx = build_momentum_velocity(momentum_ticks)
        vz = 0.0
        z = start_z

    x, y, vy = EDGE_TAKEOFF_X, 0.0, 0.0
    on_ground = True
    trajectory: list[TickState] = []
    jumped = False
    ground_friction = DEFAULT_BLOCK_FRICTION * FRICTION_MULTIPLIER

    trajectory.append(TickState(0, x, y, z, vx, vy, vz, on_ground))

    for tick in range(1, max_ticks + 1):
        if vx * vx + vz * vz < HORIZONTAL_VELOCITY_THRESHOLD_SQR:
            vx = 0.0
            vz = 0.0
        if abs(vy) < VERTICAL_VELOCITY_THRESHOLD:
            vy = 0.0

        do_jump = False
        if not jumped and on_ground:
            do_jump = True
            jumped = True

        if do_jump:
            vy = max(BASE_JUMP_POWER, vy)
            if sprint:
                vx += SPRINT_JUMP_HORIZONTAL_BOOST * cos_yaw
                vz += SPRINT_JUMP_HORIZONTAL_BOOST * sin_yaw

        speed = get_ground_speed() if on_ground else AIR_ACCEL
        if has_lateral:
            input_x = (cos_yaw + strafe_input * (-sin_yaw)) * INPUT_FRICTION
            input_z = (sin_yaw + strafe_input * cos_yaw) * INPUT_FRICTION
            vx += input_x * speed
            vz += input_z * speed
        else:
            vx += INPUT_FRICTION * speed

        new_x = x + vx
        new_y = y + vy
        new_z = z + vz
        new_on_ground = False

        if wall_z is not None and new_z + HALF_WIDTH > wall_z:
            new_z = wall_z - HALF_WIDTH
            if vz > 0:
                vz = 0.0

        if ceiling_y is not None:
            head_y = new_y + PLAYER_HEIGHT
            if head_y > ceiling_y:
                new_y = ceiling_y - PLAYER_HEIGHT
                if vy > 0:
                    vy = 0.0

        if jumped:
            contact = _find_landing_contact(
                start_x=x,
                start_y=y,
                end_x=new_x,
                end_y=new_y,
                landing_y=landing_y,
                landing_x_start=landing_x_start,
                landing_width=landing_width,
            )
            if contact is not None:
                new_x, new_y = contact
                vy = 0.0
                new_on_ground = True

        x = new_x
        y = new_y
        z = new_z
        on_ground = new_on_ground

        vy -= GRAVITY
        vy *= DRAG_Y

        if on_ground:
            vx *= ground_friction
            vz *= ground_friction
        else:
            vx *= FRICTION_MULTIPLIER
            vz *= FRICTION_MULTIPLIER

        trajectory.append(TickState(tick, x, y, z, vx, vy, vz, on_ground))

        if jumped and on_ground:
            break

    return trajectory


def get_landing(
    sprint: bool,
    target_y: float,
    landing_x_start: float = 0.0,
    momentum_ticks: int = 12,
    ceiling_y: Optional[float] = None,
    landing_width: Optional[float] = None,
    yaw_degrees: float = 0.0,
    strafe_input: float = 0.0,
    wall_z: Optional[float] = None,
    start_z: float = 0.0,
) -> Optional[tuple[float, float]]:
    trajectory = simulate_jump(
        sprint=sprint,
        momentum_ticks=momentum_ticks,
        ceiling_y=ceiling_y,
        landing_y=target_y,
        landing_x_start=landing_x_start,
        landing_width=landing_width,
        yaw_degrees=yaw_degrees,
        strafe_input=strafe_input,
        wall_z=wall_z,
        start_z=start_z,
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

    needed_x = 0.5 + gap_blocks - HALF_WIDTH
    landing_platform_start = 0.5 + gap_blocks

    if gap_blocks == 0 and dy > 0:
        landing_platform_start = 0.5

    result = get_landing(
        sprint=sprint,
        target_y=dy,
        landing_x_start=landing_platform_start,
        momentum_ticks=momentum_ticks,
        landing_width=TARGET_BLOCK_WIDTH,
    )
    if result is None:
        return False, None, needed_x

    landing_x, landing_y = result
    if abs(landing_y - dy) > 0.01:
        return False, landing_x, needed_x

    if gap_blocks > 0 and landing_x < needed_x:
        return False, landing_x, needed_x

    return True, landing_x, needed_x


SIDE_WALL_YAW_SWEEP = [0.0, 3.0, 5.0, 8.0, 10.0]


def can_reach_gap_with_side_wall(
    gap_blocks: int,
    dy: float,
    wall_offset: int,
    sprint: bool = True,
    momentum_ticks: int = 12,
) -> tuple[bool, Optional[float], float]:
    """Check gap reachability with a side wall parallel to the jump direction.

    wall_offset=0 means the wall is flush with the platform edge (wall at z=1.0
    for a 1-wide platform centered at z=0.5). wall_offset=1 means one air block
    between the platform edge and the wall face.

    Sweeps yaw angles from 0 to 10 degrees toward the wall to find the
    worst-case trajectory. Uses the most pessimistic result: if any realistic
    yaw angle causes a failure, the case is marked unreachable or gets a
    reduced margin. This models the real-world constraint where MCC's
    pathfinder can't guarantee perfect yaw alignment.
    """
    if dy > 1.252:
        return False, None, 0.0

    wall_z = 1.0 + wall_offset
    start_z = 0.5

    clearance = wall_z - (start_z + HALF_WIDTH)
    if clearance < 0:
        return False, None, 0.0

    needed_x = 0.5 + gap_blocks - HALF_WIDTH
    landing_platform_start = 0.5 + gap_blocks
    if gap_blocks == 0 and dy > 0:
        landing_platform_start = 0.5

    worst_ok = True
    worst_landing_x: Optional[float] = None
    worst_margin: Optional[float] = None

    for yaw in SIDE_WALL_YAW_SWEEP:
        result = get_landing(
            sprint=sprint,
            target_y=dy,
            landing_x_start=landing_platform_start,
            momentum_ticks=momentum_ticks,
            landing_width=TARGET_BLOCK_WIDTH,
            yaw_degrees=yaw,
            wall_z=wall_z,
            start_z=start_z,
        )

        if result is None:
            return False, worst_landing_x, needed_x

        landing_x, landing_y = result
        if abs(landing_y - dy) > 0.01:
            return False, landing_x, needed_x

        if gap_blocks > 0 and landing_x < needed_x:
            return False, landing_x, needed_x

        margin = landing_x - needed_x
        if worst_margin is None or margin < worst_margin:
            worst_margin = margin
            worst_landing_x = landing_x

    return True, worst_landing_x, needed_x
