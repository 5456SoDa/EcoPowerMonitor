@echo off
title EcoPower Monitor Launcher
if exist "%~dp0publish_bin\EcoPower.exe" (
    start "" "%~dp0publish_bin\EcoPower.exe"
) else if exist "%~dp0publish\EcoPower.exe" (
    start "" "%~dp0publish\EcoPower.exe"
) else if exist "%~dp0bin\Release\net8.0-windows\EcoPower.exe" (
    start "" "%~dp0bin\Release\net8.0-windows\EcoPower.exe"
) else (
    echo กำลังคอมไพล์โปรแกรม...
    dotnet build -c Release
    start "" "%~dp0bin\Release\net8.0-windows\EcoPower.exe"
)

