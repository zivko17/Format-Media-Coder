; ── Instalador de Format Media Coder (Inno Setup) ───────────────────────────
; Genera un Setup.exe clásico de Windows a partir de la carpeta dist\ que crea
; publish.cmd. Requiere Inno Setup (https://jrsoftware.org/isdl.php).
;
; Uso: primero ejecuta publish.cmd; luego abre este .iss con Inno Setup y pulsa
;      Compile (o ejecuta:  iscc installer\FormatMediaCoder.iss ).

#define AppName "Format Media Coder"
#define AppVersion "2.0.0"
#define AppExe "FormatMediaCoder.exe"
#define AppPublisher "Andrés"

[Setup]
AppId={{7F3B2A10-2C4E-4E9A-9E2F-FMC200000000}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\dist-installer
OutputBaseFilename=FormatMediaCoder-Setup-v{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\build\icon.ico
UninstallDisplayIcon={app}\{#AppExe}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Todo el contenido publicado (incluye el exe autónomo y bin\ffmpeg si lo copiaste)
Source: "..\dist\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
