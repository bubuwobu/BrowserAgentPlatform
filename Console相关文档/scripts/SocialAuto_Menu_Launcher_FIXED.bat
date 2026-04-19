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

:menu
cls
echo ======================================
echo         SocialAuto Menu
echo ======================================
echo 1. Start Chrome Debug
echo 2. Import Reddit Login State
echo 3. Run Reddit
echo 4. Import Instagram Login State
echo 5. Run Instagram
echo 0. Exit
echo.
set /p CHOICE=Select: 

if "%CHOICE%"=="1" call "%~dp001_Start_Chrome_Debug.bat" & goto menu
if "%CHOICE%"=="2" call "%~dp002_Reddit_Import_Login_State.bat" & goto menu
if "%CHOICE%"=="3" call "%~dp003_Reddit_Run.bat" & goto menu
if "%CHOICE%"=="4" call "%~dp004_Instagram_Import_Login_State.bat" & goto menu
if "%CHOICE%"=="5" call "%~dp005_Instagram_Run.bat" & goto menu
if "%CHOICE%"=="0" exit /b 0

echo [ERROR] Invalid choice.
pause
goto menu
