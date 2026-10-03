@echo off
chcp 65001 >nul
title EcoPower Portable Publisher
echo ========================================================
echo   EcoPower Monitor - Portable Single-File Builder
echo ========================================================
echo.
echo หมายเหตุ: หากเปิดโปรแกรม EcoPower.exe อยู่ กรุณาปิดโปรแกรมก่อนกดสร้างไฟล์
echo.
dotnet build-server shutdown >nul 2>&1
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:UseSharedCompilation=false --self-contained true -o ./publish
if %ERRORLEVEL% equ 0 (
    copy /y app.ico .\publish\app.ico >nul 2>&1
    echo.
    echo ========================================================
    echo   [สำเร็จ] ไฟล์โปรแกรมพร้อมใช้งานอยู่ที่: ./publish/EcoPower.exe
    echo ========================================================
) else (
    echo.
    echo [ผิดพลาด] ไม่สามารถเขียนไฟล์ได้ (หากกำลังเปิด EcoPower.exe อยู่ ให้ปิดโปรแกรมก่อนแล้วลองใหม่ครับ)
)
pause
