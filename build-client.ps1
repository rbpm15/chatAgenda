# Script para compilar el cliente WPF y crear el instalador InnoSetup
# Uso: .\build-client.ps1

param(
	[switch]$BuildOnly = $false,
	[switch]$InstallersOnly = $false,
	[string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "ChatAgenda - Compilador de Cliente WPF" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Validar que estamos en el directorio correcto
if (!(Test-Path "Client.WPF")) {
	Write-Host "ERROR: No se encontró el directorio Client.WPF" -ForegroundColor Red
	exit 1
}

$ProjectPath = "Client.WPF"
$Framework = "net8.0-windows"
$BuildPath = "$ProjectPath\bin\$Configuration\$Framework"
$SetupScript = "$ProjectPath\setup.iss"

# Paso 1: Compilar
if (!$InstallersOnly) {
	Write-Host "[1/3] Limpiando compilaciones previas..." -ForegroundColor Yellow
	if (Test-Path "$ProjectPath\bin\$Configuration") {
		Remove-Item "$ProjectPath\bin\$Configuration" -Recurse -Force -ErrorAction SilentlyContinue
	}
	if (Test-Path "$ProjectPath\obj") {
		Remove-Item "$ProjectPath\obj" -Recurse -Force -ErrorAction SilentlyContinue
	}

	Write-Host "[2/3] Compilando Cliente WPF en modo $Configuration..." -ForegroundColor Yellow
	Push-Location $ProjectPath
	& dotnet build -c $Configuration -p:Platform=x64
	if ($LASTEXITCODE -ne 0) {
		Pop-Location
		Write-Host "ERROR: La compilación falló." -ForegroundColor Red
		exit 1
	}
	Pop-Location

	if (!$BuildPath -or !(Test-Path $BuildPath)) {
		Write-Host "ERROR: La compilación no generó los binarios esperados en $BuildPath" -ForegroundColor Red
		exit 1
	}

	Write-Host "✓ Compilación completada" -ForegroundColor Green
	Write-Host ""
}

# Paso 2: Crear instalador con InnoSetup
if (!$BuildOnly) {
	Write-Host "[3/3] Creando instalador con InnoSetup..." -ForegroundColor Yellow

	# Buscar InnoSetup
	$InnoSetupPaths = @(
		"${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
		"${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
	)

	$IsccPath = $null
	foreach ($path in $InnoSetupPaths) {
		if (Test-Path $path) {
			$IsccPath = $path
			break
		}
	}

	if (!$IsccPath) {
		Write-Host "WARNING: InnoSetup 6 no está instalado." -ForegroundColor Yellow
		Write-Host "Descárgalo desde: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
		Write-Host ""
		Write-Host "Cliente compilado en: $BuildPath" -ForegroundColor Green
		exit 0
	}

	# Compilar el instalador
	$SetupDir = Split-Path -Parent $SetupScript
	Push-Location $SetupDir
	& $IsccPath (Split-Path -Leaf $SetupScript)
	if ($LASTEXITCODE -ne 0) {
		Pop-Location
		Write-Host "ERROR: InnoSetup falló al crear el instalador." -ForegroundColor Red
		exit 1
	}
	Pop-Location

	Write-Host "✓ Instalador creado" -ForegroundColor Green
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Green
Write-Host "ÉXITO: Proceso completado" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host "Cliente compilado en: $BuildPath" -ForegroundColor Green
if (!(Test-Path "$ProjectPath\Output")) {
	Write-Host "Instalador en: $ProjectPath\Output\" -ForegroundColor Green
}
Write-Host ""
