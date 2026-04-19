Place these BAT files under:
BrowserAgentPlatform\Console相关文档\scripts

These versions avoid Chinese output and use relative paths.

If you see "dotnet not found in PATH", install .NET SDK or add:
C:\Program Files\dotnet
to PATH, then reopen terminal.

Recommended daily flow:
1) 01_Start_Chrome_Debug.bat
2) Login manually in that browser
3) 02_Reddit_Import_Login_State.bat or 04_Instagram_Import_Login_State.bat
4) Later use 03_Reddit_Run.bat or 05_Instagram_Run.bat
