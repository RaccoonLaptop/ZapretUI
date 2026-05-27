; Zapret UI — Windows installer (Inno Setup 6)
; Build: ..\build-installer.ps1

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef SourceDir
  #define SourceDir "..\ZapretUI-installer-staging"
#endif

#ifndef OutputDir
  #define OutputDir "..\ZapretUI-dist"
#endif

[Setup]
AppId={{8F4E2A91-3C7D-4B6E-9F12-0A1B2C3D4E5F}
AppName=Zapret UI
AppVersion={#AppVersion}
AppVerName=Zapret UI {#AppVersion}
AppPublisher=Niko
AppPublisherURL=https://github.com/RaccoonLaptop/ZapretUI
AppSupportURL=https://github.com/RaccoonLaptop/ZapretUI
AppUpdatesURL=https://github.com/RaccoonLaptop/ZapretUI/releases
DefaultDirName={localappdata}\ZapretUI
DisableDirPage=yes
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=ZapretUI-Setup
SetupIconFile=..\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardImageFile=..\Assets\installer-wizard.bmp
WizardSmallImageFile=..\Assets\installer-small.bmp
WizardImageBackColor=$10121A
WizardImageStretch=no
UninstallDisplayIcon={app}\ZapretUI.exe
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=..\LICENSE

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительно:"; Flags: checkedonce

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Zapret UI"; Filename: "{app}\ZapretUI.exe"; Comment: "Zapret UI — обход блокировок Discord, YouTube и др."
Name: "{autodesktop}\Zapret UI"; Filename: "{app}\ZapretUI.exe"; Tasks: desktopicon; Comment: "Zapret UI"

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Scripts\bootstrap-zapret.ps1"" -TargetDir ""{app}\zapret"""; StatusMsg: "Скачивание zapret (Flowseal)..."; Flags: waituntilterminated
Filename: "powershell.exe"; Parameters: "-NoProfile -WindowStyle Hidden -Command ""Start-Process -FilePath '{app}\ZapretUI.exe' -Verb RunAs"""; Description: "Запустить Zapret UI"; Flags: nowait postinstall skipifsilent shellexec

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"; ValueType: string; ValueName: "{app}\ZapretUI.exe"; ValueData: "RUNASADMIN"; Flags: uninsdeletevalue

[UninstallDelete]
Type: filesandordirs; Name: "{app}\zapret"
Type: filesandordirs; Name: "{app}\Assets"
Type: filesandordirs; Name: "{app}\Scripts"
Type: files; Name: "{app}\settings.json"

[Code]
function StopZapretBeforeUninstall(): Boolean;
var
  ResultCode: Integer;
  ScriptPath: String;
begin
  ScriptPath := ExpandConstant('{app}\Scripts\stop-zapret-components.ps1');
  if not FileExists(ScriptPath) then
  begin
    Result := True;
    Exit;
  end;
  Result := ShellExec('runas',
    ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "' + ScriptPath + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDir: String;
begin
  if CurUninstallStep = usUninstall then
    StopZapretBeforeUninstall();

  if CurUninstallStep = usPostUninstall then
  begin
    AppDir := ExpandConstant('{app}');
    if DirExists(AppDir) then
      DelTree(AppDir, True, True, True);
  end;
end;
