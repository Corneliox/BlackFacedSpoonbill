@echo off
title Spoonbill AI Studio - Electron Edition V4
cd /d "%~dp0"
echo =====================================================================
echo   SPOONBILL AI STUDIO - ELECTRON DESKTOP EDITION (V4)
echo   Unified ONNX + PyTorch CoordAtt @ P3 + SAHI + Grad-CAM + Benchmark
echo =====================================================================
if not exist "node_modules\electron" (
    echo [INFO] Installing Electron runtime...
    call npm install
)
echo [INFO] Launching Native Electron Desktop Window...
call npm start
