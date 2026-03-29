#!/bin/sh
# Minecraft Console Client - Installer
# Downloads the latest MinecraftClient binary for your Linux or macOS platform.
# Usage: curl -fsSL https://mccteam.github.io/install.sh | sh
#    or: wget -qO- https://mccteam.github.io/install.sh | sh

set -e

REPO="MCCTeam/Minecraft-Console-Client"
OUTPUT="MinecraftClient"

# --- Detect OS ---
OS=$(uname -s)
case "$OS" in
    Linux)  PLATFORM="linux" ;;
    Darwin) PLATFORM="osx"   ;;
    *)
        echo "Error: Unsupported OS '$OS'. This script supports Linux and macOS." >&2
        exit 1
        ;;
esac

# --- Detect CPU architecture ---
ARCH=$(uname -m)
case "$ARCH" in
    x86_64|amd64)         ARCH_ID="x64"   ;;
    aarch64|arm64)        ARCH_ID="arm64" ;;
    armv7l|armv8l|armhf)  ARCH_ID="arm"   ;;
    arm*)                 ARCH_ID="arm"   ;;
    *)
        echo "Error: Unsupported CPU architecture '$ARCH'." >&2
        exit 1
        ;;
esac

# macOS does not have an arm (32-bit) build
if [ "$PLATFORM" = "osx" ] && [ "$ARCH_ID" = "arm" ]; then
    echo "Error: 32-bit ARM is not supported on macOS." >&2
    exit 1
fi

SUFFIX="${PLATFORM}-${ARCH_ID}"

# --- Download helpers: prefer curl, fall back to wget ---
_download_stdout() {
    if command -v curl >/dev/null 2>&1; then
        curl -fsSL "$1"
    elif command -v wget >/dev/null 2>&1; then
        wget -qO- "$1"
    else
        echo "Error: Neither 'curl' nor 'wget' is available. Please install one and retry." >&2
        exit 1
    fi
}

_download_file() {
    if command -v curl >/dev/null 2>&1; then
        curl -fL --progress-bar -o "$2" "$1"
    elif command -v wget >/dev/null 2>&1; then
        # --show-progress forces the progress bar even when stdout is not a TTY.
        # Fall back silently to default output if the flag is not supported
        # (older wget versions, e.g. BusyBox wget).
        if wget --show-progress -O "$2" "$1" 2>/dev/null; then
            return 0
        fi
        wget -O "$2" "$1"
    else
        echo "Error: Neither 'curl' nor 'wget' is available. Please install one and retry." >&2
        exit 1
    fi
}

# --- Fetch latest release metadata from GitHub API ---
API_URL="https://api.github.com/repos/${REPO}/releases/latest"
echo "Fetching latest release information..."
RELEASE_JSON=$(_download_stdout "$API_URL")

# --- Parse asset download URL (no external tools required) ---
# The JSON key "browser_download_url" appears once per asset.
# We match the key followed by the URL, anchoring on the platform-arch suffix
# and the closing quote so that e.g. "linux-arm" does not match "linux-arm64".
# The ' *: *' pattern handles optional spaces around the colon (GitHub API adds spaces).
ASSET_URL=$(printf '%s' "$RELEASE_JSON" \
    | grep -o '"browser_download_url" *: *"[^"]*-'"${SUFFIX}"'"' \
    | grep -o 'https://[^"]*' \
    | head -1)

if [ -z "$ASSET_URL" ]; then
    echo "Error: Could not find a release asset for platform '${SUFFIX}'." >&2
    exit 1
fi

# --- Extract tag name for display ---
TAG=$(printf '%s' "$RELEASE_JSON" \
    | grep -o '"tag_name" *: *"[^"]*"' \
    | head -1 \
    | grep -o '"[^"]*"$' \
    | tr -d '"')

echo "Downloading MinecraftClient ${TAG} (${SUFFIX})..."
_download_file "$ASSET_URL" "$OUTPUT"
chmod +x "$OUTPUT"

echo ""
echo "Downloaded: ./${OUTPUT}"
echo "Run with:   ./${OUTPUT} --help"
