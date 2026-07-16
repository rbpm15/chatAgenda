@echo off
REM Script para compilar el cliente WPF y crear el instalador InnoSetup
REM Este script solo compila y empaqueta el cliente (Client.WPF)

setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ===============================================
echo ChatAgenda - Compilador de Cliente WPF
echo ===============================================
echo.

REM Detectar la ruta del proyecto Client.WPF
set PROJECT_PATH=Client.WPF
set CONFIG=Release
set FRAMEWORK=net8.0-windows

echo [1/3] Limpiando compilaciones previas...
if exist "%PROJECT_PATH%\bin\%CONFIG%" rmdir /s /q "%PROJECT_PATH%\bin\%CONFIG%" 2>nul
if exist "%PROJECT_PATH%\obj" rmdir /s /q "%PROJECT_PATH%\obj" 2>nul

echo [2/3] Compilando Cliente WPF en modo Release...
pushd "%PROJECT_PATH%"
dotnet build -c %CONFIG% -p:Platform=x64
if !errorlevel! neq 0 (
	echo ERROR: La compilación falló. Verifica los errores arriba.
	popd
	pause
	exit /b 1
)
popd

echo.
echo [3/3] Verificando que exista InnoSetup...
if not exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
	if not exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
		echo WARNING: InnoSetup no está instalado. Se requiere para crear el instalador.
		echo Descárgalo desde: https://jrsoftware.org/isdl.php
		echo.
		echo El cliente compilado está en: %PROJECT_PATH%\bin\%CONFIG%\%FRAMEWORK%\
		pause
		exit /b 1
	) else (
		set ISCC="%ProgramFiles%\Inno Setup 6\ISCC.exe"
	)
) else (
	set ISCC="%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
)

echo Compilando instalador con InnoSetup...
cd /d "%~dp0Client.WPF"
%ISCC% setup.iss
if !errorlevel! neq 0 (
	echo ERROR: La creación del instalador falló.
	cd /d "%~dp0"
	pause
	exit /b 1
)

echo.
echo ===============================================
echo ÉXITO: El cliente ha sido compilado e instalador creado
echo Compilado en: %PROJECT_PATH%\bin\%CONFIG%\%FRAMEWORK%\
echo Instalador en: Output\
echo ===============================================
cd /d "%~dp0"
pause
exit /b 0
