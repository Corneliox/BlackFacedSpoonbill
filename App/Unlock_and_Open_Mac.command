#!/bin/bash
# ==============================================================================
# Spoonbill AI Studio — 1-Click macOS Gatekeeper Unlock & Safe Launcher
# Automatically removes com.apple.quarantine and App Translocation locks
# for Apple Silicon (M1/M2/M3/M4) and Intel Macs.
# ==============================================================================

clear
echo "=================================================================="
echo "  Spoonbill AI Studio v4.1 — macOS Security Unlocker & Launcher"
echo "=================================================================="
echo ""

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DMG_APP="$SCRIPT_DIR/Spoonbill_AI_Studio.app"
DEST_APP="/Applications/Spoonbill_AI_Studio.app"

# 1. If running from inside the mounted DMG, copy to /Applications first so it is not read-only
if [ -d "$DMG_APP" ] && [[ "$SCRIPT_DIR" == /Volumes/* ]]; then
    echo "[1/3] Installing Spoonbill_AI_Studio.app to /Applications..."
    rm -rf "$DEST_APP" 2>/dev/null
    ditto "$DMG_APP" "$DEST_APP"
fi

TARGET_APP=""
if [ -d "$DEST_APP" ]; then
    TARGET_APP="$DEST_APP"
elif [ -d "$DMG_APP" ]; then
    TARGET_APP="$DMG_APP"
fi

if [ -z "$TARGET_APP" ]; then
    echo "[Error] Spoonbill_AI_Studio.app not found in /Applications or current folder."
    echo "Please drag Spoonbill_AI_Studio.app to the Applications folder first."
    read -p "Press [Enter] to exit..."
    exit 1
fi

echo "[2/3] Clearing macOS Gatekeeper quarantine attributes (com.apple.quarantine)..."
xattr -cr "$TARGET_APP" 2>/dev/null || sudo xattr -cr "$TARGET_APP"
codesign --force --deep --sign - "$TARGET_APP" 2>/dev/null || true

echo "[3/3] Launching Spoonbill AI Studio..."
open "$TARGET_APP"
echo ""
echo "Done! Spoonbill AI Studio is now unlocked and trusted on this Mac."
echo "You can now launch 'Spoonbill_AI_Studio' directly from Finder > Applications."
sleep 2
exit 0
