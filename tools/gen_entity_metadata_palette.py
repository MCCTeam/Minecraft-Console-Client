#!/usr/bin/env python3
"""
Generate an MCC EntityMetadataPalette C# file from decompiled EntityDataSerializers.java.

Reads the static {} block registration order to determine serializer IDs,
then maps Java field names to MCC's EntityMetaDataType enum values.

Usage:
    python3 tools/gen_entity_metadata_palette.py <mc_version> <class_suffix>

Example:
    python3 tools/gen_entity_metadata_palette.py 1.20.6 1206
    # Generates EntityMetadataPalette1206.cs
"""

import re
import sys
from pathlib import Path

DECOMPILED_ROOT = Path(__file__).resolve().parent.parent / "MinecraftOfficial"
OUTPUT_DIR = (Path(__file__).resolve().parent.parent /
              "MinecraftClient" / "Mapping" / "EntityMetadataPalettes")

# Java field name → MCC EntityMetaDataType enum name
FIELD_TO_ENUM = {
    "BYTE":                  "Byte",
    "INT":                   "VarInt",
    "LONG":                  "VarLong",
    "FLOAT":                 "Float",
    "STRING":                "String",
    "COMPONENT":             "Chat",
    "OPTIONAL_COMPONENT":    "OptionalChat",
    "ITEM_STACK":            "Slot",
    "BOOLEAN":               "Boolean",
    "ROTATIONS":             "Rotation",
    "BLOCK_POS":             "Position",
    "OPTIONAL_BLOCK_POS":    "OptionalPosition",
    "DIRECTION":             "Direction",
    "OPTIONAL_UUID":         "OptionalUuid",
    "BLOCK_STATE":           "BlockId",
    "OPTIONAL_BLOCK_STATE":  "OptionalBlockId",
    "COMPOUND_TAG":          "Nbt",
    "PARTICLE":              "Particle",
    "PARTICLES":             "Particles",
    "VILLAGER_DATA":         "VillagerData",
    "OPTIONAL_UNSIGNED_INT": "OptionalVarInt",
    "POSE":                  "Pose",
    "CAT_VARIANT":           "CatVariant",
    "WOLF_VARIANT":          "WolfVariant",
    "FROG_VARIANT":          "FrogVariant",
    "OPTIONAL_GLOBAL_POS":   "OptionalGlobalPosition",
    "PAINTING_VARIANT":      "PaintingVariant",
    "SNIFFER_STATE":         "SnifferState",
    "ARMADILLO_STATE":       "ArmadilloState",
    "VECTOR3":               "Vector3",
    "QUATERNION":            "Quaternion",
}


def extract_static_register_order(filepath: Path) -> list[str]:
    results = []
    in_static = False
    with open(filepath) as f:
        for line in f:
            if 'static {' in line:
                in_static = True
                continue
            if in_static and 'registerSerializer(' in line:
                m = re.search(r'registerSerializer\((\w+)\)', line)
                if m:
                    results.append(m.group(1))
            if in_static and '}' in line and 'registerSerializer' not in line:
                break
    return results


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)

    mc_version = sys.argv[1]
    class_suffix = sys.argv[2]
    version_dir = DECOMPILED_ROOT / f"{mc_version}-decompiled"
    eds_java = version_dir / "net" / "minecraft" / "network" / "syncher" / "EntityDataSerializers.java"

    if not eds_java.exists():
        print(f"Error: {eds_java} not found")
        sys.exit(1)

    fields = extract_static_register_order(eds_java)
    print(f"Found {len(fields)} entity data serializers in MC {mc_version}:")

    unmapped = []
    mappings = []
    for i, field in enumerate(fields):
        if field in FIELD_TO_ENUM:
            enum_name = FIELD_TO_ENUM[field]
            mappings.append((i, enum_name))
            print(f"  {i}: {field} -> EntityMetaDataType.{enum_name}")
        else:
            unmapped.append((i, field))
            print(f"  {i}: {field} -> ??? UNMAPPED")

    if unmapped:
        print(f"\nWARNING: {len(unmapped)} unmapped fields:")
        for idx, field in unmapped:
            print(f"  [{idx}] {field}")
        print("\nAdd entries to FIELD_TO_ENUM in this script and to EntityMetaDataType.cs enum.")

    class_name = f"EntityMetadataPalette{class_suffix}"
    output_path = OUTPUT_DIR / f"{class_name}.cs"

    lines = [
        "using System.Collections.Generic;",
        "",
        f"namespace MinecraftClient.Mapping.EntityMetadataPalettes;",
        "",
        f"public class {class_name} : EntityMetadataPalette",
        "{",
        "    private readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings = new()",
        "    {",
    ]
    for idx, enum_name in mappings:
        lines.append(f"        {{ {idx}, EntityMetaDataType.{enum_name} }},")
    lines += [
        "    };",
        "",
        "    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()",
        "    {",
        "        return entityMetadataMappings;",
        "    }",
        "}",
        "",
    ]

    output_path.write_text("\n".join(lines))
    print(f"\nGenerated {output_path} with {len(mappings)} mappings")


if __name__ == "__main__":
    main()
