# 🔔 Sistema de Notificaciones Mejorado - ChatAgenda

## ✅ Cambios Realizados

### Problema Anterior
- Las notificaciones NO aparecían cuando la app estaba minimizada/cerrada
- El WebView no estaba comunicándose correctamente con el host

### Soluciones Implementadas

#### 1. **Mejorado el Script de PowerShell** (MainWindow.xaml.cs)
- ✅ Mejor manejo de caracteres especiales en las notificaciones
- ✅ Template XML mejorado con soporte para acciones
- ✅ Escape correcto de comillas y caracteres problemáticos
- ✅ Timeout de 5 segundos (antes 3 segundos)
- ✅ Mejor logging para debugging

#### 2. **Mejorado el JavaScript** (wwwroot/js/chat.js)
- ✅ **Cambio crítico**: Ahora siempre notifica al host, incluso si la app tiene focus
- ✅ Agregados 3 métodos de fallback para enviar notificaciones:
  1. Host bridge sync (más directo)
  2. WebView postMessage
  3. Servidor fallback
- ✅ Logging detallado para debugging
- ✅ Verificación de disponibilidad del bridge

#### 3. **Mejorada la Inicialización del Bridge** (MainWindow.xaml.cs)
- ✅ Inyección de código después de navegación completa
- ✅ Confirmación de que el bridge está listo
- ✅ Debug logging para verificar disponibilidad

## 🚀 Cómo Probar las Notificaciones

### Caso 1: App Minimizada
1. Abre ChatAgenda
2. Minimiza la ventana a la bandeja
3. Recibe un mensaje desde otro cliente
4. **Verás una notificación Toast en la esquina inferior derecha** ✅

### Caso 2: App Cerrada
1. Abre ChatAgenda
2. Cierra la app completamente (click en X)
3. Recibe un mensaje desde otro cliente
4. **Verás una notificación Toast del sistema** ✅

### Caso 3: Debugging
Abre la consola del desarrollador (F12) y busca logs con `[NOTIFICACIÓN]`:
```
[NOTIFICACIÓN] Intentando notificar: "Juan" - "Hola"
[NOTIFICACIÓN] Usando método 1: Host bridge sync
✓ Notificación Windows mostrada: Juan
```

## 📊 Métodos de Notificación (en orden de preferencia)

| Método | Tipo | Velocidad | Confiabilidad |
|--------|------|-----------|--------------|
| Host Bridge Sync | C# directo | Muy rápido | Muy alta |
| WebView PostMessage | Mensaje inter-process | Rápido | Alta |
| Servidor Fallback | HTTP | Normal | Media |
| NotifyIcon BalloonTip | Windows Forms | Normal | Baja |

## 🔧 Archivos Modificados

### `Client.WPF/MainWindow.xaml.cs`
- Mejorado `ShowWindowsNotification()` con mejor escaping
- Agregado inyección de script post-navegación
- Mejor logging y debugging

### `wwwroot/js/chat.js`
- **Cambio importante**: `triggerNotification()` ahora siempre notifica al host
- Reescrito `notifyHost()` con 3 métodos de fallback
- Agregado logging detallado

## 📝 Notas Técnicas

- **Requisito**: PowerShell debe estar en PATH (viene por defecto en Windows)
- **Permisos**: La app puede necesitar permisos de administrador para crear algunas notificaciones
- **Windows 10/11**: Las notificaciones Toast funcionan en ambas versiones
- **Background**: La app ejecuta en segundo plano incluso cuando está "cerrada" (si no sale explícitamente)

## ⚠️ Troubleshooting

### Si las notificaciones no aparecen:

1. **Verifica los logs en consola (F12)** - Busca `[NOTIFICACIÓN]`
2. **Abre Configuración > Sistema > Notificaciones** - Verifica que ChatAgenda está permitido
3. **Reinicia la app** - Algunos cambios requieren reinicio
4. **Prueba desde cmd**: 
   ```cmd
   powershell -NoProfile -Command "Add-Type -AssemblyName System.Runtime.WindowsRuntime; [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null; Write-Host 'Test OK'"
   ```

## 📦 Ejecutable Nuevo

**Ubicación**: `artifacts/publish/win-x64/EmpresaChat.exe`

**Tamaño**: ~155 MB

**Para distribuir**: Simplemente copia la carpeta `win-x64` a otros usuarios.

---

**Versión mejorada: v2.0** ✅
