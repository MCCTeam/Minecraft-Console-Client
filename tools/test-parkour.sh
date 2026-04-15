#!/usr/bin/env bash
# Full-coverage parkour test suite for MCC pathfinding
# Thin wrapper around test-parkour.py
# Usage: source tools/mcc-env.sh && bash tools/test-parkour.sh [args...]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
exec python3 "$SCRIPT_DIR/test-parkour.py" "$@"
