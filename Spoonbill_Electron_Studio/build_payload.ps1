Add-Type -AssemblyName System.IO.Compression.FileSystem

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$studioDist = "dist\Spoonbill_Studio_v4.0.0_Windows_x64"
$portableZip = "dist\Spoonbill_Studio_v4.0.0_Windows_x64.zip"
$outExe = "dist\Spoonbill_Studio_v4.0.0_Setup.exe"
$setupCs = "Setup.cs"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

# 1. Ensure portable payload zip exists
if (!(Test-Path $portableZip)) {
    Write-Host "[1/2] Creating portable payload zip..."
    [System.IO.Compression.ZipFile]::CreateFromDirectory($studioDist, $portableZip, [System.IO.Compression.CompressionLevel]::Fastest, $false)
} else {
    $zipMb = [Math]::Round((Get-Item $portableZip).Length / (1024 * 1024), 2)
    Write-Host "[1/2] Using existing portable zip: $portableZip ($zipMb MB)"
}

# 2. Compile native Setup.exe using relative paths (immune to spaces in parent directories)
Write-Host "[2/2] Compiling native Setup.exe with embedded payload using csc.exe..."
if (Test-Path $outExe) { Remove-Item $outExe -Force }

& $csc /target:winexe "/out:$outExe" "/resource:$portableZip,Payload" /r:System.Windows.Forms.dll,System.Drawing.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll $setupCs

if (Test-Path $outExe) {
    $exeSize = [Math]::Round((Get-Item $outExe).Length / (1024 * 1024), 2)
    Write-Host "[SUCCESS] Setup.exe compiled successfully: $outExe ($exeSize MB)"
} else {
    Write-Error "[ERROR] Failed to compile Setup.exe"
}
