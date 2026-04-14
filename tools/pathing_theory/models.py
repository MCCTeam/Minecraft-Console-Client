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
