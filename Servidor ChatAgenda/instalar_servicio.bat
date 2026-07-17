@echo off
chcp 65001 >nul 2>&1
title ChatAgenda - Instalador de Servidor

:: Verificar permisos de administrador
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ===================================================
    echo   ERROR: Ejecuta como Administrador
    echo   Click derecho - Ejecutar como administrador
    echo ===================================================
    echo.
    pause
    exit /b 1
)

echo.
echo ===================================================
echo        ChatAgenda Server - Instalador Final
echo ===================================================
echo.

set TASK_NAME=ChatAgendaServerTask
set SERVICE_NAME=ChatAgendaServer
set INSTALL_DIR=C:\ChatAgenda
set EXE_NAME=chatAgenda.exe
set PORT=5002

echo [1/6] Limpiando instalaciones anteriores...
sc query %SERVICE_NAME% >nul 2>&1
if %errorlevel% equ 0 (
    sc stop %SERVICE_NAME% >nul 2>&1
    timeout /t 3 /nobreak >nul
    sc delete %SERVICE_NAME% >nul 2>&1
)
schtasks /query /tn "%TASK_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    schtasks /end /tn "%TASK_NAME%" >nul 2>&1
    schtasks /delete /tn "%TASK_NAME%" /f >nul 2>&1
)
:: Matar procesos fantasma por si acaso
taskkill /F /IM %EXE_NAME% /T >nul 2>&1

echo.
echo [2/6] Copiando archivos del servidor...
set SCRIPT_DIR=%~dp0
if not exist "%SCRIPT_DIR%%EXE_NAME%" (
    echo [ERROR] No se encontro %EXE_NAME%
    pause
    exit /b 1
)
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
xcopy "%SCRIPT_DIR%*" "%INSTALL_DIR%\" /E /Y /I /Q >nul 2>&1

echo.
echo [3/6] Preparando directorio de datos...
set DATA_DIR=%LOCALAPPDATA%\ChatAgenda
if not exist "%DATA_DIR%" mkdir "%DATA_DIR%"
if not exist "%DATA_DIR%\uploads" mkdir "%DATA_DIR%\uploads"

echo.
echo [4/6] Configurando Tarea Programada (Servidor de fondo)...
powershell -ExecutionPolicy Bypass -Command "$action = New-ScheduledTaskAction -Execute '%INSTALL_DIR%\%EXE_NAME%' -Argument '--urls http://0.0.0.0:%PORT%' -WorkingDirectory '%INSTALL_DIR%'; $trigger = New-ScheduledTaskTrigger -AtStartup; $principal = New-ScheduledTaskPrincipal -UserId 'NT AUTHORITY\SYSTEM' -LogonType ServiceAccount -RunLevel Highest; $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -DontStopOnIdleEnd -ExecutionTimeLimit 0; Register-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings -TaskName '%TASK_NAME%' -Description 'ChatAgenda Server' -Force;" >nul 2>&1

echo.
echo [5/6] Configurando firewall...
netsh advfirewall firewall delete rule name="ChatAgenda Server" >nul 2>&1
netsh advfirewall firewall add rule name="ChatAgenda Server" dir=in action=allow protocol=TCP localport=%PORT% >nul 2>&1

echo.
echo [6/6] Iniciando servidor...
schtasks /run /tn "%TASK_NAME%" >nul 2>&1
timeout /t 3 /nobreak >nul

echo.
echo ===================================================
echo   Instalacion completada exitosamente!
echo ===================================================
echo.
echo   URL local:     http://localhost:%PORT%
echo   URL en red:    http://TU_IP:%PORT%
echo.
echo   El servidor se ejecutara automaticamente
echo   cada vez que enciendas la PC de forma invisible.
echo.
pause
