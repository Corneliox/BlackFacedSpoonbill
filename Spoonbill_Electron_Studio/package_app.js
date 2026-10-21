/**
 * Packaging Script for Spoonbill Electron Studio V4
 * Packages pre-compiled Electron runtime into standalone Windows distribution
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const ROOT_DIR = __dirname;
const ELECTRON_DIST = path.join(ROOT_DIR, 'node_modules', 'electron', 'dist');
const OUTPUT_DIR = path.join(ROOT_DIR, 'dist', 'Spoonbill_Studio_v4.0.0_Windows_x64');
const APP_DEST = path.join(OUTPUT_DIR, 'resources', 'app');

function copyRecursiveSync(src, dest) {
  const exists = fs.existsSync(src);
  const stats = exists && fs.statSync(src);
  const isDirectory = exists && stats.isDirectory();
  if (isDirectory) {
    if (!fs.existsSync(dest)) fs.mkdirSync(dest, { recursive: true });
    fs.readdirSync(src).forEach((childItemName) => {
      copyRecursiveSync(path.join(src, childItemName), path.join(dest, childItemName));
    });
  } else {
    fs.copyFileSync(src, dest);
  }
}

console.log('[Packager] Preparing distribution folder...');
if (fs.existsSync(OUTPUT_DIR)) {
  fs.rmSync(OUTPUT_DIR, { recursive: true, force: true });
}
fs.mkdirSync(OUTPUT_DIR, { recursive: true });

console.log('[Packager] Copying Electron Windows x64 binary runtime...');
const electronFiles = fs.readdirSync(ELECTRON_DIST);
for (const file of electronFiles) {
  const src = path.join(ELECTRON_DIST, file);
  if (file.toLowerCase() === 'electron.exe') {
    const dest = path.join(OUTPUT_DIR, 'Spoonbill_Studio.exe');
    console.log(`[Packager] Renaming executable -> Spoonbill_Studio.exe`);
    fs.copyFileSync(src, dest);
  } else {
    const dest = path.join(OUTPUT_DIR, file);
    copyRecursiveSync(src, dest);
  }
}

console.log('[Packager] Assembling app bundle in resources/app...');
fs.mkdirSync(APP_DEST, { recursive: true });

fs.copyFileSync(path.join(ROOT_DIR, 'package.json'), path.join(APP_DEST, 'package.json'));
fs.copyFileSync(path.join(ROOT_DIR, 'main.js'), path.join(APP_DEST, 'main.js'));
fs.copyFileSync(path.join(ROOT_DIR, 'preload.js'), path.join(APP_DEST, 'preload.js'));

console.log('[Packager] Copying renderer (Photoshop UI)...');
copyRecursiveSync(path.join(ROOT_DIR, 'renderer'), path.join(APP_DEST, 'renderer'));

console.log('[Packager] Copying backend (FastAPI + Unified Engine)...');
copyRecursiveSync(path.join(ROOT_DIR, 'backend'), path.join(APP_DEST, 'backend'));

if (fs.existsSync(path.join(ROOT_DIR, 'models'))) {
  console.log('[Packager] Copying bundled AI models...');
  copyRecursiveSync(path.join(ROOT_DIR, 'models'), path.join(APP_DEST, 'models'));
}

if (fs.existsSync(path.join(ROOT_DIR, 'requirements_lean.txt'))) {
  fs.copyFileSync(path.join(ROOT_DIR, 'requirements_lean.txt'), path.join(APP_DEST, 'requirements_lean.txt'));
  fs.copyFileSync(path.join(ROOT_DIR, 'requirements_lean.txt'), path.join(OUTPUT_DIR, 'requirements_lean.txt'));
}

// Add launcher scripts
console.log('[Packager] Creating helper scripts (shortcut creator, launcher)...');

const startBat = `@echo off
setlocal
cd /d "%~dp0"
title Spoonbill AI Studio V4
echo ===================================================================
echo   Spoonbill AI Studio V4 (Photoshop Warehouse Edition)
echo   Platalea minor Census & Research Suite
echo ===================================================================
echo [1/2] Verifying Python environment...
python --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Python not found on PATH! Please install Python 3.10+ with CUDA/ONNX.
    pause
    exit /b 1
)
echo [2/2] Launching Spoonbill Studio desktop interface...
start "" "%~dp0Spoonbill_Studio.exe"
exit
`;
fs.writeFileSync(path.join(OUTPUT_DIR, 'Start_Spoonbill_Studio.bat'), startBat);

const startVbs = `Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
strDir = fso.GetParentFolderName(WScript.ScriptFullName)
WshShell.Run chr(34) & strDir & "\\Spoonbill_Studio.exe" & chr(34), 0
Set WshShell = Nothing
`;
fs.writeFileSync(path.join(OUTPUT_DIR, 'Start_Spoonbill_Studio_Silent.vbs'), startVbs);

const installShortcutBat = `@echo off
setlocal
cd /d "%~dp0"
echo ===================================================================
echo   Creating Desktop Shortcut for Spoonbill AI Studio V4...
echo ===================================================================
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([System.IO.Path]::Combine([System.Environment]::GetFolderPath('Desktop'), 'Spoonbill AI Studio.lnk')); $s.TargetPath = '%~dp0Spoonbill_Studio.exe'; $s.WorkingDirectory = '%~dp0'; $s.Description = 'Black-Faced Spoonbill AI Studio V4 (Photoshop Edition)'; $s.Save(); Write-Host '[SUCCESS] Desktop shortcut created successfully!'"
echo.
echo You can now launch Spoonbill AI Studio directly from your Desktop.
pause
`;
fs.writeFileSync(path.join(OUTPUT_DIR, 'Install_Desktop_Shortcut.bat'), installShortcutBat);

console.log('[Packager] Creating Portable ZIP archive...');
const zipTarget = path.join(ROOT_DIR, 'dist', 'Spoonbill_Studio_v4.0.0_Windows_x64.zip');
try {
  if (fs.existsSync(zipTarget)) fs.unlinkSync(zipTarget);
  execSync(`powershell -NoProfile -Command "Compress-Archive -Path '${OUTPUT_DIR}\\*' -DestinationPath '${zipTarget}' -Force"`, { stdio: 'inherit' });
  console.log(`[Packager] ZIP created: ${zipTarget}`);
} catch (e) {
  console.warn('[Packager] Could not create zip archive automatically:', e.message);
}

console.log('[Packager] Build complete!');
console.log(`[Packager] Standalone app directory: ${OUTPUT_DIR}`);
