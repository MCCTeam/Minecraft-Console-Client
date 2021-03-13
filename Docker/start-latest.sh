#!/bin/sh

cd /opt/data || exit 1

# Get latest Job ID
JOB_ID=$(curl -L https://ci.appveyor.com/api/projects/ORelio/minecraft-console-client | jq -r .build.jobs[0].jobId)

# Download latest version
curl -L https://ci.appveyor.com/api/buildjobs/"$JOB_ID"/artifacts/MinecraftClient%2Fbin%2FRelease%2FMinecraftClient.exe --output MinecraftClient.exe

# Start Client
mono MinecraftClient.exe