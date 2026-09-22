@echo off
setlocal enabledelayedexpansion
title MicMute Diagnostic Runner
echo =======================================================
echo    MicMute Local Diagnostic Runner
echo =======================================================
echo.

:: 1. Terminate any running background MicMute instances to prevent file/mutex collisions
taskkill /f /im MicMute.exe >nul 2>&1

:: 2. Resolve local dotnet SDK
set "DOTNET_EXE=%~dp0.tools\dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" (
    set "DOTNET_EXE=dotnet"
)

echo [*] Compiling MicMute (Debug)...
"%DOTNET_EXE%" build "%~dp0MicMute.csproj" -c Debug --nologo
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. Please inspect errors above.
    pause
    exit /b 1
)

echo.
echo [*] Launching MicMute in Diagnostic Mode...
echo [*] Live audio and hotkey events will stream in real time.
echo =======================================================
echo.

:: 3. Launch application with diagnostic flags and wait for exit
start /wait "" "%~dp0bin\Debug\net8.0-windows\win-x64\MicMute.exe" --diagnostic --show

echo.
echo =======================================================
echo [INFO] MicMute application has closed.
pause
