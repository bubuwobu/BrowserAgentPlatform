@echo off
setlocal EnableExtensions

rem Resolve project root relative to this script location.
set "SCRIPT_DIR=%~dp0"
set "PROJECT_ROOT=%SCRIPT_DIR%..\..\"
cd /d "%PROJECT_ROOT%" || (
  echo [ERROR] Cannot change to project root: %PROJECT_ROOT%
  pause
  exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] dotnet not found in PATH.
  echo Install .NET SDK and reopen cmd, or add dotnet to PATH.
  echo Common path:
  echo   C:\Program Files\dotnet
  pause
  exit /b 1
)

echo [INFO] Importing Instagram login state...
dotnet run --project "SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj" -- --import-browser
pause
