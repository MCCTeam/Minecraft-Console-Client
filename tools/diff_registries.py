#!/usr/bin/env python3
"""
Compare Minecraft registry data between two decompiled server versions.

Compares Items, EntityTypes, Blocks, DataComponents, and EntityDataSerializers
to determine which MCC palettes need updating for a new MC version.

Usage:
    python3 tools/diff_registries.py <old_version> <new_version> [--registry <registries.json>]

Examples:
    python3 tools/diff_registries.py 1.20.6 1.21.1
    python3 tools/diff_registries.py 1.21.8 1.21.9 --registry /tmp/mc_reports/reports/registries.json

The optional --registry flag cross-validates decompiled source counts against
the server's authoritative registries.json (generated via --reports).
Since MC 1.21.9, some items/blocks are registered outside Items.java/Blocks.java,
making this cross-validation essential for detecting hidden entries.
"""

import json
import re
import sys
import os
from pathlib import Path

DECOMPILED_ROOT = Path(__file__).resolve().parent.parent / "MinecraftOfficial"


def find_java_file(version_dir: Path, *possible_paths: str) -> Path | None:
    for p in possible_paths:
        full = version_dir / p
        if full.exists():
            return full
    return None


def extract_field_names(filepath: Path, pattern: str) -> list[str]:
    """Extract field names from public static final declarations."""
    results = []
    with open(filepath) as f:
        for line in f:
            m = re.match(pattern, line)
            if m:
                results.append(m.group(1))
    return results


def extract_register_multiline(filepath: Path) -> list[str]:
    """Extract register("name", ...) calls, handling multiline Java formatting."""
    with open(filepath) as f:
        content = f.read()
    flat = re.sub(r'\s+', ' ', content)
    return re.findall(r'(?:= |return )register\(\s*"([^"]+)"', flat)


def extract_static_register_order(filepath: Path) -> list[str]:
    """Extract registerSerializer(FIELD_NAME) calls from the static {} block."""
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


def compare_lists(old: list[str], new: list[str], label: str) -> bool:
    """Compare two ordered lists and report differences."""
    set_old, set_new = set(old), set(new)
    added = sorted(set_new - set_old)
    removed = sorted(set_old - set_new)

    print(f"\n{'='*60}")
    print(f"  {label}")
    print(f"{'='*60}")
    print(f"  Old: {len(old)} entries, New: {len(new)} entries")

    if not added and not removed:
        if old == new:
            print(f"  Result: IDENTICAL — no palette update needed")
        else:
            print(f"  Result: Same set but DIFFERENT ORDER — palette update needed!")
            for i, (a, b) in enumerate(zip(old, new)):
                if a != b:
                    print(f"    First diff at index {i}: old={a}, new={b}")
                    break
        return False

    if added:
        print(f"  Added ({len(added)}): {added}")
        for item in added:
            idx = new.index(item)
            prev_name = new[idx - 1] if idx > 0 else "(start)"
            next_name = new[idx + 1] if idx < len(new) - 1 else "(end)"
            print(f"    \"{item}\" at index {idx}, between \"{prev_name}\" and \"{next_name}\"")
    if removed:
        print(f"  Removed ({len(removed)}): {removed}")

    common_old = [x for x in old if x in set_new]
    common_new = [x for x in new if x in set_old]
    if common_old != common_new:
        print(f"  Common items REORDERED — palette update needed!")
    else:
        print(f"  Common items have same relative order")

    # ID shift analysis
    id_old = {name: i for i, name in enumerate(old)}
    id_new = {name: i for i, name in enumerate(new)}
    shifted = [(n, id_old[n], id_new[n]) for n in sorted(set_old & set_new) if id_old[n] != id_new[n]]
    if shifted:
        from collections import Counter
        deltas = Counter(new_id - old_id for _, old_id, new_id in shifted)
        print(f"  {len(shifted)} entries with changed IDs, delta distribution: {sorted(deltas.items())}")

    print(f"  Result: PALETTE UPDATE NEEDED")
    return True


def cross_validate(registry_data: dict, java_entries: list[str], registry_key: str,
                   label: str, convert_fn=None):
    """Cross-validate Java source entries against server registries.json."""
    reg = registry_data.get(registry_key, {}).get("entries", {})
    server_names = set()
    for key in reg:
        name = key.removeprefix("minecraft:")
        server_names.add(name)

    if convert_fn:
        java_names = set(convert_fn(n) for n in java_entries)
    else:
        java_names = set(n.lower() for n in java_entries)

    server_count = len(server_names)
    java_count = len(java_names)

    print(f"\n  --- Cross-validation: {label} ---")
    print(f"  Java source: {java_count} entries, Server registry: {server_count} entries")

    if server_count == java_count:
        print(f"  ✓ Counts match — Java source is complete")
    else:
        diff = server_count - java_count
        print(f"  ⚠ Count MISMATCH: server has {diff:+d} entries vs Java source")
        extra_in_server = server_names - java_names
        extra_in_java = java_names - server_names
        if extra_in_server:
            print(f"  In server but NOT in Java source ({len(extra_in_server)}):")
            for n in sorted(extra_in_server):
                pid = reg[f"minecraft:{n}"]["protocol_id"]
                print(f"    [{pid}] {n}")
            print(f"  ⚠ MUST use --from-registry / server data to generate palette!")
        if extra_in_java:
            print(f"  In Java source but NOT in server ({len(extra_in_java)}):")
            for n in sorted(extra_in_java):
                print(f"    {n}")


