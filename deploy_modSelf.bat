@echo off
chcp 65001 >nul
setlocal EnableExtensions DisableDelayedExpansion

REM 脚本职责：只部署当前模组，不执行编译。需要编译时请先运行 compile_modSelf.bat。
set "DEPLOY_EXE=E:\aow\ModPack\xmlremove\bin\Release\net8.0-windows\xmlremove.exe"
set "SOURCE_MOD=E:\RimModDev\PhoenixRace\PhoenixRace"
set "TARGET_NAME=PhoenixRace"

if not exist "%DEPLOY_EXE%" (
    echo [ERROR] 统一部署程序不存在: %DEPLOY_EXE%
    pause
    exit /b 1
)

if not exist "%SOURCE_MOD%\About\About.xml" (
    echo [ERROR] 源模组缺少 About.xml: %SOURCE_MOD%
    pause
    exit /b 1
)

"%DEPLOY_EXE%" deploy --source "%SOURCE_MOD%" --target-name "%TARGET_NAME%"
set "DEPLOY_ERROR=%ERRORLEVEL%"

if not "%DEPLOY_ERROR%"=="0" (
    echo [ERROR] 部署失败，退出码: %DEPLOY_ERROR%
    pause
    exit /b %DEPLOY_ERROR%
)

REM 统一部署器尚未收录 Waylith Shader 目录，补齐本模组的 1.7 原生渲染资源。
robocopy "%SOURCE_MOD%\1.7\Shaders" "E:\steam\steamapps\common\RimWorld\Mods\%TARGET_NAME%\1.7\Shaders" /E /R:0 /W:0 /NFL /NDL /NJH /NJS
if errorlevel 8 (
    echo [ERROR] Shader 资源部署失败。
    pause
    exit /b 1
)

echo [SUCCESS] 部署完成: %TARGET_NAME%
pause
exit /b 0
