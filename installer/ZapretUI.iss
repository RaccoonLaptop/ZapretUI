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
DefaultDirName={autopf}\ZapretUI
DisableDirPage=yes
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=ZapretUI-Setup
SetupIconFile=
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
Name: "{autoprograms}\Zapret UI"; Filename: "{app}\ZapretUI.exe"; Comment: "Zapret UI — графический интерфейс для zapret"
Name: "{autodesktop}\Zapret UI"; Filename: "{app}\ZapretUI.exe"; Tasks: desktopicon; Comment: "Zapret UI"

[Run]
Filename: "{app}\ZapretUI.exe"; Description: "Запустить Zapret UI"; Flags: nowait postinstall skipifsilent

[Code]
var
  ZapretPage: TInputDirWizardPage;

function IsValidZapretRoot(const Dir: string): Boolean;
begin
  Result := FileExists(AddBackslash(Dir) + 'service.bat') and
            DirExists(AddBackslash(Dir) + 'bin');
end;

function GetDefaultZapretDir: string;
var
  Desktop, Dir: string;
  FindRec: TFindRec;
begin
  Desktop := ExpandConstant('{userdesktop}');
  if FindFirst(Desktop + '\zapret-discord-youtube*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Dir := Desktop + '\' + FindRec.Name;
          if IsValidZapretRoot(Dir) then
          begin
            Result := Dir;
            Exit;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
  Result := Desktop;
end;

procedure InitializeWizard;
begin
  ZapretPage := CreateInputDirPage(wpWelcome,
    'Папка zapret',
    'Укажите папку zapret-discord-youtube',
    'Выберите каталог, где лежат service.bat и папка bin\ (скачайте с GitHub Flowseal, если ещё нет).' + #13#10 + #13#10 +
    'Zapret UI установится в подпапку ZapretUI внутри неё.',
    False, 'Новая папка');
  ZapretPage.Add('');
  ZapretPage.Values[0] := GetDefaultZapretDir;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Dir: string;
begin
  Result := True;
  if CurPageID = ZapretPage.ID then
  begin
    Dir := AddBackslash(ZapretPage.Values[0]);
    if not IsValidZapretRoot(Dir) then
    begin
      MsgBox(
        'В выбранной папке не найдены service.bat и bin\.' + #13#10 + #13#10 +
        'Скачайте zapret-discord-youtube с GitHub Flowseal и укажите корень этой папки.',
        mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = wpSelectDir);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpReady then
    WizardForm.DirEdit.Text := AddBackslash(ZapretPage.Values[0]) + 'ZapretUI';
end;
