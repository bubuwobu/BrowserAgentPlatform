@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo [1/3] Searching Chrome...
set "CHROME="
if exist "C:\Program Files\Google\Chrome\Application\chrome.exe" set "CHROME=C:\Program Files\Google\Chrome\Application\chrome.exe"
if not defined CHROME if exist "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" set "CHROME=C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
if not defined CHROME if exist "%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" set "CHROME=%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"
if not defined CHROME (
  echo Chrome not found.
  echo Try Edge instead:
  echo "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --remote-debugging-port=9222 --user-data-dir="D:\ChromeDebugProfile"
  pause
  exit /b 1
)

echo Found: %CHROME%
echo [2/3] Starting Chrome in debug mode...
start "" "%CHROME%" --remote-debugging-port=9222 --user-data-dir="D:\ChromeDebugProfile"

echo [3/3] Waiting 3 seconds...
timeout /t 3 /nobreak >nul

echo Open this URL in any browser to verify debug mode:
echo http://127.0.0.1:9222/json/version
echo.
echo If you see JSON, Chrome debug mode started successfully.
echo Then log into Instagram or Reddit in that Chrome window.
pause
