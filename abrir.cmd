@echo off
REM ── Lanzador de Format Media Coder ──────────────────────────────────────────
REM Doble clic para abrir la app. Funciona desde cualquier sitio: se coloca
REM en la carpeta del script (%~dp0) y arranca el proyecto WPF.
cd /d "%~dp0"
echo Arrancando Format Media Coder...
dotnet run --project src/FormatMediaCoder.App -c Release
if errorlevel 1 (
  echo.
  echo Hubo un error al arrancar. Revisa el mensaje de arriba.
  pause
)
