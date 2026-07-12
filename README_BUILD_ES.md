Resumen de compilación y creación de instalador (Windows)

Este repositorio contiene un proyecto WPF (.NET 8). Los scripts añadidos permiten publicar un .exe autocontenido y generar un instalador Inno Setup.

Requisitos previos
- .NET 8 SDK (dotnet) en PATH
- (Opcional) UPX en PATH para comprimir el ejecutable
- (Opcional) Inno Setup (ISCC.exe) en PATH para crear el instalador

Pasos rápidos
1. Abrir PowerShell en la raíz del repo.
2. Ejecutar: .\build_and_package.ps1

Qué hace el script
- dotnet publish Client.WPF/Client.WPF.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
- Coloca la salida en artifacts/publish/win-x64
- Si UPX está disponible, comprimirá los .exe generados
- Si existe installer/ClientWPF.iss y ISCC está instalado, generará un instalador en artifacts/installer

Notas
- Ajuste el runtime (win-x64) si necesita otra arquitectura.
- PublishTrimmed se deja en false por seguridad; activar puede reducir tamaño pero romper dependencias reflexivas.
- Firmado de código no incluido; para firmas Authenticode use SignTool y un certificado válido.
