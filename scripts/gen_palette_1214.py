#!/usr/bin/env python3
"""
Generate Palette1214.cs from vanilla reports/blocks.json (1.21.4).

Obtain blocks.json:
  java -DbundlerMainClass=net.minecraft.data.Main -jar server.jar --reports --output <dir>
  -> <dir>/reports/blocks.json

Default input: /tmp/mc1214_reports/reports/blocks.json (run this script after generating reports).
"""
from __future__ import annotations

import json
import re
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path("/home/ryan/Minecraft/Minecraft-Console-Client-milutinke")
DEFAULT_JSON = Path("/tmp/mc1214_reports/reports/blocks.json")
OUT = ROOT / "MinecraftClient/Mapping/BlockPalettes/Palette1214.cs"


def snake_to_material_pascal(snake: str) -> str:
    return "".join(part.capitalize() for part in snake.split("_"))


def merge_ranges(sorted_ids: list[int]) -> list[tuple[int, int]]:
    if not sorted_ids:
        return []
    ids = sorted(sorted_ids)
    out: list[tuple[int, int]] = []
    s = e = ids[0]
    for x in ids[1:]:
        if x == e + 1:
            e = x
        else:
            out.append((s, e))
            s = e = x
    out.append((s, e))
    return out


def emit_palette(assignments: list[tuple[str, list[tuple[int, int]]]]) -> str:
    lines = [
        "using System.Collections.Generic;",
        "",
        "namespace MinecraftClient.Mapping.BlockPalettes",
        "{",
        "    public class Palette1214 : BlockPalette",
        "    {",
        "        private static readonly Dictionary<int, Material> materials = new();",
        "",
        "        static Palette1214()",
        "        {",
    ]
    for mat, ranges in sorted(assignments, key=lambda x: x[0]):
        for start, end in ranges:
            if start == end:
                lines.append(f"            materials[{start}] = Material.{mat};")
            else:
                lines.append(f"            for (int i = {start}; i <= {end}; i++)")
                lines.append(f"                materials[i] = Material.{mat};")
    lines.extend(
        [
            "        }",
            "",
            "        protected override Dictionary<int, Material> GetDict()",
            "        {",
            "            return materials;",
            "        }",
            "    }",
            "}",
        ]
    )
    return "\n".join(lines) + "\n"


def main() -> None:
    json_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_JSON
    if not json_path.is_file():
        raise SystemExit(f"Missing {json_path} — generate with server.jar --reports")

    data = json.loads(json_path.read_text(encoding="utf-8"))
    ids_by_material: dict[str, list[int]] = defaultdict(list)
    known: set[int] = set()
    max_id = -1

    for key, val in data.items():
        if not key.startswith("minecraft:"):
            raise SystemExit(f"Unexpected block key: {key}")
        name = key.split(":", 1)[1]
        mat = snake_to_material_pascal(name)
        for st in val["states"]:
            sid = int(st["id"])
            if sid in known:
                raise SystemExit(f"Duplicate state id {sid}")
            known.add(sid)
            max_id = max(max_id, sid)
            ids_by_material[mat].append(sid)

    expected = max_id + 1
    if len(known) != expected:
        raise SystemExit(f"Non-contiguous state IDs: have {len(known)}, expected 0..{max_id}")

    assignments = [(m, merge_ranges(ids)) for m, ids in ids_by_material.items()]
    OUT.write_text(emit_palette(assignments), encoding="utf-8")
    print(f"Wrote {OUT} ({expected} states, {len(data)} block types)")


if __name__ == "__main__":
    main()
