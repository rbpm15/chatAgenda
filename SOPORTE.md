# 🆘 Soporte y Troubleshooting - ChatAgenda v2.0

## ❓ Preguntas Frecuentes

### P: ¿Dónde está el ejecutable?
**R:** 
```
C:\Users\OKOKIMI\Documents\chatAgenda\artifacts\publish\win-x64\EmpresaChat.exe
```

### P: ¿Necesito instalar .NET?
**R:** No. El ejecutable incluye .NET 8 Runtime autocontenido.

### P: ¿Funciona en Windows 7?
**R:** No. Solo Windows 10/11. Necesita .NET 8.

### P: ¿Cuál es el tamaño del archivo?
**R:** 148 MB (incluye todo lo necesario).

### P: ¿Puedo ejecutar desde USB?
**R:** Sí, copia la carpeta win-x64 completa a un USB y ejecuta desde ahí.

### P: ¿Las notificaciones funcionan con No Molestar activo?
**R:** Sí, van al Action Center (Windows + A).

---

## 🐛 Problemas Comunes y Soluciones

### ❌ Problema: La app no inicia

**Síntomas:**
- Haces clic en EmpresaChat.exe pero nada pasa
- O ves una ventana pero cierra al instante

**Soluciones (en orden):**

#### 1. Verifica antivirus
```
- Algunos antivirus bloquean aplicaciones desconocidas
- Agrega C:\...\EmpresaChat.exe a excepciones
- Temporalmente desactiva el antivirus y prueba
```

#### 2. Ejecuta como Administrador
```
- Click derecho en EmpresaChat.exe
- "Ejecutar como administrador"
```

#### 3. Revisa los logs
```
- Abre PowerShell como Admin
- Ejecuta: C:\...\EmpresaChat.exe
- Verifica si hay errores en consola
```

#### 4. Verifica que Windows 10/11 está actualizado
```
Configuración > Sistema > Información del sistema
Windows 10 versión 1909 o superior
```

---

### ❌ Problema: No veo notificaciones

**Síntomas:**
- La app funciona pero sin notificaciones
- Envío mensajes pero no aparece nada

**Soluciones (en orden):**

#### 1. Verifica que está habilitado en Settings
```
Configuración > Sistema > Notificaciones
Busca: ChatAgenda o EmpresaChat
Verifica que el toggle está ACTIVADO (azul)
```

#### 2. Verifica que No Molestar no está activo
```
Configuración > Sistema > Notificaciones > No molestar
Si está activo, las notificaciones van a Action Center (Windows + A)
```

#### 3. Ejecuta el script de diagnóstico
```powershell
cd C:\Users\OKOKIMI\Documents\chatAgenda
.\test_notifications.ps1
```

Si ves una notificación de prueba → Sistema OK
Si NO ves notificación → Problema en Windows

#### 4. Verifica los logs en consola (F12)
```
Abre la app > Presiona F12 > Console
Envía un mensaje > Busca [NOTIFICACIÓN]

Deberías ver:
[NOTIFICACIÓN] Intentando notificar: "Juan" - "Hola"
[NOTIFICACIÓN] Usando método 1: Host bridge sync
✓ Notificación Windows mostrada: Juan
```

Si ves errores aquí → Problema en la app

#### 5. Reinicia la app
```
- Cierra EmpresaChat.exe completamente
- Reabre
- Prueba nuevamente
```

---

### ❌ Problema: Las notificaciones son lentas

**Síntomas:**
- Envío mensaje, espero 2-3 segundos antes de ver notificación
- O a veces no aparecen

**Explicación:**
Las notificaciones usan PowerShell, que tarda ~500ms-1seg en ejecutarse.

**Soluciones:**

#### 1. Normal
```
< 1 segundo = Esperado (método 1 o 2)
1-2 segundos = Normal (método 3, servidor)
> 2 segundos = Considera reiniciar app
```

#### 2. Si es muy lento (> 3 segundos)
```
1. Ejecuta test_notifications.ps1
2. Si test también es lento = Problema Windows
3. Actualiza Windows (Windows Update)
4. Reinicia tu PC
```

---

### ❌ Problema: PowerShell da error

**Síntomas:**
```
"No se puede cargar el archivo ... porque la ejecución de scripts está deshabilitada"
```

