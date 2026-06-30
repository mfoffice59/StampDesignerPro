# Stamp Designer Pro

Cross-platform stamp designer for Windows and macOS.

## Status

**2.0 Alpha 1**

This repository is the new clean codebase for Stamp Designer Pro 2.0.

The old WinForms prototype is not used as the main architecture anymore.
The new version is based on:

- Avalonia UI
- .NET 8
- Skia-based rendering
- external symbol/template libraries
- future AI-assisted stamp generation

## Current Alpha 1 features

- cross-platform Avalonia project structure
- basic stamp project model
- basic circular stamp preview
- double-ring mode
- blue-band mode
- editable main texts and key geometry parameters
- external libraries folder structure
- GitHub Actions for Windows and macOS builds
- roadmap for AI-assisted editing

## Roadmap

- 2.0 Alpha 2 - logo layer, PNG import, project save/load, PNG export
- 2.0 Beta 1 - symbols and template libraries
- 2.0 RC - AI assistant and stamp reconstruction workflow
- 2.0 Stable - production release

## Build locally

### Windows

```cmd
build-windows-x64.bat
```

### macOS

```bash
chmod +x build-macos.sh
./build-macos.sh
```

## GitHub releases

The repository includes GitHub Actions workflow:

```text
.github/workflows/build-release.yml
```

It builds:

- Windows x64
- macOS Apple Silicon
- macOS Intel
