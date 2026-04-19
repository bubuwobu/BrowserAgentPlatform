@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

cd /d "%~dp0"

echo ==============================================
echo SocialAuto 登录态自动导入启动器（自动检测版）
echo ==============================================
echo.

if not exist "SocialAuto.Instagram.Console" (
  echo [ERROR] 当前目录下未找到 SocialAuto.Instagram.Console
  echo 请把本脚本放在 BrowserAgentPlatform 根目录后再运行。
  goto :end
)

if not exist "SocialAuto.Reddit.Console" (
  echo [ERROR] 当前目录下未找到 SocialAuto.Reddit.Console
  echo 请把本脚本放在 BrowserAgentPlatform 根目录后再运行。
  goto :end
)

set "BROWSER_EXE="
set "BROWSER_NAME="

if exist "%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" (
  set "BROWSER_EXE=%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"
  set "BROWSER_NAME=Chrome"
)
if not defined BROWSER_EXE if exist "C:\Program Files\Google\Chrome\Application\chrome.exe" (
  set "BROWSER_EXE=C:\Program Files\Google\Chrome\Application\chrome.exe"
  set "BROWSER_NAME=Chrome"
)
if not defined BROWSER_EXE if exist "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" (
  set "BROWSER_EXE=C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
  set "BROWSER_NAME=Chrome"
)
if not defined BROWSER_EXE if exist "%LOCALAPPDATA%\Microsoft\Edge\Application\msedge.exe" (
  set "BROWSER_EXE=%LOCALAPPDATA%\Microsoft\Edge\Application\msedge.exe"
  set "BROWSER_NAME=Edge"
)
if not defined BROWSER_EXE if exist "C:\Program Files\Microsoft\Edge\Application\msedge.exe" (
  set "BROWSER_EXE=C:\Program Files\Microsoft\Edge\Application\msedge.exe"
  set "BROWSER_NAME=Edge"
)
if not defined BROWSER_EXE if exist "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" (
  set "BROWSER_EXE=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
  set "BROWSER_NAME=Edge"
)

if not defined BROWSER_EXE (
  echo [ERROR] 未找到 Chrome 或 Edge。
  goto :end
)

echo 已找到浏览器：%BROWSER_NAME%
echo %BROWSER_EXE%
echo.
echo 请选择导入目标：
echo   1. Instagram
echo   2. Reddit
echo   3. 两个都导入
set /p CHOICE=请输入 1 / 2 / 3：

if "%CHOICE%"=="1" (
  set "MODE=instagram"
) else if "%CHOICE%"=="2" (
  set "MODE=reddit"
) else if "%CHOICE%"=="3" (
  set "MODE=both"
) else (
  echo [ERROR] 无效选择。
  goto :end
)

set "DEBUG_PORT=9222"
set "DEBUG_PROFILE=%TEMP%\SocialAutoChromeDebugProfile"

if not exist "%DEBUG_PROFILE%" mkdir "%DEBUG_PROFILE%" >nul 2>nul

echo.
echo 正在启动 %BROWSER_NAME% 调试模式...
start "" "%BROWSER_EXE%" --remote-debugging-port=%DEBUG_PORT% --user-data-dir="%DEBUG_PROFILE%"
timeout /t 3 /nobreak >nul

echo.
if /I "%MODE%"=="instagram" (
  start "" "https://www.instagram.com/"
  call :wait_and_import instagram "instagram.com" "SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj"
) else if /I "%MODE%"=="reddit" (
  start "" "https://www.reddit.com/"
  call :wait_and_import reddit "reddit.com" "SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj"
) else (
  start "" "https://www.instagram.com/"
  start "" "https://www.reddit.com/"
  call :wait_and_import instagram "instagram.com" "SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj"
  call :wait_and_import reddit "reddit.com" "SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj"
)

echo.
echo 所有导入流程已执行完成。
goto :end

:wait_and_import
set "PLATFORM=%~1"
set "TARGET_DOMAIN=%~2"
set "CSPROJ=%~3"
set "FOUND_FILE=%TEMP%\socialauto_%PLATFORM%_found.txt"
if exist "%FOUND_FILE%" del /f /q "%FOUND_FILE%" >nul 2>nul

echo.
echo [%PLATFORM%] 请在刚启动的浏览器中完成登录。
echo [%PLATFORM%] 检测到已打开 %TARGET_DOMAIN% 后，将自动开始导出。

:poll_loop
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='SilentlyContinue';" ^
  "$ok=$false;" ^
  "try { $tabs = Invoke-RestMethod -Uri 'http://127.0.0.1:%DEBUG_PORT%/json' -TimeoutSec 3;" ^
  "foreach($t in $tabs){ if($null -ne $t.url -and $t.url -match '%TARGET_DOMAIN%'){ $ok=$true; break } } } catch {} ;" ^
  "if($ok){ Set-Content -Path '%FOUND_FILE%' -Value 'ok' -Encoding ASCII }"

if not exist "%FOUND_FILE%" (
  timeout /t 2 /nobreak >nul
  goto :poll_loop
)

echo [%PLATFORM%] 已检测到目标站点标签页，开始导出登录态...
if not exist "%CSPROJ%" (
  echo [ERROR] 未找到项目文件：%CSPROJ%
  exit /b 1
)

dotnet run --project "%CSPROJ%" -- --import-browser
if errorlevel 1 (
  echo [ERROR] %PLATFORM% 导出失败。
  exit /b 1
)

echo [%PLATFORM%] 导出完成。
exit /b 0

:end
echo.
pause
endlocal
