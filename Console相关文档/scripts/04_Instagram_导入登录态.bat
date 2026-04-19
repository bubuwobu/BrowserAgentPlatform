@echo off
chcp 65001 >nul
title Instagram 导入登录态
setlocal
cd /d "%~dp0\.."
echo [STEP] 导入 Instagram 当前浏览器登录态...
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj -- --import-browser
echo.
echo [DONE] 如果控制台出现 Storage state exported / Cookies exported，则说明导入成功。
pause
