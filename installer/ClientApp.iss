; ChatAgenda — Instalador del CLIENTE
; Solo instala la app WPF cliente. No incluye servidor.

#define MyAppName      "ChatAgenda Cliente"
#define MyAppVersion   "2.1"
#define MyAppPublisher "ChatAgenda"
#define MyAppExeName   "EmpresaChat.exe"

[Setup]
AppId                    ={{A1B2C3D4-E5F6-7890-ABCD-EF0987654321}
AppName                  ={#MyAppName}
AppVersion               ={#MyAppVersion}
AppPublisher             ={#MyAppPublisher}
DefaultDirName           ={autopf}\ChatAgenda\Cliente
DefaultGroupName         =ChatAgenda
OutputBaseFilename       =ChatAgenda_Cliente_Setup
Compression              =lzma2/ultra64
SolidCompression         =yes
WizardStyle              =modern
PrivilegesRequired       =lowest
UninstallDisplayIcon     ={app}\{#MyAppExeName}
SetupIconFile            ={#SourcePath}\..\Client.WPF\app.ico
ArchitecturesInstallIn64BitMode=x64
MinVersion               =10.0

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "*.WebView2\*,*.pdb"

[Icons]
Name: "{group}\ChatAgenda";            Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar ChatAgenda Cliente"; Filename: "{uninstallexe}"
Name: "{commondesktop}\ChatAgenda";    Filename: "{app}\{#MyAppExeName}"
Name: "{userstartup}\ChatAgenda";      Filename: "{app}\{#MyAppExeName}"; Comment: "Iniciar con Windows"

[Run]
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Abrir ChatAgenda"; \
    Flags: nowait postinstall skipifsilent

[Messages]
WelcomeLabel2=Este instalador configura ChatAgenda como cliente en esta PC.%n%nNecesitarás la dirección IP del servidor de la oficina para conectarte.
