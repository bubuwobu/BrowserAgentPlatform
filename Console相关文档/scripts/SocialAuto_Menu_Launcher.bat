@echo off
setlocal enabledelayedexpansion
cd /d "D:\Project\桌面app\web+客户端agent\v8\BrowserAgentPlatform"

:menu
cls
echo ==========================================
echo         SocialAuto 总控菜单启动器
echo ==========================================
echo.
echo 1. 启动 Chrome 调试模式
echo 2. Reddit 导入登录态
echo 3. Reddit 正常启动
echo 4. Instagram 导入登录态
echo 5. Instagram 正常启动
echo 0. 退出
echo.
set /p choice=请输入选项: 

if "%choice%"=="1" goto chrome
if "%choice%"=="2" goto reddit_import
if "%choice%"=="3" goto reddit_run
if "%choice%"=="4" goto ins_import
if "%choice%"=="5" goto ins_run
if "%choice%"=="0" goto end

echo.
echo [ERROR] 无效选项，请重新输入。
pause
goto menu

:chrome
set "CHROME_PATH="
if exist "C:\Program Files\Google\Chrome\Application\chrome.exe" set "CHROME_PATH=C:\Program Files\Google\Chrome\Application\chrome.exe"
if not defined CHROME_PATH if exist "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" set "CHROME_PATH=C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
if not defined CHROME_PATH if exist "%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" set "CHROME_PATH=%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"
if not defined CHROME_PATH if exist "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" set "CHROME_PATH=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
if not defined CHROME_PATH if exist "C:\Program Files\Microsoft\Edge\Application\msedge.exe" set "CHROME_PATH=C:\Program Files\Microsoft\Edge\Application\msedge.exe"

if not defined CHROME_PATH (
    echo [ERROR] 未找到 Chrome 或 Edge。
    pause
    goto menu
)

echo [INFO] 浏览器路径: %CHROME_PATH%
echo [INFO] 正在以调试模式启动浏览器...
start "" "%CHROME_PATH%" --remote-debugging-port=9222 --user-data-dir="D:\ChromeDebugProfile"
echo.
echo [INFO] 已启动调试浏览器，请在其中手工登录 Reddit 或 Instagram。
pause
goto menu

:reddit_import
echo [INFO] 正在导入 Reddit 登录态...
dotnet run --project SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj -- --import-browser
pause
goto menu

:reddit_run
echo [INFO] 正在正常启动 Reddit 项目...
dotnet run --project SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj
pause
goto menu

:ins_import
echo [INFO] 正在导入 Instagram 登录态...
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj -- --import-browser
pause
goto menu

:ins_run
echo [INFO] 正在正常启动 Instagram 项目...
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj
pause
goto menu

:end
exit /b 0
