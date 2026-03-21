
#!/usr/bin/env python3
"""
Generate ordered DeclareCommands argument-type arrays from decompiled ArgumentTypeInfos.java.

The runtime server registry excludes registrations guarded by SharedConstants.IS_RUNNING_IN_IDE,
so this script skips that block before emitting the final order.
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path


REGISTER_RE = re.compile(r'register\(\$\$0, "([^"]+)"')


def extract_runtime_argument_types(path: Path) -> list[str]:
    names: list[str] = []
    skipping_ide_block = False
    brace_depth = 0

    for line in path.read_text(encoding="utf-8").splitlines():
        if "if (SharedConstants.IS_RUNNING_IN_IDE)" in line:
            skipping_ide_block = True
            brace_depth += line.count("{") - line.count("}")
            continue

        if skipping_ide_block:
            brace_depth += line.count("{") - line.count("}")
            if brace_depth <= 0:
                skipping_ide_block = False
                brace_depth = 0
            continue

        match = REGISTER_RE.search(line)
        if match:
            names.append(match.group(1))

    return names


def emit_csharp_array(version: str, names: list[str]) -> str:
    lines = [
        f"// {version} ({len(names)})",
        f"private static readonly string[] s_modernArgumentTypes{version.replace('.', '')} =",
        "[",
    ]

    for index, name in enumerate(names):
        suffix = "," if index < len(names) - 1 else ""
        lines.append(f'    "{name}"{suffix}')

    lines.append("];")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "versions",
        nargs="+",
        help="Minecraft version folders under MinecraftOfficial/, for example: 1.20.6 1.21.5 1.21.6",
    )
    parser.add_argument(
        "--repo-root",
        default=Path(__file__).resolve().parents[1],
        type=Path,
        help="Repository root. Defaults to the current repo.",
    )
    args = parser.parse_args()

    for version in args.versions:
        source = args.repo_root / "MinecraftOfficial" / f"{version}-decompiled" / "net" / "minecraft" / "commands" / "synchronization" / "ArgumentTypeInfos.java"
        names = extract_runtime_argument_types(source)
        print(emit_csharp_array(version, names))
        print()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
