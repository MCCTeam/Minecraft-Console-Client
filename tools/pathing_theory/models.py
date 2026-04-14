from dataclasses import dataclass


@dataclass(frozen=True)
class TheoryCase:
    case_id: str
    family: str
    subfamily: str
    movement_mode: str
    momentum_ticks: int
    gap_blocks: int | None
    delta_y: float | None
    ceiling_height: float | None
    wall_width: int | None
    expected_reachable: bool
    landing_x: float | None
    apex_y: float | None
    margin: float | None
    notes: str = ""


@dataclass(frozen=True)
class CanonicalLiveCase:
    case_id: str
    bucket_id: str
    family: str
    subfamily: str
    movement_mode: str
    momentum_ticks: int
    difficulty_band: str
    expected_result: str
    world_recipe_id: str
    gap_blocks: int | None
    delta_y: float | None
    ceiling_height: float | None
    wall_width: int | None
    start: dict[str, float]
    goal: dict[str, float]
