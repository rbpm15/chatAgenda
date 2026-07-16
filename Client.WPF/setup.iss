#define MyAppName "ChatAgenda"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ChatAgenda"
#define MyAppURL "https://github.com/rbpm15/chatAgenda"
#define MyAppExeName "EmpresaChat.exe"
#define SourcePath "bin\x64\Release\net8.0-windows\win-x64"

[Setup]
AppId={{27A28A8B-1E5B-4D3E-9F2A-8B1C2D3E4F5A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
SetupIconFile=
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Iniciar ChatAgenda automáticamente al abrir Windows"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourcePath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "{#SourcePath}\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\WebView2"; DestDir: "{app}\WebView2"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: "{app}\{#MyAppExeName}"; Flags: createvalueifdoesntexist; Tasks: autostart

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  BuildPath: String;
begin
  if CurStep = ssInstall then
  begin
	// Opcional: Compilar antes de instalar si se desea
	// BuildPath := ExpandConstant('{src}\..\..');
	// Exec('dotnet', 'build -c Release', BuildPath, SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
