from collections import defaultdict

from tools.pathing_theory.models import MomentumCapabilityBand, TheoryCase


def _linear_group_key(case: TheoryCase) -> tuple[object, ...]:
    return (
        case.family,
        case.subfamily,
        case.movement_mode,
        "gap_blocks",
        case.delta_y,
        None,
        None,
    )


def _neo_group_key(case: TheoryCase) -> tuple[object, ...]:
    return (
        case.family,
        case.subfamily,
        case.movement_mode,
        "wall_width",
        None,
        None,
        None,
    )


def _ceiling_group_key(case: TheoryCase) -> tuple[object, ...]:
    return (
        case.family,
        case.subfamily,
        case.movement_mode,
        "gap_blocks",
        None,
        case.ceiling_height,
        None,
    )


def _sidewall_group_key(case: TheoryCase) -> tuple[object, ...]:
    return (
        case.family,
        case.subfamily,
        case.movement_mode,
        "gap_blocks",
        case.delta_y,
        None,
        case.wall_offset,
    )


def _case_group_key(case: TheoryCase) -> tuple[object, ...]:
    if case.family == "linear":
        return _linear_group_key(case)
    if case.family == "neo":
        return _neo_group_key(case)
    if case.family == "ceiling":
        return _ceiling_group_key(case)
    if case.family == "sidewall":
        return _sidewall_group_key(case)
    raise ValueError(f"Unsupported theory family for capability bands: {case.family}")


def _case_reach_value(case: TheoryCase) -> int | None:
    if not case.expected_reachable:
        return None
    if case.family in {"linear", "ceiling", "sidewall"}:
        return case.gap_blocks
    if case.family == "neo":
        return case.wall_width
    raise ValueError(f"Unsupported theory family for capability bands: {case.family}")


def _compress_mm_ranges(
    mm_to_reach: list[tuple[int, int | None]],
    family: str,
    subfamily: str,
    movement_mode: str,
    capability_metric: str,
    delta_y: float | None,
    ceiling_height: float | None,
    wall_offset: int | None = None,
) -> list[MomentumCapabilityBand]:
    bands: list[MomentumCapabilityBand] = []
    current_start = mm_to_reach[0][0]
    current_end = current_start
    current_reach = mm_to_reach[0][1]

    for mm, max_reach in mm_to_reach[1:]:
        if max_reach == current_reach and mm == current_end + 1:
            current_end = mm
            continue

        bands.append(
            MomentumCapabilityBand(
                family=family,
                subfamily=subfamily,
                movement_mode=movement_mode,
                capability_metric=capability_metric,
                min_mm=current_start,
                max_mm=current_end,
                max_reach=current_reach,
                delta_y=delta_y,
                ceiling_height=ceiling_height,
                wall_offset=wall_offset,
            )
        )
        current_start = mm
        current_end = mm
        current_reach = max_reach

    bands.append(
        MomentumCapabilityBand(
            family=family,
            subfamily=subfamily,
            movement_mode=movement_mode,
            capability_metric=capability_metric,
            min_mm=current_start,
            max_mm=current_end,
            max_reach=current_reach,
            delta_y=delta_y,
            ceiling_height=ceiling_height,
            wall_offset=wall_offset,
        )
    )
    return bands


def build_momentum_capability_bands(
    cases: list[TheoryCase],
) -> list[MomentumCapabilityBand]:
    grouped_cases: dict[tuple[object, ...], list[TheoryCase]] = defaultdict(list)
    for case in cases:
        grouped_cases[_case_group_key(case)].append(case)

    bands: list[MomentumCapabilityBand] = []
    for key in sorted(grouped_cases):
        family, subfamily, movement_mode, capability_metric, delta_y, ceiling_height, wall_offset = key
        mm_groups: dict[int, list[TheoryCase]] = defaultdict(list)
        for case in grouped_cases[key]:
            mm_groups[case.momentum_ticks].append(case)

        mm_to_reach = [
            (
                mm,
                max(
                    (
                        _case_reach_value(case)
                        for case in mm_groups[mm]
                        if _case_reach_value(case) is not None
                    ),
                    default=None,
                ),
            )
            for mm in sorted(mm_groups)
        ]
        bands.extend(
            _compress_mm_ranges(
                mm_to_reach=mm_to_reach,
                family=family,
                subfamily=subfamily,
                movement_mode=movement_mode,
                capability_metric=capability_metric,
                delta_y=delta_y,
                ceiling_height=ceiling_height,
                wall_offset=wall_offset,
            )
        )

    return bands


def _format_range(band: MomentumCapabilityBand) -> str:
    return f"{band.min_mm}..{band.max_mm}"


def _format_reach(band: MomentumCapabilityBand) -> str:
    label = "max_gap" if band.capability_metric == "gap_blocks" else "max_wall_width"
    value = "none" if band.max_reach is None else str(band.max_reach)
    return f"{label}={value}"


def format_momentum_capability_lines(
    bands: list[MomentumCapabilityBand],
) -> list[str]:
    lines: list[str] = []
    for band in bands:
        parts = [band.family, band.subfamily, band.movement_mode]
        if band.delta_y is not None:
            parts.append(f"dy={band.delta_y}")
        if band.ceiling_height is not None:
            parts.append(f"ceil={band.ceiling_height}")
        if band.wall_offset is not None:
            parts.append(f"wo={band.wall_offset}")
        parts.append(f"mm={_format_range(band)}")
        parts.append(_format_reach(band))
        lines.append(" | ".join(parts))
    return lines
