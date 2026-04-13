#!/bin/bash
# Download (if needed) and run MinecraftDecompiler to produce decompiled source
# and server.jar for a given Minecraft version.
#
# Usage:
#   ./tools/decompile.sh --version <ver> [--side SERVER|CLIENT]
#
# Examples:
#   ./tools/decompile.sh --version 1.21.11-Vanilla
#   ./tools/decompile.sh --version 1.21.11 --side CLIENT

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MC_OFFICIAL="$REPO_ROOT/MinecraftOfficial"
SERVERS_ROOT="${MCC_SERVERS:-$MC_OFFICIAL/downloads}"
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
            echo "  --version <ver>     Minecraft version or local server dir (e.g. 1.21.11 or 1.21.11-Vanilla)"
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

MC_VERSION="${VERSION%-Vanilla}"
if [[ -z "$MC_VERSION" ]]; then
    MC_VERSION="$VERSION"
fi

SERVER_DIR_NAME="$VERSION"
if [[ "$SIDE" == "SERVER" && "$VERSION" != *-Vanilla ]]; then
    SERVER_DIR_NAME="${MC_VERSION}-Vanilla"
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
    REMAPPED_JAR="$MC_OFFICIAL/remapped_jar/${MC_VERSION}-remapped.jar"
    DECOMPILED_DIR="$MC_OFFICIAL/${MC_VERSION}-decompiled"
else
    REMAPPED_JAR="$MC_OFFICIAL/remapped_jar/${MC_VERSION}-${SIDE_LOWER}-remapped.jar"
    DECOMPILED_DIR="$MC_OFFICIAL/${MC_VERSION}-${SIDE_LOWER}-decompiled"
fi

if [[ -d "$DECOMPILED_DIR" ]]; then
    echo "Decompiled source already exists: $DECOMPILED_DIR"
    echo "Delete it first if you want to re-decompile."
    exit 0
fi

mkdir -p "$MC_OFFICIAL/remapped_jar"

