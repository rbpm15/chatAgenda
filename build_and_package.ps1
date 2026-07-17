# ChatAgenda - Script de empaquetado completo
# Genera dos paquetes: Servidor y Cliente
#
# Requisitos: .NET 8 SDK en PATH
# Opcional:   Inno Setup (ISCC.exe) para generar instaladores .exe

param(
    [string]$Configuration = "Release",
    [string]$Runtime       = "win-x64"
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$ArtifactsDir = Join-Path $Root "artifacts"

function Write-Step { param([string]$msg) Write-Host "" ; Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Ok   { param([string]$msg) Write-Host "  OK: $msg" -ForegroundColor Green }
function Write-Warn { param([string]$msg) Write-Host "  ADVERTENCIA: $msg" -ForegroundColor Yellow }

# Directorios de salida
$OutBackend   = Join-Path $ArtifactsDir "backend\win-x64"
$OutServer    = Join-Path $ArtifactsDir "server_v2\win-x64"
$OutClient    = Join-Path $ArtifactsDir "client_v2\win-x64"
$ZipDir       = Join-Path $ArtifactsDir "zip"
$InstallerDir = Join-Path $ArtifactsDir "installer"

New-Item -ItemType Directory -Path $OutBackend   -Force | Out-Null
New-Item -ItemType Directory -Path $OutServer    -Force | Out-Null
New-Item -ItemType Directory -Path $OutClient    -Force | Out-Null
New-Item -ItemType Directory -Path $ZipDir       -Force | Out-Null
New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null

# --- 1. Publicar backend ASP.NET (DESACTIVADO A PETICION DEL USUARIO) ---
# Write-Step "1/4 Publicando servidor ASP.NET (chatAgenda)..."
# $BackendProj = Join-Path $Root "chatAgenda.csproj"
# dotnet publish $BackendProj -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o $OutBackend
# if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Fallo en publicacion del backend" -ForegroundColor Red; exit 1 }
# Write-Ok "Backend publicado en: $OutBackend"

# --- 2. Publicar WPF Cliente ---
Write-Step "2/2 Publicando WPF Cliente (Client.WPF)..."

$ClientWpfProj = Join-Path $Root "Client.WPF\Client.WPF.csproj"
dotnet publish $ClientWpfProj `
    -c $Configuration -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -o $OutClient

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo en publicacion del WPF Cliente" -ForegroundColor Red
    exit 1
}
Write-Ok "WPF Cliente publicado en: $OutClient"

# --- Generar ZIPs ---
Write-Step "Generando archivos ZIP..."

$ZipClient = Join-Path $ZipDir "ChatAgenda_Cliente.zip"

if (Test-Path $ZipClient) { Remove-Item $ZipClient -Force }

Compress-Archive -Path "$OutClient\*" -DestinationPath $ZipClient

Write-Ok "ZIP Cliente:  $ZipClient"

# --- Inno Setup (opcional) ---
$IsccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$Iscc = $IsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $Iscc) {
    $IsccCmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($IsccCmd) {
        $Iscc = $IsccCmd.Source
    }
}

if ($Iscc) {
    Write-Step "Inno Setup encontrado. Generando instaladores .exe..."
    $InstallerDir = Join-Path $Root "artifacts\installer"
    New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null

    $IssServer = Join-Path $Root "installer\ServerApp.iss"
    $IssClient = Join-Path $Root "installer\ClientApp.iss"

    if (Test-Path $IssClient) {
        & $Iscc $IssClient /O"$InstallerDir" /DPublishDir="$OutClient"
        if ($LASTEXITCODE -eq 0) {
            Write-Ok "Instalador cliente generado en: $InstallerDir"
        } else {
            Write-Warn "ISCC fallo para instalador cliente (codigo $LASTEXITCODE)"
        }
    } else {
        Write-Warn "No encontrado: $IssClient"
    }
} else {
    Write-Warn "Inno Setup no encontrado. Solo se generaron ZIPs."
    Write-Host "  Para instalar Inno Setup: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
}

# --- Resumen ---
Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  EMPAQUETADO COMPLETADO" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Cliente WPF:   $OutClient"
Write-Host "  ZIPs:          $ZipDir"
Write-Host ""
Write-Host "  Distribuir ChatAgenda_Cliente.zip  => Resto de PCs"
Write-Host "=================================================" -ForegroundColor Cyan
