@echo off
REM Requires .NET 8 SDK. NuGet packages restore automatically on first run.
cd /d "%~dp0"
dotnet run --project src\IdleQuest.API
