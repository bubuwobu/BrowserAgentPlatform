@echo off
chcp 65001 >nul
title Reddit 正常启动
setlocal
cd /d "%~dp0\.."
echo [STEP] 正常启动 Reddit 项目...
dotnet run --project SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj
pause
