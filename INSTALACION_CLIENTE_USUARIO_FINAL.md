# 🚀 INSTRUCCIONES PARA INSTALAR CHATAGENDA CLIENTE

## 📋 Pasos Rápidos

### 1️⃣ DESCARGAR EL CLIENTE

**Opción A: Versión Portátil (Recomendado)**
```
Archivo: ChatAgendaCliente-1.0.0-portable.zip
Ubicación: Output\
Tamaño: 1.85 MB
```

**Opción B: Instalador (Si tienes InnoSetup)**
```
Ejecuta: build-cliente.bat
Resultado: ChatAgendaCliente-1.0.0.exe
```

---

### 2️⃣ OBTENER LA IP DEL SERVIDOR

Pregunta al administrador cuál es la IP y puerto del servidor ChatAgenda.

**Debe ser similar a:**
```
http://192.168.1.100:5002
http://10.0.0.50:5002
http://192.168.99.1:5002
Etc...
```

---

### 3️⃣ INSTALAR LA DEPENDENCIA: .NET 8.0 Runtime

**Si aún no lo tienes:**

1. Abre navegador y ve a: https://dotnet.microsoft.com/download/dotnet/8.0
2. Descarga **".NET 8.0 Runtime"** (no SDK)
3. Ejecuta el instalador
4. Reinicia la computadora

**Verificar si lo tienes:**
```bash
dotnet --version
```
Debe mostrar "8.0.x"

---

### 4️⃣ INSTALAR LA DEPENDENCIA: WebView2 Runtime

**Si aún no lo tienes:**

1. Abre navegador y ve a: https://developer.microsoft.com/en-us/microsoft-edge/webview2/
2. Haz click en "Download"
3. Ejecuta el instalador "WebView2Runtime_x64.exe"
4. Espera a que termine

---

### 5️⃣ INSTALAR CHATAGENDA CLIENTE

#### **Si usas la versión Portátil (ZIP):**
1. Descomprime `ChatAgendaCliente-1.0.0-portable.zip`
2. Copia la carpeta `ChatAgendaCliente-Portable` a donde prefieras (Documentos, Desktop, C:, etc)
3. Ve a esa carpeta
4. Haz click derecho en `server.config.example`
5. Selecciona "Copiar"
6. Presiona Ctrl+V en la misma carpeta
7. Renombra la copia a `server.config`
8. Abre `server.config` con Bloc de Notas
9. Reemplaza `IP_DEL_SERVIDOR` con la IP real (ej: 192.168.1.100)
10. Guarda el archivo
11. Haz doble click en `EmpresaChat.exe` para ejecutar

#### **Si usas el instalador .exe:**
1. Ejecuta `ChatAgendaCliente-1.0.0.exe`
2. Sigue el wizard de instalación
3. Durante la instalación, marca si deseas:
   - Icono en el escritorio
   - Auto-inicio con Windows
4. Haz click "Instalar"
5. Cuando termine, se ejecutará automáticamente
6. Configura la IP del servidor (ver Paso 6)

---

### 6️⃣ CONFIGURAR LA IP DEL SERVIDOR

**IMPORTANTE: El client debe saber dónde está el servidor**

#### **Primer inicio (si no hay server.config):**
- Verás una ventana con opciones
- Haz click en "Cliente LAN"
- Ingresa la IP del servidor (ej: 192.168.1.100)
- Ingresa el puerto (generalmente 5002)
- Haz click "Conectar"

#### **Si ya existe server.config:**
1. Abre el archivo: `%APPDATA%\ChatAgenda\server.config`
2. Busca la línea: `"ServerUrl": "http://IP..."`
3. Reemplaza `IP` con la IP real del servidor
4. Ejemplo:
   ```json
   {
	 "ServerUrl": "http://192.168.1.100:5002",
	 "Mode": "CLIENT",
	 "AutoConnect": true
   }
   ```
5. Guarda y cierra
6. Reinicia EmpresaChat.exe

---

### 7️⃣ USAR CHATAGENDA

#### **Interfaz:**
- Izquierda: Panel de mensajes (chat)
- Derecha: Panel de calendario (eventos)
- Arriba: Barra de herramientas

#### **Notificaciones:**
- Cuando llegas mensajes o hay eventos: ◆ Notificación en bandeja
- Haz click en la notificación → Abre la app
- Haz doble click en el icono de bandeja → Restaura ventana

