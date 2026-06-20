#!/usr/bin/env python3
"""
Generate MinimapBlockColors.json from decompiled Minecraft source.

Parses MapColor.java for the 62 base map colors (ID -> RGB), then parses
Blocks.java to extract each block's mapColor assignment, and outputs a
JSON mapping from MCC Material enum names (PascalCase) to RGB triples.

Usage:
    python3 tools/gen_block_color_map.py <decompiled_root>

Example:
    python3 tools/gen_block_color_map.py MinecraftOfficial/26.1-rc-2-decompiled
"""

import json
import re
import sys
from pathlib import Path

OUTPUT_PATH = (Path(__file__).resolve().parent.parent
               / "MinecraftClient" / "Tui" / "MinimapBlockColors.json")
MATERIAL_CS = (Path(__file__).resolve().parent.parent
               / "MinecraftClient" / "Mapping" / "Material.cs")


def mc_name_to_csharp(mc_name: str) -> str:
    name = mc_name.removeprefix("minecraft:")
    return "".join(word.capitalize() for word in name.split("_"))


def parse_map_colors(map_color_java: Path) -> dict[str, tuple[int, int, int]]:
    """Parse MapColor.java: extract name -> (R, G, B) for each constant."""
    text = map_color_java.read_text()
    colors: dict[str, tuple[int, int, int]] = {}

    pattern = re.compile(
        r'public static final MapColor\s+(\w+)\s*=\s*new\s+MapColor\(\s*(\d+)\s*,\s*(\d+)\s*\)')
    for m in pattern.finditer(text):
        name = m.group(1)
        color_int = int(m.group(3))
        r = (color_int >> 16) & 0xFF
        g = (color_int >> 8) & 0xFF
        b = color_int & 0xFF
        colors[name] = (r, g, b)

    return colors


def parse_dye_to_map_color(dye_color_java: Path) -> dict[str, str]:
    """Parse DyeColor.java: extract DyeColor name -> MapColor name."""
    text = dye_color_java.read_text()
    mapping: dict[str, str] = {}

    pattern = re.compile(
        r'(\w+)\(\d+,\s*"[^"]+",\s*\d+,\s*MapColor\.(\w+)')
    for m in pattern.finditer(text):
        mapping[m.group(1)] = m.group(2)

    return mapping


def extract_block_declarations(text: str) -> list[tuple[str, str, str]]:
    """Extract (field_name, block_id, full_register_body) for each block declaration.

    Returns list of (FIELD_NAME, "block_name", "register(...) content").
    """
    results = []

    # Find all "public static final Block FIELD = register(...)" declarations.
    # These span multiple lines and end with ");".
    # Strategy: find start pattern, then track parens to find matching end.
    field_pattern = re.compile(
        r'public\s+static\s+final\s+Block\s+(\w+)\s*=\s*register\s*\(')

    pos = 0
    while pos < len(text):
        m = field_pattern.search(text, pos)
        if not m:
            break

        field_name = m.group(1)
        paren_start = m.end() - 1  # position of opening '('

        # Find matching closing ')' then ';'
        depth = 1
        i = paren_start + 1
        while i < len(text) and depth > 0:
            if text[i] == '(':
                depth += 1
            elif text[i] == ')':
                depth -= 1
            i += 1

        register_body = text[paren_start:i]

        # Extract block name string from register call
        name_match = re.search(r'(?:BlockIds\.(\w+)|"(\w+)")', register_body)
        if name_match:
            raw_id = name_match.group(1) or name_match.group(2)
            block_id = raw_id.lower() if raw_id.isupper() else raw_id
        else:
            block_id = field_name.lower()

        results.append((field_name, block_id, register_body))
        pos = i

    return results


def parse_blocks(blocks_java: Path, map_colors: dict[str, tuple[int, int, int]],
                 dye_to_map: dict[str, str]) -> dict[str, tuple[int, int, int]]:
    """Parse Blocks.java: extract block_name -> (R, G, B)."""
    text = blocks_java.read_text()

    declarations = extract_block_declarations(text)
    print(f"  Found {len(declarations)} block register() declarations")

    # First pass: assign MapColor name to each block
    field_to_block_id: dict[str, str] = {}
    block_color_name: dict[str, str] = {}

    map_color_direct = re.compile(r'\.mapColor\(MapColor\.(\w+)\)')
    map_color_dye = re.compile(r'\.mapColor\(DyeColor\.(\w+)\)')
    map_color_ref = re.compile(r'\.mapColor\((\w+)\.defaultMapColor\(\)')
    map_color_waterlogged = re.compile(r'\.mapColor\(waterloggedMapColor\(MapColor\.(\w+)\)')

    for field_name, block_id, body in declarations:
        field_to_block_id[field_name] = block_id

        mc = map_color_direct.search(body)
        if mc:
            block_color_name[block_id] = mc.group(1)
            continue

        mc = map_color_dye.search(body)
        if mc:
            dye_name = mc.group(1)
            if dye_name in dye_to_map:
                block_color_name[block_id] = dye_to_map[dye_name]
            continue

        mc = map_color_waterlogged.search(body)
        if mc:
            block_color_name[block_id] = mc.group(1)
            continue

        mc = map_color_ref.search(body)
        if mc:
            ref_field = mc.group(1)
            ref_block = field_to_block_id.get(ref_field)
            if ref_block and ref_block in block_color_name:
                block_color_name[block_id] = block_color_name[ref_block]

    # Second pass: resolve remaining BLOCK.defaultMapColor() references
    for field_name, block_id, body in declarations:
        if block_id in block_color_name:
            continue
        mc = map_color_ref.search(body)
        if mc:
            ref_field = mc.group(1)
            ref_block = field_to_block_id.get(ref_field)
            if ref_block and ref_block in block_color_name:
                block_color_name[block_id] = block_color_name[ref_block]

    result: dict[str, tuple[int, int, int]] = {}
    for block_id, color_name in block_color_name.items():
        if color_name in map_colors:
            cs_name = mc_name_to_csharp(block_id)
            result[cs_name] = map_colors[color_name]

    return result


