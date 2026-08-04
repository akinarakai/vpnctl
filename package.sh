#!/bin/bash

set -e

RUNTIME=${1:-linux-x64}

echo "Building project..."
./build.sh $RUNTIME

echo "Preparing package..."

mkdir -p package

echo "Creating archive..."

cd package

zip -r $RUNTIME.zip ../release/$RUNTIME

cd ..

echo "Package ready: package/$RUNTIME.zip"