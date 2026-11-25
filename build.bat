@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
title ParaFaceLab - Build

set "GAME=%PARALIVES_DIR%"
if "%GAME%"=="" (
    for %%P in (
        "C:\Program Files (x86)\Steam\steamapps\common\Paralives"
        "C:\SteamLibrary\steamapps\common\Paralives"
        "D:\SteamLibrary\steamapps\common\Paralives"
        "E:\SteamLibrary\steamapps\common\Paralives"
        "F:\SteamLibrary\steamapps\common\Paralives"
        "G:\SteamLibrary\steamapps\common\Paralives"
    ) do (
        if "!GAME!"=="" if exist "%%~P\Paralives_Data\Managed\Paralives.dll" set "GAME=%%~P"
    )
)

if "%GAME%"=="" (
    echo [ERR] Paralives folder not found. Set PARALIVES_DIR env var and retry.
    exit /b 1
)

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "MANAGED=%GAME%\Paralives_Data\Managed"
set "OUT=%~dp0ParaFaceLab.dll"
set "LOG=%~dp0build.log"

echo ==== ParaFaceLab build ==== > "%LOG%"
echo GAME=%GAME% >> "%LOG%"

if not exist "%CSC%" (
    echo [ERR] Compiler not found: %CSC% >> "%LOG%"
    echo [ERR] Compiler not found.
    exit /b 1
)

set REFS=/r:"%MANAGED%\Paralives.dll" /r:"%GAME%\BepInEx\core\BepInEx.dll"
set REFS=!REFS! /r:"%GAME%\BepInEx\core\0Harmony.dll"
set REFS=!REFS! /r:"%MANAGED%\UnityEngine.dll"
set REFS=!REFS! /r:"%MANAGED%\UnityEngine.CoreModule.dll"
set REFS=!REFS! /r:"%MANAGED%\UnityEngine.IMGUIModule.dll"
set REFS=!REFS! /r:"%MANAGED%\UnityEngine.TextRenderingModule.dll"
set REFS=!REFS! /r:"%MANAGED%\UnityEngine.InputLegacyModule.dll"
set REFS=!REFS! /r:"%MANAGED%\Newtonsoft.Json.dll"
set REFS=!REFS! /r:"%MANAGED%\netstandard.dll"
set REFS=!REFS! /r:"%MANAGED%\System.Runtime.dll"
set REFS=!REFS! /r:"%MANAGED%\mscorlib.dll"
set REFS=!REFS! /r:"%MANAGED%\System.dll"
set REFS=!REFS! /r:"%MANAGED%\System.Core.dll"
set REFS=!REFS! /r:"%MANAGED%\System.Numerics.dll"

echo [1/2] Compiling ParaFaceLab.dll ...
"%CSC%" /nologo /noconfig /target:library /optimize+ /nostdlib+ /utf8output /out:"%OUT%" "%~dp0*.cs" !REFS! >> "%LOG%" 2>&1
if errorlevel 1 (
    echo [FAIL] Compile failed. See build.log:
    type "%LOG%"
    exit /b 1
)

echo [2/2] Installing to BepInEx\plugins ...
if not exist "%GAME%\BepInEx\plugins" mkdir "%GAME%\BepInEx\plugins"
copy /y "%OUT%" "%GAME%\BepInEx\plugins\ParaFaceLab.dll" >nul
if errorlevel 1 (
    echo [FAIL] Copy failed. Close Paralives and retry.
    exit /b 1
)

echo [OK] ParaFaceLab.dll built and installed!
exit /b 0
