[Setup]
AppName=Client WPF
AppVersion=1.0
DefaultDirName={pf}\ClientWPF
DefaultGroupName=ClientWPF
OutputBaseFilename=ClientWPF_Installer
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Copy all archivos publicados. El directorio de publicación se pasa como variable /DMyAppPublishDir
Source: "{code:GetPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Client WPF"; Filename: "{app}\Client.WPF.exe"

[Run]
Filename: "{app}\Client.WPF.exe"; Description: "Iniciar Client WPF"; Flags: nowait postinstall skipifsilent

; Código para resolver la variable GetPublishDir proporcionada por ISCC /D
[Code]
function GetPublishDir(Param: String): String;
begin
  Result := ExpandConstant('{#MyAppPublishDir}');
end;
