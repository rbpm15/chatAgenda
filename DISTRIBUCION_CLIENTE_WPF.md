# ChatAgenda - Cliente WPF
## Guía de Distribución

### ¿Qué se ha arreglado?

#### 1. **Interfaz No Aparecía Tras Instalación**
   - ✅ Mejorado InitializeSystemTray() para garantizar que NotifyIcon se cree correctamente
   - ✅ Mejorada lógica de carga de configuración (LoadConfig) para auto-conectar
   - ✅ Si existe configuración guardada, se carga automáticamente sin mostrar overlay

#### 2. **Conexión Automática al Servidor**
   - ✅ Creada clase ServerConfig.cs para gestionar configuración JSON
   - ✅ Almacena: ServerUrl, Mode, AutoConnect, LastUpdated
   - ✅ Ubicación de archivo: AppData\ChatAgenda\server.config
   - ✅ Client se conecta automáticamente al servidor configurado

#### 3. **Notificaciones de Calendario**
   - ✅ WebViewMessage ahora soporta NotificationType y EventId
   - ✅ Handler de WebMessageReceived detecta eventos "calendarEvent"
   - ✅ Muestra diferentes iconos: Info para chat, Warning para calendario
   - ✅ Al recibir notificación de calendario:
	 - Muestra notificación con icono especial
	 - Restaura la ventana si está minimizada
	 - Intenta navegar a la vista de calendario via JavaScript

#### 4. **Abrirse Desde Notificaciones**
   - ✅ ScriptBridge.Notify() mejorado con parámetro "type"
   - ✅ Menú de contexto mejorado en bandeja:
	 - "Ver Chat"
	 - "Ver Calendario"
   - ✅ Click en notificación abre app y navega a vista correcta
   - ✅ NavigateToView() ejecuta JavaScript hooks para cambiar vistas

#### 5. **No Afecta el Servidor**
   - ✅ NINGÚN cambio se hizo en los proyectos del servidor (chatAgenda)
   - ✅ El servidor sigue funcionando en IP:5002
   - ✅ Solo se modificó Client.WPF

---

### Archivos Modificados/Creados

#### **Código Principal (Client.WPF)**
- `MainWindow.xaml.cs` - Mejorado con nuevas funciones de notificaciones y navegación
- `ServerConfig.cs` - NUEVO: Gestor de configuración JSON
- `MainWindow.xaml` - Sin cambios en layout (mismo visual)

#### **Configuración (Client.WPF)**
- `Client.WPF.csproj` - Sin cambios, ya tenía RuntimeIdentifier correcta
- `App.xaml/App.xaml.cs` - Sin cambios

#### **Scripts de Compilación y Distribución**
- `build-cliente.bat` - NUEVO: Compila cliente Release x64 + ejecuta InnoSetup si existe
- `create-cliente-portable.bat` - NUEVO: Crea distribución ZIP con instrucciones
- `Client.WPF/setup.iss` - Ya existía, InnoSetup script para instalador .exe

---

### Cómo Distribuir el Cliente

#### **Opción 1: Instalador (.exe) con InnoSetup**
```batch
Requisito: Tener InnoSetup instalado (https://jrsoftware.org/isdl.php)

Ejecutar: build-cliente.bat
Resultado: ChatAgendaCliente-1.0.0.exe en Output\
```

#### **Opción 2: Distribución Portátil (ZIP)**
```
Archivo: Output\ChatAgendaCliente-1.0.0-portable.zip
Contenido:
  - EmpresaChat.exe
  - Todas las DLLs necesarias
  - WebView2\
  - server.config.example
  - LEEME.txt (instrucciones en español)
```

---

### Pasos para Instalar el Cliente

#### **Si usas el instalador .exe**
1. Ejecuta `ChatAgendaCliente-1.0.0.exe`
2. Sigue el wizard de instalación
3. Se instalará en `Program Files\ChatAgenda\`
4. Se creará acceso directo en Escritorio (opcional)
5. Se puede configurar auto-inicio con Windows

#### **Si usas la versión portátil**
1. Descomprime `ChatAgendaCliente-1.0.0-portable.zip`
2. Ve a la carpeta `ChatAgendaCliente-Portable`
3. Copia `server.config.example` a `AppData\ChatAgenda\server.config`
4. Edita con la IP/puerto de tu servidor
5. Ejecuta `EmpresaChat.exe`

---

### Configuración del Cliente

El cliente espera un archivo `server.config` en: `%APPDATA%\ChatAgenda\`

```json
{
  "ServerUrl": "http://192.168.1.100:5002",
  "Mode": "CLIENT",
  "AutoConnect": true
}
```

#### Campos:
- **ServerUrl**: URL completa del servidor (ej: http://IP:5002)
- **Mode**: "CLIENT" para modo cliente LAN, "SERVER" para servidor local
- **AutoConnect**: true para conectar automáticamente al iniciar

---

### Características Implementadas

✅ **Interfaz Visible**
- La ventana se muestra al iniciar si hay configuración guardada
- Se minimiza a bandeja del sistema
- Se restaura con click en bandeja

✅ **Notificaciones**
- Chat: Icono info, sonido de notificación
- Calendario: Icono warning, abre vista calendario

✅ **Navegación Rápida**
- Menú contextual en bandeja del sistema
- Click en notificación abre app en vista correspondiente
- Doble-click en bandeja restaura ventana

✅ **Configuración Automática**
- Si existe server.config, conecta automáticamente
- Si no existe, muestra opciones para configurar

---

### Dependencias Requeridas en Cliente

1. **.NET 8.0 Runtime** (net8.0-windows)
   - https://dotnet.microsoft.com/download/dotnet/8.0

2. **WebView2 Runtime**
   - https://developer.microsoft.com/en-us/microsoft-edge/webview2/

3. **Windows 10+** o **Windows Server 2019+**

---

### Solución de Problemas

#### "No aparece la interfaz"
1. Verifica que .NET 8.0 Runtime está instalado
2. Verifica que WebView2 Runtime está instalado
3. Si existe server.config, bórralo y reinicia para ver overlay de configuración

#### "No se conecta al servidor"
1. Verifica la IP del servidor: `ping 192.168.x.x`
2. Verifica que servidor está corriendo: abre navegador y ve a `http://IP:5002`
3. Verifica que server.config tiene URL correcta
4. Comprueba firewall: permite puerto 5002

#### "No recibo notificaciones"
1. Verifica que el servidor envía notificaciones correctamente
2. Abre DevTools en navegador (F12) en el WebView para ver errores
3. Comprueba que ScriptBridge está disponible en web UI

---

### Próximas Mejoras (Opcional)

- [ ] Auto-descargar WebView2 Runtime si falta
- [ ] Auto-actualización del cliente
- [ ] Widgets de calendario en notifications
- [ ] Sincronización de calendario local
- [ ] Notificaciones de Windows (Toast) además de balloon

---

**Compilado**: 15/07/2026
**Versión**: 1.0.0
**Estado**: ✅ Listo para distribución
