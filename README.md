# BackgroundMute

Automatically mute programs when minimized and unmute them when opened again.

## Install

```
winget install BackgroundMute
```

Or download from [releases](https://github.com/Myst1024/BackgroundMute/releases).

## Usage

Run the application. Select which applications to control.  It automatically mutes audio from background processes while preserving sound from the active window.


## User Requirements

Windows 10 or later.

## Dev Requirements

Windows 10 or later

.NET SDK 10.0+

Inno Setup 6 (for installer builds)

Install Inno Setup:

```powershell
winget install --id JRSoftware.InnoSetup -e --accept-source-agreements --accept-package-agreements
```

## Build Installer

From the repository root:

```powershell
.\scripts\build-installer.ps1 -Version 1.0.0
```

This script:

1. Publishes a self-contained single-file app.

2. Builds an Inno Setup installer.

3. Prints the installer SHA256 hash.

Installer output:

```text
.\dist\BackgroundMute-Setup-1.0.0.exe
```

## Manual Publish (Optional)

```powershell
dotnet publish .\BackgroundMute.csproj -c Release
```

Published app output:

```text
.\bin\Release\net10.0-windows\win-x64\publish\BackgroundMute.exe
```

## Release And Winget Checklist

1. Build installer with the version you are releasing.

2. Create GitHub release tag `vX.Y.Z`.

3. Upload `BackgroundMute-Setup-X.Y.Z.exe` to the release assets.

4. Update `manifests/BackgroundMute.yaml`:

- `PackageVersion`

- `InstallerUrl`

- `InstallerSha256`

- `ReleaseNotesUrl`

5. Validate manifest:

```powershell
winget validate .\manifests\BackgroundMute.yaml
```

6. Submit manifest to `microsoft/winget-pkgs`.

## License

MIT
