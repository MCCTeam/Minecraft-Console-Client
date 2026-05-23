#!/usr/bin/env python3
"""Verify representative mining hardness and tool-requirement data."""

from __future__ import annotations

import re
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
BLOCK_HARDNESS = REPO_ROOT / "MinecraftClient" / "Mapping" / "BlockHardness.cs"

EXPECTED_HARDNESS = {
    "AcaciaLog": 2.0,
    "Bedrock": -1.0,
    "Cobblestone": 2.0,
    "CopperOre": 3.0,
    "CutCopper": 3.0,
    "DeepslateBricks": 3.5,
    "Stone": 1.5,
    "StoneButton": 0.5,
    "Torch": 0.0,
    "WaxedCutCopperStairs": 3.0,
}

EXPECTED_REQUIRES_TOOL = {
    "AcaciaLog": False,
    "CopperOre": True,
    "CutCopper": True,
    "DeepslateBricks": True,
    "Stone": True,
    "StoneButton": False,
    "WaxedCutCopperStairs": True,
}


def parse_hardness(source: str) -> dict[str, float]:
    return {
        material: float(value)
        for material, value in re.findall(
            r"\{\s*Material\.([A-Za-z0-9_]+),\s*(-?[0-9]+(?:\.[0-9]+)?)f\s*\}",
            source,
        )
    }


def parse_requires_tool(source: str) -> set[str]:
    set_match = re.search(
        r"RequiresCorrectToolSet\s*=\s*new HashSet<Material>\s*\{(?P<body>.*?)\}\.ToFrozenSet\(\)",
        source,
        re.S,
    )
    if set_match is None:
        raise AssertionError("Could not find RequiresCorrectToolSet")

    return set(re.findall(r"Material\.([A-Za-z0-9_]+)", set_match.group("body")))


def main() -> int:
    source = BLOCK_HARDNESS.read_text()
    hardness = parse_hardness(source)
    requires_tool = parse_requires_tool(source)

    failures: list[str] = []

    for material, expected in EXPECTED_HARDNESS.items():
        actual = hardness.get(material)
        if actual != expected:
            failures.append(f"{material} hardness: expected {expected}, got {actual}")

    for material, expected in EXPECTED_REQUIRES_TOOL.items():
        actual = material in requires_tool
        if actual != expected:
            failures.append(f"{material} requires tool: expected {expected}, got {actual}")

    if failures:
        print("Mining data verification failed:")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    print("Mining data verification passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
