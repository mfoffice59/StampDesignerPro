@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK not found. Install .NET 8 SDK.
  pause
  exit /b 1
)
dotnet publish src\StampDesignerPro\StampDesignerPro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\windows-x64
echo.
echo Done.
echo EXE:
echo publish\windows-x64\StampDesignerPro.exe
pause
