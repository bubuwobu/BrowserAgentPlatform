@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo Select platform:
echo 1. Instagram
echo 2. Reddit
set /p CHOICE=Enter 1 or 2: 

where dotnet >nul 2>nul
if errorlevel 1 (
  echo dotnet not found in PATH.
  pause
  exit /b 1
)

if "%CHOICE%"=="1" (
  if not exist ".\SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj" (
    echo Instagram project not found. Put this bat in BrowserAgentPlatform root.
    pause
    exit /b 1
  )
  echo Importing Instagram login state...
  dotnet run --project ".\SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj" -- --import-browser
  pause
  exit /b %errorlevel%
)

if "%CHOICE%"=="2" (
  if not exist ".\SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj" (
    echo Reddit project not found. Put this bat in BrowserAgentPlatform root.
    pause
    exit /b 1
  )
  echo Importing Reddit login state...
  dotnet run --project ".\SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj" -- --import-browser
  pause
  exit /b %errorlevel%
)

echo Invalid choice.
pause
exit /b 1
