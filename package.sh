#!/bin/bash

set -e

echo "Building project..."
./build.sh

echo "Preparing package..."

rm -rf package
mkdir package

echo "Package ready!"