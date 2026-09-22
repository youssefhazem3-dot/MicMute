@echo off
setlocal enabledelayedexpansion
title MicMute Diagnostic Runner
echo =======================================================
echo    MicMute Local Diagnostic Runner
echo =======================================================
echo.

:: Keep the active instance and its pending settings intact.
tasklist /FI "IMAGENAME eq MicMute.exe" | find /I "MicMute.exe" >nul
if not errorlevel 1 (
    echo [ERROR] MicMute is already running. Quit it from the tray before starting diagnostics.
    pause
    exit /b 2
)

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
