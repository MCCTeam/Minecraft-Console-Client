#!/bin/sh

cd /opt/data || exit 1

echo "platform is ${MCC_PLATFORM}"

if [ -e "./MinecraftClient" -a -n "$MCC_SKIP_REDOWNLOAD" ]; then
  echo "Skip re-download MinecraftClient"
else
  # Use the provided version tag or get the latest release tag
  RELEASE_TAG=${MCC_VERSION:-$(curl -s -v https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest 2>&1 | grep -i location: | tr -d '\r' | cut -d/ -f8)}

  # Taken from https://stackoverflow.com/a/70369688
  ARCH=$(arch | sed s/aarch64/arm64/ | sed s/x86_64/x64/)

  # Delete the old build
  [ -e MinecraftClient ] && rm -- MinecraftClient

  echo "Donwloading MinecraftClient for ${RELEASE_TAG}-${ARCH}"

  # Download the specified build or the latest one
  curl -L https://github.com/MCCTeam/Minecraft-Console-Client/releases/download/${RELEASE_TAG}/MinecraftClient-${RELEASE_TAG}-${MCC_PLATFORM:=linux}-${ARCH} --output MinecraftClient
fi

# Set Executable
chmod +x ./MinecraftClient

# Start the Client
./MinecraftClient