def load_known_materials() -> set[str]:
    known = set()
    if MATERIAL_CS.exists():
        with open(MATERIAL_CS) as f:
            for line in f:
                m = re.match(r'\s+(\w+),?\s*$', line)
                if m:
                    known.add(m.group(1))
    return known


TRANSPARENT_BLOCKS = [
    "Air", "CaveAir", "VoidAir",
    "Glass", "GlassPane",
    "WhiteStainedGlass", "OrangeStainedGlass", "MagentaStainedGlass",
    "LightBlueStainedGlass", "YellowStainedGlass", "LimeStainedGlass",
    "PinkStainedGlass", "GrayStainedGlass", "LightGrayStainedGlass",
    "CyanStainedGlass", "PurpleStainedGlass", "BlueStainedGlass",
    "BrownStainedGlass", "GreenStainedGlass", "RedStainedGlass",
    "BlackStainedGlass",
    "WhiteStainedGlassPane", "OrangeStainedGlassPane", "MagentaStainedGlassPane",
    "LightBlueStainedGlassPane", "YellowStainedGlassPane", "LimeStainedGlassPane",
    "PinkStainedGlassPane", "GrayStainedGlassPane", "LightGrayStainedGlassPane",
    "CyanStainedGlassPane", "PurpleStainedGlassPane", "BlueStainedGlassPane",
    "BrownStainedGlassPane", "GreenStainedGlassPane", "RedStainedGlassPane",
    "BlackStainedGlassPane",
    "TintedGlass", "Barrier", "Light", "StructureVoid",
]

WATER_BLOCKS = ["Water"]
ICE_BLOCKS = ["Ice", "PackedIce", "BlueIce", "FrostedIce"]


def build_map_palette(map_color_java: Path) -> dict[str, list[int]]:
    """Build MapColor ID -> [R, G, B] palette for the Map bot (map packet rendering).

    Returns a dict keyed by string IDs ("0", "1", ...) to keep JSON simple.
    """
    text = map_color_java.read_text()
    palette: dict[str, list[int]] = {}

    pattern = re.compile(
        r'new\s+MapColor\(\s*(\d+)\s*,\s*(\d+)\s*\)')
    for m in pattern.finditer(text):
        cid = int(m.group(1))
        raw = int(m.group(2))
        r = (raw >> 16) & 0xFF
        g = (raw >> 8) & 0xFF
        b = raw & 0xFF
        palette[str(cid)] = [r, g, b]

    print(f"  Built map_palette with {len(palette)} base color entries")
    return dict(sorted(palette.items(), key=lambda x: int(x[0])))


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(1)

    root = Path(sys.argv[1])
    if not root.is_dir():
        print(f"Error: {root} is not a directory")
        sys.exit(1)

    map_color_java = root / "net/minecraft/world/level/material/MapColor.java"
    dye_color_java = root / "net/minecraft/world/item/DyeColor.java"
    blocks_java = root / "net/minecraft/world/level/block/Blocks.java"

    for f in [map_color_java, dye_color_java, blocks_java]:
        if not f.exists():
            print(f"Error: {f} not found")
            sys.exit(1)

    print("Parsing MapColor.java...")
    map_colors = parse_map_colors(map_color_java)
    print(f"  Found {len(map_colors)} map colors")

    print("Parsing DyeColor.java...")
    dye_to_map = parse_dye_to_map_color(dye_color_java)
    print(f"  Found {len(dye_to_map)} dye->map color mappings")

    print("Parsing Blocks.java...")
    block_colors = parse_blocks(blocks_java, map_colors, dye_to_map)
    print(f"  Extracted colors for {len(block_colors)} blocks")

    known_materials = load_known_materials()
    if known_materials:
        matched = {k: v for k, v in block_colors.items() if k in known_materials}
        unmatched = [k for k in block_colors if k not in known_materials]
        if unmatched:
            print(f"\n  {len(unmatched)} blocks not in Material.cs (will be skipped):")
            for name in sorted(unmatched)[:20]:
                print(f"    {name}")
            if len(unmatched) > 20:
                print(f"    ... and {len(unmatched) - 20} more")
        block_colors = matched
        print(f"  {len(block_colors)} blocks matched to Material.cs entries")

    map_palette = build_map_palette(map_color_java)

    output = {
        "version": root.name.replace("-decompiled", "").replace("-client", ""),
        "colors": {k: list(v) for k, v in sorted(block_colors.items())},
        "transparent": sorted(TRANSPARENT_BLOCKS),
        "water": WATER_BLOCKS,
        "ice": ICE_BLOCKS,
        "map_palette": map_palette,
    }

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with open(OUTPUT_PATH, 'w') as f:
        json.dump(output, f, indent=2)
    print(f"\nGenerated {OUTPUT_PATH}")
    print(f"  {len(block_colors)} color entries")


if __name__ == "__main__":
    main()
