@echo off
chcp 65001 >nul
title Instagram 正常启动
setlocal
cd /d "%~dp0\.."
echo [STEP] 正常启动 Instagram 项目...
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj
pause
