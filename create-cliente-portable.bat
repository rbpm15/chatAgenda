@echo off
REM Script para crear distribucion portatil del cliente ChatAgenda

setlocal enabledelayedexpansion
cd /d "%~dp0"

set PORT_NAME=ChatAgendaCliente-Portable
set SOURCE_PATH=Client.WPF\bin\x64\Release\net8.0-windows\win-x64
set OUTPUT_PATH=Output

echo ===============================================
echo Creando distribucion portatil del Cliente
echo ===============================================
echo.

REM Crear carpeta Output si no existe
if not exist "%OUTPUT_PATH%" mkdir "%OUTPUT_PATH%"

REM Eliminar carpeta portatil anterior si existe
if exist "%PORT_NAME%" (
	echo Eliminando carpeta anterior...
	rmdir /s /q "%PORT_NAME%"
)

echo.
echo Copiando archivos...
mkdir "%PORT_NAME%"
xcopy "%SOURCE_PATH%\*" "%PORT_NAME%\" /E /Y /Q

REM Crear archivo de configuracion de ejemplo
echo Creando archivo de configuracion de ejemplo...
(
	echo {
	echo   "ServerUrl": "http://IP_DEL_SERVIDOR:5002",
	echo   "Mode": "CLIENT",
	echo   "AutoConnect": true
	echo }
) > "%PORT_NAME%\server.config.example"

REM Crear archivo README
echo Creando archivos de instrucciones...
(
	echo ChatAgenda - Cliente WPF
	echo.
	echo INSTRUCCIONES DE USO:
	echo.
	echo 1. Asegúrate de que el servidor ChatAgenda está funcionando en IP:5002
	echo.
	echo 2. Edita o crea el archivo server.config con tu configuracion:
	echo    ^{
	echo      "ServerUrl": "http://192.168.x.x:5002",
	echo      "Mode": "CLIENT",
	echo      "AutoConnect": true
	echo    ^}
	echo.
	echo 3. Ejecuta EmpresaChat.exe
	echo.
	echo CARACTERISTICAS:
	echo - Se conecta automaticamente al servidor configurado
	echo - Muestra notificaciones de chat y calendario
	echo - Al hacer clic en notificacion, abre la app en la vista correspondiente
	echo - Se minimiza a bandeja del sistema
	echo.
	echo SOLUCION DE PROBLEMAS:
	echo.
	echo Si no aparece la interfaz:
	echo - Asegúrate de que tienes .NET 8.0 Runtime instalado
	echo - Verifica que WebView2 Runtime esté instalado (se puede descargar de Microsoft)
	echo.
	echo Si no se conecta al servidor:
	echo - Verifica que la IP y puerto son correctos
	echo - Asegúrate de que el servidor está ejecutándose
	echo - Comprueba la conectividad de red
	echo.
) > "%PORT_NAME%\LEEME.txt"

REM Crear script de configuracion
(
	echo @echo off
	echo REM Script para configurar la conexion del servidor
	echo.
	echo echo ChatAgenda - Configurador de Servidor
	echo echo.
	echo set /p IP="Introduce la IP del servidor (ej: 192.168.1.100): "
	echo set /p PORT="Introduce el puerto (ej: 5002, Enter para defecto): "
	echo.
	echo if "!PORT!"=="" set PORT=5002
	echo.
	echo (
	echo   echo ^{
	echo   echo   "ServerUrl": "http://!IP!:!PORT!",
	echo   echo   "Mode": "CLIENT",
	echo   echo   "AutoConnect": true
	echo   echo ^}
	echo ) ^> "AppData\ChatAgenda\server.config"
	echo.
	echo echo.
	echo echo Configuracion guardada en: AppData\ChatAgenda\server.config
	echo echo.
	echo pause
) > "%PORT_NAME%\Configurar-Servidor.bat"

echo.
echo Comprimiendo archivos...
if exist "%OUTPUT_PATH%\ChatAgendaCliente-1.0.0-portable.zip" del "%OUTPUT_PATH%\ChatAgendaCliente-1.0.0-portable.zip"
powershell -Command "Compress-Archive -Path '%PORT_NAME%' -DestinationPath '%OUTPUT_PATH%\ChatAgendaCliente-1.0.0-portable.zip'"

echo.
echo ===============================================
echo Distribucion portatil creada exitosamente!
echo ===============================================
echo.
echo Ubicacion: Output\ChatAgendaCliente-1.0.0-portable.zip
echo.
pause
