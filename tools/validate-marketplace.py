"""Validate a schema-v2 marketplace through the shared runtime models."""
import os
from pathlib import Path
import subprocess
import sys


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: validate-marketplace.py <index-file-or-marketplace-folder>", file=sys.stderr)
        return 1
    root = Path(__file__).resolve().parents[1]
    client = Path(os.environ.get("MCC_CLI_DLL", str(root / "src/Mcc.Cli/bin/Release/net10.0/Mcc.Cli.dll")))
    if not client.is_file():
        print("Build MCC with mcc-build before validating a marketplace.", file=sys.stderr)
        return 1
    return subprocess.run(["dotnet", str(client), "--validate-marketplace", sys.argv[1]], check=False).returncode


if __name__ == "__main__":
    sys.exit(main())
