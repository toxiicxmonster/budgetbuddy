@echo off
rem Starts Budget Buddy from wherever this folder lives (USB drive, desktop, etc.)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0app\server.ps1"
if errorlevel 1 pause
