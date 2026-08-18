@echo off
title Spoonbill AI Studio - Universal V3 Lean Launcher
color 0B
cls

echo =====================================================================
echo           SPOONBILL AI STUDIO - UNIVERSAL V3 LEAN LAUNCHER
echo      Black-faced Spoonbill AI Instance Segmentation & Census
echo =====================================================================
echo.

cd /d "%~dp0"

echo [1/3] Checking environment & lean dependencies...
python -m pip install -r requirements_lean.txt --quiet --no-warn-script-location

echo [2/3] Launching Spoonbill Studio local backend server...
start "" cmd /c "timeout /t 2 >nul & start http://127.0.0.1:8080"

echo [3/3] Server active at: http://127.0.0.1:8080
echo Press Ctrl+C in this terminal to stop the application.
echo =====================================================================
echo.

python app.py
pause