def diff_items(old_dir: Path, new_dir: Path, registry_data: dict | None = None):
    old_f = find_java_file(old_dir, "net/minecraft/world/item/Items.java")
    new_f = find_java_file(new_dir, "net/minecraft/world/item/Items.java")
    if not old_f or not new_f:
        print("  [SKIP] Items.java not found")
        return
    pattern = r'\s+public static final Item (\w+)\s*='
    old = extract_field_names(old_f, pattern)
    new = extract_field_names(new_f, pattern)
    compare_lists(old, new, "Items.java (Item registry)")

    if registry_data:
        cross_validate(registry_data, new, "minecraft:item", "Items",
                       convert_fn=lambda n: n.lower())


def diff_entity_types(old_dir: Path, new_dir: Path, registry_data: dict | None = None):
    old_f = find_java_file(old_dir, "net/minecraft/world/entity/EntityType.java")
    new_f = find_java_file(new_dir, "net/minecraft/world/entity/EntityType.java")
    if not old_f or not new_f:
        print("  [SKIP] EntityType.java not found")
        return
    old = extract_register_multiline(old_f)
    new = extract_register_multiline(new_f)
    compare_lists(old, new, "EntityType.java (Entity registry)")

    if registry_data:
        cross_validate(registry_data, new, "minecraft:entity_type", "EntityType",
                       convert_fn=lambda n: n)


def diff_blocks(old_dir: Path, new_dir: Path, registry_data: dict | None = None):
    old_f = find_java_file(old_dir, "net/minecraft/world/level/block/Blocks.java")
    new_f = find_java_file(new_dir, "net/minecraft/world/level/block/Blocks.java")
    if not old_f or not new_f:
        print("  [SKIP] Blocks.java not found")
        return
    old = extract_register_multiline(old_f)
    new = extract_register_multiline(new_f)
    compare_lists(old, new, "Blocks.java (Block registry)")

    if registry_data:
        cross_validate(registry_data, new, "minecraft:block", "Blocks",
                       convert_fn=lambda n: n)


def diff_data_components(old_dir: Path, new_dir: Path):
    old_f = find_java_file(old_dir, "net/minecraft/core/component/DataComponents.java")
    new_f = find_java_file(new_dir, "net/minecraft/core/component/DataComponents.java")
    if not old_f or not new_f:
        print("  [SKIP] DataComponents.java not found")
        return
    old = extract_register_multiline(old_f)
    new = extract_register_multiline(new_f)
    needs_update = compare_lists(old, new, "DataComponents.java (StructuredComponents registry)")
    if needs_update or True:
        print("\n  Registration order (new version):")
        for i, name in enumerate(new):
            marker = " <-- NEW" if name not in set(old) else ""
            print(f"    {i}: {name}{marker}")


def diff_entity_data_serializers(old_dir: Path, new_dir: Path):
    old_f = find_java_file(old_dir, "net/minecraft/network/syncher/EntityDataSerializers.java")
    new_f = find_java_file(new_dir, "net/minecraft/network/syncher/EntityDataSerializers.java")
    if not old_f or not new_f:
        print("  [SKIP] EntityDataSerializers.java not found")
        return
    old = extract_static_register_order(old_f)
    new = extract_static_register_order(new_f)
    needs_update = compare_lists(old, new, "EntityDataSerializers.java (EntityMetadata palette)")
    print("\n  Registration order (new version):")
    for i, name in enumerate(new):
        marker = " <-- NEW" if name not in set(old) else ""
        print(f"    {i}: {name}{marker}")


def main():
    # Parse arguments
    args = sys.argv[1:]
    registry_path = None

    if "--registry" in args:
        idx = args.index("--registry")
        if idx + 1 >= len(args):
            print("Error: --registry requires a path argument")
            sys.exit(1)
        registry_path = Path(args[idx + 1])
        args = args[:idx] + args[idx + 2:]

    if len(args) != 2:
        print(__doc__)
        sys.exit(1)

    old_ver, new_ver = args[0], args[1]
    old_dir = DECOMPILED_ROOT / f"{old_ver}-decompiled"
    new_dir = DECOMPILED_ROOT / f"{new_ver}-decompiled"

    for d, v in [(old_dir, old_ver), (new_dir, new_ver)]:
        if not d.exists():
            print(f"Error: {d} not found. Decompile {v} first:")
            print(f"  cd MinecraftOfficial && java -jar MinecraftDecompiler.jar "
                  f"--version {v} --side SERVER --decompile "
                  f"--output {v}-remapped.jar --decompiled-output {v}-decompiled")
            sys.exit(1)

    registry_data = None
    if registry_path:
        if not registry_path.exists():
            print(f"Error: {registry_path} not found")
            sys.exit(1)
        with open(registry_path) as f:
            registry_data = json.load(f)
        print(f"Loaded server registries.json for cross-validation")

    print(f"Comparing MC {old_ver} → {new_ver}")
    print(f"Old: {old_dir}")
    print(f"New: {new_dir}")

    diff_items(old_dir, new_dir, registry_data)
    diff_entity_types(old_dir, new_dir, registry_data)
    diff_blocks(old_dir, new_dir, registry_data)
    diff_data_components(old_dir, new_dir)
    diff_entity_data_serializers(old_dir, new_dir)

    print(f"\n{'='*60}")
    print("  Summary")
    print(f"{'='*60}")
    print("  Review each section above. For any marked 'PALETTE UPDATE NEEDED',")
    print("  create a new palette file in MCC and update the version routing.")
    print("  For 'IDENTICAL' sections, the existing palette can be reused.")
    if registry_data:
        print("\n  Cross-validation was performed against server registries.json.")
        print("  If any count mismatches were found, use server data generator output")
        print("  (--from-registry) instead of decompiled Java source for palette generation.")


if __name__ == "__main__":
    main()