# --- Resolve version metadata from Mojang manifest ---
MANIFEST_URL="https://launchermeta.mojang.com/mc/game/version_manifest_v2.json"
VERSION_URL=$(curl -sL "$MANIFEST_URL" | python3 -c "
import json, sys
data = json.load(sys.stdin)
for v in data['versions']:
    if v['id'] == '$MC_VERSION':
        print(v['url'])
        break
")
if [[ -z "$VERSION_URL" ]]; then
    echo "Error: version $MC_VERSION not found in Mojang launcher manifest."
    exit 1
fi

VERSION_META=$(curl -sL "$VERSION_URL")
MAPPING_KEY="${SIDE_LOWER}_mappings"
HAS_MAPPINGS=$(echo "$VERSION_META" | python3 -c "
import json, sys
data = json.load(sys.stdin)
print('true' if '$MAPPING_KEY' in data.get('downloads', {}) else 'false')
")

echo "=== Decompiling Minecraft $MC_VERSION ($SIDE) ==="
echo "  Remapped JAR: $REMAPPED_JAR"
echo "  Decompiled:   $DECOMPILED_DIR"
if [[ "$SIDE" == "SERVER" ]]; then
    echo "  Server dir:   $SERVERS_ROOT/$SERVER_DIR_NAME"
fi
echo "  Obfuscated:   $HAS_MAPPINGS"
echo ""

cd "$MC_OFFICIAL"

if [[ "$HAS_MAPPINGS" == "true" ]]; then
    # Obfuscated version: use --version/--side to auto-download jar + mappings + deobfuscate
    java -jar "$DECOMPILER_JAR" \
        --version "$MC_VERSION" \
        --side "$SIDE" \
        --decompile \
        --output "$REMAPPED_JAR" \
        --decompiled-output "$DECOMPILED_DIR"
else
    # Unobfuscated version (26.1+): download jar, extract inner jar from bundle, decompile directly.
    # MinecraftDecompiler requires --mapping-path with --input, but unobfuscated versions
    # have no mappings. We use Vineflower directly instead.
    echo "No Proguard mappings for $MC_VERSION; decompiling without deobfuscation."

    JAR_URL=$(echo "$VERSION_META" | python3 -c "
import json, sys
data = json.load(sys.stdin)
print(data['downloads']['${SIDE_LOWER}']['url'])
")
    ORIGINAL_JAR="$MC_OFFICIAL/remapped_jar/${MC_VERSION}-${SIDE_LOWER}-original.jar"
    if [[ ! -f "$ORIGINAL_JAR" ]]; then
        echo "Downloading ${SIDE_LOWER}.jar ..."
        curl -L -o "$ORIGINAL_JAR" "$JAR_URL"
    fi

    # Since 1.18, server.jar is a bundled jar containing the actual game jar inside
    # META-INF/versions/<ver>/server-<ver>.jar. Extract it if present.
    DECOMPILE_TARGET="$ORIGINAL_JAR"
    EXTRACT_DIR=$(mktemp -d)
    trap "rm -rf '$EXTRACT_DIR'" EXIT
    if unzip -q -o "$ORIGINAL_JAR" "META-INF/versions.list" -d "$EXTRACT_DIR" 2>/dev/null; then
        INNER_PATH=$(awk '{print $NF}' "$EXTRACT_DIR/META-INF/versions.list" | head -1)
        if [[ -n "$INNER_PATH" ]]; then
            unzip -q -o "$ORIGINAL_JAR" "META-INF/versions/$INNER_PATH" -d "$EXTRACT_DIR"
            DECOMPILE_TARGET="$EXTRACT_DIR/META-INF/versions/$INNER_PATH"
            echo "Extracted inner jar: $INNER_PATH"
        fi
    fi

    # Use Vineflower directly (bundled with MinecraftDecompiler, or standalone)
    VINEFLOWER_JAR="$MC_OFFICIAL/downloads/decompiler/vineflower.jar"
    if [[ ! -f "$VINEFLOWER_JAR" ]]; then
        # Fall back to vineflower bundled inside MinecraftDecompiler's cache
        VINEFLOWER_JAR=$(find "$MC_OFFICIAL" -name "vineflower*.jar" -not -name "MinecraftDecompiler.jar" 2>/dev/null | head -1)
    fi
    if [[ -z "$VINEFLOWER_JAR" || ! -f "$VINEFLOWER_JAR" ]]; then
        echo "Error: vineflower.jar not found. Place it at $MC_OFFICIAL/downloads/decompiler/vineflower.jar"
        exit 1
    fi

    echo "Decompiling with Vineflower: $VINEFLOWER_JAR"
    java -jar "$VINEFLOWER_JAR" "$DECOMPILE_TARGET" "$DECOMPILED_DIR"
fi

echo ""
echo "=== Done ==="
echo "Decompiled source: $DECOMPILED_DIR"

# --- For SERVER side, also ensure downloads/<server-dir>/server.jar exists ---
if [[ "$SIDE" == "SERVER" ]]; then
    DOWNLOADS_DIR="$SERVERS_ROOT/$SERVER_DIR_NAME"
    if [[ ! -f "$DOWNLOADS_DIR/server.jar" ]]; then
        mkdir -p "$DOWNLOADS_DIR"
        echo ""
        echo "Downloading server.jar for $MC_VERSION into $DOWNLOADS_DIR ..."
        SERVER_JAR_URL=$(echo "$VERSION_META" | python3 -c "
import json, sys
data = json.load(sys.stdin)
print(data['downloads']['server']['url'])
")
        if [[ -n "$SERVER_JAR_URL" ]]; then
            curl -L -o "$DOWNLOADS_DIR/server.jar" "$SERVER_JAR_URL"
            echo "Downloaded server.jar"
        else
            echo "Warning: could not download server.jar for $VERSION."
        fi
    else
        echo "server.jar already exists: $DOWNLOADS_DIR/server.jar"
    fi
fi
