# Correcciones del Cliente WPF - ChatAgenda

## Resumen de cambios

Se han corregido tres problemas principales en el cliente WPF:

### 1. ✅ WebView2 - Error de inicialización
**Problema:** El instalador fallaba porque WebView2 no se inicializaba correctamente.

**Solución:**
- Se configuró un UserDataFolder explícito en `AppData\Roaming\ChatAgenda\WebView2`
- Se agregó manejo de fallback automático si la inicialización personalizada falla
- Se actualizó `Client.WPF.csproj` con `RuntimeIdentifier=win-x64` para asegurar compatibilidad

**Archivos modificados:**
- `Client.WPF/Client.WPF.csproj` - Configuración de proyecto
- `Client.WPF/MainWindow.xaml.cs` - Inicialización mejorada de WebView2

### 2. ✅ Notificaciones del sistema
**Problema:** Las notificaciones no aparecían cuando llegaban mensajes.

**Solución:**
- Se mejoró `ShowBackgroundNotification()` para recrear el NotifyIcon si es necesario
- Se agregó validación de null en `ScriptBridge.Notify()`
- Se mejoró `InitializeSystemTray()` con mejor manejo de excepciones
- Se agregó logging para debugging

**Archivos modificados:**
- `Client.WPF/MainWindow.xaml.cs` - Lógica de notificaciones robusta

### 3. ✅ Compilación e instalador InnoSetup
**Problema:** No había forma automatizada de compilar y empaquetar solo el cliente.

**Solución:**
Se crearon tres archivos para automatizar el proceso:

#### a) `Client.WPF/setup.iss` - Script InnoSetup
- Empaqueta solo el cliente compilado (x64 Release)
- Configura instalación en `Program Files\ChatAgenda`
- Incluye opciones de autostart y acceso directo en escritorio
- Soporta instalaciones en español e inglés

#### b) `build-client.bat` - Script Batch (Windows CMD)
```bash
# Uso: ejecutar desde la raíz del proyecto
build-client.bat
```
- Limpia compilaciones previas
- Compila en modo Release (x64)
- Ejecuta InnoSetup si está instalado
- Muestra rutas del binario y instalador

#### c) `build-client.ps1` - Script PowerShell
```powershell
# Uso básico
.\build-client.ps1

# Solo compilar (sin InnoSetup)
.\build-client.ps1 -BuildOnly

# Solo crear instalador (si ya está compilado)
.\build-client.ps1 -InstallersOnly

# Especificar configuración
.\build-client.ps1 -Configuration Debug
```

## Requisitos previos

### Para compilar:
- .NET 8.0 SDK instalado
- Visual Studio Community 2026 (opcional, pero se recomienda)

### Para crear el instalador:
- InnoSetup 6 instalado desde: https://jrsoftware.org/isdl.php
- Descargar la versión: "Inno Setup 6.X.X (Unicode)" (preferentemente 6.2.0+)

## Instrucciones de uso

### Opción 1: Compilar e instalar usando Batch
```cmd
cd C:\Users\Merino\Desktop\chatAgenda
build-client.bat
```

### Opción 2: Compilar e instalar usando PowerShell
```powershell
cd C:\Users\Merino\Desktop\chatAgenda
.\build-client.ps1
```

### Opción 3: Compilar manualmente
```cmd
cd C:\Users\Merino\Desktop\chatAgenda\Client.WPF
dotnet build -c Release -p:Platform=x64
```

El binario compilado estará en:
```
Client.WPF\bin\x64\Release\net8.0-windows\win-x64\EmpresaChat.exe
```

### Opción 4: Compilar desde Visual Studio
1. Abrir la solución en Visual Studio
2. Hacer clic derecho en proyecto `Client.WPF`
3. Seleccionar "Build" o "Rebuild"
4. El ejecutable estará en `Client.WPF\bin\x64\Release\net8.0-windows\win-x64\`

## Instalación para usuarios finales

Una vez se cree el instalador (`.exe`), los usuarios pueden:
1. Ejecutar el instalador
2. Seleccionar opciones de instalación (autostart, acceso directo)
3. El programa se instalará en `C:\Program Files\ChatAgenda\`

## Solución de problemas

### WebView2 Runtime no encontrado
Si aún recibe error sobre WebView2:
1. Descargar WebView2 Runtime desde: https://developer.microsoft.com/es-es/microsoft-edge/webview2/
2. Ejecutar el instalador de WebView2 Runtime
3. Reiniciar la aplicación ChatAgenda

### Las notificaciones aún no aparecen
1. Verificar que Windows permita notificaciones de la aplicación:
   - Configuración > Sistema > Notificaciones e acciones
   - Buscar "ChatAgenda" y habilitar notificaciones
2. Verificar que la bandeja del sistema está visible

### InnoSetup no se encuentra
Si no está instalado:
1. Descargar desde: https://jrsoftware.org/isdl.php
2. Instalar en la ruta por defecto
3. Ejecutar nuevamente los scripts de compilación

## Cambios de código resumidos

### Client.WPF.csproj
```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>false</SelfContained>
```

### MainWindow.xaml.cs - Inicialización WebView2
```csharp
var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
	null, 
	Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
				 "ChatAgenda", "WebView2")
);
await WebView.EnsureCoreWebView2Async(env);
```

### MainWindow.xaml.cs - Notificaciones mejoradas
```csharp
private void ShowBackgroundNotification(string title, string body)
{
	if (_notifyIcon == null)
	{
		// Recrear si es necesario
		_notifyIcon = new System.Windows.Forms.NotifyIcon();
		_notifyIcon.Icon = System.Drawing.SystemIcons.Information;
		_notifyIcon.Text = "ChatAgenda";
		_notifyIcon.Visible = true;
	}

	_notifyIcon.BalloonTipTitle = title;
	_notifyIcon.BalloonTipText = body;
	_notifyIcon.ShowBalloonTip(5000);
}
```

## Contacto

Para reportar problemas: https://github.com/rbpm15/chatAgenda/issues
