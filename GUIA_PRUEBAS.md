# 🧪 GUÍA DE PRUEBA - Verificar Notificaciones

## ✅ Verificación Pre-Prueba

Antes de probar, asegúrate que:

### 1. Windows está configurado correctamente
```powershell
# Ejecuta esto en PowerShell como ADMIN si las notificaciones no aparecen:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 2. PowerShell está disponible
```powershell
powershell -NoProfile -Command "Write-Host 'PowerShell OK'"
```
Debe mostrar: `PowerShell OK`

### 3. Notificaciones están habilitadas
```
Configuración (Win + I) > Sistema > Notificaciones > 
Recibe notificaciones de estas aplicaciones > Busca ChatAgenda o EmpresaChat
```

---

## 📋 Prueba 1: Script de Diagnóstico

### Paso 1: Ejecutar el script
```powershell
cd C:\Users\OKOKIMI\Documents\chatAgenda
.\test_notifications.ps1
```

### Paso 2: Verificar salida
```
✓ System.Runtime.WindowsRuntime disponible
✓ Windows.UI.Notifications disponible
✓ Notificación enviada exitosamente
```

### Resultado esperado
- Verás una notificación Toast en tu pantalla
- Mostrará: "ChatAgenda - Prueba"
- Durará ~3 segundos

---

## 🧪 Prueba 2: App Minimizada

### Paso 1: Abrir la app
```
Haz doble clic en: artifacts\publish\win-x64\EmpresaChat.exe
```

### Paso 2: Configurar
- Elige Servidor o Cliente
- Configura como de costumbre

### Paso 3: Minimizar
- Haz clic en el botón `-` (minimizar)
- O presiona Win + D (ocultar)

### Paso 4: Recibir mensaje
- Desde otro cliente/usuario, envía un mensaje
- Espera 1-2 segundos

### Resultado esperado ✅
- Verás una notificación Toast en la esquina inferior derecha
- **Dice**: Nombre del usuario + Texto del mensaje
- **Dura**: ~5 segundos

### Si NO ves notificación ❌
```
1. Abre F12 (Developer Tools)
2. Busca logs con: [NOTIFICACIÓN]
3. Verifica que diga: "Notificación Windows mostrada"
4. Si falla, ejecuta test_notifications.ps1
```

---

## 🧪 Prueba 3: App Completamente Cerrada

### Paso 1: Cerrar la app
- Cierra EmpresaChat.exe (botón X)
- Verifica que NO está en bandeja del sistema

### Paso 2: Enviar mensaje
- Desde otro cliente, envía un mensaje
- Espera 2-3 segundos

### Resultado esperado ✅
- Verás una notificación Toast
- Aparecerá aunque la app esté cerrada
- Se guardará en Action Center

### Ver en Action Center
```
Presiona: Windows + A
```

### Si NO ves notificación ❌
```
1. Verifica que "No Molestar" no esté activo
2. Abre Action Center (Windows + A)
3. Busca ahí la notificación
4. Si no está, ejecuta test_notifications.ps1
```

---

## 🧪 Prueba 4: Debugging con Consola (F12)

### Paso 1: Abrir herramientas de desarrollo
- Con la app ejecutándose, presiona **F12**
- O click derecho > Inspeccionar

### Paso 2: Ir a Consola
- Tab "Console"
- Limpiar (Ctrl + L)

### Paso 3: Enviar un mensaje
- Desde otro cliente
- Observa la consola

### Qué buscar ✅
```
[NOTIFICACIÓN] Intentando notificar: "Juan" - "Hola"
[NOTIFICACIÓN] Usando método 1: Host bridge sync
✓ Notificación Windows mostrada: Juan
```

### Si solo ves errores ❌
```
[NOTIFICACIÓN] Error con host bridge sync: ...
[NOTIFICACIÓN] Usando método 2: WebView postMessage
[NOTIFICACIÓN] Usando método 3: Servidor fallback
```

Esto significa que está usando fallbacks, lo cual está bien (la notificación debería funcionar igual).

---

## 📊 Interpretación de Logs

### Mensaje: `[NOTIFICACIÓN] Usando método 1: Host bridge sync`
✅ **Excelente** - Método más rápido y confiable

### Mensaje: `[NOTIFICACIÓN] Usando método 2: WebView postMessage`
✅ **Bueno** - Segundo método más confiable

### Mensaje: `[NOTIFICACIÓN] Usando método 3: Servidor fallback`
✅ **Funciona** - Tercer método, un poco más lento

### Mensaje: `[NOTIFICACIÓN] No se pudo notificar por ningún método`
❌ **Error** - Ejecuta test_notifications.ps1

### Mensaje: `Error con Windows Notifications Toast`
⚠️ **Fallback usado** - Notificación probablemente funcionó de otro modo

---

## 🔍 Verificación Manual en Windows

### Verificar que ChatAgenda esté en Notificaciones

**Opción 1: Configuración**
```
1. Win + I (Configuración)
2. Sistema > Notificaciones
3. Desplázate a "Recibe notificaciones de estas aplicaciones"
4. Busca "ChatAgenda" o "EmpresaChat"
5. Verifica que esté ACTIVADO (azul)
```

**Opción 2: Comando PowerShell**
```powershell
Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings" -ErrorAction SilentlyContinue
```

### Ver notificaciones previas
```
1. Presiona Windows + A (Action Center)
2. Busca notificaciones de ChatAgenda
3. Si está en "Antiguo", las notificaciones están llegando
```

---

## ✅ Checklist de Verificación

- [ ] PowerShell está disponible
- [ ] APIs de Windows funcionan (test_notifications.ps1)
- [ ] ChatAgenda está permitido en Notificaciones
- [ ] App se ejecuta correctamente
- [ ] Notificación aparece cuando app tiene focus (F12 muestra logs)
- [ ] Notificación aparece cuando app está minimizada
- [ ] Notificación aparece cuando app está cerrada
- [ ] Notificaciones aparecen en Action Center (Windows + A)
- [ ] Logs en consola muestran "Método 1" (Host bridge sync)

---

## 🆘 Si Algo Falla

### Escenario 1: No hay notificación con app minimizada
```
1. Verifica F12 > Console
2. Busca [NOTIFICACIÓN]
3. Si no ve logs, recarga la página (F5)
4. Si ve logs pero no hay notificación, ejecuta test_notifications.ps1
5. Si test_notifications.ps1 funciona pero app no, el problema es la comunicación WebView
```

### Escenario 2: No hay notificación con app cerrada
```
1. Abre Action Center (Windows + A)
2. Si ves la notificación ahí = FUNCIONA (estás en "No Molestar")
3. Si no ves nada = Ejecuta test_notifications.ps1
4. Si test_notifications.ps1 funciona, verifica que la app ejecuta en background
```

### Escenario 3: test_notifications.ps1 falla
```
1. Verifica que estás en Windows 10/11
2. Ejecuta como ADMIN
3. Comprueba que PowerShell está en PATH:
   where.exe powershell
4. Si falla, actualiza Windows
```

### Escenario 4: App no se ejecuta
```
1. Verifica que antivirus no bloquea EmpresaChat.exe
2. Agrega a exclusiones si es necesario
3. Ejecuta como ADMIN
4. Revisa los logs en consola
```

---

## 📈 Métricas de Éxito

| Métrica | Esperado | Tu resultado |
|---------|----------|--------------|
| Notificación minimizada | < 1 seg | ___ |
| Notificación cerrada | 1-3 seg | ___ |
| test_notifications.ps1 | ✅ Exitoso | ___ |
| Logs en F12 | [NOTIFICACIÓN] | ___ |
| Notificaciones en Action Center | ✅ Sí | ___ |

---

## 🎯 Resultado Final

Si todo funciona:
```
✅ Notificaciones minimizada:  OK
✅ Notificaciones cerrada:     OK
✅ Logging en consola:         OK
✅ Action Center:              OK
✅ Script de diagnóstico:      OK
```

**¡Tu aplicación está funcionando correctamente!** 🚀

---

**Última verificación**: 12/07/2026  
**Versión**: 2.0  
**Plataforma**: Windows 10/11 x64
