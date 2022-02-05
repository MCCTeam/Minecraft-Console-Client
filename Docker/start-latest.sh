#!/bin/sh

cd /opt/data || exit 1

# Get latest release tag
RELEASE_TAG=$(curl -s -v https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest 2>&1 | grep -i location: | tr -d '\r' | cut -d/ -f8)

# Download latest version
curl -L https://github.com/MCCTeam/Minecraft-Console-Client/releases/download/${RELEASE_TAG}/MinecraftClient.exe --output MinecraftClient.exe

# Start Client
mono MinecraftClient.exe
