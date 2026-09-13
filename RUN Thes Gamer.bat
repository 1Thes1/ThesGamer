@echo off
cd /d "%~dp0"
if exist "dist\ThesGamer.exe" (
  start "" "dist\ThesGamer.exe"
) else if exist "ThesGamer\bin\Release\net8.0-windows\ThesGamer.exe" (
  start "" "ThesGamer\bin\Release\net8.0-windows\ThesGamer.exe"
) else (
  echo Build first: dotnet build ThesGamer\ThesGamer.csproj -c Release
  pause
)
