#!/usr/bin/env python3
"""
Download block collision shapes from PrismarineJS minecraft-data and compact
them into a single JSON file for embedding in MCC's physics engine.

Usage:
    python3 tools/gen_block_shapes.py <mc_version>
    python3 tools/gen_block_shapes.py --from-file /path/to/blockCollisionShapes.json
    # e.g. python3 tools/gen_block_shapes.py 1.21.11

Output:
    MinecraftClient/Physics/BlockShapeData.json

The output JSON has two top-level keys:
  - "shapes": { shapeId -> [[x0,y0,z0,x1,y1,z1], ...] }
  - "blocks": { blockName -> shapeId | [shapeId, ...] }
"""

import json
import sys
import os
import subprocess
import tempfile

REPO = "PrismarineJS/minecraft-data"
BRANCH = "master"

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.dirname(SCRIPT_DIR)
OUTPUT_PATH = os.path.join(REPO_ROOT, "MinecraftClient", "Physics", "BlockShapeData.json")


def resolve_version_path(version: str) -> str:
    """Resolve actual data path using PrismarineJS dataPaths.json."""
    url = f"https://raw.githubusercontent.com/{REPO}/{BRANCH}/data/dataPaths.json"
    tmp = tempfile.mktemp(suffix=".json")
    try:
        subprocess.run(
            ["curl", "-sL", "--connect-timeout", "10", "--max-time", "30",
             "-o", tmp, url],
            check=True, timeout=35
        )
        with open(tmp) as f:
            data = json.load(f)
        pc = data.get("pc", {})
        if version in pc:
            entry = pc[version]
            bcs_path = entry.get("blockCollisionShapes", "")
            if bcs_path:
                return bcs_path  # e.g. "pc/1.21.11"
        return f"pc/{version}"
    except Exception as e:
        print(f"  Warning: could not resolve version path ({e}), using default")
        return f"pc/{version}"
    finally:
        if os.path.exists(tmp):
            os.remove(tmp)


def download_collision_shapes(version: str) -> dict:
    """Download blockCollisionShapes.json with curl and resume support."""
    ver_path = resolve_version_path(version)
    url = f"https://raw.githubusercontent.com/{REPO}/{BRANCH}/data/{ver_path}/blockCollisionShapes.json"
    print(f"Downloading: {url}")

    tmp = tempfile.mktemp(suffix=".json")
    max_retries = 5

    for attempt in range(1, max_retries + 1):
        print(f"  Attempt {attempt}/{max_retries}...")
        result = subprocess.run(
            ["curl", "-sL", "-C", "-",
             "--connect-timeout", "15", "--max-time", "180",
             "--retry", "3", "--retry-delay", "2",
             "-o", tmp, url],
            timeout=200
        )

        if not os.path.exists(tmp):
            print(f"  No file downloaded")
            continue

        size = os.path.getsize(tmp)
        print(f"  Downloaded {size:,} bytes")

        try:
            with open(tmp) as f:
                data = json.load(f)
            os.remove(tmp)
            return data
        except json.JSONDecodeError as e:
            print(f"  Incomplete/corrupt JSON ({e}), retrying...")
            # Don't delete tmp, curl -C - will resume

    if os.path.exists(tmp):
        os.remove(tmp)
    print(f"ERROR: Failed to download complete file after {max_retries} attempts.")
    print(f"You can manually download from: {url}")
    print(f"Then run: {sys.argv[0]} --from-file /path/to/blockCollisionShapes.json")
    sys.exit(1)


def compact(raw: dict) -> dict:
    """Convert PrismarineJS format to compacted format for embedding."""
    shapes_raw = raw.get("shapes", {})
    blocks_raw = raw.get("blocks", {})

    if not shapes_raw:
        raise ValueError("Could not find 'shapes' key in input JSON")
    if not blocks_raw:
        raise ValueError("Could not find 'blocks' key in input JSON")

    shapes = {}
    for sid, boxes in shapes_raw.items():
        compacted = []
        for box in boxes:
            compacted.append([round(c, 6) for c in box])
        shapes[sid] = compacted

    blocks = {}
    for name, data in blocks_raw.items():
        blocks[name] = data

    return {"shapes": shapes, "blocks": blocks}


def main():
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <mc_version>")
        print(f"       {sys.argv[0]} --from-file <path/to/blockCollisionShapes.json>")
        print()
        print(f"Example: {sys.argv[0]} 1.21.11")
        sys.exit(1)

    if sys.argv[1] == "--from-file":
        if len(sys.argv) < 3:
            print("Error: --from-file requires a file path")
            sys.exit(1)
        input_path = sys.argv[2]
        print(f"Reading from: {input_path}")
        with open(input_path) as f:
            raw = json.load(f)
    else:
        version = sys.argv[1]
        print(f"Fetching block collision shapes for MC {version}...")
        raw = download_collision_shapes(version)

    result = compact(raw)

    shape_count = len(result["shapes"])
    block_count = len(result["blocks"])
    print(f"  Shapes: {shape_count}")
    print(f"  Blocks: {block_count}")

    with open(OUTPUT_PATH, "w") as f:
        json.dump(result, f, separators=(",", ":"))

    file_size = os.path.getsize(OUTPUT_PATH)
    print(f"  Written to: {OUTPUT_PATH}")
    print(f"  File size: {file_size:,} bytes")
    print()
    print("Done. The file is embedded as a resource via MinecraftClient.csproj.")
    print("Rebuild MCC to include updated collision data.")


if __name__ == "__main__":
    main()
