@echo off
setlocal
cd /d "%~dp0src\ChungChiBlockchain.Web"
if errorlevel 1 goto fail
where dotnet >nul 2>nul
if errorlevel 1 goto fail
echo Mo trinh duyet tai http://localhost:5087 sau khi ung dung khoi dong.
dotnet run --launch-profile http
set "result=%errorlevel%"
pause
exit /b %result%
:fail
echo Khong tim thay thu muc ung dung hoac dotnet. Can .NET SDK 10.
pause
exit /b 1
