@echo off
chcp 65001 >nul
REM 仅构建当前维护的 1.7 程序集。
dotnet build "%~dp0PhoenixRace\1.7\Source\PhoenixRaceLib\PhoenixRaceLib.csproj" -c Release --nologo
exit /b %ERRORLEVEL%
