@echo off
REM ── Genera la versión descargable de Format Media Coder ─────────────────────
REM Doble clic en Windows. Produce en dist\ un FormatMediaCoder.exe autónomo
REM (self-contained: NO necesita tener .NET instalado para arrancar).
setlocal
cd /d "%~dp0"

echo.
echo === Publicando Format Media Coder (WinUI 3, self-contained, win-x64) ===
dotnet publish src/FormatMediaCoder.App -c Release -r win-x64 --self-contained true ^
  -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None ^
  -o dist
if errorlevel 1 (
  echo.
  echo ERROR al publicar. Revisa el mensaje de arriba.
  pause & exit /b 1
)

REM ── FFmpeg: el .exe busca bin\ffmpeg.exe junto a él. Si tienes los binarios
REM    (build con HAP) en bin\, se copian al paquete. Si no, la app usa el PATH.
if exist "bin\ffmpeg.exe" (
  echo Copiando FFmpeg (bin\) al paquete...
  if not exist "dist\bin" mkdir "dist\bin"
  copy /Y "bin\ffmpeg.exe"  "dist\bin\" >nul
  copy /Y "bin\ffprobe.exe" "dist\bin\" >nul
) else (
  echo AVISO: no hay bin\ffmpeg.exe. El paquete usara el FFmpeg del PATH.
  echo        Para bundlear HAP, pon ffmpeg.exe y ffprobe.exe en la carpeta bin\.
)

echo.
echo === LISTO ===
echo Tu version descargable esta en:  dist\FormatMediaCoder.exe
echo Subela como archivo adjunto a la Release v2.0.0 en GitHub.
echo (O comprime toda la carpeta dist\ en un .zip y sube el .zip.)
echo.
pause