#### **Bandeja del Sistema (esquina inferior derecha):**
- Icono: Haz click derecho para menú
- Opciones:
  - "Abrir ChatAgenda" - Muestra la ventana
  - "Ver Chat" - Abre pestaña de mensajes
  - "Ver Calendario" - Abre pestaña de eventos
  - "Salir" - Cierra la aplicación

---

---

## 🆘 SOLUCIÓN DE PROBLEMAS

### ❌ "No aparece la ventana"
**Solución:**
1. Verifica que instalaste .NET 8.0 Runtime
2. Verifica que instalaste WebView2 Runtime
3. Si el problema persiste:
   - Abre Administrador de Tareas (Ctrl+Shift+Esc)
   - Busca "EmpresaChat"
   - Si está "En ejecución", haz click derecho → "Finalizar tarea"
   - Ejecuta de nuevo

### ❌ "No se conecta al servidor"
**Solución:**
1. Verifica que la IP en `server.config` es correcta
2. Abre navegador y ve a: `http://IP_DEL_SERVIDOR:5002`
   - Si se abre, el servidor está OK
   - Si no, el servidor está apagado o IP es incorrecta
3. Verifica firewall: quizás bloquea el puerto 5002
4. Verifica conectividad de red: `ping IP_DEL_SERVIDOR`

### ❌ "No recibo notificaciones"
**Solución:**
1. Verifica que alguien está enviando mensajes
2. Abre navegador directamente a `http://IP:5002` y prueba enviar mensaje
3. Si funciona en navegador pero no en cliente:
   - Cierra y reabre EmpresaChat.exe
   - Verifica que el firewall permite la app

### ❌ ".NET 8.0 Runtime no instala"
**Solución:**
1. Ejecuta como Administrador
2. Desactiva antivirus temporalmente
3. Reinicia la computadora
4. Intenta instalar de nuevo

### ❌ WebView2 dice "Ya instalado" pero no funciona
**Solución:**
1. Abre Panel de Control → Programas → Programas y características
2. Busca "WebView2"
3. Si ves múltiples versiones, desinstala todas
4. Descarga e instala de nuevo: https://developer.microsoft.com/en-us/microsoft-edge/webview2/

---

## 📶 CONECTAR DESDE VARIAS MÁQUINAS

Cada máquina con cliente necesita su propio `server.config`:

**Máquina 1:**
```json
{
  "ServerUrl": "http://192.168.1.100:5002",
  "Mode": "CLIENT",
  "AutoConnect": true
}
```

**Máquina 2 (misma red):**
```json
{
  "ServerUrl": "http://192.168.1.100:5002",
  "Mode": "CLIENT",
  "AutoConnect": true
}
```

**Máquina 3 (diferente servidor):**
```json
{
  "ServerUrl": "http://10.0.0.50:5002",
  "Mode": "CLIENT",
  "AutoConnect": true
}
```

Cada máquina tiene su propia copia de `AppData\ChatAgenda\server.config` → puede conectar a servidores diferentes.

---

## 🎯 VERIFICAR QUE TODO FUNCIONA

1. ✅ Instalaste .NET 8.0 Runtime (verificar: `dotnet --version`)
2. ✅ Instalaste WebView2 Runtime
3. ✅ Descomprimiste ChatAgendaCliente
4. ✅ Creaste/editaste `server.config` con IP del servidor
5. ✅ Ejecutaste `EmpresaChat.exe`
6. ✅ Ves la interfaz (calendario + chat)
7. ✅ Puedes escribir mensajes en el chat
8. ✅ Ves eventos en el calendario
9. ✅ Recibes notificaciones cuando llegan mensajes

Si todo está ✅ → ¡**LISTO PARA USAR!** 🎉

---

## 📞 SI SIGUE SIN FUNCIONAR

Proporciona esta información al técnico:

1. Versión de Windows: `winver`
2. Versión de .NET: `dotnet --version`
3. Contenido de `server.config`
4. Captura de pantalla donde falla
5. Archivo de logs si lo hay (busca en `AppData\Roaming\ChatAgenda\`)
6. Resultado de ver `http://IP:5002` en navegador

---

**¡Listo! Ya tienes ChatAgenda Cliente instalado y funcionando.**

Para preguntas o soporte, contacta al administrador del servidor.
