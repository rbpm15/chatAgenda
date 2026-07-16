@echo off
REM Script para compilar el cliente WPF y crear instalador InnoSetup
REM Solo modifica Client.WPF - NO toca el servidor

setlocal enabledelayedexpansion
cd /d "%~dp0.."

echo ===============================================
echo ChatAgenda - Compilador de Cliente WPF
echo ===============================================
echo.

set PROJECT_PATH=Client.WPF
set CONFIG=Release
set PLATFORM=x64

echo [1/4] Limpiando compilaciones previas...
if exist "%PROJECT_PATH%\bin\%CONFIG%" rmdir /s /q "%PROJECT_PATH%\bin\%CONFIG%" 2>nul
if exist "%PROJECT_PATH%\obj" rmdir /s /q "%PROJECT_PATH%\obj" 2>nul

echo.
echo [2/4] Compilando Cliente WPF en Release x64...
cd "%PROJECT_PATH%"
dotnet build -c %CONFIG% -p:Platform=%PLATFORM%
if errorlevel 1 (
	echo.
	echo ERROR: La compilacion del cliente fallo!
	cd ..
	pause
	exit /b 1
)
cd ..

echo.
echo [3/4] Verificando ejecutable...
set EXE_PATH=%PROJECT_PATH%\bin\%PLATFORM%\%CONFIG%\net8.0-windows\win-x64\EmpresaChat.exe
if not exist "%EXE_PATH%" (
	echo ERROR: EmpresaChat.exe no fue generado!
	pause
	exit /b 1
)
echo. Encontrado: %EXE_PATH%

echo.
echo [4/4] Buscando InnoSetup para crear instalador...
set INNO_PATH=
for %%x in (
	"C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
	"C:\Program Files\Inno Setup 6\ISCC.exe"
	"C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
	"C:\Program Files\Inno Setup 5\ISCC.exe"
) do (
	if exist %%x (
		set INNO_PATH=%%x
		goto found_inno
	)
)

:found_inno
if "%INNO_PATH%"=="" (
	echo.
	echo ADVERTENCIA: InnoSetup no encontrado en rutas comunes.
	echo Instalable NO creado. El ejecutable esta listo en:
	echo.
	echo   %EXE_PATH%
	echo.
	echo Para crear un instalador .exe:
	echo   1. Instala Inno Setup desde: https://jrsoftware.org/isdl.php
	echo   2. Copia setup.iss a: Client.WPF\setup.iss
	echo   3. Ejecuta: ISCC.exe %PROJECT_PATH%\setup.iss
	echo.
	pause
) else (
	echo. Creando instalador con InnoSetup...
	"%INNO_PATH%" "%PROJECT_PATH%\setup.iss"
	if errorlevel 1 (
		echo ERROR: InnoSetup fallo!
		pause
		exit /b 1
	)
	echo.
	echo Instalador creado exitosamente en: Output\ChatAgendaCliente-1.0.0.exe
)

echo.
echo ===============================================
echo Compilacion completada!
echo ===============================================
echo.
pause
