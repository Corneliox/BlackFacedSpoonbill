#!/usr/bin/env bash
# =====================================================================
# Spoonbill AI Studio - Universal V3 Lean Launcher for macOS
# Compatible with macOS Intel (x86_64) & Apple Silicon (M1/M2/M3/M4)
# =====================================================================

cd "$(dirname "$0")" || exit 1

echo "====================================================================="
echo "       SPOONBILL AI STUDIO - UNIVERSAL V3 LEAN (macOS EDITION)"
echo "      Black-faced Spoonbill AI Instance Segmentation & Census"
echo "====================================================================="
echo ""

# Check Python 3
if command -v python3 &>/dev/null; then
    PYTHON_CMD="python3"
elif command -v python &>/dev/null; then
    PYTHON_CMD="python"
else
    echo "❌ Error: Python 3 is not found. Please install Python 3 (e.g. from brew or python.org)."
    read -p "Press Enter to exit..."
    exit 1
fi

echo "[1/3] Checking environment & lean requirements..."
$PYTHON_CMD -m pip install -r requirements_lean.txt --quiet

echo "[2/3] Opening browser at http://127.0.0.1:8080 ..."
(sleep 2 && open "http://127.0.0.1:8080") &

echo "[3/3] Starting backend server on http://127.0.0.1:8080 ..."
echo "Press Ctrl+C in this Terminal window to stop the application."
echo "====================================================================="
echo ""

$PYTHON_CMD app.py
