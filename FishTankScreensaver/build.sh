#!/bin/bash
# Build and install the Fish Tank Screensaver
# Usage: ./build.sh [--install]

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo "Building Fish Tank Screensaver..."

xcodebuild \
    -project FishTankScreensaver.xcodeproj \
    -target FishTankScreensaver \
    -configuration Release \
    clean build \
    2>&1 | tail -20

BUILD_DIR=$(xcodebuild -project FishTankScreensaver.xcodeproj -target FishTankScreensaver -configuration Release -showBuildSettings 2>/dev/null | grep " BUILT_PRODUCTS_DIR" | sed 's/.*= //')
SAVER_PATH="$BUILD_DIR/FishTankScreensaver.saver"

if [ ! -d "$SAVER_PATH" ]; then
    echo "ERROR: Build product not found at $SAVER_PATH"
    exit 1
fi

echo ""
echo "Build successful: $SAVER_PATH"

if [ "$1" = "--install" ]; then
    INSTALL_DIR="$HOME/Library/Screen Savers"
    mkdir -p "$INSTALL_DIR"

    # Remove old version if exists
    if [ -d "$INSTALL_DIR/FishTankScreensaver.saver" ]; then
        echo "Removing old version..."
        rm -rf "$INSTALL_DIR/FishTankScreensaver.saver"
    fi

    echo "Installing to $INSTALL_DIR..."
    cp -R "$SAVER_PATH" "$INSTALL_DIR/"

    echo ""
    echo "Installed! Open System Settings > Screen Saver to select 'Fish Tank'."
    echo "You may need to close and reopen System Settings if it was already open."
else
    echo ""
    echo "To install, run:"
    echo "  ./build.sh --install"
    echo ""
    echo "Or manually copy:"
    echo "  cp -R \"$SAVER_PATH\" ~/Library/Screen\\ Savers/"
fi
