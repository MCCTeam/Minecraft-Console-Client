#!/usr/bin/env python3
"""
Generate an MCC ItemPalette C# file.

Supports two input modes:
  1. Server registry (preferred since 1.21.9):
     python3 tools/gen_item_palette.py --from-registry /tmp/mc_reports/reports/registries.json <suffix>

  2. Decompiled Items.java (legacy):
     python3 tools/gen_item_palette.py <mc_version> <suffix>

The --from-registry mode uses the server's authoritative protocol_id assignments,
which is required since MC 1.21.9 where some items are registered outside Items.java.

The <suffix> determines the class name (ItemPalette<suffix>) and should match MCC's
naming convention (e.g., 121 for 1.21, 1219 for 1.21.9).
"""

import json
import re
import sys
from pathlib import Path

DECOMPILED_ROOT = Path(__file__).resolve().parent.parent / "MinecraftOfficial"
OUTPUT_DIR = Path(__file__).resolve().parent.parent / "MinecraftClient" / "Inventory" / "ItemPalettes"
ITEM_TYPE_CS = OUTPUT_DIR.parent / "ItemType.cs"

OVERRIDES = {
    "CUT_STANDSTONE_SLAB": "CutSandstoneSlab",  # Mojang typo in source
}


def mc_name_to_csharp(mc_name: str) -> str:
    """Convert minecraft:snake_case to PascalCase C# enum name."""
    name = mc_name.removeprefix("minecraft:")
    if name.upper() in OVERRIDES:
        return OVERRIDES[name.upper()]
    return "".join(word.capitalize() for word in name.split("_"))


def java_to_csharp_name(java_name: str) -> str:
    """Convert SCREAMING_SNAKE_CASE Java field name to PascalCase C# enum name."""
    if java_name in OVERRIDES:
        return OVERRIDES[java_name]
    return "".join(word.capitalize() for word in java_name.lower().split("_"))


def load_known_enums() -> set[str]:
    known = set()
    if ITEM_TYPE_CS.exists():
        with open(ITEM_TYPE_CS) as f:
            for line in f:
                m = re.match(r'\s+(\w+),?\s*$', line)
                if m and m.group(1) not in ("Null", "Unknown"):
                    known.add(m.group(1))
    return known


def items_from_registry(registry_path: Path) -> list[tuple[int, str]]:
    """Load items from server registries.json, returns sorted (protocol_id, cs_name) pairs."""
    with open(registry_path) as f:
        data = json.load(f)
    items_reg = data.get("minecraft:item", {}).get("entries", {})
    result = []
    for item_key, info in items_reg.items():
        pid = info["protocol_id"]
        cs_name = mc_name_to_csharp(item_key)
        result.append((pid, cs_name))
    result.sort(key=lambda x: x[0])
    return result


def items_from_java(mc_version: str) -> list[tuple[int, str]]:
    """Load items from decompiled Items.java field declaration order."""
    version_dir = DECOMPILED_ROOT / f"{mc_version}-decompiled"
    items_java = version_dir / "net" / "minecraft" / "world" / "item" / "Items.java"
    if not items_java.exists():
        print(f"Error: {items_java} not found")
        sys.exit(1)

    pattern = re.compile(r'\s+public static final Item (\w+)\s*=')
    result = []
    with open(items_java) as f:
        for line in f:
            m = pattern.match(line)
            if m:
                idx = len(result)
                cs_name = java_to_csharp_name(m.group(1))
                result.append((idx, cs_name))
    return result


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)

    from_registry = sys.argv[1] == "--from-registry"

    if from_registry:
        if len(sys.argv) != 4:
            print("Usage: gen_item_palette.py --from-registry <registries.json> <suffix>")
            sys.exit(1)
        registry_path = Path(sys.argv[2])
        class_suffix = sys.argv[3]
        if not registry_path.exists():
            print(f"Error: {registry_path} not found")
            sys.exit(1)
        mappings = items_from_registry(registry_path)
        print(f"Loaded {len(mappings)} items from {registry_path}")
    else:
        mc_version = sys.argv[1]
        class_suffix = sys.argv[2]
        mappings = items_from_java(mc_version)
        print(f"Found {len(mappings)} items in MC {mc_version} Items.java")

    known_enums = load_known_enums()
    missing = [(pid, cs) for pid, cs in mappings if known_enums and cs not in known_enums]
    if missing:
        print(f"\nWARNING: {len(missing)} items not found in ItemType.cs enum:")
        for pid, cs_name in missing:
            print(f"  [{pid}] {cs_name}")
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
    for pid, cs_name in mappings:
        lines.append(f"            mappings[{pid}] = ItemType.{cs_name};")
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
