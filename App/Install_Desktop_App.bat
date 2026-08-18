@echo off
title Spoonbill AI Studio - Desktop Installer
color 0A
cls

echo =====================================================================
echo            SPOONBILL AI STUDIO - 1-CLICK DESKTOP INSTALLER
echo      Black-faced Spoonbill AI Instance Segmentation & Census
echo =====================================================================
echo.

cd /d "%~dp0"

echo [1/3] Memeriksa & Menginstal dependensi ringan...
python -m pip install -r requirements_lean.txt pywebview --quiet --no-warn-script-location

echo [2/3] Membuat Desktop Shortcut di Layar Desktop Anda...

set SCRIPT_DIR=%~dp0
set VBS_LAUNCHER=%SCRIPT_DIR%launch_silent.vbs
set SHORTCUT_PATH=%USERPROFILE%\Desktop\Spoonbill AI Studio.lnk

:: Buat silent VBScript launcher agar tidak ada jendela CMD hitam yang mengganggu
(
echo Set WshShell = CreateObject^("WScript.Shell"^)
echo WshShell.CurrentDirectory = "%SCRIPT_DIR%"
echo WshShell.Run "pythonw desktop_app.py", 0, False
) > "%VBS_LAUNCHER%"

:: Buat Shortcut .lnk di Desktop via PowerShell
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT_PATH%'); $Shortcut.TargetPath = 'wscript.exe'; $Shortcut.Arguments = '\"%VBS_LAUNCHER%\"'; $Shortcut.WorkingDirectory = '%SCRIPT_DIR%'; $Shortcut.Description = 'Spoonbill AI Studio - Universal Edition'; $Shortcut.Save()"

echo [3/3] Instalasi Selesai 100%!
echo.
echo =====================================================================
echo   SUKSES: Ikon 'Spoonbill AI Studio' telah berhasil dibuat di Desktop!
echo   Anda dapat langsung membukanya kapan saja dari Desktop Anda.
echo =====================================================================
echo.

powershell -Command "[System.Windows.Forms.MessageBox]::Show('Instalasi Berhasil!\n\nIkon Spoonbill AI Studio telah siap di Desktop Anda. Silakan klik dua kali ikon di Desktop untuk mulai.', 'Spoonbill AI Studio', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)" 2>nul

echo Menjalankan aplikasi untuk pertama kali...
start "" "%SHORTCUT_PATH%"

exit
