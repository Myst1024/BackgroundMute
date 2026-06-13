# BackgroundMute

Mute background applications while keeping foreground audio active.

## Install

```
winget install BackgroundMute
```

Or download from [releases](https://github.com/Myst1024/BackgroundMute/releases).

## Usage

Run the application. Select which applications to control.  It automatically mutes audio from background processes while preserving sound from the active window.

## Requirements

Windows 10 or later.

## Build Release Artifact

```
dotnet publish .\BackgroundMute.csproj -c Release
```

The publish output is self-contained and does not require a separate .NET runtime install.

## License

MIT
