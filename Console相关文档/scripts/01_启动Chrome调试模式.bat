@echo off
chcp 65001 >nul
title 启动 Chrome 调试模式
setlocal

set ROOT=%~dp0
set CHROME=
set EDGE=

if exist "C:\Program Files\Google\Chrome\Application\chrome.exe" set CHROME=C:\Program Files\Google\Chrome\Application\chrome.exe
if not defined CHROME if exist "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" set CHROME=C:\Program Files (x86)\Google\Chrome\Application\chrome.exe
if not defined CHROME if exist "%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" set CHROME=%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe

if exist "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" set EDGE=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe
if not defined EDGE if exist "C:\Program Files\Microsoft\Edge\Application\msedge.exe" set EDGE=C:\Program Files\Microsoft\Edge\Application\msedge.exe

if defined CHROME (
    echo [INFO] 使用 Chrome 启动调试模式...
    start "" "%CHROME%" --remote-debugging-port=9222 --user-data-dir="D:\ChromeDebugProfile"
    echo [OK] Chrome 已尝试启动。
    echo [CHECK] 请访问 http://127.0.0.1:9222/json/version
    pause
    exit /b 0
)

if defined EDGE (
    echo [INFO] 未找到 Chrome，改用 Edge 启动调试模式...
    start "" "%EDGE%" --remote-debugging-port=9222 --user-data-dir="D:\EdgeDebugProfile"
    echo [OK] Edge 已尝试启动。
    echo [CHECK] 请访问 http://127.0.0.1:9222/json/version
    pause
    exit /b 0
)

echo [ERROR] 未找到 Chrome 或 Edge。
pause
exit /b 1
