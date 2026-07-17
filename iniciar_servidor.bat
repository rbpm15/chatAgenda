@echo off
:: Cambiar al directorio donde está guardado este archivo .bat
cd /d "%~dp0"
title ChatAgenda Servidor

echo Iniciando ChatAgenda Servidor...
echo.

:: Ejecutar el servidor
chatAgenda.exe

pause
