Add-Type -AssemblyName System.IO.Compression.FileSystem

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$studioDist = "dist\Spoonbill_Studio_v4.1.0_Experimental_Windows_x64"
$portableZip = "dist\Spoonbill_Studio_v4.1.0_Experimental_Windows_x64.zip"
$outExe = "dist\Spoonbill_Studio_v4.1.0_Experimental_Setup.exe"
$setupCs = "Setup.cs"
$uninstallCs = "Uninstall.cs"
$uninstallExe = "$studioDist\Uninstall.exe"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

# 1. Compile native NVIDIA-styled Uninstaller into distribution folder
Write-Host "[1/3] Compiling native NVIDIA-styled Uninstaller..."
& $csc /target:winexe "/out:$uninstallExe" /r:System.Windows.Forms.dll,System.Drawing.dll $uninstallCs

if (!(Test-Path $uninstallExe)) {
    Write-Error "[ERROR] Failed to compile Uninstall.exe"
    exit 1
}
Write-Host "[SUCCESS] Uninstaller compiled: $uninstallExe"

# 2. Package into portable distribution zip
Write-Host "[2/3] Updating portable payload zip..."
if (Test-Path $portableZip) {
    Remove-Item $portableZip -Force
}
[System.IO.Compression.ZipFile]::CreateFromDirectory($studioDist, $portableZip, [System.IO.Compression.CompressionLevel]::Fastest, $false)
$zipMb = [Math]::Round((Get-Item $portableZip).Length / (1024 * 1024), 2)
Write-Host "[SUCCESS] Portable zip created: $portableZip ($zipMb MB)"

# 3. Compile native NVIDIA-styled Setup.exe with embedded payload
Write-Host "[3/3] Compiling native NVIDIA-styled Setup.exe with embedded payload using csc.exe..."
if (Test-Path $outExe) { Remove-Item $outExe -Force }

& $csc /target:winexe "/out:$outExe" "/resource:$portableZip,Payload" /r:System.Windows.Forms.dll,System.Drawing.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll $setupCs

if (Test-Path $outExe) {
    $exeSize = [Math]::Round((Get-Item $outExe).Length / (1024 * 1024), 2)
    Write-Host "[SUCCESS] Setup.exe compiled successfully: $outExe ($exeSize MB)"
} else {
    Write-Error "[ERROR] Failed to compile Setup.exe"
    exit 1
}
