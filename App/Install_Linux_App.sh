#!/usr/bin/env bash
# =====================================================================
# Spoonbill AI Studio - 1-Click Desktop Installer for Linux (Ubuntu/Debian/Fedora/Arch)
# =====================================================================

cd "$(dirname "$0")" || exit 1

echo "====================================================================="
echo "       SPOONBILL AI STUDIO - 1-CLICK LINUX INSTALLER"
echo "====================================================================="
echo ""

# Check Python 3
if command -v python3 &>/dev/null; then
    PYTHON_CMD="python3"
elif command -v python &>/dev/null; then
    PYTHON_CMD="python"
else
    echo "❌ Error: Python 3 tidak ditemukan. Silakan pasang Python 3 (sudo apt install python3 python3-pip)"
    exit 1
fi

echo "[1/3] Memeriksa & Menginstal dependensi ringan..."
$PYTHON_CMD -m pip install -r requirements_lean.txt pywebview --quiet

echo "[2/3] Mendaftarkan Desktop Launcher & Menu Aplikasi..."
CURRENT_DIR="$(pwd)"
DESKTOP_ENTRY_DIR="$HOME/.local/share/applications"
mkdir -p "$DESKTOP_ENTRY_DIR"

DESKTOP_FILE="$DESKTOP_ENTRY_DIR/spoonbill-ai-studio.desktop"

cat << EOF > "$DESKTOP_FILE"
[Desktop Entry]
Version=1.0
Type=Application
Name=Spoonbill AI Studio
Comment=Black-faced Spoonbill AI Detection & Instance Segmentation Studio
Exec=$PYTHON_CMD "$CURRENT_DIR/desktop_app.py"
Path=$CURRENT_DIR
Terminal=false
Categories=Science;Graphics;Education;
StartupNotify=true
EOF

chmod +x "$DESKTOP_FILE"

# Salin juga ke ~/Desktop jika ada
if [ -d "$HOME/Desktop" ]; then
    cp "$DESKTOP_FILE" "$HOME/Desktop/"
    chmod +x "$HOME/Desktop/spoonbill-ai-studio.desktop" 2>/dev/null || true
fi

echo "[3/3] Instalasi Selesai 100%!"
echo "====================================================================="
echo "   SUKSES: Spoonbill AI Studio telah terpasang di Menu Aplikasi Linux & Desktop!"
echo "====================================================================="
echo ""

$PYTHON_CMD "$CURRENT_DIR/desktop_app.py"
