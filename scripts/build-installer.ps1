param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Get-Process BackgroundMute -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item .\bin\Release\net10.0-windows\win-x64\publish -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Publishing app..."
dotnet publish .\BackgroundMute.csproj -c Release

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $isccCandidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe"
    )
    $isccPath = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $isccPath) {
        throw "Inno Setup compiler (iscc) not found. Install it with: winget install --id JRSoftware.InnoSetup -e --accept-source-agreements --accept-package-agreements"
    }
}
else {
    $isccPath = $iscc.Source
}

Write-Host "Building installer..."
& $isccPath .\installer\BackgroundMute.iss "/DMyAppVersion=$Version"

$installerPath = ".\dist\BackgroundMute-Setup-$Version.exe"
if (-not (Test-Path $installerPath)) {
    throw "Installer was not produced at $installerPath"
}

$hash = (Get-FileHash $installerPath -Algorithm SHA256).Hash
Write-Host "Installer: $installerPath"
Write-Host "SHA256:   $hash"
