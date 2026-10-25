; Instalador do Autoclicker Wardogs (Inno Setup 6)
; Compilado pelo publicar.ps1:  ISCC.exe /DAppVersion=3.1.0 instalador\AutoclickerWardogs.iss
;
; Instala na pasta do usuário (sem pedir administrador) para que a
; atualização automática consiga trocar o .exe sozinha.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
; não mude o AppId: é por ele que o Windows reconhece instalações anteriores
AppId={{6F2C8E1A-4B7D-4E39-9A51-3D8C0B7E2F64}
AppName=Autoclicker Wardogs
AppVersion={#AppVersion}
AppVerName=Autoclicker Wardogs {#AppVersion}
AppPublisher=soumarciosantos
AppPublisherURL=https://github.com/soumarciosantos/autoclicker-wardogs
AppSupportURL=https://github.com/soumarciosantos/autoclicker-wardogs/issues
AppUpdatesURL=https://github.com/soumarciosantos/autoclicker-wardogs/releases
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\AutoclickerWardogs
DisableDirPage=yes
DefaultGroupName=Autoclicker Wardogs
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=AutoclickerWardogs-Setup
SetupIconFile=..\src\AutoclickerWardogs\icone.ico
UninstallDisplayIcon={app}\AutoclickerWardogs.exe
UninstallDisplayName=Autoclicker Wardogs
; mesmo nome do Mutex usado pelo programa: o instalador pede para fechá-lo antes
AppMutex=AutoclickerWardogs_unico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x86compatible
MinVersion=6.1sp1

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\dist\AutoclickerWardogs.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Autoclicker Wardogs"; Filename: "{app}\AutoclickerWardogs.exe"
Name: "{autodesktop}\Autoclicker Wardogs"; Filename: "{app}\AutoclickerWardogs.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AutoclickerWardogs.exe"; Description: "{cm:LaunchProgram,Autoclicker Wardogs}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; sobras da atualização automática e os ajustes salvos
Type: files; Name: "{app}\AutoclickerWardogs.exe.old"
Type: files; Name: "{app}\AutoclickerWardogs.exe.new"
Type: filesandordirs; Name: "{userappdata}\AutoclickerWardogs"
