@echo off
where pwsh >nul 2>nul
if %errorlevel%==0 (
  pwsh -NoProfile -File "%~dp0StartLab.ps1"
) else (
  powershell -NoProfile -File "%~dp0StartLab.ps1"
)
pause
