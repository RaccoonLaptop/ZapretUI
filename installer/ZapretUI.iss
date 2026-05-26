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
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\ZapretUI.exe
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=..\LICENSE
InfoBeforeFile=..\packaging\ПРОЧТИ МЕНЯ.txt

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
Filename: "{app}\ZapretUI.exe"; Description: "Запустить Zapret UI"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\zapret"
