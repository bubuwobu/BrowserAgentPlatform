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

set "BROWSER="
if exist "C:\Program Files\Google\Chrome\Application\chrome.exe" set "BROWSER=C:\Program Files\Google\Chrome\Application\chrome.exe"
if not defined BROWSER if exist "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" set "BROWSER=C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
if not defined BROWSER if exist "%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" set "BROWSER=%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"
if not defined BROWSER if exist "C:\Program Files\Microsoft\Edge\Application\msedge.exe" set "BROWSER=C:\Program Files\Microsoft\Edge\Application\msedge.exe"
if not defined BROWSER if exist "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" set "BROWSER=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

if not defined BROWSER (
  echo [ERROR] Chrome or Edge not found.
  pause
  exit /b 1
)

echo [INFO] Browser: %BROWSER%
start "" "%BROWSER%" --remote-debugging-port=9222 --user-data-dir="D:\ChromeDebugProfile"

echo [INFO] Debug browser started.
echo [INFO] Check:
echo   http://127.0.0.1:9222/json/version
pause