**Solución:**
```powershell
# Ejecuta como ADMIN en PowerShell:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

### ❌ Problema: La app se ve pixelada o borrosa

**Síntomas:**
- Interfaz se ve distorsionada

**Solución:**
```
1. Cierra la app
2. Click derecho en EmpresaChat.exe > Propiedades
3. Pestaña: Compatibilidad
4. Desactiva: "Reducir brillo de colores"
5. Activa: "Ejecutar este programa en modo de pantalla completa"
6. OK
```

---

### ❌ Problema: La app no se conecta al servidor

**Síntomas:**
- No se conecta a http://localhost:5002 (servidor local)
- O no se conecta a la IP del servidor remoto

**Soluciones:**

#### Servidor Local (localhost:5002)
```
1. Verifica que el servidor está ejecutándose
2. Abre navegador > http://localhost:5002
3. Debería cargarse la página
```

#### Servidor Remoto
```
1. Verifica la IP correcta: ipconfig /all
2. Verifica que no hay firewall bloqueando puerto 5002
3. Prueba: ping <IP_DEL_SERVIDOR>
4. Prueba: telnet <IP> 5002 (desde PowerShell: Test-NetConnection)
```

---

### ❌ Problema: Errores SSL/HTTPS

**Síntomas:**
```
"La conexión no es privada"
"NET::ERR_CERT_AUTHORITY_INVALID"
```

**Solución para desarrollo:**
```
1. Usa http:// en lugar de https://
2. O acepta el certificado en el navegador primero
3. Luego vuelve a intentar en la app
```

---

## 📊 Tabla de Diagnóstico

| Síntoma | Causa Probable | Solución |
|---------|---|---|
| App no inicia | Antivirus / Permisos | Antivirus: agregar excepción / Ejecutar como Admin |
| Sin notificaciones | Windows Settings | Activar en Configuración > Notificaciones |
| Notificaciones lentas | PowerShell lento | Normal (< 1 seg) / Actualizar Windows |
| Errores en consola (F12) | Código JavaScript | Reiniciar app / Limpiar cache (F12) |
| No se conecta | Red / Firewall | Verificar servidor / Firewall |
| App se congela | Conexión lenta | Esperar / Comprobar conexión |

---

## 🔍 Debugging Avanzado

### Activar Debug en Consola

**Paso 1: Abrir F12**
```
Con la app abierta, presiona F12
```

**Paso 2: Limpiar logs**
```
console.clear()
```

**Paso 3: Enviar mensaje desde otro cliente**

**Paso 4: Buscar en los logs**
```
[NOTIFICACIÓN]      ← Logs de notificaciones
[BRIDGE]            ← Logs del bridge
[TRIGGER]           ← Logs de activación
```

### Logs que Deberías Ver

**Esperado (Exitoso):**
```
[TRIGGER] document.hidden=true, hasFocus=false, isCurrentChat=false
[NOTIFICACIÓN] Intentando notificar: "Juan" - "Hola"
[NOTIFICACIÓN] Usando método 1: Host bridge sync
✓ Notificación Windows mostrada: Juan
```

**Fallback (Funciona igual):**
```
[NOTIFICACIÓN] Error con host bridge sync: ...
[NOTIFICACIÓN] Usando método 2: WebView postMessage
[NOTIFICACIÓN] Notificación enviada (método 2)
```

**Error:**
```
[NOTIFICACIÓN] No se pudo notificar por ningún método
```
Si ves esto → Ejecuta test_notifications.ps1

---

## 🆙 Verificación de Versión

### Verificar qué versión estás usando
```powershell
# En PowerShell:
Get-Item "C:\Users\OKOKIMI\Documents\chatAgenda\artifacts\publish\win-x64\EmpresaChat.exe" | Select-Object VersionInfo
```

Deberías ver:
```
ProductVersion: 1.0
FileVersion: 1.0
```

---

## 📋 Información para Reportar Problemas

Si necesitas reportar un problema, recopila:

```
1. Versión de Windows: (Win + Pausa)
2. Versión de .NET: dotnet --version
3. Versión de PowerShell: $PSVersionTable.PSVersion
4. Logs de consola (F12): [copia todo]
5. Salida de test_notifications.ps1
6. Mensaje de error exacto
7. Qué fue lo último que hiciste antes del error
```

---

## 📞 Obtener Ayuda

### Si el problema persiste:

1. **Revisa la documentación**
   - [README_NOTIFICACIONES.md](README_NOTIFICACIONES.md)
   - [GUIA_PRUEBAS.md](GUIA_PRUEBAS.md)
   - [NOTIFICACIONES_MEJORAS.md](NOTIFICACIONES_MEJORAS.md)

2. **Ejecuta el diagnóstico**
   ```powershell
   .\test_notifications.ps1
   ```

3. **Verifica los logs (F12)**
   - Consola: Busca `[NOTIFICACIÓN]`
   - Network: Verifica conexiones HTTP

4. **Reinicia todo**
   - Cierra la app
   - Reinicia tu PC
   - Reabre

---

## ✅ Checklist de Verificación

- [ ] Windows 10/11 está actualizado
- [ ] Antivirus no bloquea la app
- [ ] ChatAgenda está permitido en Notificaciones
- [ ] PowerShell está disponible
- [ ] test_notifications.ps1 funciona
- [ ] Puedes conectarte al servidor
- [ ] Ves logs en consola (F12)
- [ ] Recibiste una notificación de prueba

Si todo está marcado ✓ → Tu sistema está OK

---

## 🎯 Resumen

- **Problema más común:** Notificaciones deshabilitadas en Windows
- **Solución más rápida:** `.\test_notifications.ps1`
- **Verificación más importante:** Logs en F12 (Developer Tools)
- **Reinicio más efectivo:** Reiniciar PC

---

**¿Problema resuelto?** ✅  
Si no → Revisa [DOCUMENTACION.md](DOCUMENTACION.md) para más recursos

Versión: 2.0  
Última actualización: 12/07/2026
