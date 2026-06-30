#!/bin/bash
set -e
ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJ="$ROOT/src/StampDesignerPro/StampDesignerPro.csproj"
if ! command -v dotnet >/dev/null 2>&1; then
  echo ".NET SDK not found. Install .NET 8 SDK."
  exit 1
fi
dotnet publish "$PROJ" -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$ROOT/publish/osx-arm64"
dotnet publish "$PROJ" -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$ROOT/publish/osx-x64"
echo "Done."
