@echo off
setlocal
title Patan Explorer
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deployment\Run-PatanExplorer.ps1" %*
set "exitCode=%ERRORLEVEL%"
if not "%exitCode%"=="0" (
    echo.
    echo Press any key to close this window.
    pause >nul
)
exit /b %exitCode%
