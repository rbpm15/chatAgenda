@echo off
chcp 65001 >nul 2>&1
title ChatAgenda - Desinstalador

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Ejecuta como Administrador
    pause
    exit /b 1
)

set TASK_NAME=ChatAgendaServerTask
set SERVICE_NAME=ChatAgendaServer
set INSTALL_DIR=C:\ChatAgenda
set EXE_NAME=chatAgenda.exe

echo.
echo Desinstalando ChatAgenda Server...
echo.

:: Detener y eliminar servicio (si existia en versiones anteriores)
sc query %SERVICE_NAME% >nul 2>&1
if %errorlevel% equ 0 (
    sc stop %SERVICE_NAME% >nul 2>&1
    timeout /t 3 /nobreak >nul
    sc delete %SERVICE_NAME% >nul 2>&1
)

:: Eliminar tarea programada
schtasks /query /tn "%TASK_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    schtasks /end /tn "%TASK_NAME%" >nul 2>&1
    schtasks /delete /tn "%TASK_NAME%" /f >nul 2>&1
    echo [OK] Tarea silenciosa de fondo eliminada
) else (
    echo [OK] La tarea silenciosa no estaba instalada
)

:: Matar procesos fantasma por si acaso
taskkill /F /IM %EXE_NAME% /T >nul 2>&1

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
echo   Para eliminarla permanentemente, borra la carpeta:
echo   %LOCALAPPDATA%\ChatAgenda
echo.
pause
