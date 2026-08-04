#!/bin/bash

set -e

RUNTIME=${1:-linux-x64}
CONFIG="Release"
PUBLISH_ARGS="-r $RUNTIME -c $CONFIG --self-contained -p:PublishSingleFile=true -p:DebugSymbols=false -p:DebugType=None"

rm -rf release/$RUNTIME
mkdir release/$RUNTIME

echo "Building for $RUNTIME..."

if [ $RUNTIME == "linux-x64" ]; then
echo "Building vpnctld..."
dotnet publish src/vpnctld/vpnctld.csproj \
    $PUBLISH_ARGS \
    -o release/$RUNTIME

echo "Building vpnctl-setup..."
dotnet publish src/vpnctl-setup/vpnctl-setup.csproj \
    $PUBLISH_ARGS \
    -o release/$RUNTIME
fi

echo "Building vpnctl..."
dotnet publish src/vpnctl/vpnctl.csproj \
    $PUBLISH_ARGS \
    -o release/$RUNTIME

chmod +x release/**

echo "Done!"