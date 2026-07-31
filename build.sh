#!/bin/bash

set -e

RUNTIME="linux-x64"
CONFIG="Release"
PUBLISH_ARGS="-r $RUNTIME -c $CONFIG --self-contained -p:PublishSingleFile=true -p:DebugSymbols=false -p:DebugType=None"

rm -rf release
mkdir release

echo "Building vpnctld..."
dotnet publish src/vpnctld/vpnctld.csproj \
    $PUBLISH_ARGS \
    -o release

echo "Building vpnctl..."
dotnet publish src/vpnctl/vpnctl.csproj \
    $PUBLISH_ARGS \
    -o release

echo "Building vpnctl-setup..."
dotnet publish src/vpnctl-setup/vpnctl-setup.csproj \
    $PUBLISH_ARGS \
    -o release

chmod +x release/**

echo "Done!"