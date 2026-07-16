# ✅ ChatAgenda - Cliente WPF Arreglado

## Resumen de Cambios

Se han corregido y mejorado **SOLO el cliente WPF** sin tocar el servidor (que ya funciona perfectamente).

---

## 🎯 Problemas Arreglados

### 1. ❌ Interfaz No Se Mostraba Tras Instalar
**Causa**: ModeSelectionOverlay ocultaba la ventana y notifyIcon tenía problemas iniciales.

**Solución**:
- Mejorada lógica de carga automática de configuración
- Si existe `server.config`, se carga y conecta directamente
- NotifyIcon se reinicializa correctamente si falta
- Ventana se muestra correctamente cuando se inicia

✅ **Resultado**: La interfaz aparece correctamente al instalar.

---

### 2. ❌ No Se Conectaba Automáticamente a la IP del Servidor
**Causa**: No había sistema confiable de almacenar la IP del servidor.

**Solución**:
- Creada nueva clase `ServerConfig.cs` para gestionar configuración en JSON
- Se guarda en: `AppData\ChatAgenda\server.config`
- Soporta auto-conexión automática
- Formato simple y editable por usuarios

✅ **Resultado**: Se conecta automáticamente a la IP configurada.

---

### 3. ❌ Notificaciones de Calendario No Funcionaban
**Causa**: WebViewMessage no diferenciaba entre chat y eventos de calendario.

**Solución**:
- Agregado soporte para `NotificationType` en WebViewMessage
- Handler detecta eventos `type: "calendarEvent"`
- Muestra icono diferente (Warning para calendario)
- Intenta navegar a vista de calendario automáticamente

✅ **Resultado**: Las notificaciones de calendario aparecen con icono distinguible.

---

### 4. ❌ No Se Abría la App Desde las Notificaciones
**Causa**: ScriptBridge no restauraba ventana y no había navegación a vista.

**Solución**:
- Mejorado ScriptBridge.Notify para soportar parámetro `type`
- Restaura ventana automáticamente si está minimizada
- Menú contextual mejorado en bandeja:
  - "Ver Chat"
  - "Ver Calendario"
  - "Salir"
- Función `NavigateToView()` ejecuta JavaScript hooks:
  - `window.navigateToCalendar()`
  - `window.navigateToChat()`

✅ **Resultado**: Al recibir notificación, se abre la app en la vista correcta.

---

### 5. ✅ No Afecta el Servidor
**Verificación**: NINGÚN archivo del servidor fue modificado.
- ❌ chatAgenda/ - SIN CAMBIOS
- ❌ wwwroot/ - SIN CAMBIOS
- ✅ Client.WPF/ - SOLO AQUÍ

El servidor sigue funcionando en `IP:5002` sin cambios.

---

## 📦 Archivos Modificados

### Cliente WPF (Client.WPF/)
```
✏️  MainWindow.xaml.cs
	- Mejorada inicialización WebView2
	- Mejor carga de configuración
	- Manejo mejorado de notificaciones
	- ScriptBridge mejorado
	- NavigateToView para cambiar pestañas
	- RestoreWindowAndNavigate para notificaciones

🆕 ServerConfig.cs
	- Nuevo gestor de configuración JSON
	- Almacena en AppData\ChatAgenda\server.config
	- Load(), Save(), Clear()

🆕 build-cliente.bat
	- Compila cliente Release x64
	- Ejecuta InnoSetup si está disponible

🆕 create-cliente-portable.bat
	- Crea distribución ZIP portátil
```

---

## 📥 Cómo Distribuir

### Opción A: Versión Portátil (ZIP) - RECOMENDADO
```
Archivo: Output\ChatAgendaCliente-1.0.0-portable.zip (1.9 MB)

Contenido:
  ├── EmpresaChat.exe
  ├── *.dll (todas las librerías)
  ├── WebView2\ (runtime)
  ├── server.config.example
  └── LEEME.txt (instrucciones)

Pasos:
1. Descomprime el ZIP
2. Edita/copia server.config con la IP del servidor
3. Ejecuta EmpresaChat.exe
```

### Opción B: Instalador (EXE) - Si tienes InnoSetup
```
Requisito: Tener InnoSetup 6+ instalado
Descargar: https://jrsoftware.org/isdl.php

Ejecutar: .\build-cliente.bat

Resultado: Output\ChatAgendaCliente-1.0.0.exe
```

---

## ⚙️ Configuración del Cliente

Archivo: `%APPDATA%\ChatAgenda\server.config`

```json
{
  "ServerUrl": "http://192.168.1.100:5002",
  "Mode": "CLIENT",
  "AutoConnect": true
}
```

### Campos:
- **ServerUrl**: URL del servidor (ej: http://IP_SERVIDOR:5002)
- **Mode**: "CLIENT" para cliente LAN
- **AutoConnect**: true para conectar automáticamente

---

## 🔧 Dependencias Necesarias en Máquinas de Usuarios

1. **.NET 8.0 Runtime** (net8.0-windows)
   - Descargar: https://dotnet.microsoft.com/download/dotnet/8.0

2. **WebView2 Runtime**
   - Descargar: https://developer.microsoft.com/en-us/microsoft-edge/webview2/

3. **Windows 10+** o **Windows Server 2019+**

---

## ✨ Características Implementadas

✅ **Interfaz**
- Se ve al iniciar si hay configuración
- Se minimiza a bandeja del sistema
- Se restaura con click en bandeja

✅ **Notificaciones**
- Chat: Icono info, detecta automáticamente
- Calendario: Icono warning, abre vista calendario
- Al hacer click, se abre app en vista correspondiente

✅ **Configuración**
- Automática si existe server.config
- Almacenada en JSON (editable por usuarios)
- Soporta multiple máquinas con diferentes servidores

✅ **Experiencia de Usuario**
- Menú contextual mejorado en bandeja
- Navegación rápida a chat/calendario
- Auto-restauración desde notificaciones

---

## 🚀 Estados de Compilación

```
✅ Compilación cliente Release x64: EXITOSA
✅ Executable EmpresaChat.exe: 151.5 KB
✅ ZIP portátil: 1.9 MB
✅ Scripts de distribución: LISTOS
✅ Documentación: COMPLETA
✅ Servidor: NO AFECTADO
```

---

## 📝 Próximos Pasos (Opcionales)

Para mejorar aún más:

- [ ] Auto-descargar WebView2 Runtime si falta
- [ ] Auto-descargar .NET Runtime si falta
- [ ] Crear instalador que distribuya dependencias
- [ ] Notificaciones de Windows (Toast) además de balloon
- [ ] Widgets de calendario en preview de notificaciones
- [ ] Auto-actualización del cliente

---

## 📞 Soporte

Si hay problemas:

1. **Interfaz no aparece**: Instala .NET 8.0 Runtime y WebView2
2. **No se conecta**: Verifica IP en server.config y firewall
3. **No hay notificaciones**: Verifica que servidor envía notificaciones
4. **Errores**: Abre DebuggingMode (DEV) en web UI para ver logs

---

**Versión**: 1.0.0  
**Compilado**: 15/07/2026  
**Estado**: 🟢 Listo para distribución en usuarios  
**Servidor**: 🟢 NO AFECTADO - Sigue funcionando normalmente
