#!/usr/bin/env python3
"""
Generate an MCC ItemPalette C# file from decompiled Items.java.

Reads public static final Item field declarations (which define item IDs
by declaration order) and generates a complete C# palette class.

Usage:
    python3 tools/gen_item_palette.py <mc_version> <class_suffix>

Example:
    python3 tools/gen_item_palette.py 1.21.1 121
    # Generates ItemPalette121.cs

The <class_suffix> determines the class name (ItemPalette<suffix>) and
should match MCC's naming convention (e.g., 121 for 1.21, 1206 for 1.20.6).
"""

import re
import sys
from pathlib import Path

DECOMPILED_ROOT = Path(__file__).resolve().parent.parent / "MinecraftOfficial"
OUTPUT_DIR = Path(__file__).resolve().parent.parent / "MinecraftClient" / "Inventory" / "ItemPalettes"

# Java field name → C# ItemType enum name
# Most conversions are automatic (SCREAMING_SNAKE → PascalCase).
# Add manual overrides here for irregular names.
OVERRIDES = {
    "CUT_STANDSTONE_SLAB": "CutSandstoneSlab",  # Mojang typo in source
}


def java_to_csharp_name(java_name: str) -> str:
    """Convert SCREAMING_SNAKE_CASE Java field name to PascalCase C# enum name."""
    if java_name in OVERRIDES:
        return OVERRIDES[java_name]
    return "".join(word.capitalize() for word in java_name.lower().split("_"))


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)

    mc_version = sys.argv[1]
    class_suffix = sys.argv[2]
    version_dir = DECOMPILED_ROOT / f"{mc_version}-decompiled"
    items_java = version_dir / "net" / "minecraft" / "world" / "item" / "Items.java"

    if not items_java.exists():
        print(f"Error: {items_java} not found")
        sys.exit(1)

    pattern = re.compile(r'\s+public static final Item (\w+)\s*=')
    field_names = []
    with open(items_java) as f:
        for line in f:
            m = pattern.match(line)
            if m:
                field_names.append(m.group(1))

    print(f"Found {len(field_names)} items in MC {mc_version}")

    # Verify enum name conversion against existing ItemType.cs
    item_type_cs = OUTPUT_DIR.parent / "ItemType.cs"
    known_enums = set()
    if item_type_cs.exists():
        with open(item_type_cs) as f:
            for line in f:
                m = re.match(r'\s+(\w+),?\s*$', line)
                if m and m.group(1) not in ("Null", "Unknown"):
                    known_enums.add(m.group(1))

    missing = []
    mappings = []
    for i, name in enumerate(field_names):
        cs_name = java_to_csharp_name(name)
        mappings.append((i, cs_name))
        if known_enums and cs_name not in known_enums:
            missing.append((i, name, cs_name))

    if missing:
        print(f"\nWARNING: {len(missing)} items not found in ItemType.cs enum:")
        for idx, java_name, cs_name in missing:
            print(f"  [{idx}] {java_name} -> {cs_name}")
        print("\nYou need to add these to ItemType.cs before the palette will compile.")
        print("Insert them in alphabetical order within the enum.")

    class_name = f"ItemPalette{class_suffix}"
    output_path = OUTPUT_DIR / f"{class_name}.cs"

    lines = [
        "using System.Collections.Generic;",
        "",
        "namespace MinecraftClient.Inventory.ItemPalettes",
        "{",
        f"    public class {class_name} : ItemPalette",
        "    {",
        "        private static readonly Dictionary<int, ItemType> mappings = new();",
        "",
        f"        static {class_name}()",
        "        {",
    ]
    for idx, cs_name in mappings:
        lines.append(f"            mappings[{idx}] = ItemType.{cs_name};")
    lines += [
        "        }",
        "",
        "        protected override Dictionary<int, ItemType> GetDict()",
        "        {",
        "            return mappings;",
        "        }",
        "    }",
        "}",
        "",
    ]

    output_path.write_text("\n".join(lines))
    print(f"Generated {output_path} with {len(mappings)} mappings")


if __name__ == "__main__":
    main()
