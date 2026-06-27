@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK was not found.
  echo Install .NET 8 SDK from Microsoft:
  echo https://dotnet.microsoft.com/download
  pause
  exit /b 1
)

dotnet publish StampDesigner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true

echo.
echo Done.
echo EXE location:
echo bin\Release\net8.0-windows\win-x64\publish\StampDesigner.exe
pause
