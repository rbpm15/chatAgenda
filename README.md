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

### 2. Iniciar el Servidor
Para iniciar la base de datos local SQLite y correr el servidor backend/web, ejecuta los siguientes comandos desde la carpeta raíz:

```bash
dotnet build
dotnet run --launch-profile http
```

Por defecto, el servidor web local estará escuchando en:
* **http://localhost:5002** (o la dirección IP local de la máquina en la red local, ej: `http://192.168.1.100:5002`).

### 3. Credenciales de Prueba (Semilla Automática)
Al iniciar por primera vez, el servidor crea una base de datos local SQLite (`chatagenda.db`) y genera los siguientes usuarios de prueba listos para usar:

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

* **Última modificación gana:** Si un evento es modificado simultáneamente en Google Calendar (celular) y en el servidor local (oficina), el sistema comparará las marcas de tiempo UTC (`LastModified`). El cambio con la fecha más reciente sobreescribirá al otro.
* **Registro de Auditoría:** En el Panel de Administración, el sistema mantiene una terminal en tiempo real con el historial de todas las sincronizaciones, modificaciones, creaciones y eliminaciones de eventos, indicando el usuario responsable (ej: `"Juan Pérez"` o `"Google Mobile User"`) y cómo se resolvió cualquier conflicto.

---

## 🖥️ Cliente Windows Desktop (`EmpresaChat.exe`)

El directorio `Client.WPF/` contiene una aplicación nativa Windows C# WPF que encapsula la interfaz web para los empleados. 

### Características del Cliente de Escritorio:
* **CefSharp (Chromium Embedded Framework):** La aplicación utiliza **CefSharp** como motor de renderizado interno de Chromium en vez del WebView2 tradicional de Windows. Esto asegura que la aplicación sea totalmente self-contained y no dependa de si el usuario final tiene instalados componentes de Windows Edge actualizados.
* **Configuración de IP de Servidor LAN:** El cliente incluye un botón superior de configuración (`⚙️ Configuración`) que permite a cualquier usuario de la oficina ingresar la IP del servidor local en la red (ej: `http://192.168.1.120:5002`) y conectarse instantáneamente. Esta dirección se guarda localmente en el archivo `client_config.txt` junto al ejecutable.
* **Inicio con Windows (Auto-start):** El instalador configura un acceso directo en la carpeta de Inicio de Windows (`{userstartup}`) apuntando a `EmpresaChat.exe` y definiendo el directorio de trabajo correcto (`WorkingDir`) para que la aplicación se inicie automáticamente en segundo plano (minimizada en el System Tray) al encender la PC.

---

## 🛠️ Empaquetado y Compilación Completa

Para facilitar la distribución de la app, el proyecto contiene un script PowerShell maestro que compila todas las partes del cliente y prepara el instalador del cliente con Inno Setup:

### Generar Instalador del Cliente

Ejecuta el script maestro desde una consola de PowerShell en la raíz del proyecto:

```powershell
.\build_and_package.ps1
```

Este script:
1. Compila y publica la aplicación cliente en modo `Release` autocompilado de 64 bits.
2. Copia todos los binarios integrados de Chromium (CefSharp).
3. Si Inno Setup 6 está instalado, compila el script `installer\ClientApp.iss`.
4. El instalador completo (~160MB debido a las librerías Chromium integradas) se generará en:
   👉 `artifacts\installer\ChatAgenda_Cliente_Setup.exe`

---

## 📁 Estructura del Proyecto

* **`Client.WPF`**: Aplicación de escritorio C# WPF basada en CefSharp.
* **`installer`**: Script de Inno Setup (`ClientApp.iss`) para empaquetado del cliente.
* **`wwwroot`**: Archivos estáticos HTML, CSS, e imágenes de la interfaz web del sistema.
* **`artifacts`**: Carpeta donde se depositan las compilaciones finales y ejecutables.
* **`Servidor ChatAgenda`**: Carpeta utilizada para instalar y ejecutar el servidor.
* **`chatAgenda.csproj`**: Proyecto backend principal ASP.NET Core API.