; ChatAgenda — Instalador del SERVIDOR
; Instala el backend ASP.NET + el panel WPF servidor
; y registra el servicio de Windows automáticamente.

#define MyAppName      "ChatAgenda Servidor"
#define MyAppVersion   "2.1"
#define MyAppPublisher "ChatAgenda"
#define MyAppExeName   "ChatAgendaServidor.exe"
#define MyServiceExe   "chatAgenda.exe"

[Setup]
AppId                    ={{B1A2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName                  ={#MyAppName}
AppVersion               ={#MyAppVersion}
AppPublisher             ={#MyAppPublisher}
DefaultDirName           ={autopf}\ChatAgenda\Servidor
DefaultGroupName         =ChatAgenda
OutputBaseFilename       =ChatAgenda_Servidor_Setup
Compression              =lzma2/ultra64
SolidCompression         =yes
WizardStyle              =modern
PrivilegesRequired       =admin
UninstallDisplayIcon     ={app}\{#MyAppExeName}
SetupIconFile            ={#SourcePath}\..\Server.WPF\app.ico
ArchitecturesInstallIn64BitMode=x64
MinVersion               =10.0

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
; Todos los archivos publicados del servidor WPF + backend
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "*.WebView2\*,*.pdb"

[Icons]
Name: "{group}\Panel del Servidor ChatAgenda"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar ChatAgenda Servidor"; Filename: "{uninstallexe}"
Name: "{commondesktop}\ChatAgenda Servidor"; Filename: "{app}\{#MyAppExeName}"

[Run]
; Registrar el servicio de Windows (requiere admin — el instalador ya pide elevación)
Filename: "sc.exe"; \
    Parameters: "create ChatAgendaServer binPath= ""{app}\{#MyServiceExe}"" start= auto DisplayName= ""ChatAgenda Servidor"""; \
    StatusMsg: "Registrando servicio de Windows..."; \
    Flags: runhidden waituntilterminated

Filename: "sc.exe"; \
    Parameters: "description ChatAgendaServer ""Servidor web de mensajería y agenda ChatAgenda"""; \
    Flags: runhidden waituntilterminated

Filename: "sc.exe"; \
    Parameters: "failure ChatAgendaServer reset= 60 actions= restart/5000/restart/10000/restart/30000"; \
    Flags: runhidden waituntilterminated

; Iniciar el servicio inmediatamente
Filename: "sc.exe"; \
    Parameters: "start ChatAgendaServer"; \
    StatusMsg: "Iniciando el servicio..."; \
    Flags: runhidden waituntilterminated

; Abrir el panel WPF al finalizar la instalación
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Abrir el panel del servidor"; \
    Flags: nowait postinstall skipifsilent

[UninstallRun]
; Detener y eliminar el servicio al desinstalar
Filename: "sc.exe"; Parameters: "stop ChatAgendaServer";    Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete ChatAgendaServer";  Flags: runhidden waituntilterminated

[Messages]
WelcomeLabel2=Este instalador configura ChatAgenda como servidor en esta PC.%n%nSe instalará un servicio de Windows que iniciará automáticamente con la PC, permitiendo que los clientes de la red se conecten.
