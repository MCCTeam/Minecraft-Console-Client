#!/bin/bash
# Download (if needed) and run MinecraftDecompiler to produce decompiled source
# and server.jar for a given Minecraft version.
#
# Usage:
#   ./tools/decompile.sh --version <ver> [--side SERVER|CLIENT]
#
# Examples:
#   ./tools/decompile.sh --version 1.21.11
#   ./tools/decompile.sh --version 1.21.11 --side CLIENT

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MC_OFFICIAL="$REPO_ROOT/MinecraftOfficial"
DECOMPILER_JAR="$MC_OFFICIAL/MinecraftDecompiler.jar"
DECOMPILER_REPO="MaxPixelStudios/MinecraftDecompiler"

VERSION=""
SIDE="SERVER"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --version) VERSION="$2"; shift 2 ;;
        --side)    SIDE="$(echo "$2" | tr '[:lower:]' '[:upper:]')"; shift 2 ;;
        -h|--help)
            echo "Usage: $0 --version <ver> [--side SERVER|CLIENT]"
            echo ""
            echo "Options:"
            echo "  --version <ver>     Minecraft version (e.g. 1.21.11)"
            echo "  --side <env>        SERVER (default) or CLIENT"
            exit 0
            ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

if [[ -z "$VERSION" ]]; then
    echo "Error: --version is required"
    echo "Usage: $0 --version <ver> [--side SERVER|CLIENT]"
    exit 1
fi

if [[ "$SIDE" != "SERVER" && "$SIDE" != "CLIENT" ]]; then
    echo "Error: --side must be SERVER or CLIENT (got: $SIDE)"
    exit 1
fi

# --- Ensure MinecraftDecompiler.jar exists ---
if [[ ! -f "$DECOMPILER_JAR" ]]; then
    echo "MinecraftDecompiler.jar not found, downloading latest release..."
    DOWNLOAD_URL=$(curl -sL "https://api.github.com/repos/$DECOMPILER_REPO/releases/latest" \
        | python3 -c "
import json, sys
data = json.load(sys.stdin)
for a in data['assets']:
    if a['name'] == 'MinecraftDecompiler.jar':
        print(a['browser_download_url'])
        break
")
    if [[ -z "$DOWNLOAD_URL" ]]; then
        echo "Error: could not find MinecraftDecompiler.jar in latest release"
        exit 1
    fi
    echo "Downloading from $DOWNLOAD_URL ..."
    curl -L -o "$DECOMPILER_JAR" "$DOWNLOAD_URL"
    echo "Downloaded MinecraftDecompiler.jar"
fi

# --- Build output paths ---
SIDE_LOWER="$(echo "$SIDE" | tr '[:upper:]' '[:lower:]')"

if [[ "$SIDE" == "SERVER" ]]; then
    REMAPPED_JAR="$MC_OFFICIAL/remapped_jar/${VERSION}-remapped.jar"
    DECOMPILED_DIR="$MC_OFFICIAL/${VERSION}-decompiled"
else
    REMAPPED_JAR="$MC_OFFICIAL/remapped_jar/${VERSION}-${SIDE_LOWER}-remapped.jar"
    DECOMPILED_DIR="$MC_OFFICIAL/${VERSION}-${SIDE_LOWER}-decompiled"
fi

if [[ -d "$DECOMPILED_DIR" ]]; then
    echo "Decompiled source already exists: $DECOMPILED_DIR"
    echo "Delete it first if you want to re-decompile."
    exit 0
fi

mkdir -p "$MC_OFFICIAL/remapped_jar"

echo "=== Decompiling Minecraft $VERSION ($SIDE) ==="
echo "  Remapped JAR: $REMAPPED_JAR"
echo "  Decompiled:   $DECOMPILED_DIR"
echo ""

cd "$MC_OFFICIAL"
java -jar "$DECOMPILER_JAR" \
    --version "$VERSION" \
    --side "$SIDE" \
    --decompile \
    --output "$REMAPPED_JAR" \
    --decompiled-output "$DECOMPILED_DIR"

echo ""
echo "=== Done ==="
echo "Decompiled source: $DECOMPILED_DIR"

# --- For SERVER side, also ensure downloads/<ver>/server.jar exists ---
if [[ "$SIDE" == "SERVER" ]]; then
    DOWNLOADS_DIR="$MC_OFFICIAL/downloads/$VERSION"
    if [[ ! -f "$DOWNLOADS_DIR/server.jar" ]]; then
        mkdir -p "$DOWNLOADS_DIR"
        # MinecraftDecompiler downloads the original jar into its cache;
        # extract it from the bundled remapped jar or re-download via manifest.
        echo ""
        echo "Downloading server.jar for $VERSION into $DOWNLOADS_DIR ..."
        MANIFEST_URL="https://launchermeta.mojang.com/mc/game/version_manifest_v2.json"
        VERSION_URL=$(curl -sL "$MANIFEST_URL" | python3 -c "
import json, sys
data = json.load(sys.stdin)
for v in data['versions']:
    if v['id'] == '$VERSION':
        print(v['url'])
        break
")
        if [[ -n "$VERSION_URL" ]]; then
            SERVER_JAR_URL=$(curl -sL "$VERSION_URL" | python3 -c "
import json, sys
data = json.load(sys.stdin)
print(data['downloads']['server']['url'])
")
            curl -L -o "$DOWNLOADS_DIR/server.jar" "$SERVER_JAR_URL"
            echo "Downloaded server.jar"
        else
            echo "Warning: could not find version $VERSION in Mojang manifest; server.jar not downloaded."
        fi
    else
        echo "server.jar already exists: $DOWNLOADS_DIR/server.jar"
    fi
fi
