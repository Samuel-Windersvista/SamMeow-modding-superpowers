@echo off
setlocal

set "game_root=%~dp0"
set "spt_dir=%game_root%SPT"
set "server_exe=SPT.Server.exe"
set "launcher_exe=SPT.Launcher.exe"

REM Start the server from SPT (CWD hard constraint)
start "" /D "%spt_dir%" "%spt_dir%\%server_exe%"

REM Health-poll the server until ready (max ~120s, then force launcher)
set /a tries=0
:wait_health
set /a tries+=1
if %tries% gtr 60 goto :launch
curl.exe -k -s --max-time 5 -o NUL -w "%%{http_code}" "https://127.0.0.1:6969/health" | findstr /C:"200" >nul
if errorlevel 1 (
    timeout /t 2 /nobreak >nul
    goto :wait_health
)

:launch
REM Server ready (or timed out) -> start the launcher
start "" /D "%spt_dir%" "%spt_dir%\%launcher_exe%"

endlocal
