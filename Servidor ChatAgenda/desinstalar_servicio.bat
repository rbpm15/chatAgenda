@echo off
chcp 65001 >nul 2>&1
title ChatAgenda - Desinstalador

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Ejecuta como Administrador
    pause
    exit /b 1
)

set SERVICE_NAME=ChatAgendaServer
set INSTALL_DIR=C:\ChatAgenda

echo.
echo Desinstalando ChatAgenda Server...
echo.

:: Detener servicio
sc query %SERVICE_NAME% >nul 2>&1
if %errorlevel% equ 0 (
    sc stop %SERVICE_NAME% >nul 2>&1
    timeout /t 3 /nobreak >nul
    sc delete %SERVICE_NAME%
    echo [OK] Servicio eliminado
) else (
    echo [OK] El servicio no estaba instalado
)

:: Eliminar regla de firewall
netsh advfirewall firewall delete rule name="ChatAgenda Server" >nul 2>&1
echo [OK] Regla de firewall eliminada

:: Eliminar directorio de instalación
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
    echo [OK] Directorio de instalacion eliminado
)

echo.
echo Desinstalacion completada.
echo NOTA: La base de datos NO fue eliminada.
echo   Para eliminarla: elimina la carpeta %%LOCALAPPDATA%%\ChatAgenda
echo.
pause
