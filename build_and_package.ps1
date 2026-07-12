<#
Script de compilación y empaquetado para Windows.

Requisitos:
- .NET 8 SDK instalado y en PATH
- (Opcional) UPX instalado y en PATH para comprimir el .exe
- (Opcional) Inno Setup (ISCC.exe) para generar instalador .exe

Uso:
1. Ejecutar en PowerShell: .\build_and_package.ps1
2. El resultado de publish se deja en .\artifacts\publish\win-x64
3. Si ISCC está instalado, generará un instalador en .\artifacts\installer
#>

set -e

Write-Host "Iniciando build y empaquetado..."

$projectPath = "Client.WPF/Client.WPF.csproj"
$configuration = "Release"
$runtime = "win-x64"
$publishDir = Join-Path -Path (Get-Location) -ChildPath "artifacts/publish/$runtime"

if (-not (Test-Path $projectPath)) {
	Write-Error "No se encontró el proyecto: $projectPath"
	exit 1
}

Write-Host "Publicando proyecto $projectPath (Release, $runtime)..."

dotnet publish $projectPath -c $configuration -r $runtime --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false -o $publishDir

if ($LASTEXITCODE -ne 0) {
	Write-Error "dotnet publish falló (código $LASTEXITCODE)"
	exit $LASTEXITCODE
}

Write-Host "Publicación completada: $publishDir"

# Intentar comprimir con UPX si está disponible
if (Get-Command upx -ErrorAction SilentlyContinue) {
	Write-Host "UPX detectado: comprimiendo ejecutables..."
	Get-ChildItem -Path $publishDir -Filter *.exe -Recurse | ForEach-Object {
		Write-Host "Comprimiendo $_.FullName"
		& upx --best --lzma $_.FullName
	}
} else {
	Write-Host "UPX no encontrado: omitiendo compresión. Para comprimir, instale UPX y vuelva a ejecutar el script."
}

# Generar instalador con Inno Setup si ISCC está disponible
$issPath = "installer/ClientWPF.iss"
if (Test-Path $issPath) {
	$iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
	if ($iscc) {
		Write-Host "ISCC encontrado: generando instalador..."
		$installerOut = Join-Path -Path (Get-Location) -ChildPath "artifacts/installer"
		New-Item -ItemType Directory -Path $installerOut -Force | Out-Null
		& $iscc.Path $issPath /O"$installerOut" /DMyAppPublishDir="$publishDir"
		if ($LASTEXITCODE -ne 0) {
			Write-Warning "ISCC falló con código $LASTEXITCODE"
		} else {
			Write-Host "Instalador generado en: $installerOut"
		}
	} else {
		Write-Host "ISCC (Inno Setup Compiler) no encontrado: para crear un instalador instale Inno Setup y asegúrese de que ISCC.exe esté en PATH."
	}
} else {
	Write-Host "No existe el script de Inno Setup: $issPath. Omite creación de instalador."
}

Write-Host "Proceso finalizado."
