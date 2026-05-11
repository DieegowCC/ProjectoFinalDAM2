; SETT Installer Script for InnoSetup
; Professional installer with Service and Portable options

#define MyAppName "SETT"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SETT"
#define MyAppExeAPI "settAPI.exe"
#define MyAppExeAgent "settAGENT.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\publish\installer
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
; Uncomment the next line if you have an icon file
; SetupIconFile=..\assets\sett.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "installService"; Description: "Install as Windows Services (recommended)"; GroupDescription: "Installation type:"; Flags: checkedonce
Name: "installPortable"; Description: "Install as Portable (no services)"; GroupDescription: "Installation type:"
Name: "desktopIcon"; Description: "Create Desktop shortcut"; GroupDescription: "Additional options:"
Name: "startMenuIcon"; Description: "Create Start Menu shortcut"; GroupDescription: "Additional options:"

[Files]
; API files
Source: "..\publish\api\*"; DestDir: "{app}\API"; Flags: ignoreversion recursesubdirs
; Agent files
Source: "..\publish\agent\*"; DestDir: "{app}\Agent"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName} Dashboard"; Filename: "http://localhost:5263"; IconFilename: "{app}\API\settAPI.exe"; Tasks: startMenuIcon
Name: "{autoprograms}\{#MyAppName} API (Console)"; Filename: "{app}\API\settAPI.exe"; Tasks: startMenuIcon And Not installService
Name: "{autoprograms}\{#MyAppName} Agent (Console)"; Filename: "{app}\Agent\settAGENT.exe"; Tasks: startMenuIcon And Not installService
Name: "{userdesktop}\{#MyAppName} Dashboard"; Filename: "http://localhost:5263"; Tasks: desktopIcon

[Run]
; Create Windows Services (run hidden, don't wait)
Filename: "sc"; Parameters: "create SETTAPI binPath= ""{app}\API\settAPI.exe"" start= auto"; Flags: runhidden; Tasks: installService; StatusMsg: "Creating SETT API service..."
Filename: "sc"; Parameters: "create SETTAGENT binPath= ""{app}\Agent\settAGENT.exe"" start= auto"; Flags: runhidden; Tasks: installService; StatusMsg: "Creating SETT Agent service..."

; Start the services
Filename: "sc"; Parameters: "start SETTAPI"; Flags: runhidden; Tasks: installService; StatusMsg: "Starting SETT API service..."
Filename: "sc"; Parameters: "start SETTAGENT"; Flags: runhidden; Tasks: installService; StatusMsg: "Starting SETT Agent service..."

; Portable mode - launch apps without waiting
Filename: "{app}\API\settAPI.exe"; Flags: runhidden nowait; Tasks: installPortable; Description: "Launch SETT API"
Filename: "{app}\Agent\settAGENT.exe"; Flags: runhidden nowait; Tasks: installPortable; Description: "Launch SETT Agent"

[UninstallRun]
Filename: "cmd"; Parameters: "/c sc stop SETTAPI 2>nul & sc delete SETTAPI 2>nul"; Flags: runhidden; RunOnceId: "RemoveSETTAPI"; Tasks: installService
Filename: "cmd"; Parameters: "/c sc stop SETTAGENT 2>nul & sc delete SETTAGENT 2>nul"; Flags: runhidden; RunOnceId: "RemoveSETTAGENT"; Tasks: installService

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  
  if MsgBox('SETT requires PostgreSQL to be running.' + #13#10 + #13#10 +
            'Make sure PostgreSQL is running (e.g., "docker compose up -d" in the backend folder).' + #13#10 +
            'Do you want to continue with the installation?', mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    MsgBox('SETT has been uninstalled, but Windows Services may still be registered.' + #13#10 +
           'If services still exist, run these commands as Administrator:' + #13#10 + #13#10 +
           '  sc stop SETTAPI' + #13#10 + '  sc delete SETTAPI' + #13#10 + 
           '  sc stop SETTAGENT' + #13#10 + '  sc delete SETTAGENT', mbInformation, MB_OK);
  end;
end;