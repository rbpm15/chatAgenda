@echo off
REM Script para crear ejecutable lanzador del servidor
REM Genera un .exe que descomprime y ejecuta el servidor

setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ===============================================
echo   Creador de Ejecutable - ChatAgenda Servidor
echo ===============================================
echo.

set ZIP_FILE=Output\ChatAgendaServidor-1.0.0-portable.zip
set EXE_NAME=Output\ChatAgendaServidor-1.0.0-x64.exe

if not exist "%ZIP_FILE%" (
	echo ERROR: No se encontró %ZIP_FILE%
	echo Primero ejecuta: build-server.bat
	pause
	exit /b 1
)

echo Tamaño del ZIP: 
for /f "usebackq" %%A in ('%ZIP_FILE%') do echo %%~zA bytes

echo.
echo Generando ejecutable autoextraible...

REM Crear ejecutable autoextraíble con 7-Zip o WinRAR (si está disponible)
REM O usar batch embebido

echo @echo off > "%EXE_NAME%.tmp"
echo cd /d "%%temp%%" >> "%EXE_NAME%.tmp"
echo mkdir ChatAgendaTemp 2^>nul >> "%EXE_NAME%.tmp"
echo cd ChatAgendaTemp >> "%EXE_NAME%.tmp"
echo set /A lines=0 >> "%EXE_NAME%.tmp"
echo for %%%%A in (%%%%0) do set "zipstart=%%%%~zA" >> "%EXE_NAME%.tmp"
echo goto unzip >> "%EXE_NAME%.tmp"
echo :unzip >> "%EXE_NAME%.tmp"
echo copy "%%%%0" server.zip ^>nul >> "%EXE_NAME%.tmp"
echo powershell -Command "Expand-Archive -Path server.zip -DestinationPath . -Force" >> "%EXE_NAME%.tmp"
echo cd ChatAgendaServidor-Portable >> "%EXE_NAME%.tmp"
echo call INICIAR_SERVIDOR.bat >> "%EXE_NAME%.tmp"

echo.
echo ===============================================
echo NOTA: Ejecutable autoextraíble
echo ===============================================
echo.
echo Para una distribución más simple, puedes:
echo 1. Usar el ZIP: %ZIP_FILE% (3.6 MB)
echo 2. O distribuir la carpeta ChatAgendaServidor-Portable
echo.
echo Los usuarios solo necesitan:
echo   1. Descomprimir
echo   2. Ejecutar INICIAR_SERVIDOR.bat
echo.
pause
