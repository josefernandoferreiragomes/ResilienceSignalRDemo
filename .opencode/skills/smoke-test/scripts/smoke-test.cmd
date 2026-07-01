@echo off
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0smoke-test.ps1"
if %errorlevel% neq 0 exit /b %errorlevel%
