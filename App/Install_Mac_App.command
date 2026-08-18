#!/usr/bin/env bash
# =====================================================================
# Spoonbill AI Studio - 1-Click Desktop Installer for macOS
# =====================================================================

cd "$(dirname "$0")" || exit 1

echo "====================================================================="
echo "       SPOONBILL AI STUDIO - 1-CLICK macOS INSTALLER"
echo "====================================================================="
echo ""

# Check Python 3
if command -v python3 &>/dev/null; then
    PYTHON_CMD="python3"
elif command -v python &>/dev/null; then
    PYTHON_CMD="python"
else
    osascript -e 'display dialog "Python 3 tidak ditemukan. Silakan pasang Python 3 terlebih dahulu." buttons {"OK"} default button "OK" with icon stop'
    exit 1
fi

echo "[1/3] Memeriksa & Menginstal dependensi ringan..."
$PYTHON_CMD -m pip install -r requirements_lean.txt pywebview --quiet

echo "[2/3] Membuat Desktop Shortcut di macOS Desktop..."
CURRENT_DIR="$(pwd)"
DESKTOP_DIR="$HOME/Desktop"
APP_LAUNCHER="$DESKTOP_DIR/Spoonbill AI Studio.command"

cat << 'EOF' > "$APP_LAUNCHER"
#!/usr/bin/env bash
cd "__TARGET_DIR__" || exit 1
python3 desktop_app.py
EOF

# Ganti placeholder path
sed -i '' "s|__TARGET_DIR__|$CURRENT_DIR|g" "$APP_LAUNCHER"
chmod +x "$APP_LAUNCHER"

echo "[3/3] Instalasi Berhasil!"
echo "====================================================================="
echo "   SUKSES: Shortcut 'Spoonbill AI Studio.command' telah dibuat di Desktop Mac Anda!"
echo "====================================================================="

osascript -e 'display dialog "Instalasi Berhasil!\n\nIkon Spoonbill AI Studio telah siap di Desktop Mac Anda. Silakan klik dua kali untuk membuka aplikasi." buttons {"Buka Sekarang"} default button "Buka Sekarang" with icon note'

open "$APP_LAUNCHER"
