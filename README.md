# ChatAgenda - Servidor de Oficina Local + Sincronización Google Calendar

**ChatAgenda** es una solución empresarial local diseñada para coordinar la comunicación interna (Chat LAN) y la agenda compartida de la oficina, permitiendo la visualización y edición remota desde dispositivos móviles a través de una única cuenta corporativa de Google Calendar.

La aplicación elimina la necesidad de crear cuentas individuales de Google para cada empleado, y mantiene el canal de mensajería (chat) corriendo 100% de manera interna dentro de la red LAN de la oficina (sin dependencia de internet).

---

## 🏗️ Arquitectura General

```text
                    INTERNET
                       │
                       │
              Google Calendar
              (Cuenta de Empresa)
                       ▲
                       │
             Sincronizador Google (SyncService)
                       │ (API HTTPS)
              ┌────────┴────────┐
              │ SERVIDOR LOCAL  │
              │ (SQLite, .NET8) │
              │                 │
              │ - Usuarios LAN  │
              │ - Chat SignalR  │
              │ - Agenda Local  │
              └────────┬────────┘
                       │ (Red Local / LAN)
           ┌───────────┼───────────┐
           ▼           ▼           ▼
       PC Oficina  PC Oficina  PC Oficina
       (WPF EXE)   (WPF EXE)   (WPF EXE)
```

---

## 🚀 Guía de Inicio Rápido (Servidor Local)

### 1. Requisitos Previos
* Tener instalado el **.NET 8 SDK** en la computadora que actuará como servidor.
* Windows, Linux o macOS en la red local.

### 2. Compilar e Iniciar el Servidor
Ejecuta los siguientes comandos en la raíz del proyecto para restaurar paquetes, compilar e iniciar la aplicación:

```bash
dotnet build
dotnet run --launch-profile http
```

Por defecto, el servidor web local estará escuchando en:
* **http://localhost:5002** (o la dirección IP local asignada en la red de la oficina, por ejemplo, `http://192.168.1.100:5002`).

### 3. Credenciales de Prueba (Semilla Automática)
Al iniciar por primera vez, el servidor crea una base de datos local SQLite (`chatagenda.db`) y genera los siguientes usuarios de demostración listos para usar:

| Usuario | Contraseña | Rol | Departamento |
| :--- | :--- | :--- | :--- |
| **admin** | `admin123` | Administrador | Administración |
| **ana** | `ana123` | Empleado | Ventas |
| **juan** | `juan123` | Empleado | Desarrollo |
| **pedro** | `pedro123` | Supervisor | Ventas |

---

## 📅 Sincronización con Google Calendar

El sistema sincroniza automáticamente los eventos en segundo plano en intervalos de 30 segundos, o de manera instantánea cuando un usuario realiza un cambio local en la agenda.

### Pasos para Configurar la Cuenta de Google Única:

1. **Crear un Proyecto en Google Cloud Console:**
   * Entra a [Google Cloud Console](https://console.cloud.google.com/).
   * Crea un nuevo proyecto llamado `ChatAgenda`.

2. **Habilitar Google Calendar API:**
   * Ve a la sección **Biblioteca de API** (API Library).
   * Busca "Google Calendar API" y haz clic en **Habilitar**.

3. **Crear una Cuenta de Servicio (Service Account):**
   * Dirígete a **IAM y administración** > **Cuentas de servicio**.
   * Haz clic en **Crear cuenta de servicio**, escribe un nombre (ej. `sincronizador-agenda`) y presiona Crear.
   * Copia el correo electrónico generado para la cuenta de servicio (tendrá un formato como `sincronizador@project-id.iam.gserviceaccount.com`).

4. **Generar la Clave JSON:**
   * Haz clic sobre la cuenta de servicio recién creada, entra a la pestaña **Claves** (Keys).
   * Selecciona **Agregar clave** > **Crear clave nueva** en formato **JSON**.
   * Descarga el archivo `.json` en tu computadora (contiene las claves de cifrado privadas).

5. **Compartir tu Agenda Corporativa:**
   * Entra a [Google Calendar](https://calendar.google.com/) con la cuenta corporativa de la empresa (ej: `agenda@empresa.com`).
   * En la configuración de la agenda que deseas compartir, ve a **Compartir con personas o grupos específicos**.
   * Agrega el correo de la cuenta de servicio copiado en el paso 3 y otórgale permisos de **"Realizar cambios y administrar el uso compartido"**.

6. **Activar en el Panel de Administración:**
   * Inicia sesión en ChatAgenda como `admin` y entra al panel de **Administración**.
   * Introduce el **ID del Calendario** (el correo de la cuenta corporativa o `primary`).
   * Activa la casilla **Habilitar Sincronización Bidireccional**.
   * Abre el archivo `.json` descargado en el paso 4, copia todo su contenido de texto y pégalo en el cuadro de **Credenciales de Cuenta de Servicio (JSON)**.
   * Haz clic en **Guardar Configuración**. Puedes presionar **Forzar Sync** para sincronizar inmediatamente.

---

## 🔒 Resolución de Conflictos y Auditoría

Para garantizar la coherencia en un entorno de red local y móvil híbrido, el sincronizador sigue las siguientes directrices:
* **Última modificación gana:** Si un evento es modificado simultáneamente en Google Calendar (celular) y en el servidor local (oficina), el sistema comparará las marcas de tiempo UTC (`LastModified`). El cambio con la fecha más reciente sobreescribirá al otro.
* **Registro de Auditoría:** En el Panel de Administración, el sistema mantiene una terminal en tiempo real con el historial de todas las sincronizaciones, modificaciones, creaciones y eliminaciones de eventos, indicando el usuario responsable (ej: `"Juan Pérez"` o `"Google Mobile User"`) y cómo se resolvió cualquier conflicto.

---

## 🖥️ Cliente Windows Desktop (`EmpresaChat.exe`)

El directorio `Client.WPF/` contiene una aplicación nativa Windows C# WPF que envuelve nuestra interfaz en un ejecutable de escritorio utilizando **Microsoft WebView2 Runtime**.

### Características del Cliente de Escritorio:
* **Configuración de IP de Servidor LAN:** El cliente incluye un botón superior de configuración (`⚙️ Servidor`) que permite a cualquier usuario de la oficina ingresar la IP del servidor local en la red (ej: `http://192.168.1.120:5002`) y conectarse instantáneamente. Esta dirección se guarda localmente en un archivo de texto de configuración persistente.
* **Inicio Seguro:** La sesión de usuario se mantiene de manera segura mediante cookies locales en el contenedor del navegador.

### Compilar y Publicar el Ejecutable:
Para compilar la aplicación y generar el archivo `.exe` ejecutable para los empleados, abre tu terminal y ejecuta:

```bash
cd Client.WPF
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

El ejecutable standalone se generará en:
`Client.WPF/bin/Release/net8.0-windows/win-x64/publish/EmpresaChat.exe`

---

## 🛠️ Tecnologías y Estructura del Código

* **Backend:** ASP.NET Core Web API con .NET 8.
* **Base de Datos:** EF Core + SQLite (Ligero y libre de instalaciones de motores externos en el servidor local).
* **Mensajería LAN:** SignalR (WebSockets y WebSockets fallback) para comunicación bidireccional inmediata en tiempo real.
* **Seguridad:** Hashing PBKDF2 de contraseñas con sal aleatoria de 128 bits.
* **Frontend:** Vanilla JS (ES6) + CSS Glassmorphism premium (sin dependencias de frameworks pesados o Tailwind, ideal para renderizado rápido y minimalista).


dotnet publish Client.WPF/Client.WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section