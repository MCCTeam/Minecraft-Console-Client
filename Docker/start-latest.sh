#!/bin/sh

cd /opt/data || exit 1

# Download latest version
curl -L https://ci.appveyor.com/api/buildjobs/51an4dvs204ak6tq/artifacts/MinecraftClient%2Fbin%2FRelease%2FMinecraftClient.exe --output MinecraftClient.exe

# Start Client
mono MinecraftClient.exe